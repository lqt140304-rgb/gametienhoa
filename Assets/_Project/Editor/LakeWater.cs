using System.Collections.Generic;
using System.IO;
using Evolve.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Evolve.EditorTools
{
    /// <summary>
    /// Dresses the lake with proper water. Menu: Evolve -> Dress Lake: Water.
    /// It finds the water that is already there (every WaterZone, the life pool, the deep pockets), hides the flat
    /// grey-blue boxes and puts a moving, rippling, turquoise surface in their place (WaterSurface). It never changes
    /// colliders, triggers or positions, and it is safe to run again (it replaces its own surfaces).
    /// It reads the scene, not the terrain code, so it keeps working when the zone gets bigger.
    /// </summary>
    public static class LakeWater
    {
        const string Root = "Assets/_Project";
        const string DressedName = "Water Surface (Dressed)";
        const string NormalPath = Root + "/Models/WaterRipples_v2.png";

        [MenuItem("Evolve/Dress Lake: Water")]
        public static void Dress()
        {
            EnsureFolders();
            var normal = RippleNormalMap();
            var clean = WaterMaterial("Lake_WaterClean", new Color(0.25f, 0.84f, 0.82f, 0.5f), normal, 0.3f, new Color(0.02f, 0.10f, 0.10f));
            var dirty = WaterMaterial("Lake_WaterDirty", new Color(0.38f, 0.44f, 0.24f, 0.78f), normal, 0.18f, Color.black);
            var life = WaterMaterial("Lake_WaterLife", new Color(0.50f, 0.48f, 0.24f, 0.66f), normal, 0.16f, new Color(0.03f, 0.03f, 0.0f));   // the Life Shallows: warm, olive, murky
            var deep = WaterMaterial("Lake_WaterDeep", new Color(0.04f, 0.22f, 0.34f, 0.88f), normal, 0.22f, new Color(0.0f, 0.03f, 0.05f));

            foamMaterial = FoamMaterial();
            bankMaterial = BankMaterial();
            seedCounter = 0;

            // Take back anything a previous run made, so running the menu twice never stacks surfaces.
            foreach (var old in Object.FindObjectsByType<WaterSurface>())
                if (old.name == DressedName) Object.DestroyImmediate(old.gameObject);

            int made = 0;

            // 1) Every shallow water zone: a surface as big as its trigger box, at the water level.
            foreach (var zone in Object.FindObjectsByType<WaterZone>())
            {
                var box = zone.GetComponent<BoxCollider>();
                if (box == null) continue;
                Bounds b = box.bounds;
                HideFlatVisuals(zone.transform);
                float longSide = Mathf.Max(b.size.x, b.size.z), shortSide = Mathf.Min(b.size.x, b.size.z);
                if (longSide >= 20f && longSide >= shortSide * 4f)
                {
                    // A long thin zone is a stream: it winds like a real one (the trigger box stays straight).
                    bool alongX = b.size.x >= b.size.z;
                    Vector2 a = alongX ? new Vector2(b.min.x, b.center.z) : new Vector2(b.center.x, b.min.z);
                    Vector2 e = alongX ? new Vector2(b.max.x, b.center.z) : new Vector2(b.center.x, b.max.z);
                    float halfWidth = Mathf.Clamp(shortSide * 0.5f, 2f, 3.5f);
                    var world = GroundMeshes.WindingLine(a, e, 900 + made, 4f, 2f, 50f);   // very long, slow bends so the two banks never cross
                    var centre = new Vector2(b.center.x, b.center.z);
                    var local = new Vector2[world.Length];
                    for (int i = 0; i < world.Length; i++) local[i] = world[i] - centre;
                    var go = MakeSurface(zone.transform, new Vector3(b.center.x, 0.06f, b.center.z), WaterShape.Stream,
                        new Vector2(longSide, halfWidth * 2f), zone.Clean ? clean : dirty, zone.Clean ? 0.03f : 0.015f);
                    var s = go.GetComponent<WaterSurface>();
                    s.StreamPath = local;
                    s.StreamHalfWidth = halfWidth;
                    made++;
                    // The upstream end opens into a round spring pool (the source), instead of ending in a point.
                    LakeLayout.StreamEnds(a, e, out Vector2 source, out Vector2 flow);
                    float poolRadius = Mathf.Max(4.5f, halfWidth * 1.9f);
                    MakeSurface(zone.transform, new Vector3(source.x, 0.06f, source.y), WaterShape.Disc, new Vector2(poolRadius, 0f),
                        zone.Clean ? clean : dirty, zone.Clean ? 0.03f : 0.015f);
                    made++;
                    continue;
                }
                MakeSurface(zone.transform, new Vector3(b.center.x, 0.06f, b.center.z), WaterShape.Rect,
                    new Vector2(b.size.x, b.size.z), zone.Clean ? clean : dirty, zone.Clean ? 0.03f : 0.015f);
                made++;
            }

            // 2) The life pool (a big disc where the slimes appear).
            var pool = GameObject.Find("Pool Water");
            if (pool != null)
            {
                float radius = pool.transform.lossyScale.x * 0.5f;
                var r = pool.GetComponent<Renderer>();
                if (r != null) r.enabled = false;
                MakeSurface(pool.transform.parent != null ? pool.transform.parent : pool.transform,
                    new Vector3(pool.transform.position.x, 0.05f, pool.transform.position.z), WaterShape.Disc, new Vector2(radius, 0f), life, 0.03f);
                made++;
            }

            // 3) Deep pockets: dark, deeper water instead of the black disc.
            var deepPockets = new List<(Vector2 centre, float radius, Transform root)>();
            foreach (var t in Object.FindObjectsByType<Transform>())
            {
                if (!t.name.StartsWith("Deep Pocket")) continue;
                var darkness = t.Find("Darkness");
                float radius = 5f;
                if (darkness != null)
                {
                    radius = darkness.lossyScale.x * 0.5f;
                    var r = darkness.GetComponent<Renderer>();
                    if (r != null) r.enabled = false;
                }
                MakeSurface(t, new Vector3(t.position.x, 0.04f, t.position.z), WaterShape.Disc, new Vector2(radius, 0f), deep, 0.02f);
                made++;
                deepPockets.Add((new Vector2(t.position.x, t.position.z), radius, t));
            }

            // 4) Deep means deep: dig a real basin in the terrain under each deep pocket (when the zone has a terrain),
            //    and add darker, nearly opaque layers toward the middle so the water looks like it goes down.
            var terrain = Object.FindAnyObjectByType<Terrain>();
            var abyss = WaterMaterial("Lake_WaterAbyss", new Color(0.01f, 0.07f, 0.12f, 0.5f), normal, 0.1f, Color.black);
            foreach (var pocket in deepPockets)
            {
                if (terrain != null) CarveBasin(terrain, pocket.centre, pocket.radius);
                for (int ring = 0; ring < 3; ring++)
                {
                    float r = pocket.radius * (0.8f - ring * 0.25f);
                    var disc = new GameObject("Depth (Dressed)");
                    disc.transform.SetParent(pocket.root, true);
                    disc.transform.position = new Vector3(pocket.centre.x, 0.07f + ring * 0.004f, pocket.centre.y);
                    disc.AddComponent<MeshFilter>().sharedMesh = WaterMeshes.Disc(r, 1.25f);
                    var renderer = disc.AddComponent<MeshRenderer>();
                    renderer.sharedMaterial = abyss;
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                }
            }
            // 5) Shallow water is a hollow too: the slime wades IN it (about half its body under the surface) instead of
            //    standing on top of a film of water. The ponds already know their shores, so each hollow follows its pond.
            if (terrain != null)
            {
                WaterSurface.Refresh();
                foreach (var pond in WaterSurface.All)
                {
                    if (pond == null || (pond.transform.parent != null && pond.transform.parent.name.StartsWith("Deep Pocket"))) continue;
                    var material = pond.GetComponent<MeshRenderer>().sharedMaterial;
                    float hollow = material != null && material.name == "Lake_WaterDirty" ? 0.2f : pond.Shape == WaterShape.Stream ? 0.4f : 0.5f;   // about half a slime body deep
                    CarveHollow(terrain, pond, hollow);
                }
                AssetDatabase.SaveAssets();
            }

            EditorSceneManager.MarkAllScenesDirty();
            AssetDatabase.SaveAssets();
            Debug.Log($"Đã dựng {made} mặt nước. Bấm Ctrl+S để lưu scene. (Nước chỉ chuyển động khi Play.)");
        }

        /// <summary>
        /// Lowers the terrain into a round bowl (about 2.5 m deep in the middle) under a deep pocket, steep at the
        /// water's edge, so the deep water is a real hollow and not a flat disc. Only changes the heights near the pocket.
        /// </summary>
        static void CarveBasin(Terrain terrain, Vector2 centre, float waterRadius)
        {
            var data = terrain.terrainData;
            Vector3 origin = terrain.transform.position, size = data.size;
            int res = data.heightmapResolution;
            float cell = size.x / (res - 1);
            float reach = waterRadius + 3f;
            int x0 = Mathf.Clamp(Mathf.FloorToInt((centre.x - reach - origin.x) / cell), 0, res - 1);
            int x1 = Mathf.Clamp(Mathf.CeilToInt((centre.x + reach - origin.x) / cell), 0, res - 1);
            int z0 = Mathf.Clamp(Mathf.FloorToInt((centre.y - reach - origin.z) / cell), 0, res - 1);
            int z1 = Mathf.Clamp(Mathf.CeilToInt((centre.y + reach - origin.z) / cell), 0, res - 1);
            int w = x1 - x0 + 1, h = z1 - z0 + 1;
            if (w < 2 || h < 2) return;
            var heights = data.GetHeights(x0, z0, w, h);
            const float depth = 2.5f;
            for (int j = 0; j < h; j++)
                for (int i = 0; i < w; i++)
                {
                    var p = new Vector2(origin.x + (x0 + i) * cell, origin.z + (z0 + j) * cell);
                    float t = Vector2.Distance(p, centre) / (waterRadius + 2f);
                    if (t >= 1f) continue;
                    float bowl = Mathf.Pow(1f - t * t, 1.5f);
                    float worldY = Mathf.Min(origin.y + heights[j, i] * size.y, -depth * bowl);   // from the flat ground: running again does not dig deeper
                    heights[j, i] = Mathf.Clamp01((worldY - origin.y) / size.y);
                }
            data.SetHeights(x0, z0, heights);
            EditorUtility.SetDirty(data);
            var collider = terrain.GetComponent<TerrainCollider>();
            if (collider != null) collider.terrainData = data;   // refresh the physics shape
        }

        /// <summary>
        /// Digs the terrain under a pond: `depth` metres in the middle, shelving up to the shore over about 3 m. Always
        /// measured from the flat ground (y = 0), so running it again does not dig deeper.
        /// </summary>
        static void CarveHollow(Terrain terrain, WaterSurface pond, float depth)
        {
            var data = terrain.terrainData;
            Vector3 origin = terrain.transform.position, size = data.size;
            int res = data.heightmapResolution;
            float cell = size.x / (res - 1);
            var outline = pond.WorldOutline;
            Vector2 min = outline[0], max = outline[0];
            foreach (var p in outline) { min = Vector2.Min(min, p); max = Vector2.Max(max, p); }
            int x0 = Mathf.Clamp(Mathf.FloorToInt((min.x - 1f - origin.x) / cell), 0, res - 1);
            int x1 = Mathf.Clamp(Mathf.CeilToInt((max.x + 1f - origin.x) / cell), 0, res - 1);
            int z0 = Mathf.Clamp(Mathf.FloorToInt((min.y - 1f - origin.z) / cell), 0, res - 1);
            int z1 = Mathf.Clamp(Mathf.CeilToInt((max.y + 1f - origin.z) / cell), 0, res - 1);
            int w = x1 - x0 + 1, h = z1 - z0 + 1;
            if (w < 2 || h < 2) return;
            var heights = data.GetHeights(x0, z0, w, h);
            const float shelf = 5f;
            for (int j = 0; j < h; j++)
                for (int i = 0; i < w; i++)
                {
                    var p = new Vector2(origin.x + (x0 + i) * cell, origin.z + (z0 + j) * cell);
                    if (!pond.ContainsWorld(p)) continue;
                    float best = float.MaxValue;
                    for (int k = 0; k < outline.Length; k++) best = Mathf.Min(best, Vector2.SqrMagnitude(outline[k] - p));
                    float inside = Mathf.Sqrt(best);
                    float dig = depth * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(inside / shelf));
                    float worldY = Mathf.Min(origin.y + heights[j, i] * size.y, -dig);   // from the flat ground, never deeper than asked
                    heights[j, i] = Mathf.Clamp01((worldY - origin.y) / size.y);
                }
            data.SetHeights(x0, z0, heights);
            EditorUtility.SetDirty(data);
            var collider = terrain.GetComponent<TerrainCollider>();
            if (collider != null) collider.terrainData = data;
        }

        static Material foamMaterial, bankMaterial; // set once per run (the menu is not re-entrant)
        static int seedCounter;

        static GameObject MakeSurface(Transform parent, Vector3 worldPos, WaterShape shape, Vector2 size, Material mat, float wave)
        {
            var go = new GameObject(DressedName);
            go.transform.SetParent(parent, true);
            go.transform.position = worldPos;
            go.transform.rotation = Quaternion.identity;
            var surface = go.AddComponent<WaterSurface>();
            surface.Shape = shape;
            surface.Size = size;
            surface.WaveHeight = wave;
            surface.FoamMaterial = foamMaterial;
            surface.BankMaterial = bankMaterial;
            surface.Seed = 3 + 17 * (++seedCounter); // every pond gets its own winding shore
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            go.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            return go;
        }

        // The grey-blue boxes / discs that used to stand for water are hidden (they stay, so nothing else changes).
        static void HideFlatVisuals(Transform zone)
        {
            foreach (var r in zone.GetComponentsInChildren<Renderer>(true))
                if (r.name == "Surface" || r.name == "Pool Water") r.enabled = false;
        }

        // ---------------------------------------------------------------- assets

        /// <summary>URP Lit as clear, glossy, translucent water: alpha blend, no depth write, rippling normal map.</summary>
        static Material WaterMaterial(string name, Color color, Texture2D normal, float bump, Color emission)
        {
            string path = $"{Root}/Materials/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 1f);   // the water hides whatever is below its surface (a wading slime shows only above it)
            mat.SetFloat("_Smoothness", 0.75f);
            mat.SetFloat("_Metallic", 0f);
            mat.SetTexture("_BumpMap", normal);
            mat.SetFloat("_BumpScale", bump);
            mat.EnableKeyword("_NORMALMAP");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = (int)RenderQueue.Transparent - 10; // water is drawn before the gel and the glass-like things above it
            if (emission.maxColorComponent > 0.001f)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emission); // a faint glow so the water is never pitch black in the cave
            }
            else mat.DisableKeyword("_EMISSION");
            EditorUtility.SetDirty(mat);
            return mat;
        }

        /// <summary>A tileable ripple normal map, painted in code from a few crossing sine waves (saved as a PNG).</summary>
        static Texture2D RippleNormalMap()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(NormalPath);
            if (existing != null) return existing;

            const int size = 128;
            // (frequency x, frequency y, amplitude, phase): whole numbers of repeats so the texture tiles.
            // Few, low, wide waves: calm water, no glittering speckles.
            var waves = new[] { (2, 1, 1.0f, 0.3f), (-1, 2, 0.8f, 1.9f), (3, -2, 0.45f, 4.1f), (1, 3, 0.3f, 2.6f) };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)size, v = y / (float)size;
                    float dx = 0f, dy = 0f;
                    foreach (var (fx, fy, amp, phase) in waves)
                    {
                        float cos = Mathf.Cos(2f * Mathf.PI * (fx * u + fy * v) + phase);
                        dx += amp * 2f * Mathf.PI * fx * cos;
                        dy += amp * 2f * Mathf.PI * fy * cos;
                    }
                    var n = new Vector3(-dx * 0.006f, -dy * 0.006f, 1f).normalized;
                    pixels[y * size + x] = new Color32(
                        (byte)Mathf.RoundToInt((n.x * 0.5f + 0.5f) * 255f),
                        (byte)Mathf.RoundToInt((n.y * 0.5f + 0.5f) * 255f),
                        (byte)Mathf.RoundToInt((n.z * 0.5f + 0.5f) * 255f), 255);
                }
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.SetPixels32(pixels);
            File.WriteAllBytes(NormalPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(NormalPath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(NormalPath);
            importer.textureType = TextureImporterType.NormalMap;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(NormalPath);
        }

        /// <summary>
        /// Foam: white and translucent, strongest in the middle of the band, fading out on both sides, broken up
        /// into blotches by smooth random noise (not regular stripes).
        /// </summary>
        static Material FoamMaterial()
        {
            var tex = PaintedTexture(Root + "/Models/WaterFoam_v2.png", 128, 32, (u, v) =>
            {
                float across = Mathf.Sin(Mathf.Clamp01(v) * Mathf.PI);
                across = Mathf.Pow(across, 0.8f);
                float blotch = TileableNoise(u * 12f, v * 3f, 12, 5);
                float fine = TileableNoise(u * 28f, v * 6f, 28, 7);
                float broken = Mathf.Clamp01(0.15f + 0.95f * blotch * (0.65f + 0.35f * fine));
                return new Color(1f, 1f, 1f, Mathf.Clamp01(across * broken * 1.2f));
            });
            return TranslucentMaterial("Lake_Foam", tex, new Color(0.95f, 1f, 1f, 0.8f), new Color(0.10f, 0.12f, 0.12f), 0.1f, (int)RenderQueue.Transparent - 5);
        }

        /// <summary>Wet bank: dark brown mud darkening the ground next to the water, fading out away from it.</summary>
        static Material BankMaterial()
        {
            var tex = PaintedTexture(Root + "/Models/WaterBank.png", 128, 32, (u, v) =>
            {
                // v: 0 = far from the water, 1 = at the water. Dark right at the water, fading out smoothly.
                float a = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(v));
                float mottled = 0.75f + 0.25f * TileableNoise(u * 10f, v * 3f, 10, 11);
                return new Color(1f, 1f, 1f, a * mottled);
            });
            return TranslucentMaterial("Lake_Bank", tex, new Color(0.10f, 0.08f, 0.06f, 0.75f), Color.black, 0.05f, (int)RenderQueue.Transparent - 15);
        }

        internal static Texture2D PaintedTexture(string path, int w, int h, System.Func<float, float, Color> paint)
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex != null) return tex;
            var pixels = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    pixels[y * w + x] = paint(x / (float)w, y / (float)(h - 1));
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            t.SetPixels32(pixels);
            File.WriteAllBytes(path, t.EncodeToPNG());
            Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.alphaIsTransparency = true;
            importer.wrapModeU = TextureWrapMode.Repeat;
            importer.wrapModeV = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        // Smooth value noise that repeats along x every `periodX` cells and along y every `periodY` cells.
        internal static float TileableNoise(float x, float y, int periodX, int periodY)
        {
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
            float fx = x - x0, fy = y - y0;
            float a = Hash(Mod(x0, periodX), Mod(y0, periodY)), b = Hash(Mod(x0 + 1, periodX), Mod(y0, periodY));
            float c = Hash(Mod(x0, periodX), Mod(y0 + 1, periodY)), d = Hash(Mod(x0 + 1, periodX), Mod(y0 + 1, periodY));
            fx = fx * fx * (3f - 2f * fx); fy = fy * fy * (3f - 2f * fy);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

        static int Mod(int v, int m) => ((v % m) + m) % m;

        static float Hash(int x, int y)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263);
                h = (h ^ (h >> 13)) * 1274126177u;
                return ((h ^ (h >> 16)) & 0xFFFF) / 65535f;
            }
        }

        internal static Material TranslucentMaterial(string name, Texture2D tex, Color color, Color emission, float smoothness, int queue)
        {
            string path = $"{Root}/Materials/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetTexture("_BaseMap", tex);
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            mat.SetFloat("_Smoothness", smoothness);
            mat.SetFloat("_Metallic", 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            if (emission.maxColorComponent > 0.001f) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", emission); }
            else mat.DisableKeyword("_EMISSION");
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = queue;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static void EnsureFolders()
        {
            foreach (var f in new[] { "Materials", "Models" })
                PrototypeSceneBuilder.EnsureFolder($"{Root}/{f}");
        }
    }
}
