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
        /// <summary>0..1: the legs stretched out straight behind the body, level with it when the body lies flat (a jet's exhaust): hip back, knee and ankle straightened. Pitch 85 lays the body flat.</summary>
        public float Stretch;

        public static BirdPose Lerp(BirdPose a, BirdPose b, float t) => new BirdPose
        {
            Spread = a.Spread + (b.Spread - a.Spread) * t, Beat = a.Beat + (b.Beat - a.Beat) * t, Raise = a.Raise + (b.Raise - a.Raise) * t,
            Legs = a.Legs + (b.Legs - a.Legs) * t, Pitch = a.Pitch + (b.Pitch - a.Pitch) * t, Beak = a.Beak + (b.Beak - a.Beak) * t, Stretch = a.Stretch + (b.Stretch - a.Stretch) * t,
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
        static readonly float[] FoldedSweep = { 10f, 25f, 20f };       // sweep back with the wing folded (Spread 0) ...
        static readonly float[] FoldedLift = { -55f, 0f, 0f };         // ... and the collar drops the whole wing along the flank, like an ordinary resting bird (checked in Blender from the front, side and three-quarter view; the old all-back fold stood the wing up behind the back)
        public const float Lag = 0.55f, FoldLag = 0.25f;

        /// <summary>Beats per second: a steady 0.8 Hz when it is not beating (no visible motion then: the amplitude is 0) and 1.6 Hz at full power. The speed it walks at does not change it: an irregular wingbeat looked like random flapping.</summary>
        public static float Frequency(float beat, float moving) => 0.8f + 0.8f * beat;
        /// <summary>The shoulder's swing in degrees.</summary>
        public static float Amplitude(float beat, float moving) => 26f * beat;
        /// <summary>The down stroke goes 1.4 times as far as the up stroke (more and the wings cross in front of the body: checked on the Dreadwing in Blender).</summary>
        public const float DownFactor = 1.4f;
        public static float Stroke(float sine) => sine > 0f ? sine : sine * DownFactor;

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
            lift = pose.Raise * Hold[i] + (1f - pose.Spread) * FoldedLift[i] + s * Amplitude(pose.Beat, moving) * Share[i] - UpBend[i] * fold;
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
    /// throws them up into the sky (<see cref="TalonEvent.Tossed"/>), then flies fast runs through them like a jet (wings wide, legs straight back) and rakes with its talons, <see cref="RakeCount"/> times, one pass every
    /// <see cref="RakePass"/> seconds (<see cref="TalonEvent.Raked"/> in the middle of each run through the player), and finally comes down (<see cref="LeaveSeconds"/>). A dash that catches nothing ends at once (<see cref="Miss"/>).
    /// </summary>
    public sealed class TalonFsm
    {
        public const float CarrySeconds = 1.5f, TossSeconds = 0.6f, RakePass = 1f, LeaveSeconds = 1.2f;   // unhurried: the player must be able to see each part of it
        /// <summary>A pass is a fast straight run through the player (the first <see cref="LineShare"/> of it, 30 m/s) and a wide curve round to the start of the next run (the rest).</summary>
        public const float LineShare = 0.4f, RunHalfLength = 6f;
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
                        if (!scored && inPass >= RakePass * LineShare * 0.5f) { scored = true; return TalonEvent.Raked; }   // in the middle of the run, right on the player
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
        /// Cubic Hermite curve between <paramref name="p0"/> (leaving with tangent <paramref name="t0"/>) and <paramref name="p1"/> (arriving with <paramref name="t1"/>), s in 0..1: one coordinate of the wide curve a flying bird
        /// makes between the end of one run and the start of the next, so it turns round smoothly instead of jumping.
        /// </summary>
        public static float Hermite(float p0, float t0, float p1, float t1, float s)
        {
            s = Math.Min(1f, Math.Max(0f, s));
            float s2 = s * s, s3 = s2 * s;
            return (2f * s3 - 3f * s2 + 1f) * p0 + (s3 - 2f * s2 + s) * t0 + (-2f * s3 + 3f * s2) * p1 + (s3 - s2) * t1;
        }

        /// <summary>The pass under way (0-based) and how far through it (0..1) the bird is, <paramref name="rakeSeconds"/> into the raking.</summary>
        public static (int index, float u) PassAt(float rakeSeconds)
        {
            float t = Math.Max(0f, rakeSeconds) / RakePass;
            int i = (int)t;
            return (i, t - i);
        }
    }

    /// <summary>What the body does in each move and stage (the stand-in for animation clips: the rig bones are turned by code).</summary>
    public static class BirdPoseRules
    {
        static float Smooth(float t) { t = Math.Min(1f, Math.Max(0f, t)); return t * t * (3f - 2f * t); }
        static float Clamp01(float t) => Math.Min(1f, Math.Max(0f, t));
        public static readonly BirdPose Rest = new BirdPose();
        /// <summary>The flare of a landing: wings wide, a few slow beats, talons out in front, nose up.</summary>
        static BirdPose Flare => new BirdPose { Spread = 1f, Beat = 1f, Raise = 0.5f, Legs = -0.5f, Pitch = -26f, Beak = 0.4f };
        /// <summary>Flying along: wings open and beating, legs tucked, a little nose down.</summary>
        public static BirdPose Cruise(float effort01) => new BirdPose { Spread = 1f, Beat = 1f, Raise = 0.1f, Legs = 0f, Stretch = 1f, Pitch = 75f, Beak = 0f };

        /// <summary>Like a jet: body flat (pitch 85), wings wide open and still, hind legs straight back level with the body, the head up to look ahead.</summary>
        public static BirdPose JetPose => new BirdPose { Spread = 1f, Beat = 0f, Raise = 0.15f, Legs = 0f, Stretch = 1f, Pitch = 85f, Beak = 0.3f };

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
                    return new BirdPose { Spread = 1f, Beat = 0f, Raise = Smooth(p), Legs = 0.3f, Pitch = -14f * Smooth(p), Beak = Smooth(p) };
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
                    return new BirdPose { Spread = 1f, Beat = 1f, Raise = 0.8f, Legs = -0.4f * Smooth(p), Pitch = -20f * Smooth(p), Beak = 0.5f };
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
                    return new BirdPose { Spread = 0.7f, Beat = 0f, Raise = 0.5f * Smooth(p), Legs = 0f, Pitch = -16f * Smooth(p), Beak = Smooth(p) };
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
                        return new BirdPose { Spread = 1f, Beat = 0f, Raise = Lerp(1f, -0.8f, Smooth(k * 2f)), Legs = Lerp(-0.5f, 0f, k), Stretch = Smooth(k), Pitch = Lerp(-22f, 70f, Smooth(k)), Beak = 1f };
                    }
                case TalonStage.Rake:    // a jet: the body lies flat, the wings are spread wide and still, the legs stretched straight back, flying through the player at speed (no flapping)
                    return JetPose;
                case TalonStage.Leave:   // lets itself down with the wings spread, flaring at the end
                    {
                        float k = Clamp01(t / TalonFsm.LeaveSeconds);
                        return new BirdPose { Spread = 1f, Beat = 1f, Raise = 0.3f, Legs = Lerp(0.2f, -0.6f, Smooth(k)), Stretch = 1f - Smooth(k * 2f), Pitch = Lerp(60f, -26f, Smooth(k)), Beak = 0f };
                    }
                default: return Rest;
            }
        }

        static float Lerp(float a, float b, float t) => a + (b - a) * t;
    }
}
