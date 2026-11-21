using System;

namespace Evolve.Combat
{
    /// <summary>Arm and body angles (degrees) of the golem's body language: both arms raised (+) or driven down (-), the right arm cocked back (+) or thrown through (-), the arms spread wide, the spine bent forward (+) or leaned back (-), and how bright the chest core glows (0..1).</summary>
    public struct ArmPose
    {
        public float Raise, Back, Spread, Hunch, Glow;
        public static ArmPose Lerp(ArmPose a, ArmPose b, float t) => new ArmPose
        {
            Raise = a.Raise + (b.Raise - a.Raise) * t, Back = a.Back + (b.Back - a.Back) * t, Spread = a.Spread + (b.Spread - a.Spread) * t,
            Hunch = a.Hunch + (b.Hunch - a.Hunch) * t, Glow = a.Glow + (b.Glow - a.Glow) * t,
        };
    }

    /// <summary>
    /// What Golem Thạch Anh's arms, back and chest core do in each phase of each move (its walk and run are Meshy's clips; these poses are added on top by code):
    /// Slam (arms overhead, lean back, then both fists driven down), Throw (right arm back, then through), Harden (a hunched guard, the core glowing while
    /// the armour holds), CoreBlast (arms thrown wide, the core brightening to a flash). Pure, so the timing can be tested.
    /// </summary>
    public static class GolemRules
    {
        static float Smooth(float t) { t = Math.Min(1f, Math.Max(0f, t)); return t * t * (3f - 2f * t); }

        static void Keys(string anim, out ArmPose windup, out ArmPose strike)
        {
            switch (anim)
            {
                case "Slam": windup = new ArmPose { Raise = 150f, Hunch = -12f, Glow = 0.2f }; strike = new ArmPose { Raise = -20f, Hunch = 26f, Glow = 0.5f }; return;
                case "Throw": windup = new ArmPose { Back = 115f, Hunch = -14f }; strike = new ArmPose { Back = -55f, Hunch = 18f }; return;
                case "Harden": windup = new ArmPose { Raise = 40f, Hunch = 18f, Glow = 0.6f }; strike = windup; return;
                case "CoreBlast": windup = new ArmPose { Raise = 70f, Spread = 60f, Hunch = -8f, Glow = 1f }; strike = new ArmPose { Raise = 90f, Spread = 80f, Hunch = -12f, Glow = 1f }; return;
                default: windup = default; strike = default; return;
            }
        }

        public static ArmPose Evaluate(string anim, MovePhase phase, float progress01)
        {
            Keys(anim, out var windup, out var strike);
            var rest = default(ArmPose);
            switch (phase)
            {
                case MovePhase.Windup:
                    var w = ArmPose.Lerp(rest, windup, Smooth(progress01));
                    if (anim == "CoreBlast") w.Glow = progress01 * progress01;   // the core brightens slowly, then fast
                    return w;
                case MovePhase.Active: return ArmPose.Lerp(windup, strike, anim == "Slam" || anim == "Throw" ? Smooth(progress01 / 0.5f) : Smooth(progress01));
                case MovePhase.Recover: return ArmPose.Lerp(strike, rest, Smooth(progress01));
                default: return rest;
            }
        }
    }
}
