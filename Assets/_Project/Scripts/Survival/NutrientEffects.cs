using UnityEngine;

namespace Evolve.Survival
{
    /// <summary>
    /// What a low store does to the body. Every function is 1 (no effect) from 40 upward and moves linearly to its
    /// bound at 0. Values are the store (0..100) of the nutrient concerned.
    /// </summary>
    public static class NutrientEffects
    {
        public const float Threshold = 40f;

        static float Towards(float value, float boundAtZero)
        {
            if (value >= Threshold) return 1f;
            return Mathf.Lerp(boundAtZero, 1f, Mathf.Clamp01(value / Threshold));
        }

        /// <summary>Sợi Nhầy (protein): weak muscle, slow healing.</summary>
        public static float HealMultiplier(float protein) => Towards(protein, 0.4f);
        /// <summary>Tinh Vôi (calcium): brittle bones, hits hurt more.</summary>
        public static float DamageTakenMultiplier(float calcium) => Towards(calcium, 1.4f);
        /// <summary>Huyết Thiết (iron): anaemia, lower maximum health.</summary>
        public static float MaxHealthMultiplier(float iron) => Towards(iron, 0.7f);
        /// <summary>Huyết Thiết (iron): anaemia, tires quickly.</summary>
        public static float EnergyRegenMultiplier(float iron) => Towards(iron, 0.6f);
        /// <summary>Mỡ Nhờn (fat): no insulation, body temperature follows the air faster.</summary>
        public static float TemperatureDriftMultiplier(float fat) => Towards(fat, 1.8f);
        /// <summary>Tinh Chất Sống (vitamin): dull senses.</summary>
        public static float SenseMultiplier(float vitamin) => Towards(vitamin, 0.6f);
    }
}
