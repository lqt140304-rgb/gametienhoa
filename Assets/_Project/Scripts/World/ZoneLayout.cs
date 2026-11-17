using UnityEngine;

namespace Evolve.World
{
    /// <summary>
    /// One area of the zone. <see cref="Size"/> is the full width (x) and depth (z) of its core;
    /// <see cref="Radius"/> is a circle that fits inside it (for code that only needs "near the centre").
    /// </summary>
    public readonly struct Pad
    {
        public readonly string Name;
        public readonly Vector2 Center;
        public readonly Vector2 Size;
        public readonly float Radius;

        public Pad(string name, Vector2 center, Vector2 size)
        {
            Name = name;
            Center = center;
            Size = size;
            Radius = Mathf.Min(size.x, size.y) * 0.5f;
        }

        /// <summary>Distance from p to this area's core rectangle (0 inside).</summary>
        public float DistanceOutside(Vector2 p)
        {
            float dx = Mathf.Max(0f, Mathf.Abs(p.x - Center.x) - Size.x * 0.5f);
            float dz = Mathf.Max(0f, Mathf.Abs(p.y - Center.y) - Size.y * 0.5f);
            return Mathf.Sqrt(dx * dx + dz * dz);
        }
    }

    /// <summary>
    /// Layout of the first zone, Hồ Khởi Sinh / Underroot Lake (spec 2026-11-17-lake-map-500m-design): a 500 x 500 m cavern
    /// with a big lake in the middle and six areas round it, as on the concept zoning map (north = +z).
    /// Shapes are natural, not geometric: shores, the stream's width and the areas' borders all wander with a seeded noise,
    /// so the same numbers always give the same map. Pure data and maths, no scene objects.
    /// </summary>
    public static class ZoneLayout
    {
        public static readonly Pad LifeShallows = new Pad("Vũng Sinh Mệnh", new Vector2(-8f, -120f), new Vector2(80f, 70f));
        public static readonly Pad RootFoot = new Pad("Chân Rễ Cổ Thụ", new Vector2(-150f, -18f), new Vector2(70f, 60f));
        public static readonly Pad Humus = new Pad("Bãi Mùn Rễ", new Vector2(-125f, 118f), new Vector2(80f, 70f));
        public static readonly Pad Spore = new Pad("Vườn Bào Tử", new Vector2(137f, 110f), new Vector2(70f, 70f));
        public static readonly Pad WaterVein = new Pad("Khe Thủy Mạch", new Vector2(173f, -8f), new Vector2(90f, 40f));
        public static readonly Pad HuntEdge = new Pad("Rìa Săn Mồi", new Vector2(13f, 153f), new Vector2(80f, 55f));

        /// <summary>All six areas: 01 Life Shallows, 02 Root Foot, 03 Humus, 04 Spore, 05 Water Vein, 06 Hunt Edge.</summary>
        public static readonly Pad[] Zones = { LifeShallows, RootFoot, Humus, Spore, WaterVein, HuntEdge };
        /// <summary>The land biomes that hold creature populations.</summary>
        public static readonly Pad[] Biomes = { RootFoot, Humus, Spore, HuntEdge };

        /// <summary>Height of every water surface (m).</summary>
        public const float WaterLevel = 0.06f;
        /// <summary>How far (m) an area's influence reaches past its core before it fades out.</summary>
        public const float InfluenceReach = 60f;

        // ---- the lake ----
        public static readonly Vector2 LakeCenter = new Vector2(-8f, -42f);
        public static readonly Vector2 LakeRadii = new Vector2(110f, 80f);
        /// <summary>The Life Shallows bay: the lake is at most 0.4 m deep this close to this point.</summary>
        public static readonly Vector2 BayCenter = new Vector2(-8f, -112f);
        public const float BayRadius = 50f;
        public const float BayDepth = 0.4f;

        // ---- Khe Thủy Mạch ----
        static readonly Vector2[] StreamKeys =
        {
            new Vector2(213f, 10f), new Vector2(187f, -7f), new Vector2(158f, -17f), new Vector2(133f, -25f), new Vector2(103f, -37f), new Vector2(90f, -40f)
        };
        /// <summary>The stream's centre line from its spring (first point) to the lake, densely sampled and gently winding.</summary>
        public static readonly Vector2[] StreamLine = BuildStream();
        public const float StreamDepth = 0.15f;
        public static readonly Vector2[] PoolCenters = { new Vector2(193f, 0f), new Vector2(157f, -20f) };
        public const float PoolRadius = 10f;
        public const float PoolDepth = 2f;

