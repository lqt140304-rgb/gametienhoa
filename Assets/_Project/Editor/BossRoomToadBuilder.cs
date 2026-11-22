using UnityEditor;
using UnityEngine;

namespace Evolve.EditorTools
{
    /// <summary>
    /// Menu "Evolve -> Boss Rooms -> Build Toad Boss Room" &amp; "Validate Toad Boss Room":
    /// Dựng Prefab và Scene cho Phòng Trùm Đợt 2 - Phòng 3: Cóc Hồ Già ("Đầm Rễ Cổ").
    /// </summary>
    public static class BossRoomToadBuilder
    {
        internal static BossRoomSpec Spec => new BossRoomSpec
        {
            RoomName = "BossRoom_Toad",
            ModelFbxPath = BossRoomBuildCommon.ModelFolder + "BossRoom_Toad.fbx",
            PrefabPath = BossRoomBuildCommon.PrefabFolder + "/BossRoom_Toad.prefab",
            ScenePath = BossRoomBuildCommon.Root + "/Scenes/BossRoom_Toad.unity",
            ProfileAssetPath = BossRoomBuildCommon.Root + "/Settings/BossRoomToadLook.asset",
            RockMatName = "BossRoom_Toad_RockVC",
            GlowMatName = "BossRoom_Toad_BioGlow",
            BossWaitLocal = new Vector3(0f, 0f, 20.0f),
            AmbientColor = new Color(0.14f, 0.20f, 0.12f),
            FogColor = new Color(0.038f, 0.055f, 0.028f),
            DomeLightColor = new Color(0.55f, 0.88f, 0.35f),
            AccentLightColor = new Color(0.45f, 0.92f, 0.25f),
            BossLightColor = new Color(0.48f, 0.90f, 0.28f),
            MinCeilingRequired = 28.0f,
            RequiresAxeAnchor = false,
            RequiresOpenSkyAboveCenter = false,
            NonColliderObjectNames = new[]
            {
                "Story_ToadTongueDrag",
                "Story_ToadSlimePuddles_Glow",
                "Story_ToadPreyShells",
            },
            BossPrefabPaths = new[]
            {
                "Assets/_Project/Prefabs/Wildlife/CocHoGia (Meshy).prefab",
                "Assets/_Project/Prefabs/Wildlife/CocHoGia.prefab",
            },
        };

        [MenuItem("Evolve/Boss Rooms/Build Toad Boss Room")]
        public static void BuildInteractive() => Build(interactiveConfirm: true);

        public static bool Build(bool interactiveConfirm) =>
            BossRoomBuildCommon.BuildRoom(Spec, interactiveConfirm, null);

        [MenuItem("Evolve/Boss Rooms/Validate Toad Boss Room")]
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
