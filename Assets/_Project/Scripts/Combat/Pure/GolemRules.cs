using System;

namespace Evolve.Combat
{
    /// <summary>Arm and body angles (degrees) of the golem's body language: both arms raised (+) or driven down (-), the right arm cocked back (+) or thrown through (-), the arms spread wide, the spine bent forward (+) or leaned back (-), and how bright the chest core glows (0..1).</summary>
    public struct ArmPose
    {
        public float Raise, Back, Spread, Hunch, Glow;
        /// <summary>Whole-body values: Crouch 0..1 (knees bent, body low), Tuck 0..1 (both legs pulled up, in the air), Stance -1..1 (+ = the left foot planted forward, - = the right), Twist degrees of spine turn (- = wound back to the right, + = through).</summary>
        public float Crouch, Tuck, Stance, Twist;
        public static ArmPose Lerp(ArmPose a, ArmPose b, float t) => new ArmPose
        {
            Raise = a.Raise + (b.Raise - a.Raise) * t, Back = a.Back + (b.Back - a.Back) * t, Spread = a.Spread + (b.Spread - a.Spread) * t,
            Hunch = a.Hunch + (b.Hunch - a.Hunch) * t, Glow = a.Glow + (b.Glow - a.Glow) * t,
            Crouch = a.Crouch + (b.Crouch - a.Crouch) * t, Tuck = a.Tuck + (b.Tuck - a.Tuck) * t, Stance = a.Stance + (b.Stance - a.Stance) * t, Twist = a.Twist + (b.Twist - a.Twist) * t,
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
                case "Slam": windup = new ArmPose { Raise = 150f, Hunch = -12f, Glow = 0.2f }; strike = new ArmPose { Raise = -20f, Hunch = 26f, Glow = 0.5f, Crouch = 0.55f }; return;
                case "Throw": windup = new ArmPose { Back = 115f, Hunch = -14f, Stance = 0.6f, Twist = -35f, Crouch = 0.15f }; strike = new ArmPose { Back = -55f, Hunch = 18f, Stance = 0.8f, Twist = 30f, Crouch = 0.25f }; return;
                case "Punch": windup = new ArmPose { Back = 125f, Hunch = -14f, Glow = 0.15f, Twist = -32f, Stance = 0.4f, Crouch = 0.25f }; strike = new ArmPose { Back = -45f, Hunch = 24f, Glow = 0.4f, Twist = 35f, Stance = 1f, Crouch = 0.35f }; return;
                case "Grab": windup = new ArmPose { Raise = 75f, Spread = 15f, Hunch = 10f, Crouch = 0.3f }; strike = new ArmPose { Raise = 60f, Hunch = 26f, Glow = 0.4f, Crouch = 0.5f }; return;
                case "Meteor": windup = new ArmPose { Raise = 150f, Hunch = -10f, Glow = 0.3f }; strike = new ArmPose { Raise = -10f, Hunch = 32f, Glow = 0.6f, Crouch = 0.9f }; return;   // arms thrown up to leap, both fists driven down on the landing
                case "Hurt": windup = new ArmPose { Hunch = -22f, Raise = 25f, Glow = 0.5f, Crouch = 0.1f }; strike = windup; return;   // thrown back by a blow
                case "Sweep": windup = new ArmPose { Spread = 70f, Raise = 20f, Hunch = 6f, Twist = 40f, Stance = 0.3f, Crouch = 0.2f }; strike = new ArmPose { Spread = 20f, Raise = 35f, Hunch = 14f, Twist = -45f, Stance = 0.5f, Crouch = 0.3f, Glow = 0.3f }; return;   // an arm drawn wide, then swept across in front of it
                case "Lob": windup = new ArmPose { Raise = 150f, Hunch = -14f, Glow = 0.2f }; strike = new ArmPose { Raise = 40f, Hunch = 42f, Crouch = 0.35f, Glow = 0.4f }; return;   // the rock overhead in both hands, then thrown down and forward as the body folds
                case "Rush": windup = new ArmPose { Raise = 150f, Hunch = 30f, Crouch = 0.45f }; strike = new ArmPose { Raise = 150f, Hunch = 42f, Crouch = 0.3f, Glow = 0.5f }; return;   // both arms wrapped over the head, the body low, head first
                case "Dizzy": windup = new ArmPose { Hunch = 28f, Raise = -8f, Crouch = 0.15f }; strike = windup; return;
                case "Harden": windup = new ArmPose { Raise = 40f, Hunch = 18f, Glow = 0.6f, Crouch = 0.55f }; strike = windup; return;
                case "CoreBlast": windup = new ArmPose { Raise = 70f, Spread = 60f, Hunch = -8f, Glow = 1f, Crouch = 0.35f }; strike = new ArmPose { Raise = 90f, Spread = 80f, Hunch = -12f, Glow = 1f }; return;
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
                    if (anim == "Meteor") return MeteorWindup(progress01);
                    if (anim == "Lob") return Lob(progress01);
                    if (anim == "Hurt") return ArmPose.Lerp(rest, windup, Smooth(progress01 / 0.3f));
                    return w;
                case MovePhase.Active:
                    if (anim == "Meteor") { var d = ArmPose.Lerp(windup, strike, Smooth(progress01)); d.Tuck = 1f - Smooth(progress01 / 0.5f); d.Crouch = progress01 > 0.8f ? strike.Crouch * Smooth((progress01 - 0.8f) / 0.2f) : 0f; return d; }   // legs reach down for the ground, then the landing is absorbed
                    return ArmPose.Lerp(windup, strike, anim == "Slam" || anim == "Throw" || anim == "Punch" || anim == "Lob" || anim == "Sweep" ? Smooth(progress01 / 0.5f) : Smooth(progress01));
                case MovePhase.Recover: return ArmPose.Lerp(anim == "Hurt" ? windup : strike, rest, Smooth(progress01));
                default: return rest;
            }
        }

