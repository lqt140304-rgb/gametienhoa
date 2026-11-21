using System;
using System.Collections.Generic;

namespace Evolve.Combat
{
    public enum MoveShape { Lunge, Charge, Spin, Drain, Call, IceDrop, Gust, Throw, Perch, Spears, Roar, Summon, Harden, Meteor }
    public enum MovePhase { Idle, Windup, Active, Recover }

    /// <summary>What a move is for in a boss's standard kit: two Normal attacks, one Ranged, one Special (it holds the player in a mini game), one Ultimate near the end of its health.</summary>
    public enum MoveRole { Normal, Ranged, Special, Ultimate, Combo }

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
        /// <summary>A pin that is a grab (button mashing, a heavier squeeze) instead of a bite (alternate A and D).</summary>
        public bool Grab;
        public MoveRole Role;
        /// <summary>The special skill is a dodge puzzle (a marked landing circle, then a rock volley) instead of a hold.</summary>
        public bool Dodge;
        /// <summary>Meteor: how many stone spikes rise round the landing spot, how far from it, and how hard the rocks pulled out of them hit (their size is MissileSize).</summary>
        public int SpikeCount;
        public float SpikeRadius, MissileDamage;
        /// <summary>Damage multiplier while it recovers (open to a counter): 1.5 normally; a move whose recovery is itself an attack (the golem's rock volley) says 1.</summary>
        /// <summary>A charge that starts slowly and speeds up (see <see cref="ChargeRules"/>) instead of running at one speed.</summary>
        public bool Ramp;
        /// <summary>A roar after a landing: the radius and the knock-back strength (metres per second) of its shock wave; 0 for none.</summary>
        public float RoarRadius, RoarForce;
        /// <summary>Seconds it stays crouched, winded, after the recover (2-5 for a boss, the bigger the move the longer); added to the end of the recover.</summary>
        public float Rest;
        /// <summary>Names of moves it strings together right after this one (a combo), each shortly after the last if the player is still in reach; the rest comes only at the end, the longest of the chain.</summary>
        public string[] Combo;
        public float RecoverFactor = 1.5f;
        /// <summary>Harden: the damage multiplier while the move is in its Active phase (the crystal armour holds); 1 = none.</summary>
        public float DamageTakenFactor = 1f;
        /// <summary>Throw: the size of the missile in metres (0 = the default).</summary>
        public float MissileSize;
        /// <summary>If the target is farther than this while the move winds up, the windup is dropped (the cooldown is not spent): a move that must land does not leap at nothing. 0 = never.</summary>
        public float AbortBeyond;
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
        /// <summary>While true, a move in its Recover does not end (its time stands still): Giáng Thạch's rock volley takes as long as it takes. Release it and the rest begins.</summary>
        public bool HoldRecover { get; set; }
        bool stunned;
        float idleLeft;
        /// <summary>Seconds (up to 1.8 times that, random) it stays idle after a move before it may pick the next one: a boss that circles and sizes up its prey.</summary>
        public float MinIdle { get; set; }
        /// <summary>Between two moves, in the idle gap.</summary>
        public bool Circling => Phase == MovePhase.Idle && idleLeft > 0f;
        MoveDef comboOwner; int comboNext; float chainRest, currentRest, recoverLen;

        /// <summary>The last part of the recover, after the recover proper: the boss is crouched and out of breath (never after a stun).</summary>
        public bool Resting => Phase == MovePhase.Recover && !HoldRecover && !stunned && currentRest > 0f && PhaseTime >= recoverLen - Eps;
        public float RestTime => Resting ? PhaseTime - recoverLen : 0f;
        public float RestLength => currentRest;

        public MoveScheduler(IReadOnlyList<MoveDef> moves, int seed)
        {
            this.moves = moves;
            cooldownLeft = new float[moves.Count];
            rnd = new Random(seed);
        }

        public float Progress01 => phaseLength <= Eps ? 1f : Math.Min(1f, PhaseTime / phaseLength);
        public bool Busy => Phase != MovePhase.Idle;
        public bool IsArmored => Current != null && (Current.Armored || Current.Shape == MoveShape.Harden) && (Phase == MovePhase.Windup || Phase == MovePhase.Active);
        public float DamageTakenFactor => Phase == MovePhase.Recover ? (Current != null ? Current.RecoverFactor : RecoverDamageFactor) : Phase == MovePhase.Active && Current != null ? Current.DamageTakenFactor : 1f;

