using System;
using System.Collections.Generic;
using UnityEngine;

namespace Evolve.World
{
    /// <summary>The five glowing plants of the first zone (spec 6.9): one per area, all edible.</summary>
    public enum VegetationKind { GlowMoss, AmberBud, SporePod, RootDew, EmberBerry }

    public readonly struct VegetationPoint
    {
        public readonly Vector2 Position;
        public readonly VegetationKind Kind;

        public VegetationPoint(Vector2 position, VegetationKind kind)
        {
            Position = position;
            Kind = kind;
        }
    }

    /// <summary>
    /// Where the edible plants grow: dense along the trails and in the Humus Bed, thin in the wild and at the Hunting Edge,
    /// so a long walk has hungry stretches. Pure maths with a seed, so the scene builder and the tests agree.
    /// </summary>
    public static class VegetationScatter
    {
        /// <summary>The co-op sync sends at most 400 food blobs per scene; the zone already has 6, so plants stop at 380.</summary>
        public const int MaxPoints = 380;
        public const float MinSpacing = 6f;
        /// <summary>Half side of the planted square, by max(|x|, |z|); keeps plants off the rim cliffs.</summary>
        public const float Reach = 220f;
        public const float PathReach = 12f;
        const int Candidates = 6000;

        static readonly Pad[] Pads = ZoneLayout.Biomes;
        static readonly float[] PadDensity = { 0.35f, 0.9f, 0.7f, 0.15f };
        static readonly VegetationKind[] PadKind =
        {
            VegetationKind.RootDew, VegetationKind.AmberBud, VegetationKind.SporePod, VegetationKind.EmberBerry
        };

        /// <summary>Chance (0..1) that a candidate spot keeps its plant.</summary>
        public static float DensityAt(float x, float z)
        {
            float d = Mathf.Max(Mathf.Abs(x), Mathf.Abs(z));
            if (d > Reach) return 0f;

            if (ZoneLayout.DepthAt(x, z) > 0f) return 0f; // nothing grows in the water
            float best = 0.04f; // the wild
            if (ZoneLayout.DistanceToPath(x, z) <= PathReach) best = 1f;
            var p = new Vector2(x, z);
            for (int i = 0; i < Pads.Length; i++)
                if (Pads[i].DistanceOutside(p) <= 0f) best = Mathf.Max(best, PadDensity[i]);
            return best;
        }

        /// <summary>Which plant grows here: moss along the trails and in the open, the biome's own plant inside a pad.</summary>
        public static VegetationKind KindAt(float x, float z)
        {
            if (ZoneLayout.DistanceToPath(x, z) <= PathReach) return VegetationKind.GlowMoss;
            var p = new Vector2(x, z);
            for (int i = 0; i < Pads.Length; i++)
                if (Pads[i].DistanceOutside(p) <= 0f) return PadKind[i];
            return VegetationKind.GlowMoss;
        }

        /// <summary>
        /// At most <see cref="MaxPoints"/> plants, at least <see cref="MinSpacing"/> apart, none where
        /// <paramref name="blocked"/> says so. Same seed, same plants.
        /// </summary>
        public static List<VegetationPoint> Points(int seed, Func<float, float, bool> blocked)
        {
            var rnd = new System.Random(seed);
            var kept = new List<Vector2>();
            var grid = new Dictionary<long, List<Vector2>>();
            float cell = MinSpacing;

            for (int i = 0; i < Candidates; i++)
            {
                // always draw all three numbers so the sequence does not depend on earlier rejections
                float x = ((float)rnd.NextDouble() * 2f - 1f) * Reach;
                float z = ((float)rnd.NextDouble() * 2f - 1f) * Reach;
                float roll = (float)rnd.NextDouble();
                if (roll > DensityAt(x, z)) continue;
                if (blocked != null && blocked(x, z)) continue;

                int cx = Mathf.FloorToInt(x / cell), cz = Mathf.FloorToInt(z / cell);
                bool tooClose = false;
                for (int dx = -1; dx <= 1 && !tooClose; dx++)
                    for (int dz = -1; dz <= 1 && !tooClose; dz++)
                        if (grid.TryGetValue(Key(cx + dx, cz + dz), out var near))
                            for (int k = 0; k < near.Count; k++)
                                if ((near[k] - new Vector2(x, z)).sqrMagnitude < MinSpacing * MinSpacing) { tooClose = true; break; }
                if (tooClose) continue;

                var pos = new Vector2(x, z);
                kept.Add(pos);
                long key = Key(cx, cz);
                if (!grid.TryGetValue(key, out var list)) grid[key] = list = new List<Vector2>();
                list.Add(pos);
            }

            if (kept.Count > MaxPoints)
            {
                for (int i = kept.Count - 1; i > 0; i--) // seeded shuffle keeps the share of each area when trimming
                {
                    int j = rnd.Next(i + 1);
                    (kept[i], kept[j]) = (kept[j], kept[i]);
                }
                kept.RemoveRange(MaxPoints, kept.Count - MaxPoints);
            }

            var result = new List<VegetationPoint>(kept.Count);
            for (int i = 0; i < kept.Count; i++) result.Add(new VegetationPoint(kept[i], KindAt(kept[i].x, kept[i].y)));
            return result;
        }

        static long Key(int cx, int cz) => ((long)cx << 32) ^ (uint)cz;
    }
}
