using UnityEngine;

namespace Evolve.World
{
    /// <summary>Where the areas' landmarks stand (pure maths from ZoneLayout and TerrainShape, so it can be tested).</summary>
    public static class LandmarkLayout
    {
        /// <summary>The great rotting log of the humus bed (03): its middle and the direction it lies.</summary>
        public static readonly Vector2 LogCenter = new Vector2(-150f, 145f);
        public const float LogAngle = 25f, LogLength = 9f, LogRadius = 0.95f;

        /// <summary>The giant mushroom cluster on the spore garden's plateau (04).</summary>
        public static Vector2 GiantMushrooms => ZoneLayout.Spore.Center;

        /// <summary>
        /// The root bridge from the plateau (04) to the ridge (06): <paramref name="a"/> is the last point still on the plateau
        /// walking from its centre toward the ridge's, <paramref name="b"/> the last point still on the ridge walking back.
        /// False if there is no gap worth a bridge.
        /// </summary>
        public static bool RootBridge(int seed, out Vector3 a, out Vector3 b)
        {
            Vector2 from = ZoneLayout.Spore.Center, to = ZoneLayout.HuntEdge.Center;
            float plateau = ZoneLayout.ZoneLevels[3] - 1f, ridge = ZoneLayout.ZoneLevels[5];
            a = Edge(from, to, plateau, seed);
            b = Edge(to, from, ridge, seed);
            float gap = Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
            return gap > 10f && Vector2.Dot(new Vector2(b.x - a.x, b.z - a.z), to - from) > 0f;
        }

        static Vector3 Edge(Vector2 from, Vector2 to, float minHeight, int seed)
        {
            Vector2 dir = (to - from).normalized;
            float length = Vector2.Distance(from, to);
            Vector2 last = from;
            for (float d = 0f; d < length; d += 1f)
            {
                Vector2 p = from + dir * d;
                if (TerrainShape.HeightAt(p.x, p.y, seed) < minHeight) break;
                last = p;
            }
            return new Vector3(last.x, TerrainShape.HeightAt(last.x, last.y, seed), last.y);
        }
    }
}
