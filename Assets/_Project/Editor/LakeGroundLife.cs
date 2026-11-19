using System;
using System.Collections.Generic;
using Evolve.Ecology;
using Evolve.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Evolve.EditorTools
{
    /// <summary>
    /// Fills the ground level with life and light: luminous mushroom clusters, glowing crystal clusters, lantern flowers and dark root stubs, in each
    /// zone's own colour of light (01 teal, 02 amber, 03 yellow-green, 04 violet, 05 ice blue, 06 dim red), plus little beacons along the trails.
    /// About a third of the glowing items carry a small light (at most 90; they fade with sight and switch off beyond 60 m).
    /// Menu: Evolve -> Dress Lake: Ground Life (rerunnable); undo: Restore Lake Ground Life.
    /// </summary>
    public static class LakeGroundLife
    {
        const string ModelFolder = "Assets/_Project/Models/Underroot/";
        const string RootName = "Lake Ground Life (Dressed)";
        const string ShaderName = "Evolve/RockVertexColor";
        const int Seed = 4242;
        const int MaxLights = 90;

        static readonly string[] Names =
        {
            "LumaShroom_0", "LumaShroom_1", "LumaShroom_2", "CrystalCluster_0", "CrystalCluster_1", "CrystalCluster_2",
            "BulbPlant_0", "BulbPlant_1", "RootStub_0", "RootStub_1"
        };
        // zone light colours, in ZoneLayout.Zones order
        static readonly Color[] ZoneLight =
        {
            new Color(0.25f, 0.95f, 0.85f), new Color(1.00f, 0.62f, 0.22f), new Color(0.72f, 0.92f, 0.28f),
            new Color(0.72f, 0.38f, 1.00f), new Color(0.55f, 0.82f, 1.00f), new Color(1.00f, 0.32f, 0.22f)
        };
        // per zone: weights of (mushrooms, crystals, bulb plants, root stubs) and how many items
        static readonly float[][] Weights =
        {
            new[] { 3f, 1f, 2f, 1f },   // 01 shallows
            new[] { 2f, 2f, 1f, 3f },   // 02 root cave
            new[] { 4f, 0f, 2f, 3f },   // 03 mulch bank
            new[] { 4f, 0f, 4f, 1f },   // 04 spore garden
            new[] { 1f, 4f, 0f, 1f },   // 05 water vein
            new[] { 1f, 1f, 0f, 3f }    // 06 hunting edge
        };
        static readonly int[] Counts = { 80, 60, 80, 90, 70, 50 };

        [MenuItem("Evolve/Dress Lake: Ground Life")]
        public static void Dress()
        {
            var terrain = Terrain.activeTerrain;
            if (terrain == null) { Debug.LogWarning("Không có Terrain trong scene: bỏ qua sự sống tầng đất."); return; }
            PropInstaller.EnsureLandmarks();
            var prefabs = new Dictionary<string, GameObject>();
            foreach (var n in Names)
            {
                var p = AssetDatabase.LoadAssetAtPath<GameObject>(ModelFolder + n + ".fbx");
                if (p == null) { Debug.LogWarning("Thiếu " + n + ".fbx (chạy Reinstall Blender Props rồi thử lại): bỏ qua sự sống tầng đất."); return; }
                prefabs[n] = p;
            }
            var mats = new Material[ZoneLight.Length];
            for (int i = 0; i < mats.Length; i++) mats[i] = GlowMaterial(i);

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
            foreach (var n in UnityEngine.Object.FindObjectsByType<Nutrient>()) solids.Add(new Bounds(n.transform.position, new Vector3(5f, 5f, 5f)));
            float WaterDepth(float x, float z) => Mathf.Max(0f, ZoneLayout.WaterLevel - (terrain.SampleHeight(new Vector3(x, 0f, z)) + terrain.transform.position.y));
            bool Blocked(float x, float z, bool onTrail)
            {
                if (ZoneLayout.InsideCave(x, z, 3f)) return true;
                foreach (var b in solids) if (x >= b.min.x && x <= b.max.x && z >= b.min.z && z <= b.max.z) return true;
                if (WaterDepth(x, z) > PropPlacement.MaxWadeDepth) return true;
                if (!onTrail && ZoneLayout.DistanceToPath(x, z) < 2f) return true;   // the trail itself stays free
                foreach (var k in keepClear) if (Vector2.Distance(new Vector2(x, z), k) < 4f) return true;
                return false;
            }

            var rnd = new System.Random(Seed);
            var placed = new List<Vector2>();
            int lights = 0, made = 0, beacons = 0;
            Func<string, Transform, Vector2, int, bool, GameObject> place = (name, parent, p, zone, light) =>
            {
                float y = terrain.SampleHeight(new Vector3(p.x, 0f, p.y)) + terrain.transform.position.y;
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[name], parent);
                bool stub = name.StartsWith("RootStub");
                go.transform.position = new Vector3(p.x, y - (stub ? 0.05f : 0.02f), p.y);
                go.transform.rotation = Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f);
                go.transform.localScale = Vector3.one * (0.8f + 0.7f * (float)rnd.NextDouble());
                foreach (var r in go.GetComponentsInChildren<Renderer>(true)) if (r.name.Contains("Glow")) r.sharedMaterial = mats[zone];
                foreach (var tr in go.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(tr.gameObject, (StaticEditorFlags)(-1));
                if (light && lights < MaxLights)
                {
                    var lg = new GameObject("Glow Light");
                    lg.transform.SetParent(go.transform, false);
                    lg.transform.position = go.transform.position + Vector3.up * (0.7f * go.transform.localScale.y);
                    var l = lg.AddComponent<Light>();
                    l.type = LightType.Point;
                    l.color = ZoneLight[zone];
                    l.range = name.StartsWith("Crystal") ? 9f : 7f;
                    l.intensity = name.StartsWith("Crystal") ? 1.5f : 1.2f;
                    l.shadows = LightShadows.None;
                    var lv = lg.AddComponent<LightByVision>();
                    lv.Pulse = name.StartsWith("Crystal") ? 0f : 0.12f;
                    lights++;
                }
                return go;
            };

            for (int zi = 0; zi < ZoneLayout.Zones.Length; zi++)
            {
                var z = ZoneLayout.Zones[zi];
                const float margin = 12f;
                var rect = new Rect(z.Center.x - z.Size.x * 0.5f - margin, z.Center.y - z.Size.y * 0.5f - margin, z.Size.x + margin * 2f, z.Size.y + margin * 2f);
                var group = new GameObject(z.Name).transform;
                group.SetParent(root, false);
                var w = Weights[zi];
                float total = w[0] + w[1] + w[2] + w[3];
                for (int attempt = 0, got = 0; attempt < Counts[zi] * 25 && got < Counts[zi]; attempt++)
                {
                    var p = new Vector2(rect.xMin + (float)rnd.NextDouble() * rect.width, rect.yMin + (float)rnd.NextDouble() * rect.height);
                    if (Blocked(p.x, p.y, false)) continue;
                    bool near = false;
                    foreach (var o in placed) if (Vector2.Distance(p, o) < 3.2f) { near = true; break; }
                    if (near) continue;
                    float pick = (float)rnd.NextDouble() * total;
                    int kind = 0;
                    for (; kind < 3 && pick >= w[kind]; kind++) pick -= w[kind];
                    string name = kind == 0 ? "LumaShroom_" + rnd.Next(3) : kind == 1 ? "CrystalCluster_" + rnd.Next(3) : kind == 2 ? "BulbPlant_" + rnd.Next(2) : "RootStub_" + rnd.Next(2);
                    place(name, group, p, zi, kind != 3 && rnd.NextDouble() < 0.33);
                    placed.Add(p);
                    got++; made++;
                }
            }

            // beacons along the trails: a luminous cluster every ~16 m, a few metres to the side, alternating sides, in the colour of the zone it stands in
            var trailGroup = new GameObject("Trail Beacons").transform;
            trailGroup.SetParent(root, false);
            foreach (var path in ZoneLayout.Paths)
            {
                float carry = 0f; int side = 1;
                for (int i = 0; i + 1 < path.Length; i++)
                {
                    Vector2 a = path[i], b = path[i + 1];
                    float len = Vector2.Distance(a, b);
                    Vector2 dir = (b - a) / Mathf.Max(0.01f, len), perp = new Vector2(-dir.y, dir.x);
                    for (float s = carry; s < len; s += 16f)
                    {
                        var p = a + dir * s + perp * (4.5f * side + ((float)rnd.NextDouble() - 0.5f) * 1.5f);
                        side = -side;
                        if (Blocked(p.x, p.y, true) || ZoneLayout.DistanceToPath(p.x, p.y) < 2.5f) continue;
                        int zone = 0; float best = -1f;
                        for (int k = 0; k < ZoneLayout.Zones.Length; k++) { float v = ZoneLayout.Influence(k, p.x, p.y); if (v > best) { best = v; zone = k; } }
                        place(rnd.NextDouble() < 0.5 ? "LumaShroom_" + rnd.Next(3) : "CrystalCluster_" + rnd.Next(3), trailGroup, p, zone, true);
                        beacons++;
                    }
                    carry = (carry + len) % 16f;
                }
            }
            EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"Sự sống tầng đất: {made} cụm, {beacons} đèn dẫn đường dọc đường mòn, {lights} đèn nhỏ. Ctrl+S để lưu scene.");
        }

        [MenuItem("Evolve/Restore Lake Ground Life (undo)")]
        public static void Restore()
        {
            var old = GameObject.Find(RootName);
            if (old != null) { UnityEngine.Object.DestroyImmediate(old); EditorSceneManager.MarkAllScenesDirty(); Debug.Log("Đã gỡ sự sống tầng đất. Ctrl+S để lưu scene."); }
        }

        static Material GlowMaterial(int zone)
        {
            string path = $"Assets/_Project/Materials/Lake_Glow_{zone + 1}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader = Shader.Find(ShaderName);
            if (mat == null)
            {
                mat = new Material(shader) { name = $"Lake_Glow_{zone + 1}" };
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = shader;
            var c = ZoneLight[zone];
            mat.SetColor("_Tint", new Color(c.r, c.g, c.b, 1f));
            mat.SetFloat("_Glow", 1.6f);
            EditorUtility.SetDirty(mat);
            return mat;
        }
    }
}
