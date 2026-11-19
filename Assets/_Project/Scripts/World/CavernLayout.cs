using System;
using System.Collections.Generic;
using UnityEngine;

namespace Evolve.World
{
    /// <summary>
    /// The Slime Cavern (spec 2026-11-19-slime-cavern-design): a small cavern with one lake in the middle, the only place the slime is born.
    /// A ring of small plants and ore round the lake, steep walls, a roof with a hole of sky above the lake. Pure data and maths, no scene objects.
    /// North = +z. Heights are metres above the water surface (y = 0 at the lake level, like the old zone).
    /// </summary>
    public static class CavernLayout
    {
        /// <summary>Side of the terrain square (m).</summary>
        public const float Size = 128f;
        public const float WaterLevel = 0.06f;

        public static readonly Vector2 LakeCenter = Vector2.zero;
        public const float LakeMeanRadius = 16f;
        public const float MaxDepth = 8f;

        /// <summary>The floor is dry and gentle out to here; the wall rises over the next stretch.</summary>
        public const float FloorRadius = 34f;
        public const float WallTopRadius = 46f;
        public const float WallHeight = 30f;
        /// <summary>Height of the roof over the middle; it comes down to WallHeight toward the edge.</summary>
        public const float CeilingHeight = 36f;
        public const float HoleRadius = 6.5f;

        /// <summary>The sun: elevation and compass heading (degrees), chosen so its beam through the hole falls on the lake.</summary>
        public const float SunElevation = 66f;
        public const float SunAzimuth = 35f;

