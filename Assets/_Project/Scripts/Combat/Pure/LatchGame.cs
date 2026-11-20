using System;

namespace Evolve.Combat
{
    public enum StruggleKind { Even, Jerky, TwoPhase }
    public enum LatchState { Holding, Escaped, MonsterDead }

    /// <summary>How a species struggles when a slime latches on (spec 2026-11-20): zone width as a share of the bar, faults allowed, rhythm, strength, how often it heaves.</summary>
    public readonly struct StruggleProfile
    {
        public readonly float ZoneWidth, Amplitude, HeavyPerSecond;
        public readonly int MaxFaults;
        public readonly StruggleKind Kind;
        public StruggleProfile(float zoneWidth, int maxFaults, StruggleKind kind, float amplitude, float heavyPerSecond)
        { ZoneWidth = zoneWidth; MaxFaults = maxFaults; Kind = kind; Amplitude = amplitude; HeavyPerSecond = heavyPerSecond; }
    }

    public struct LatchStep
    {
        public float Damage, EnergySpent;
        public LatchState State;
        public bool HeavyWarning;
    }

    /// <summary>
    /// The grip mini game (like fishing in Stardew Valley): a dot on a bar drifts as the monster struggles; the player pulls it back
    /// into the green zone. In the zone the monster is digested (damage); out of it for longer than <see cref="Grace"/> is a fault, and
    /// enough faults and the monster shakes the slime off. A heavy struggle is announced for <see cref="HeavyWindow"/>: squeeze in time
    /// and the zone widens for a while, miss and it is a fault. Pure and seeded so it can be tested.
    /// </summary>
    public sealed class LatchGame
    {
        public const float DigestPerSecond = 8f, Grace = 0.25f, EnergyPerSecond = 4f, PlayerSpeed = 1.2f, HeavyWindow = 0.5f, HeavyCooldown = 2f,
            SqueezeBoost = 1.4f, SqueezeSeconds = 3f, DesperateBelow = 0.3f, DesperateFactor = 1.6f, NoEnergyZone = 0.6f, WeakWidth = 0.06f;

        readonly StruggleProfile p;
        readonly float sizeFactor, phase;
        readonly Random rnd;
        float outTime, heavyTimer, heavyCooldown, squeezeLeft, impulse, impulseTimer;
        bool squeezedThisHeavy;

        public float Dot { get; private set; } = 0.5f;
        public float ZoneWidth { get; private set; }
        public float WeakCenter { get; private set; } = 0.5f;
        public float Time { get; private set; }
        public int Faults { get; private set; }
        public bool Telegraphing { get; private set; }
        public int MaxFaults => p.MaxFaults;

        public LatchGame(StruggleProfile profile, float slimeMass, float monsterMass, int seed)
        {
            p = profile;
            sizeFactor = Math.Min(1.5f, Math.Max(0.6f, slimeMass / Math.Max(0.01f, monsterMass)));
            rnd = new Random(seed);
            phase = (float)(rnd.NextDouble() * Math.PI * 2.0);
            ZoneWidth = Zone(1f);
        }

        float Zone(float energy) => p.ZoneWidth * sizeFactor * (energy <= 0f ? NoEnergyZone : 1f) * (squeezeLeft > 0f ? SqueezeBoost : 1f);

        public LatchStep Step(float dt, float input, bool squeeze, float monsterHealth01, float energy)
        {
            var r = new LatchStep { State = LatchState.Holding };
            if (monsterHealth01 <= 0f) { r.State = LatchState.MonsterDead; return r; }
            dt = Math.Max(0f, dt);
            Time += dt;
            ZoneWidth = Zone(energy);
            squeezeLeft = Math.Max(0f, squeezeLeft - dt);
            r.EnergySpent = energy <= 0f ? 0f : Math.Min(energy, EnergyPerSecond * dt);

            float amp = p.Amplitude * (monsterHealth01 < DesperateBelow ? DesperateFactor : 1f);
            float drift;
            switch (p.Kind)
            {
                case StruggleKind.Jerky:
                    impulseTimer -= dt;
                    if (impulseTimer <= 0f) { impulse = ((float)rnd.NextDouble() * 2f - 1f) * amp * 1.5f; impulseTimer = 0.6f; }
                    drift = impulse; break;
                case StruggleKind.TwoPhase:
                    drift = (float)Math.Sin(Time * 2.5 + phase) * amp * (Time % 3f < 2f ? 0.5f : 1.8f); break;
                default:
                    drift = (float)Math.Sin(Time * 2.0 + phase) * amp; break;
            }
            Dot = Math.Min(1f, Math.Max(0f, Dot + (drift + Math.Min(1f, Math.Max(-1f, input)) * PlayerSpeed) * dt));
            WeakCenter = 0.5f + (float)Math.Sin(Time * 0.7) * Math.Max(0f, ZoneWidth / 2f - WeakWidth / 2f);

            if (Math.Abs(Dot - 0.5f) <= ZoneWidth / 2f)
            {
                outTime = 0f;
                r.Damage = DigestPerSecond * dt * (Math.Abs(Dot - WeakCenter) <= WeakWidth / 2f ? 2f : 1f);
            }
            else if ((outTime += dt) > Grace) { Faults++; outTime = 0f; }

            if (Telegraphing)
            {
                r.HeavyWarning = true;
                squeezedThisHeavy |= squeeze;
                if ((heavyTimer -= dt) <= 0f)
                {
                    Telegraphing = false; r.HeavyWarning = false; heavyCooldown = HeavyCooldown;
                    if (squeezedThisHeavy) squeezeLeft = SqueezeSeconds; else Faults++;
                }
            }
            else
            {
                heavyCooldown = Math.Max(0f, heavyCooldown - dt);
                if (p.HeavyPerSecond > 0f && heavyCooldown <= 0f && rnd.NextDouble() < p.HeavyPerSecond * dt)
                { Telegraphing = true; heavyTimer = HeavyWindow; squeezedThisHeavy = false; r.HeavyWarning = true; }
            }

            ZoneWidth = Zone(energy);   // a squeeze that just landed shows at once
            if (Faults >= p.MaxFaults) r.State = LatchState.Escaped;
            return r;
        }
    }
}
