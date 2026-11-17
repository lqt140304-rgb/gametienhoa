using Evolve.Organs;
using Evolve.Player;
using Evolve.Visuals;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Evolve.EditorTools
{
    /// <summary>
    /// Builds the gel look of the slime: translucent gel materials, the dome mesh asset and the gel organ prefabs
    /// (eye stalks, curved pseudopods, spikes with plates). Everything is code-made stand-ins: each part is its own
    /// prefab, so a real model can replace it later without touching game code.
    /// Menu: Evolve -> Rebuild Slime Visuals (re-styles the slime in the open scene and rewrites the organ prefabs).
    /// </summary>
    public static class GelVisuals
    {
        const string Root = "Assets/_Project";
        static readonly Color GelColor = new Color(0.62f, 0.80f, 0.55f, 0.45f);
        static readonly Vector3 BackSocket = new Vector3(0f, 0.3f, -0.25f);

        [MenuItem("Evolve/Rebuild Slime Visuals")]
        public static void RebuildAll()
        {
            RebuildOrganPrefabs();
            foreach (var controller in Object.FindObjectsByType<SlimeController>())
            {
                var visual = controller.transform.Find("Visual");
                if (visual != null) StyleBody(visual.gameObject);
            }
            EditorSceneManager.MarkAllScenesDirty();
            AssetDatabase.SaveAssets();
            Debug.Log("Đã dựng lại hình slime gel và cơ quan. Bấm Ctrl+S để lưu scene.");
        }

        // ---------------------------------------------------------------- body

        /// <summary>Swap the primitive sphere on "Visual" for the gel dome and add the glowing core.</summary>
        internal static void StyleBody(GameObject visual)
        {
            EnsureFolders();
            var filter = visual.GetComponent<MeshFilter>();
            if (filter == null) filter = visual.AddComponent<MeshFilter>();
            filter.sharedMesh = BodyMesh();
            var renderer = visual.GetComponent<MeshRenderer>();
            if (renderer == null) renderer = visual.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = GelMaterial("SlimeGel", GelColor);
            if (visual.GetComponent<SlimeGelCore>() == null) visual.AddComponent<SlimeGelCore>();
        }

        static Mesh BodyMesh()
        {
            const string path = Root + "/Models/SlimeBody.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh != null) return mesh;
            mesh = SlimeMeshBuilder.Build();
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        static Mesh ConeMesh()
        {
            const string path = Root + "/Models/GelCone.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh != null) return mesh;
            mesh = SlimeMeshBuilder.Cone(6, 0.08f, 0.28f);
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        // ---------------------------------------------------------------- organs

        /// <summary>Overwrites the organ prefabs at their existing paths, so OrganSO references keep working.</summary>
        internal static void RebuildOrganPrefabs()
        {
            EnsureFolders();
            var gel = GelMaterial("SlimeGel", GelColor);
            var gelOrgan = GelMaterial("GelOrgan", new Color(0.50f, 0.74f, 0.50f, 0.62f));
            var gelStrong = GelMaterial("GelOrganStrong", new Color(0.45f, 0.82f, 0.45f, 0.66f));
            var gelRunner = GelMaterial("GelOrganRunner", new Color(0.45f, 0.65f, 0.90f, 0.66f));
            var gelHeat = GelMaterial("GelOrganHeat", new Color(0.95f, 0.60f, 0.25f, 0.55f));
            var gelSpike = GelMaterial("GelOrganSpike", new Color(0.60f, 0.45f, 0.75f, 0.70f));
            var white = PrototypeSceneBuilder.Mat("Eye", new Color(0.95f, 0.95f, 0.95f), "Universal Render Pipeline/Lit");
            var black = PrototypeSceneBuilder.Mat("Pupil", new Color(0.05f, 0.05f, 0.05f), "Universal Render Pipeline/Lit");

            Save(EyeStalks(gelOrgan, white, black), "Organ_Eyes");
            Save(Pseudopods("Organ_Pseudopods", gelOrgan, 1f), "Organ_Pseudopods");
            Save(Pseudopods("Organ_MuscularPseudopods", gelStrong, 1.3f), "Organ_MuscularPseudopods");
            Save(Pseudopods("Organ_RunnerLegs", gelRunner, 1.6f), "Organ_RunnerLegs");
            Save(HeatBands(gelHeat), "Organ_HeatMembrane");
            Save(SpikesAndPlates(gelSpike, gelOrgan), "Organ_ToxicSpikes");
            AssetDatabase.SaveAssets();
        }

        static GameObject EyeStalks(Material gel, Material white, Material black)
        {
            var root = new GameObject("Organ_Eyes");
            foreach (float side in new[] { -1f, 1f })
            {
                float x = 0.13f * side;
                Part(PrimitiveType.Sphere, "Base", root.transform, new Vector3(x, -0.02f, 0f), new Vector3(0.14f, 0.08f, 0.14f), gel);
                var stalk = Part(PrimitiveType.Capsule, "Stalk", root.transform, new Vector3(x * 1.35f, 0.1f, 0.02f), new Vector3(0.045f, 0.11f, 0.045f), gel);
                stalk.transform.localRotation = Quaternion.Euler(0f, 0f, -side * 18f);
                var eye = Part(PrimitiveType.Sphere, "Eye", root.transform, new Vector3(x * 1.7f, 0.22f, 0.03f), Vector3.one * 0.16f, white);
                Part(PrimitiveType.Sphere, "Pupil", eye.transform, new Vector3(0f, 0f, 0.42f), Vector3.one * 0.5f, black);
            }
            return root;
        }

        static GameObject Pseudopods(string name, Material gel, float size)
        {
            var root = new GameObject(name);
            foreach (float sx in new[] { -1f, 1f })
                foreach (float sz in new[] { -1f, 1f })
                {
                    var corner = new Vector3(sx * 0.27f, 0.15f, sz * 0.22f);
                    Vector3 dir = new Vector3(sx * 0.6f, -0.8f, sz * 0.5f).normalized;
                    Part(PrimitiveType.Sphere, "Knee", root.transform, corner, Vector3.one * 0.15f * size, gel);
                    var leg = Part(PrimitiveType.Capsule, "Pseudopod", root.transform, corner + dir * 0.13f * size,
                        new Vector3(0.1f, 0.16f, 0.1f) * size, gel);
                    leg.transform.localRotation = Quaternion.FromToRotation(Vector3.up, dir);
                    Part(PrimitiveType.Sphere, "Foot", root.transform, corner + dir * 0.3f * size,
                        new Vector3(0.16f, 0.07f, 0.16f) * size, gel);
                }
            return root;
        }

        static GameObject HeatBands(Material gel)
        {
            var root = new GameObject("Organ_HeatMembrane");
            Part(PrimitiveType.Cylinder, "Band", root.transform, Vector3.zero, new Vector3(1.06f, 0.04f, 1.06f), gel);
            Part(PrimitiveType.Cylinder, "BandTop", root.transform, new Vector3(0f, 0.22f, 0f), new Vector3(0.92f, 0.03f, 0.92f), gel);
            Part(PrimitiveType.Cylinder, "BandLow", root.transform, new Vector3(0f, -0.22f, 0f), new Vector3(0.92f, 0.03f, 0.92f), gel);
            return root;
        }

        static GameObject SpikesAndPlates(Material spikeMat, Material plateMat)
        {
            var root = new GameObject("Organ_ToxicSpikes");
            var cone = ConeMesh();
            var offsets = new[]
            {
                new Vector3(0f, 0.08f, 0.05f), new Vector3(0.15f, 0f, 0.02f), new Vector3(-0.15f, 0f, 0.02f),
                new Vector3(0.1f, -0.06f, -0.12f), new Vector3(-0.1f, -0.06f, -0.12f), new Vector3(0f, 0.02f, -0.1f),
                new Vector3(0.22f, -0.08f, -0.05f), new Vector3(-0.22f, -0.08f, -0.05f)
            };
            foreach (var off in offsets)
            {
                var go = new GameObject("Spike");
                go.transform.SetParent(root.transform, false);
                go.transform.localPosition = off;
                Vector3 outward = (BackSocket + off - new Vector3(0f, -0.1f, 0f)).normalized;
                go.transform.localRotation = Quaternion.FromToRotation(Vector3.up, outward);
                go.AddComponent<MeshFilter>().sharedMesh = cone;
                go.AddComponent<MeshRenderer>().sharedMaterial = spikeMat;
            }
            var plateA = Part(PrimitiveType.Cylinder, "Plate", root.transform, new Vector3(0f, 0.03f, 0.0f), new Vector3(0.36f, 0.012f, 0.26f), plateMat);
            plateA.transform.localRotation = Quaternion.Euler(-8f, 0f, 0f);
            var plateB = Part(PrimitiveType.Cylinder, "Plate", root.transform, new Vector3(0f, -0.05f, -0.16f), new Vector3(0.28f, 0.012f, 0.2f), plateMat);
            plateB.transform.localRotation = Quaternion.Euler(-35f, 0f, 0f);
            return root;
        }

        // ---------------------------------------------------------------- helpers

        static GameObject Part(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale, Material mat) =>
            PrototypeSceneBuilder.Part(type, name, parent, pos, scale, mat);

        static void Save(GameObject go, string name)
        {
            PrefabUtility.SaveAsPrefabAsset(go, $"{Root}/Prefabs/{name}.prefab");
            Object.DestroyImmediate(go);
        }

        static void EnsureFolders()
        {
            foreach (var f in new[] { "Prefabs", "Materials", "Models" })
                PrototypeSceneBuilder.EnsureFolder($"{Root}/{f}");
        }

        /// <summary>URP Lit set up as translucent gel (alpha blend, glossy, no depth write). Created once, reconfigured each time.</summary>
        static Material GelMaterial(string name, Color color)
        {
            string path = $"{Root}/Materials/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Surface", 1f);       // transparent
            mat.SetFloat("_Blend", 0f);         // alpha
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            mat.SetFloat("_Smoothness", 0.9f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(mat);
            return mat;
        }
    }
}
