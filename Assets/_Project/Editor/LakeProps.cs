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

        [MenuItem("Evolve/Dress Lake: Props")]
        public static void Dress()
        {
            var terrain = Terrain.activeTerrain;
            if (terrain == null) { Debug.LogWarning("Không có Terrain trong scene: bỏ qua bước vật thể Blender."); return; }
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
            var waters = new List<Bounds>();
            foreach (var w in Object.FindObjectsByType<WaterZone>()) waters.Add(w.GetComponent<Collider>().bounds);

            float WaterDepth(float x, float z)
            {
                foreach (var b in waters)
                    if (x >= b.min.x - 1f && x <= b.max.x + 1f && z >= b.min.z - 1f && z <= b.max.z + 1f) return 1f; // any shallow zone or its shore: keep clear
                return 0f;
            }

            bool Blocked(float x, float z) => !PropPlacement.Allowed(x, z, WaterDepth, ZoneLayout.DistanceToPath, keepClear, 6f);

            var areas = new List<Area> { new Area { Name = "Core", Rect = new Rect(-60f, -60f, 120f, 120f), Count = 40, Sizes = new Vector2(0.3f, 1.6f) } };
            var sizes = new[] { new Vector2(0.6f, 3.2f), new Vector2(0.3f, 1.6f), new Vector2(0.3f, 1.0f), new Vector2(0.6f, 2.4f) };
            for (int i = 0; i < ZoneLayout.Biomes.Length; i++)
            {
                var pad = ZoneLayout.Biomes[i];
                float e = ZoneLayout.ContentExtent;
                areas.Add(new Area
                {
                    Name = pad.Name,
                    Rect = new Rect(pad.Center.x - e, pad.Center.y - e, e * 2f, e * 2f),
                    Count = 60,
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
                    total++;
                }
            }
            EditorSceneManager.MarkSceneDirty(root.scene);
            Debug.Log($"Vật thể Blender: {total} tảng đá trong {areas.Count} vùng.");
        }

        static float NominalSize(int variant) => new[] { 0.3f, 0.6f, 1.0f, 1.6f, 2.4f, 3.2f }[Mathf.Clamp(variant, 0, 5)];
    }
}
