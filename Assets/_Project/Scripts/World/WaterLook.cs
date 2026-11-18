using UnityEngine;

namespace Evolve.World
{
    /// <summary>Pure maths and colours for how the lake looks (the water shader and the Editor look tool share them).</summary>
    public static class WaterLook
    {
        public static readonly Color AmbientSky = new Color(0.10f, 0.17f, 0.15f, 1f);
        public static readonly Color AmbientEquator = new Color(0.07f, 0.12f, 0.11f, 1f);
        public static readonly Color AmbientGround = new Color(0.04f, 0.06f, 0.05f, 1f);
        public static readonly Color FogColor = new Color(0.05f, 0.09f, 0.09f, 1f);

        // Post-processing values (kept in safe ranges by WaterLookTests: the cave must stay dark but readable, no blown highlights).
        public const float BloomThreshold = 0.9f, BloomIntensity = 0.5f, BloomScatter = 0.6f;
        public const float VignetteIntensity = 0.2f, VignetteSmoothness = 0.4f;

        const float FogMin = 0.002f, FogMax = 0.03f;

        /// <summary>0 at the surface, 1 at <paramref name="fadeDistance"/> metres deep (smoothstep).</summary>
        public static float DepthFade(float waterDepth, float fadeDistance)
        {
            if (fadeDistance <= 0f) return waterDepth > 0f ? 1f : 0f;
            float t = Mathf.Clamp01(waterDepth / fadeDistance);
            return t * t * (3f - 2f * t);
        }

        /// <summary>Colour (alpha included) between shallow and deep water.</summary>
        public static Color DepthColor(Color shallow, Color deep, float waterDepth, float deepDistance)
            => Color.Lerp(shallow, deep, DepthFade(waterDepth, deepDistance));

        /// <summary>1 where the water meets the ground, falling to 0 at <paramref name="foamWidth"/> metres of depth.</summary>
        public static float FoamAmount(float waterDepth, float foamWidth)
        {
            if (foamWidth <= 0f) return 0f;
            return 1f - Mathf.Clamp01(waterDepth / foamWidth);
        }

        /// <summary>Exponential-squared fog density that leaves roughly this visibility, kept within a sane range.</summary>
        public static float FogDensityFor(float visibilityMetres)
            => Mathf.Clamp(2.5f / Mathf.Max(1f, visibilityMetres), FogMin, FogMax);
    }
}
