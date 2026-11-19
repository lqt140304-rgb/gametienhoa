using UnityEngine;

namespace Evolve.Visuals
{
    /// <summary>The brightness setting (0 to 1) turned into post-processing values (pure, tested): exposure in stops and a lift of the shadows and midtones, so a dark cave can be raised
    /// without washing the highlights out.</summary>
    public static class BrightnessMath
    {
        public const float Default = 0.35f;
        public const float MinExposure = -0.5f, MaxExposure = 3.0f;
        public const float MaxGammaLift = 0.45f;

        public static float Clamp(float b) => Mathf.Clamp01(b);
        public static float PostExposure(float b) => Mathf.Lerp(MinExposure, MaxExposure, Clamp(b));
        /// <summary>Added to the midtones' luminance in Lift/Gamma/Gain (positive lightens): the dark parts rise the most.</summary>
        public static float GammaLift(float b) => Mathf.Lerp(0f, MaxGammaLift, Clamp(b));
    }
}
