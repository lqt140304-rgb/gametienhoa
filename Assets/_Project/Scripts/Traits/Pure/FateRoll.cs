using System;
using System.Collections.Generic;

namespace Evolve.Traits
{
    /// <summary>How rare a core is: the stronger the core, the rarer (Thường, Hiếm, Cực hiếm).</summary>
    public enum TraitRarity { Common, Rare, Mythic }

    /// <summary>One core in the pool fate draws from: its id, rarity and the three flaw options that go with it.</summary>
    public readonly struct FateCard
    {
        public readonly string Id;
        public readonly TraitRarity Rarity;
        public readonly string[][] Options;
        public FateCard(string id, TraitRarity rarity, string[][] options) { Id = id; Rarity = rarity; Options = options; }
    }

    /// <summary>What fate gave: a core and one of its flaw options.</summary>
    public readonly struct FateResult
    {
        public readonly string CoreId;
        public readonly TraitRarity Rarity;
        public readonly int OptionIndex;
        public readonly string[] FlawIds;
        public FateResult(string coreId, TraitRarity rarity, int optionIndex, string[] flawIds)
        { CoreId = coreId; Rarity = rarity; OptionIndex = optionIndex; FlawIds = flawIds; }
    }

    /// <summary>
    /// The birth draw (spec 2026-11-19-birth-scene-design 5.4): pick a rarity tier by weight (tiers with no card share their weight
    /// among the others), then a core evenly inside the tier, then one of its flaw options evenly. Same seed, same result.
    /// </summary>
    public static class FateRoll
    {
        public static float Weight(TraitRarity r) => r switch
        {
            TraitRarity.Rare => 0.25f,
            TraitRarity.Mythic => 0.10f,
            _ => 0.65f
        };

        public static FateResult? Roll(int seed, IReadOnlyList<FateCard> pool)
        {
            if (pool == null || pool.Count == 0) return null;
            var rnd = new Random(seed);
            var tiers = new[] { TraitRarity.Common, TraitRarity.Rare, TraitRarity.Mythic };
            float total = 0f;
            foreach (var t in tiers) if (HasCard(pool, t)) total += Weight(t);
            if (total <= 0f) return null;

            float pick = (float)rnd.NextDouble() * total;
            TraitRarity chosen = TraitRarity.Common;
            foreach (var t in tiers)
            {
                if (!HasCard(pool, t)) continue;
                chosen = t;
                pick -= Weight(t);
                if (pick < 0f) break;
            }

            var inTier = new List<FateCard>();
            foreach (var c in pool) if (c.Rarity == chosen) inTier.Add(c);
            var card = inTier[rnd.Next(inTier.Count)];
            int option = card.Options != null && card.Options.Length > 0 ? rnd.Next(card.Options.Length) : 0;
            var flaws = card.Options != null && option < card.Options.Length ? card.Options[option] : new string[0];
            return new FateResult(card.Id, card.Rarity, option, flaws);
        }

        static bool HasCard(IReadOnlyList<FateCard> pool, TraitRarity r)
        {
            foreach (var c in pool) if (c.Rarity == r) return true;
            return false;
        }

        /// <summary>Every player has a seed of their own: the run's shared seed mixed with the player's number.</summary>
        public static int PlayerSeed(int sessionSeed, int playerId) => unchecked(sessionSeed * 73856093 ^ (playerId + 1) * 19349663);
    }
}
