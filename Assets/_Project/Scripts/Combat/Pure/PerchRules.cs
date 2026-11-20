using System;
using System.Collections.Generic;

namespace Evolve.Combat
{
    /// <summary>A spot a creature could jump up to: the ground height there (world x, y, z).</summary>
    public readonly struct Ledge
    {
        public readonly float X, Y, Z;
        public Ledge(float x, float y, float z) { X = x; Y = y; Z = z; }
    }

    /// <summary>Where a monkey jumps to get high (spec 2026-11-20): a ledge a little higher than its feet, close enough, on the far side from the player.</summary>
    public static class PerchRules
    {
        /// <summary>Index of the best ledge, or -1: rise within [minRise, maxRise] above the creature, within maxDistance of it, farther from the player than the creature is; the one farthest from the player wins.</summary>
        public static int PickLedge(IReadOnlyList<Ledge> ledges, float cx, float cy, float cz, float px, float pz, float minRise = 1.5f, float maxRise = 3f, float maxDistance = 7f)
        {
            float own = Dist(cx, cz, px, pz);
            int best = -1; float bestFar = -1f;
            for (int i = 0; i < ledges.Count; i++)
            {
                var l = ledges[i];
                float rise = l.Y - cy;
                if (rise < minRise || rise > maxRise || Dist(cx, cz, l.X, l.Z) > maxDistance) continue;
                float far = Dist(l.X, l.Z, px, pz);
                if (far <= own || far <= bestFar) continue;
                best = i; bestFar = far;
            }
            return best;
        }

        static float Dist(float ax, float az, float bx, float bz) => (float)Math.Sqrt((ax - bx) * (ax - bx) + (az - bz) * (az - bz));
    }
}
