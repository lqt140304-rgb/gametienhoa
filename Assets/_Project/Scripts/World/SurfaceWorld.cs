using UnityEngine;

namespace Evolve.World
{
    /// <summary>
    /// The surface world seen from the bottom of the open-sky pit (spec 2026-11-19-open-sky-pit-design): ground that continues from the rim
    /// outward as low hills and a distant treeline. Nobody walks there; it is only looked at. Pure functions of position, so the editor tool
    /// that builds the mesh and the tests share them. Heights are metres above the lake level (y = 0), like TerrainShape.
    /// </summary>
    public static class SurfaceWorld
    {
        /// <summary>The ground level at the top of the rim wall.</summary>
        public static float RimLevel => TerrainShape.CliffHeight;

        /// <summary>Chebyshev distance from the middle (the rim is a square).</summary>
        public static float DistanceFromRim(float x, float z) => Mathf.Max(Mathf.Abs(x), Mathf.Abs(z)) - WorldScale.ZoneSize * 0.5f;

        /// <summary>
        /// Height of the surface ground at (x, z): flat at the rim level where it meets the wall, then rolling hills that grow with distance
        /// (up to about 45 m) so the horizon is not a straight line. Never below the rim level except where the north entry's gap runs on as a short canyon.
        /// </summary>
        public static float HeightAt(float x, float z, int seed)
        {
            float d = Mathf.Max(0f, DistanceFromRim(x, z));
            float grow = Smooth01(d / 160f);
            float ox = seed * 0.731f, oz = seed * 0.377f;
            float hills = 45f * grow * (0.35f + 0.65f * ZoneLayout.Noise(x / 55f + 200f + ox, z / 55f + 200f + oz))
                        + 8f * grow * ZoneLayout.Noise(x / 17f + 11f + ox, z / 17f + 31f + oz);
            float h = RimLevel + hills;

            // the gap at the north entry carries on outward for a while as a canyon
            if (z > 0f)
            {
                float across = Mathf.Abs(x - ZoneLayout.NorthEntry.x) - ZoneLayout.NorthEntryHalfWidth;
                float gap = (1f - Smooth01(across / 12f)) * (1f - Smooth01(d / 120f));
                h = Mathf.Lerp(h, 8f + 0.1f * d, gap);
            }
            return h;
        }

        static float Smooth01(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }
    }
}
