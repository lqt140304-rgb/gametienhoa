using System;
using Evolve.Creatures;

namespace Evolve.Combat
{
    /// <summary>
    /// A boss's passive (its "nội tại": always on, no button): a damage multiplier, a speed multiplier and a slow regeneration, each depending on its health.
    /// Golem Thạch Anh: Thân Tinh Thể (takes 20% less damage, 35% less once the crystals have grown out below 66% health, mends 1% of its health a second after 8 quiet seconds).
    /// Chó Xương: Khát Máu (runs 25% faster below half health). Other creatures have none.
    /// </summary>
    public static class PassiveRules
    {
        public const float GolemCrystalsBelow = 0.66f, GolemQuietSeconds = 8f, HoundBloodBelow = 0.5f;

        static string Key(string species) => SpeciesNames.Key(species ?? "");
        public static bool Has(string species) { string k = Key(species); return k == "golemthachanh" || k == "choxuong"; }

        public static float DamageFactor(string species, float health01) =>
            Key(species) == "golemthachanh" ? (health01 <= GolemCrystalsBelow + 1e-4f ? 0.65f : 0.8f) : 1f;

        public static bool CrystalsOut(string species, float health01) => Key(species) == "golemthachanh" && health01 <= GolemCrystalsBelow + 1e-4f;

        public static float SpeedFactor(string species, float health01) =>
            Key(species) == "choxuong" && health01 <= HoundBloodBelow + 1e-4f ? 1.25f : 1f;

        /// <summary>Fraction of max health regained per second after <paramref name="quietSeconds"/> without being hit.</summary>
        public static float RegenPerSecond(string species, float quietSeconds) =>
            Key(species) == "golemthachanh" && quietSeconds >= GolemQuietSeconds ? 0.01f : 0f;
    }

    /// <summary>
    /// A heavy biped's walk, written as code (the Meshy clips did not move the golem's legs): everything follows one phase that advances with the distance walked, so the
    /// feet never slide. Phase 0..1 is one full cycle of two steps; the left foot lands (the hip is farthest forward) at 0.25, the right at 0.75; the knee bends while the leg swings forward.
    /// </summary>
    public static class GaitRules
    {
        public static float PhaseAdvance(float distance, float stride) => stride <= 0f ? 0f : distance / stride;

        /// <summary>Hip swing in degrees: + is forward. side 0 = left, 1 = right (half a cycle apart).</summary>
        public static float Hip(float phase, int side, float amplitude) => amplitude * (float)Math.Sin(2.0 * Math.PI * (phase + 0.5 * side));

        /// <summary>Knee bend in degrees (0 = straight): only while the leg swings forward, the most at mid swing.</summary>
        public static float Knee(float phase, int side, float flex) => flex * Math.Max(0f, (float)Math.Cos(2.0 * Math.PI * (phase + 0.5 * side)));

        /// <summary>Hip roll in degrees from side to side, once per step: 0 at a landing's start, the most at a quarter cycle (the weight rides over the planted foot).</summary>
        public static float Sway(float phase, float amplitude) => amplitude * (float)Math.Sin(2.0 * Math.PI * phase);

        /// <summary>0 at a walk (≤ 3 m/s), 1 at a run (≥ 9 m/s): how much the gait stretches into a run (longer swing, deeper knees, lean forward).</summary>
        public static float RunBlend(float speed) => Math.Min(1f, Math.Max(0f, (speed - 3f) / 6f));

        /// <summary>The body's drop in metres at each footfall (twice a cycle): 0 at mid swing, the lowest at a landing.</summary>
        public static float Bob(float phase, float depth) => -depth * 0.5f * (1f - (float)Math.Cos(4.0 * Math.PI * phase));
    }
}
