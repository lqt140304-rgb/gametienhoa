using System;
using System.Collections.Generic;
using Evolve.Swallow;

namespace Evolve.Traits
{
    /// <summary>One learnable gene a reward can hand out (the scene maps its id back to the asset).</summary>
    public readonly struct GeneCandidate
    {
        public readonly string Id;
        public readonly GeneRarity Rarity;
        public GeneCandidate(string id, GeneRarity rarity) { Id = id; Rarity = rarity; }
    }

    /// <summary>
    /// The decisions behind the cave encounter rewards (spec 2026-11-20 section 3) as pure functions, so they can be tested without Unity:
    /// which gene, which flaw is re-rolled, how much a core is strengthened, how much heat resistance the body may gain.
    /// All numbers are provisional.
    /// </summary>
    public static class EncounterRewardRules
    {
        /// <summary>Each Lõi boost adds this much strength; at most <see cref="MaxCoreBoosts"/> boosts per round.</summary>
        public const float CoreBoostStep = 0.15f;
        public const int MaxCoreBoosts = 3;
        /// <summary>The body never gains more than this many °C of heat resistance from rewards.</summary>
        public const float MaxBonusHeat = 20f;

        /// <summary>Chance weight of a gene inside the allowed tiers: commoner ones come first, Huyền thoại is rare even when allowed.</summary>
        public static float Weight(GeneRarity r) => r switch { GeneRarity.Common => 8f, GeneRarity.Rare => 5f, GeneRarity.Epic => 2f, _ => 1f };

        /// <summary>
        /// A gene the player does not know yet and whose rarity is at least <paramref name="min"/>, picked by weight (same seed, same gene).
        /// Null when there is none left; the caller then gives something else (mass).
        /// </summary>
        public static string PickGene(IReadOnlyList<GeneCandidate> pool, ISet<string> known, GeneRarity min, int seed)
        {
            if (pool == null) return null;
            var eligible = new List<GeneCandidate>();
            float total = 0f;
            foreach (var c in pool)
            {
                if (string.IsNullOrEmpty(c.Id) || c.Rarity < min || (known != null && known.Contains(c.Id))) continue;
                eligible.Add(c); total += Weight(c.Rarity);
            }
            if (eligible.Count == 0) return null;
            double r = new Random(seed).NextDouble() * total;
            foreach (var c in eligible) { r -= Weight(c.Rarity); if (r <= 0) return c.Id; }
            return eligible[eligible.Count - 1].Id;
        }

        /// <summary>
        /// Re-draws one of the player's flaws (the one at a seeded position) from the pool: never the same flaw again, never one the player already
        /// has, and it can be worse than the old one (that is the risk of the encounter). No flaws, or nothing else to draw: unchanged.
        /// </summary>
        public static string[] RerollFlaw(IReadOnlyList<string> current, IReadOnlyList<string> pool, int seed)
        {
            var result = new List<string>(current ?? new string[0]);
            if (result.Count == 0 || pool == null) return result.ToArray();
            var rnd = new Random(seed);
            int at = rnd.Next(result.Count);
            var options = new List<string>();
            foreach (var id in pool) if (!string.IsNullOrEmpty(id) && !result.Contains(id)) options.Add(id);
            if (options.Count == 0) return result.ToArray();
            result[at] = options[rnd.Next(options.Count)];
            return result.ToArray();
        }

        /// <summary>Strength of the core after this many boosts: 1, 1.15, 1.3, 1.45.</summary>
        public static float CoreBoost(int boosts) => 1f + CoreBoostStep * Math.Max(0, Math.Min(MaxCoreBoosts, boosts));

        /// <summary>Strengthens a core's effect: the part above 1 of a multiplier (1.5 = +50%) grows with the boost; 1 stays 1.</summary>
        public static float BoostMultiplier(float multiplier, float boost) => 1f + (multiplier - 1f) * boost;

        /// <summary>Strengthens a core's reduction (0.4 = detected at 40% of the range): the part below 1 grows, never past zero.</summary>
        public static float BoostReduction(float factor, float boost) => Math.Max(0f, 1f - (1f - factor) * boost);

        /// <summary>Heat resistance gained from rewards, never above the cap and never negative.</summary>
        public static float AddHeat(float current, float add) => Math.Max(0f, Math.Min(MaxBonusHeat, current + add));
    }
}
