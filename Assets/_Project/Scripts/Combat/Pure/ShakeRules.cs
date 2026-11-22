using System;

namespace Evolve.Combat
{
    /// <summary>The camera thump of a heavy blow or footfall: how hard it shakes at a distance, how fast it dies away, how two add up, how big a footstep shakes by body size.</summary>
    public static class ShakeRules
    {
        public const float MaxAmplitude = 0.6f;

        /// <summary>1 at the source, 0 at <paramref name="radius"/> and beyond, a straight line between.</summary>
        public static float Falloff(float distance, float radius) => radius <= 0f ? 0f : Math.Max(0f, 1f - distance / radius);

        /// <summary>A sharp thump: the amplitude falls with the square of the time left, so most of it is gone by the middle.</summary>
        public static float Amplitude(float baseAmplitude, float elapsed, float duration)
        {
            if (duration <= 0f || elapsed >= duration) return 0f;
            float left = 1f - Math.Max(0f, elapsed) / duration;
            return baseAmplitude * left * left * left;
        }

        public static float Combine(float a, float b) => Math.Min(MaxAmplitude, a + b);

        /// <summary>A footstep's shake grows with the body scale, up to a cap (a boss of scale 6 and one of scale 20 shake the same).</summary>
        public static float StepAmplitude(float bodyScale) => 0.012f * Math.Min(6f, Math.Max(0.5f, bodyScale));
    }

    /// <summary>When the feet touch the ground in an animation cycle: which contact phases (0..1 through the cycle) lie between two frames.</summary>
    public static class FootstepRules
    {
        /// <summary>How many of the <paramref name="phases"/> were crossed going from cycle time <paramref name="from"/> to <paramref name="to"/> (normalised time, may pass 1 and wrap).</summary>
        public static int Contacts(float from, float to, float[] phases)
        {
            if (to <= from || phases == null) return 0;
            int n = 0;
            int lo = (int)Math.Floor(from), hi = (int)Math.Floor(to);
            for (int cycle = lo; cycle <= hi; cycle++)
                foreach (float ph in phases)
                {
                    float t = cycle + ph;
                    if (t > from && t <= to) n++;
                }
            return n;
        }
    }
}
