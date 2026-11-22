using System;

namespace Evolve.Combat
{
    public enum PlayerStyle { Unknown, Hugger, Runner, Camper, Balanced }

    /// <summary>
    /// The boss reads the player: it watches, over the last several seconds, how close the player stays and how fast they move, and which way they sidestep its blows.
    /// A player glued to it (<see cref="PlayerStyle.Hugger"/>) is met with close attacks, one who stays far (<see cref="PlayerStyle.Camper"/>) with ranged and special moves,
    /// one who runs about at mid range (<see cref="PlayerStyle.Runner"/>) with the charge. Three sidesteps the same way in a row can be predicted (<see cref="PredictedLateral"/>), and
    /// projectiles are aimed ahead of a moving player (<see cref="Lead"/>). Pure and testable.
    /// </summary>
    public sealed class PlayerReader
    {
        public const float WatchSeconds = 3f, DistTau = 5f, SpeedTau = 4f, HuggerBelow = 5f, CamperAbove = 12f, RunnerSpeed = 4f, SidestepMeters = 1f;
        const int Memory = 4;

        float distEma, speedEma, watched;
        readonly float[] dodges = new float[Memory];
        int dodgeCount;

        public void Observe(float dt, float distance, float playerSpeed)
        {
            if (dt <= 0f) return;
            if (watched <= 0f) { distEma = distance; speedEma = playerSpeed; }
            float kd = 1f - (float)Math.Exp(-dt / DistTau), ks = 1f - (float)Math.Exp(-dt / SpeedTau);
            distEma += (distance - distEma) * kd; speedEma += (playerSpeed - speedEma) * ks;
            watched += dt;
        }

        public PlayerStyle Style
        {
            get
            {
                if (watched < WatchSeconds) return PlayerStyle.Unknown;
                if (distEma <= HuggerBelow) return PlayerStyle.Hugger;
                if (distEma >= CamperAbove) return PlayerStyle.Camper;
                return speedEma >= RunnerSpeed ? PlayerStyle.Runner : PlayerStyle.Balanced;
            }
        }

        /// <summary>How much more (or less) likely this move is to be picked, given how the player behaves.</summary>
        public float Weight(MoveDef m)
        {
            switch (Style)
            {
                case PlayerStyle.Hugger: return m.Role == MoveRole.Normal ? 1.8f : m.Role == MoveRole.Ranged ? 0.4f : m.Role == MoveRole.Special ? 0.5f : m.Role == MoveRole.Combo ? 0.8f : 1f;
                case PlayerStyle.Camper: return m.Role == MoveRole.Ranged ? 1.8f : m.Role == MoveRole.Special ? 1.6f : m.Role == MoveRole.Normal ? 0.2f : 1f;
                case PlayerStyle.Runner: return m.Role == MoveRole.Combo ? 2f : m.Role == MoveRole.Ranged ? 0.7f : 1f;
                default: return 1f;
            }
        }

        /// <summary>The player sidestepped a blow: <paramref name="lateralMeters"/> sideways (+ right, - left) as seen from the boss. Standing still is not a dodge.</summary>
        public void NoteDodge(float lateralMeters)
        {
            if (Math.Abs(lateralMeters) < SidestepMeters) return;
            for (int i = Memory - 1; i > 0; i--) dodges[i] = dodges[i - 1];
            dodges[0] = Math.Sign(lateralMeters);
            dodgeCount = Math.Min(Memory, dodgeCount + 1);
        }

        /// <summary>-1..1: the side the player tends to sidestep to (+ right), or 0 unless the last three or more all went the same way.</summary>
        public float PredictedLateral()
        {
            if (dodgeCount < 3) return 0f;
            float s = dodges[0];
            for (int i = 1; i < 3; i++) if (dodges[i] != s) return 0f;
            int same = 0; for (int i = 0; i < dodgeCount; i++) if (dodges[i] == s) same++;
            return s * (same / (float)dodgeCount);
        }

        /// <summary>Where to aim, as an offset (x, z) from the player's position: ahead of their velocity (<paramref name="vx"/>, <paramref name="vz"/>) over the flight <paramref name="seconds"/>, more for a runner.</summary>
        public (float x, float z) Lead(float vx, float vz, float seconds)
        {
            float k = Style == PlayerStyle.Runner ? 0.9f : 0.4f;
            return (vx * seconds * k, vz * seconds * k);
        }
    }
}
