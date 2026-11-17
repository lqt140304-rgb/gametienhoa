using UnityEngine;

namespace Evolve.Survival
{
    /// <summary>
    /// Where each disease comes from, what it does and how it goes away. Pure rules; all numbers are placeholders to
    /// tune while playing. Each disease is a timer of seconds left: it runs down by itself, faster with the right cure.
    /// </summary>
    public static class DiseaseRules
    {
        public const int Count = 3;

        // Sources
        public const float DirtyWaterPerSecond = 0.04f;  // chance per second while drinking stagnant water
        public const float RottenBiteChance = 0.35f;     // per bite of a rotten carcass
        public const float HitChance = 0.08f;            // per hard hit from outside
        public const float HitMinDamage = 3f;
        public const float SporePerSecond = 0.05f;       // chance per second inside a spore cloud
        public const float ContagionPerSecond = 0.02f;   // chance per second next to someone with Thối Gel
        public const float ContagionRange = 3f;

        // Symptoms
        public const float HealthDrainFloor = 0.1f;      // Thối Gel never drains below this fraction of max health

        public static string Name(DiseaseKind k) => k switch
        {
            DiseaseKind.StagnantWater => "Nhiễm Nước Đọng",
            DiseaseKind.RottenGel => "Thối Gel",
            _ => "Mờ Bào Tử"
        };

        /// <summary>How to get better, in plain words.</summary>
        public static string Cure(DiseaseKind k) => k switch
        {
            DiseaseKind.StagnantWater => "nghỉ ở nơi trú và uống nước sạch",
            DiseaseKind.RottenGel => "ăn thức ăn giàu Tinh Chất Sống",
            _ => "tránh xa vùng bào tử, ăn đủ Tinh Chất Sống"
        };

        /// <summary>Seconds the disease lasts if nothing speeds the recovery.</summary>
        public static float Duration(DiseaseKind k) => k switch
        {
            DiseaseKind.StagnantWater => 180f,
            DiseaseKind.RottenGel => 240f,
            _ => 150f
        };

        public static bool Contagious(DiseaseKind k) => k == DiseaseKind.RottenGel;

        /// <summary>Low Tinh Chất Sống (vitamin) makes every infection likelier: 1 from 40 up, 3 at 0.</summary>
        public static float InfectionMultiplier(float vitamin)
        {
            if (vitamin >= NutrientEffects.Threshold) return 1f;
            return Mathf.Lerp(3f, 1f, Mathf.Clamp01(vitamin / NutrientEffects.Threshold));
        }

        /// <summary>Seconds of disease lost per second.</summary>
        public static float RecoveryRate(DiseaseKind k, bool inShelter, bool inCleanWater, float vitamin)
        {
            float rate = 1f;
            if (inShelter) rate *= 2f;
            if (k == DiseaseKind.StagnantWater && inCleanWater) rate *= 3f;
            if (k == DiseaseKind.SporeBlur && vitamin >= 60f) rate *= 2f;
            return rate;
        }

        /// <summary>Seconds of disease lost at once by eating food rich in Tinh Chất Sống (only Thối Gel is cured by food).</summary>
        public static float VitaminCure(DiseaseKind k, float vitaminGained) => k == DiseaseKind.RottenGel ? 6f * Mathf.Max(0f, vitaminGained) : 0f;

        public static float ThirstDrainMultiplier(bool stagnant) => stagnant ? 1.6f : 1f;
        public static float HealMultiplier(bool stagnant) => stagnant ? 0.6f : 1f;
        public static float HealthDrainPerSecond(bool rotten) => rotten ? 0.3f : 0f;
        public static float OrganRecoverMultiplier(bool rotten) => rotten ? 0.5f : 1f;
        public static float SenseMultiplier(bool spore) => spore ? 0.6f : 1f;
        public static float StressPerSecond(bool spore) => spore ? 0.5f : 0f;
    }
}
