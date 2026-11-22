using System;

namespace Evolve.Combat
{
    /// <summary>
    /// When the boss-fight music plays: any blow in the fight (the boss attacking, or taking damage) calls <see cref="Ping"/> and the music fades in
    /// within 0.6 s; after <see cref="QuietSeconds"/> with no blow it fades out over <see cref="FadeSeconds"/> and stops; a blow during the fade brings it
    /// back. <see cref="Silence"/> ends it at once (the boss died). Pure: the Unity side only sets an AudioSource volume from <see cref="Volume"/>.
    /// </summary>
    public sealed class BossMusicRules
    {
        public const float QuietSeconds = 10f, FadeSeconds = 5f, FadeInSeconds = 0.6f;

        float sinceBlow = float.MaxValue, silenceSeconds;
        bool silenced;
        public float Volume { get; private set; }
        public bool Playing => Volume > 0.001f;

        public void Ping() { sinceBlow = 0f; silenced = false; }

        public void Silence(float seconds) { silenced = true; silenceSeconds = Math.Max(0.05f, seconds); }

        public void Tick(float dt)
        {
            if (dt <= 0f) return;
            sinceBlow += dt;
            float target;
            if (silenced) { Volume = Math.Max(0f, Volume - dt / silenceSeconds); return; }
            if (sinceBlow < QuietSeconds) target = 1f;
            else target = Math.Max(0f, 1f - (sinceBlow - QuietSeconds) / FadeSeconds);   // a straight fade from full to nothing
            if (target > Volume) Volume = Math.Min(target, Volume + dt / FadeInSeconds);
            else Volume = target;
            if (Volume < 0.001f) Volume = 0f;
        }
    }
}
