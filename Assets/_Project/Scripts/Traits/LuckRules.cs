using System;
using System.Collections.Generic;
using Evolve.Organs;
using UnityEngine;

namespace Evolve.Traits
{
    /// <summary>Nhân Con Bạc: drops come more often, by organ tier (D +20% ... S +4%, half against bosses), never to certainty and never a tier higher.</summary>
    public static class LuckRules
    {
        public const string CoreId = "core.con_bac";

        /// <summary>1 + the tier's bonus; against a boss only half the bonus.</summary>
        public static float TierMultiplier(OrganTier tier, bool isBoss)
        {
            float bonus = TraitTuning.LuckBonus(tier);
            return 1f + (isBoss ? bonus * TraitTuning.BossLuckFactor : bonus);
        }

        /// <summary>Average multiplier over the table's tier shares (weighted by share); 1 when the table is empty.</summary>
        public static float DropChanceMultiplier(IEnumerable<(OrganTier tier, float share)> distribution, bool isBoss)
        {
            float sum = 0f, weighted = 0f;
            if (distribution != null)
                foreach (var (tier, share) in distribution)
                {
                    if (share <= 0f) continue;
                    sum += share;
                    weighted += share * TierMultiplier(tier, isBoss);
                }
            return sum > 0f ? weighted / sum : 1f;
        }

        /// <summary>Base chance times the multiplier, kept in [0, DropChanceCap]; a base chance already above the cap is returned unchanged (luck never lowers it).</summary>
        public static float AdjustedDropChance(float baseChance, float multiplier)
        {
            if (baseChance <= 0f) return 0f;
            if (baseChance >= TraitTuning.DropChanceCap) return baseChance;
            return Math.Min(TraitTuning.DropChanceCap, baseChance * Math.Max(1f, multiplier));
        }

        /// <summary>The multiplier a catcher's luck gives against a table: 1 if the catcher has no Nhân Con Bạc.</summary>
        public static float ForCatcher(DropTableSO table, GameObject catcher, bool isBoss = false)
        {
            if (table == null || catcher == null) return 1f;
            var traits = catcher.GetComponent<PlayerTraits>();
            if (traits == null || !traits.HasCore(CoreId)) return 1f;
            var shares = new List<(OrganTier, float)>();
            foreach (var kv in table.Distribution()) shares.Add((kv.Key.Tier, kv.Value));
            return DropChanceMultiplier(shares, isBoss);
        }
    }
}
