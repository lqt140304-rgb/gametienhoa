using System.Collections.Generic;
using System.IO;
using Evolve.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Evolve.EditorTools
{
    /// <summary>
    /// Evolve -> Dress Lake: Scanned Flora Sample. A 40 m sample strip on the south shore of the lake (zone 01) with every installed scanned asset,
    /// grouped as they would grow (ferns and nettles in the damp edge, moss and creepers on the ground, rocks with moss, a fallen dead trunk, a root
    /// cluster, a young tree under the nearest light hole if one is close), plus two fixed camera spots for before/after screenshots. For judging
    /// quality and FPS (target at least 90 FPS on an RTX 3050 at Full HD). Missing assets are skipped with a warning. Undo: Restore Lake Scanned Flora Sample.
    /// </summary>
    public static class LakeScannedFloraSample
    {
        const string RootName = "Lake Scanned Flora Sample (Dressed)";
        const string ManifestPath = "tools/assets/flora_manifest.json";

        [MenuItem("Evolve/Dress Lake: Scanned Flora Sample")]
        public static void Dress()
        {
            if (Terrain.activeTerrain == null) { Debug.LogWarning("Không có Terrain trong scene: bỏ qua dải mẫu."); return; }
            string manifestFile = Path.Combine(Path.GetDirectoryName(Application.dataPath), ManifestPath);
            var manifest = FloraManifest.Parse(File.Exists(manifestFile) ? File.ReadAllText(manifestFile) : "");
            var prefabs = new Dictionary<string, GameObject>();
            var missing = new List<string>();
            foreach (var a in manifest.assets)
            {
                var p = AssetDatabase.LoadAssetAtPath<GameObject>(FloraDownload.FolderFor(a) + "/" + a.id + ".prefab");
                if (p != null) prefabs[a.id] = p; else missing.Add(a.id);
            }
            if (prefabs.Count == 0) { Debug.LogWarning("Chưa cài tài sản quét: chạy Evolve → Install Scanned Assets trước. Dải mẫu không được dựng."); return; }

            var old = GameObject.Find(RootName);
            if (old != null) Object.DestroyImmediate(old);
            var root = new GameObject(RootName).transform;
            var rnd = new System.Random(2026);

            // the strip: 40 m along the shore, 2 to 14 m back from the water, centred north of zone 01's middle
            var zone = ZoneLayout.Zones[0].Center;
            var origin = new Vector2(zone.x, zone.y);
            var placed = new List<Vector2>();
            int count = 0;
            void Put(string id, int n, float minGap, float waterMin, float waterMax, float scaleLo, float scaleHi)
            {
                if (!prefabs.TryGetValue(id, out var prefab)) return;
                for (int k = 0, tries = 0; k < n && tries < n * 60; tries++)
                {
                    var p = origin + new Vector2(((float)rnd.NextDouble() - 0.5f) * 40f, ((float)rnd.NextDouble() - 0.5f) * 40f);
                    float dw = ZoneLayout.DistanceToWater(p.x, p.y);
                    if (ZoneLayout.DepthAt(p.x, p.y) > 0.05f || dw < waterMin || dw > waterMax || ZoneLayout.DistanceToPath(p.x, p.y) < 2.5f) continue;
                    bool near = false;
                    foreach (var o in placed) if (Vector2.Distance(o, p) < minGap) { near = true; break; }
                    if (near) continue;
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root);
                    go.transform.position = new Vector3(p.x, LakeGround.GroundY(p.x, p.y) - 0.03f, p.y);
                    go.transform.rotation = Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f);
                    go.transform.localScale = Vector3.one * Mathf.Lerp(scaleLo, scaleHi, (float)rnd.NextDouble());
                    placed.Add(p); k++; count++;
                }
            }
            // big things first, then what grows round them
            Put("dead_tree_trunk", 1, 6f, 4f, 14f, 0.9f, 1.1f);
            Put("dead_tree_trunk_02", 1, 6f, 4f, 14f, 0.9f, 1.1f);
            Put("rock_moss_set_01", 3, 3f, 3f, 16f, 0.8f, 1.3f);
            Put("rock_moss_set_02", 3, 3f, 3f, 16f, 0.8f, 1.3f);
            Put("boulder_01", 1, 5f, 6f, 18f, 0.7f, 1f);
            Put("root_cluster_01", 1, 4f, 5f, 18f, 0.8f, 1.1f);
            Put("root_cluster_02", 1, 4f, 5f, 18f, 0.8f, 1.1f);
            Put("tree_stump_01", 1, 4f, 5f, 18f, 0.9f, 1.1f);
            Put("tree_stump_02", 1, 4f, 5f, 18f, 0.9f, 1.1f);
            Put("tree_small_02", 1, 5f, 8f, 20f, 0.8f, 1f);
            Put("fern_02", 14, 1.2f, 0.5f, 8f, 0.8f, 1.3f);            // the damp edge
            Put("nettle_plant", 5, 1.5f, 1f, 9f, 0.8f, 1.2f);
            Put("shrub_02", 4, 2f, 4f, 16f, 0.8f, 1.2f);
            Put("shrub_03", 4, 2f, 4f, 16f, 0.8f, 1.2f);
            Put("moss_01", 18, 0.9f, 0.5f, 14f, 0.8f, 1.4f);
            Put("celandine_01", 10, 0.8f, 2f, 14f, 0.8f, 1.2f);
            Put("periwinkle_plant", 6, 1.2f, 3f, 16f, 0.8f, 1.2f);
            Put("single_root", 3, 2f, 3f, 16f, 0.8f, 1.2f);
            Put("bark_debris_01", 4, 1.5f, 3f, 16f, 0.8f, 1.2f);
            Put("dry_branches_medium_01", 4, 1.5f, 3f, 16f, 0.8f, 1.2f);
            Put("rock_07", 4, 1.5f, 1f, 16f, 0.6f, 1.1f);
            Put("rock_face_01", 1, 5f, 10f, 20f, 0.6f, 0.9f);

            // two fixed camera spots for screenshots (slime height and a higher overview), both looking at the strip
            var target = new Vector3(origin.x, LakeGround.GroundY(origin.x, origin.y) + 0.5f, origin.y);
            foreach (var (name, offset) in new[] { ("Camera Spot 01a", new Vector3(-14f, 1.2f, -10f)), ("Camera Spot 01b", new Vector3(12f, 7f, -16f)) })
            {
                var spot = new GameObject(name);
                spot.transform.SetParent(root, false);
                var pos = target + offset;
                pos.y = LakeGround.GroundY(pos.x, pos.z) + offset.y;
                spot.transform.position = pos;
                spot.transform.rotation = Quaternion.LookRotation(target - pos);
            }
            EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
            Debug.Log($"Dải mẫu tài sản quét: {count} vật ở khu 01, hai góc máy 'Camera Spot 01a/01b' để chụp và đo FPS (mục tiêu ≥ 90)."
                      + (missing.Count > 0 ? $" Thiếu {missing.Count}: {string.Join(", ", missing)}." : ""));
        }

        [MenuItem("Evolve/Restore Lake Scanned Flora Sample (undo)")]
        public static void Restore()
        {
            var old = GameObject.Find(RootName);
            if (old != null) { Object.DestroyImmediate(old); EditorSceneManager.MarkAllScenesDirty(); Debug.Log("Đã gỡ dải mẫu tài sản quét."); }
        }
    }
}