        // ---- the stagnant puddle of the humus bed (dirty water) ----
        public static readonly Vector2 PuddleCenter = new Vector2(-120f, 103f);
        public const float PuddleRadius = 6f;
        public const float PuddleDepth = 0.3f;

        /// <summary>Where creature B comes in from outside: a gap through the north rim.</summary>
        public static readonly Vector2 NorthEntry = new Vector2(17f, 233f);
        public const float NorthEntryHalfWidth = 10f;
        public const float PathWidth = 5f;

        /// <summary>Trails round the lake from area to area, plus the hunter's way in from the north.</summary>
        public static readonly Vector2[][] Paths =
        {
            new[] { new Vector2(-20f, -150f), new Vector2(-95f, -128f), new Vector2(-140f, -75f), new Vector2(-150f, -18f) },   // 01 -> 02
            new[] { new Vector2(-150f, -18f), new Vector2(-142f, 50f), new Vector2(-125f, 118f) },                              // 02 -> 03
            new[] { new Vector2(-125f, 118f), new Vector2(-67f, 142f), new Vector2(13f, 153f) },                                // 03 -> 06
            new[] { new Vector2(13f, 153f), new Vector2(75f, 142f), new Vector2(137f, 110f) },                                  // 06 -> 04
            new[] { new Vector2(137f, 110f), new Vector2(167f, 58f), new Vector2(173f, 22f) },                                  // 04 -> 05
            new[] { new Vector2(150f, -45f), new Vector2(110f, -110f), new Vector2(40f, -148f), new Vector2(-20f, -150f) },    // 05 -> 01
            new[] { NorthEntry, new Vector2(13f, 153f) },                                                                       // the hunter's way in
        };

        // ================================================================== depth

        /// <summary>Depth of water below the water level at (x, z); 0 on dry land.</summary>
        public static float DepthAt(float x, float z) => Mathf.Max(CleanDepthAt(x, z), PuddleDepthAt(x, z));

        /// <summary>Depth of the clean water only (lake, stream, pools).</summary>
        public static float CleanDepthAt(float x, float z)
        {
            var p = new Vector2(x, z);
            float depth = LakeDepthAt(p);

            float ds = DistanceToLine(p, StreamLine, out float t);
            float hw = StreamHalfWidthAt(t);
            if (ds < hw) depth = Mathf.Max(depth, StreamDepth * Smooth01((hw - ds) / 1.5f));

            for (int i = 0; i < PoolCenters.Length; i++)
            {
                Vector2 d = p - PoolCenters[i];
                float r = PoolRadiusAt(i, Mathf.Atan2(d.y, d.x));
                float dp = d.magnitude;
                if (dp < r) depth = Mathf.Max(depth, PoolDepth * Smooth01((r - dp) / 5f));
            }
            return depth;
        }

        /// <summary>True inside the dirty puddle of the humus bed.</summary>
        public static bool InPuddle(float x, float z) => PuddleDepthAt(x, z) > 0f;

        static float PuddleDepthAt(float x, float z)
        {
            float d = Vector2.Distance(new Vector2(x, z), PuddleCenter);
            return d < PuddleRadius ? PuddleDepth * Smooth01((PuddleRadius - d) / 2f) : 0f;
        }

        static float LakeDepthAt(Vector2 p)
        {
            Vector2 d = p - LakeCenter;
            float shoreR = LakeRadiusAt(Mathf.Atan2(d.y, d.x));
            float inside = shoreR - d.magnitude;                   // metres in from the shore
            if (inside <= 0f) return 0f;
            float middle = 1.5f + 0.6f * (Noise(p.x / 35f + 11f, p.y / 35f + 3f) - 0.5f);   // 1.2 .. 1.8 m
            // shoals: a few shallow banks, never in the very middle
            float shoal = Smooth01((Noise(p.x / 50f + 40f, p.y / 50f + 17f) - 0.66f) / 0.08f) * Smooth01((d.magnitude - 25f) / 15f);
            float depth = Mathf.Lerp(middle, 0.35f, shoal) * Smooth01(inside / 10f);
            // the Life Shallows bay stays wadeable
            float bay = Smooth01((BayRadius - Vector2.Distance(p, BayCenter)) / 20f);
            return Mathf.Lerp(depth, Mathf.Min(depth, BayDepth), bay);
        }

