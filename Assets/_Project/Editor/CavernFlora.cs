using System.Collections.Generic;
using Evolve.Ecology;
using Evolve.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Evolve.EditorTools
{
    /// <summary>
    /// Evolve -> Dress Cavern: Flora. The cave's vegetation that cannot be eaten (the edible plants are Dress Cavern: Forage): reeds in the shallows, ferns and moss in the
    /// damp, leafy bushes on the drier rises, clusters of mushrooms with glowing caps in the shade, roots breaking out at the wall's foot, and vines hanging from the roof round
    /// the hole of light. Blender models (tools/blender/build_cavern_flora.py); where each grows comes from World/CavernFloraMath. Rerunnable; undo: Restore Cavern Flora.
    /// </summary>
    public static class CavernFlora
    {
        const string ModelFolder = "Assets/_Project/Models/Underroot/";
        const string RootName = "Cavern Flora";
        const int Seed = 7007;

        struct Plan
        {
            public CavernFloraMath.Kind Kind;
            public string Model;
            public int Variants, Count;
            public float Spacing, ScaleLo, ScaleHi;
            public Plan(CavernFloraMath.Kind kind, string model, int variants, int count, float spacing, float lo, float hi) { Kind = kind; Model = model; Variants = variants; Count = count; Spacing = spacing; ScaleLo = lo; ScaleHi = hi; }
        }

        static readonly Plan[] Plans =
        {
            new Plan(CavernFloraMath.Kind.Reed, "Reed", 2, 130, 1.0f, 0.9f, 1.4f),
            new Plan(CavernFloraMath.Kind.Moss, "Moss", 3, 170, 0.9f, 0.9f, 1.8f),
            new Plan(CavernFloraMath.Kind.Fern, "Fern", 2, 150, 1.6f, 0.8f, 1.5f),
            new Plan(CavernFloraMath.Kind.Bush, "Bush", 2, 70, 2.2f, 0.9f, 1.5f),
            new Plan(CavernFloraMath.Kind.Mushrooms, "Mushrooms", 2, 80, 1.4f, 0.8f, 1.5f),
            new Plan(CavernFloraMath.Kind.RootArc, "RootArc", 2, 40, 3.0f, 0.9f, 1.5f),
            new Plan(CavernFloraMath.Kind.Vine, "Vine", 2, 26, 3.0f, 0.9f, 1.3f)
        };

        [MenuItem("Evolve/Dress Cavern: Flora")]
        public static void Dress()
        {
            PropInstaller.EnsureLandmarks();
            var prefabs = new Dictionary<string, GameObject>();
            foreach (var p in Plans)
                for (int v = 0; v < p.Variants; v++)
                {
                    string name = $"Flora_{p.Model}_{v}";
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModelFolder + name + ".fbx");
                    if (prefab == null) { Debug.LogWarning("Thiếu " + name + ".fbx (chạy Reinstall Blender Props rồi thử lại): bỏ qua thảm thực vật."); return; }
                    prefabs[name] = prefab;
                }
            var old = GameObject.Find(RootName);
            if (old != null) Object.DestroyImmediate(old);
            var root = new GameObject(RootName).transform;
            var glow = GlowMaterial();

            var taken = new List<Vector2>();
            foreach (var n in Object.FindObjectsByType<Nutrient>()) taken.Add(new Vector2(n.transform.position.x, n.transform.position.z));
            var spawn = CavernLayout.SpawnPoint();
            var rnd = new System.Random(Seed);
            var summary = new List<string>();
            int total = 0;
            foreach (var plan in Plans)
            {
                var group = new GameObject(plan.Model).transform;
                group.SetParent(root, false);
                var mine = new List<Vector2>();
                int got = 0;
                bool vine = plan.Kind == CavernFloraMath.Kind.Vine;
                for (int attempt = 0; attempt < plan.Count * 120 && got < plan.Count; attempt++)
                {
                    float x = ((float)rnd.NextDouble() - 0.5f) * 2f * 44f, z = ((float)rnd.NextDouble() - 0.5f) * 2f * 44f;
                    float dens = vine ? CavernFloraMath.VineDensity(x, z) : CavernFloraMath.Density(plan.Kind, x, z);
                    if (dens <= 0.02f || rnd.NextDouble() > dens) continue;
                    var p = new Vector2(x, z);
                    if (Vector2.Distance(p, new Vector2(spawn.x, spawn.z)) < 2.5f) continue;         // the slime's birth place stays clear
                    bool near = false;
                    foreach (var o in mine) if (Vector2.Distance(p, o) < plan.Spacing) { near = true; break; }
                    if (!near && !vine) foreach (var t in taken) if (Vector2.Distance(p, t) < 1.3f) { near = true; break; }   // not on top of food
                    if (near) continue;
                    var prefab = prefabs[$"Flora_{plan.Model}_{rnd.Next(plan.Variants)}"];
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, group);
                    float y = vine ? CavernLayout.CeilingY(x, z) + 0.3f : CavernLayout.GroundHeight(x, z) - 0.03f;
                    go.transform.position = new Vector3(x, y, z);
                    go.transform.rotation = Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f);
                    go.transform.localScale = Vector3.one * Mathf.Lerp(plan.ScaleLo, plan.ScaleHi, (float)rnd.NextDouble());
                    foreach (var r in go.GetComponentsInChildren<Renderer>(true)) if (r.name.Contains("Glow")) r.sharedMaterial = glow;
                    foreach (var tr in go.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(tr.gameObject, StaticEditorFlags.OccludeeStatic | StaticEditorFlags.ReflectionProbeStatic);
                    mine.Add(p);
                    got++;
                }
                total += got;
                summary.Add($"{plan.Model} {got}");
            }
            EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"Thảm thực vật hang: {total} ({string.Join(", ", summary)}). Ctrl+S để lưu scene.");
        }

        [MenuItem("Evolve/Restore Cavern Flora (undo)")]
        public static void Restore()
        {
            var old = GameObject.Find(RootName);
            if (old != null) { Object.DestroyImmediate(old); EditorSceneManager.MarkAllScenesDirty(); Debug.Log("Đã gỡ thảm thực vật hang. Ctrl+S để lưu scene."); }
        }

        static Material GlowMaterial()
        {
            const string path = "Assets/_Project/Materials/Cavern_Glow_Cluster.mat";
            var shader = Shader.Find("Evolve/RockVertexColor");
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { mat = new Material(shader) { name = "Cavern_Glow_Cluster" }; AssetDatabase.CreateAsset(mat, path); }
            mat.shader = shader;
            mat.SetColor("_Tint", new Color(0.55f, 0.95f, 0.85f, 1f));
            mat.SetFloat("_Glow", 1.3f);
            EditorUtility.SetDirty(mat);
            return mat;
        }
    }
}
