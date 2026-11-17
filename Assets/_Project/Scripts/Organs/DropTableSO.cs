using System;
using System.Collections.Generic;
using UnityEngine;

namespace Evolve.Organs
{
    /// <summary>
    /// What a creature can drop when caught or killed, and how likely.
    /// Chance of a specific organ = DropChance × (tierWeight × organ.DropWeight) / sum over candidates.
    /// Use the ⋮ menu → "Print drop rates" to see the resulting percentages.
    /// </summary>
    [CreateAssetMenu(menuName = "Evolve/Drop Table", fileName = "DropTableSO")]
    public class DropTableSO : ScriptableObject
    {
        [Tooltip("Chance that anything drops at all")]
        [Range(0, 1)] public float DropChance = 0.6f;

        [Header("Tier weights (relative)")]
        [Min(0)] public float WeightC = 70f;
        [Min(0)] public float WeightB = 22f;
        [Min(0)] public float WeightA = 7f;
        [Min(0)] public float WeightS = 1f;

        public List<OrganSO> Organs = new List<OrganSO>();

        public float TierWeight(OrganTier t) => t switch
        {
            OrganTier.B => WeightB,
            OrganTier.A => WeightA,
            OrganTier.S => WeightS,
            _ => WeightC
        };

        float Weight(OrganSO o) => TierWeight(o.Tier) * Mathf.Max(0f, o.DropWeight);

        /// <summary>Roll a drop. <paramref name="allowed"/> filters out organs (e.g. already owned).</summary>
        public OrganSO Roll(Func<OrganSO, bool> allowed = null)
        {
            if (UnityEngine.Random.value > DropChance) return null;

            float total = 0f;
            var candidates = new List<OrganSO>();
            foreach (var o in Organs)
            {
                if (o == null || (allowed != null && !allowed(o)) || Weight(o) <= 0f) continue;
                candidates.Add(o);
                total += Weight(o);
            }
            if (total <= 0f) return null;

            float r = UnityEngine.Random.value * total;
            foreach (var o in candidates)
            {
                r -= Weight(o);
                if (r <= 0f) return o;
            }
            return candidates[candidates.Count - 1];
        }

        [ContextMenu("Print drop rates")]
        void PrintRates()
        {
            float total = 0f;
            foreach (var o in Organs) if (o != null) total += Weight(o);
            var sb = new System.Text.StringBuilder($"{name}: rơi đồ {DropChance:P0}\n");
            foreach (var o in Organs)
            {
                if (o == null) continue;
                float p = total > 0f ? DropChance * Weight(o) / total : 0f;
                sb.AppendLine($"  [{o.Tier}] {o.DisplayName}: {p:P1} mỗi lần bắt");
            }
            sb.Append($"  Không rơi gì: {1f - DropChance:P0}");
            Debug.Log(sb.ToString());
        }
    }
}
