using Evolve.Survival;
using UnityEngine;

namespace Evolve.Traits
{
    /// <summary>
    /// What predators need to know about a player's core and flaws (Ngụy Sắc, Vệt Mùi, Dễ Bị Chú Ý). Runs where the predators run (the host);
    /// a remote player's PlayerTraits is filled by TraitsSync, and its stillness and trail are tracked from the avatar's own movement.
    /// </summary>
    public static class TraitSense
    {
        static PlayerTraits Of(SurvivalStats p) => p != null ? p.GetComponent<PlayerTraits>() : null;

        /// <summary>Multiplier on a predator's detection range for this player (Nhân Ngụy Sắc).</summary>
        public static float DetectMultiplier(SurvivalStats p)
        {
            var t = Of(p);
            if (t == null) return 1f;
            float f = EnvTraitRules.NguySacDetect(t.HasCore(EnvTraitRules.NguySac), t.StillSeconds);
            return f < 1f ? EncounterRewardRules.BoostReduction(f, t.CoreBoost) : f;
        }

        /// <summary>Vệt Mùi: is a fresh part of this player's trail within reach of a predator standing at <paramref name="at"/>?</summary>
        public static bool SmellsTrail(SurvivalStats p, Vector3 at)
        {
            var t = Of(p);
            if (t == null || !t.HasFlaw(EnvTraitRules.VetMui)) return false;
            float radius = EnvTraitRules.ScentRadius * t.FlawStrength(EnvTraitRules.VetMui);
            return radius > 0f && t.Trail.Near(at.x, at.z, Time.time, radius);
        }

        /// <summary>Dễ Bị Chú Ý: the nearest living player who draws a patrolling predator at <paramref name="at"/>, or null.</summary>
        public static SurvivalStats Lure(Vector3 at)
        {
            SurvivalStats best = null;
            float bestD = float.MaxValue;
            foreach (var p in SurvivalStats.All)
            {
                if (p == null || p.State.IsDead) continue;
                var t = Of(p);
                if (t == null || !t.HasFlaw(EnvTraitRules.DeBiChuY)) continue;
                float reach = EnvTraitRules.LureDistance(t.FlawStrength(EnvTraitRules.DeBiChuY));
                float d = Vector3.Distance(at, p.transform.position);
                if (d <= reach && d < bestD) { best = p; bestD = d; }
            }
            return best;
        }
    }
}
