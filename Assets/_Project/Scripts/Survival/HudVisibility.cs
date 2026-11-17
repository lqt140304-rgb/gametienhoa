namespace Evolve.Survival
{
    /// <summary>Which HUD stat bars to show: only those with a problem, or everything while Tab is held.</summary>
    public static class HudVisibility
    {
        public const float HealthBelow = 0.7f;
        public const float EnergyBelow = 0.5f;
        public const float HungerBelow = 0.5f;
        public const float ThirstBelow = 0.5f;
        public const float StressAbove = 40f;
        public const float NutrientBelow = 40f;

        public static bool ShowHealth(float frac, bool full = false) => full || frac < HealthBelow;
        public static bool ShowEnergy(float frac, bool full = false) => full || frac < EnergyBelow;
        public static bool ShowHunger(float frac, bool full = false) => full || frac < HungerBelow;
        public static bool ShowThirst(float frac, bool enabled, bool full = false) => enabled && (full || frac < ThirstBelow);
        public static bool ShowStress(float stress, bool full = false) => full || stress > StressAbove;
        public static bool ShowNutrient(float value, bool full = false) => full || value < NutrientBelow;
        public static bool ShowTemperature(bool unsafeTemp, bool full = false) => full || unsafeTemp;
    }
}
