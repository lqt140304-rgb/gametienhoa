using System;

namespace Evolve.Survival
{
    /// <summary>
    /// Pure data + rules for survival stats. No MonoBehaviour, so it can later be
    /// owned by a server (Netcode) and mirrored to clients without changes.
    /// </summary>
    [Serializable]
    public class SurvivalState
    {
        public float MaxHealth = 100f;
        public float MaxHunger = 100f;
        public float MaxEnergy = 100f;

        public float Health;
        public float Hunger;   // 100 = full, 0 = starving
        public float Energy;

        public float MaxThirst = 100f;
        public float Thirst;   // 100 = quenched, 0 = dehydrated
        public const float MaxStress = 100f;
        /// <summary>0 = calm, 100 = panic.</summary>
        public float Stress;
        /// <summary>Thirst only drains where the world turns it on.</summary>
        [NonSerialized] public bool ThirstEnabled;
        /// <summary>Inside a shelter: faster energy regen and stress relief.</summary>
        [NonSerialized] public bool InShelter;
        /// <summary>Set by SurvivalStats; used by Damage() for stress. Null = no stress from hits.</summary>
        [NonSerialized] public SurvivalConfig Config;

        /// <summary>Evolution EXP waiting to be spent on organs (kept on death).</summary>
        public float EvolutionExp;

        /// <summary>Body temperature, °C.</summary>
        public float Temperature = 22f;
        /// <summary>Air temperature around the body (set by ClimateSensor).</summary>
        [NonSerialized] public float Ambient = 22f;
        /// <summary>Extra degrees of heat tolerated (from organs).</summary>
        [NonSerialized] public float HeatResistance;

        [UnityEngine.Tooltip("Lives per player. Each death costs one; at 0 the player can no longer respawn on their own.")]
        public int MaxLives = 3;
        /// <summary>Lives left, counting the one in use. 0 = out of lives.</summary>
        [NonSerialized] public int Lives = 3;

        /// <summary>Debug: ignore all damage (God menu).</summary>
        [NonSerialized] public bool Invulnerable;

        public bool IsDead => Health <= 0f;
        /// <summary>Dead and can respawn by themselves (still has lives).</summary>
        public bool CanRespawn => IsDead && Lives > 0;
        /// <summary>Dead with no lives left: a corpse that only teammates can rescue.</summary>
        public bool IsCorpse => IsDead && Lives <= 0;

        public event Action Changed;

        /// <summary>Co-op: the corpse was not rescued in time; it is gone for good and its owner only spectates.</summary>
        [NonSerialized] public bool Lost;

        /// <summary>A teammate pulled this corpse back: alive again with a little health, continuing the same life.</summary>
        public void Revive(float health)
        {
            if (!IsCorpse || Lost) return;
            Lives = 1;
            Health = Clamp(health, MaxHealth);
            Changed?.Invoke();
        }

        /// <summary>Health given by a teammate (blood transfer).</summary>
        public void Heal(float amount)
        {
            if (IsDead || amount <= 0f) return;
            Health = Clamp(Health + amount, MaxHealth);
            Changed?.Invoke();
        }

        /// <summary>Health given away; never kills (stops at 1).</summary>
        public void Drain(float amount)
        {
            if (IsDead || amount <= 0f) return;
            Health = UnityEngine.Mathf.Max(1f, Health - amount);
            Changed?.Invoke();
        }

        /// <summary>
        /// Optional hook that takes a share of eaten food (e.g. for growing organs).
        /// Receives the amount eaten, returns what is left for hunger.
        /// </summary>
        public Func<float, float> FoodFilter;
        public event Action Died;

        /// <summary>Give back all lives (start of a run).</summary>
        public void ResetLives()
        {
            Lives = MaxLives;
            Lost = false;
            Changed?.Invoke();
        }

        public void Reset()
        {
            Health = MaxHealth;
            Hunger = MaxHunger;
            Energy = MaxEnergy;
            Thirst = MaxThirst;
            Stress = 0f;
            Temperature = Ambient;
            Changed?.Invoke();
        }

