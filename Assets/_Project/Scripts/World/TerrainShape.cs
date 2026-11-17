using UnityEngine;

namespace Evolve.World
{
    /// <summary>
    /// Shape of the zone terrain: exactly flat core, rolling hills outside it, cliffs at the rim.
    /// Pure functions of position and seed, so they can be tested and rebuilt identically.
    /// </summary>
    public static class TerrainShape
    {
        /// <summary>TerrainData.size.y in metres.</summary>
        public const float TerrainHeight = 60f;
        /// <summary>The core surface (y = 0) sits this far above the terrain's lowest point; place the Terrain at y = -BaseOffset.</summary>
        public const float BaseOffset = 10f;
        public const float CliffStart = 215f;
        public const float CliffHeight = 45f;

        const float BlendWidth = 70f;     // core edge (65 m) to full hills (135 m)
        const float HillAmplitude = 14f;
        const float RimDistance = WorldScale.ZoneSize * 0.5f;

        /// <summary>Height in metres above the core surface at world position (x, z). 0 in the core, never negative.</summary>
        public static float HeightAt(float x, float z, int seed)
        {
            float d = Mathf.Max(Mathf.Abs(x), Mathf.Abs(z)); // square distance: core is a square
            float blend = Smooth01((d - WorldScale.CoreHalf) / BlendWidth);

            float ox = 5000f + seed * 13.37f, oz = 5000f + seed * 7.91f;
            float n1 = Mathf.PerlinNoise((x + ox) / 170f, (z + oz) / 170f);
            float n2 = Mathf.PerlinNoise((x + ox) / 70f, (z + oz) / 70f);
            float n = Mathf.Clamp01(0.65f * n1 + 0.35f * n2);
            float hills = HillAmplitude * n * blend;

            float cliff = CliffHeight * Smooth01((d - CliffStart) / (RimDistance - CliffStart));
            return Mathf.Max(hills, cliff);
        }

        /// <summary>Normalised 0..1 heightmap [z, x] for TerrainData.SetHeights; cell i is at -ZoneSize/2 + i * ZoneSize/(resolution-1).</summary>
        public static float[,] Heights(int resolution, int seed)
        {
            var map = new float[resolution, resolution];
            float cell = WorldScale.ZoneSize / (resolution - 1);
            for (int zi = 0; zi < resolution; zi++)
            {
                float z = -RimDistance + zi * cell;
                for (int xi = 0; xi < resolution; xi++)
                {
                    float x = -RimDistance + xi * cell;
                    map[zi, xi] = Mathf.Clamp01((BaseOffset + HeightAt(x, z, seed)) / TerrainHeight);
                }
            }
            return map;
        }

        static float Smooth01(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }
    }
}
