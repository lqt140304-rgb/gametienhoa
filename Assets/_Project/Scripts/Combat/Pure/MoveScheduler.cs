using System;
using System.Collections.Generic;

namespace Evolve.Combat
{
    public enum MoveShape { Lunge, Charge, Spin, Drain, Call, IceDrop, Gust, Throw, Perch, Spears, Roar, Summon }
    public enum MovePhase { Idle, Windup, Active, Recover }

    /// <summary>One attack of a monster (data): its timing, reach, damage and when it unlocks.</summary>
    public sealed class MoveDef
    {
        public string Name, Anim, TargetSpecies;
        public MoveShape Shape;
        public float Windup, Active, Recover, Cooldown, MinRange, MaxRange, Damage, Distance, Radius, Weight = 1f;
        /// <summary>Metres it runs away from the player during Recover (hit and run); Force is the wind speed of a Gust; Freeze the seconds an ice block holds the player.</summary>
        public float RetreatDistance, Force, Freeze;
        /// <summary>Throw: seconds the missile flies. Perch: seconds spent on the ledge, and the ledge height window above the feet (Distance is how far to look).</summary>
        public float FlightSeconds, PerchSeconds, MinHeight, MaxHeight;
        /// <summary>Spears: how many circle above the tails, how fast each flies once thrown (m/s), and the pause between throws (Force is the knockback speed).</summary>
        /// <summary>Roar: seconds smaller creatures in Radius flee in fear (a smaller player is pushed back and frozen for Freeze seconds, Force is the push speed).</summary>
        public float FearSeconds;
        /// <summary>Summon: how many small copies of the monster climb out of the ground (around it within Radius) and how many seconds they stay.</summary>
        public int SummonCount;
        public float SummonSeconds;
        public int SpearCount;
        public float SpearSpeed, LaunchInterval;
        /// <summary>The move is usable while the monster's health fraction is at or below this (1 = always); lords unlock new moves as they lose health.</summary>
        public float UnlockBelow = 1f;
        /// <summary>Armoured while winding up and striking: a pounce bounces off.</summary>
        public bool Armored;
        /// <summary>A lunge that lands head first clamps on and starts the pin mini game (see PinGame) instead of knocking the player away.</summary>
        public bool Pins;
    }

    /// <summary>
    /// The rhythm of a monster's attacks: Idle → Windup (the tell, can be dodged) → Active (the strike) → Recover (open to a counter,
    /// it takes <see cref="RecoverDamageFactor"/> times the damage) → Idle. Picks a usable move by range, health and cooldown, weighted
    /// and never the same one twice in a row when another is ready. Pure and seeded.
    /// </summary>
    public sealed class MoveScheduler
    {
        public const float RecoverDamageFactor = 1.5f;
        const float Eps = 1e-4f;

        readonly IReadOnlyList<MoveDef> moves;
        readonly float[] cooldownLeft;
        readonly Random rnd;
        MoveDef last;
        float phaseLength;

        public MovePhase Phase { get; private set; }
        public MoveDef Current { get; private set; }
        public float PhaseTime { get; private set; }
        /// <summary>Multiplies the windup time (the start cavern gives the player longer to read the tell).</summary>
        public float TellScale { get; set; } = 1f;
        /// <summary>Optional filter: a move for which this returns false is never picked (e.g. a perch jump when there is no ledge).</summary>
        public Func<MoveDef, bool> Allow;

        public MoveScheduler(IReadOnlyList<MoveDef> moves, int seed)
        {
            this.moves = moves;
            cooldownLeft = new float[moves.Count];
            rnd = new Random(seed);
        }

        public float Progress01 => phaseLength <= Eps ? 1f : Math.Min(1f, PhaseTime / phaseLength);
        public bool Busy => Phase != MovePhase.Idle;
        public bool IsArmored => Current != null && Current.Armored && (Phase == MovePhase.Windup || Phase == MovePhase.Active);
        public float DamageTakenFactor => Phase == MovePhase.Recover ? RecoverDamageFactor : 1f;

        /// <summary>Starts <paramref name="move"/> now with a windup of exactly <paramref name="windupSeconds"/> (an ambusher bursting out). Only when idle.</summary>
        public bool Force(MoveDef move, float windupSeconds)
        {
            if (Phase != MovePhase.Idle || move == null) return false;
            Current = move; Phase = MovePhase.Windup; PhaseTime = 0f; phaseLength = Math.Max(0f, windupSeconds);
            return true;
        }

        public void Reset() { Phase = MovePhase.Idle; Current = null; PhaseTime = 0f; phaseLength = 0f; }

        /// <summary>Knocked out of its move (a wall, a stun): straight to Recover, lasting its normal recovery plus <paramref name="extraRecover"/>.</summary>
        public void Stun(float extraRecover)
        {
            if (Current == null) return;
            Phase = MovePhase.Recover; PhaseTime = 0f; phaseLength = Current.Recover + Math.Max(0f, extraRecover);
        }

        /// <summary>Advances time. Returns the move that has just started its strike (Active), otherwise null.</summary>
        public MoveDef Update(float dt, bool hasTarget, float targetDistance, float health01)
        {
            for (int i = 0; i < cooldownLeft.Length; i++) cooldownLeft[i] = Math.Max(0f, cooldownLeft[i] - dt);
            if (Phase == MovePhase.Idle)
            {
                if (!hasTarget) return null;
                var pick = Pick(targetDistance, health01);
                if (pick == null) return null;
                Current = pick; Phase = MovePhase.Windup; PhaseTime = 0f; phaseLength = pick.Windup * Math.Max(0.01f, TellScale);
                return null;
            }
            if (Phase == MovePhase.Windup && !hasTarget) { Reset(); return null; }

            PhaseTime += dt;
            if (PhaseTime + Eps < phaseLength) return null;
            switch (Phase)
            {
                case MovePhase.Windup:
                    Phase = MovePhase.Active; PhaseTime = 0f; phaseLength = Current.Active;
                    cooldownLeft[IndexOf(Current)] = Current.Cooldown; last = Current;
                    return Current;
                case MovePhase.Active:
                    Phase = MovePhase.Recover; PhaseTime = 0f; phaseLength = Current.Recover; return null;
                default:
                    Reset(); return null;
            }
        }

        int IndexOf(MoveDef m) { for (int i = 0; i < moves.Count; i++) if (ReferenceEquals(moves[i], m)) return i; return 0; }

        MoveDef Pick(float distance, float health01)
        {
            var ok = new List<int>();
            for (int i = 0; i < moves.Count; i++)
            {
                var m = moves[i];
                if (cooldownLeft[i] > 0f || health01 > m.UnlockBelow + Eps || distance < m.MinRange || distance > m.MaxRange || (Allow != null && !Allow(m))) continue;
                ok.Add(i);
            }
            if (ok.Count == 0) return null;
            if (ok.Count > 1 && last != null) ok.RemoveAll(i => ReferenceEquals(moves[i], last));
            float total = 0f; foreach (int i in ok) total += Math.Max(0.0001f, moves[i].Weight);
            float r = (float)rnd.NextDouble() * total;
            foreach (int i in ok) { r -= Math.Max(0.0001f, moves[i].Weight); if (r <= 0f) return moves[i]; }
            return moves[ok[ok.Count - 1]];
        }
    }
}
