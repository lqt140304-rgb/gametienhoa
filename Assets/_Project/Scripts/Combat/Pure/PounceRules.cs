using System;
using System.Collections.Generic;
using Evolve.Swallow;

namespace Evolve.Combat
{
    public enum PounceOutcome { SwallowWhole, Latch, Bounce }

    /// <summary>A creature the slime could pounce on: where it is (flat) and whether another slime already holds it.</summary>
    public readonly struct Candidate
    {
        public readonly float X, Z;
        public readonly bool Taken;
        public Candidate(float x, float z, bool taken) { X = x; Z = z; Taken = taken; }
    }

    /// <summary>Pure rules for the Vồ (pounce) action: what it does to a creature of a given mass and which creature it aims at.</summary>
    public static class PounceRules
    {
        /// <summary>A creature heavier than this many slime masses cannot be held: the pounce bounces off.</summary>
        public const float BounceRatio = 4f;

        public static PounceOutcome Decide(float slimeMass, float monsterMass)
        {
            if (MassRules.CanSwallowWhole(slimeMass, monsterMass)) return PounceOutcome.SwallowWhole;
            return monsterMass > slimeMass * BounceRatio + 1e-4f ? PounceOutcome.Bounce : PounceOutcome.Latch;
        }

        /// <summary>Index of the nearest free candidate in front of the slime within <paramref name="maxDistance"/> (cosine to the facing at least <paramref name="minDot"/>), or -1.</summary>
        public static int PickTarget(IReadOnlyList<Candidate> candidates, float px, float pz, float fx, float fz, float maxDistance = 4f, float minDot = 0.5f)
        {
            float fl = (float)Math.Sqrt(fx * fx + fz * fz);
            if (fl < 1e-5f) return -1;
            fx /= fl; fz /= fl;
            int best = -1; float bestD = float.MaxValue;
            for (int i = 0; i < candidates.Count; i++)
            {
                var c = candidates[i];
                if (c.Taken) continue;
                float dx = c.X - px, dz = c.Z - pz, d = (float)Math.Sqrt(dx * dx + dz * dz);
                if (d > maxDistance || d < 1e-5f) continue;
                if ((dx * fx + dz * fz) / d < minDot) continue;
                if (d < bestD) { bestD = d; best = i; }
            }
            return best;
        }
    }
}
