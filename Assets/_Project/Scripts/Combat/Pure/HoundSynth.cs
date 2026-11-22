using System;

namespace Evolve.Combat
{
    /// <summary>
    /// Sounds for Chó Xương made from code (no files), as mono samples with a peak of 0.85: a growl (a buzzing low voice with a wobble and a breathy
    /// noise, its pitch gliding from one frequency to another: a short one for the tell, a long one for the roar, a rising-falling one for the howl),
    /// a thud (a body landing: a falling sine plus a burst of noise) and a snap (jaws closing: two sharp clicks over a low knock).
    /// </summary>
    public static class HoundSynth
    {
        static uint Next(ref uint state) { state = state * 1664525u + 1013904223u; return state; }
        static float Noise(ref uint state) => ((Next(ref state) >> 8) / (float)(1 << 24)) * 2f - 1f;

        static float[] Normalise(float[] s)
        {
            float peak = 1e-6f;
            foreach (var x in s) peak = Math.Max(peak, Math.Abs(x));
            float k = 0.85f / peak;
            for (int i = 0; i < s.Length; i++) s[i] *= k;
            return s;
        }

        public static float[] Growl(int seed, int sampleRate, float seconds, float startHz, float endHz)
        {
            int n = Math.Max(1, (int)(sampleRate * seconds));
            var s = new float[n];
            uint st = (uint)seed * 2654435761u + 977u;
            double phase = 0, wob = 0; float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                float hz = startHz + (endHz - startHz) * t;
                phase += hz / sampleRate; wob += 31.0 / sampleRate;
                float saw = (float)(phase - Math.Floor(phase)) * 2f - 1f;
                float am = 0.65f + 0.35f * (float)Math.Sin(2 * Math.PI * wob);   // the throat wobbles
                lp += 0.12f * (Noise(ref st) - lp);                               // breath: low-passed noise
                float env = (float)Math.Sin(Math.PI * Math.Pow(t, 0.7)); env *= env;
                s[i] = (0.75f * saw * am + 0.9f * lp) * env;
            }
            return Normalise(s);
        }

        public static float[] Thud(int seed, int sampleRate, float seconds)
        {
            int n = Math.Max(1, (int)(sampleRate * seconds));
            var s = new float[n];
            uint st = (uint)seed * 2654435761u + 31u;
            double phase = 0; float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                float hz = 38f + 70f * (float)Math.Exp(-9f * t);
                phase += hz / sampleRate;
                lp += 0.2f * (Noise(ref st) - lp);
                s[i] = ((float)Math.Sin(2 * Math.PI * phase) * (float)Math.Exp(-5f * t) + 0.7f * lp * (float)Math.Exp(-22f * t)) * Math.Min(1f, t * 400f);
            }
            return Normalise(s);
        }

        public static float[] Snap(int seed, int sampleRate, float seconds)
        {
            int n = Math.Max(1, (int)(sampleRate * seconds));
            var s = new float[n];
            uint st = (uint)seed * 2654435761u + 7u;
            double phase = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                phase += (130f - 60f * t) / sampleRate;
                float click1 = (float)Math.Exp(-t * 90f), click2 = i > n / 8 ? (float)Math.Exp(-(t - 0.125f) * 70f) : 0f;
                s[i] = Noise(ref st) * (click1 + 0.7f * click2) + 0.6f * (float)Math.Sin(2 * Math.PI * phase) * (float)Math.Exp(-t * 14f);
            }
            return Normalise(s);
        }
    }
}
