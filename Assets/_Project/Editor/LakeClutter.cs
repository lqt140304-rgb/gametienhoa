using System.Collections.Generic;
using System.IO;
using Evolve.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Evolve.EditorTools
{
    /// <summary>
    /// Evolve → Dress Lake: Ground Clutter. Covers the ground with thousands of little real 3D things so it is no longer
    /// a flat layer: grass tufts, tall reeds by the water, ferns, moss clumps, pebbles, twigs, fallen leaves, clusters of tiny
    /// mushrooms, small bushes and root nubs. They are Unity terrain "details" (GPU-instanced meshes that Unity draws only
    /// near the camera and culls by itself), so they cost very little however many there are. What grows where follows
    /// World/PlantCommunities (each area its own mix), the water (reeds and pebbles on the banks) and keeps off the trails,
    /// the hall, the houses and the water. Run after Dress Lake: Terrain. Run again to repaint.
    ///
    /// The small things (grass, reeds, pebbles, twigs, fallen leaves) are made here in code with real care: grass and leaves
    /// are crossed cards with a painted alpha-cut texture (blades dark at the root, bright at the tip; leaves with veins),
    /// pebbles are faceted rocks with cracks, twigs are knotted, grooved, broken-ended branches; several variants of each.
    /// The bigger things (fern, bush, mini mushrooms, root nub) are meant to be modelled by hand in Blender: put a model
    /// named Clutter_Fern / Clutter_Bush / Clutter_MiniMushrooms / Clutter_RootNub (.fbx, .glb or .prefab) in
    /// Assets/_Project/Models/Blender and it is used instead of the code version, which stays as the fallback.
    /// </summary>
    public static class LakeClutter
    {
        const string Root = "Assets/_Project";
        const string HandmadeFolder = Root + "/Models/Blender";
        const int Res = 512;   // detail map: about 1 m per cell over 500 m

        enum Kind { Grass, Reeds, Fern, MossClump, Pebbles, Twigs, Leaves, MiniMushrooms, Bush, RootNub }

        /// <summary>How many variants each kind has (each variant is its own terrain detail prototype).</summary>
        static int Variants(Kind k) => k switch { Kind.Grass => 4, Kind.Reeds => 2, Kind.Pebbles => 4, Kind.Twigs => 4, Kind.Leaves => 3, _ => 1 };

        struct Proto { public Kind Kind; public int Variant; }

        [MenuItem("Evolve/Bản đồ cũ (Underroot)/Dress Lake: Ground Clutter")]
        public static void Dress()
        {
            var terrain = Object.FindAnyObjectByType<Terrain>();
            if (terrain == null) { Debug.LogWarning("Không có Terrain trong scene. Bỏ qua bước phủ chi tiết mặt đất."); return; }
            PrototypeSceneBuilder.EnsureFolder(Root + "/Models");
            PrototypeSceneBuilder.EnsureFolder(Root + "/Models/Clutter");
            PrototypeSceneBuilder.EnsureFolder(Root + "/Prefabs");

            // ---- the prototypes (every variant of every kind) ----
            var protos = new List<DetailPrototype>();
            var index = new List<Proto>();
            var firstOf = new int[System.Enum.GetValues(typeof(Kind)).Length];
            foreach (Kind k in System.Enum.GetValues(typeof(Kind)))
            {
                firstOf[(int)k] = protos.Count;
                int n = Variants(k);
                for (int v = 0; v < n; v++)
                {
                    try { protos.Add(MakePrototype(k, v)); }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"[Clutter] Lỗi khi tạo mẫu {k} #{v}: {e.GetType().Name}: {e.Message}\n{e.StackTrace}");
                        throw;   // stops the step; the message above names the kind
                    }
                    index.Add(new Proto { Kind = k, Variant = v });
                }
            }
            var data = terrain.terrainData;
            data.SetDetailResolution(Res, 16);
            data.detailPrototypes = protos.ToArray();

            // ---- the density maps ----
            Vector3 origin = terrain.transform.position, size = data.size;
            var maps = new int[protos.Count][,];
            for (int i = 0; i < maps.Length; i++) maps[i] = new int[Res, Res];
            var houses = CityLayout.Houses();
            var classHouses = CityLayout.ClassHouses();
            var paths = CityLayout.HousePaths();
            Vector2 hall = CityLayout.HallCenter;
            for (int z = 0; z < Res; z++)
            {
                if (z % 32 == 0 && EditorUtility.DisplayCancelableProgressBar("Phủ chi tiết mặt đất", $"{z}/{Res}", z / (float)Res)) { EditorUtility.ClearProgressBar(); return; }
                for (int x = 0; x < Res; x++)
                {
                    float wx = origin.x + (x + 0.5f) / Res * size.x, wz = origin.z + (z + 0.5f) / Res * size.z;
                    if (Mathf.Max(Mathf.Abs(wx), Mathf.Abs(wz)) > 205f) continue;                       // not up the rim cliffs
                    float depth = ZoneLayout.CleanDepthAt(wx, wz);
                    if (depth > 0f) continue;                                                           // nothing under water
                    if (Blocked(wx, wz, hall, houses, classHouses, paths)) continue;
                    float dw = ZoneLayout.DistanceToWater(wx, wz), dp = ZoneLayout.DistanceToPath(wx, wz);
                    var d = PlantCommunities.DensityAll(wx, wz, 0.45f);
                    float onTrail = Mathf.Clamp01((dp - 1.4f) / 2.2f);                                  // clear in the trail's middle
                    float macro = ZoneLayout.Noise(wx / 17f + 5f, wz / 17f + 9f);
                    float macro2 = ZoneLayout.Noise(wx / 6f + 31f, wz / 6f + 12f);
                    float moss = d[(int)Plant.WetMoss] + d[(int)Plant.ThickMoss] + d[(int)Plant.Clover] + 0.5f * d[(int)Plant.SmallCream];
                    float leaf = d[(int)Plant.LeafLitter] + 0.5f * d[(int)Plant.EarFungus];
                    float roots = d[(int)Plant.SurfaceRoot] + d[(int)Plant.DarkRoot];
                    float bank = Mathf.Clamp01(1f - Mathf.Abs(dw - 3.2f) / 3.2f);                       // the water's margin
                    float wild = 0.25f + 0.6f * macro;

                    Put(maps, firstOf, Kind.Grass, x, z, (0.5f + 0.5f * onTrail) * ((0.55f + 0.45f * macro) * (1f - 0.3f * leaf) + 0.9f * moss) * 28f * (1f - 0.85f * GrassMath.Density(wx, wz)), 1);   // the 3D grass carpet (Dress Lake: Grass) takes over near the water
                    Put(maps, firstOf, Kind.Reeds, x, z, onTrail * bank * bank * (0.4f + macro2) * 10f, 2);
                    Put(maps, firstOf, Kind.Fern, x, z, onTrail * (0.8f * moss + 0.9f * roots + 0.15f * macro) * 3.5f, 3);
                    Put(maps, firstOf, Kind.MossClump, x, z, onTrail * (1.2f * moss + 0.2f * macro2) * 2.5f, 4);
                    Put(maps, firstOf, Kind.Pebbles, x, z, (0.35f * Mathf.Clamp01((macro2 - 0.5f) * 3f) + 1.1f * bank * macro2 + 0.5f * d[(int)Plant.LimestoneRock]) * 6f, 5);
                    Put(maps, firstOf, Kind.Twigs, x, z, onTrail * (0.3f * wild + 0.8f * leaf + 0.6f * roots) * 5f, 6);
                    Put(maps, firstOf, Kind.Leaves, x, z, (1.6f * leaf + 0.15f * macro) * 8f, 7);
                    Put(maps, firstOf, Kind.MiniMushrooms, x, z, onTrail * (1.4f * (d[(int)Plant.SmallCream] + d[(int)Plant.CreamMushroom]) + 0.5f * d[(int)Plant.EarFungus]) * 2.2f, 8);
                    Put(maps, firstOf, Kind.Bush, x, z, onTrail * (0.6f * moss + 0.3f * wild * macro) * 0.55f, 9);
                    Put(maps, firstOf, Kind.RootNub, x, z, onTrail * (1.3f * roots + 0.12f * wild) * 1.6f, 10);
                }
            }
            EditorUtility.ClearProgressBar();
            for (int i = 0; i < maps.Length; i++)
            {
                data.SetDetailLayer(0, 0, i, maps[i]);
                int filled = 0; foreach (int c in maps[i]) if (c > 0) filled++;
                Debug.Log($"[Clutter] {index[i].Kind} #{index[i].Variant}: {filled} ô có vật");
            }

            terrain.detailObjectDistance = 110f;      // drawn only this close to the camera
            terrain.detailObjectDensity = 1f;
            terrain.drawInstanced = true;
            EditorUtility.SetDirty(data);
            EditorSceneManager.MarkAllScenesDirty();
            AssetDatabase.SaveAssets();
            Debug.Log($"Đã phủ chi tiết 3D lên mặt đất ({protos.Count} mẫu: cỏ, lau sậy, cụm rêu, cuội, cành khô, lá rụng làm bằng code kỹ; dương xỉ, bụi cây, nấm nhỏ, mấu rễ dùng mô hình Blender nếu có trong {HandmadeFolder}). Bấm Ctrl+S để lưu scene.");
        }

        /// <summary>Count for a cell, split between the kind's variants; the fractional part comes from a hash of the cell (no banding).</summary>
        static void Put(int[][,] maps, int[] firstOf, Kind kind, int x, int z, float amount, int salt)
        {
            if (amount <= 0.02f) return;
            int n = Variants(kind);
            float each = amount / n;
            for (int v = 0; v < n; v++)
                maps[firstOf[(int)kind] + v][z, x] = Mathf.Clamp(Mathf.FloorToInt(each + Hash(x, z, salt * 11 + v)), 0, 16);
        }

        static float Hash(int x, int y, int s)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263 + s * 1274126177;
                h = (h ^ (h >> 13)) * 1274126177;
                h ^= h >> 16;
                return (h & 0xffff) / 65536f;
            }
        }

        /// <summary>Where nothing is scattered: inside the hall, over the tunnel, at the houses and their paving, beside the class houses.</summary>
        static bool Blocked(float x, float z, Vector2 hall, CityLayout.House[] houses, CityLayout.ClassHouse[] classHouses, List<Vector2[]> paths)
        {
            var p = new Vector2(x, z);
            if (ZoneLayout.InsideCave(x, z, 1.5f) || ZoneLayout.DistanceToLine(p, ZoneLayout.TunnelLine) < 3.5f) return true;
            if ((p - hall).sqrMagnitude > 90f * 90f) return false;   // the village is within 90 m of the hall
            foreach (var h in houses) if (Vector2.Distance(p, h.Position) < h.Radius + 1.6f) return true;
            foreach (var c in classHouses) if (Vector2.Distance(p, c.Position) < 5f) return true;
            foreach (var path in paths) if (ZoneLayout.DistanceToLine(p, path) < 1.4f) return true;
            float nx = (x - hall.x) / (ZoneLayout.CaveRadii.x + CityLayout.RingOffset), nz = (z - hall.y) / (ZoneLayout.CaveRadii.y + CityLayout.RingOffset);
            if (Mathf.Abs(Mathf.Sqrt(nx * nx + nz * nz) - 1f) * 26f < 1.8f) return true;   // the paved ring round the hall
            return false;
        }

        // ---------------------------------------------------------------- the prototypes

        static DetailPrototype MakePrototype(Kind kind, int variant)
        {
            string name = $"Clutter_{kind}{(Variants(kind) > 1 ? "_" + variant : "")}";
            GameObject prefab = LoadHandmade(kind);
            bool handmade = prefab != null;
            if (!handmade)
            {
                string meshPath = $"{Root}/Models/Clutter/{name}.asset", prefabPath = $"{Root}/Prefabs/{name}.prefab";
                Material[] mats;
                Mesh mesh = Build(kind, variant, out mats);
                AssetDatabase.DeleteAsset(meshPath);
                AssetDatabase.CreateAsset(mesh, meshPath);
                var go = new GameObject(name);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterials = mats;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
                Object.DestroyImmediate(go);
            }
            var proto = new DetailPrototype
            {
                prototype = prefab,
                usePrototypeMesh = true,
                useInstancing = true,
                renderMode = DetailRenderMode.VertexLit,
                alignToGround = kind == Kind.Leaves || kind == Kind.Pebbles ? 0.8f : 0.2f,
                positionJitter = 0.9f,
                holeEdgePadding = 0f,
                noiseSpread = 6f,
                useDensityScaling = false,
            };
            switch (kind)   // size ranges (a multiple of the model, built at its natural size)
            {
                case Kind.Grass: proto.minWidth = 1.1f; proto.maxWidth = 1.9f; proto.minHeight = 1.0f; proto.maxHeight = 2.2f; break;
                case Kind.Reeds: proto.minWidth = 0.8f; proto.maxWidth = 1.3f; proto.minHeight = 0.75f; proto.maxHeight = 1.4f; break;
                case Kind.Fern: proto.minWidth = 0.7f; proto.maxWidth = 1.5f; proto.minHeight = 0.7f; proto.maxHeight = 1.4f; break;
                case Kind.Bush: proto.minWidth = 0.7f; proto.maxWidth = 1.6f; proto.minHeight = 0.7f; proto.maxHeight = 1.6f; break;
                default: proto.minWidth = 1.2f; proto.maxWidth = 2.2f; proto.minHeight = 1.2f; proto.maxHeight = 2.2f; break;
            }
            return proto;
        }

        /// <summary>A hand-made model (Blender) for the big things: Clutter_Fern, Clutter_Bush, Clutter_MiniMushrooms, Clutter_RootNub, if one is in the folder.</summary>
        static GameObject LoadHandmade(Kind kind)
        {
            if (kind != Kind.Fern && kind != Kind.Bush && kind != Kind.MiniMushrooms && kind != Kind.RootNub) return null;
            foreach (string ext in new[] { ".prefab", ".fbx", ".glb", ".gltf", ".blend" })
            {
                var g = AssetDatabase.LoadAssetAtPath<GameObject>($"{HandmadeFolder}/Clutter_{kind}{ext}");
                if (g != null) return g;
            }
            return null;
        }

        // ---------------------------------------------------------------- materials and textures

        static Material Mat(string name, Color color, Color emission = default)
        {
            var m = LakeGround.OpaqueMaterial("Clutter_" + name, color, 0.08f, true, emission);
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>A double-sided alpha-cut material from a painted texture (blades, leaves), instanced.</summary>
        static Material Cutout(string name, Texture2D tex)
        {
            // the cave is dark: a little glow keeps blades and leaves from sinking into the soil
            var m = LakeGround.OpaqueMaterial("Clutter_" + name, new Color(1.5f, 1.5f, 1.5f), 0.05f, true, new Color(0.05f, 0.07f, 0.03f));
            m.SetTexture("_BaseMap", tex);
            m.SetFloat("_AlphaClip", 1f);
            m.SetFloat("_Cutoff", 0.45f);
            m.EnableKeyword("_ALPHATEST_ON");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            return m;
        }

        static Material Textured(string name, Texture2D tex)
        {
            var m = LakeGround.OpaqueMaterial("Clutter_" + name, Color.white, 0.08f, false);
            m.SetTexture("_BaseMap", tex);
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            return m;
        }

        static Texture2D SaveTexture(string name, int w, int h, Color[] pixels, bool alpha)
        {
            string path = $"{Root}/Models/Clutter/{name}.png";
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            t.SetPixels(pixels);
            File.WriteAllBytes(path, t.EncodeToPNG());
            Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.alphaIsTransparency = alpha;
            importer.mipMapsPreserveCoverage = alpha;   // keeps the cut-out blades from thinning out with distance
            importer.wrapMode = alpha ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
            importer.mipmapEnabled = true;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static float Nz(float u, float v, int cells) => LakeWater.TileableNoise(u * cells, v * cells, cells, cells);

        static Color Mix(Color a, Color b, float t) => Color.Lerp(a, b, Mathf.Clamp01(t));

        /// <summary>
        /// A card of grass or reed blades painted into a texture: several tapering, leaning blades, dark and a little yellowish
        /// at the root, bright at the tip, with a darker midrib; everything outside the blades has alpha 0.
        /// </summary>
        static Texture2D BladesTexture(string name, int seed, int blades, float maxWidth, float lean, Color root, Color mid, Color tip)
        {
            const int W = 128, H = 256;
            var rnd = new System.Random(seed);
            var bx = new float[blades]; var bh = new float[blades]; var bw = new float[blades]; var bb = new float[blades];
            for (int i = 0; i < blades; i++)
            {
                bx[i] = 0.12f + 0.76f * (i + (float)rnd.NextDouble() * 0.7f) / blades;
                bh[i] = 0.55f + 0.45f * (float)rnd.NextDouble();
                bw[i] = maxWidth * (0.6f + 0.6f * (float)rnd.NextDouble());
                bb[i] = lean * ((float)rnd.NextDouble() * 2f - 1f);
            }
            var px = new Color[W * H];
            for (int y = 0; y < H; y++)
            {
                float v = y / (float)(H - 1);
                for (int x = 0; x < W; x++)
                {
                    float u = x / (float)(W - 1);
                    float cover = 0f, shade = 0f, t = 0f;
                    for (int i = 0; i < blades; i++)
                    {
                        if (v > bh[i]) continue;
                        float f = v / bh[i];
                        float cx = bx[i] + bb[i] * f * f;
                        float half = bw[i] * Mathf.Pow(1f - f, 0.75f) * 0.5f + 0.002f;
                        float d = Mathf.Abs(u - cx);
                        if (d < half)
                        {
                            cover = 1f; t = f;
                            shade = 1f - 0.35f * Mathf.Abs(d) / half - (d < half * 0.12f ? 0.12f : 0f);   // the midrib and the darker edges
                        }
                    }
                    if (cover < 0.5f) { px[y * W + x] = new Color(root.r, root.g, root.b, 0f); continue; }
                    var c = t < 0.5f ? Mix(root, mid, t * 2f) : Mix(mid, tip, (t - 0.5f) * 2f);
                    c *= shade * (0.9f + 0.2f * Nz(u, v, 24));
                    c.a = 1f;
                    px[y * W + x] = c;
                }
            }
            return SaveTexture(name, W, H, px, true);
        }

        /// <summary>A sheet of four fallen leaves (2 x 2), each with veins, a pointed tip and a slightly ragged edge.</summary>
        static Texture2D LeavesTexture(string name, int seed, Color[] colours)
        {
            const int S = 128;
            var px = new Color[S * 2 * S * 2];
            var rnd = new System.Random(seed);
            for (int cell = 0; cell < 4; cell++)
            {
                int ox = (cell % 2) * S, oy = (cell / 2) * S;
                float angle = (float)rnd.NextDouble() * 0.5f - 0.25f, ratio = 0.38f + 0.12f * (float)rnd.NextDouble();
                var baseColour = colours[cell % colours.Length];
                for (int y = 0; y < S; y++)
                    for (int x = 0; x < S; x++)
                    {
                        float u = (x / (float)(S - 1)) * 2f - 1f, v = (y / (float)(S - 1)) * 2f - 1f;
                        // rotate a little, then test against a pointed oval
                        float ru = u * Mathf.Cos(angle) - v * Mathf.Sin(angle), rv = u * Mathf.Sin(angle) + v * Mathf.Cos(angle);
                        float t = (rv + 0.9f) / 1.8f;                                           // 0 at the stem, 1 at the tip
                        float half = ratio * Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI) * (1f - 0.25f * t) + 0.004f;
                        float ragged = 0.03f * Mathf.Sin(rv * 31f + cell) + 0.02f * Mathf.Sin(rv * 57f);
                        bool inside = t > 0f && t < 1f && Mathf.Abs(ru) < half + ragged;
                        var c = new Color(baseColour.r, baseColour.g, baseColour.b, 0f);
                        if (inside)
                        {
                            float vein = Mathf.Abs(ru) < 0.012f ? 0.55f : (Mathf.Abs(Mathf.Abs(ru) - 0.18f * (1f - t) - 0.02f * Mathf.Sin(rv * 20f)) < 0.01f && t > 0.15f ? 0.8f : 1f);
                            float mottle = 0.82f + 0.3f * Nz(u * 0.5f + 0.5f + cell, v * 0.5f + 0.5f, 7);
                            c = baseColour * vein * mottle * (0.85f + 0.25f * t);
                            c.a = 1f;
                        }
                        px[(oy + y) * (S * 2) + (ox + x)] = c;
                    }
            }
            return SaveTexture(name, S * 2, S * 2, px, true);
        }

        static Texture2D StoneTexture(string name, int seed)
        {
            const int S = 128;
            var px = new Color[S * S];
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float u = x / (float)S, v = y / (float)S;
                    float big = Nz(u + seed * 0.13f, v, 4), mid = Nz(u, v + seed * 0.07f, 12), fine = Nz(u, v, 40);
                    var c = Mix(new Color(0.36f, 0.33f, 0.3f), new Color(0.62f, 0.58f, 0.52f), 0.2f + 0.6f * big) * (0.85f + 0.25f * mid * (0.6f + 0.4f * fine));
                    float crack = Mathf.Abs(Mathf.Sin((u * 3f + 0.9f * Nz(u, v, 5) + seed) * Mathf.PI * 2f) * Mathf.Sin((v * 2f + 0.7f * Nz(v, u, 6)) * Mathf.PI * 2f));
                    if (crack < 0.035f) c *= 0.55f;
                    if (Nz(u + 3f, v, 9) > 0.78f) c = Mix(c, new Color(0.3f, 0.4f, 0.2f), 0.45f);   // a patch of moss
                    c.a = 1f;
                    px[y * S + x] = c;
                }
            return SaveTexture(name, S, S, px, false);
        }

        static Texture2D BarkTexture(string name, int seed)
        {
            const int W = 64, H = 128;
            var px = new Color[W * H];
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float u = x / (float)W, v = y / (float)H;
                    float groove = 0.5f + 0.5f * Mathf.Sin((u * 9f + 0.9f * Nz(u, v, 3) + seed * 0.3f) * Mathf.PI * 2f);
                    float fine = Nz(u, v, 20);
                    var c = Mix(new Color(0.2f, 0.14f, 0.09f), new Color(0.46f, 0.34f, 0.23f), 0.15f + 0.55f * groove * (0.6f + 0.4f * fine));
                    if (Nz(u + 7f, v, 5) > 0.8f) c = Mix(c, new Color(0.55f, 0.6f, 0.42f), 0.5f);   // lichen
                    c.a = 1f;
                    px[y * W + x] = c;
                }
            return SaveTexture(name, W, H, px, false);
        }

        // ---------------------------------------------------------------- the meshes

        static Mesh Build(Kind kind, int variant, out Material[] mats)
        {
            var rnd = new System.Random((int)kind * 7919 + variant * 101 + 13);
            Mesh mesh;
            switch (kind)
            {
                case Kind.Grass:
                {
                    var palette = new[] { (new Color(0.2f, 0.34f, 0.1f), new Color(0.46f, 0.72f, 0.22f), new Color(0.78f, 0.92f, 0.4f)),
                                          (new Color(0.22f, 0.32f, 0.1f), new Color(0.52f, 0.7f, 0.22f), new Color(0.88f, 0.92f, 0.42f)),
                                          (new Color(0.3f, 0.28f, 0.1f), new Color(0.7f, 0.66f, 0.24f), new Color(0.95f, 0.86f, 0.44f)),
                                          (new Color(0.16f, 0.3f, 0.12f), new Color(0.36f, 0.62f, 0.24f), new Color(0.66f, 0.86f, 0.44f)) };
                    var pal = palette[variant % palette.Length];
                    var tex = BladesTexture($"Grass_{variant}", 40 + variant, 7 + variant, 0.075f, 0.25f, pal.Item1, pal.Item2, pal.Item3);
                    mesh = Cards(rnd, 4 + variant % 2, 0.5f + 0.08f * variant, 0.42f + 0.05f * variant, 0.12f, 0.2f);
                    mats = new[] { Cutout($"Grass_{variant}", tex) };
                    break;
                }
                case Kind.Reeds:
                {
                    var tex = BladesTexture($"Reed_{variant}", 70 + variant, 5, 0.05f, 0.14f, new Color(0.2f, 0.22f, 0.08f), new Color(0.5f, 0.54f, 0.22f), variant == 0 ? new Color(0.78f, 0.7f, 0.36f) : new Color(0.62f, 0.62f, 0.3f));
                    mesh = Cards(rnd, 5, 0.45f, 1.15f, 0.1f, 0.1f);
                    mats = new[] { Cutout($"Reed_{variant}", tex) };
                    break;
                }
                case Kind.Fern:
                    mesh = Fronds(9, 0.7f, 0.16f, 0.5f);
                    mats = new[] { Mat("Fern", new Color(0.24f, 0.42f, 0.16f)) };
                    break;
                case Kind.MossClump:
                {
                    var parts = new List<CombineInstance>();
                    var blob = FloraMeshes.Blob();
                    for (int i = 0; i < 4; i++)
                        parts.Add(new CombineInstance { mesh = blob, transform = Matrix4x4.TRS(new Vector3(((float)rnd.NextDouble() - 0.5f) * 0.3f, 0.02f, ((float)rnd.NextDouble() - 0.5f) * 0.3f), Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f), new Vector3(0.28f + 0.1f * (float)rnd.NextDouble(), 0.12f, 0.24f)) });
                    mesh = Combine(parts);
                    mats = new[] { Mat("MossClump", new Color(0.26f, 0.4f, 0.13f)) };
                    break;
                }
                case Kind.Pebbles:
                    mesh = PebbleCluster(rnd, 4 + variant % 3);
                    mats = new[] { Textured($"Pebbles_{variant}", StoneTexture($"Stone_{variant}", variant)) };
                    break;
                case Kind.Twigs:
                    mesh = TwigCluster(rnd, variant);
                    mats = new[] { Textured($"Twigs_{variant}", BarkTexture($"Bark_{variant}", variant)) };
                    break;
                case Kind.Leaves:
                {
                    var palettes = new[]
                    {
                        new[] { new Color(0.62f, 0.34f, 0.12f), new Color(0.5f, 0.27f, 0.1f), new Color(0.7f, 0.46f, 0.16f), new Color(0.36f, 0.22f, 0.1f) },
                        new[] { new Color(0.72f, 0.52f, 0.16f), new Color(0.6f, 0.4f, 0.13f), new Color(0.46f, 0.3f, 0.1f), new Color(0.58f, 0.3f, 0.1f) },
                        new[] { new Color(0.4f, 0.24f, 0.1f), new Color(0.3f, 0.2f, 0.09f), new Color(0.52f, 0.3f, 0.12f), new Color(0.34f, 0.3f, 0.12f) },
                    };
                    var tex = LeavesTexture($"Leaves_{variant}", 90 + variant, palettes[variant % palettes.Length]);
                    mesh = LeafHeap(rnd, 10, 0.2f);
                    mats = new[] { Cutout($"Leaves_{variant}", tex) };
                    break;
                }
                case Kind.MiniMushrooms:
                {
                    var stalks = new List<CombineInstance>();
                    var caps = new List<CombineInstance>();
                    for (int i = 0; i < 4; i++)
                    {
                        FloraMeshes.Mushroom(500 + i, 0.14f + 0.12f * (float)rnd.NextDouble(), out var st, out var cp, out _);
                        var m = Matrix4x4.Translate(new Vector3(((float)rnd.NextDouble() - 0.5f) * 0.3f, 0f, ((float)rnd.NextDouble() - 0.5f) * 0.3f));
                        stalks.Add(new CombineInstance { mesh = st, transform = m });
                        caps.Add(new CombineInstance { mesh = cp, transform = m });
                    }
                    var two = new List<CombineInstance> { new CombineInstance { mesh = Combine(stalks), transform = Matrix4x4.identity }, new CombineInstance { mesh = Combine(caps), transform = Matrix4x4.identity } };
                    mesh = new Mesh { name = "MiniMushrooms" };
                    mesh.CombineMeshes(two.ToArray(), false, false);   // two sub-meshes: stalks, caps
                    mesh.RecalculateBounds();
                    mats = new[] { Mat("MushroomStalk", new Color(0.93f, 0.89f, 0.78f)), Mat("MushroomCap", new Color(0.9f, 0.84f, 0.66f), new Color(0.07f, 0.06f, 0.04f)) };
                    break;
                }
                case Kind.Bush:
                {
                    var parts = new List<CombineInstance>();
                    var blob = FloraMeshes.Blob();
                    for (int i = 0; i < 6; i++)
                    {
                        float a = i / 6f * Mathf.PI * 2f + (float)rnd.NextDouble();
                        float r = 0.25f + 0.2f * (float)rnd.NextDouble(), sz = 0.28f + 0.14f * (float)rnd.NextDouble();
                        parts.Add(new CombineInstance { mesh = blob, transform = Matrix4x4.TRS(new Vector3(Mathf.Cos(a) * r, 0.28f + 0.25f * (float)rnd.NextDouble(), Mathf.Sin(a) * r), Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f), new Vector3(sz * 1.3f, sz, sz * 1.2f)) });
                    }
                    parts.Add(new CombineInstance { mesh = blob, transform = Matrix4x4.TRS(new Vector3(0f, 0.55f, 0f), Quaternion.identity, new Vector3(0.5f, 0.38f, 0.5f)) });
                    mesh = Combine(parts);
                    mats = new[] { Mat("Bush", new Color(0.28f, 0.42f, 0.16f)) };
                    break;
                }
                default:   // RootNub: a short arch of root coming out of the ground
                {
                    var path = RootMeshes.ArchPath(new Vector3(-0.35f, 0f, 0f), new Vector3(0.35f, 0f, 0.05f), 0.28f, 600, 0.4f, 8, 0.1f);
                    mesh = RootMeshes.RootTube(path, 0.07f, 0.045f, 601, 0.3f, 6);
                    mats = new[] { Mat("RootNub", new Color(0.3f, 0.21f, 0.13f)) };
                    break;
                }
            }
            mesh.name = $"Clutter_{kind}_{variant}";
            return mesh;
        }

        static Mesh Combine(List<CombineInstance> parts)
        {
            var mesh = new Mesh();
            mesh.CombineMeshes(parts.ToArray(), true, true);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// Crossed cards for grass and reeds: each card a column of quads that bends sideways toward its tip, turned a random way,
        /// standing in a small tuft. UV v runs up the card (0 at the root), u across it.
        /// </summary>
        static Mesh Cards(System.Random rnd, int cards, float width, float height, float spread, float bend)
        {
            var verts = new List<Vector3>(); var uvs = new List<Vector2>(); var tris = new List<int>();
            const int rows = 4;
            for (int c = 0; c < cards; c++)
            {
                float yaw = (c / (float)cards + 0.12f * (float)rnd.NextDouble()) * Mathf.PI;
                Vector3 across = new Vector3(Mathf.Cos(yaw), 0f, Mathf.Sin(yaw)), normal = new Vector3(-across.z, 0f, across.x);
                Vector3 offset = new Vector3(((float)rnd.NextDouble() - 0.5f) * spread, 0f, ((float)rnd.NextDouble() - 0.5f) * spread);
                float w = width * (0.8f + 0.4f * (float)rnd.NextDouble()), h = height * (0.8f + 0.4f * (float)rnd.NextDouble());
                float bendSign = (float)rnd.NextDouble() < 0.5f ? -1f : 1f, b = bend * (0.4f + 0.8f * (float)rnd.NextDouble()) * bendSign;
                int start = verts.Count;
                for (int r = 0; r <= rows; r++)
                {
                    float t = r / (float)rows;
                    Vector3 centre = offset + Vector3.up * (t * h) + normal * (b * t * t * h);
                    verts.Add(centre - across * w * 0.5f); verts.Add(centre + across * w * 0.5f);
                    uvs.Add(new Vector2(0f, t)); uvs.Add(new Vector2(1f, t));
                }
                for (int r = 0; r < rows; r++)
                {
                    int i = start + r * 2;
                    tris.Add(i); tris.Add(i + 2); tris.Add(i + 1);
                    tris.Add(i + 1); tris.Add(i + 2); tris.Add(i + 3);
                }
            }
            var mesh = new Mesh();
            mesh.SetVertices(verts); mesh.SetUVs(0, uvs); mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            // the card normals are all made to point up a little, so the tuft is lit like soft foliage, not like flat paper
            var normals = mesh.normals;
            for (int i = 0; i < normals.Length; i++) normals[i] = (normals[i] * 0.4f + Vector3.up * 0.9f).normalized;
            mesh.normals = normals;
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>A heap of fallen leaves: cards lying nearly flat, tilted and curled, in a little scatter, UVs into the 2 x 2 leaf sheet.</summary>
        static Mesh LeafHeap(System.Random rnd, int count, float size)
        {
            var verts = new List<Vector3>(); var uvs = new List<Vector2>(); var tris = new List<int>();
            for (int i = 0; i < count; i++)
            {
                float yaw = (float)rnd.NextDouble() * Mathf.PI * 2f, tilt = ((float)rnd.NextDouble() - 0.5f) * 0.5f, curl = 0.25f * ((float)rnd.NextDouble() - 0.3f);
                Vector3 centre = new Vector3(((float)rnd.NextDouble() - 0.5f) * 0.7f, 0.012f + 0.02f * (i % 4) + 0.01f * (float)rnd.NextDouble(), ((float)rnd.NextDouble() - 0.5f) * 0.7f);
                Vector3 along = new Vector3(Mathf.Cos(yaw), 0f, Mathf.Sin(yaw)), across = new Vector3(-along.z, 0f, along.x);
                float len = size * (0.75f + 0.5f * (float)rnd.NextDouble());
                int cell = rnd.Next(4);
                float u0 = (cell % 2) * 0.5f, v0 = (cell / 2) * 0.5f;
                int start = verts.Count;
                for (int r = 0; r <= 2; r++)
                    for (int c = 0; c <= 2; c++)
                    {
                        float a = (c - 1) * 0.5f, l = (r - 1) * 0.5f;                          // -0.5, 0, 0.5
                        Vector3 p = centre + along * l * len * 1.8f + across * a * len * 1.8f;
                        p.y += tilt * l * len + curl * a * a * len * 4f;                       // the edges curl up
                        verts.Add(p);
                        uvs.Add(new Vector2(u0 + 0.5f * (c / 2f), v0 + 0.5f * (r / 2f)));
                    }
                for (int r = 0; r < 2; r++)
                    for (int c = 0; c < 2; c++)
                    {
                        int k = start + r * 3 + c;
                        tris.Add(k); tris.Add(k + 3); tris.Add(k + 1);
                        tris.Add(k + 1); tris.Add(k + 3); tris.Add(k + 4);
                    }
            }
            var mesh = new Mesh();
            mesh.SetVertices(verts); mesh.SetUVs(0, uvs); mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>A small cluster of faceted pebbles: each a low-poly icosphere pulled about at random and flattened, flat-shaded, planar UVs.</summary>
        static Mesh PebbleCluster(System.Random rnd, int count)
        {
            var verts = new List<Vector3>(); var uvs = new List<Vector2>(); var tris = new List<int>();
            for (int p = 0; p < count; p++)
            {
                float s = 0.045f + 0.075f * (float)rnd.NextDouble() * (p == 0 ? 1.4f : 1f);
                Vector3 centre = p == 0 ? Vector3.zero : new Vector3(((float)rnd.NextDouble() - 0.5f) * 0.55f, 0f, ((float)rnd.NextDouble() - 0.5f) * 0.55f);
                AddPebble(rnd, verts, uvs, tris, centre, s);
            }
            var mesh = new Mesh();
            mesh.SetVertices(verts); mesh.SetUVs(0, uvs); mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        static void AddPebble(System.Random rnd, List<Vector3> verts, List<Vector2> uvs, List<int> tris, Vector3 centre, float size)
        {
            // an icosahedron (12 points, 20 faces), each point pushed in or out, the whole thing flattened and stretched
            float t = (1f + Mathf.Sqrt(5f)) * 0.5f;
            var p = new[]
            {
                new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0),
                new Vector3(0, -1, t), new Vector3(0, 1, t), new Vector3(0, -1, -t), new Vector3(0, 1, -t),
                new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1)
            };
            var scale = new Vector3(1f + 0.5f * (float)rnd.NextDouble(), 0.55f + 0.25f * (float)rnd.NextDouble(), 1f + 0.4f * (float)rnd.NextDouble());
            float turn = (float)rnd.NextDouble() * 360f;
            for (int i = 0; i < p.Length; i++)
            {
                var v = p[i].normalized * (0.75f + 0.45f * (float)rnd.NextDouble());
                v = Vector3.Scale(v, scale);
                p[i] = Quaternion.Euler(0f, turn, 0f) * v * size + centre + Vector3.up * size * 0.3f;
            }
            int[] f =
            {
                0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11, 1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
                3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9, 4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1
            };
            for (int i = 0; i < f.Length; i += 3)
            {
                int b = verts.Count;
                for (int k = 0; k < 3; k++)
                {
                    var v = p[f[i + k]];
                    verts.Add(v);
                    uvs.Add(new Vector2((v.x - centre.x) / size * 0.35f + 0.5f + centre.x, (v.z - centre.z) / size * 0.35f + 0.5f + centre.z));   // planar from above
                }
                tris.Add(b); tris.Add(b + 1); tris.Add(b + 2);
            }
        }

        /// <summary>
        /// A handful of dead twigs: each a bark-textured, tapering, knotted branch lying bent on the ground with a side twig or two
        /// and a snapped (flat) end. UV u runs round the branch, v along it.
        /// </summary>
        static Mesh TwigCluster(System.Random rnd, int variant)
        {
            var verts = new List<Vector3>(); var uvs = new List<Vector2>(); var tris = new List<int>();
            int main = 2 + variant % 2;
            for (int i = 0; i < main; i++)
            {
                float a = (float)rnd.NextDouble() * Mathf.PI, len = 0.5f + 0.45f * (float)rnd.NextDouble();
                Vector3 dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)), side = new Vector3(-dir.z, 0f, dir.x);
                Vector3 c = new Vector3(((float)rnd.NextDouble() - 0.5f) * 0.3f, 0.025f + 0.02f * i, ((float)rnd.NextDouble() - 0.5f) * 0.3f);
                int pts = 7;
                var path = new Vector3[pts]; var radii = new float[pts];
                float bendA = ((float)rnd.NextDouble() - 0.5f) * 0.35f, bendB = ((float)rnd.NextDouble() - 0.5f) * 0.12f;
                for (int k = 0; k < pts; k++)
                {
                    float t = k / (float)(pts - 1);
                    path[k] = c + dir * (t - 0.5f) * len + side * (bendA * Mathf.Sin(t * Mathf.PI) * len) + Vector3.up * (bendB * Mathf.Sin(t * Mathf.PI * 2f) * len);
                    float knot = 1f + 0.35f * (k == 2 || k == 4 ? 1f : 0f) * (float)rnd.NextDouble();
                    radii[k] = Mathf.Lerp(0.024f, 0.011f, t) * knot;
                }
                AddBranch(verts, uvs, tris, path, radii, 6);
                // a side twig from the middle
                Vector3 from = path[3];
                var sp = new[] { from, from + side * 0.08f + dir * 0.06f + Vector3.up * 0.03f, from + side * 0.17f + dir * 0.1f + Vector3.up * 0.015f };
                AddBranch(verts, uvs, tris, sp, new[] { 0.011f, 0.008f, 0.004f }, 5);
            }
            var mesh = new Mesh();
            mesh.SetVertices(verts); mesh.SetUVs(0, uvs); mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        static void AddBranch(List<Vector3> verts, List<Vector2> uvs, List<int> tris, Vector3[] path, float[] radii, int sides)
        {
            int start = verts.Count;
            float along = 0f;
            for (int k = 0; k < path.Length; k++)
            {
                Vector3 dir = (path[Mathf.Min(k + 1, path.Length - 1)] - path[Mathf.Max(k - 1, 0)]).normalized;
                Vector3 n1 = Vector3.Cross(dir, Vector3.up); if (n1.sqrMagnitude < 1e-4f) n1 = Vector3.Cross(dir, Vector3.right);
                n1.Normalize();
                Vector3 n2 = Vector3.Cross(dir, n1);
                if (k > 0) along += Vector3.Distance(path[k], path[k - 1]);
                for (int s = 0; s <= sides; s++)
                {
                    float a = s / (float)sides * Mathf.PI * 2f;
                    verts.Add(path[k] + (n1 * Mathf.Cos(a) + n2 * Mathf.Sin(a)) * radii[k]);
                    uvs.Add(new Vector2(s / (float)sides, along * 6f));
                }
            }
            int row = sides + 1;
            for (int k = 0; k + 1 < path.Length; k++)
                for (int s = 0; s < sides; s++)
                {
                    int i = start + k * row + s;
                    tris.Add(i); tris.Add(i + row); tris.Add(i + 1);
                    tris.Add(i + 1); tris.Add(i + row); tris.Add(i + row + 1);
                }
            // the snapped end: a flat cap at the thick end
            int centre = verts.Count;
            verts.Add(path[0]); uvs.Add(new Vector2(0.5f, 0f));
            for (int s = 0; s < sides; s++) { tris.Add(centre); tris.Add(start + s + 1); tris.Add(start + s); }
        }

        /// <summary>A fan of arching fronds (double-sided strips tapering to a point), for ferns.</summary>
        static Mesh Fronds(int count, float length, float width, float droop)
        {
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            const int seg = 6;
            for (int f = 0; f < count; f++)
            {
                float a = f / (float)count * Mathf.PI * 2f + 0.3f * (f % 2);
                Vector3 dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)), side = new Vector3(-dir.z, 0f, dir.x);
                float len = length * (0.8f + 0.35f * ((f * 37) % 10) / 10f);
                int b = verts.Count;
                for (int s = 0; s <= seg; s++)
                {
                    float t = s / (float)seg;
                    Vector3 c = dir * len * t * 0.8f + Vector3.up * (len * (0.55f * Mathf.Sin(t * Mathf.PI * 0.6f)) - droop * len * t * t * 0.5f) + Vector3.up * 0.02f;
                    float w = width * Mathf.Sin(Mathf.Clamp01(t * 1.15f) * Mathf.PI) * (1f - t * 0.4f) + 0.004f;
                    verts.Add(c + side * w); verts.Add(c - side * w);
                    uvs.Add(new Vector2(0f, t)); uvs.Add(new Vector2(1f, t));
                }
                for (int s = 0; s < seg; s++)
                {
                    int i = b + s * 2;
                    tris.Add(i); tris.Add(i + 2); tris.Add(i + 1);
                    tris.Add(i + 1); tris.Add(i + 2); tris.Add(i + 3);
                }
            }
            var mesh = new Mesh { name = "Fronds" };
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
