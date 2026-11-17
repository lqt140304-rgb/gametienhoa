namespace Evolve.Survival
{
    /// <summary>How stress bends the senses and the body. Pure functions.</summary>
    public static class StressEffects
    {
        /// <summary>Camera shake amplitude: 0 below StressShakeStart, rising linearly to 0.05 at 100.</summary>
        public static float CameraShake(float stress, SurvivalConfig cfg)
        {
            float start = cfg.StressShakeStart;
            if (stress <= start) return 0f;
            float t = (stress - start) / (SurvivalState.MaxStress - start);
            return 0.05f * (t > 1f ? 1f : t);
        }

        /// <summary>Sense radius multiplier: 1 below StressSenseStart, falling linearly to 0.6 at 100.</summary>
        public static float SenseRadiusMultiplier(float stress, SurvivalConfig cfg)
        {
            float start = cfg.StressSenseStart;
            if (stress <= start) return 1f;
            float t = (stress - start) / (SurvivalState.MaxStress - start);
            return 1f - 0.4f * (t > 1f ? 1f : t);
        }

        /// <summary>Energy regen multiplier: 1 below StressEnergyStart, StressHighEnergyRegenMultiplier from there up.</summary>
        public static float EnergyRegenMultiplier(float stress, SurvivalConfig cfg) =>
            stress >= cfg.StressEnergyStart ? cfg.StressHighEnergyRegenMultiplier : 1f;
    }
}
