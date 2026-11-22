using UnityEditor;
using UnityEngine;

namespace Evolve.EditorTools
{
    /// <summary>
    /// Menu "Evolve -> Boss Rooms -> Build Hound Boss Room" &amp; "Validate Hound Boss Room":
    /// Dựng Prefab và Scene cho Phòng Trùm Đợt 2 - Phòng 1: Chó Xương ("Nghĩa Địa Cự Thú").
    /// </summary>
    public static class BossRoomHoundBuilder
    {
        internal static BossRoomSpec Spec => new BossRoomSpec
        {
            RoomName = "BossRoom_Hound",
            ModelFbxPath = BossRoomBuildCommon.ModelFolder + "BossRoom_Hound.fbx",
            PrefabPath = BossRoomBuildCommon.PrefabFolder + "/BossRoom_Hound.prefab",
            ScenePath = BossRoomBuildCommon.Root + "/Scenes/BossRoom_Hound.unity",
            ProfileAssetPath = BossRoomBuildCommon.Root + "/Settings/BossRoomHoundLook.asset",
            RockMatName = "BossRoom_Hound_RockVC",
            GlowMatName = "BossRoom_Hound_EctoGlow",
            BossWaitLocal = new Vector3(0f, 0f, 21.0f),
            AmbientColor = new Color(0.14f, 0.20f, 0.20f),
            FogColor = new Color(0.035f, 0.055f, 0.058f),
            DomeLightColor = new Color(0.45f, 0.85f, 0.78f),
            AccentLightColor = new Color(0.25f, 0.90f, 0.75f),
            BossLightColor = new Color(0.25f, 0.92f, 0.68f),
            MinCeilingRequired = 30.0f,
            RequiresAxeAnchor = false,
            RequiresOpenSkyAboveCenter = false,
            NonColliderObjectNames = new[]
            {
                "Story_HoundClawMarks",
                "Story_HoundSummonBurrows",
                "Story_HoundSummonGlow",
                "Story_BoneSplinters",
                "Boss_Nest_BrokenChain",
            },
            BossPrefabPaths = new[]
            {
                "Assets/_Project/Prefabs/Wildlife/ChoXuong (Meshy).prefab",
                "Assets/_Project/Prefabs/Wildlife/ChoXuong.prefab",
            },
        };

        [MenuItem("Evolve/Boss Rooms/Build Hound Boss Room")]
        public static void BuildInteractive() => Build(interactiveConfirm: true);

        public static bool Build(bool interactiveConfirm) =>
            BossRoomBuildCommon.BuildRoom(Spec, interactiveConfirm, null);

        [MenuItem("Evolve/Boss Rooms/Validate Hound Boss Room")]
        public static void Validate()
        {
            var go = GameObject.Find(Spec.RoomName);
            if (go == null) go = AssetDatabase.LoadAssetAtPath<GameObject>(Spec.PrefabPath);
            if (go == null) { Debug.LogError($"[{Spec.RoomName}] Chưa có scene hoặc prefab."); return; }
            bool ok = BossRoomBuildCommon.ValidateContract(go, Spec, out string report);
            if (ok) Debug.Log($"[{Spec.RoomName}] {report}");
            else Debug.LogError($"[{Spec.RoomName}] {report}");
        }
    }
}
