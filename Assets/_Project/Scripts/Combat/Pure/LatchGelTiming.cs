using System;

namespace Evolve.Combat
{
    public static class LatchGelTiming
    {
        public const float CloseSeconds = 0.22f;
        public const float HoldSeconds = 0.5f;
        public const float AbsorbSeconds = 2f;

        public static float Coverage(float gripProgress) => 0.18f + 0.64f * Clamp(gripProgress);

        public static float Dissolve(float elapsed) =>
            Clamp((elapsed - CloseSeconds - HoldSeconds) / 0.82f);

        public static float Clamp(float value) => Math.Max(0f, Math.Min(1f, value));
    }
}