        static float Smooth01(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        // ---------------------------------------------------------------- the lake

        /// <summary>Radius of the shore in the direction <paramref name="angle"/> (radians): a wandering outline, not a circle.</summary>
        public static float LakeRadius(float angle) =>
            LakeMeanRadius * (0.82f + 0.36f * ZoneLayout.Noise(Mathf.Cos(angle) * 2.2f + 7f, Mathf.Sin(angle) * 2.2f + 3f));

        static float Angle(float x, float z) => Mathf.Atan2(z - LakeCenter.y, x - LakeCenter.x);
        static float Distance(float x, float z) => Vector2.Distance(new Vector2(x, z), LakeCenter);

        /// <summary>Metres from the lake's middle.</summary>
        public static float RadiusFromCenter(float x, float z) => Distance(x, z);

        /// <summary>How far (m) a point is beyond the shore: negative in the water, 0 at the edge, positive on land.</summary>
        public static float ShoreOffset(float x, float z) => Distance(x, z) - LakeRadius(Angle(x, z));

        /// <summary>Water depth in metres (0 = dry): shallow and gentle at the shore, deepest (8 m) in the middle.</summary>
        public static float DepthAt(float x, float z)
        {
            float r = Distance(x, z);
            if (r < 0.0001f) return MaxDepth;
            float shore = LakeRadius(Angle(x, z));
            if (r >= shore) return 0f;
            return MaxDepth * Smooth01((1f - r / shore) / 0.6f);
        }

        // ---------------------------------------------------------------- the ground

        /// <summary>Ground height (m above the water): the lake bed, a gentle shore with small swells, then the wall.</summary>
        public static float GroundHeight(float x, float z)
        {
            float r = Distance(x, z);
            float depth = DepthAt(x, z);
            if (depth > 0f) return -depth;
            float d = r - LakeRadius(Angle(x, z));
            // the floor is not a plain: patches of mounds and low rises (up to about 2 m, a few metres to a dozen across) with flat ground between them and a little fine unevenness everywhere
            float patch = Smooth01((ZoneLayout.Noise(x / 15f + 11f, z / 15f + 23f) - 0.46f) / 0.3f);
            float mound = patch * (1.0f + 0.9f * ZoneLayout.Noise(x / 6f + 5f, z / 6f + 8f));
            float fine = (ZoneLayout.Noise(x / 4.5f + 40f, z / 4.5f + 17f) - 0.5f) * 0.35f;
            float h = 0.12f * Smooth01(d / 1.5f) + 0.9f * Smooth01(d / 14f) + (mound + fine) * Smooth01(d / 5f);   // starts at the water's level: no little cliff along the shore
            float rw = r + 8f * (ZoneLayout.Noise(x / 14f + 3f, z / 14f + 9f) - 0.5f);   // the wall's foot wanders by about 4 m
            float wall = WallHeight * Smooth01((rw - FloorRadius) / (WallTopRadius - FloorRadius));
            return Mathf.Max(h, wall);
        }

        /// <summary>Where the slimes appear: on the dry shore, a few metres from the water, to the south-west.</summary>
        public static Vector3 SpawnPoint()
        {
            float a = 215f * Mathf.Deg2Rad;
            float r = LakeRadius(a) + 3f;
            float x = LakeCenter.x + Mathf.Cos(a) * r, z = LakeCenter.y + Mathf.Sin(a) * r;
            return new Vector3(x, GroundHeight(x, z) + 0.6f, z);
        }

        // ---------------------------------------------------------------- the roof and the sky above the lake

        /// <summary>Height of the roof at (x, z), before the hole is cut.</summary>
        public static float CeilingY(float x, float z)
        {
            float r = Mathf.Sqrt(x * x + z * z);
            float u = Mathf.Clamp01(r / 50f);
            return Mathf.Lerp(CeilingHeight, WallHeight, u * u);
        }

        /// <summary>The direction the sunlight travels (pointing down).</summary>
        public static Vector3 SunDirection => Quaternion.Euler(SunElevation, SunAzimuth, 0f) * Vector3.forward;

        /// <summary>
        /// Centre of the hole in the roof: the point on the roof from which the sun's beam lands in the middle of the lake. It lies
        /// up-sun of the lake, a few metres to the side.
        /// </summary>
        public static Vector2 HoleCenter()
        {
            var d = SunDirection;
            var p = LakeCenter;
            for (int i = 0; i < 4; i++)
            {
                float t = CeilingY(p.x, p.y) / -d.y;           // metres travelled along the beam from the roof to the lake's surface
                p = LakeCenter - new Vector2(d.x, d.z) * t;
            }
            return p;
        }

        public static bool InHole(float x, float z) => Vector2.Distance(new Vector2(x, z), HoleCenter()) < HoleRadius;

        // ---------------------------------------------------------------- plants, ore, rocks

        /// <summary>Small plants: a carpet round the lake from the wet edge out across the dry floor, thinning toward the wall, never in water or on the wall.</summary>
        public static float PlantDensity(float x, float z)
        {
            if (DepthAt(x, z) > 0f || GroundHeight(x, z) > 3f) return 0f;
            float d = Distance(x, z) - LakeRadius(Angle(x, z));
            float band = Smooth01(d / 2f) * (1f - Smooth01((d - 16f) / 14f));
            return band * (0.45f + 0.55f * ZoneLayout.Noise(x / 6f + 11f, z / 6f + 5f));
        }

        /// <summary>Glowing mushrooms: in clumps, mostly in the shade of the wall's foot and at the water's edge.</summary>
        public static float MushroomDensity(float x, float z)
        {
            if (DepthAt(x, z) > 0f || GroundHeight(x, z) > 6f) return 0f;
            float d = Distance(x, z) - LakeRadius(Angle(x, z));
            float edge = 1f - Smooth01(Mathf.Abs(d - 1.5f) / 3f);                                   // the wet rim of the lake
            float foot = Smooth01((Distance(x, z) - (FloorRadius - 8f)) / 6f);                      // the foot of the wall
            float clump = Mathf.Clamp01((ZoneLayout.Noise(x / 7f + 70f, z / 7f + 31f) - 0.55f) * 4f);
            return Mathf.Max(edge * 0.5f, foot) * clump;
        }

        /// <summary>Ore: rare clusters on the dry floor between the shore and the wall.</summary>
        public static float OreDensity(float x, float z)
        {
            if (DepthAt(x, z) > 0f || GroundHeight(x, z) > 8f) return 0f;
            float d = Distance(x, z) - LakeRadius(Angle(x, z));
            float band = Smooth01((d - 3f) / 3f) * (1f - Smooth01((d - 24f) / 8f));
            float rare = Mathf.Clamp01((ZoneLayout.Noise(x / 8f + 120f, z / 8f + 80f) - 0.66f) * 6f);
            return band * rare;
        }

        /// <summary>Plain rocks: many, small, anywhere dry and not on the wall.</summary>
        public static float RockDensity(float x, float z)
        {
            if (DepthAt(x, z) > 0f || GroundHeight(x, z) > 5f) return 0f;
            return 0.15f + 0.5f * Mathf.Clamp01((ZoneLayout.Noise(x / 5f + 9f, z / 5f + 51f) - 0.5f) * 3f);
        }

        /// <summary>
        /// Seeded points for a density: <paramref name="attempts"/> random spots over the terrain square, each kept with the density's
        /// chance. Returns (x, ground height, z). The same seed always gives the same points.
        /// </summary>
        /// <summary>
        /// Like <see cref="Scatter"/> but no two kept points are closer than <paramref name="minDistance"/> (metres) and at most
        /// <paramref name="max"/> are kept: for things that stand alone, like ore clusters.
        /// </summary>
        public static List<Vector3> ScatterSpaced(int seed, int attempts, Func<float, float, float> density, float minDistance, int max, float limit = 44f)
        {
            var list = new List<Vector3>();
            var rnd = new System.Random(seed);
            for (int i = 0; i < attempts && list.Count < max; i++)
            {
                float x = ((float)rnd.NextDouble() * 2f - 1f) * limit, z = ((float)rnd.NextDouble() * 2f - 1f) * limit;
                if (rnd.NextDouble() >= density(x, z)) continue;
                bool clear = true;
                foreach (var p in list)
                    if ((p.x - x) * (p.x - x) + (p.z - z) * (p.z - z) < minDistance * minDistance) { clear = false; break; }
                if (clear) list.Add(new Vector3(x, GroundHeight(x, z), z));
            }
            return list;
        }

        public static List<Vector3> Scatter(int seed, int attempts, Func<float, float, float> density, float limit = 44f)
        {
            var list = new List<Vector3>();
            var rnd = new System.Random(seed);
            for (int i = 0; i < attempts; i++)
            {
                float x = ((float)rnd.NextDouble() * 2f - 1f) * limit, z = ((float)rnd.NextDouble() * 2f - 1f) * limit;
                if (rnd.NextDouble() < density(x, z)) list.Add(new Vector3(x, GroundHeight(x, z), z));
            }
            return list;
        }
    }
}
