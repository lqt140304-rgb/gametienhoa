using System;

namespace Evolve.Combat
{
    /// <summary>
    /// How the bird holds its body (all data, no Unity): wings, legs, body pitch, beak. <see cref="BirdRigFx"/> turns the bones to it.
    /// Spread 0 = wings folded against the body .. 1 = wide open; Beat 0 = no flapping .. 1 = full-power beat; Raise -1 = wings swept down .. +1 = held high (the beat rides on it);
    /// Legs -1 = talons reaching forward .. 0 = hanging .. +1 = tucked up under the belly; Pitch in degrees, + = nose down; Beak 0 = shut .. 1 = open.
    /// </summary>
    public struct BirdPose
    {
        public float Spread, Beat, Raise, Legs, Pitch, Beak;

        public static BirdPose Lerp(BirdPose a, BirdPose b, float t) => new BirdPose
        {
            Spread = a.Spread + (b.Spread - a.Spread) * t, Beat = a.Beat + (b.Beat - a.Beat) * t, Raise = a.Raise + (b.Raise - a.Raise) * t,
            Legs = a.Legs + (b.Legs - a.Legs) * t, Pitch = a.Pitch + (b.Pitch - a.Pitch) * t, Beak = a.Beak + (b.Beak - a.Beak) * t,
        };
    }

    /// <summary>
    /// One wing is three segments (shoulder, elbow, wrist). A beat is a fast power stroke down and a slower recovery stroke up; on the way up the elbow and wrist
    /// <i>fold in</i> (the wing is drawn toward the body and bends so the tip trails) and open again at the top, so it flaps and tucks at the same time.
    /// The outer segments lag the shoulder, which makes the tip whip. Angles are in degrees: lift = up (+), sweep = toward the tail (+, i.e. folding).
    /// </summary>
    public static class WingRules
    {
        static readonly float[] Share = { 1f, 0.5f, 0.4f };            // how much of the shoulder's swing each segment adds
        static readonly float[] Hold = { 55f, 12f, 8f };               // lift at Raise = 1
        static readonly float[] UpBend = { 0f, 24f, 32f };             // the elbow and wrist droop this much at the middle of the up stroke
        static readonly float[] UpSweep = { 0f, 38f, 52f };            // ... and draw in this much
        static readonly float[] FoldedSweep = { 55f, 80f, 60f };       // sweep with the wing fully folded (Spread 0)
        public const float Lag = 0.55f, FoldLag = 0.25f;

        /// <summary>Beats per second: a slow idle flutter, quicker walking, a heavy 2.7 Hz beat at full power.</summary>
        public static float Frequency(float beat, float moving) => 0.5f + 0.9f * moving + 2.2f * beat;
        /// <summary>The shoulder's swing in degrees.</summary>
        public static float Amplitude(float beat, float moving) => 6f + 8f * moving + 40f * beat;
        /// <summary>The down stroke goes 1.7 times as far as the up stroke.</summary>
        public static float Stroke(float sine) => sine > 0f ? sine : sine * 1.7f;

        /// <summary>0..1: how far the wing is drawn in at this beat phase (radians). Zero for the whole down stroke, peaks a little after the middle of the up stroke.</summary>
        public static float UpFold(float phase)
        {
            double c = Math.Cos(phase - 0.35);
            return c > 0.0 ? (float)Math.Pow(c, 1.4) : 0f;
        }

        public static void Segment(int i, float phase, BirdPose pose, float moving, out float lift, out float sweep)
        {
            i = Math.Min(2, Math.Max(0, i));
            float s = Stroke((float)Math.Sin(phase - Lag * i));
            float fold = pose.Beat * UpFold(phase - FoldLag * i);
            lift = pose.Raise * Hold[i] + s * Amplitude(pose.Beat, moving) * Share[i] - UpBend[i] * fold;
            sweep = (1f - pose.Spread) * FoldedSweep[i] + (i == 0 ? 6f * pose.Spread : 0f) + UpSweep[i] * fold;
        }
    }

