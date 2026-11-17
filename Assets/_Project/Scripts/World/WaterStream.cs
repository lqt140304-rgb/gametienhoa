using UnityEngine;

namespace Evolve.World
{
    /// <summary>
    /// The two banks of a winding stream: along a centre line, one point to the left and one to the right of every
    /// centre point. The stream's width wanders a little (wider here, narrower there) so it never looks ruled.
    /// </summary>
    public static class WaterStream
    {
        public static void Edges(Vector2[] path, float halfWidth, int seed, float wobble, out Vector2[] left, out Vector2[] right)
        {
            int n = path.Length;
            left = new Vector2[n];
            right = new Vector2[n];
            var rnd = new System.Random(seed);
            float p1 = (float)rnd.NextDouble() * Mathf.PI * 2f, p2 = (float)rnd.NextDouble() * Mathf.PI * 2f;
            float travelled = 0f;
            for (int i = 0; i < n; i++)
            {
                if (i > 0) travelled += Vector2.Distance(path[i], path[i - 1]);
                Vector2 t = (path[Mathf.Min(i + 1, n - 1)] - path[Mathf.Max(i - 1, 0)]);
                t = t.sqrMagnitude > 1e-8f ? t.normalized : Vector2.right;
                Vector2 normal = new Vector2(-t.y, t.x);
                float w = halfWidth * (1f + wobble * (Mathf.Sin(travelled * 0.35f + p1) + 0.5f * Mathf.Sin(travelled * 0.8f + p2)) / 1.5f);
                left[i] = path[i] + normal * w;
                right[i] = path[i] - normal * w;
            }
        }

        /// <summary>The outline of the whole stream as one closed polygon (left bank forward, right bank back).</summary>
        public static Vector2[] Polygon(Vector2[] left, Vector2[] right)
        {
            int n = left.Length;
            var polygon = new Vector2[n * 2];
            for (int i = 0; i < n; i++) polygon[i] = left[i];
            for (int i = 0; i < n; i++) polygon[n + i] = right[n - 1 - i];
            return polygon;
        }
    }
}
