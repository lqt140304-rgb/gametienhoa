using System;
using System.Collections.Generic;

namespace Evolve.Maze
{
    /// <summary>Where the parts of a wide chamber go (local x, z): a landmark seen from the main entrance, nests, and a hidden nook.</summary>
    public struct ChamberPlan
    {
        public (float X, float Z) Beacon;
        public List<(float X, float Z)> Nests;
        public (float X, float Z) Nook;
        /// <summary>Direction (degrees) the nook's boulder ring is open towards: outward, away from the chamber centre.</summary>
        public float NookOpeningDeg;
    }

    /// <summary>
    /// Zones of a wide chamber (spec 2026-11-20): the first tunnel is the main entrance and a big landmark stands across from it,
    /// so it is seen as soon as the player enters; nests sit in clearings away from every tunnel mouth; one nook far from the mouths
    /// hides a reward. Pure and seeded: same inputs, same plan.
    /// </summary>
    public static class ChamberZones
    {
        public static ChamberPlan Plan(float radius, IReadOnlyList<float> tunnelAnglesDeg, int seed, int nestCount)
        {
            float usable = radius * 0.74f;
            var rnd = new Random(seed);
            float main = tunnelAnglesDeg != null && tunnelAnglesDeg.Count > 0 ? tunnelAnglesDeg[0] : 0f;

            double ba = (main + 180.0 + rnd.NextDouble() * 60.0 - 30.0) * Math.PI / 180.0;
            double br = usable * (0.4 + 0.2 * rnd.NextDouble());
            var beacon = ((float)(Math.Cos(ba) * br), (float)(Math.Sin(ba) * br));

            var nests = ChamberDensity.NestPositions(nestCount, usable, seed + 1, tunnelAnglesDeg, new[] { beacon });

            var taken = new List<(float X, float Z)>(nests) { beacon };
            (float X, float Z) nook = default;
            float nookAngle = 0f, best = -1f;
            for (int attempt = 0; attempt < 120; attempt++)
            {
                float angle = (float)(rnd.NextDouble() * 360.0);
                float gap = float.MaxValue;
                if (tunnelAnglesDeg != null) foreach (float t in tunnelAnglesDeg) gap = Math.Min(gap, AngleGap(angle, t));
                double r = usable * (0.8 + 0.15 * rnd.NextDouble());
                var p = ((float)(Math.Cos(angle * Math.PI / 180.0) * r), (float)(Math.Sin(angle * Math.PI / 180.0) * r));
                bool clear = true;
                foreach (var o in taken) { float dx = o.X - p.Item1, dz = o.Z - p.Item2; if (dx * dx + dz * dz < 400f) { clear = false; break; } }
                if (!clear) continue;
                if (gap > best) { best = gap; nook = p; nookAngle = angle; }   // keep the clearest-of-mouths spot found
                if (gap > 40f) break;                                          // good enough: far from every mouth
            }
            return new ChamberPlan { Beacon = beacon, Nests = nests, Nook = nook, NookOpeningDeg = nookAngle };
        }

        static float AngleGap(float a, float b) => (float)Math.Abs(((a - b) % 360.0 + 540.0) % 360.0 - 180.0);
    }
}