    /// <summary>
    /// Bổ Nhào: it flaps up over the player's head during the windup (a ring on the ground marks the spot late in it), then folds its wings and drops on the spot,
    /// faster and faster, talons first. The height of the climb is <see cref="ApexMeters"/> (less under a low ceiling).
    /// </summary>
    public static class SwoopRules
    {
        public const float ApexMeters = 9f, ClimbShare = 0.8f, MarkAt = 0.6f, ImpactAt = 0.94f, HoverStandOff = 6f;

        static float Smooth(float t) { t = Math.Min(1f, Math.Max(0f, t)); return t * t * (3f - 2f * t); }

        /// <summary>0..1 of the climb during the windup: it reaches the top at 80% of the windup and hangs there.</summary>
        public static float Rise(float progress01) => Smooth(progress01 / ClimbShare);

        /// <summary>1..0 during the dive: the height left. Starts slowly and speeds up (free fall: the distance goes with the square of the time).</summary>
        public static float Fall(float progress01) { float p = Math.Min(1f, Math.Max(0f, progress01)); return 1f - p * p; }
    }

    public enum TalonStage { Dash, Carry, Toss, Rake, Leave, Done }
    public enum TalonEvent { None, Tossed, Raked, Finished }

    /// <summary>
    /// Quắp: the bird dashes in low with its talons out (<see cref="TalonStage.Dash"/>). If it catches the player it climbs with them in its feet (<see cref="CarrySeconds"/>),
    /// throws them up into the sky (<see cref="TalonEvent.Tossed"/>), then flies passes through them and rakes with its talons, <see cref="RakeCount"/> times, one pass every
    /// <see cref="RakePass"/> seconds (<see cref="TalonEvent.Raked"/> in the middle of each pass), and finally comes down (<see cref="LeaveSeconds"/>). A dash that catches nothing ends at once (<see cref="Miss"/>).
    /// </summary>
    public sealed class TalonFsm
    {
        public const float CarrySeconds = 1.1f, TossSeconds = 0.4f, RakePass = 0.5f, LeaveSeconds = 0.9f;
        public const int RakeCount = 4;

        public TalonStage Stage { get; private set; } = TalonStage.Dash;
        /// <summary>Seconds in the current stage.</summary>
        public float StageTime { get; private set; }
        /// <summary>The pass under way, 0-based.</summary>
        public int RakeIndex { get; private set; }
        /// <summary>The dash found the player.</summary>
        public bool Caught { get; private set; }
        public bool Finished => Stage == TalonStage.Done;
        bool scored;

        public void Grab() { if (Stage != TalonStage.Dash) return; Caught = true; Go(TalonStage.Carry); }
        public void Miss() { if (Stage == TalonStage.Dash) Go(TalonStage.Done); }
        public void Abort() { Go(TalonStage.Done); }
        /// <summary>The player is gone (dead, or got free): skip what is left of the carry and the raking and come down.</summary>
        public void GiveUp() { if (Stage == TalonStage.Carry || Stage == TalonStage.Toss || Stage == TalonStage.Rake) Go(TalonStage.Leave); }

        void Go(TalonStage s) { Stage = s; StageTime = 0f; }

        public TalonEvent Update(float dt)
        {
            if (Stage == TalonStage.Dash || Stage == TalonStage.Done) return TalonEvent.None;
            StageTime += Math.Max(0f, dt);
            switch (Stage)
            {
                case TalonStage.Carry:
                    if (StageTime < CarrySeconds) return TalonEvent.None;
                    Go(TalonStage.Toss); return TalonEvent.Tossed;
                case TalonStage.Toss:
                    if (StageTime >= TossSeconds) { Go(TalonStage.Rake); RakeIndex = 0; scored = false; }
                    return TalonEvent.None;
                case TalonStage.Rake:
                    {
                        float inPass = StageTime - RakeIndex * RakePass;
                        if (!scored && inPass >= RakePass * 0.5f) { scored = true; return TalonEvent.Raked; }
                        if (inPass >= RakePass)
                        {
                            RakeIndex++; scored = false;
                            if (RakeIndex >= RakeCount) Go(TalonStage.Leave);
                        }
                        return TalonEvent.None;
                    }
                default:   // Leave
                    if (StageTime < LeaveSeconds) return TalonEvent.None;
                    Go(TalonStage.Done); return TalonEvent.Finished;
            }
        }