        /// <summary>Starts <paramref name="move"/> now with a windup of exactly <paramref name="windupSeconds"/> (an ambusher bursting out). Only when idle.</summary>
        public bool Force(MoveDef move, float windupSeconds)
        {
            if (Phase != MovePhase.Idle || move == null) return false;
            Current = move; stunned = false; StartCombo(move); Phase = MovePhase.Windup; PhaseTime = 0f; phaseLength = Math.Max(0f, windupSeconds);
            return true;
        }

        void StartCombo(MoveDef m) { comboOwner = m.Combo != null && m.Combo.Length > 0 ? m : null; comboNext = 0; chainRest = 0f; currentRest = 0f; }

        MoveDef Find(string name) { for (int i = 0; i < moves.Count; i++) if (moves[i].Name == name) return moves[i]; return null; }

        public void Reset() { comboOwner = null; currentRest = 0f; HoldRecover = false; stunned = false; Phase = MovePhase.Idle; Current = null; PhaseTime = 0f; phaseLength = 0f; }

        /// <summary>Knocked out of its move (a wall, a stun): straight to Recover, lasting its normal recovery plus <paramref name="extraRecover"/>.</summary>
        public void Stun(float extraRecover)
        {
            if (Current == null) return;
            Phase = MovePhase.Recover; PhaseTime = 0f; phaseLength = Current.Recover + Math.Max(0f, extraRecover); stunned = true; comboOwner = null; currentRest = 0f; recoverLen = Current.Recover;
        }

        /// <summary>Advances time. Returns the move that has just started its strike (Active), otherwise null.</summary>
        public MoveDef Update(float dt, bool hasTarget, float targetDistance, float health01)
        {
            for (int i = 0; i < cooldownLeft.Length; i++) cooldownLeft[i] = Math.Max(0f, cooldownLeft[i] - dt);
            idleLeft = Math.Max(0f, idleLeft - dt);
            if (Phase == MovePhase.Idle)
            {
                if (!hasTarget || idleLeft > 0f) return null;
                var pick = Pick(targetDistance, health01);
                if (pick == null) return null;
                Current = pick; stunned = false; StartCombo(pick); Phase = MovePhase.Windup; PhaseTime = 0f; phaseLength = pick.Windup * Math.Max(0.01f, TellScale);
                return null;
            }
            if (Phase == MovePhase.Windup && (!hasTarget || (Current.AbortBeyond > 0f && targetDistance > Current.AbortBeyond))) { Reset(); return null; }

            if (Phase == MovePhase.Recover && HoldRecover) return null;   // the time of a held recover stands still
            PhaseTime += dt;
            if (PhaseTime + Eps < phaseLength) return null;
            switch (Phase)
            {
                case MovePhase.Windup:
                    Phase = MovePhase.Active; PhaseTime = 0f; phaseLength = Current.Active;
                    cooldownLeft[IndexOf(Current)] = Current.Cooldown; last = Current;
                    return Current;
                case MovePhase.Active:
                    {
                        bool more = comboOwner != null && comboNext < comboOwner.Combo.Length;
                        chainRest = Math.Max(chainRest, Current.Rest);
                        currentRest = more ? 0f : chainRest;
                        recoverLen = more ? Math.Min(Current.Recover, 0.5f) : Current.Recover;   // inside a combo the next blow follows almost at once
                        Phase = MovePhase.Recover; PhaseTime = 0f; phaseLength = recoverLen + currentRest; return null;
                    }
                default:
                    if (comboOwner != null && !stunned && currentRest <= 0f && comboNext < comboOwner.Combo.Length)   // a combo step ended: the next one, if the player is still in its reach
                    {
                        var next = Find(comboOwner.Combo[comboNext]);
                        if (next != null && hasTarget && targetDistance >= next.MinRange && targetDistance <= next.MaxRange + 1f)
                        {
                            comboNext++; Current = next; Phase = MovePhase.Windup; PhaseTime = 0f; phaseLength = next.Windup * Math.Max(0.01f, TellScale) * 0.7f;   // a combo blow comes quicker
                            return null;
                        }
                        comboNext = comboOwner.Combo.Length; currentRest = chainRest; phaseLength = recoverLen + currentRest;   // out of reach: the combo is over, it breathes
                        return null;
                    }
                    Reset(); idleLeft = MinIdle * (1f + 0.8f * (float)rnd.NextDouble()); return null;   // a move ended by itself: it circles for a while
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
