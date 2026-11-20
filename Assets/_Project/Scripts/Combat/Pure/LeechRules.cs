using System;

namespace Evolve.Combat
{
    public enum LeechState { Idle, Hunt, Drain, Scared }

    public struct LeechInput
    {
        public float PlayerDistance, PlayerSpeed;
        public bool Hopped, SkillUsed, Hit, PlayerAlive;
    }

    /// <summary>
    /// The mosquito's life with a player (spec 2026-11-20): it hunts, lands and drains blood until it is shaken off (a hop, a skill, a hit,
    /// or running fast for half a second), then flies away scared for 30 seconds and comes back only if the player is still near.
    /// Pure and driven by the scene each step.
    /// </summary>
    public sealed class LeechRules
    {
        public const float DetectRange = 14f, AttachRange = 1.2f, DetachRange = 2.5f, ReturnRange = 15f, ScaredSeconds = 30f,
            ShakeSpeed = 3f, ShakeSeconds = 0.5f, DrainPerSecond = 1.5f, AttachWindup = 0.4f;
        const float LostRange = DetectRange * 1.5f, Eps = 1e-4f;

        float fastTime;

        public LeechState State { get; private set; }
        public float StateTime { get; private set; }

        void Go(LeechState s) { State = s; StateTime = 0f; fastTime = 0f; }

        /// <summary>A swarm mate noticed the player: an idle mosquito joins the hunt.</summary>
        public void Alert() { if (State == LeechState.Idle) Go(LeechState.Hunt); }

        /// <summary>Advances time; returns the blood drained during this step.</summary>
        public float Update(float dt, LeechInput i)
        {
            StateTime += dt;
            switch (State)
            {
                case LeechState.Idle:
                    if (i.PlayerAlive && i.PlayerDistance <= DetectRange) Go(LeechState.Hunt);
                    return 0f;
                case LeechState.Hunt:
                    if (!i.PlayerAlive || i.PlayerDistance > LostRange) { Go(LeechState.Idle); return 0f; }
                    if (i.PlayerDistance > AttachRange) { StateTime = 0f; return 0f; }
                    if (StateTime + Eps >= AttachWindup) Go(LeechState.Drain);
                    return 0f;
                case LeechState.Drain:
                    if (!i.PlayerAlive || i.PlayerDistance > DetachRange) { Go(LeechState.Hunt); return 0f; }
                    fastTime = i.PlayerSpeed >= ShakeSpeed ? fastTime + dt : 0f;
                    if (i.Hopped || i.SkillUsed || i.Hit || fastTime + Eps >= ShakeSeconds) { Go(LeechState.Scared); return 0f; }
                    return DrainPerSecond * dt;
                default:   // Scared
                    if (StateTime + Eps >= ScaredSeconds) Go(i.PlayerAlive && i.PlayerDistance <= ReturnRange ? LeechState.Hunt : LeechState.Idle);
                    return 0f;
            }
        }
    }
}