        /// <summary>
        /// Where the bird is along its line through the player, in spans, <paramref name="rakeSeconds"/> into the raking: it swings back and forth like a pendulum, one pass per <see cref="RakePass"/>.
        /// +1 at the start, 0 in the middle of a pass (straight through the player, fastest: that is where the talons score), -1 at the end of it, then back again; it is slow at both ends, where it turns,
        /// and its position never jumps from one pass to the next.
        /// </summary>
        public static float SwingOffset(float rakeSeconds) => (float)Math.Cos(Math.PI * rakeSeconds / RakePass);
    }

    /// <summary>What the body does in each move and stage (the stand-in for animation clips: the rig bones are turned by code).</summary>
    public static class BirdPoseRules
    {
        static float Smooth(float t) { t = Math.Min(1f, Math.Max(0f, t)); return t * t * (3f - 2f * t); }
        static float Clamp01(float t) => Math.Min(1f, Math.Max(0f, t));
        public static readonly BirdPose Rest = new BirdPose();
        /// <summary>The flare of a landing: wings wide, a few slow beats, talons out in front, nose up.</summary>
        static BirdPose Flare => new BirdPose { Spread = 1f, Beat = 0.5f, Raise = 0.5f, Legs = -0.5f, Pitch = -26f, Beak = 0.4f };
        /// <summary>Flying along: wings open and beating, legs tucked, a little nose down.</summary>
        public static BirdPose Cruise(float effort01) => new BirdPose { Spread = 1f, Beat = 0.45f + 0.35f * Clamp01(effort01), Raise = 0f, Legs = 0.85f, Pitch = 7f, Beak = 0f };

        /// <summary>The slash's two chops: +1 (wings up) at the start and between them, -1 (wings down and forward) in the middle of each chop.</summary>
        public static float SlashRaise(float activeProgress01) => 0.1f + 0.9f * (float)Math.Cos(4.0 * Math.PI * Clamp01(activeProgress01));

        /// <summary>The target pose of a move that has no stages of its own beyond Windup, Active and Recover.</summary>
        public static BirdPose Evaluate(string anim, MovePhase phase, float p)
        {
            switch (anim)
            {
                case "Swoop": return Swoop(phase, p);
                case "Slash": return Slash(phase, p);
                case "Talon": return TalonLead(phase, p);
                case "Peck": return Peck(phase, p);
                default: return Rest;
            }
        }

        static BirdPose Swoop(MovePhase phase, float p)
        {
            switch (phase)
            {
                case MovePhase.Windup:   // climbing: flapping hard, legs drawn up, nose up
                    return new BirdPose { Spread = 1f, Beat = 1f, Raise = 0.25f, Legs = 0.8f * Smooth(p), Pitch = -18f + 12f * Smooth(p), Beak = 0f };
                case MovePhase.Active:   // the stoop: wings swept back, head down, talons thrust forward at the end
                    return new BirdPose { Spread = 0.12f, Beat = 0f, Raise = -0.3f, Legs = Lerp(0.6f, -1f, Smooth(p * 1.4f)), Pitch = 62f, Beak = 1f };
                case MovePhase.Recover:
                    return BirdPose.Lerp(Flare, Rest, Smooth(p));
                default: return Rest;
            }
        }

        static BirdPose Slash(MovePhase phase, float p)
        {
            switch (phase)
            {
                case MovePhase.Windup:   // wings drawn up and back, the head lifts
                    return new BirdPose { Spread = 1f, Beat = 0.35f, Raise = Smooth(p), Legs = 0.3f, Pitch = -14f * Smooth(p), Beak = Smooth(p) };
                case MovePhase.Active:
                    {
                        float r = SlashRaise(p);
                        return new BirdPose { Spread = 1f, Beat = 0f, Raise = r, Legs = 0.3f, Pitch = -4f + 12f * (0.5f - 0.5f * r), Beak = 1f };
                    }
                case MovePhase.Recover:
                    return BirdPose.Lerp(new BirdPose { Spread = 1f, Beat = 0f, Raise = 0.2f, Legs = 0.3f, Pitch = -4f, Beak = 0.6f }, Rest, Smooth(p));
                default: return Rest;
            }
        }

