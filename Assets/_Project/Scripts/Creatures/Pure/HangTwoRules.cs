using System;

namespace Evolve.Creatures
{
    public static class HangTwoRules
    {
        public const float GlowSeconds = 10f, GlowCooldown = 20f;
        public const float DodgeSeconds = 0.35f, DodgeCooldown = 6f, DodgeDistance = 2f;
        public static bool Ready(float now, float lastUse, float cooldown) => now >= lastUse + cooldown;
        public static bool GlowActive(float elapsed) => elapsed >= 0f && elapsed < GlowSeconds;
        public static bool Dodging(float elapsed) => elapsed >= 0f && elapsed < DodgeSeconds;
        public static float DodgeProgress(float elapsed)
        {
            float t = Math.Max(0f, Math.Min(1f, elapsed / DodgeSeconds));
            return t * t * (3f - 2f * t);
        }
    }
}
