using Evolve.Organs;

namespace Evolve.Swallow
{
    /// <summary>
    /// Giải Mã odds (spec 5.3): every swallowed sample is one lucky roll plus one step of pity.
    /// A full pity bar always teaches the gene. Numbers are proposals to tune in play tests.
    /// </summary>
    public static class AnalysisRules
    {
        /// <summary>Sample quality: swallowed alive or just killed, old carcass, rotten carcass.</summary>
        public const float Fresh = 1f, Old = 0.5f, Rotten = 0.2f;
        /// <summary>Pity counts as full from here (25 × 0.04 must reach it despite float error).</summary>
        public const float PityFull = 0.9999f;

        public static float Chance(GeneRarity r) => r switch
        {
            GeneRarity.Rare => 0.08f,
            GeneRarity.Epic => 0.02f,
            GeneRarity.Legendary => 0.10f,
            _ => 0.25f
        };

        public static float PityGain(GeneRarity r) => r switch
        {
            GeneRarity.Rare => 0.07f,
            GeneRarity.Epic => 0.04f,
            GeneRarity.Legendary => 0.34f,
            _ => 0.15f
        };

        /// <summary>
        /// One sample of one gene. Quality scales both the chance and the pity gained.
        /// Returns true when the gene is learned; pity then resets to 0.
        /// </summary>
        public static bool Roll(GeneRarity r, ref float pity, float quality, float chanceMultiplier, float roll01)
        {
            if (quality <= 0f) return false;
            bool hit = roll01 < Chance(r) * quality * chanceMultiplier;
            if (!hit)
            {
                pity += PityGain(r) * quality;
                hit = pity >= PityFull;
            }
            if (hit) pity = 0f;
            return hit;
        }

        /// <summary>Stars of a learned gene from the samples swallowed after learning it: 3 → ★2, 9 → ★3.</summary>
        public static int Stars(int samplesAfterLearn) => samplesAfterLearn >= 9 ? 3 : samplesAfterLearn >= 3 ? 2 : 1;

        public static GeneRarity FromTier(OrganTier t) => t switch
        {
            OrganTier.B => GeneRarity.Rare,
            OrganTier.A => GeneRarity.Epic,
            OrganTier.S => GeneRarity.Legendary,
            _ => GeneRarity.Common
        };

        /// <summary>Giải Mã grows with use: level 2 after 10 analyses, level 3 after 30.</summary>
        public static int DecoderLevel(int analysesDone) => analysesDone >= 30 ? 3 : analysesDone >= 10 ? 2 : 1;

        public static float DecoderChanceMultiplier(int level) => 1f + 0.1f * (level - 1);

        /// <summary>Level 1 shows only the pity bar and "???"; level 2 the gene's name; level 3 its chance.</summary>
        public static bool ShowsName(int level) => level >= 2;
        public static bool ShowsChance(int level) => level >= 3;
    }
}
