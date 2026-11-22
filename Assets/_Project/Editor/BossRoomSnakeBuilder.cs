using UnityEditor;
using UnityEngine;

namespace Evolve.EditorTools
{
    /// <summary>
    /// Menu "Evolve -> Boss Rooms -> Build Snake Boss Room" &amp; "Validate Snake Boss Room":
    /// Dựng Prefab và Scene cho Phòng Trùm Đợt 2 - Phòng 4: Rắn Dung Nham ("Hố Mạch Dung Nham").
    /// </summary>
    public static class BossRoomSnakeBuilder
    {
        internal static BossRoomSpec Spec => new BossRoomSpec
        {
            RoomName = "BossRoom_Snake",
            ModelFbxPath = BossRoomBuildCommon.ModelFolder + "BossRoom_Snake.fbx",
            PrefabPath = BossRoomBuildCommon.PrefabFolder + "/BossRoom_Snake.prefab",
            ScenePath = BossRoomBuildCommon.Root + "/Scenes/BossRoom_Snake.unity",
            ProfileAssetPath = BossRoomBuildCommon.Root + "/Settings/BossRoomSnakeLook.asset",
            RockMatName = "BossRoom_Snake_RockVC",
            GlowMatName = "BossRoom_Snake_LavaGlow",
            BossWaitLocal = new Vector3(0f, 0f, 21.0f),
            AmbientColor = new Color(0.20f, 0.12f, 0.10f),
            FogColor = new Color(0.068f, 0.028f, 0.020f),
            DomeLightColor = new Color(1.00f, 0.45f, 0.16f),
            AccentLightColor = new Color(1.00f, 0.36f, 0.10f),
            BossLightColor = new Color(1.00f, 0.38f, 0.08f),
            MinCeilingRequired = 28.0f,
            RequiresAxeAnchor = false,
            RequiresOpenSkyAboveCenter = false,
            NonColliderObjectNames = new[]
            {
                "Story_SnakeLavaTrails_Glow",
                "Boss_Nest_LavaRim_Glow",
                "Story_SnakePreyShells",
            },
            BossPrefabPaths = new[]
            {
                "Assets/_Project/Prefabs/Wildlife/RanDungNham (Meshy).prefab",
                "Assets/_Project/Prefabs/Wildlife/RanDungNham.prefab",
            },
        };

        [MenuItem("Evolve/Boss Rooms/Build Snake Boss Room")]
        public static void BuildInteractive() => Build(interactiveConfirm: true);

        public static bool Build(bool interactiveConfirm) =>
            BossRoomBuildCommon.BuildRoom(Spec, interactiveConfirm, null);

        [MenuItem("Evolve/Boss Rooms/Validate Snake Boss Room")]
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
