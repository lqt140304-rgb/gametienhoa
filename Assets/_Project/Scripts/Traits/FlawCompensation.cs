using System;
using System.Collections.Generic;
using Evolve.Organs;

namespace Evolve.Traits
{
    /// <summary>How much of a flaw the worn organs offset: each organ in a matching slot gives a share scaled by its integrity, never more than 70% in total.</summary>
    public static class FlawCompensation
    {
        /// <summary>Share compensated, in [0, MaxCompensation]. An empty or null <paramref name="compensatedBy"/> (flaw not compensable yet) gives 0.</summary>
        public static float Fraction(IEnumerable<(OrganSlot slot, float integrity)> worn, OrganSlot[] compensatedBy)
        {
            if (worn == null || compensatedBy == null || compensatedBy.Length == 0) return 0f;
            float total = 0f;
            foreach (var (slot, integrity) in worn)
            {
                if (Array.IndexOf(compensatedBy, slot) < 0) continue;
                float clean = float.IsNaN(integrity) ? 0f : Math.Min(1f, Math.Max(0f, integrity));
                total += TraitTuning.PerOrganCompensation * clean;
            }
            return Math.Min(TraitTuning.MaxCompensation, Math.Max(0f, total));
        }

        /// <summary>What is left of the flaw after compensation: 1 = fully felt, never below 0.3.</summary>
        public static float Strength(float fraction) => 1f - Math.Min(TraitTuning.MaxCompensation, Math.Max(0f, fraction));
    }
}
