using UnityEngine;

namespace Evolve.World
{
    /// <summary>The plants and ground cover that make up each area's community (spec 2026-11-17-lake-map-500m-design §5).</summary>
    public enum Plant
    {
        WetMoss, SmallCream, ThickMoss, Clover, SurfaceRoot, LeafLitter, RottingLog, EarFungus,
        CreamMushroom, LimestoneRock, BareMud, TrackMark, DarkRoot
    }

    /// <summary>
    /// Which plant grows where. Every plant has a liking for each area (and for the wild between them) and its own patch
    /// pattern (seeded noise): mushrooms come in clusters, moss in mats, leaves in drifts, with bare gaps between. At a point
    /// the areas' influences mix those likings, so one community fades into the next. Pure maths: same input, same plants.
    /// </summary>
    public static class PlantCommunities
    {
        struct Profile
        {
            public float[] Zones;     // liking per area, in ZoneLayout.Zones order (01..06)
            public float Wild;        // liking in the wild between areas
            public float PatchSize;   // metres across one patch
            public float Threshold;   // 0..1: higher = fewer, smaller patches
            public float Density;     // chance per candidate spot inside a full patch where it is liked fully

            public Profile(float z1, float z2, float z3, float z4, float z5, float z6, float wild, float patch, float threshold, float density)
            {
                Zones = new[] { z1, z2, z3, z4, z5, z6 };
                Wild = wild; PatchSize = patch; Threshold = threshold; Density = density;
            }
        }

        //                                        01    02    03    04    05    06   wild  patch thr  dens
        static readonly Profile[] Profiles =
        {
            new Profile(0.8f, 0.2f, 0.1f, 0f,   0.1f, 0f,   0.15f, 9f,  0.45f, 0.45f),   // WetMoss
            new Profile(0.5f, 0.1f, 0.2f, 0.4f, 0.3f, 0.05f, 0.1f, 6f,  0.55f, 0.35f),   // SmallCream
            new Profile(0.1f, 0.9f, 0.2f, 0.1f, 0.3f, 0f,   0.2f, 12f, 0.40f, 0.55f),   // ThickMoss
            new Profile(0.1f, 0.6f, 0f,   0.2f, 0.6f, 0f,   0.15f, 7f,  0.50f, 0.45f),   // Clover
            new Profile(0f,   0.5f, 0.1f, 0f,   0.1f, 0.2f, 0.1f, 20f, 0.55f, 0.06f),   // SurfaceRoot
            new Profile(0.2f, 0.2f, 1f,   0.2f, 0.1f, 0.3f, 0.25f, 10f, 0.35f, 0.65f),  // LeafLitter
            new Profile(0f,   0.05f, 0.5f, 0f,  0f,   0.05f, 0.03f, 25f, 0.60f, 0.04f), // RottingLog
            new Profile(0f,   0.1f, 0.7f, 0.1f, 0f,   0f,   0.02f, 8f,  0.55f, 0.2f),   // EarFungus
            new Profile(0.05f, 0.05f, 0.1f, 1f, 0.05f, 0.02f, 0.03f, 7f, 0.40f, 0.6f),  // CreamMushroom
            new Profile(0.05f, 0f,  0f,   0.1f, 1f,   0.1f, 0.05f, 15f, 0.50f, 0.1f),   // LimestoneRock
            new Profile(0.1f, 0f,   0.1f, 0f,   0.05f, 1f,  0.1f, 14f, 0.35f, 0.45f),   // BareMud
            new Profile(0f,   0f,   0f,   0f,   0f,   0.6f, 0f,   10f, 0.50f, 0.15f),   // TrackMark
            new Profile(0f,   0.1f, 0.1f, 0f,   0f,   0.8f, 0.05f, 22f, 0.50f, 0.06f),  // DarkRoot
        };

        public static int PlantCount => Profiles.Length;

        /// <summary>Chance (0..1) that a candidate spot at (x, z) grows <paramref name="plant"/>; 0 in the water.</summary>
        public static float Density(Plant plant, float x, float z)
        {
            if (ZoneLayout.DepthAt(x, z) > 0f) return 0f;
            return DensityOnLand(plant, x, z, Influences(x, z, out _));
        }

        /// <summary>
        /// The plant growing at a candidate spot, or null for bare ground. <paramref name="roll"/> is a uniform random number
        /// in [0, 1): the plants share the spot in proportion to their densities.
        /// </summary>
        public static Plant? Pick(float x, float z, double roll)
        {
            if (ZoneLayout.DepthAt(x, z) > 0f) return null;
            var influences = Influences(x, z, out _);
            float total = 0f;
            var densities = new float[Profiles.Length];
            for (int i = 0; i < Profiles.Length; i++) { densities[i] = DensityOnLand((Plant)i, x, z, influences); total += densities[i]; }
            float scale = Mathf.Max(1f, total);          // crowded spots: plants share, never more than one per spot
            double acc = 0.0;
            for (int i = 0; i < Profiles.Length; i++)
            {
                acc += densities[i] / scale;
                if (roll < acc) return (Plant)i;
            }
            return null;
        }

        static float[] Influences(float x, float z, out float wild)
        {
            var influences = new float[ZoneLayout.Zones.Length];
            float max = 0f;
            for (int k = 0; k < influences.Length; k++) { influences[k] = ZoneLayout.Influence(k, x, z); max = Mathf.Max(max, influences[k]); }
            wild = 1f - max;
            return influences;
        }

        static float DensityOnLand(Plant plant, float x, float z, float[] influences)
        {
            var p = Profiles[(int)plant];
            float liking = 0f, weights = 0f, max = 0f;
            for (int k = 0; k < influences.Length; k++)
            {
                liking += influences[k] * p.Zones[k];
                weights += influences[k];
                max = Mathf.Max(max, influences[k]);
            }
            float wild = 1f - max;
            liking = (liking + wild * p.Wild) / Mathf.Max(1e-4f, weights + wild);
            int id = (int)plant;
            float n = ZoneLayout.Noise(x / p.PatchSize + 500f + id * 37f, z / p.PatchSize + 200f + id * 11f);
            float patch = Smooth01((n - p.Threshold) / 0.15f);
            return liking * patch * p.Density;
        }

        static float Smooth01(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }
    }
}
