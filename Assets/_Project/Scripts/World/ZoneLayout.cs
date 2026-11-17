using UnityEngine;

namespace Evolve.World
{
    /// <summary>A circular flat area (bệ) where one biome stands. The terrain is exactly flat at y = 0 under it.</summary>
    public readonly struct Pad
    {
        public readonly string Name;
        public readonly Vector2 Center;
        public readonly float Radius;

        public Pad(string name, Vector2 center, float radius)
        {
            Name = name;
            Center = center;
            Radius = radius;
        }
    }

    /// <summary>
    /// Layout of the first zone (spec 6.9): where each biome stands, the stream and the trails between them.
    /// Pure data and maths, no scene objects, so the terrain, the scene builder and the tests all read the same numbers.
    /// Coordinates are metres from the zone centre (x east, z north).
    /// </summary>
    public static class ZoneLayout
    {
        public static readonly Pad RootFoot = new Pad("Chân Rễ Cổ Thụ", new Vector2(0f, 170f), 50f);
        public static readonly Pad Humus = new Pad("Bãi Mùn Rễ", new Vector2(170f, 0f), 50f);
        public static readonly Pad Spore = new Pad("Vườn Bào Tử", new Vector2(-170f, 0f), 50f);
        public static readonly Pad HuntEdge = new Pad("Rìa Săn Mồi", new Vector2(170f, -170f), 50f);

        public static readonly Pad[] Biomes = { RootFoot, Humus, Spore, HuntEdge };

        /// <summary>Khe Thủy Mạch: a flat channel running south from the core.</summary>
        public static readonly Vector2 StreamStart = new Vector2(0f, -65f);
        public static readonly Vector2 StreamEnd = new Vector2(0f, -170f);
        public const float StreamHalfWidth = 10f;

        public const float PadBlend = 60f;      // distance over which the terrain rises from a pad to the hills
        public const float StreamBlend = 40f;
        public const float ContentExtent = 35f; // how far from a pad centre the biome's content reaches
        public const float PathWidth = 5f;

        /// <summary>Trails between the core and the biomes; the Hunting Edge has two (an alternative route).</summary>
        public static readonly Vector2[][] Paths =
        {
            new[] { new Vector2(0f, 60f), new Vector2(0f, 125f) },        // core -> north
            new[] { new Vector2(60f, 0f), new Vector2(125f, 0f) },        // core -> east
            new[] { new Vector2(-60f, 0f), new Vector2(-125f, 0f) },      // core -> west
            new[] { new Vector2(14f, -60f), new Vector2(14f, -170f) },    // core -> south, beside the stream
            new[] { new Vector2(170f, -50f), new Vector2(170f, -125f) },  // east -> hunting edge
            new[] { new Vector2(14f, -170f), new Vector2(125f, -170f) },  // south -> hunting edge (loop)
        };

        /// <summary>1 on a pad or the stream channel, falling smoothly to 0 beyond the blend distance.</summary>
        public static float PadMask(float x, float z)
        {
            var p = new Vector2(x, z);
            float mask = 0f;
            for (int i = 0; i < Biomes.Length; i++)
            {
                float d = Vector2.Distance(p, Biomes[i].Center);
                mask = Mathf.Max(mask, 1f - Smooth01((d - Biomes[i].Radius) / PadBlend));
            }
            float ds = DistanceToSegment(p, StreamStart, StreamEnd);
            mask = Mathf.Max(mask, 1f - Smooth01((ds - StreamHalfWidth) / StreamBlend));
            return mask;
        }

        /// <summary>Distance from (x, z) to the nearest trail centre line.</summary>
        public static float DistanceToPath(float x, float z)
        {
            var p = new Vector2(x, z);
            float best = float.MaxValue;
            for (int i = 0; i < Paths.Length; i++)
            {
                var path = Paths[i];
                for (int j = 0; j + 1 < path.Length; j++)
                    best = Mathf.Min(best, DistanceToSegment(p, path[j], path[j + 1]));
            }
            return best;
        }

        static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float len2 = ab.sqrMagnitude;
            float t = len2 > 1e-6f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / len2) : 0f;
            return Vector2.Distance(p, a + ab * t);
        }

        static float Smooth01(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }
    }
}
