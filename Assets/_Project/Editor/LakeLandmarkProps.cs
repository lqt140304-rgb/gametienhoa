using Evolve.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Evolve.EditorTools
{
    /// <summary>
    /// Replaces the code-built landmarks of "Dress Lake: Landmarks" (03 rotting log, 04 giant mushrooms, 06 root arch) with the
    /// Blender versions (Models/Underroot/Log_Hollow, Mushroom_Giant_N, Mushroom_Small_N, RootArch) at the same places.
    /// Menu: Evolve -> Dress Lake: Landmark Props. Run it after Landmarks. If a model is missing nothing is removed.
    /// </summary>
    public static class LakeLandmarkProps
    {
        const string ModelFolder = "Assets/_Project/Models/Underroot/";
        const string RootName = "Lake Landmark Props (Dressed)";
        const string LogName = "03 Rotting Log", MushroomsName = "04 Giant Mushrooms", ArchName = "06 Root Arch";

        [MenuItem("Evolve/Dress Lake: Landmark Props")]
        public static void Dress()
        {
            if (Terrain.activeTerrain == null) { Debug.LogWarning("Không có Terrain trong scene: bỏ qua điểm nhấn Blender."); return; }
            PropInstaller.EnsureLandmarks();
            if (!PropInstaller.LandmarksPresent()) { Debug.LogWarning("Thiếu FBX điểm nhấn trong " + ModelFolder + " (xem tools/blender/out): giữ bản dựng bằng code."); return; }

            DestroyByName(RootName);
            DestroyByName(LogName);
            DestroyByName(MushroomsName);
            DestroyByName(ArchName);
            var root = new GameObject(RootName).transform;
            int count = 0;

            // ---- 03 hollow log ----
            var c = LandmarkLayout.LogCenter;
            var log = Place("Log_Hollow", root, c, Quaternion.Euler(0f, -LandmarkLayout.LogAngle, 0f), 1f, -0.12f, ref count);

            // ---- 04 giant mushrooms (3 giants + 14 smaller ones, same layout seed as the code version) ----
            var m = LandmarkLayout.GiantMushrooms;
            Place("Mushroom_Giant_0", root, m, Quaternion.identity, 1f, -0.05f, ref count);
            Place("Mushroom_Giant_1", root, m + new Vector2(6.5f, 2.5f), Quaternion.Euler(0f, 70f, 0f), 1f, -0.05f, ref count);
            Place("Mushroom_Giant_2", root, m + new Vector2(-3f, 6f), Quaternion.Euler(0f, 150f, 0f), 1f, -0.05f, ref count);
            var rnd = new System.Random(4040);
            for (int i = 0; i < 14; i++)
            {
                float a = (float)rnd.NextDouble() * Mathf.PI * 2f, d = 5f + (float)rnd.NextDouble() * 10f;
                var p = m + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * d;
                if (ZoneLayout.DistanceToPath(p.x, p.y) < ZoneLayout.PathWidth) continue;
                int variant = i < 5 ? 2 + rnd.Next(2) : rnd.Next(2);
                Place("Mushroom_Small_" + variant, root, p, Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f), 1f, -0.05f, ref count);
            }
            var glow = new GameObject("Under-cap Glow");
            glow.transform.SetParent(root, false);
            glow.transform.position = new Vector3(m.x, LakeGround.GroundY(m.x, m.y) + 3f, m.y);
            var l = glow.AddComponent<Light>();
            l.type = LightType.Point;
            l.range = 16f;
            l.intensity = 0.9f;
            l.color = new Color(1f, 0.9f, 0.62f);
            l.shadows = LightShadows.None;

            // ---- 06 root arch at the hunter's way in (same spot as the code version; the clear gap lies along the span axis) ----
            Vector2 from = ZoneLayout.HuntEdge.Center, to = ZoneLayout.NorthEntry;
            Vector2 dir = (to - from).normalized, side = new Vector2(-dir.y, dir.x);
            Vector2 at = from + dir * Mathf.Min(Vector2.Distance(from, to) - 20f, 40f);
            float yaw = -Mathf.Atan2(side.y, side.x) * Mathf.Rad2Deg; // the arch spans its local X axis: point that along `side`
            Place("RootArch", root, at, Quaternion.Euler(0f, yaw, 0f), 1f, -0.1f, ref count);

            var scene = EditorSceneManager.GetActiveScene();
            if (scene.IsValid()) EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"Điểm nhấn Blender: đã thay khúc gỗ, nấm khổng lồ và cung rễ bằng {count} vật thể. Bấm Ctrl+S để lưu scene.");
        }

        static GameObject Place(string model, Transform parent, Vector2 xz, Quaternion rotation, float scale, float sink, ref int count)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModelFolder + model + ".fbx");
            if (prefab == null) { Debug.LogWarning("Không nạp được " + model + ".fbx"); return null; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.transform.position = new Vector3(xz.x, LakeGround.GroundY(xz.x, xz.y) + sink, xz.y);
            go.transform.rotation = rotation;
            go.transform.localScale = Vector3.one * scale;
            LakeProps.EnsureCollision(go, count == 0);
            count++;
            return go;
        }

        static void DestroyByName(string name)
        {
            var go = GameObject.Find(name);
            if (go != null) Object.DestroyImmediate(go);
        }
    }
}
