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

        /// <summary>Body temperature, °C.</summary>
        public float Temperature = 22f;
        /// <summary>Air temperature around the body (set by ClimateSensor).</summary>
        [NonSerialized] public float Ambient = 22f;
        /// <summary>Extra degrees of heat tolerated (from organs).</summary>
        [NonSerialized] public float HeatResistance;

        public bool IsDead => Health <= 0f;

        public event Action Changed;

        /// <summary>
        /// Optional hook that takes a share of eaten food (e.g. for growing organs).
        /// Receives the amount eaten, returns what is left for hunger.
        /// </summary>
        public Func<float, float> FoodFilter;
        public event Action Died;

        public void Reset()
        {
            Health = MaxHealth;
            Hunger = MaxHunger;
            Energy = MaxEnergy;
            Temperature = Ambient;
            Changed?.Invoke();
        }

        /// <summary>Advance stats by dt seconds.</summary>
        public void Tick(float dt, SurvivalConfig cfg, bool isMoving, bool isSprinting)
        {
            if (IsDead) return;

            // Metabolism only burns food while active; resting costs nothing.
            float burn = (isMoving ? cfg.HungerDecayPerSec : 0f) + (isSprinting ? cfg.SprintHungerPerSec : 0f);
            Hunger = Clamp(Hunger - burn * dt, MaxHunger);

            if (Hunger > 0f && !isMoving)
                Energy = Clamp(Energy + cfg.EnergyRegenPerSec * dt, MaxEnergy);

            if (Hunger <= 0f)
                Damage(cfg.StarveDamagePerSec * dt);
            else if (Hunger >= MaxHunger * cfg.HealHungerThreshold)
                Health = Clamp(Health + cfg.HealPerSec * dt, MaxHealth);

            // Body temperature drifts toward the air; outside the comfort range it hurts.
            Temperature = MoveTowards(Temperature, Ambient, cfg.TemperatureChangePerSec * dt);
            float over = Temperature - (cfg.ComfortMax + HeatResistance);
            float under = cfg.ComfortMin - Temperature;
            if (over > 0f) Damage(over * cfg.DamagePerDegreePerSec * dt);
            else if (under > 0f) Damage(under * cfg.DamagePerDegreePerSec * dt);

            Changed?.Invoke();
        }

        /// <summary>Highest body temperature tolerated with current organs.</summary>
        public float HeatLimit(SurvivalConfig cfg) => cfg.ComfortMax + HeatResistance;

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

        public void Damage(float amount)
        {
            if (IsDead) return;
            Health = Clamp(Health - amount, MaxHealth);
            Changed?.Invoke();
            if (IsDead) Died?.Invoke();
        }

        static float MoveTowards(float v, float target, float step) =>
            v < target ? (v + step > target ? target : v + step) : (v - step < target ? target : v - step);

        static float Clamp(float v, float max) => v < 0f ? 0f : (v > max ? max : v);
    }
}
