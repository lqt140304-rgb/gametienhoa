using UnityEngine;

namespace Evolve.World
{
    /// <summary>
    /// Shape of the zone terrain (spec 2026-11-17-lake-map-500m-design): gentle bumps inside the areas (about ±1.3 m),
    /// rolling root-mounds up to ~6 m between them, banks that lie down gently toward the water, the lake / stream / pools
    /// carved to their depths (ZoneLayout.DepthAt), and a cliff all round the rim with a gap at the north entry.
    /// Pure functions of position and seed, so they can be tested and rebuilt identically.
    /// </summary>
    public static class TerrainShape
    {
        /// <summary>TerrainData.size.y in metres.</summary>
        public const float TerrainHeight = 40f;
        /// <summary>The lake level (y = 0) sits this far above the terrain's lowest point; place the Terrain at y = -BaseOffset.</summary>
        public const float BaseOffset = 9f;
        public const float CliffStart = 200f;
        public const float CliffHeight = 20f;

        const float AreaBumps = 0.65f;   // gentle, long swells: about 0 .. 1.3 m over ~40 m
        const float MoundHeight = 6f;
        const float RimDistance = WorldScale.ZoneSize * 0.5f;

        /// <summary>Height in metres above the lake level (y = 0) at (x, z); negative under water and in the sinkhole.</summary>
        public static float HeightAt(float x, float z, int seed)
        {
            float depth = ZoneLayout.CleanDepthAt(x, z);
            if (depth > 0f) return -depth;

            float ox = seed * 0.731f, oz = seed * 0.377f;
            float bumps = AreaBumps * 2f * ZoneLayout.Noise(x / 40f + ox, z / 40f + oz);
            float mounds = MoundHeight * Smooth01((ZoneLayout.Noise(x / 60f + 300f + ox, z / 60f + 300f + oz) - 0.3f) / 0.5f);
            float inArea = ZoneLayout.MaxInfluence(x, z);
            float h = Mathf.Lerp(mounds + 0.5f * bumps, bumps, inArea);
            // trails are worn down, banks lie down toward the water (never below it)
            h *= Mathf.Lerp(0.35f, 1f, Smooth01(ZoneLayout.DistanceToPath(x, z) / 8f));
            float bank = Smooth01(ZoneLayout.DistanceToWater(x, z) / 15f);
            h = Mathf.Max(h * bank, 0.1f * bank);
            float level = ZoneLayout.LevelAt(x, z);
            h += level;
            if (ZoneLayout.InPuddle(x, z)) h = level - ZoneLayout.DepthAt(x, z);   // the sinkhole's stagnant puddle

            float d = Mathf.Max(Mathf.Abs(x), Mathf.Abs(z)); // square: the rim follows the zone's sides
            float cliff = CliffHeight * Smooth01((d - CliffStart) / (RimDistance - CliffStart));
            if (z > 0f) cliff *= Smooth01((Mathf.Abs(x - ZoneLayout.NorthEntry.x) - ZoneLayout.NorthEntryHalfWidth) / 8f);
            return cliff > 0f && cliff > h ? cliff : h;
        }

        /// <summary>Normalised 0..1 heightmap [z, x] for TerrainData.SetHeights; cell i is at -ZoneSize/2 + i * ZoneSize/(resolution-1).</summary>
        public static float[,] Heights(int resolution, int seed)
        {
            var map = new float[resolution, resolution];
            var dry = new bool[resolution, resolution];
            float cell = WorldScale.ZoneSize / (resolution - 1);
            for (int zi = 0; zi < resolution; zi++)
            {
                float z = -RimDistance + zi * cell;
                for (int xi = 0; xi < resolution; xi++)
                {
                    float x = -RimDistance + xi * cell;
                    map[zi, xi] = Mathf.Clamp01((BaseOffset + HeightAt(x, z, seed)) / TerrainHeight);
                    dry[zi, xi] = ZoneLayout.DepthAt(x, z) <= 0f;
                }
            }
            // Two gentle smoothing passes over dry land (water beds keep their exact depth), so walking never meets a crease.
            for (int pass = 0; pass < 2; pass++)
            {
                var copy = (float[,])map.Clone();
                for (int zi = 1; zi < resolution - 1; zi++)
                    for (int xi = 1; xi < resolution - 1; xi++)
                    {
                        if (!dry[zi, xi]) continue;
                        float sum = 0f; int n = 0;
                        for (int dz = -1; dz <= 1; dz++)
                            for (int dx = -1; dx <= 1; dx++)
                                if (dry[zi + dz, xi + dx]) { sum += copy[zi + dz, xi + dx]; n++; }
                        map[zi, xi] = sum / n;
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
