using UnityEngine;

namespace Evolve.World
{
    /// <summary>Pure numbers and maths for the "sun through the root roof" look. Values are proposals: tune them here.</summary>
    public static class DappledSunMath
    {
        public const float Elevation = 72f;       // degrees above the horizon: almost straight down
        public const float Yaw = 25f;
        public const float Intensity = 0.6f;      // dusk-dim: the roof is a thick mat of roots (1.8 was far too bright)
        public const float SwayAmplitude = 2.5f;  // degrees
        public const float SwayPeriod = 70f;      // seconds
        public const float ShadowDistance = 120f;
        public const int ShadowCascades = 4;
        public const int ShadowResolution = 2048;
        public const float ReflectionIntensity = 0.3f;

        public static readonly Color AmbientColor = new Color(0.06f, 0.08f, 0.08f, 1f);
        public static readonly Color SunColor = new Color(1f, 0.88f, 0.65f, 1f);

        /// <summary>(pitch, yaw) of the sun at a time: the base angles swayed slowly so the patches of light drift. Period 0 or less: no sway.</summary>
        public static Vector2 Angles(float elevation, float yaw, float time, float amplitude, float period)
        {
            if (period <= 0f) return new Vector2(elevation, yaw);
            float phase = 2f * Mathf.PI * time / period;
            return new Vector2(elevation + amplitude * Mathf.Sin(phase), yaw + 0.6f * amplitude * Mathf.Sin(phase * 0.73f + 1.3f));
        }

        public static Quaternion Rotation(float elevation, float yaw, float time, float amplitude, float period)
        {
            var a = Angles(elevation, yaw, time, amplitude, period);
            return Quaternion.Euler(a.x, a.y, 0f);
        }
    }
}
