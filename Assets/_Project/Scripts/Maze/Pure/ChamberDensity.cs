using System;
using System.Collections.Generic;

namespace Evolve.Maze
{
    /// <summary>
    /// How much life and scenery a chamber gets for its size. The numbers for creatures and props were tuned on the small start
    /// cavern (radius <see cref="ReferenceRadius"/>); the maze's chambers are far wider (75 m), so counts scale up with the radius
    /// and the nests spread out over the whole floor instead of clumping around the centre.
    /// </summary>
    public static class ChamberDensity
    {
        public const float ReferenceRadius = 40f;

        /// <summary>1 for the small start cavern, up to 3 for a huge chamber (75 m gives 1.875).</summary>
        public static float Multiplier(float radius) => Math.Min(3f, Math.Max(1f, radius / ReferenceRadius));

        /// <summary>A count tuned for the reference size, scaled to this chamber (never fewer; the epsilon keeps 0.2×10 from rounding up).</summary>
        public static int Scaled(int baseCount, float radius) => baseCount <= 0 ? 0 : (int)Math.Ceiling(baseCount * Multiplier(radius) - 1e-3);

        /// <summary>Separate nests a species gets: one in the start cavern, two or three in the wide chambers.</summary>
        public static int NestsPerSpecies(float radius) => radius >= 70f ? 3 : radius >= 50f ? 2 : 1;

        /// <summary><paramref name="total"/> heads split over <paramref name="nests"/> nests: the first nests take the remainder.</summary>
        public static int Share(int total, int nests, int index) => total / nests + (index < total % nests ? 1 : 0);

        /// <summary>
        /// Up to <paramref name="count"/> extra nest spots on the floor (local x, z): on rings between 40% and 85% of
        /// <paramref name="usableRadius"/>, not in front of a tunnel mouth (<paramref name="blockedAnglesDeg"/>, 22° each side)
        /// and at least <paramref name="minSeparation"/> from each other and from <paramref name="existing"/>. Same seed, same spots.
        /// </summary>
        public static List<(float X, float Z)> NestPositions(int count, float usableRadius, int seed, IReadOnlyList<float> blockedAnglesDeg,
            IReadOnlyList<(float X, float Z)> existing = null, float minSeparation = 20f)
        {
            var result = new List<(float X, float Z)>();
            var rnd = new Random(seed);
            for (int attempt = 0; attempt < 400 && result.Count < count; attempt++)
            {
                double angle = rnd.NextDouble() * 360.0;
                if (Blocked(angle, blockedAnglesDeg)) continue;
                double r = usableRadius * (0.4 + 0.45 * Math.Sqrt(rnd.NextDouble()));
                float x = (float)(Math.Cos(angle * Math.PI / 180.0) * r), z = (float)(Math.Sin(angle * Math.PI / 180.0) * r);
                if (TooClose(x, z, result, minSeparation) || (existing != null && TooClose(x, z, existing, minSeparation))) continue;
                result.Add((x, z));
            }
            return result;
        }

        static bool Blocked(double angle, IReadOnlyList<float> blocked)
        {
            if (blocked == null) return false;
            foreach (float b in blocked)
            {
                double d = Math.Abs(((angle - b) % 360.0 + 540.0) % 360.0 - 180.0);
                if (d < 22.0) return true;
            }
            return false;
        }

        static bool TooClose(float x, float z, IReadOnlyList<(float X, float Z)> others, float min)
        {
            foreach (var o in others) { float dx = o.X - x, dz = o.Z - z; if (dx * dx + dz * dz < min * min) return true; }
            return false;
        }
    }
}
