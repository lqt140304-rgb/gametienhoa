using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Evolve.World;

namespace Evolve.EditorTools
{
    /// <summary>
    /// Menu Evolve → Boss Rooms → Place Scorpion In The Open Boss Room: with the scene BossRoom_Scorpion open, puts the scorpion prefab at Boss_Wait (facing the middle of the room), gives it to the room's
    /// BossRoom component (the field Boss) and saves the scene. Run it with Play stopped, after Evolve → Build Bọ Cạp Thạch Anh (Meshy) Prefab. In Play the scorpion sleeps (inactive) until every living
    /// player is inside Activate_Zone; when it wakes it learns the room's boulders (ScorpionRoom): they stop its underground mound.
    /// </summary>
    public static class ScorpionBossRoomPlacer
    {
        const string ScorpionPrefab = "Assets/_Project/Prefabs/Wildlife/BoCapThachAnh (Meshy).prefab";

        [MenuItem("Evolve/Boss Rooms/Place Scorpion In The Open Boss Room")]
        static void Place()
        {
            if (EditorApplication.isPlaying) { Debug.LogError("[Phòng Bọ Cạp] Dừng Play trước."); return; }
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ScorpionPrefab);
            if (prefab == null) { Debug.LogError("[Phòng Bọ Cạp] Chưa có " + ScorpionPrefab + ": chạy menu Build Bọ Cạp Thạch Anh (Meshy) Prefab trước."); return; }
            Transform wait = null;
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)) if (t.name == "Boss_Wait") { wait = t; break; }
            var room = Object.FindFirstObjectByType<BossRoom>(FindObjectsInactive.Include);
            if (wait == null || room == null) { Debug.LogError("[Phòng Bọ Cạp] Mở cảnh BossRoom_Scorpion trước (không thấy Boss_Wait hoặc BossRoom)."); return; }
            foreach (var old in Object.FindObjectsByType<Evolve.Combat.CreatureCombat>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (old.name.Contains("BoCapThachAnh")) { Debug.LogWarning("[Phòng Bọ Cạp] Cảnh đã có " + old.name + ": không đặt thêm."); return; }
            var scorpion = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Vector3 toCentre = room.transform.position - wait.position; toCentre.y = 0f;
            scorpion.transform.SetPositionAndRotation(wait.position, toCentre.sqrMagnitude > 0.01f ? Quaternion.LookRotation(toCentre) : Quaternion.identity);
            var so = new SerializedObject(room);
            so.FindProperty("boss").objectReferenceValue = scorpion;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scorpion.scene);
            EditorSceneManager.SaveScene(scorpion.scene);
            Debug.Log("[Phòng Bọ Cạp] Đã đặt " + scorpion.name + " tại Boss_Wait và gắn vào BossRoom. Bấm Play, bước vào Activate_Zone để kích hoạt.");
        }
    }
}
