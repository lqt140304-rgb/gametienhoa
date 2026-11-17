using UnityEngine;

namespace Evolve.World
{
    /// <summary>
    /// One area of the zone. <see cref="Size"/> is the full width (x) and depth (z) of its rough rectangle;
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

        /// <summary>Distance from p to this area's rectangle (0 inside).</summary>
        public float DistanceOutside(Vector2 p)
        {
            float dx = Mathf.Max(0f, Mathf.Abs(p.x - Center.x) - Size.x * 0.5f);
            float dz = Mathf.Max(0f, Mathf.Abs(p.y - Center.y) - Size.y * 0.5f);
            return Mathf.Sqrt(dx * dx + dz * dz);
        }
    }

    /// <summary>
    /// Layout of the first zone, Hồ Khởi Sinh / Underroot Lake (spec 2026-11-17-lake-map-300m-design): a 300 x 300 m
    /// cavern (each area 1.5 x its concept size) with a big lake in the middle and six areas around it, as on the concept zoning map (north = +z).
    /// Pure data and maths, no scene objects: the terrain, the water, the decoration tools and the tests all read these numbers.
    /// </summary>
    public static class ZoneLayout
    {
        public static readonly Pad LifeShallows = new Pad("Vũng Sinh Mệnh", new Vector2(8f, -80f), new Vector2(45f, 37.5f));
        public static readonly Pad RootFoot = new Pad("Chân Rễ Cổ Thụ", new Vector2(-85f, -15f), new Vector2(37.5f, 30f));
        public static readonly Pad Humus = new Pad("Bãi Mùn Rễ", new Vector2(-70f, 68f), new Vector2(45f, 37.5f));
        public static readonly Pad Spore = new Pad("Vườn Bào Tử", new Vector2(98f, 65f), new Vector2(37.5f, 37.5f));
        public static readonly Pad WaterVein = new Pad("Khe Thủy Mạch", new Vector2(122f, -10f), new Vector2(22.5f, 52.5f));
        public static readonly Pad HuntEdge = new Pad("Rìa Săn Mồi", new Vector2(20f, 90f), new Vector2(45f, 30f));

        /// <summary>All six areas, in map order 01..06 except that 06 is last.</summary>
        public static readonly Pad[] Zones = { LifeShallows, RootFoot, Humus, Spore, WaterVein, HuntEdge };
        /// <summary>The land biomes that hold creature populations.</summary>
        public static readonly Pad[] Biomes = { RootFoot, Humus, Spore, HuntEdge };

        /// <summary>Height of every water surface (m).</summary>
        public const float WaterLevel = 0.06f;

        // ---- the lake: a wobbly ellipse in the middle, and the shallow bay of the Life Shallows to its south ----
        public static readonly Vector2 LakeCenter = new Vector2(25f, -5f);
        public static readonly Vector2 LakeRadii = new Vector2(60f, 35f);
        public const float LakeDepth = 1.6f;
        public static readonly Vector2 ShallowsCenter = new Vector2(8f, -70f);
        public static readonly Vector2 ShallowsRadii = new Vector2(42f, 30f);
        public const float ShallowsDepth = 0.4f;

        // ---- Khe Thủy Mạch: a shallow stream from the north-east into the lake, with two deep pools on it ----
        public static readonly Vector2[] StreamLine =
        {
            new Vector2(132f, 62f), new Vector2(124f, 30f), new Vector2(118f, 0f), new Vector2(104f, -28f), new Vector2(84f, -14f)
        };
        public const float StreamHalfWidth = 4f;
        public const float StreamDepth = 0.15f;
        public static readonly Vector2[] PoolCenters = { new Vector2(122f, 18f), new Vector2(110f, -16f) };
        public const float PoolRadius = 8f;
        public const float PoolDepth = 2f;

        /// <summary>Where creature B comes in from outside: a tunnel through the north rim.</summary>
        public static readonly Vector2 NorthEntry = new Vector2(20f, 140f);
        public const float NorthEntryHalfWidth = 8f;

        public const float PathWidth = 5f;

        /// <summary>Trails round the lake from area to area, plus the hunter's way in from the north entry.</summary>
        public static readonly Vector2[][] Paths =
        {
            new[] { new Vector2(-40f, -70f), new Vector2(-65f, -40f), new Vector2(-85f, -15f) },   // 01 -> 02
            new[] { new Vector2(-85f, -15f), new Vector2(-80f, 25f), new Vector2(-70f, 68f) },     // 02 -> 03
            new[] { new Vector2(-70f, 68f), new Vector2(-25f, 80f), new Vector2(20f, 90f) },       // 03 -> 06
            new[] { new Vector2(20f, 90f), new Vector2(60f, 80f), new Vector2(98f, 65f) },         // 06 -> 04
            new[] { new Vector2(98f, 65f), new Vector2(104f, 40f), new Vector2(100f, 10f), new Vector2(92f, -30f) }, // 04 -> 05 (west bank)
            new[] { new Vector2(92f, -30f), new Vector2(65f, -62f), new Vector2(58f, -82f) },      // 05 -> 01
            new[] { new Vector2(20f, 90f), NorthEntry },                                           // 06 -> the way out north
        };

        /// <summary>Depth of water below the water level at (x, z); 0 on dry land.</summary>
        public static float DepthAt(float x, float z)
        {
            var p = new Vector2(x, z);
            float depth = 0f;

            float lake = EllipseT(p, LakeCenter, LakeRadii, 0f);
            if (lake < 1f) depth = Mathf.Max(depth, LakeDepth * Smooth01((1f - lake) / 0.45f));
            float bay = EllipseT(p, ShallowsCenter, ShallowsRadii, 1.7f);
            if (bay < 1f) depth = Mathf.Max(depth, ShallowsDepth * Smooth01((1f - bay) / 0.25f));

            float ds = DistanceToLine(p, StreamLine);
            if (ds < StreamHalfWidth) depth = Mathf.Max(depth, StreamDepth * Smooth01((StreamHalfWidth - ds) / 1.5f));
            for (int i = 0; i < PoolCenters.Length; i++)
            {
                float dp = Vector2.Distance(p, PoolCenters[i]);
                if (dp < PoolRadius) depth = Mathf.Max(depth, PoolDepth * Smooth01((PoolRadius - dp) / 4f));
            }
            return depth;
        }

        /// <summary>The lake's shoreline (world x, z), <paramref name="samples"/> points round it.</summary>
        public static Vector2[] LakeShore(int samples) => Shore(LakeCenter, LakeRadii, 0f, samples);

        /// <summary>The Life Shallows bay's shoreline (world x, z).</summary>
        public static Vector2[] ShallowsShore(int samples) => Shore(ShallowsCenter, ShallowsRadii, 1.7f, samples);

        static Vector2[] Shore(Vector2 centre, Vector2 radii, float phase, int samples)
        {
            var points = new Vector2[samples];
            for (int i = 0; i < samples; i++)
            {
                float a = i * Mathf.PI * 2f / samples;
                float c = Mathf.Cos(a), s = Mathf.Sin(a);
                float r = Wobble(a, phase) / Mathf.Sqrt(c * c / (radii.x * radii.x) + s * s / (radii.y * radii.y));
                points[i] = centre + new Vector2(c, s) * r;
            }
            return points;
        }

        static float Wobble(float angle, float phase) =>
            1f + 0.08f * Mathf.Sin(3f * angle + phase) + 0.05f * Mathf.Sin(5f * angle + 1.3f + phase);

        /// <summary>
        /// Normalised distance from an ellipse centre (1 on its shore); the shore wobbles a little so it is not a perfect oval.
        /// </summary>
        static float EllipseT(Vector2 p, Vector2 centre, Vector2 radii, float phase)
        {
            Vector2 d = p - centre;
            float a = Mathf.Atan2(d.y, d.x);
            float wobble = Wobble(a, phase);
            float nx = d.x / (radii.x * wobble), nz = d.y / (radii.y * wobble);
            return Mathf.Sqrt(nx * nx + nz * nz);
        }

        /// <summary>1 inside an area, on a trail or in/near water (the ground there stays flat); 0 far from all of them.</summary>
        public static float FlatMask(float x, float z)
        {
            var p = new Vector2(x, z);
            float d = DistanceToPath(x, z) - PathWidth;
            for (int i = 0; i < Zones.Length; i++) d = Mathf.Min(d, Zones[i].DistanceOutside(p));
            if (DepthAt(x, z) > 0f) d = 0f;
            d = Mathf.Min(d, EllipseT(p, LakeCenter, LakeRadii, 0f) < 1.25f ? 0f : d);
            return 1f - Smooth01(d / 25f);
        }

        /// <summary>Distance from (x, z) to the nearest trail centre line.</summary>
        public static float DistanceToPath(float x, float z)
        {
            var p = new Vector2(x, z);
            float best = float.MaxValue;
            for (int i = 0; i < Paths.Length; i++) best = Mathf.Min(best, DistanceToLine(p, Paths[i]));
            return best;
        }

        /// <summary>The area whose rectangle holds p (the nearest one if several overlap), or null-name pad if none.</summary>
        public static bool TryZoneAt(Vector2 p, out Pad zone)
        {
            zone = default;
            float best = float.MaxValue;
            for (int i = 0; i < Zones.Length; i++)
            {
                if (Zones[i].DistanceOutside(p) > 0f) continue;
                float d = Vector2.Distance(p, Zones[i].Center);
                if (d < best) { best = d; zone = Zones[i]; }
            }
            return best < float.MaxValue;
        }

        public static float DistanceToLine(Vector2 p, Vector2[] line)
        {
            float best = float.MaxValue;
            for (int j = 0; j + 1 < line.Length; j++) best = Mathf.Min(best, DistanceToSegment(p, line[j], line[j + 1]));
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
