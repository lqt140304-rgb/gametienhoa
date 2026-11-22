using System;
using Evolve.Creatures;

namespace Evolve.Combat
{
    public enum VfxTint { Dust, Ember, Wind, Bone }

    /// <summary>What a move shows in its first and last stage (its impact is drawn by the species' own FX): the charge during the tell and the aftermath during the recover.</summary>
    public struct VfxPlan
    {
        public VfxTint Tint;
        /// <summary>Puffs per second at full charge, rings of warning marked on the ground over the tell, particles orbiting the body (the big moves only).</summary>
        public float ChargePuffsPerSecond; public int ChargeRings, OrbitCount;
        /// <summary>How long the traces of the hit last, how many ember bursts they have, and whether a mark is left on the ground.</summary>
        public float AfterSeconds; public int AfterEmbers; public bool GroundMark;
    }

    /// <summary>
    /// Every move is shown in three stages: <b>charge</b> (the tell: dust and sparks gather and grow with it, rings on the ground), <b>impact</b> (the burst: drawn by GolemFx, BirdFx, HoundFx)
    /// and <b>aftermath</b> (embers and dust settling, a mark on the ground). The bigger the move, the bigger every stage. Pure, so every move can be checked to have all three.
    /// </summary>
    public static class VfxRules
    {
        public static VfxPlan PlanFor(string species, MoveDef m)
        {
            string key = SpeciesNames.Key(species ?? "");
            var p = new VfxPlan { Tint = key == "golemthachanh" ? VfxTint.Ember : key == "chimhangme" ? VfxTint.Wind : VfxTint.Bone };
            switch (m.Role)
            {
                case MoveRole.Ultimate: p.ChargePuffsPerSecond = 16f; p.ChargeRings = 4; p.OrbitCount = 30; p.AfterSeconds = 5f; p.AfterEmbers = 8; p.GroundMark = true; break;
                case MoveRole.Special: p.ChargePuffsPerSecond = 11f; p.ChargeRings = 3; p.OrbitCount = 18; p.AfterSeconds = 4f; p.AfterEmbers = 6; p.GroundMark = true; break;
                case MoveRole.Combo: p.ChargePuffsPerSecond = 9f; p.ChargeRings = 2; p.OrbitCount = 8; p.AfterSeconds = 3f; p.AfterEmbers = 4; p.GroundMark = true; break;
                case MoveRole.Ranged: p.ChargePuffsPerSecond = 8f; p.ChargeRings = 2; p.OrbitCount = 6; p.AfterSeconds = 2.5f; p.AfterEmbers = 3; break;
                default: p.ChargePuffsPerSecond = 5f; p.ChargeRings = 1; p.OrbitCount = 0; p.AfterSeconds = 2f; p.AfterEmbers = 2; p.GroundMark = m.Radius > 0f; break;
            }
            return p;
        }

        /// <summary>0..1 over the tell: it gathers slowly at first and then fast (the square), so the last moment before the hit is the most intense.</summary>
        public static float ChargeIntensity(float progress01) { float t = Math.Min(1f, Math.Max(0f, progress01)); return t * t; }

        /// <summary>1 at the moment of impact, fading to 0 over <paramref name="length"/> seconds.</summary>
        public static float AfterIntensity(float seconds, float length) => length <= 0f ? 0f : Math.Max(0f, 1f - seconds / length);
    }
}
