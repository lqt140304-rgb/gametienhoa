using System;

namespace Evolve.Combat
{
    /// <summary>The push of a wind gust (Gió Hú): full on a light slime, half on a heavy one, none on a slime that holds on.</summary>
    public static class WindRules
    {
        public static float PushSpeed(float force, float slimeMass, bool resists)
        {
            if (resists) return 0f;
            float heavy = Math.Min(1f, Math.Max(0f, (slimeMass - 10f) / 30f));
            return force * (1f - 0.5f * heavy);
        }
    }
}
