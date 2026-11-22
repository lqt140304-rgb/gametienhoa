using System;

namespace Evolve.Combat
{
    /// <summary>
    /// Bọ Cạp Thạch Anh: what its rig, its animator and its shell share (spec 2026-11-22-bo-cap-thach-anh-moveset-design). Pure numbers, no engine.
    /// The walk clips are baked by tools/blender/rig_scorpion.py: <see cref="ClipSpeed"/> must say the same as its STRIDE / (CYCLE / 2 / FPS).
    /// </summary>
    public static class ScorpionRules
    {
        /// <summary>Metres per second the baked walk clips cover at animator speed 1 (a 0.5 m step every half of a 30 frame cycle at 30 fps).</summary>
        public const float ClipSpeed = 1f;

        /// <summary>Every walk and move clip has this many keys (30 at 30 fps span <see cref="ClipSeconds"/>); Idle, Hit, Death, Exposed and Stuck keep their own length.</summary>
        public const int ClipKeys = 30;
        public const float ClipSeconds = (ClipKeys - 1) / 30f;

        /// <summary>Animator speed while walking at <paramref name="speed"/> m/s, so the feet keep up with the ground (0.6 to 3).</summary>
        public static float WalkAnimSpeed(float speed) => Clamp(speed / ClipSpeed, 0.6f, 3f);

        /// <summary>Animator speed that stretches a clip of <paramref name="clipSeconds"/> over a phase of <paramref name="phaseSeconds"/> (0.2 to 4; a phase with no length plays at 1).</summary>
        public static float PhaseAnimSpeed(float clipSeconds, float phaseSeconds) => phaseSeconds <= 0f ? 1f : Clamp(clipSeconds / phaseSeconds, 0.2f, 4f);

        /// <summary>Turns a world velocity on the ground (x, z) into the animator's MoveX (right +) and MoveY (forward +) for a body facing <paramref name="yawDegrees"/> (0 = +Z).</summary>
        public static void LocalMove(float yawDegrees, float vx, float vz, out float x, out float y)
        {
            double a = yawDegrees * Math.PI / 180.0, s = Math.Sin(a), c = Math.Cos(a);
            y = (float)(vx * s + vz * c);
            x = (float)(vx * c - vz * s);
        }

        /// <summary>Signed angle in degrees (-180 to 180) from the way the scorpion faces to the way an attacker lies, both as yaw angles.</summary>
        public static float ShellAngle(float facingYaw, float towardAttackerYaw)
        {
            float d = (towardAttackerYaw - facingYaw) % 360f;
            if (d > 180f) d -= 360f; else if (d <= -180f) d += 360f;
            return d;
        }

        /// <summary>Phóng Gai Tinh Thể: the sideways angle of spike <paramref name="index"/> of <paramref name="count"/> spread evenly over <paramref name="spreadDegrees"/> around the aim.</summary>
        public static float FanAngle(int index, int count, float spreadDegrees) =>
            count <= 1 ? 0f : (index - (count - 1) * 0.5f) * (spreadDegrees / (count - 1));

        /// <summary>Phóng Gai Tinh Thể: a spike that lands plants a crystal column that stands this many seconds (it blocks later spikes and the claw charge).</summary>
        public const float ColumnLife = 5f;

        /// <summary>Lao Kẹp into a wall or a column: the claws jam for this long (it is never stunned, but it lies open).</summary>
        public const float StuckSeconds = 1.2f;

        /// <summary>Seconds a spike takes to fly <paramref name="distance"/> metres at <paramref name="speed"/> (when its column appears).</summary>
        public static float ColumnDelay(float distance, float speed) => distance <= 0f || speed <= 0f ? 0f : distance / speed;

        static float Clamp(float v, float lo, float hi) => v < lo ? lo : v > hi ? hi : v;
    }

    public enum BurrowStage { Dig, Travel, Marked, Emerge, Done }

    /// <summary>
    /// Đào Chui Trồi Lên, the special skill (a dodge puzzle): it digs in (the move's windup), runs under the ground as a mound that follows the player,
    /// then marks a red circle on the spot and stops following (the player has <see cref="Marked"/> seconds to run out of it), and bursts out of it.
    /// </summary>
    public static class BurrowRules
    {
        public const float Dig = 1.2f, Travel = 1.0f, Marked = 1.0f, Emerge = 0.35f, MoundSpeed = 7f;

        public static float MarkTime => Dig + Travel;
        public static float EmergeTime => Dig + Travel + Marked;
        public static float Total => Dig + Travel + Marked + Emerge;

        public static BurrowStage StageAt(float t) =>
            t < Dig ? BurrowStage.Dig : t < MarkTime ? BurrowStage.Travel : t < EmergeTime ? BurrowStage.Marked : t < Total ? BurrowStage.Emerge : BurrowStage.Done;

        /// <summary>How far along its way to the spot the mound has run <paramref name="t"/> seconds into the move (it never goes past the spot).</summary>
        public static float MoundDistance(float t, float gap) => Math.Min(Math.Max(0f, gap), Math.Max(0f, MoundSpeed * (t - Dig)));

        /// <summary>The circle still slides after the player until the mark time; after that it stays put.</summary>
        public static bool CircleFollows(float t) => t < MarkTime;

        public static bool Hits(float distanceFromSpot, float radius) => distanceFromSpot <= radius;

