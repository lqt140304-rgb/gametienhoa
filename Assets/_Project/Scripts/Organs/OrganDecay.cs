using UnityEngine;

namespace Evolve.Organs
{
    /// <summary>How an organ weakens when the nutrient it needs runs out, and how it recovers (decay 0 = healthy, 1 = dissolved).</summary>
    public static class OrganDecay
    {
        /// <summary>
        /// Below the warning threshold the organ decays and dissolves after <paramref name="dissolveSeconds"/>;
        /// at or above the equip threshold it recovers over <paramref name="recoverSeconds"/>; in between nothing changes.
        /// </summary>
        public static float Step(float decay, float nutrientValue, float equipThreshold, float warnThreshold,
            float dissolveSeconds, float recoverSeconds, float dt)
        {
            if (nutrientValue < warnThreshold) decay += dt / Mathf.Max(0.01f, dissolveSeconds);
            else if (nutrientValue >= equipThreshold) decay -= dt / Mathf.Max(0.01f, recoverSeconds);
            return Mathf.Clamp01(decay);
        }

        /// <summary>Effect strength left at a given decay: 1 when healthy, 0.2 just before dissolving.</summary>
        public static float StrengthFactor(float decay) => 1f - 0.8f * Mathf.Clamp01(decay);
    }
}