        /// <summary>Advance stats by dt seconds.</summary>
        public void Tick(float dt, SurvivalConfig cfg, bool isMoving, bool isSprinting)
        {
            if (IsDead) return;

            // Thirst (only where enabled).
            if (ThirstEnabled)
            {
                float drain = cfg.ThirstDecayPerSec
                    + (isSprinting ? cfg.ThirstSprintExtraPerSec : 0f)
                    + (Ambient > cfg.ComfortMax - 5f ? cfg.ThirstHotExtraPerSec : 0f);
                Thirst = Clamp(Thirst - drain * dt, MaxThirst);
                if (Thirst <= 0f) ApplyDamage(cfg.DehydrationDamagePerSec * dt);
            }

            // Metabolism only burns food while active; resting costs nothing.
            float burn = (isMoving ? cfg.HungerDecayPerSec : 0f) + (isSprinting ? cfg.SprintHungerPerSec : 0f);
            Hunger = Clamp(Hunger - burn * dt, MaxHunger);

            // Energy comes back even while moving, just slower than when resting. Not while starving.
            if (Hunger > 0f)
            {
                float regen = isMoving ? cfg.EnergyRegenMovingPerSec : cfg.EnergyRegenPerSec;
                if (InShelter) regen *= cfg.ShelterEnergyRegenMultiplier;
                regen *= StressEffects.EnergyRegenMultiplier(Stress, cfg);
                Energy = Clamp(Energy + regen * dt, MaxEnergy);
            }

            bool thirstOk = !ThirstEnabled || Thirst >= MaxThirst * cfg.HealThirstThreshold;
            if (Hunger <= 0f)
                ApplyDamage(cfg.StarveDamagePerSec * dt);
            else if (Hunger >= MaxHunger * cfg.HealHungerThreshold && thirstOk)
                Health = Clamp(Health + cfg.HealPerSec * dt, MaxHealth);

            // Body temperature drifts toward the air; outside the comfort range it hurts.
            Temperature = MoveTowards(Temperature, Ambient, cfg.TemperatureChangePerSec * dt);
            float over = Temperature - (cfg.ComfortMax + HeatResistance);
            float under = cfg.ComfortMin - Temperature;
            if (over > 0f) ApplyDamage(over * cfg.DamagePerDegreePerSec * dt);
            else if (under > 0f) ApplyDamage(under * cfg.DamagePerDegreePerSec * dt);

            // Stress: rises with unmet needs and bad temperature, falls when calm or sheltered.
            bool lowNeeds = Hunger < MaxHunger * 0.25f || (ThirstEnabled && Thirst < MaxThirst * 0.25f);
            bool badTemp = over > 0f || under > 0f;
            bool calm = Hunger >= MaxHunger * 0.7f && (!ThirstEnabled || Thirst >= MaxThirst * 0.5f);
            float dStress = 0f;
            if (lowNeeds) dStress += cfg.StressLowNeedsPerSec;
            if (badTemp) dStress += cfg.StressBadTempPerSec;
            if (calm) dStress -= cfg.StressCalmDecayPerSec;
            if (InShelter) dStress -= cfg.StressShelterDecayPerSec;
            Stress = StressClamp(Stress + dStress * dt);

            Changed?.Invoke();
        }

        /// <summary>Highest body temperature tolerated with current organs.</summary>
        public float HeatLimit(SurvivalConfig cfg) => cfg.ComfortMax + HeatResistance;

        public void AddExp(float amount)
        {
            if (amount <= 0f) return;
            EvolutionExp += amount;
            Changed?.Invoke();
        }

        public bool TrySpendEnergy(float amount)
        {
            if (Energy < amount) return false;
            Energy -= amount;
            Changed?.Invoke();
            return true;
        }

        public void Feed(float nutrition)
        {
            if (IsDead) return;
            if (FoodFilter != null) nutrition = FoodFilter(nutrition);
            Hunger = Clamp(Hunger + nutrition, MaxHunger);
            Changed?.Invoke();
        }

        /// <summary>Quench thirst.</summary>
        public void Drink(float amount)
        {
            if (IsDead || amount <= 0f) return;
            Thirst = Clamp(Thirst + amount, MaxThirst);
            Changed?.Invoke();
        }

        /// <summary>Damage from outside (a bite): also adds stress. Needs/temperature damage uses ApplyDamage.</summary>
        public void Damage(float amount)
        {
            if (IsDead || Invulnerable) return;
            if (Config != null && amount > 0f) Stress = StressClamp(Stress + amount * Config.StressPerDamage);
            ApplyDamage(amount);
        }

        void ApplyDamage(float amount)
        {
            if (IsDead || Invulnerable) return;
            Health = Clamp(Health - amount, MaxHealth);
            Changed?.Invoke();
            if (IsDead)
            {
                if (Lives > 0) Lives--;
                Died?.Invoke();
            }
        }

        static float MoveTowards(float v, float target, float step) =>
            v < target ? (v + step > target ? target : v + step) : (v - step < target ? target : v - step);

        static float StressClamp(float v) => v < 0f ? 0f : (v > MaxStress ? MaxStress : v);

        static float Clamp(float v, float max) => v < 0f ? 0f : (v > max ? max : v);
    }
}