        /// <summary>The damage multiplier while it lies open after a burst that found nobody (a counter window).</summary>
        public static float ExposedFactor(bool missed) => missed ? 1.5f : 1f;

        // ---- the second dive (health at two thirds or less): straight after coming up it goes down again and comes up where the player ran to ----
        public const float DoubleBelow = 0.66f, SecondDive = 0.8f, SecondTravel = 0.6f, SecondMarked = 0.7f, SecondEmerge = 0.35f, SecondRadius = 3f;

        public static bool WantsDouble(float health01) => health01 <= DoubleBelow + 1e-4f;
        public static float SecondMarkTime => SecondDive + SecondTravel;
        public static float SecondEmergeTime => SecondDive + SecondTravel + SecondMarked;
        public static float SecondTotal => SecondDive + SecondTravel + SecondMarked + SecondEmerge;

        public static BurrowStage SecondStageAt(float t) =>
            t < SecondDive ? BurrowStage.Dig : t < SecondMarkTime ? BurrowStage.Travel : t < SecondEmergeTime ? BurrowStage.Marked : t < SecondTotal ? BurrowStage.Emerge : BurrowStage.Done;

        public static bool CircleFollowsSecond(float t) => t < SecondMarkTime;
    }

    /// <summary>Where a boss room lets the scorpion dig (a room sets <see cref="CanBurrowAt"/>: soft ground yes, bare rock no). Anywhere by default.</summary>
    public static class ScorpionArena
    {
        public static Func<float, float, bool> CanBurrowAt = Anywhere;
        static bool Anywhere(float x, float z) => true;
        public static void Reset() => CanBurrowAt = Anywhere;
    }

    /// <summary>
    /// Bùng Pha Lê, the ultimate: three rings of crystal spikes run out from the body one after another. Each ring has a gap to slip through; the first gap is
    /// where the player stood when it began, the next two are a third of a turn round, so standing still is the mistake from the second ring on.
    /// </summary>
    public static class ShatterRules
    {
        public const int RingCount = 3;
        public const float RingInterval = 0.6f, RingSpeed = 9f, MaxRadius = 10f, GapHalfWidth = 35f, RingDamage = 13f;

        public static float RingStart(int ring) => ring * RingInterval;

        // ---- after the rings: the tail stabs three crystals that rise in front of it and flicks them at the player, one after another ----
        public const int FlickCount = 3;
        public const float FlickInterval = 0.7f, FlickThrow = 0.45f, FlickSpread = 50f;
        public static float FlickStart => RingStart(RingCount - 1) + MaxRadius / RingSpeed;   // the last ring has run out
        public static float FlickTime(int i) => FlickStart + i * FlickInterval;
        public static float FlickEnd => FlickStart + FlickCount * FlickInterval;
        /// <summary>The sideways angle (degrees from the aim) at which crystal <paramref name="i"/> rises.</summary>
        public static float FlickAngle(int i) => ScorpionRules.FanAngle(i, FlickCount, FlickSpread);

        /// <summary>How far out ring <paramref name="ring"/> has run <paramref name="t"/> seconds after the burst began (0 before it is born, then up to <see cref="MaxRadius"/>).</summary>
        public static float RingRadius(int ring, float t) => t <= RingStart(ring) ? 0f : Math.Min(MaxRadius, RingSpeed * (t - RingStart(ring)));

        /// <summary>Bearing in degrees (0 to 360) of the middle of ring <paramref name="ring"/>'s gap, given the bearing of the player when the burst began.</summary>
        public static float GapCenter(int ring, float aimBearing) => Wrap360(aimBearing + 120f * ring);

        public static bool InGap(int ring, float bearing, float aimBearing)
        {
            float d = Math.Abs(Wrap360(bearing - GapCenter(ring, aimBearing)));
            return Math.Min(d, 360f - d) <= GapHalfWidth;
        }

        /// <summary>True when ring <paramref name="ring"/> has swept over a player at <paramref name="distance"/> and <paramref name="bearing"/> (not in the gap, and not already hit by this ring).</summary>
        public static bool RingHits(int ring, float t, float distance, float bearing, float aimBearing, bool alreadyHit) =>
            !alreadyHit && distance <= MaxRadius && RingRadius(ring, t) >= distance && !InGap(ring, bearing, aimBearing);

        static float Wrap360(float a) { a %= 360f; return a < 0f ? a + 360f : a; }
    }

    /// <summary>A solid rock of a boss room (flat circle) the mound cannot dig through.</summary>
    public readonly struct ScorpionRock
    {
        public readonly float X, Z, Radius;
        public ScorpionRock(float x, float z, float radius) { X = x; Z = z; Radius = radius; }
    }

    /// <summary>The rocks of the scorpion's boss room (the Prop_Pillar boulders) stop the underground mound: hide behind one from Đào Chui.</summary>
    public static class ScorpionRoomRules
    {
        public const float RockMargin = 1.2f;

        public static bool CanDig(float x, float z, System.Collections.Generic.IReadOnlyList<ScorpionRock> rocks)
        {
            if (rocks == null) return true;
            for (int i = 0; i < rocks.Count; i++)
            {
                float dx = x - rocks[i].X, dz = z - rocks[i].Z, r = rocks[i].Radius + RockMargin;
                if (dx * dx + dz * dz <= r * r) return false;
            }
            return true;
        }
    }
}
