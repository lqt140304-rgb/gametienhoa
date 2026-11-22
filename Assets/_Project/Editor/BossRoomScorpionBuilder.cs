using UnityEditor;
using UnityEngine;

namespace Evolve.EditorTools
{
    /// <summary>
    /// Menu "Evolve -> Boss Rooms -> Build Scorpion Boss Room" & "Validate Scorpion Boss Room":
    /// Dựng Prefab và Scene cho Phòng Trùm 3: Bọ Cạp Thạch Anh ("Bãi Cát Tinh Thể").
    /// </summary>
    public static class BossRoomScorpionBuilder
    {
        internal static BossRoomSpec Spec => new BossRoomSpec
        {
            RoomName = "BossRoom_Scorpion",
            ModelFbxPath = BossRoomBuildCommon.ModelFolder + "BossRoom_Scorpion.fbx",
            PrefabPath = BossRoomBuildCommon.PrefabFolder + "/BossRoom_Scorpion.prefab",
            ScenePath = BossRoomBuildCommon.Root + "/Scenes/BossRoom_Scorpion.unity",
            ProfileAssetPath = BossRoomBuildCommon.Root + "/Settings/BossRoomScorpionLook.asset",
            RockMatName = "BossRoom_Scorpion_SandRockVC",
            GlowMatName = "BossRoom_Scorpion_AmethystGlow",
            BossWaitLocal = new Vector3(0f, -0.10f, 10.0f),
            AmbientColor = new Color(0.24f, 0.14f, 0.12f),
            FogColor = new Color(0.085f, 0.036f, 0.042f),
            DomeLightColor = new Color(0.96f, 0.68f, 0.52f),
            AccentLightColor = new Color(0.84f, 0.34f, 0.92f),
            BossLightColor = new Color(0.98f, 0.56f, 0.32f),
            MinCeilingRequired = 28.0f,
            RequiresAxeAnchor = false,
            RequiresOpenSkyAboveCenter = false,
            NonColliderObjectNames = new[] { "SandPit_RimMarkers", "SandPit_RimCrystals_Glow" },
        };

        [MenuItem("Evolve/Boss Rooms/Build Scorpion Boss Room")]
        public static void BuildInteractive() => Build(interactiveConfirm: true);

        public static bool Build(bool interactiveConfirm) =>
            BossRoomBuildCommon.BuildRoom(Spec, interactiveConfirm, onRoomExtras: null);

        [MenuItem("Evolve/Boss Rooms/Validate Scorpion Boss Room")]
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