        /// <summary>Ném Đá: bends down and takes a big rock from the ground (first 45%), lifts it over the head in both hands (to 85%), holds it there, then the strike folds the body forward.</summary>
        static ArmPose Lob(float p)
        {
            if (p < 0.45f) { float k = Smooth(p / 0.45f); return new ArmPose { Raise = -10f * k, Hunch = 48f * k, Crouch = 0.7f * k, Glow = 0.1f }; }   // hands down to the rock
            float u = Smooth((p - 0.45f) / 0.4f);
            return new ArmPose { Raise = -10f + 160f * u, Hunch = 48f - 62f * u, Crouch = 0.7f * (1f - u), Glow = 0.2f * u };
        }

        /// <summary>Giáng Thạch before the jump: points at the player (first 20%), then gathers and crouches deep (to 45%), then throws its arms up as it springs and tucks its legs.</summary>
        static ArmPose MeteorWindup(float p)
        {
            var w = default(ArmPose);
            if (p < 0.2f) { w.Back = -78f * Smooth(p / 0.2f); w.Hunch = 4f * Smooth(p / 0.2f); w.Glow = 0.15f; }   // the right arm stretched toward the player
            else if (p < 0.45f) { float k = Smooth((p - 0.2f) / 0.25f); w.Back = -78f * (1f - k); w.Raise = 20f * k; w.Hunch = 4f + 18f * k; w.Crouch = k; w.Glow = 0.15f + 0.15f * k; }
            else { float k = Smooth((p - 0.45f) / 0.25f); w.Raise = 20f + 130f * k; w.Hunch = 22f - 32f * k; w.Crouch = Math.Max(0f, 1f - Smooth((p - 0.45f) / 0.1f)); w.Glow = 0.3f; }
            w.Tuck = Smooth((p - 0.55f) / 0.3f);
            return w;
        }

        /// <summary>The body during the rock volley that follows the landing: getting up from the crouch, hugging the spike while pulling it out, aiming it like a javelin.</summary>
        public static ArmPose Volley(VolleyStage stage, float seconds)
        {
            switch (stage)
            {
                case VolleyStage.Rise: { float k = 1f - Smooth(seconds / VolleyFsm.RiseSeconds); return new ArmPose { Crouch = 0.9f * k, Hunch = 24f * k, Raise = -5f * k }; }
                case VolleyStage.Pull: { float k = Smooth(seconds / 0.4f); return new ArmPose { Raise = 55f * k, Hunch = 32f * k, Crouch = 0.35f * k, Glow = 0.25f * k }; }   // arms round the column, leaning back as it comes out
                case VolleyStage.Hold: { float k = Smooth(seconds / 0.3f); return new ArmPose { Raise = 55f * k, Back = 140f * k, Hunch = -6f * k, Stance = 0.5f * k, Twist = -22f * k, Glow = 0.3f * k }; }   // a javelin: one arm back, the other pointing at the target
                default: return default;
            }
        }
    }
}
