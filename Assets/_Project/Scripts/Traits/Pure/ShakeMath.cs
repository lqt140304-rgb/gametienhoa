using System;
using UnityEngine;

namespace Evolve.Traits
{
    /// <summary>Screen shake (spec 5.2 and 5.6): a deterministic jitter that never exceeds its amplitude, and a decay for a single hit.</summary>
    public static class ShakeMath
    {
        /// <summary>The shake offset at <paramref name="time"/> for an amplitude (pixels or units): each axis stays within ±amplitude.</summary>
        public static Vector2 Offset(float time, float amplitude)
        {
            if (amplitude <= 0f) return new Vector2(0f, 0f);
            float x = (float)(Math.Sin(time * 41.3) + 0.6 * Math.Sin(time * 67.1 + 1.7) + 0.4 * Math.Sin(time * 23.9 + 4.2)) / 2f;
            float y = (float)(Math.Sin(time * 37.7 + 2.1) + 0.6 * Math.Sin(time * 59.3 + 0.4) + 0.4 * Math.Sin(time * 29.1 + 5.3)) / 2f;
            return new Vector2(x * amplitude, y * amplitude);
        }

        /// <summary>How much of a hit's shake is left <paramref name="age"/> seconds after it: 1 at the hit, 0 after <paramref name="duration"/>, falling fast at first.</summary>
        public static float Decay(float age, float duration)
        {
            if (age <= 0f) return 1f;
            if (duration <= 0f || age >= duration) return 0f;
            float k = 1f - age / duration;
            return k * k;
        }
    }
}
