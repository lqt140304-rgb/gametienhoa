using UnityEngine;

namespace Evolve.World
{
    /// <summary>
    /// Shape of the zone terrain: flat floors in the areas and along the trails, low mounds between them, the lake, stream
    /// and pools carved below the water level (ZoneLayout.DepthAt), and a cliff all round the rim (with a gap at the north
    /// entry). Pure functions of position and seed, so they can be tested and rebuilt identically.
    /// </summary>
    public static class TerrainShape
    {
        /// <summary>TerrainData.size.y in metres.</summary>
        public const float TerrainHeight = 60f;
        /// <summary>The floor (y = 0) sits this far above the terrain's lowest point; place the Terrain at y = -BaseOffset.</summary>
        public const float BaseOffset = 10f;
        public const float CliffStart = 132f;
        public const float CliffHeight = 35f;

        const float MoundAmplitude = 3f;
        const float RimDistance = WorldScale.ZoneSize * 0.5f;

        /// <summary>Height in metres above the floor at world position (x, z); negative under water.</summary>
        public static float HeightAt(float x, float z, int seed)
        {
            float depth = ZoneLayout.DepthAt(x, z);
            if (depth > 0f) return -depth;

            float ox = 5000f + seed * 13.37f, oz = 5000f + seed * 7.91f;
            float n1 = Mathf.PerlinNoise((x + ox) / 60f, (z + oz) / 60f);
            float n2 = Mathf.PerlinNoise((x + ox) / 23f, (z + oz) / 23f);
            float n = Mathf.Clamp01(0.65f * n1 + 0.35f * n2);
            float mounds = MoundAmplitude * n * (1f - ZoneLayout.FlatMask(x, z));

            float d = Mathf.Max(Mathf.Abs(x), Mathf.Abs(z)); // square: the rim follows the zone's sides
            float cliff = CliffHeight * Smooth01((d - CliffStart) / (RimDistance - CliffStart));
            // the hunter's way in: a level gap through the north cliff
            float gap = Mathf.Abs(x - ZoneLayout.NorthEntry.x);
            if (z > 0f) cliff *= Smooth01((gap - ZoneLayout.NorthEntryHalfWidth) / 6f);
            return Mathf.Max(mounds, cliff);
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
