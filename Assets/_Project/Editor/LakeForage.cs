using System;
using System.Collections.Generic;
using Evolve.Ecology;
using Evolve.Survival;
using Evolve.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Evolve.EditorTools
{
    /// <summary>
    /// Evolve -> Dress Lake: Forage. More things a slime can absorb, scattered by zone: six plants (water lily, cattail, bitter fern, fat mushroom, sweet root,
    /// ice bloom) and four minerals (lime crystal, iron nodule, salt crust, glow quartz), each its own <see cref="FoodKind"/> with its own nutrient recipe
    /// (World/FoodProfile). They are ordinary Nutrient objects (trigger sphere + the Blender model), so eating, regrowth and co-op sync work as for every other food:
    /// plants are a snack that grows back in about 2.5 minutes; minerals feed more per bite, absorb slowly and take 10 minutes to form again.
    /// Rerunnable (the old "Lake Forage (Dressed)" is replaced); undo: Restore Lake Forage.
    /// </summary>
    public static class LakeForage
    {
        const string ModelFolder = "Assets/_Project/Models/Underroot/";
        const string RootName = "Lake Forage (Dressed)";
        const int Seed = 5151;
        const int PerZone = 34;

        struct Kind
        {
            public string Model;
            public FoodKind Food;
            public bool Mineral, Water, Bank;
            public Vector2 Scale;
            public Kind(string model, FoodKind food, bool mineral, bool water, bool bank, float lo, float hi) { Model = model; Food = food; Mineral = mineral; Water = water; Bank = bank; Scale = new Vector2(lo, hi); }
        }

        static readonly Kind[] Kinds =
        {
            new Kind("WaterLily", FoodKind.WaterLily, false, true, false, 0.9f, 1.4f),
            new Kind("Cattail", FoodKind.Cattail, false, false, true, 0.9f, 1.3f),
            new Kind("BitterFern", FoodKind.BitterFern, false, false, false, 0.9f, 1.4f),
            new Kind("FatMushroom", FoodKind.FatMushroom, false, false, false, 0.8f, 1.3f),
            new Kind("SweetRoot", FoodKind.SweetRoot, false, false, false, 0.9f, 1.3f),
            new Kind("IceBloom", FoodKind.IceBloom, false, false, false, 0.8f, 1.3f),
            new Kind("LimeCrystal", FoodKind.LimeCrystal, true, false, false, 0.8f, 1.4f),
            new Kind("IronNodule", FoodKind.IronNodule, true, false, false, 0.8f, 1.5f),
            new Kind("SaltCrust", FoodKind.SaltCrust, true, false, false, 0.9f, 1.4f),
            new Kind("Quartz", FoodKind.GlowQuartz, true, false, false, 0.8f, 1.3f)
        };
        // per zone (ZoneLayout.Zones order): weight of each kind above
        static readonly float[][] Weights =
        {
            //        lily cattail fern fat  sweet ice  lime iron salt quartz
            new[] { 8f, 5f, 0f, 1f, 1f, 0f, 0f, 0f, 3f, 0f },   // 01 shallows
            new[] { 0f, 0f, 3f, 1f, 5f, 0f, 0f, 4f, 0f, 0f },   // 02 root cave
            new[] { 0f, 0f, 3f, 5f, 3f, 0f, 0f, 0f, 1f, 0f },   // 03 mulch bank
            new[] { 0f, 0f, 4f, 2f, 0f, 1f, 0f, 0f, 0f, 3f },   // 04 spore garden
            new[] { 0f, 3f, 0f, 0f, 1f, 5f, 4f, 2f, 0f, 0f },   // 05 water vein
            new[] { 0f, 0f, 1f, 0f, 3f, 0f, 3f, 4f, 0f, 0f }    // 06 hunting edge
        };

        [MenuItem("Evolve/Dress Lake: Forage")]
        public static void Dress()
        {
            var terrain = Terrain.activeTerrain;
            if (terrain == null) { Debug.LogWarning("Không có Terrain trong scene: bỏ qua thực vật và quặng."); return; }
            PropInstaller.EnsureLandmarks();
            var models = new Dictionary<string, GameObject>();
            foreach (var k in Kinds)
            {
                var m = AssetDatabase.LoadAssetAtPath<GameObject>(ModelFolder + "Forage_" + k.Model + ".fbx");
                if (m == null) { Debug.LogWarning("Thiếu Forage_" + k.Model + ".fbx (chạy Reinstall Blender Props rồi thử lại): bỏ qua thực vật và quặng."); return; }
                models[k.Model] = m;
            }
            var old = GameObject.Find(RootName);
            if (old != null) UnityEngine.Object.DestroyImmediate(old);
            var root = new GameObject(RootName).transform;

            var keepClear = new List<Vector2>();
            foreach (var s in UnityEngine.Object.FindObjectsByType<ShelterZone>()) keepClear.Add(new Vector2(s.transform.position.x, s.transform.position.z));
            foreach (var d in UnityEngine.Object.FindObjectsByType<Den>()) keepClear.Add(new Vector2(d.transform.position.x, d.transform.position.z));
            foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>())
                if (t.name.StartsWith("Spawn Point")) keepClear.Add(new Vector2(t.position.x, t.position.z));
            var solids = new List<Bounds>();
            foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>())
                if (t.name.StartsWith("Landmark ")) solids.Add(new Bounds(t.position, new Vector3(11f, 1f, 11f)));
            var foods = new List<Vector2>();
            foreach (var n in UnityEngine.Object.FindObjectsByType<Nutrient>()) foods.Add(new Vector2(n.transform.position.x, n.transform.position.z));

            bool Land(float x, float z, Kind k)
            {
                if (ZoneLayout.InsideCave(x, z, 3f) || Mathf.Max(Mathf.Abs(x), Mathf.Abs(z)) > 215f) return false;
                foreach (var b in solids) if (x >= b.min.x && x <= b.max.x && z >= b.min.z && z <= b.max.z) return false;
                foreach (var c in keepClear) if (Vector2.Distance(new Vector2(x, z), c) < 4f) return false;
                float depth = ZoneLayout.DepthAt(x, z);
                if (k.Water) return depth > 0.08f && depth < 0.42f;                         // lilies float in wading water
                if (depth > 0.05f) return false;                                            // everything else on dry ground
                if (ZoneLayout.DistanceToPath(x, z) < 2f) return false;
                if (k.Bank) return ZoneLayout.DistanceToWater(x, z) < 5f;                   // cattails by the water
                return true;
            }

            var rnd = new System.Random(Seed);
            var placed = new List<Vector2>();
            var quartzGlow = GlowMaterial();
            int total = 0;
            var perKind = new Dictionary<FoodKind, int>();
            for (int zi = 0; zi < ZoneLayout.Zones.Length; zi++)
            {
                var z = ZoneLayout.Zones[zi];
                const float margin = 10f;
                var rect = new Rect(z.Center.x - z.Size.x * 0.5f - margin, z.Center.y - z.Size.y * 0.5f - margin, z.Size.x + margin * 2f, z.Size.y + margin * 2f);
                var group = new GameObject(z.Name).transform;
                group.SetParent(root, false);
                var w = Weights[zi];
                float sum = 0f; foreach (var x in w) sum += x;
                for (int got = 0, attempt = 0; got < PerZone && attempt < PerZone * 80; attempt++)
                {
                    float pick = (float)rnd.NextDouble() * sum;
                    int ki = 0;
                    for (; ki < w.Length - 1 && pick >= w[ki]; ki++) pick -= w[ki];
                    if (w[ki] <= 0f) continue;
                    var kind = Kinds[ki];
                    var p = new Vector2(rect.xMin + (float)rnd.NextDouble() * rect.width, rect.yMin + (float)rnd.NextDouble() * rect.height);
                    if (!Land(p.x, p.y, kind)) continue;
                    bool crowded = false;
                    foreach (var o in placed) if (Vector2.Distance(p, o) < 3.5f) { crowded = true; break; }
                    if (!crowded) foreach (var f in foods) if (Vector2.Distance(p, f) < 2.5f) { crowded = true; break; }
                    if (crowded) continue;

                    float ground = terrain.SampleHeight(new Vector3(p.x, 0f, p.y)) + terrain.transform.position.y;
                    float y = kind.Water ? ZoneLayout.WaterLevel : ground - 0.02f;
                    var go = new GameObject("Forage " + kind.Model);
                    go.transform.SetParent(group, false);
                    go.transform.position = new Vector3(p.x, y, p.y);
                    go.transform.rotation = Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f);
                    go.transform.localScale = Vector3.one * Mathf.Lerp(kind.Scale.x, kind.Scale.y, (float)rnd.NextDouble());
                    var sc = go.AddComponent<SphereCollider>();
                    sc.isTrigger = true;
                    sc.radius = 0.6f;
                    sc.center = new Vector3(0f, 0.35f, 0f);
                    go.AddComponent<Nutrient>();
                    var so = new SerializedObject(go.GetComponent<Nutrient>());
                    so.FindProperty("foodKind").enumValueIndex = (int)kind.Food;
                    so.FindProperty("totalNutrition").floatValue = kind.Mineral ? 24f : 14f;       // a bite of rock is richer than a leaf
                    so.FindProperty("absorbRate").floatValue = kind.Mineral ? 2.5f : 5f;            // but takes longer to digest
                    so.FindProperty("respawnSeconds").floatValue = kind.Mineral ? 600f : 150f;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    var model = (GameObject)PrefabUtility.InstantiatePrefab(models[kind.Model], go.transform);
                    model.transform.localPosition = Vector3.zero;
                    model.transform.localRotation = Quaternion.identity;
                    foreach (var r in model.GetComponentsInChildren<Renderer>(true)) if (r.name.Contains("Glow")) r.sharedMaterial = quartzGlow;
                    foreach (var tr in model.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(tr.gameObject, (StaticEditorFlags)(-1));
                    placed.Add(p);
                    got++; total++;
                    perKind[kind.Food] = perKind.TryGetValue(kind.Food, out int c) ? c + 1 : 1;
                }
            }
            int all = UnityEngine.Object.FindObjectsByType<Nutrient>().Length;
            if (all > FoodSyncLimit.MaxBlobs)
                Debug.LogWarning($"Underroot_Lake có {all} cục thức ăn, vượt giới hạn đồng bộ co-op {FoodSyncLimit.MaxBlobs}: người vào phòng sẽ thấy thức ăn lệch.");
            var summary = new List<string>();
            foreach (var kv in perKind) summary.Add($"{kv.Key} {kv.Value}");
            EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"Thực vật và quặng hấp thụ được: {total} cái ({string.Join(", ", summary)}); tổng {all} cục thức ăn trong scene. Ctrl+S để lưu scene.");
        }

        [MenuItem("Evolve/Restore Lake Forage (undo)")]
        public static void Restore()
        {
            var old = GameObject.Find(RootName);
            if (old != null) { UnityEngine.Object.DestroyImmediate(old); EditorSceneManager.MarkAllScenesDirty(); Debug.Log("Đã gỡ thực vật và quặng thêm. Ctrl+S để lưu scene."); }
        }

        static Material GlowMaterial()
        {
            const string path = "Assets/_Project/Materials/Lake_Glow_Quartz.mat";
            var shader = Shader.Find("Evolve/RockVertexColor");
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { mat = new Material(shader) { name = "Lake_Glow_Quartz" }; AssetDatabase.CreateAsset(mat, path); }
            mat.shader = shader;
            mat.SetColor("_Tint", new Color(0.62f, 0.9f, 1f, 1f));
            mat.SetFloat("_Glow", 1.5f);
            EditorUtility.SetDirty(mat);
            return mat;
        }
    }
}
