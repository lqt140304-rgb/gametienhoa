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
            var dirty = WaterMaterial("Lake_WaterDirty", new Color(0.52f, 0.56f, 0.22f, 0.8f), normal, 0.18f, Color.black);
            foamMaterial = FoamMaterial();
            bankMaterial = BankMaterial();
            seedCounter = 0;

            // Take back anything a previous run made, so running the menu twice never stacks surfaces.
            foreach (var old in Object.FindObjectsByType<WaterSurface>())
                if (old.name == DressedName) Object.DestroyImmediate(old.gameObject);
            var oldRoot = GameObject.Find(LayoutRootName);
            if (oldRoot != null) Object.DestroyImmediate(oldRoot);

            int made = 0;

            // 1) The layout's water (World/ZoneLayout): the lake (its south bay is the Life Shallows), the winding stream,
            //    its two deep pools and the humus bed's dirty puddle. The terrain is already carved to their depths.
            var root = new GameObject(LayoutRootName).transform;
            made += Shoreline(root, LakeName, ZoneLayout.LakeShore(220), clean);
            made += Shoreline(root, StreamName, ZoneLayout.StreamShore(), clean);
            for (int i = 0; i < ZoneLayout.PoolCenters.Length; i++)
                made += Shoreline(root, "Deep Pool " + (i + 1), ZoneLayout.PoolShore(i, 64), clean);
            var puddle = MakeSurface(root, new Vector3(ZoneLayout.PuddleCenter.x, ZoneLayout.WaterLevel, ZoneLayout.PuddleCenter.y),
                WaterShape.Disc, new Vector2(ZoneLayout.PuddleRadius, 0f), dirty, 0.01f);
            puddle.name = "Vung Nuoc Dong";
            made++;

            // 2) The flat stand-in boxes of the water zones stay hidden (the surfaces above replace them).
            foreach (var zone in Object.FindObjectsByType<WaterZone>()) HideFlatVisuals(zone.transform);

            WaterSurface.Refresh(); // the shapes were set after the components woke up
            EditorSceneManager.MarkAllScenesDirty();
            AssetDatabase.SaveAssets();
            Debug.Log($"Đã dựng {made} mặt nước. Bấm Ctrl+S để lưu scene. (Nước chỉ chuyển động khi Play.)");
        }

        const string LayoutRootName = "Lake Water (Dressed)";
        /// <summary>Name of the lake surface (the gel nursery is built on its shore near the Life Shallows).</summary>
        internal const string LakeName = "Lake";
        internal const string StreamName = "Khe Thuy Mach";

        /// <summary>A pond whose shore is the given world outline.</summary>
        static int Shoreline(Transform parent, string name, Vector2[] world, Material mat)
        {
            Vector2 centre = Vector2.zero;
            foreach (var p in world) centre += p;
            centre /= world.Length;
            var go = MakeSurface(parent, new Vector3(centre.x, ZoneLayout.WaterLevel, centre.y), WaterShape.Outline, Vector2.one, mat, 0.02f);
            go.name = name;
            var local = new Vector2[world.Length];
            for (int i = 0; i < world.Length; i++) local[i] = world[i] - centre;
            go.GetComponent<WaterSurface>().OutlinePoints = local;
            return 1;
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
            mat.SetFloat("_ZWrite", 0f);
            mat.SetFloat("_Smoothness", 0.75f);
            mat.SetFloat("_Metallic", 0f);
            mat.SetTexture("_BumpMap", normal);
            mat.SetFloat("_BumpScale", bump);
            mat.EnableKeyword("_NORMALMAP");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = (int)RenderQueue.Transparent + 20; // after the gel: what is under the surface shows through the water, tinted
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
