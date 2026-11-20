using System.Collections.Generic;
using Evolve.Ecology;
using Evolve.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Evolve.EditorTools
{
    /// <summary>
    /// Places the solid Blender rocks (Models/Underroot/Rock_0..5.fbx, each with its own collider) around the lake zone.
    /// Menu: Evolve -> Dress Lake: Props. Rerunnable: the previous "Lake Props (Dressed)" object is replaced.
    /// Rocks stay off swimming-depth water, off the trails, and away from shelters, dens and spawn points.
    /// </summary>
    public static class LakeProps
    {
        const string ModelFolder = "Assets/_Project/Models/Underroot/";
        const string RootName = "Lake Props (Dressed)";
        const int Seed = 9001;
        const int MaxRocks = 500;

        struct Area
        {
            public string Name;
            public Rect Rect;
            public int Count;
            public Vector2 Sizes;
        }

        [MenuItem("Evolve/Bản đồ cũ (Underroot)/Dress Lake: Props")]
        public static void Dress()
        {
            var terrain = Terrain.activeTerrain;
            if (terrain == null) { Debug.LogWarning("Không có Terrain trong scene: bỏ qua bước vật thể Blender."); return; }
            PropInstaller.EnsureRocks(); // writes Rock_N.fbx from the tools/blender/out text copies when missing
            var prefabs = new List<GameObject>();
            for (int i = 0; i < 6; i++)
            {
                var p = AssetDatabase.LoadAssetAtPath<GameObject>(ModelFolder + "Rock_" + i + ".fbx");
                if (p != null) prefabs.Add(p);
            }
            if (prefabs.Count == 0) { Debug.LogWarning("Chưa có Rock_*.fbx trong " + ModelFolder + " (chạy tools/blender/build_rocks.py và kéo FBX vào). Bỏ qua."); return; }

            var old = GameObject.Find(RootName);
            if (old != null) Object.DestroyImmediate(old);
            var root = new GameObject(RootName);
            Physics.SyncTransforms();

            var keepClear = new List<Vector2>();
            foreach (var s in Object.FindObjectsByType<ShelterZone>()) keepClear.Add(new Vector2(s.transform.position.x, s.transform.position.z));
            foreach (var d in Object.FindObjectsByType<Den>()) keepClear.Add(new Vector2(d.transform.position.x, d.transform.position.z));
            foreach (var t in Object.FindObjectsByType<Transform>())
                if (t.name.StartsWith("Spawn Point")) keepClear.Add(new Vector2(t.position.x, t.position.z));
            float WaterDepth(float x, float z)
            {
                float ground = terrain.SampleHeight(new Vector3(x, 0f, z)) + terrain.transform.position.y;
                return Mathf.Max(0f, ZoneLayout.WaterLevel - ground); // lake, stream, pools and puddle are carved into the terrain
            }

            // Same exclusions as the vegetation builder: landmark pillars and food; never inside the Great Hall (cave).
            var solids = new List<Bounds>();
            foreach (var t in Object.FindObjectsByType<Transform>())
                if (t.name.StartsWith("Landmark ")) solids.Add(new Bounds(t.position, new Vector3(11f, 1f, 11f)));
            foreach (var n in Object.FindObjectsByType<Nutrient>()) solids.Add(new Bounds(n.transform.position, new Vector3(6f, 6f, 6f)));

            bool Blocked(float x, float z)
            {
                if (ZoneLayout.InsideCave(x, z, 4f)) return true;
                foreach (var b in solids)
                    if (x >= b.min.x && x <= b.max.x && z >= b.min.z && z <= b.max.z) return true;
                return !PropPlacement.Allowed(x, z, WaterDepth, ZoneLayout.DistanceToPath, keepClear, 6f);
            }

            // One area per map zone (01..06), the zone's core rectangle plus a 15 m margin; sizes follow each zone's character.
            var sizes = new[]
            {
                new Vector2(0.3f, 1.6f), // 01 life shallows: small polished stones
                new Vector2(0.6f, 3.2f), // 02 ancient root cave: big boulders
                new Vector2(0.3f, 1.6f), // 03 root mulch bank
                new Vector2(0.3f, 1.0f), // 04 spore garden: few, small
                new Vector2(0.6f, 2.4f), // 05 water vein gorge
                new Vector2(0.6f, 3.2f)  // 06 predator edge
            };
            var areas = new List<Area>();
            for (int i = 0; i < ZoneLayout.Zones.Length; i++)
            {
                var z = ZoneLayout.Zones[i];
                const float margin = 15f;
                areas.Add(new Area
                {
                    Name = z.Name,
                    Rect = new Rect(z.Center.x - z.Size.x * 0.5f - margin, z.Center.y - z.Size.y * 0.5f - margin, z.Size.x + margin * 2f, z.Size.y + margin * 2f),
                    Count = 80,
                    Sizes = sizes[i % sizes.Length]
                });
            }

            int total = 0;
            for (int a = 0; a < areas.Count && total < MaxRocks; a++)
            {
                var group = new GameObject(areas[a].Name).transform;
                group.SetParent(root.transform, false);
                var spots = PropPlacement.Rocks(Seed + a, areas[a].Rect, Mathf.Min(areas[a].Count, MaxRocks - total), Blocked, prefabs.Count, areas[a].Sizes);
                foreach (var s in spots)
                {
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[s.Variant % prefabs.Count], group);
                    float y = terrain.SampleHeight(new Vector3(s.Position.x, 0f, s.Position.y)) + terrain.transform.position.y;
                    go.transform.position = new Vector3(s.Position.x, y - 0.05f * s.Size, s.Position.y); // sit slightly in the ground
                    go.transform.rotation = Quaternion.Euler(0f, s.YawDegrees, 0f);
                    float k = s.Size / Mathf.Max(0.01f, NominalSize(s.Variant));
                    go.transform.localScale = Vector3.one * k;
                    EnsureCollision(go, total == 0);
                    total++;
                }
            }
            EditorSceneManager.MarkSceneDirty(root.scene);
            Debug.Log($"Vật thể Blender: {total} tảng đá trong {areas.Count} khu.");
        }

        /// <summary>
        /// Makes sure the placed rock is solid whatever the import did: every "_COL" child gets a MeshCollider (and no renderer);
        /// if the FBX has no collider mesh, the rock's own mesh becomes the collider (a rock must never be walkable-through).
        /// </summary>
        internal static void EnsureCollision(GameObject go, bool log)
        {
            var filters = go.GetComponentsInChildren<MeshFilter>(true);
            bool hasCollider = false;
            foreach (var f in filters)
            {
                if (f.sharedMesh == null || !PropNaming.IsCollider(f.gameObject.name)) continue;
                hasCollider = true;
                var mc = ColliderOn(f.gameObject);
                mc.sharedMesh = f.sharedMesh;
                mc.convex = false;
                var r = f.GetComponent<Renderer>();
                if (r != null) r.enabled = false; // the simplified collider mesh is never drawn
            }
            if (!hasCollider)
                foreach (var f in filters)
                {
                    if (f.sharedMesh == null) continue;
                    var mc = ColliderOn(f.gameObject);
                    mc.sharedMesh = f.sharedMesh;
                    mc.convex = false;
                }
            foreach (var t in go.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(t.gameObject, (StaticEditorFlags)(-1));
            if (log)
            {
                var names = new List<string>();
                foreach (var f in filters) names.Add($"{f.gameObject.name} ({(f.sharedMesh != null ? f.sharedMesh.triangles.Length / 3 : 0)} mặt, {(f.GetComponent<Renderer>() != null && f.GetComponent<Renderer>().enabled ? "vẽ" : "không vẽ")}, {(f.GetComponent<Collider>() != null ? "có va chạm" : "không va chạm")})");
                Debug.Log("Cấu trúc đá mẫu " + go.name + ": " + string.Join("; ", names) + (hasCollider ? "" : " [KHÔNG thấy mesh _COL: dùng chính mesh đá làm va chạm]"));
            }
        }

        // `??` bypasses Unity's own null check (GetComponent returns a fake null in the Editor), so use TryGetComponent.
        static MeshCollider ColliderOn(GameObject go) => go.TryGetComponent<MeshCollider>(out var existing) ? existing : go.AddComponent<MeshCollider>();

        static float NominalSize(int variant) => new[] { 0.3f, 0.6f, 1.0f, 1.6f, 2.4f, 3.2f }[Mathf.Clamp(variant, 0, 5)];
    }
}
