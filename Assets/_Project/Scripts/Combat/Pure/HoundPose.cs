using System;

namespace Evolve.Combat
{
    /// <summary>Bone angles (degrees) of the hound's body language: neck pitch (+ lifts the head) and jaw opening (+ opens), plus a quiver strength.</summary>
    public struct HoundPose
    {
        public float NeckPitch, JawDeg, Quiver, PawDeg;
        public static HoundPose Lerp(HoundPose a, HoundPose b, float t) =>
            new HoundPose { NeckPitch = a.NeckPitch + (b.NeckPitch - a.NeckPitch) * t, JawDeg = a.JawDeg + (b.JawDeg - a.JawDeg) * t, Quiver = a.Quiver + (b.Quiver - a.Quiver) * t, PawDeg = a.PawDeg + (b.PawDeg - a.PawDeg) * t };
    }

    /// <summary>
    /// What Chó Xương's neck and jaw do in each phase of each move (no animation clips: the rig's bones are turned by code).
    /// The pounce lowers the head and opens the jaw as it leaps, the bite opens wide then snaps shut, the roar and the howl throw the head back with
    /// the jaw open and trembling, the leap dips then lifts, the claw raises a forepaw and rakes it down. Also the flinch when it is hit. Pure, so the timing can be tested.
    /// </summary>
    public static class HoundRules
    {
        public const float MaxJawDeg = 24f, FlinchSeconds = 0.25f, HeavyFlinchSeconds = 0.6f;

        static float Smooth(float t) { t = Math.Min(1f, Math.Max(0f, t)); return t * t * (3f - 2f * t); }
        static HoundPose P(float neck, float jaw, float quiver = 0f, float paw = 0f) => new HoundPose { NeckPitch = neck, JawDeg = Math.Min(jaw, MaxJawDeg), Quiver = quiver, PawDeg = paw };

        /// <summary>The pose at the peak of a move's windup, the strike, and the end of it (it recovers from the strike to rest).</summary>
        static void Keys(string anim, out HoundPose windup, out HoundPose strike)
        {
            switch (anim)
            {
                case "Pounce": windup = P(-20f, 8f); strike = P(10f, 20f); return;
                case "Bite": windup = P(8f, 24f); strike = P(-6f, 0f); return;
                case "Roar": windup = P(35f, 24f, 0.5f); strike = P(35f, 24f, 1f); return;
                case "Howl": windup = P(50f, 20f, 0.3f); strike = P(50f, 20f, 0.6f); return;
                case "Leap": windup = P(-10f, 0f); strike = P(15f, 6f); return;
                case "Claw": windup = P(12f, 6f, 0f, 55f); strike = P(-12f, 12f, 0f, -20f); return;   // a forepaw rises, then rakes down
                default: windup = P(0f, 0f); strike = P(0f, 0f); return;
            }
        }

        public static HoundPose Evaluate(string anim, MovePhase phase, float progress01)
        {
            Keys(anim, out var windup, out var strike);
            var rest = P(0f, 0f);
            switch (phase)
            {
                case MovePhase.Windup: return HoundPose.Lerp(rest, windup, Smooth(progress01));
                case MovePhase.Active:
                    // the bite snaps shut within the first 40% of the strike; the others move from the windup peak to the strike pose
                    return anim == "Bite" ? HoundPose.Lerp(windup, strike, Smooth(progress01 / 0.4f)) : HoundPose.Lerp(windup, strike, Smooth(progress01));
                case MovePhase.Recover: return HoundPose.Lerp(strike, rest, Smooth(progress01));
                default: return rest;
            }
        }

        /// <summary>The head's kick when hit: 1 at the blow, decaying to 0 (a heavy blow lasts longer).</summary>
        public static float Flinch(float secondsSinceHit, bool heavy)
        {
            float len = heavy ? HeavyFlinchSeconds : FlinchSeconds;
            if (secondsSinceHit < 0f || secondsSinceHit >= len) return 0f;
            float x = 1f - secondsSinceHit / len;
            return x * x;
        }

        /// <summary>A hit of at least this fraction of its health staggers it.</summary>
        public const float HeavyFraction = 0.12f;
        public static bool IsHeavy(float damage, float maxHealth) => maxHealth > 0f && damage >= maxHealth * HeavyFraction;
    }
}
