using UnityEngine;

namespace Evolve.World
{
    /// <summary>
    /// The vibration scan as a sound wave (pure maths, mirrored in Shaders/EvolveSight.hlsl): a ring that spreads out from where the slime stood when it pressed C. Things light up only while
    /// the ring is passing through them, then fall dark again; nothing is lit by a lamp. The ring thins out and fades once it has reached its full radius.
    /// </summary>
    public static class SonarWaveMath
    {
        /// <summary>The blind slime's own faint light never goes above this, so lamps no longer blow out the things close to it.</summary>
        public const float MaxBlindLight = 1.8f;

        /// <summary>Radius (m) of the wave front <paramref name="t"/> seconds after the press.</summary>
        public static float Radius(float t, float expand, float maxRadius) => Mathf.Lerp(0f, maxRadius, Mathf.Clamp01(t / Mathf.Max(0.01f, expand)));

        /// <summary>1 while the wave spreads, then falling to 0 over hold + fade seconds.</summary>
        public static float Fade(float t, float expand, float hold, float fade)
        {
            if (t < expand) return 1f;
            return Mathf.Clamp01(1f - (t - expand) / Mathf.Max(0.01f, hold + fade));
        }

        /// <summary>Half width (m) of the lit ring: thin near the slime, wider far away.</summary>
        public static float BandWidth(float radius) => Mathf.Max(1.2f, 0.1f * radius);

        /// <summary>0..1: how much a point at <paramref name="distance"/> from the origin is touched by the front (a smooth bump on the ring).</summary>
        public static float Ring(float distance, float radius, float width)
        {
            float x = (distance - radius) / Mathf.Max(0.01f, width);
            return Mathf.Exp(-x * x);
        }
    }
}
