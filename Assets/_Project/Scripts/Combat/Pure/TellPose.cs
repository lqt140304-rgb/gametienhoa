using System;

namespace Evolve.Combat
{
    public enum PoseKind { Crouch, HornDown, Withdraw, Hover, Spin, Roar }

    /// <summary>A body pose as offsets from the creature's normal shape: scale per axis, nose pitch, a small shift, and a shake strength.</summary>
    public struct Pose
    {
        public float ScaleX, ScaleY, ScaleZ, PitchDeg, OffsetY, OffsetZ, Shake;
        public static Pose Identity => new Pose { ScaleX = 1f, ScaleY = 1f, ScaleZ = 1f };

        public static Pose Lerp(Pose a, Pose b, float t) => new Pose
        {
            ScaleX = a.ScaleX + (b.ScaleX - a.ScaleX) * t, ScaleY = a.ScaleY + (b.ScaleY - a.ScaleY) * t, ScaleZ = a.ScaleZ + (b.ScaleZ - a.ScaleZ) * t,
            PitchDeg = a.PitchDeg + (b.PitchDeg - a.PitchDeg) * t, OffsetY = a.OffsetY + (b.OffsetY - a.OffsetY) * t, OffsetZ = a.OffsetZ + (b.OffsetZ - a.OffsetZ) * t,
            Shake = a.Shake + (b.Shake - a.Shake) * t,
        };
    }

    /// <summary>
    /// The body language of an attack (spec 2026-11-20: the tell must show in the body's shape): the pose grows through the windup to a clear
    /// peak, holds or releases while striking, and eases back to normal while recovering. Exaggerated on purpose so it reads from far away.
    /// Used by CreatureCombat when the species has no animation clips of its own.
    /// </summary>
    public static class TellPose
    {
        static Pose Peak(PoseKind k)
        {
            var p = Pose.Identity;
            switch (k)
            {
                case PoseKind.Crouch: p.ScaleX = 1.1f; p.ScaleY = 0.62f; p.ScaleZ = 1.22f; p.PitchDeg = 14f; p.OffsetZ = -0.2f; break;      // coiled low, backed off
                case PoseKind.HornDown: p.ScaleY = 0.9f; p.ScaleZ = 1.1f; p.PitchDeg = 22f; p.OffsetZ = -0.25f; p.Shake = 0.6f; break;     // head and horn down, pawing
                case PoseKind.Withdraw: p.ScaleX = 1.15f; p.ScaleY = 0.7f; p.ScaleZ = 1.15f; break;                                        // tucked into the shell
                case PoseKind.Hover: p.ScaleX = 0.95f; p.ScaleY = 1.15f; p.ScaleZ = 0.95f; p.PitchDeg = -10f; p.OffsetY = 0.5f; p.Shake = 0.3f; break;   // up and quivering
                case PoseKind.Spin: p.ScaleX = 1.1f; p.ScaleY = 0.85f; p.ScaleZ = 1.1f; p.Shake = 1f; break;
                case PoseKind.Roar: p.ScaleY = 1.2f; p.ScaleZ = 1.1f; p.PitchDeg = -15f; p.Shake = 0.5f; break;                             // rears up
            }
            return p;
        }

        static Pose Strike(PoseKind k)
        {
            if (k != PoseKind.Crouch) return Peak(k);
            var p = Pose.Identity; p.ScaleX = 0.9f; p.ScaleY = 0.9f; p.ScaleZ = 1.3f; p.PitchDeg = -8f; return p;   // the coil springs open, stretched out
        }

        static float Smooth(float t) { t = Math.Min(1f, Math.Max(0f, t)); return t * t * (3f - 2f * t); }

        public static Pose Evaluate(PoseKind kind, MovePhase phase, float progress01)
        {
            switch (phase)
            {
                case MovePhase.Windup: return Pose.Lerp(Pose.Identity, Peak(kind), Smooth(progress01));
                case MovePhase.Active: return Strike(kind);
                case MovePhase.Recover: return Pose.Lerp(Strike(kind), Pose.Identity, Smooth(progress01));
                default: return Pose.Identity;
            }
        }
    }
}
