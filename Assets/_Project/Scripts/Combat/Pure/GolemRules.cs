using System;

namespace Evolve.Combat
{
    /// <summary>Arm and body angles (degrees) of the golem's body language: both arms raised (+) or driven down (-), the right arm cocked back (+) or thrown through (-), the arms spread wide, the spine bent forward (+) or leaned back (-), and how bright the chest core glows (0..1).</summary>
    public struct ArmPose
    {
        public float Raise, Back, Spread, Hunch, Glow;
        /// <summary>Whole-body values: Crouch 0..1 (knees bent, body low), Tuck 0..1 (both legs pulled up, in the air), Stance -1..1 (+ = the left foot planted forward, - = the right), Twist degrees of spine turn (- = wound back to the right, + = through).</summary>
        public float Crouch, Tuck, Stance, Twist;
        /// <summary>0..1: down on one knee (the left foot forward, the right knee on the ground).</summary>
        public float Kneel;
        public static ArmPose Lerp(ArmPose a, ArmPose b, float t) => new ArmPose
        {
            Raise = a.Raise + (b.Raise - a.Raise) * t, Back = a.Back + (b.Back - a.Back) * t, Spread = a.Spread + (b.Spread - a.Spread) * t,
            Hunch = a.Hunch + (b.Hunch - a.Hunch) * t, Glow = a.Glow + (b.Glow - a.Glow) * t,
            Crouch = a.Crouch + (b.Crouch - a.Crouch) * t, Tuck = a.Tuck + (b.Tuck - a.Tuck) * t, Stance = a.Stance + (b.Stance - a.Stance) * t, Twist = a.Twist + (b.Twist - a.Twist) * t, Kneel = a.Kneel + (b.Kneel - a.Kneel) * t,
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
                    if (anim == "Hurt") return ArmPose.Lerp(rest, windup, Smooth(progress01 / 0.3f));
                    return w;
                case MovePhase.Active:
                    if (anim == "Meteor") { var d = ArmPose.Lerp(windup, strike, Smooth(progress01)); d.Tuck = 1f - Smooth(progress01 / 0.5f); d.Crouch = progress01 > 0.8f ? strike.Crouch * Smooth((progress01 - 0.8f) / 0.2f) : 0f; return d; }   // legs reach down for the ground, then the landing is absorbed
                    return ArmPose.Lerp(windup, strike, anim == "Slam" || anim == "Throw" || anim == "Punch" || anim == "Sweep" ? Smooth(progress01 / 0.5f) : Smooth(progress01));
                case MovePhase.Recover: return ArmPose.Lerp(anim == "Hurt" ? windup : strike, rest, Smooth(progress01));
                default: return rest;
            }
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
                case VolleyStage.Pull:   // a struggle: bent over the column while it holds, then leaning back into each heave, shaking all the while
                {
                    float k = Smooth(seconds / 0.4f), t01 = seconds / VolleyFsm.PullSeconds, heave = PullRules.Heave(t01), strain = PullRules.Strain(t01);
                    return new ArmPose { Raise = 55f * k, Hunch = (32f - 50f * heave) * k, Crouch = 0.35f * k, Glow = (0.25f + 0.4f * heave) * k, Twist = 8f * strain * (float)Math.Sin(seconds * 35f) * k };
                }
                case VolleyStage.Roar: { float k = Smooth(seconds / 0.5f) * (1f - Smooth((seconds - (VolleyFsm.RoarSeconds - 0.4f)) / 0.4f)); return new ArmPose { Raise = 45f * k, Spread = 55f * k, Hunch = -24f * k, Glow = k, Stance = 0.2f * k }; }   // head thrown back, arms wide, the core blazing
                case VolleyStage.Hold: { float k = Smooth(seconds / 0.5f); return new ArmPose { Raise = 110f * k, Back = -40f * k, Hunch = -12f * k, Stance = 0.3f * k, Twist = -15f * k, Glow = 0.3f * k }; }   // the column carried over the head: the right arm high (110 + 40), the left up beside it
                case VolleyStage.Release: { float k = Smooth(seconds / 0.15f) * (1f - Smooth((seconds - 0.5f) / 0.3f)); return new ArmPose { Raise = 70f * k, Back = -60f * k, Hunch = 30f * k, Twist = 30f * k, Stance = 0.8f * k, Crouch = 0.15f * k }; }   // the follow-through: leaning through the throw, the left arm out, the right folded by the head
                default: return default;
            }
        }

        /// <summary>
        /// After a move: it sinks to one knee in a second, a hand planted on the ground and the other resting on its knee, bent over with the chest heaving, and for the last 1.5 s
        /// pushes itself up with both hands on its knees, like a person getting up; upright again at <paramref name="length"/> seconds.
        /// </summary>
        public static ArmPose Winded(float seconds, float length)
        {
            float into = Smooth(seconds / 1f), outOf = Smooth((seconds - (length - 1.5f)) / 1.5f), push = Smooth(outOf * 2.5f);
            float breath = (float)Math.Sin(seconds * 5.5f);
            float down = into * (1f - outOf);   // how far down it is: all the way during the breathing, less and less as it pushes up
            return new ArmPose
            {
                Kneel = down, Hunch = (52f + 5f * breath) * down,
                Raise = (30f + 10f * push) * into, Back = 30f * (1f - push) * into,   // the left hand on the knee; the right arm hangs to the floor (Raise - Back = 0), then both push (Back 0)
                Glow = 0.12f * down,
            };
        }
    }
}