        /// <summary>Distance from the lake centre to its shore in the direction <paramref name="angle"/> (radians).</summary>
        public static float LakeRadiusAt(float angle)
        {
            float c = Mathf.Cos(angle), s = Mathf.Sin(angle);
            float ellipse = 1f / Mathf.Sqrt(c * c / (LakeRadii.x * LakeRadii.x) + s * s / (LakeRadii.y * LakeRadii.y));
            float n = Noise(c * 1.6f + 20f, s * 1.6f + 20f) - 0.5f;          // big bays and points
            float m = Noise(c * 4.5f + 50f, s * 4.5f + 50f) - 0.5f;          // smaller coves
            return ellipse * (1f + 0.36f * n + 0.12f * m);
        }

        static float PoolRadiusAt(int pool, float angle)
        {
            float c = Mathf.Cos(angle), s = Mathf.Sin(angle);
            return PoolRadius * (1f + 0.3f * (Noise(c * 1.8f + 70f + pool * 9f, s * 1.8f + 70f) - 0.5f));
        }

        /// <summary>The stream's half width at <paramref name="t"/> (0 = spring, 1 = lake): 3.5 .. 6 m.</summary>
        public static float StreamHalfWidthAt(float t) => 3.5f + 2.5f * Noise(t * 7f + 90f, 5f);

        /// <summary>The lake's shoreline (world x, z), <paramref name="samples"/> points round it.</summary>
        public static Vector2[] LakeShore(int samples)
        {
            var points = new Vector2[samples];
            for (int i = 0; i < samples; i++)
            {
                float a = i * Mathf.PI * 2f / samples;
                points[i] = LakeCenter + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * LakeRadiusAt(a);
            }
            return points;
        }

        /// <summary>A deep pool's shoreline (world x, z).</summary>
        public static Vector2[] PoolShore(int pool, int samples)
        {
            var points = new Vector2[samples];
            for (int i = 0; i < samples; i++)
            {
                float a = i * Mathf.PI * 2f / samples;
                points[i] = PoolCenters[pool] + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * PoolRadiusAt(pool, a);
            }
            return points;
        }

        /// <summary>The stream's banks as one closed outline (left bank down, right bank back up).</summary>
        public static Vector2[] StreamShore()
        {
            int n = StreamLine.Length;
            var points = new Vector2[n * 2];
            for (int i = 0; i < n; i++)
            {
                Vector2 dir = (StreamLine[Mathf.Min(i + 1, n - 1)] - StreamLine[Mathf.Max(i - 1, 0)]).normalized;
                Vector2 side = new Vector2(-dir.y, dir.x);
                float hw = StreamHalfWidthAt(i / (float)(n - 1));
                points[i] = StreamLine[i] + side * hw;
                points[2 * n - 1 - i] = StreamLine[i] - side * hw;
            }
            return points;
        }

        static Vector2[] BuildStream()
        {
            const int perSegment = 12;
            int segments = StreamKeys.Length - 1;
            var line = new Vector2[segments * perSegment + 1];
            int k = 0;
            for (int s = 0; s < segments; s++)
            {
                Vector2 p0 = StreamKeys[Mathf.Max(s - 1, 0)], p1 = StreamKeys[s], p2 = StreamKeys[s + 1], p3 = StreamKeys[Mathf.Min(s + 2, segments)];
                for (int i = 0; i < perSegment; i++)
                {
                    float t = i / (float)perSegment;
                    line[k++] = CatmullRom(p0, p1, p2, p3, t);
                }
            }
            line[k] = StreamKeys[segments];
            // a gentle sideways meander (never at the two ends, which must meet the spring and the lake)
            var result = new Vector2[line.Length];
            for (int i = 0; i < line.Length; i++)
            {
                Vector2 dir = (line[Mathf.Min(i + 1, line.Length - 1)] - line[Mathf.Max(i - 1, 0)]).normalized;
                Vector2 side = new Vector2(-dir.y, dir.x);
                float u = i / (float)(line.Length - 1);
                float ends = Mathf.Sin(u * Mathf.PI);
                result[i] = line[i] + side * (8f * (Noise(u * 9f + 130f, 2f) - 0.5f) * ends);
            }
            return result;
        }

        static Vector2 CatmullRom(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            float t2 = t * t, t3 = t2 * t;
            return 0.5f * (2f * p1 + (p2 - p0) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (3f * p1 - p0 - 3f * p2 + p3) * t3);
        }

