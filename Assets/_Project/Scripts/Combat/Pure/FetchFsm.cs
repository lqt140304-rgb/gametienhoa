using System;

namespace Evolve.Combat
{
    public enum FetchStage { Walk, Pull, Done }
    public enum FetchEvent { None, PullStart, Done }

    /// <summary>
    /// Going to get something (the golem's axe): it walks to it (until it is there, or <see cref="WalkTimeout"/> if the way is blocked or the player is gone: it then pulls where it stands),
    /// then pulls it out for <c>pullSeconds</c>. Pure; the golem's pull itself is shaped by <see cref="PullRules"/>.
    /// </summary>
    public sealed class FetchFsm
    {
        public const float WalkTimeout = 4f;

        readonly float pullSeconds;
        public FetchFsm(float pullSeconds) { this.pullSeconds = Math.Max(0.01f, pullSeconds); }

        public FetchStage Stage { get; private set; }
        public float StageTime { get; private set; }

        /// <summary>0 before the pull, then the share of it done, 1 once finished.</summary>
        public float PullProgress01 => Stage == FetchStage.Done ? 1f : Stage == FetchStage.Pull ? Math.Min(1f, StageTime / pullSeconds) : 0f;

        public FetchEvent Update(float dt, bool arrived)
        {
            if (Stage == FetchStage.Done) return FetchEvent.None;
            StageTime += Math.Max(0f, dt);
            if (Stage == FetchStage.Walk)
            {
                if (!arrived && StageTime < WalkTimeout) return FetchEvent.None;
                Stage = FetchStage.Pull; StageTime = 0f; return FetchEvent.PullStart;
            }
            if (StageTime < pullSeconds) return FetchEvent.None;
            Stage = FetchStage.Done; return FetchEvent.Done;
        }

        /// <summary>Knocked out of it (stunned, killed): the fetch ends at once.</summary>
        public void Abort() { Stage = FetchStage.Done; }
    }
}
