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
            var deep = WaterMaterial("Lake_WaterDeep", new Color(0.04f, 0.22f, 0.34f, 0.88f), normal, 0.22f, new Color(0.0f, 0.03f, 0.05f));

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
                    new Vector3(pool.transform.position.x, 0.05f, pool.transform.position.z), WaterShape.Disc, new Vector2(radius, 0f), clean, 0.03f);
                made++;
            }

            // 3) Deep pockets: dark, deeper water instead of the black disc.
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
            }

            EditorSceneManager.MarkAllScenesDirty();
            AssetDatabase.SaveAssets();
            Debug.Log($"Đã dựng {made} mặt nước. Bấm Ctrl+S để lưu scene. (Nước chỉ chuyển động khi Play.)");
        }

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
            mat.SetFloat("_Smoothness", 0.85f);
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

        static void EnsureFolders()
        {
            foreach (var f in new[] { "Materials", "Models" })
                PrototypeSceneBuilder.EnsureFolder($"{Root}/{f}");
        }
    }
}
