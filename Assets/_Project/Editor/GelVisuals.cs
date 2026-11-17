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
        static readonly Color GelColor = new Color(0.55f, 0.78f, 0.50f, 0.82f);
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

        /// <summary>
        /// Overwrites the organ prefabs at their existing paths, so OrganSO references keep working.
        /// Rule for every organ (present and future): it is made of the SAME gel material as the body and grows out of
        /// a fused blob at its base, so it reads as part of the body. The heat layer is a skin-like shell, not rings.
        /// </summary>
        internal static void RebuildOrganPrefabs()
        {
            EnsureFolders();
            var gel = GelMaterial("SlimeGel", GelColor);
            var heat = GelMaterial("GelHeatLayer", new Color(0.92f, 0.74f, 0.36f, 0.55f));
            var white = PrototypeSceneBuilder.Mat("Eye", new Color(0.95f, 0.95f, 0.95f), "Universal Render Pipeline/Lit");
            var black = PrototypeSceneBuilder.Mat("Pupil", new Color(0.05f, 0.05f, 0.05f), "Universal Render Pipeline/Lit");

            Save(EyeStalks(gel, white, black), "Organ_Eyes");
            Save(Pseudopods("Organ_Pseudopods", gel, 1f), "Organ_Pseudopods");
            Save(Pseudopods("Organ_MuscularPseudopods", gel, 1.3f), "Organ_MuscularPseudopods");
            Save(Pseudopods("Organ_RunnerLegs", gel, 1.6f), "Organ_RunnerLegs");
            Save(HeatLayer(heat), "Organ_HeatMembrane");
            Save(SpikesAndPlates(gel), "Organ_ToxicSpikes");
            AssetDatabase.SaveAssets();
        }

        /// <summary>A gel blob that melts the organ into the body.</summary>
        static void Blob(Transform parent, Vector3 pos, Vector3 scale, Material gel) =>
            Part(PrimitiveType.Sphere, "Blob", parent, pos, scale, gel);

        static GameObject EyeStalks(Material gel, Material white, Material black)
        {
            var root = new GameObject("Organ_Eyes");
            foreach (float side in new[] { -1f, 1f })
            {
                float x = 0.12f * side;
                Blob(root.transform, new Vector3(x, -0.03f, 0f), new Vector3(0.2f, 0.12f, 0.2f), gel);
                Blob(root.transform, new Vector3(x * 1.1f, 0.03f, 0.01f), new Vector3(0.13f, 0.12f, 0.13f), gel);
                var stalk = Part(PrimitiveType.Capsule, "Stalk", root.transform, new Vector3(x * 1.45f, 0.14f, 0.03f), new Vector3(0.05f, 0.12f, 0.05f), gel);
                stalk.transform.localRotation = Quaternion.Euler(0f, 0f, -side * 16f);
                Blob(root.transform, new Vector3(x * 1.75f, 0.27f, 0.04f), Vector3.one * 0.12f, gel); // socket of the eye
                var eye = Part(PrimitiveType.Sphere, "Eye", root.transform, new Vector3(x * 1.75f, 0.3f, 0.06f), Vector3.one * 0.17f, white);
                Part(PrimitiveType.Sphere, "Pupil", eye.transform, new Vector3(0f, 0f, 0.42f), Vector3.one * 0.52f, black);
            }
            return root;
        }

        static GameObject Pseudopods(string name, Material gel, float size)
        {
            var root = new GameObject(name);
            foreach (float sx in new[] { -1f, 1f })
                foreach (float sz in new[] { -1f, 1f })
                {
                    var shoulder = new Vector3(sx * 0.26f, 0.18f, sz * 0.2f);
                    Vector3 dir = new Vector3(sx * 0.6f, -0.8f, sz * 0.5f).normalized;
                    Blob(root.transform, shoulder, new Vector3(0.26f, 0.2f, 0.26f) * size, gel);               // fused into the body
                    var upper = Part(PrimitiveType.Capsule, "Upper", root.transform, shoulder + dir * 0.12f * size,
                        new Vector3(0.12f, 0.15f, 0.12f) * size, gel);
                    upper.transform.localRotation = Quaternion.FromToRotation(Vector3.up, dir);
                    var lower = Part(PrimitiveType.Capsule, "Lower", root.transform, shoulder + dir * 0.27f * size,
                        new Vector3(0.085f, 0.13f, 0.085f) * size, gel);
                    lower.transform.localRotation = Quaternion.FromToRotation(Vector3.up, dir);
                    Vector3 foot = shoulder + dir * 0.4f * size;
                    Part(PrimitiveType.Sphere, "Foot", root.transform, foot, new Vector3(0.2f, 0.07f, 0.2f) * size, gel);
                    foreach (float t in new[] { -1f, 0f, 1f })   // three toe bumps
                        Part(PrimitiveType.Sphere, "Toe", root.transform, foot + new Vector3(sx * 0.07f + t * 0.05f, -0.01f, sz * 0.1f) * size,
                            Vector3.one * 0.06f * size, gel);
                }
            return root;
        }

        /// <summary>The heat insulation is a thick layer over the whole body (a slightly larger shell), not rings.</summary>
        static GameObject HeatLayer(Material heat)
        {
            var root = new GameObject("Organ_HeatMembrane");
            var shell = new GameObject("Layer");
            shell.transform.SetParent(root.transform, false);
            shell.transform.localPosition = new Vector3(0f, 0.012f, 0f);
            shell.transform.localScale = new Vector3(1.09f, 1.14f, 1.09f);
            shell.AddComponent<MeshFilter>().sharedMesh = BodyMesh();
            shell.AddComponent<MeshRenderer>().sharedMaterial = heat;
            return root;
        }

        static GameObject SpikesAndPlates(Material gel)
        {
            var root = new GameObject("Organ_ToxicSpikes");
            var cone = ConeMesh();
            // (position on the back, length factor)
            var spikes = new[]
            {
                (new Vector3(0f, 0.1f, 0.05f), 1.3f), (new Vector3(0.15f, 0.02f, 0.02f), 1.0f), (new Vector3(-0.15f, 0.02f, 0.02f), 1.0f),
                (new Vector3(0.1f, -0.05f, -0.12f), 1.15f), (new Vector3(-0.1f, -0.05f, -0.12f), 1.15f), (new Vector3(0f, 0.03f, -0.1f), 1.2f),
                (new Vector3(0.24f, -0.07f, -0.05f), 0.8f), (new Vector3(-0.24f, -0.07f, -0.05f), 0.8f),
                (new Vector3(0.07f, 0.07f, -0.2f), 0.9f), (new Vector3(-0.07f, 0.07f, -0.2f), 0.9f)
            };
            foreach (var (off, len) in spikes)
            {
                Vector3 outward = (BackSocket + off - new Vector3(0f, -0.1f, 0f)).normalized;
                Blob(root.transform, off, new Vector3(0.14f, 0.07f, 0.14f), gel);   // each spike rises out of a gel swelling
                var go = new GameObject("Spike");
                go.transform.SetParent(root.transform, false);
                go.transform.localPosition = off;
                go.transform.localRotation = Quaternion.FromToRotation(Vector3.up, outward);
                go.transform.localScale = new Vector3(1f, len, 1f);
                go.AddComponent<MeshFilter>().sharedMesh = cone;
                go.AddComponent<MeshRenderer>().sharedMaterial = gel;
            }
            // Overlapping armour plates, same gel as the body.
            var plates = new[]
            {
                (new Vector3(0f, 0.05f, 0f), new Vector3(0.36f, 0.025f, 0.26f), -8f),
                (new Vector3(0f, -0.02f, -0.15f), new Vector3(0.3f, 0.025f, 0.22f), -35f),
                (new Vector3(0.2f, -0.02f, -0.02f), new Vector3(0.2f, 0.022f, 0.2f), 20f)
            };
            foreach (var (pos, scale, tilt) in plates)
            {
                var plate = Part(PrimitiveType.Cylinder, "Plate", root.transform, pos, scale, gel);
                plate.transform.localRotation = Quaternion.Euler(tilt, 0f, 0f);
            }
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
