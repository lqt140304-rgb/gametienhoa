using System;

namespace Evolve.Combat
{
    /// <summary>
    /// The leap of Golem's Giáng Thạch: during the windup it crouches (a quarter of it), then rises to <see cref="ApexMeters"/> and hangs there while the ring on the ground warns where it will land;
    /// in the strike it drops fast (the height shrinks with the square of the time left) and flies over to the marked spot; then it is on the ground for the rock volley.
    /// </summary>
    public static class LeapRules
    {
        public const float ApexMeters = 14f;

        static float Smooth(float t) { t = Math.Min(1f, Math.Max(0f, t)); return t * t * (3f - 2f * t); }

        /// <summary>Metres above the ground at <paramref name="progress01"/> of the phase.</summary>
        public static float Height(MovePhase phase, float progress01)
        {
            switch (phase)
            {
                case MovePhase.Windup: return ApexMeters * Smooth((progress01 - 0.25f) / 0.5f);   // 0 until a quarter, apex by three quarters, then it hangs
                case MovePhase.Active: { float left = 1f - Math.Min(1f, Math.Max(0f, progress01)); return ApexMeters * left * left; }
                default: return 0f;
            }
        }

        /// <summary>How far along the way from where it jumped to the marked spot (0..1): it only travels during the fall.</summary>
        public static float Travel(MovePhase phase, float progress01) => phase == MovePhase.Active ? Smooth(progress01) : phase == MovePhase.Recover ? 1f : 0f;
    }

    /// <summary>Where the stone spikes rise: evenly round the landing spot.</summary>
    public static class SpikeRules
    {
        public static (float x, float z)[] Ring(float centerX, float centerZ, int count, float radius, float startAngleDeg)
        {
            var r = new (float, float)[Math.Max(0, count)];
            for (int i = 0; i < r.Length; i++)
            {
                double a = (startAngleDeg + 360.0 * i / r.Length) * Math.PI / 180.0;
                r[i] = (centerX + radius * (float)Math.Cos(a), centerZ + radius * (float)Math.Sin(a));
            }
            return r;
        }
    }

    public enum VolleyStage { Dash, Hold, Done }
    public enum VolleyEvent { None, Picked, Threw }

    /// <summary>
    /// The rock volley after the landing: for each spike it dashes to it (until it is there, or <see cref="DashTimeout"/> if something is in the way), pulls it out of the
    /// ground (<see cref="VolleyEvent.Picked"/>), holds it <see cref="HoldSeconds"/> to aim, throws it (<see cref="VolleyEvent.Threw"/>) and goes to the next one, until none is left.
    /// </summary>
    public sealed class VolleyFsm
    {
        public const float DashTimeout = 1.4f, HoldSeconds = 0.5f;

        readonly int spikes;
        public VolleyFsm(int spikeCount) { spikes = Math.Max(0, spikeCount); Stage = spikes == 0 ? VolleyStage.Done : VolleyStage.Dash; }

        public VolleyStage Stage { get; private set; }
        /// <summary>The spike being fetched, or the one in hand.</summary>
        public int Index { get; private set; }
        /// <summary>Seconds in the current stage.</summary>
        public float StageTime { get; private set; }

        public VolleyEvent Update(float dt, bool arrived)
        {
            if (Stage == VolleyStage.Done) return VolleyEvent.None;
            StageTime += Math.Max(0f, dt);
            if (Stage == VolleyStage.Dash)
            {
                if (!arrived && StageTime < DashTimeout) return VolleyEvent.None;
                Stage = VolleyStage.Hold; StageTime = 0f;
                return VolleyEvent.Picked;
            }
            if (StageTime < HoldSeconds) return VolleyEvent.None;
            Index++; StageTime = 0f;
            Stage = Index >= spikes ? VolleyStage.Done : VolleyStage.Dash;
            return VolleyEvent.Threw;
        }
    }
}
