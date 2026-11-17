using UnityEngine;

namespace Evolve.World
{
    /// <summary>
    /// The shoreline of a pond: a closed outline around the origin (xz plane), points evenly spaced along it.
    /// The base shape is a rounded rectangle (or a circle), bent by a few slow waves of different lengths so the
    /// bank wanders like a real shore: never straight, never a perfect curve. The same seed gives the same shore.
    /// </summary>
    public static class WaterOutline
    {
        public static Vector2[] Make(float width, float depth, bool disc, int seed, float wobble, int samples)
        {
            float hw = Mathf.Max(0.25f, width * 0.5f), hd = Mathf.Max(0.25f, depth * 0.5f);
            float power = disc ? 2f : 3.5f; // 2 = ellipse, higher = rounded rectangle
            samples = Mathf.Max(12, samples);

            // A dense walk around the base shape, then pick evenly spaced points along its length.
            int fine = Mathf.Max(600, samples * 8);
            var dense = new Vector2[fine + 1];
            var cumulative = new float[fine + 1];
            for (int i = 0; i <= fine; i++)
            {
                float a = i / (float)fine * Mathf.PI * 2f;
                float c = Mathf.Cos(a), s = Mathf.Sin(a);
                dense[i] = new Vector2(hw * Mathf.Sign(c) * Mathf.Pow(Mathf.Abs(c), 2f / power),
                                       hd * Mathf.Sign(s) * Mathf.Pow(Mathf.Abs(s), 2f / power));
                if (i > 0) cumulative[i] = cumulative[i - 1] + Vector2.Distance(dense[i], dense[i - 1]);
            }
            float perimeter = cumulative[fine];

            var outline = new Vector2[samples];
            int seg = 0;
            for (int k = 0; k < samples; k++)
            {
                float target = k / (float)samples * perimeter;
                while (seg < fine - 1 && cumulative[seg + 1] < target) seg++;
                float span = Mathf.Max(1e-5f, cumulative[seg + 1] - cumulative[seg]);
                outline[k] = Vector2.Lerp(dense[seg], dense[seg + 1], Mathf.Clamp01((target - cumulative[seg]) / span));
            }

            // Wander: three waves along the shore (about 11 m, 6 m and 3.4 m long), pushing the shore in or out.
            float amplitude = Mathf.Min(wobble, 0.3f * Mathf.Min(hw, hd));
            if (amplitude <= 0f) return outline;
            var rnd = new System.Random(seed);
            var lengths = new[] { 11f, 6f, 3.4f };
            var weights = new[] { 1f, 0.6f, 0.3f };
            var counts = new int[3];
            var phases = new float[3];
            float weightSum = 0f;
            for (int w = 0; w < 3; w++)
            {
                counts[w] = Mathf.Max(2, Mathf.RoundToInt(perimeter / lengths[w])); // whole waves, so the shore closes
                phases[w] = (float)rnd.NextDouble() * Mathf.PI * 2f;
                weightSum += weights[w];
            }
            for (int k = 0; k < samples; k++)
            {
                float u = k / (float)samples;
                float n = 0f;
                for (int w = 0; w < 3; w++) n += weights[w] * Mathf.Sin(Mathf.PI * 2f * counts[w] * u + phases[w]);
                Vector2 dir = outline[k].sqrMagnitude > 1e-6f ? outline[k].normalized : Vector2.right;
                outline[k] += dir * (amplitude * n / weightSum);
            }
            return outline;
        }

        /// <summary>Roughly how long the shore is (to choose how many points to use).</summary>
        public static float PerimeterEstimate(float width, float depth, bool disc) =>
            disc ? Mathf.PI * width : 2f * (width + depth);
    }
}
