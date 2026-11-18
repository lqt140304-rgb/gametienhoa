using System;

namespace Evolve.Traits
{
    /// <summary>
    /// The four flaws of the first slice as pure functions. <c>strength</c> is how much of the flaw is still felt after organs
    /// compensate it (PlayerTraits.FlawStrength): 0 = no such flaw, 1 = felt in full, never below 0.3 for a flaw the player has.
    /// </summary>
    public static class FlawRules
    {
        public const string DoiSom = "flaw.doi_som", RunRay = "flaw.run_ray", LuongCuong = "flaw.luong_cuong", DaDayKen = "flaw.da_day_ken";

        /// <summary>Đói Sớm: hunger burns faster by 10% (scaled by strength).</summary>
        public static float HungerBurnMultiplier(float strength) => 1f + TraitTuning.HungerBurnExtra * Clamp01(strength);

        /// <summary>Run Rẩy: seconds the body is frozen after being hit.</summary>
        public static float HitFreezeSeconds(float strength) => TraitTuning.RunRayFreeze * Clamp01(strength);

        /// <summary>
        /// Luống Cuống: below 25% health the body changes direction more slowly. 1 above the threshold; at strength 1 it drops to
        /// 0.5 at no health; a weaker (compensated) flaw eases it back toward 1.
        /// </summary>
        public static float TurnRateMultiplier(float healthFraction, float strength)
        {
            float s = Clamp01(strength);
            if (s <= 0f || healthFraction >= TraitTuning.LuongCuongHealth) return 1f;
            float depth = Clamp01(1f - Math.Max(0f, healthFraction) / TraitTuning.LuongCuongHealth);   // 0 at the threshold .. 1 at no health
            float slowest = 1f - (1f - TraitTuning.LuongCuongMinTurn) * s;
            return 1f - (1f - slowest) * depth;
        }

        /// <summary>
        /// Dạ Dày Kén: for 20 seconds after eating a food for the first time, everything eaten is absorbed 30% less.
        /// <paramref name="secondsSinceNewFood"/> is the time since a never-eaten food was last eaten (use a huge number or a
        /// negative one for none).
        /// </summary>
        public static float AbsorbMultiplier(float secondsSinceNewFood, float strength)
        {
            if (secondsSinceNewFood < 0f || secondsSinceNewFood >= TraitTuning.DaDayKenWindow) return 1f;
            return 1f - (1f - TraitTuning.DaDayKenAbsorb) * Clamp01(strength);
        }

        static float Clamp01(float v) => Math.Min(1f, Math.Max(0f, float.IsNaN(v) ? 0f : v));
    }
}
