using System.IO;
using Evolve.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Evolve.EditorTools
{
    /// <summary>
    /// Paints the ground of the zone and makes the terrain cheaper. Menu: Evolve -> Dress Lake: Terrain.
    /// Two ground textures made in code (mottled brown mud with pebbles, and moss) are painted over the terrain: more
    /// moss near the water and in big soft patches, so the ground is no longer one flat grey. The terrain's level of
    /// detail is relaxed (fewer triangles far away) and it casts no shadows.
    /// Run after "Dress Lake: Water" (the shores decide where the moss is).
    /// </summary>
    public static class LakeTerrain
    {
        const string Root = "Assets/_Project";

        [MenuItem("Evolve/Dress Lake: Terrain")]
        public static void Dress()
        {
            var terrain = Object.FindAnyObjectByType<Terrain>();
            if (terrain == null) { Debug.LogWarning("Không có Terrain trong scene (khu 480 m dùng Terrain). Bỏ qua bước vẽ nền."); return; }
            PrototypeSceneBuilder.EnsureFolder(Root + "/Models");
            PrototypeSceneBuilder.EnsureFolder(Root + "/Terrain");
            WaterSurface.Refresh();

            var mud = Tiling(Root + "/Models/LakeMud.png", 128, (u, v) =>
            {
                float big = LakeWater.TileableNoise(u * 5f, v * 5f, 5, 5);
                float mid = LakeWater.TileableNoise(u * 13f, v * 13f, 13, 13);
                float fine = LakeWater.TileableNoise(u * 40f, v * 40f, 40, 40);
                float tone = 0.65f + 0.25f * big + 0.2f * mid * (0.6f + 0.4f * fine);
                var c = new Color(0.30f, 0.24f, 0.20f) * tone;
                // a few lighter pebbles
                if (LakeWater.TileableNoise(u * 64f, v * 64f, 64, 64) > 0.93f) c = Color.Lerp(c, new Color(0.55f, 0.53f, 0.50f), 0.7f);
                c.a = 1f;
                return c;
            });
            var moss = Tiling(Root + "/Models/LakeGroundMoss.png", 128, (u, v) =>
            {
                float big = LakeWater.TileableNoise(u * 6f, v * 6f, 6, 6);
                float fine = LakeWater.TileableNoise(u * 36f, v * 36f, 36, 36);
                var c = Color.Lerp(new Color(0.14f, 0.28f, 0.10f), new Color(0.34f, 0.52f, 0.20f), 0.25f + 0.55f * big * (0.7f + 0.3f * fine));
                c.a = 1f;
                return c;
            });
            var mudLayer = Layer(Root + "/Terrain/Lake_Mud.terrainlayer", mud, 7f);
            var mossLayer = Layer(Root + "/Terrain/Lake_Moss.terrainlayer", moss, 5f);

            var data = terrain.terrainData;
            data.terrainLayers = new[] { mudLayer, mossLayer };

            // Where the water is: a coarse map of "near water" (so moss gathers along the shores).
            Vector3 origin = terrain.transform.position, size = data.size;
            const int near = 128;
            var nearWater = new float[near, near];
            var shorePoints = new System.Collections.Generic.List<Vector2>();
            foreach (var pond in WaterSurface.All)
            {
                var outline = pond.WorldOutline;
                for (int i = 0; i < outline.Length; i += 2) shorePoints.Add(outline[i]);
            }
            for (int z = 0; z < near; z++)
                for (int x = 0; x < near; x++)
                {
                    var p = new Vector2(origin.x + x / (float)(near - 1) * size.x, origin.z + z / (float)(near - 1) * size.z);
                    float best = float.MaxValue;
                    foreach (var s in shorePoints) best = Mathf.Min(best, Vector2.SqrMagnitude(s - p));
                    float d = Mathf.Sqrt(best);
                    nearWater[z, x] = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((d - 3f) / 14f)); // 1 near the shore, 0 beyond ~17 m
                }

            int res = data.alphamapResolution;
            var maps = new float[res, res, 2];
            for (int z = 0; z < res; z++)
                for (int x = 0; x < res; x++)
                {
                    float fx = x / (float)(res - 1), fz = z / (float)(res - 1);
                    float wx = origin.x + fx * size.x, wz = origin.z + fz * size.z;
                    float patches = Mathf.SmoothStep(0.45f, 0.72f, Mathf.PerlinNoise(wx * 0.035f + 11f, wz * 0.035f + 5f));
                    float edge = Sample(nearWater, fx, fz, near);
                    float w = Mathf.Clamp01(patches * 0.8f + edge * 0.55f);
                    maps[z, x, 0] = 1f - w;
                    maps[z, x, 1] = w;
                }
            data.SetAlphamaps(0, 0, maps);

            // Cheaper: fewer triangles in the distance, no shadows from the ground itself.
            terrain.heightmapPixelError = 12f;
            terrain.basemapDistance = 160f;
            terrain.drawInstanced = true;
            terrain.shadowCastingMode = ShadowCastingMode.Off;

            EditorUtility.SetDirty(data);
            EditorSceneManager.MarkAllScenesDirty();
            AssetDatabase.SaveAssets();
            Debug.Log("Đã vẽ nền địa hình (bùn, rêu) và giảm chi tiết địa hình ở xa. Bấm Ctrl+S để lưu scene.");
        }

        static float Sample(float[,] grid, float u, float v, int n)
        {
            float x = u * (n - 1), z = v * (n - 1);
            int x0 = Mathf.Clamp(Mathf.FloorToInt(x), 0, n - 2), z0 = Mathf.Clamp(Mathf.FloorToInt(z), 0, n - 2);
            float fx = x - x0, fz = z - z0;
            return Mathf.Lerp(Mathf.Lerp(grid[z0, x0], grid[z0, x0 + 1], fx), Mathf.Lerp(grid[z0 + 1, x0], grid[z0 + 1, x0 + 1], fx), fz);
        }

        static TerrainLayer Layer(string path, Texture2D diffuse, float tile)
        {
            var layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
            if (layer == null)
            {
                layer = new TerrainLayer();
                AssetDatabase.CreateAsset(layer, path);
            }
            layer.diffuseTexture = diffuse;
            layer.tileSize = new Vector2(tile, tile);
            layer.smoothness = 0.1f;
            EditorUtility.SetDirty(layer);
            return layer;
        }

        /// <summary>A texture that repeats seamlessly (both directions), painted in code and saved as a PNG.</summary>
        static Texture2D Tiling(string path, int size, System.Func<float, float, Color> paint)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null) return existing;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    pixels[y * size + x] = paint(x / (float)size, y / (float)size);
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false);
            t.SetPixels32(pixels);
            File.WriteAllBytes(path, t.EncodeToPNG());
            Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.mipmapEnabled = true;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