        static BirdPose TalonLead(MovePhase phase, float p)
        {
            switch (phase)
            {
                case MovePhase.Windup:   // hovering low, wings high, talons opening, aiming
                    return new BirdPose { Spread = 1f, Beat = Lerp(0.4f, 0.9f, Smooth(p)), Raise = 0.8f, Legs = -0.4f * Smooth(p), Pitch = -20f * Smooth(p), Beak = 0.5f };
                case MovePhase.Active:   // the dash: wings swept back, talons out in front, head down
                    return new BirdPose { Spread = 0.3f, Beat = 0f, Raise = -0.2f, Legs = -1f, Pitch = 32f, Beak = 1f };
                case MovePhase.Recover:  // missed: a flare as it lands
                    return BirdPose.Lerp(Flare, Rest, Smooth(p));
                default: return Rest;
            }
        }

        static BirdPose Peck(MovePhase phase, float p)
        {
            switch (phase)
            {
                case MovePhase.Windup:   // rears back, wings flared
                    return new BirdPose { Spread = 0.7f, Beat = 0.2f, Raise = 0.5f * Smooth(p), Legs = 0f, Pitch = -16f * Smooth(p), Beak = Smooth(p) };
                case MovePhase.Active:
                    return new BirdPose { Spread = 0.7f, Beat = 0f, Raise = 0.3f, Legs = 0f, Pitch = 38f, Beak = 1f };
                case MovePhase.Recover:
                    return BirdPose.Lerp(new BirdPose { Spread = 0.7f, Beat = 0f, Raise = 0.3f, Legs = 0f, Pitch = 38f, Beak = 0f }, Rest, Smooth(p));
                default: return Rest;
            }
        }

        /// <summary>The pose in each stage of the grab, <paramref name="t"/> seconds into the stage (<see cref="TalonStage.Dash"/> is a phase of the move: use <see cref="Evaluate"/>).</summary>
        public static BirdPose Talon(TalonStage stage, float t)
        {
            switch (stage)
            {
                case TalonStage.Carry:   // climbing hard with the player in its feet
                    return new BirdPose { Spread = 1f, Beat = 1f, Raise = 0.1f, Legs = -0.5f, Pitch = -4f - 12f * Smooth(t / TalonFsm.CarrySeconds), Beak = 0f };
                case TalonStage.Toss:    // wings up, then slammed down as it lets go
                    {
                        float k = Clamp01(t / TalonFsm.TossSeconds);
                        return new BirdPose { Spread = 1f, Beat = 0.5f, Raise = Lerp(1f, -0.8f, Smooth(k * 2f)), Legs = Lerp(-0.5f, 0.3f, k), Pitch = Lerp(-22f, -5f, k), Beak = 1f };
                    }
                case TalonStage.Rake:    // a pass through the player: talons thrust out in the middle of it
                    {
                        float u = (t / TalonFsm.RakePass) % 1f;
                        return new BirdPose { Spread = 0.85f, Beat = 0.9f, Raise = 0f, Legs = -0.3f - 0.7f * (float)Math.Sin(Math.PI * u), Pitch = 26f, Beak = 1f };
                    }
                case TalonStage.Leave:   // lets itself down with the wings spread, flaring at the end
                    {
                        float k = Clamp01(t / TalonFsm.LeaveSeconds);
                        return new BirdPose { Spread = 1f, Beat = Lerp(0.9f, 0.5f, k), Raise = 0.3f, Legs = Lerp(0.4f, -0.6f, Smooth(k)), Pitch = Lerp(-6f, -26f, k), Beak = 0f };
                    }
                default: return Rest;
            }
        }

        static float Lerp(float a, float b, float t) => a + (b - a) * t;
    }
}
