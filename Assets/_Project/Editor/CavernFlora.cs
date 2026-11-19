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
            new Plan(CavernFloraMath.Kind.Reed, "Reed", 3, 140, 1.0f, 0.9f, 1.4f),
            new Plan(CavernFloraMath.Kind.Moss, "Moss", 3, 190, 0.9f, 0.9f, 1.8f),
            new Plan(CavernFloraMath.Kind.Fern, "Fern", 3, 160, 1.6f, 0.8f, 1.5f),
            new Plan(CavernFloraMath.Kind.Bush, "Bush", 3, 80, 2.2f, 0.9f, 1.5f),
            new Plan(CavernFloraMath.Kind.Mushrooms, "Mushrooms", 3, 90, 1.4f, 0.8f, 1.5f),
            new Plan(CavernFloraMath.Kind.RootArc, "RootArc", 2, 40, 3.0f, 0.9f, 1.5f),
            new Plan(CavernFloraMath.Kind.Vine, "Vine", 2, 26, 3.0f, 0.9f, 1.3f),
            new Plan(CavernFloraMath.Kind.Flower, "Flower", 3, 130, 0.9f, 0.9f, 1.5f),
            new Plan(CavernFloraMath.Kind.Clover, "Clover", 2, 170, 0.7f, 0.9f, 1.6f),
            new Plan(CavernFloraMath.Kind.Leaves, "Leaves", 3, 150, 0.8f, 0.9f, 1.6f),
            new Plan(CavernFloraMath.Kind.Twigs, "Twigs", 2, 60, 1.5f, 0.9f, 1.4f),
            new Plan(CavernFloraMath.Kind.Pebbles, "Pebbles", 3, 120, 0.8f, 0.8f, 1.6f),
            new Plan(CavernFloraMath.Kind.Lichen, "Lichen", 2, 80, 0.9f, 0.9f, 1.7f)
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
            var glow = GlowMaterial("Cavern_Glow_Cluster", new Color(0.55f, 0.95f, 0.85f));
            var bloom = new[] { GlowMaterial("Cavern_Glow_Bloom_0", new Color(0.45f, 0.95f, 0.90f)), GlowMaterial("Cavern_Glow_Bloom_1", new Color(0.75f, 0.50f, 1.00f)), GlowMaterial("Cavern_Glow_Bloom_2", new Color(1.00f, 0.72f, 0.35f)) };

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
                    int variant = rnd.Next(plan.Variants);
                    var prefab = prefabs[$"Flora_{plan.Model}_{variant}"];
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, group);
                    float y = vine ? CavernLayout.CeilingY(x, z) + 0.3f : CavernLayout.GroundHeight(x, z) - 0.03f;
                    go.transform.position = new Vector3(x, y, z);
                    go.transform.rotation = Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f);
                    go.transform.localScale = Vector3.one * Mathf.Lerp(plan.ScaleLo, plan.ScaleHi, (float)rnd.NextDouble());
                    foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                    {
                        if (r.name.Contains("Bloom Glow")) r.sharedMaterial = bloom[variant % bloom.Length];      // each kind of bellflower its own colour
                        else if (r.name.Contains("Glow")) r.sharedMaterial = glow;
                    }
                    foreach (var tr in go.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(tr.gameObject, StaticEditorFlags.OccludeeStatic | StaticEditorFlags.ReflectionProbeStatic);
                    AddPhysics(go, plan.Kind);
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

        /// <summary>Physics of the vegetation: root arcs are solid (the slime goes round or under them, their own mesh is the collider); ferns, reeds and bushes are soft (a trigger
        /// that slows a slime pushing through, see World/SoftPlant); the rest is too small or too thin to matter.</summary>
        static void AddPhysics(GameObject go, CavernFloraMath.Kind kind)
        {
            switch (kind)
            {
                case CavernFloraMath.Kind.RootArc:
                    foreach (var mf in go.GetComponentsInChildren<MeshFilter>(true))
                    {
                        if (mf.sharedMesh == null || mf.name.EndsWith("Leaves")) continue;    // the roots, not the little leaves on them
                        var mc = mf.gameObject.AddComponent<MeshCollider>();
                        mc.sharedMesh = mf.sharedMesh;
                        mc.convex = false;
                    }
                    break;
                case CavernFloraMath.Kind.Fern: AddSoft(go, 0.55f, 0.6f, 0.3f); break;
                case CavernFloraMath.Kind.Reed: AddSoft(go, 0.4f, 1.4f, 0.45f); break;
                case CavernFloraMath.Kind.Bush: AddSoft(go, 0.45f, 0.9f, 0.8f); break;
            }
        }

        static void AddSoft(GameObject go, float radius, float height, float stiffness)
        {
            var cap = go.AddComponent<CapsuleCollider>();
            cap.isTrigger = true;
            cap.radius = radius;
            cap.height = Mathf.Max(height, radius * 2f);
            cap.center = new Vector3(0f, cap.height * 0.5f, 0f);
            go.AddComponent<SoftPlant>().Stiffness = stiffness;
        }

        [MenuItem("Evolve/Restore Cavern Flora (undo)")]
        public static void Restore()
        {
            var old = GameObject.Find(RootName);
            if (old != null) { Object.DestroyImmediate(old); EditorSceneManager.MarkAllScenesDirty(); Debug.Log("Đã gỡ thảm thực vật hang. Ctrl+S để lưu scene."); }
        }

        static Material GlowMaterial(string name, Color tint)
        {
            string path = $"Assets/_Project/Materials/{name}.mat";
            var shader = Shader.Find("Evolve/RockVertexColor");
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { mat = new Material(shader) { name = name }; AssetDatabase.CreateAsset(mat, path); }
            mat.shader = shader;
            mat.SetColor("_Tint", new Color(tint.r, tint.g, tint.b, 1f));
            mat.SetFloat("_Glow", 1.3f);
            EditorUtility.SetDirty(mat);
            return mat;
        }
    }
}
