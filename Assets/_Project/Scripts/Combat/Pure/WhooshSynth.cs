using System;

namespace Evolve.Combat
{
    /// <summary>A whoosh made from code (no sound file): noise through a band-pass filter whose centre frequency sweeps from one pitch to another, fading in and out.</summary>
    public static class WhooshSynth
    {
        /// <summary>Mono samples, peak 0.85. A sweep from high to low reads as something passing away; low to high as something winding up.</summary>
        public static float[] Make(int seed, int sampleRate, float seconds, float startHz, float endHz)
        {
            int n = Math.Max(1, (int)(sampleRate * seconds));
            var s = new float[n];
            uint state = (uint)seed * 2654435761u + 12345u;
            float low = 0f, band = 0f;
            const float damping = 0.25f;
            float peak = 1e-6f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                float fc = startHz + (endHz - startHz) * t;
                float f = Math.Min(0.95f, 2f * (float)Math.Sin(Math.PI * fc / sampleRate));
                state = state * 1664525u + 1013904223u;
                float noise = ((state >> 8) / (float)(1 << 24)) * 2f - 1f;
                low += f * band;
                float high = noise - low - damping * band;
                band += f * high;
                float env = (float)Math.Sin(Math.PI * t); env *= env;
                s[i] = band * env;
                peak = Math.Max(peak, Math.Abs(s[i]));
            }
            float k = 0.85f / peak;
            for (int i = 0; i < n; i++) s[i] *= k;
            return s;
        }
    }
}
