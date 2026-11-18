using System.IO;
using Evolve.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Evolve.EditorTools
{
    /// <summary>
    /// Paints the ground of the zone. Menu: Evolve → Dress Lake: Terrain.
    /// Nine ground layers made in code, each a 256 px tileable texture with its own relief (normal map): dry earth, wet
    /// mud, moss, leaf litter, limestone, trampled mud, pebbles, root bed and worn trail. The weights come from the same
    /// data that decides what grows where (World/PlantCommunities, the area influences), plus the distance to the water
    /// (dark wet mud on the banks, a band of pebbles) and to the trails, and a big soft colour drift so no stretch of
    /// ground looks the same as the next. The result is a ground that changes from area to area and melts into itself,
    /// instead of flat patches laid on top. The terrain's level of detail is also made finer (fewer jagged facets).
    /// </summary>
    public static class LakeTerrain
    {
        const string Root = "Assets/_Project";
        const int Res = 512;           // alpha map: about 1 m per cell over 500 m
        const int TexSize = 256;

        enum Ground { DryEarth, WetMud, Moss, Leaf, Limestone, Trampled, Pebbles, RootBed, Trail }

        [MenuItem("Evolve/Dress Lake: Terrain")]
        public static void Dress()
        {
            var terrain = Object.FindAnyObjectByType<Terrain>();
            if (terrain == null) { Debug.LogWarning("Không có Terrain trong scene. Bỏ qua bước vẽ nền."); return; }
            PrototypeSceneBuilder.EnsureFolder(Root + "/Models");
            PrototypeSceneBuilder.EnsureFolder(Root + "/Terrain");

            var layers = new TerrainLayer[9];
            layers[(int)Ground.DryEarth] = Layer("DryEarth", 6.5f, DryEarth);
            layers[(int)Ground.WetMud] = Layer("WetMud", 5f, WetMud);
            layers[(int)Ground.Moss] = Layer("Moss", 4.5f, Moss);
            layers[(int)Ground.Leaf] = Layer("Leaf", 3.5f, LeafLitter);
            layers[(int)Ground.Limestone] = Layer("Limestone", 7f, Limestone);
            layers[(int)Ground.Trampled] = Layer("Trampled", 5f, Trampled);
            layers[(int)Ground.Pebbles] = Layer("Pebbles", 3f, Pebbles);
            layers[(int)Ground.RootBed] = Layer("RootBed", 5f, RootBed);
            layers[(int)Ground.Trail] = Layer("Trail", 4f, Trail);

            var data = terrain.terrainData;
            data.terrainLayers = layers;
            data.alphamapResolution = Res;
            Vector3 origin = terrain.transform.position, size = data.size;
            var maps = new float[Res, Res, layers.Length];
            var w = new float[layers.Length];
            for (int z = 0; z < Res; z++)
            {
                if (z % 32 == 0 && EditorUtility.DisplayCancelableProgressBar("Vẽ nền địa hình", $"{z}/{Res}", z / (float)Res)) { EditorUtility.ClearProgressBar(); return; }
                for (int x = 0; x < Res; x++)
                {
                    float wx = origin.x + (x + 0.5f) / Res * size.x, wz = origin.z + (z + 0.5f) / Res * size.z;
                    Weights(wx, wz, w);
                    for (int k = 0; k < w.Length; k++) maps[z, x, k] = w[k];
                }
            }
            EditorUtility.ClearProgressBar();
            data.SetAlphamaps(0, 0, maps);

            // finer terrain detail (fewer jagged facets), still no shadows from the ground itself
            terrain.heightmapPixelError = 3f;
            terrain.basemapDistance = 250f;
            terrain.drawInstanced = true;
            terrain.shadowCastingMode = ShadowCastingMode.Off;

            EditorUtility.SetDirty(data);
            EditorSceneManager.MarkAllScenesDirty();
            AssetDatabase.SaveAssets();
            Debug.Log("Đã vẽ lại nền địa hình: 9 lớp đất có chi tiết nổi, chuyển màu mượt theo khu, bờ nước và đường mòn. Bấm Ctrl+S để lưu scene.");
        }

        // ---------------------------------------------------------------- where each ground goes

        static float Smooth01(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        /// <summary>The weights of the nine layers at a world point (they add up to 1).</summary>
        static void Weights(float wx, float wz, float[] w)
        {
            var dens = PlantCommunities.DensityAll(wx, wz, 0.45f);   // soft fields: no hard patch edges
            float macro = ZoneLayout.Noise(wx / 70f + 40f, wz / 70f + 90f);                       // big soft colour drift
            float macro2 = ZoneLayout.Noise(wx / 23f + 7f, wz / 23f + 3f);
            float dw = ZoneLayout.DistanceToWater(wx, wz);
            float dp = ZoneLayout.DistanceToPath(wx, wz);

            float moss = 1.7f * (dens[(int)Plant.WetMoss] + dens[(int)Plant.ThickMoss] + dens[(int)Plant.Clover]) + 0.7f * dens[(int)Plant.SmallCream] + 0.5f * dens[(int)Plant.CreamMushroom];
            float leaf = 2.0f * dens[(int)Plant.LeafLitter] + 0.9f * dens[(int)Plant.EarFungus] + 0.4f * dens[(int)Plant.CreamMushroom];
            float lime = 2.2f * dens[(int)Plant.LimestoneRock];
            float tramp = 1.8f * dens[(int)Plant.BareMud] + 2.4f * dens[(int)Plant.TrackMark];
            float roots = 3.2f * (dens[(int)Plant.SurfaceRoot] + dens[(int)Plant.DarkRoot]) + 0.35f * ZoneLayout.Influence(1, wx, wz);
            float wet = Smooth01(1f - dw / 7f);
            float shore = Smooth01(1f - Mathf.Abs(dw - 3f) / 3f) * Smooth01((macro2 - 0.35f) / 0.3f);   // a band of pebbles, in patches
            float trail = Smooth01(1f - (dp - 1.4f) / 2.4f);

            float others = Mathf.Clamp01(moss + leaf + lime + tramp + roots);
            w[(int)Ground.DryEarth] = (0.9f - 0.45f * macro) * (1f - others) * (1f - wet);
            w[(int)Ground.WetMud] = (0.25f + 0.5f * macro) * (1f - others * 0.6f) * 0.7f + 1.6f * wet;
            w[(int)Ground.Moss] = moss * (1f - 0.5f * wet);
            w[(int)Ground.Leaf] = leaf;
            w[(int)Ground.Limestone] = lime;
            w[(int)Ground.Trampled] = tramp;
            w[(int)Ground.Pebbles] = 1.2f * shore + 0.25f * Smooth01((macro2 - 0.7f) / 0.2f);
            w[(int)Ground.RootBed] = roots;
            w[(int)Ground.Trail] = 2.4f * trail;
            float sum = 0f;
            for (int i = 0; i < w.Length; i++) { w[i] = Mathf.Max(0f, w[i]); sum += w[i]; }
            if (sum < 1e-4f) { w[(int)Ground.DryEarth] = 1f; return; }
            for (int i = 0; i < w.Length; i++) w[i] /= sum;
        }

        // ---------------------------------------------------------------- the textures

        static float N(float u, float v, int cells) => LakeWater.TileableNoise(u * cells, v * cells, cells, cells);

        static float Hash2(int x, int y, int seed)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263 + seed * 1274126177;
                h = (h ^ (h >> 13)) * 1274126177;
                h ^= h >> 16;
                return (h & 0xffff) / 65535f;
            }
        }

        /// <summary>Tileable cellular pattern: the id (0..1) of the nearest of jittered points and the distance to the cell edge (0 on it).</summary>
        static void Cells(float u, float v, int n, int seed, out float id, out float edge)
        {
            float x = u * n, y = v * n;
            int cx = Mathf.FloorToInt(x), cy = Mathf.FloorToInt(y);
            float best = 9f, second = 9f; id = 0f;
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    int gx = cx + dx, gy = cy + dy;
                    int mx = ((gx % n) + n) % n, my = ((gy % n) + n) % n;
                    float px = gx + Hash2(mx, my, seed) * 0.8f + 0.1f, py = gy + Hash2(mx, my, seed + 1) * 0.8f + 0.1f;
                    float d = Mathf.Sqrt((x - px) * (x - px) + (y - py) * (y - py));
                    if (d < best) { second = best; best = d; id = Hash2(mx, my, seed + 2); }
                    else if (d < second) second = d;
                }
            edge = Mathf.Clamp01((second - best) * 2.2f);
        }

        static Color DryEarth(float u, float v)
        {
            float big = N(u, v, 4), mid = N(u, v, 11), fine = N(u, v, 40);
            float tone = 0.62f + 0.3f * big + 0.18f * mid * (0.6f + 0.4f * fine);
            var c = Color.Lerp(new Color(0.34f, 0.24f, 0.16f), new Color(0.52f, 0.40f, 0.27f), Mathf.Clamp01(tone - 0.35f)) ;
            Cells(u, v, 22, 3, out float id, out float edge);
            if (id > 0.82f) c = Color.Lerp(c, new Color(0.55f, 0.52f, 0.47f) * (0.8f + 0.3f * id), 0.55f * edge);   // the odd pebble
            if (N(u, v, 60) > 0.78f) c *= 0.7f;                                                                    // dark specks
            return c;
        }

        static Color WetMud(float u, float v)
        {
            float big = N(u, v, 5), fine = N(u, v, 36);
            var c = Color.Lerp(new Color(0.13f, 0.09f, 0.07f), new Color(0.26f, 0.18f, 0.12f), 0.3f + 0.6f * big * (0.7f + 0.3f * fine));
            if (N(u, v, 9) > 0.72f) c = Color.Lerp(c, new Color(0.3f, 0.28f, 0.22f), 0.35f);                        // a sheen of silt
            return c;
        }

        static Color Moss(float u, float v)
        {
            float big = N(u, v, 6), clump = N(u, v, 28), fine = N(u, v, 70);
            var c = Color.Lerp(new Color(0.12f, 0.2f, 0.07f), new Color(0.4f, 0.5f, 0.18f), Mathf.Clamp01(0.2f + 0.5f * big + 0.4f * clump));
            c *= 0.85f + 0.3f * fine;
            if (N(u, v, 14) > 0.8f) c = Color.Lerp(c, new Color(0.5f, 0.55f, 0.22f), 0.4f);                         // lighter new growth
            return c;
        }

        static Color LeafLitter(float u, float v)
        {
            Cells(u, v, 14, 11, out float id, out float edge);
            var palette = new[] { new Color(0.52f, 0.28f, 0.1f), new Color(0.64f, 0.4f, 0.16f), new Color(0.4f, 0.22f, 0.1f), new Color(0.3f, 0.2f, 0.1f) };
            var leaf = Color.Lerp(palette[(int)(id * 3.99f)], palette[(int)(Hash2((int)(id * 977f), 5, 3) * 3.99f)], 0.4f);
            var under = new Color(0.18f, 0.12f, 0.08f);
            var c = Color.Lerp(under, leaf * (0.8f + 0.3f * N(u, v, 30)), Mathf.SmoothStep(0.05f, 0.35f, edge));
            float vein = Mathf.Abs(Mathf.Sin((u * 14f + id * 9f) * Mathf.PI * 2f));
            if (vein > 0.94f) c *= 0.75f;
            return c;
        }

        static Color Limestone(float u, float v)
        {
            Cells(u, v, 9, 21, out float id, out float edge);
            float big = N(u, v, 5), fine = N(u, v, 48);
            var c = new Color(0.66f, 0.63f, 0.55f) * (0.78f + 0.2f * big + 0.12f * fine + 0.1f * id);
            c = Color.Lerp(new Color(0.22f, 0.2f, 0.17f), c, Mathf.SmoothStep(0.02f, 0.25f, edge));                 // cracks
            if (N(u, v, 12) > 0.75f) c = Color.Lerp(c, new Color(0.3f, 0.4f, 0.2f), 0.4f);                          // moss in the hollows
            return c;
        }

        static Color Trampled(float u, float v)
        {
            float big = N(u, v, 5), fine = N(u, v, 38);
            var c = new Color(0.2f, 0.14f, 0.1f) * (0.7f + 0.35f * big + 0.18f * fine);
            Cells(u, v, 10, 31, out float id, out float edge);
            if (id > 0.6f) c *= Mathf.Lerp(0.8f, 1f, Mathf.SmoothStep(0.1f, 0.5f, edge));                          // hoof-sized dents
            return c;
        }

        static Color Pebbles(float u, float v)
        {
            Cells(u, v, 12, 41, out float id, out float edge);
            var stone = Color.Lerp(new Color(0.4f, 0.37f, 0.33f), new Color(0.62f, 0.58f, 0.52f), id) * (0.85f + 0.25f * N(u, v, 50));
            var mud = new Color(0.17f, 0.12f, 0.09f);
            return Color.Lerp(mud, stone, Mathf.SmoothStep(0.08f, 0.4f, edge));
        }

        static Color RootBed(float u, float v)
        {
            float big = N(u, v, 5), fine = N(u, v, 40);
            var c = new Color(0.2f, 0.14f, 0.09f) * (0.75f + 0.3f * big + 0.15f * fine);
            float warp = 0.5f * N(u, v, 6);
            float strand = Mathf.Abs(Mathf.Sin((u * 5f + warp) * Mathf.PI * 2f)) * Mathf.Abs(Mathf.Cos((v * 4f + warp * 0.7f) * Mathf.PI));
            if (strand > 0.86f) c = Color.Lerp(c, new Color(0.38f, 0.27f, 0.17f), 0.7f);                            // roots running through the earth
            return c;
        }

        static Color Trail(float u, float v)
        {
            float big = N(u, v, 4), fine = N(u, v, 44);
            var c = Color.Lerp(new Color(0.42f, 0.32f, 0.22f), new Color(0.56f, 0.45f, 0.32f), 0.3f + 0.5f * big) * (0.88f + 0.2f * fine);
            Cells(u, v, 18, 51, out float id, out float edge);
            if (id > 0.85f) c = Color.Lerp(c, new Color(0.6f, 0.58f, 0.52f), 0.5f * edge);                          // the odd pebble worn into the path
            return c;
        }

        // ---------------------------------------------------------------- terrain layers

        static TerrainLayer Layer(string name, float tile, System.Func<float, float, Color> paint)
        {
            string tex = $"{Root}/Models/LakeGround_{name}_v3.png", normal = $"{Root}/Models/LakeGround_{name}_v3_n.png";
            var diffuse = Tiling(tex, paint);
            var nrm = NormalMap(normal, paint);
            string path = $"{Root}/Terrain/Lake_{name}.terrainlayer";
            var layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
            if (layer == null)
            {
                layer = new TerrainLayer();
                AssetDatabase.CreateAsset(layer, path);
            }
            layer.diffuseTexture = diffuse;
            layer.normalMapTexture = nrm;
            layer.normalScale = 0.9f;
            layer.tileSize = new Vector2(tile, tile);
            layer.smoothness = name == "WetMud" ? 0.35f : 0.08f;
            layer.metallic = 0f;
            EditorUtility.SetDirty(layer);
            return layer;
        }

        static Color[] Paint(System.Func<float, float, Color> paint)
        {
            var px = new Color[TexSize * TexSize];
            for (int y = 0; y < TexSize; y++)
                for (int x = 0; x < TexSize; x++)
                {
                    var c = paint((x + 0.5f) / TexSize, (y + 0.5f) / TexSize);
                    c.a = 1f;
                    px[y * TexSize + x] = c;
                }
            return px;
        }

        /// <summary>A texture that repeats seamlessly (both directions), painted in code and saved as a PNG.</summary>
        static Texture2D Tiling(string path, System.Func<float, float, Color> paint)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null) return existing;
            Save(path, Paint(paint));
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.mipmapEnabled = true;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// <summary>The relief of a texture: a normal map made from its own brightness (blurred a little), seamless.</summary>
        static Texture2D NormalMap(string path, System.Func<float, float, Color> paint)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null) return existing;
            var px = Paint(paint);
            var h = new float[TexSize * TexSize];
            for (int i = 0; i < h.Length; i++) h[i] = px[i].r * 0.3f + px[i].g * 0.5f + px[i].b * 0.2f;
            var blur = new float[h.Length];
            for (int y = 0; y < TexSize; y++)
                for (int x = 0; x < TexSize; x++)
                {
                    float s = 0f;
                    for (int dy = -1; dy <= 1; dy++)
                        for (int dx = -1; dx <= 1; dx++) s += h[((y + dy + TexSize) % TexSize) * TexSize + (x + dx + TexSize) % TexSize];
                    blur[y * TexSize + x] = s / 9f;
                }
            var outPx = new Color[h.Length];
            const float strength = 5f;
            for (int y = 0; y < TexSize; y++)
                for (int x = 0; x < TexSize; x++)
                {
                    float l = blur[y * TexSize + (x + TexSize - 1) % TexSize], r = blur[y * TexSize + (x + 1) % TexSize];
                    float d = blur[((y + TexSize - 1) % TexSize) * TexSize + x], u = blur[((y + 1) % TexSize) * TexSize + x];
                    var n = new Vector3((l - r) * strength, (d - u) * strength, 1f).normalized;
                    outPx[y * TexSize + x] = new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f, 1f);
                }
            Save(path, outPx);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.NormalMap;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.mipmapEnabled = true;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static void Save(string path, Color[] pixels)
        {
            var t = new Texture2D(TexSize, TexSize, TextureFormat.RGBA32, false);
            t.SetPixels(pixels);
            File.WriteAllBytes(path, t.EncodeToPNG());
            Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(path);
        }
    }
}