        // ================================================================== areas

        /// <summary>
        /// How strongly area <paramref name="zone"/> (index into <see cref="Zones"/>) shapes the ground at (x, z): 1 in its
        /// core, fading to 0 over <see cref="InfluenceReach"/> metres outside it, along a wandering (noisy) border.
        /// </summary>
        public static float Influence(int zone, float x, float z)
        {
            var pad = Zones[zone];
            float hx = pad.Size.x * 0.5f, hz = pad.Size.y * 0.5f;
            float nx = (x - pad.Center.x) / hx, nz = (z - pad.Center.y) / hz;
            float e = Mathf.Sqrt(nx * nx + nz * nz);                          // 1 on the core's (oval) edge
            float outside = Mathf.Max(0f, e - 1f) * (hx + hz) * 0.5f;
            float warp = 22f * (Noise(x / 40f + zone * 31f, z / 40f + 7f) - 0.5f) * Smooth01(e);
            return 1f - Smooth01((outside + warp) / InfluenceReach);
        }

        /// <summary>The strongest influence of any area at (x, z).</summary>
        public static float MaxInfluence(float x, float z)
        {
            float best = 0f;
            for (int i = 0; i < Zones.Length; i++) best = Mathf.Max(best, Influence(i, x, z));
            return best;
        }

        /// <summary>Distance from (x, z) to the nearest trail centre line.</summary>
        public static float DistanceToPath(float x, float z)
        {
            var p = new Vector2(x, z);
            float best = float.MaxValue;
            for (int i = 0; i < Paths.Length; i++) best = Mathf.Min(best, DistanceToLine(p, Paths[i]));
            return best;
        }

        /// <summary>
        /// Rough distance from a dry point to the nearest water's edge (0 in water); only accurate near the water, which is
        /// what the terrain needs to lay the banks down gently.
        /// </summary>
        public static float DistanceToWater(float x, float z)
        {
            var p = new Vector2(x, z);
            if (DepthAt(x, z) > 0f) return 0f;
            Vector2 d = p - LakeCenter;
            float best = d.magnitude - LakeRadiusAt(Mathf.Atan2(d.y, d.x));
            float ds = DistanceToLine(p, StreamLine, out float t) - StreamHalfWidthAt(t);
            best = Mathf.Min(best, ds);
            for (int i = 0; i < PoolCenters.Length; i++) best = Mathf.Min(best, Vector2.Distance(p, PoolCenters[i]) - PoolRadius * 1.15f);
            best = Mathf.Min(best, Vector2.Distance(p, PuddleCenter) - PuddleRadius);
            return Mathf.Max(0f, best);
        }

        public static float DistanceToLine(Vector2 p, Vector2[] line) => DistanceToLine(p, line, out _);

        /// <summary>Distance to a polyline, and where along it (0 = first point, 1 = last) the nearest point lies.</summary>
        public static float DistanceToLine(Vector2 p, Vector2[] line, out float along)
        {
            float best = float.MaxValue;
            along = 0f;
            int segments = line.Length - 1;
            for (int j = 0; j < segments; j++)
            {
                Vector2 a = line[j], ab = line[j + 1] - a;
                float len2 = ab.sqrMagnitude;
                float t = len2 > 1e-6f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / len2) : 0f;
                float dist = Vector2.Distance(p, a + ab * t);
                if (dist < best) { best = dist; along = (j + t) / segments; }
            }
            return best;
        }

        // ================================================================== noise

        /// <summary>Smooth value noise in 0..1 (own implementation, so tests and tools on any machine get the same map).</summary>
        public static float Noise(float x, float y)
        {
            int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y);
            float fx = x - xi, fy = y - yi;
            float u = fx * fx * (3f - 2f * fx), v = fy * fy * (3f - 2f * fy);
            float a = Hash(xi, yi), b = Hash(xi + 1, yi), c = Hash(xi, yi + 1), d = Hash(xi + 1, yi + 1);
            return Mathf.Lerp(Mathf.Lerp(a, b, u), Mathf.Lerp(c, d, u), v);
        }

        static float Hash(int x, int y)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263;
                h = (h ^ (h >> 13)) * 1274126177;
                h ^= h >> 16;
                return (h & 0xffff) / 65535f;
            }
        }

        static float Smooth01(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }
    }
}
