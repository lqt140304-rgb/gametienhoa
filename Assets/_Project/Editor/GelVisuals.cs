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
    /// Builds the gel look of the slime: the milky gel material (with painted veins), the smooth body mesh asset and
    /// the gel organ prefabs. Everything is code-made stand-ins: each part is its own prefab, so a real model can
    /// replace it later without touching game code.
    /// Rules for every organ (present and future): same gel material as the body, and it grows out of a soft flared
    /// base that spreads over the body, so the joint melts in instead of poking through.
    /// Menu: Evolve -> Rebuild Slime Visuals (re-styles the slime in the open scene and rewrites the organ prefabs).
    /// </summary>
    public static class GelVisuals
    {
        const string Root = "Assets/_Project";
        const string OrganMeshes = Root + "/Models/Organs";
        static readonly Color GelColor = new Color(0.68f, 0.88f, 0.68f, 0.85f); // default; the player's colour is applied per renderer (SlimeGelTint)

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

        /// <summary>Swap the primitive sphere on "Visual" for the gel body, move the sockets, add the glowing core.</summary>
        internal static void StyleBody(GameObject visual)
        {
            EnsureFolders();
            var filter = visual.GetComponent<MeshFilter>();
            if (filter == null) filter = visual.AddComponent<MeshFilter>();
            filter.sharedMesh = BodyMesh();
            var renderer = visual.GetComponent<MeshRenderer>();
            if (renderer == null) renderer = visual.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = BodyGel();
            foreach (var socket in visual.GetComponentsInChildren<OrganSocket>(true))
                socket.transform.localPosition = SlimeSockets.Position(socket.Slot);
            if (visual.GetComponent<SlimeGelCore>() == null) visual.AddComponent<SlimeGelCore>();
        }

        static Material BodyGel()
        {
            var mat = GelMaterial("SlimeGel", GelColor);
            mat.SetTexture("_BaseMap", null); // clean glossy gel (no vein texture)
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static Mesh BodyMesh()
        {
            const string path = Root + "/Models/SlimeBody_v2.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh != null) return mesh;
            mesh = SlimeMeshBuilder.Build();
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        // ---------------------------------------------------------------- organs

        /// <summary>Overwrites the organ prefabs at their existing paths, so OrganSO references keep working.</summary>
        internal static void RebuildOrganPrefabs()
        {
            EnsureFolders();
            if (AssetDatabase.IsValidFolder(OrganMeshes)) AssetDatabase.DeleteAsset(OrganMeshes);
            PrototypeSceneBuilder.EnsureFolder(OrganMeshes);

            // One gel material for the body and every organ (tinted per player by SlimeGelTint).
            var gel = BodyGel();
            var heat = GelMaterial("GelHeatLayer", new Color(0.9f, 0.75f, 0.4f, 0.22f));
            var black = PrototypeSceneBuilder.Mat("Pupil", new Color(0.05f, 0.05f, 0.05f), "Universal Render Pipeline/Lit");

            // Overall organ size relative to the body (kept small so the body stays the star).
            Save(EyeStalks(gel, black), "Organ_Eyes", 0.75f);
            Save(Pseudopods("Organ_Pseudopods", gel, 1f), "Organ_Pseudopods", 0.8f);
            Save(Pseudopods("Organ_MuscularPseudopods", gel, 1.3f), "Organ_MuscularPseudopods", 0.8f);
            Save(Pseudopods("Organ_RunnerLegs", gel, 1.6f), "Organ_RunnerLegs", 0.8f);
            Save(HeatLayer(heat), "Organ_HeatMembrane", 1f);
            Save(Horns(gel), "Organ_ToxicSpikes", 0.75f);
            AssetDatabase.SaveAssets();
        }

        static GameObject EyeStalks(Material gel, Material black)
        {
            var root = new GameObject("Organ_Eyes");
            foreach (float s in new[] { -1f, 1f })
            {
                var p0 = new Vector3(0.11f * s, -0.05f, 0f);
                var path = Curve(p0, new Vector3(0.13f * s, 0.06f, 0.03f), new Vector3(0.18f * s, 0.14f, 0.05f), new Vector3(0.22f * s, 0.22f, 0.07f), 9);
                var radii = new float[path.Length];
                for (int i = 0; i < radii.Length; i++) radii[i] = Mathf.Lerp(0.038f, 0.021f, Mathf.SmoothStep(0f, 1f, i / (float)(radii.Length - 1)));
                AddMesh(root, SlimeMeshBuilder.Tube(path, radii, 10), gel, Vector3.zero, Quaternion.identity);

                // A glossy black bead of an eye on the tip of the stalk, with a small white highlight.
                Vector3 tip = path[path.Length - 1];
                Vector3 dir = (tip - path[path.Length - 2]).normalized;
                var bead = PrototypeSceneBuilder.Part(PrimitiveType.Sphere, "Eye", root.transform, tip + dir * 0.03f, Vector3.one * 0.1f, black);
                var white = PrototypeSceneBuilder.Mat("Eye", new Color(0.95f, 0.95f, 0.95f), "Universal Render Pipeline/Lit");
                PrototypeSceneBuilder.Part(PrimitiveType.Sphere, "Highlight", root.transform, tip + dir * 0.045f + new Vector3(0.018f * s, 0.02f, 0.03f), Vector3.one * 0.026f, white);
                bead.name = "Eye";

                // Soft fillet that spreads over the body where the stalk leaves it.
                AddMesh(root, SlimeMeshBuilder.Flare(0.1f, 0.04f, 0.08f), gel, new Vector3(0.11f * s, -0.09f, 0f), Quaternion.identity);
            }
            return root;
        }

        static GameObject Pseudopods(string name, Material gel, float size)
        {
            var root = new GameObject(name);
            float reach = 0.24f + 0.07f * size, thick = Mathf.Sqrt(size);
            foreach (float sx in new[] { -1f, 1f })
                foreach (float sz in new[] { -1f, 1f })
                {
                    var shoulder = new Vector3(sx * 0.18f, 0.04f, sz * 0.15f);
                    var curve = Curve(shoulder,
                        new Vector3(sx * reach * 0.55f, 0.08f, sz * reach * 0.45f),
                        new Vector3(sx * reach * 0.9f, 0f, sz * reach * 0.8f),
                        new Vector3(sx * reach, -0.12f, sz * reach * 0.9f), 8);
                    var path = new Vector3[curve.Length + 3];
                    var radii = new float[path.Length];
                    for (int i = 0; i < curve.Length; i++)
                    {
                        path[i] = curve[i];
                        radii[i] = Mathf.Lerp(0.1f, 0.062f, Mathf.SmoothStep(0f, 1f, i / (float)(curve.Length - 1))) * thick;
                    }
                    Vector3 foot = curve[curve.Length - 1];
                    path[curve.Length] = foot + new Vector3(sx * 0.02f, -0.012f, sz * 0.02f); radii[curve.Length] = 0.07f * thick; // rounded pad
                    path[curve.Length + 1] = foot + new Vector3(sx * 0.035f, -0.02f, sz * 0.035f); radii[curve.Length + 1] = 0.04f * thick;
                    path[curve.Length + 2] = foot + new Vector3(sx * 0.04f, -0.024f, sz * 0.04f); radii[curve.Length + 2] = 0f;
                    AddMesh(root, SlimeMeshBuilder.Tube(path, radii, 12), gel, Vector3.zero, Quaternion.identity);

                    // Wide soft shoulder so the leg grows out of the body.
                    AddMesh(root, SlimeMeshBuilder.Flare(0.13f * thick, 0.075f * thick, 0.08f), gel, new Vector3(shoulder.x, shoulder.y - 0.08f, shoulder.z), Quaternion.identity);
                }
            return root;
        }

        /// <summary>The heat insulation is an ultra-thin film over the whole body (a barely larger shell), not rings.</summary>
        static GameObject HeatLayer(Material heat)
        {
            var root = new GameObject("Organ_HeatMembrane");
            var shell = new GameObject("Layer");
            shell.transform.SetParent(root.transform, false);
            shell.transform.localPosition = new Vector3(0f, 0.004f, 0f);
            shell.transform.localScale = new Vector3(1.02f, 1.03f, 1.02f); // an almost invisible thin film
            shell.AddComponent<MeshFilter>().sharedMesh = BodyMesh();
            shell.AddComponent<MeshRenderer>().sharedMaterial = heat;
            return root;
        }

        /// <summary>Short curved horns on the back, each rising out of a flared base.</summary>
        static GameObject Horns(Material gel)
        {
            var root = new GameObject("Organ_ToxicSpikes");
            // position on the back (socket space), length
            var horns = new[]
            {
                (new Vector3(0f, 0.07f, 0.06f), 0.30f), (new Vector3(0.15f, 0.01f, 0.02f), 0.22f), (new Vector3(-0.15f, 0.01f, 0.02f), 0.22f),
                (new Vector3(0.1f, 0.0f, -0.12f), 0.24f), (new Vector3(-0.1f, 0.0f, -0.12f), 0.24f), (new Vector3(0f, 0.04f, -0.2f), 0.2f)
            };
            var centre = new Vector3(0f, -0.2f, 0.05f);
            foreach (var (pos, length) in horns)
            {
                Vector3 dir = ((pos - centre).normalized + Vector3.up * 0.3f).normalized;
                var path = new Vector3[5];
                var radii = new[] { 0.04f, 0.03f, 0.021f, 0.011f, 0f };
                for (int i = 0; i < path.Length; i++)
                {
                    float t = i / 4f;
                    path[i] = pos + dir * length * 0.75f * t + Vector3.back * 0.03f * t * t; // slight curve toward the back
                }
                AddMesh(root, SlimeMeshBuilder.Tube(path, radii, 8), gel, Vector3.zero, Quaternion.identity);
                // Flared base lying along the horn's axis, sunk a little into the body.
                AddMesh(root, SlimeMeshBuilder.Flare(0.075f, 0.035f, 0.05f), gel, pos - dir * 0.025f, Quaternion.FromToRotation(Vector3.up, dir));
            }
            return root;
        }

        // ---------------------------------------------------------------- helpers

        static Vector3[] Curve(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, int count)
        {
            var points = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)(count - 1), u = 1f - t;
                points[i] = u * u * u * p0 + 3f * u * u * t * p1 + 3f * u * t * t * p2 + t * t * t * p3;
            }
            return points;
        }

        /// <summary>Saves the mesh as an asset (prefabs can only keep asset meshes) and adds it under the organ root.</summary>
        static void AddMesh(GameObject root, Mesh mesh, Material mat, Vector3 pos, Quaternion rot)
        {
            string path = $"{OrganMeshes}/{root.name}_{root.transform.childCount}.asset";
            AssetDatabase.CreateAsset(mesh, path);
            var go = new GameObject(mesh.name);
            go.transform.SetParent(root.transform, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = rot;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        }

        static void Save(GameObject go, string name, float scale)
        {
            go.transform.localScale = Vector3.one * scale;
            PrefabUtility.SaveAsPrefabAsset(go, $"{Root}/Prefabs/{name}.prefab");
            Object.DestroyImmediate(go);
        }

        static void EnsureFolders()
        {
            foreach (var f in new[] { "Prefabs", "Materials", "Models" })
                PrototypeSceneBuilder.EnsureFolder($"{Root}/{f}");
        }

        /// <summary>URP Lit set up as translucent gel (alpha blend, glossy, writes depth so water drawn later covers only what is under its surface). Created once, reconfigured each time.</summary>
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
            mat.SetFloat("_ZWrite", 1f);
            mat.SetFloat("_Smoothness", 0.97f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(mat);
            return mat;
        }
    }
}
