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

        public const float MaxNutrient = 100f;
        public const float DefaultNutrientStart = 80f;
        /// <summary>The five stores (Sợi Nhầy, Tinh Vôi, Huyết Thiết, Mỡ Nhờn, Tinh Chất Sống), indexed by NutrientKind.</summary>
        public float[] Nutrients = { DefaultNutrientStart, DefaultNutrientStart, DefaultNutrientStart, DefaultNutrientStart, DefaultNutrientStart };
        /// <summary>How many equipped organs need each nutrient (written by OrganHost).</summary>
        [NonSerialized] public int[] NutrientLoad = new int[NutrientKinds.Count];

        /// <summary>Seconds left of each disease, indexed by DiseaseKind (above 0 = sick).</summary>
        public float[] DiseaseTime = new float[DiseaseRules.Count];
        /// <summary>Set by WaterSensor: standing in clean water (speeds recovery from Nhiễm Nước Đọng).</summary>
        [NonSerialized] public bool InCleanWater;
        /// <summary>Dice for infections (0..1); tests replace it with a fixed value.</summary>
        [NonSerialized] public Func<float> Roll = DefaultRoll;
        static float DefaultRoll() => UnityEngine.Random.value;
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
            Thirst = UnityEngine.Mathf.Max(Thirst, MaxThirst * 0.5f);
            EnsureNutrients();
            for (int k = 0; k < Nutrients.Length; k++) Nutrients[k] = UnityEngine.Mathf.Max(Nutrients[k], 50f);
            Stress = UnityEngine.Mathf.Min(Stress, 50f);
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

        // Flaws (Khuyết Điểm), set by PlayerTraits; the defaults change nothing.
        /// <summary>Đói Sớm: multiplies how fast hunger burns.</summary>
        [NonSerialized] public float HungerBurnMultiplier = 1f;
        /// <summary>Dạ Dày Kén: how strongly the sluggish stomach is felt (0 = no such flaw).</summary>
        [NonSerialized] public float AbsorbFlawStrength;
        /// <summary>Nhân Màng Thẩm Thấu: hunger points gained per second right now (0 = none).</summary>
        [NonSerialized] public float HungerGainPerSec;
        /// <summary>Nhân Quang Hợp: extra energy per second right now (comes even while starving).</summary>
        [NonSerialized] public float EnergyBonusPerSec;
        /// <summary>Ngại Đông: multiplies energy regeneration.</summary>
        [NonSerialized] public float EnergyRegenMultiplier = 1f;
        /// <summary>Raised with the amount whenever a hit from outside lands (Run Rẩy listens).</summary>
        public event Action<float> Damaged;
        [NonSerialized] float clock;
        [NonSerialized] float lastNewFoodAt = -1e6f;
        [NonSerialized] System.Collections.Generic.HashSet<string> eatenFoods;

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
            EnsureDiseases();
            for (int k = 0; k < DiseaseTime.Length; k++) DiseaseTime[k] = 0f;
            EnsureNutrients();
            float start = Config != null ? Config.NutrientStart : DefaultNutrientStart;
            for (int k = 0; k < Nutrients.Length; k++) Nutrients[k] = start;
            Temperature = Ambient;
            Changed?.Invoke();
        }

        /// <summary>Advance stats by dt seconds.</summary>
        public void Tick(float dt, SurvivalConfig cfg, bool isMoving, bool isSprinting)
        {
            if (IsDead) return;
            clock += dt;

            // Thirst (only where enabled).
            if (ThirstEnabled)
            {
                float drain = cfg.ThirstDecayPerSec
                    + (isSprinting ? cfg.ThirstSprintExtraPerSec : 0f)
                    + (Ambient > cfg.ComfortMax - 5f ? cfg.ThirstHotExtraPerSec : 0f);
                drain *= DiseaseRules.ThirstDrainMultiplier(Has(DiseaseKind.StagnantWater));
                Thirst = Clamp(Thirst - drain * dt, MaxThirst);
                if (Thirst <= 0f) ApplyDamage(cfg.DehydrationDamagePerSec * dt);
            }

            // Nutrient stores slowly run down; each equipped organ that needs one makes it run down faster.
            EnsureNutrients();
            for (int k = 0; k < Nutrients.Length; k++)
            {
                float load = NutrientLoad != null && k < NutrientLoad.Length ? NutrientLoad[k] : 0f;
                Nutrients[k] = Clamp(Nutrients[k] - (cfg.NutrientBaseDecayPerSec + load * cfg.NutrientOrganDecayPerSec) * dt, MaxNutrient);
            }

            // Diseases run down by themselves, faster with the right cure; Thối Gel also drains health (never below the floor).
            EnsureDiseases();
            float vitaminNow = Nutrients[(int)NutrientKind.Vitamin];
            for (int k = 0; k < DiseaseTime.Length; k++)
                if (DiseaseTime[k] > 0f)
                    DiseaseTime[k] = Clamp(DiseaseTime[k] - DiseaseRules.RecoveryRate((DiseaseKind)k, InShelter, InCleanWater, vitaminNow) * dt, 1000f);
            float gelFloor = DiseaseRules.HealthDrainFloor * MaxHealth;
            if (Has(DiseaseKind.RottenGel) && Health > gelFloor && !Invulnerable)
                Health = UnityEngine.Mathf.Max(gelFloor, Health - DiseaseRules.HealthDrainPerSecond(true) * dt); // never drains below the floor

            // Metabolism only burns food while active; resting costs nothing.
            float burn = (isMoving ? cfg.HungerDecayPerSec : 0f) + (isSprinting ? cfg.SprintHungerPerSec : 0f);
            Hunger = Clamp(Hunger - burn * HungerBurnMultiplier * dt + HungerGainPerSec * dt, MaxHunger);

            // Energy comes back even while moving, just slower than when resting. Not while starving.
            if (Hunger > 0f)
            {
                float regen = isMoving ? cfg.EnergyRegenMovingPerSec : cfg.EnergyRegenPerSec;
                if (InShelter) regen *= cfg.ShelterEnergyRegenMultiplier;
                regen *= StressEffects.EnergyRegenMultiplier(Stress, cfg);
                regen *= NutrientEffects.EnergyRegenMultiplier(Nutrients[(int)NutrientKind.Iron]);
                regen *= EnergyRegenMultiplier;
                Energy = Clamp(Energy + regen * dt, MaxEnergy);
            }
            if (EnergyBonusPerSec > 0f) Energy = Clamp(Energy + EnergyBonusPerSec * dt, MaxEnergy);

            // Anaemia (low Huyết Thiết) lowers the maximum health; low Sợi Nhầy slows healing.
            float healthCap = MaxHealth * NutrientEffects.MaxHealthMultiplier(Nutrients[(int)NutrientKind.Iron]);
            if (Health > healthCap) Health = healthCap;
            bool thirstOk = !ThirstEnabled || Thirst >= MaxThirst * cfg.HealThirstThreshold;
            if (Hunger <= 0f)
                ApplyDamage(cfg.StarveDamagePerSec * dt);
            else if (Hunger >= MaxHunger * cfg.HealHungerThreshold && thirstOk)
                Health = Clamp(Health + cfg.HealPerSec * NutrientEffects.HealMultiplier(Nutrients[(int)NutrientKind.Protein])
                    * DiseaseRules.HealMultiplier(Has(DiseaseKind.StagnantWater)) * DiseaseRules.RottenHealMultiplier(Has(DiseaseKind.RottenGel)) * dt, healthCap);

            // Body temperature drifts toward the air; outside the comfort range it hurts.
            Temperature = MoveTowards(Temperature, Ambient,
                cfg.TemperatureChangePerSec * NutrientEffects.TemperatureDriftMultiplier(Nutrients[(int)NutrientKind.Fat]) * dt);
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
            dStress += DiseaseRules.StressPerSecond(Has(DiseaseKind.SporeBlur));
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

        public void Feed(float nutrition) => Feed(nutrition, FoodProfiles.Mixed);

        /// <summary>Eat: fills hunger, and adds nutrient points following the food's profile.</summary>
        public void Feed(float nutrition, FoodProfile profile)
        {
            if (IsDead) return;
            // Dạ Dày Kén: a food never eaten before makes the stomach sluggish for a while.
            if (AbsorbFlawStrength > 0f)
            {
                eatenFoods ??= new System.Collections.Generic.HashSet<string>();
                string key = $"{profile.Of(NutrientKind.Protein)}|{profile.Of(NutrientKind.Calcium)}|{profile.Of(NutrientKind.Iron)}|{profile.Of(NutrientKind.Fat)}|{profile.Of(NutrientKind.Vitamin)}";
                if (eatenFoods.Add(key)) lastNewFoodAt = clock;
                nutrition *= Evolve.Traits.FlawRules.AbsorbMultiplier(clock - lastNewFoodAt, AbsorbFlawStrength);
            }
            float eaten = nutrition;
            if (FoodFilter != null) nutrition = FoodFilter(nutrition);
            Hunger = Clamp(Hunger + nutrition, MaxHunger);
            EnsureNutrients();
            float gain = (Config != null ? Config.NutrientGainPerFood : 1f) * eaten;
            for (int k = 0; k < Nutrients.Length; k++)
                Nutrients[k] = Clamp(Nutrients[k] + gain * profile.Of((NutrientKind)k), MaxNutrient);
            // Food rich in Tinh Chất Sống clears Thối Gel faster.
            EnsureDiseases();
            int gel = (int)DiseaseKind.RottenGel;
            if (DiseaseTime[gel] > 0f)
                DiseaseTime[gel] = Clamp(DiseaseTime[gel] - DiseaseRules.VitaminCure(DiseaseKind.RottenGel, gain * profile.Vitamin), 1000f);
            Changed?.Invoke();
        }

        public bool Has(DiseaseKind k) { EnsureDiseases(); return DiseaseTime[(int)k] > 0f; }

        /// <summary>One bit per disease (bit = 1 &lt;&lt; DiseaseKind), sent with the player state.</summary>
        public byte DiseaseMask
        {
            get
            {
                EnsureDiseases();
                int mask = 0;
                for (int k = 0; k < DiseaseTime.Length; k++) if (DiseaseTime[k] > 0f) mask |= 1 << k;
                return (byte)mask;
            }
        }

        /// <summary>For the ghost of another player: show the diseases the network says they have (the ghost is never ticked).</summary>
        public void SetDiseaseMask(byte mask)
        {
            EnsureDiseases();
            for (int k = 0; k < DiseaseTime.Length; k++) DiseaseTime[k] = (mask & (1 << k)) != 0 ? 1f : 0f;
        }

        public void Infect(DiseaseKind kind)
        {
            EnsureDiseases();
            DiseaseTime[(int)kind] = UnityEngine.Mathf.Max(DiseaseTime[(int)kind], DiseaseRules.Duration(kind));
            Changed?.Invoke();
        }

        public void Cure(DiseaseKind kind)
        {
            EnsureDiseases();
            DiseaseTime[(int)kind] = 0f;
            Changed?.Invoke();
        }

        /// <summary>Roll for an infection; low Tinh Chất Sống makes it likelier. Returns true if the body is now infected.</summary>
        public bool TryInfect(DiseaseKind kind, float probability)
        {
            if (IsDead || probability <= 0f || Has(kind)) return false;
            float p = probability * DiseaseRules.InfectionMultiplier(Nutrient(NutrientKind.Vitamin));
            float roll = Roll != null ? Roll() : UnityEngine.Random.value;
            if (roll >= p) return false;
            Infect(kind);
            return true;
        }

        void EnsureDiseases()
        {
            if (DiseaseTime == null || DiseaseTime.Length != DiseaseRules.Count)
            {
                var old = DiseaseTime;
                DiseaseTime = new float[DiseaseRules.Count];
                if (old != null) for (int k = 0; k < DiseaseTime.Length && k < old.Length; k++) DiseaseTime[k] = old[k];
            }
        }

        public float Nutrient(NutrientKind k) { EnsureNutrients(); return Nutrients[(int)k]; }

        /// <summary>Below the equip threshold: new organs needing this nutrient cannot be equipped.</summary>
        public bool IsLacking(NutrientKind k) => Nutrient(k) < (Config != null ? Config.NutrientEquipThreshold : NutrientEffects.Threshold);

        /// <summary>Below the warning threshold: organs needing this nutrient are weakening.</summary>
        public bool IsWarning(NutrientKind k) => Nutrient(k) < (Config != null ? Config.NutrientWarnThreshold : 25f);

        public NutrientKind LowestNutrient()
        {
            EnsureNutrients();
            int best = 0;
            for (int k = 1; k < Nutrients.Length; k++) if (Nutrients[k] < Nutrients[best]) best = k;
            return (NutrientKind)best;
        }

        void EnsureNutrients()
        {
            if (Nutrients == null || Nutrients.Length != NutrientKinds.Count)
            {
                var old = Nutrients;
                Nutrients = new float[NutrientKinds.Count];
                for (int k = 0; k < Nutrients.Length; k++) Nutrients[k] = old != null && k < old.Length ? old[k] : DefaultNutrientStart;
            }
        }

        /// <summary>Quench thirst.</summary>
        public void Drink(float amount)
        {
            if (IsDead || amount <= 0f) return;
            Thirst = Clamp(Thirst + amount, MaxThirst);
            Changed?.Invoke();
        }

        /// <summary>Filters a hit before it lands (shield, Thu Mình, Vỏ Cứng). Set by Abilities/BodyState.</summary>
        [NonSerialized] public Func<float, float> IncomingDamage;

        /// <summary>Damage from outside (a bite): also adds stress. Needs/temperature damage uses ApplyDamage.</summary>
        public void Damage(float amount)
        {
            if (IsDead || Invulnerable) return;
            if (IncomingDamage != null) amount = IncomingDamage(amount);   // skill defences (Abilities/BodyState)
            if (amount <= 0f) return;
            if (amount > 0f) Damaged?.Invoke(amount);
            if (Config != null && amount > 0f) Stress = StressClamp(Stress + amount * Config.StressPerDamage);
            if (amount >= DiseaseRules.HitMinDamage) TryInfect(DiseaseKind.RottenGel, DiseaseRules.HitChance); // an infected wound
            ApplyDamage(amount * NutrientEffects.DamageTakenMultiplier(Nutrient(NutrientKind.Calcium))); // brittle bones: hits hurt more
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
