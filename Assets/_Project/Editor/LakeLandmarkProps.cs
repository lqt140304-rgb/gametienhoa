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

        [MenuItem("Evolve/Bản đồ cũ (Underroot)/Dress Lake: Landmark Props")]
        public static void Dress()
        {
            if (Terrain.activeTerrain == null) { Debug.LogWarning("Không có Terrain trong scene: bỏ qua điểm nhấn Blender."); return; }
            PropInstaller.EnsureLandmarks();
            if (!PropInstaller.LandmarksPresent()) { Debug.LogWarning("Thiếu FBX điểm nhấn trong " + ModelFolder + " (xem tools/blender/out): giữ bản dựng bằng code."); return; }

            // load every model first: only when all of them are there do the code-built versions go
            var all = new[] { "Log_Hollow", "Mushroom_Giant_0", "Mushroom_Giant_1", "Mushroom_Giant_2", "Mushroom_Small_0", "Mushroom_Small_1", "Mushroom_Small_2", "Mushroom_Small_3", "RootArch",
                "Cliff_0", "Cliff_1", "Cliff_2", "Cliff_3", "HangRoot_0", "HangRoot_1", "HangRoot_2", "HangRoot_3" };
            foreach (var name in all)
                if (AssetDatabase.LoadAssetAtPath<GameObject>(ModelFolder + name + ".fbx") == null)
                {
                    Debug.LogWarning("Không nạp được " + name + ".fbx (chưa nhập xong?): giữ bản dựng bằng code. Thử Assets → Reimport rồi chạy lại.");
                    return;
                }

            DestroyByName(RootName);
            DestroyByName(LogName);
            DestroyByName(MushroomsName);
            DestroyByName(ArchName);
            var root = new GameObject(RootName).transform;
            int count = 0;

            // ---- 03 hollow log ----
            var c = LandmarkLayout.LogCenter;
            // sit the 9 m log on the LOWEST of its two ends and middle, sunk 0.3 m: the bore floor is then at or below the ground
            // at both openings (no rim step for a slime to catch on), whatever the slope
            var logDir = new Vector2(Mathf.Cos(LandmarkLayout.LogAngle * Mathf.Deg2Rad), Mathf.Sin(LandmarkLayout.LogAngle * Mathf.Deg2Rad));
            float logY = Mathf.Min(LakeGround.GroundY(c.x, c.y),
                Mathf.Min(LakeGround.GroundY(c.x + logDir.x * LandmarkLayout.LogLength * 0.5f, c.y + logDir.y * LandmarkLayout.LogLength * 0.5f),
                          LakeGround.GroundY(c.x - logDir.x * LandmarkLayout.LogLength * 0.5f, c.y - logDir.y * LandmarkLayout.LogLength * 0.5f))) - 0.3f;
            var log = Place("Log_Hollow", root, c, Quaternion.Euler(0f, -LandmarkLayout.LogAngle, 0f), 1f, 0f, ref count, logY);

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

            PlaceRimCliffs(root, ref count);
            PlaceHangingRoots(root, ref count);

            var scene = EditorSceneManager.GetActiveScene();
            if (scene.IsValid()) EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"Điểm nhấn Blender: đã thay khúc gỗ, nấm khổng lồ và cung rễ bằng {count} vật thể. Bấm Ctrl+S để lưu scene.");
        }

        // ---- the cave rim: slabs of layered limestone standing in the cliff skin all round, except at the hunter's way in ----
        static void PlaceRimCliffs(Transform root, ref int count)
        {
            var parent = new GameObject("Rim Cliffs").transform;
            parent.SetParent(root, false);
            var rnd = new System.Random(8080);
            const float step = 24f, edge = 243f; // slabs are 28 m wide: they overlap a little, no seams
            Vector2 gate = ZoneLayout.NorthEntry;
            int made = 0;
            for (float t = -238f; t <= 238f; t += step)
                for (int side = 0; side < 4; side++)
                {
                    Vector2 p = side == 0 ? new Vector2(t, edge) : side == 1 ? new Vector2(t, -edge) : side == 2 ? new Vector2(edge, t) : new Vector2(-edge, t);
                    if (side == 0 && Mathf.Abs(p.x - gate.x) < 32f) continue; // keep the way in open, and the ramp up to it (the ground drops there)
                    float yaw = side < 2 ? 0f : 90f;
                    var go = Place("Cliff_" + rnd.Next(4), parent, p, Quaternion.Euler(0f, yaw, 0f), 1f, -5f, ref count);
                    if (go != null) { go.transform.localScale = new Vector3(1f, 0.8f, 1f); made++; }
                }
            // two flank slabs that end exactly where the ramp ends
            foreach (float dx in new[] { -32f, 32f })
            {
                var go = Place("Cliff_" + rnd.Next(4), parent, new Vector2(gate.x + dx, edge), Quaternion.identity, 1f, -5f, ref count);
                if (go != null) { go.transform.localScale = new Vector3(1f, 0.8f, 1f); made++; }
            }
            Debug.Log("Vách rìa Blender: " + made + " tấm.");
        }

        // ---- roots hanging from the roof (no collision: they are up in the dark) ----
        static void PlaceHangingRoots(Transform root, ref int count)
        {
            var parent = new GameObject("Blender Hanging Roots").transform; // not "Hanging Roots": LakeLayout looks that name up for the code-built ones
            parent.SetParent(root, false);
            var rnd = new System.Random(7070);
            int made = 0;
            for (int i = 0; i < 90; i++)
            {
                float x = ((float)rnd.NextDouble() - 0.5f) * 440f, z = ((float)rnd.NextDouble() - 0.5f) * 440f;
                float yaw = (float)rnd.NextDouble() * 360f;
                int variant = rnd.Next(4);
                if (ZoneLayout.InsideCave(x, z, 6f)) continue;
                bool inShaft = false;
                foreach (var s in ZoneLayout.LightShafts) if (Vector2.Distance(new Vector2(x, z), s) < 11f) { inShaft = true; break; } // 8 m hole + the side roots' reach
                if (inShaft) continue;
                float ceiling = LakeLayout.CeilingY(x, z, 28f);
                float ground = LakeGround.GroundY(x, z);
                float room = ceiling - ground - 5f;   // hang down to 5 m above the ground at most
                if (room < 5f) continue;
                float k = Mathf.Clamp(Mathf.Min(14f, room) / 10f, 0.5f, 1.4f);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModelFolder + "HangRoot_" + variant + ".fbx");
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                go.transform.position = new Vector3(x, ceiling + 0.3f, z);
                go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
                go.transform.localScale = Vector3.one * k;
                foreach (var t in go.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(t.gameObject, (StaticEditorFlags)(-1));
                made++;
                count++;
            }
            Debug.Log("Rễ buông mái Blender: " + made + " cây.");
        }

        static GameObject Place(string model, Transform parent, Vector2 xz, Quaternion rotation, float scale, float sink, ref int count, float? absoluteY = null)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModelFolder + model + ".fbx");
            if (prefab == null) { Debug.LogWarning("Không nạp được " + model + ".fbx"); return null; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.transform.position = new Vector3(xz.x, absoluteY ?? (LakeGround.GroundY(xz.x, xz.y) + sink), xz.y);
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
