using System;

namespace Evolve.Combat
{
    public enum PinKey { None, A, D }

    /// <summary>
    /// The escape mini game when a hound's pounce lands head first and it clamps its jaws on the player: tap A and D one after the other to fill the
    /// bar and shake it off. The same key twice in a row does nothing useful (it costs a little), the bar drains on its own, and the bite hurts all the
    /// time while it lasts. After <see cref="MaxSeconds"/> the hound lets go by itself. Pure, so the feel can be tested.
    /// </summary>
    public sealed class PinGame
    {
        public const float DamagePerSecond = 6f, MaxSeconds = 8f, GainPerPress = 0.09f, WrongPenalty = 0.03f, Decay = 0.18f;

        PinKey last;
        public float Progress { get; private set; }
        public float Elapsed { get; private set; }
        public bool Escaped { get; private set; }
        public bool TimedOut { get; private set; }
        public bool Finished => Escaped || TimedOut;

        public void Press(PinKey key)
        {
            if (Finished || key == PinKey.None) return;
            Progress += key != last ? GainPerPress : -WrongPenalty;
            Progress = Math.Min(1f, Math.Max(0f, Progress));
            last = key;
            if (Progress >= 1f) Escaped = true;
        }

        /// <summary>Advance time; returns the damage the bite does in this step.</summary>
        public float Tick(float dt)
        {
            if (Finished || dt <= 0f) return 0f;
            Elapsed += dt;
            Progress = Math.Max(0f, Progress - Decay * dt);
            if (Elapsed >= MaxSeconds) TimedOut = true;
            return DamagePerSecond * dt;
        }
    }
}
