using System;
using System.Collections.Generic;
using UnityEngine;

namespace Evolve.Organs
{
    /// <summary>
    /// What a creature can drop when caught or killed, and how likely.
    /// Two-step roll: first pick a tier by its weight (only tiers that have organs in this table),
    /// then pick an organ inside that tier by its DropWeight. So the tier weights are the real
    /// tier odds, no matter how many organs each tier has.
    /// Organs the catcher already wears are not removed, just less likely (OwnedWeightMultiplier),
    /// unless every organ of that tier is already owned.
    /// Use the ⋮ menu → "Print drop rates" to see the resulting percentages.
    /// </summary>
    [CreateAssetMenu(menuName = "Evolve/Drop Table", fileName = "DropTableSO")]
    public class DropTableSO : ScriptableObject
    {
        [Tooltip("Chance that anything drops at all")]
        [Range(0, 1)] public float DropChance = 0.6f;

        [Header("Tier weights (relative)")]
        [Min(0)] public float WeightD = 0f;
        [Min(0)] public float WeightC = 70f;
        [Min(0)] public float WeightB = 22f;
        [Min(0)] public float WeightA = 7f;
        [Min(0)] public float WeightS = 1f;

        [Header("Organs already owned")]
        [Tooltip("Weight kept by an organ the catcher already wears (1 = no change, 0.8 = 20% less likely)")]
        [Range(0, 1)] public float OwnedWeightMultiplier = 0.8f;

        public List<OrganSO> Organs = new List<OrganSO>();

        static readonly OrganTier[] AllTiers = { OrganTier.D, OrganTier.C, OrganTier.B, OrganTier.A, OrganTier.S };

        public float TierWeight(OrganTier t) => t switch
        {
            OrganTier.D => WeightD,
            OrganTier.B => WeightB,
            OrganTier.A => WeightA,
            OrganTier.S => WeightS,
            _ => WeightC
        };

        /// <summary>
        /// Chance of each organ given that something drops (sums to 1).
        /// <paramref name="isOwned"/> tells whether the catcher already wears an organ (null = owns nothing).
        /// </summary>
        public List<KeyValuePair<OrganSO, float>> Distribution(Func<OrganSO, bool> isOwned = null)
        {
            // Group candidate organs by tier and work out their weights inside the tier.
            var groups = new List<KeyValuePair<float, List<KeyValuePair<OrganSO, float>>>>();
            float tierTotal = 0f;
            foreach (var tier in AllTiers)
            {
                float tw = TierWeight(tier);
                if (tw <= 0f) continue;

                bool anyFree = false;
                foreach (var o in Organs)
                    if (o != null && o.Tier == tier && o.DropWeight > 0f && !(isOwned != null && isOwned(o))) anyFree = true;

                var list = new List<KeyValuePair<OrganSO, float>>();
                float sum = 0f;
                foreach (var o in Organs)
                {
                    if (o == null || o.Tier != tier || o.DropWeight <= 0f) continue;
                    bool owned = isOwned != null && isOwned(o);
                    float w = o.DropWeight * (owned && anyFree ? OwnedWeightMultiplier : 1f);
                    if (w <= 0f) continue;
                    list.Add(new KeyValuePair<OrganSO, float>(o, w));
                    sum += w;
                }
                if (sum <= 0f) continue;

                // Normalise inside the tier so the tier's total share is just its tier weight.
                for (int i = 0; i < list.Count; i++)
                    list[i] = new KeyValuePair<OrganSO, float>(list[i].Key, list[i].Value / sum);
                groups.Add(new KeyValuePair<float, List<KeyValuePair<OrganSO, float>>>(tw, list));
                tierTotal += tw;
            }

            var result = new List<KeyValuePair<OrganSO, float>>();
            if (tierTotal <= 0f) return result;
            foreach (var g in groups)
                foreach (var kv in g.Value)
                    result.Add(new KeyValuePair<OrganSO, float>(kv.Key, g.Key / tierTotal * kv.Value));
            return result;
        }

        /// <summary>Roll a drop. <paramref name="isOwned"/> marks organs the catcher already wears (they stay possible, just less likely).</summary>
        public OrganSO Roll(Func<OrganSO, bool> isOwned = null, float dropChanceMultiplier = 1f)
        {
            if (UnityEngine.Random.value > Evolve.Traits.LuckRules.AdjustedDropChance(DropChance, dropChanceMultiplier)) return null;

            var dist = Distribution(isOwned);
            if (dist.Count == 0) return null;

            float r = UnityEngine.Random.value;
            foreach (var kv in dist)
            {
                r -= kv.Value;
                if (r <= 0f) return kv.Key;
            }
            return dist[dist.Count - 1].Key;
        }

        [ContextMenu("Print drop rates")]
        void PrintRates()
        {
            var sb = new System.Text.StringBuilder($"{name}: rơi đồ {DropChance:P0}\n");
            sb.AppendLine("  (khi chưa có cơ quan nào)");
            foreach (var kv in Distribution())
                sb.AppendLine($"  [{kv.Key.Tier}] {kv.Key.DisplayName}: {DropChance * kv.Value:P1} mỗi lần bắt");
            sb.Append($"  Không rơi gì: {1f - DropChance:P0}");
            Debug.Log(sb.ToString());
        }
    }
}
