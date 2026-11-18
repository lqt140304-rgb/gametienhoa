using System;
using System.Collections.Generic;
using UnityEngine;

namespace Evolve.World
{
    public readonly struct PropSpot
    {
        public readonly Vector2 Position;
        public readonly float Size;
        public readonly int Variant;
        public readonly float YawDegrees;

        public PropSpot(Vector2 position, float size, int variant, float yawDegrees)
        {
            Position = position;
            Size = size;
            Variant = variant;
            YawDegrees = yawDegrees;
        }
    }

    /// <summary>Where props may stand. Pure maths with a seed so the scene builder and the tests agree.</summary>
    public static class PropPlacement
    {
        public const float MaxWadeDepth = 0.5f;   // deeper than this is swimming water
        public const float TrailClearance = 3f;
        public const float Gap = 1f;               // extra metres between two props

        /// <summary>Up to <paramref name="count"/> spots inside <paramref name="area"/>, none blocked and none overlapping.</summary>
        public static List<PropSpot> Rocks(int seed, Rect area, int count, Func<float, float, bool> blocked, int variantCount, Vector2 sizeRange)
        {
            var rng = new System.Random(seed);
            var result = new List<PropSpot>();
            if (variantCount <= 0 || count <= 0) return result;
            int attempts = count * 20;
            for (int i = 0; i < attempts && result.Count < count; i++)
            {
                float x = area.xMin + (float)rng.NextDouble() * area.width;
                float z = area.yMin + (float)rng.NextDouble() * area.height;
                float size = Mathf.Lerp(sizeRange.x, sizeRange.y, (float)rng.NextDouble());
                int variant = rng.Next(variantCount);
                float yaw = (float)rng.NextDouble() * 360f;
                if (blocked != null && blocked(x, z)) continue;
                bool overlaps = false;
                var p = new Vector2(x, z);
                foreach (var other in result)
                    if (Vector2.Distance(p, other.Position) < 0.5f * (size + other.Size) + Gap) { overlaps = true; break; }
                if (!overlaps) result.Add(new PropSpot(p, size, variant, yaw));
            }
            return result;
        }

        /// <summary>False in swimming water, on a trail, or too close to a point that must stay clear (shelter, den, spawn).</summary>
        public static bool Allowed(float x, float z, Func<float, float, float> waterDepth, Func<float, float, float> distToPath,
            IList<Vector2> keepClear, float keepClearRadius)
        {
            if (waterDepth != null && waterDepth(x, z) > MaxWadeDepth) return false;
            if (distToPath != null && distToPath(x, z) < TrailClearance) return false;
            if (keepClear != null)
            {
                var p = new Vector2(x, z);
                for (int i = 0; i < keepClear.Count; i++)
                    if (Vector2.Distance(p, keepClear[i]) < keepClearRadius) return false;
            }
            return true;
        }
    }
}
