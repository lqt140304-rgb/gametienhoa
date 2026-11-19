using System;
using System.Collections.Generic;

namespace Evolve.Traits
{
    /// <summary>
    /// How broken the connection is during the birth scene (spec 2026-11-20-birth-scene-system-collapse-design 5.2): 0 = stable, 1 = chaos.
    /// A base curve per step (high at the start, down to 0 at the end) plus short pulses from events (a slam, a gift appearing).
    /// It drives the noise, the shake and how broken the frames look. Always inside [0, 1].
    /// </summary>
    public static class Instability
    {
        /// <summary>A pulse fades with this time constant (seconds).</summary>
        public const float PulseDecay = 0.45f;

        static float Lerp(float a, float b, float t) => a + (b - a) * Math.Max(0f, Math.Min(1f, t));

        /// <summary>The base level at <paramref name="t01"/> (0..1) through a step. For a gift step the level also falls with <paramref name="giftIndex"/> (0..5).</summary>
        public static float BaseFor(BirthStepKind kind, int giftIndex, float t01)
        {
            switch (kind)
            {
                case BirthStepKind.Disconnect: return Lerp(0.95f, 0.90f, t01);
                case BirthStepKind.Pull: return Lerp(0.90f, 0.80f, t01);
                case BirthStepKind.Form: return Lerp(0.80f, 0.70f, t01);
                case BirthStepKind.GiftIntro: return Lerp(0.70f, 0.65f, t01);
                case BirthStepKind.Gift:
                    int g = Math.Max(0, Math.Min(5, giftIndex));
                    return Lerp(0.65f - 0.15f * g / 6f, 0.65f - 0.15f * (g + 1) / 6f, t01);
                case BirthStepKind.Fate: return 0.5f;
                case BirthStepKind.DealCore: return Lerp(0.60f, 0.55f, t01);
                case BirthStepKind.RevealCore: return Lerp(0.55f, 0.50f, t01);
                case BirthStepKind.DealFlaw: return Lerp(0.60f, 0.55f, t01);
                case BirthStepKind.RevealFlaw: return Lerp(0.55f, 0.50f, t01);
                case BirthStepKind.Slam: return Lerp(0.50f, 0.45f, t01);
                case BirthStepKind.Stabilize: return Lerp(0.45f, 0f, t01);
                default: return 0.5f;
            }
        }

        /// <summary>The extra instability a pulse of <paramref name="strength"/> gives <paramref name="age"/> seconds after it began (0 before it).</summary>
        public static float Pulse(float age, float strength) => age < 0f ? 0f : strength * (float)Math.Exp(-age / PulseDecay);

        /// <summary>Base level plus every pulse still ringing at <paramref name="now"/>, kept in [0, 1].</summary>
        public static float Value(float baseValue, IReadOnlyList<(float at, float strength)> pulses, float now)
        {
            float v = baseValue;
            if (pulses != null) foreach (var p in pulses) v += Pulse(now - p.at, p.strength);
            return Math.Max(0f, Math.Min(1f, v));
        }
    }
}
