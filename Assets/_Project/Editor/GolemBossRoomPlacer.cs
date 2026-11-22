using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Evolve.World;

namespace Evolve.EditorTools
{
    /// <summary>
    /// Menu Evolve → Boss Rooms → Place Golem In The Open Boss Room: with the scene BossRoom_Golem open, puts the golem prefab at Boss_Wait (facing the middle of the room), gives it to the room's BossRoom component
    /// (the field Boss) and saves the scene. Run it with Play stopped. In Play the golem sleeps (inactive) until every living player is inside Activate_Zone.
    /// </summary>
    public static class GolemBossRoomPlacer
    {
        const string GolemPrefab = "Assets/_Project/Prefabs/Wildlife/GolemThachAnh (Meshy).prefab";

        [MenuItem("Evolve/Boss Rooms/Place Golem In The Open Boss Room")]
        static void Place()
        {
            if (EditorApplication.isPlaying) { Debug.LogError("[Phòng Golem] Dừng Play trước."); return; }
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GolemPrefab);
            if (prefab == null) { Debug.LogError("[Phòng Golem] Chưa có " + GolemPrefab + ": chạy menu Build Golem Thạch Anh (Meshy) Prefab trước."); return; }
            Transform wait = null; BossRoom room = null;
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)) if (t.name == "Boss_Wait") { wait = t; break; }
            room = Object.FindFirstObjectByType<BossRoom>(FindObjectsInactive.Include);
            if (wait == null || room == null) { Debug.LogError("[Phòng Golem] Mở cảnh BossRoom_Golem trước (không thấy Boss_Wait hoặc BossRoom)."); return; }
            foreach (var old in Object.FindObjectsByType<Evolve.Combat.CreatureCombat>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (old.name.Contains("GolemThachAnh")) { Debug.LogWarning("[Phòng Golem] Cảnh đã có " + old.name + ": không đặt thêm."); return; }
            var golem = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Vector3 toCentre = room.transform.position - wait.position; toCentre.y = 0f;
            golem.transform.SetPositionAndRotation(wait.position, toCentre.sqrMagnitude > 0.01f ? Quaternion.LookRotation(toCentre) : Quaternion.identity);
            var so = new SerializedObject(room);
            so.FindProperty("boss").objectReferenceValue = golem;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(golem.scene);
            EditorSceneManager.SaveScene(golem.scene);
            Debug.Log("[Phòng Golem] Đã đặt " + golem.name + " tại Boss_Wait và gắn vào BossRoom. Bấm Play, bước vào Activate_Zone để kích hoạt.");
        }
    }
}
