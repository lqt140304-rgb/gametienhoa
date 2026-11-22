using UnityEditor;
using UnityEngine;

namespace Evolve.EditorTools
{
    /// <summary>
    /// Menu "Evolve -> Boss Rooms -> Build Fox Boss Room" &amp; "Validate Fox Boss Room":
    /// Dựng Prefab và Scene cho Phòng Trùm Đợt 2 - Phòng 2: Cáo Gió ("Khe Gió Hú").
    /// </summary>
    public static class BossRoomFoxBuilder
    {
        internal static BossRoomSpec Spec => new BossRoomSpec
        {
            RoomName = "BossRoom_Fox",
            ModelFbxPath = BossRoomBuildCommon.ModelFolder + "BossRoom_Fox.fbx",
            PrefabPath = BossRoomBuildCommon.PrefabFolder + "/BossRoom_Fox.prefab",
            ScenePath = BossRoomBuildCommon.Root + "/Scenes/BossRoom_Fox.unity",
            ProfileAssetPath = BossRoomBuildCommon.Root + "/Settings/BossRoomFoxLook.asset",
            RockMatName = "BossRoom_Fox_RockVC",
            GlowMatName = "BossRoom_Fox_WindGlow",
            BossWaitLocal = new Vector3(0f, 0f, 22.0f),
            AmbientColor = new Color(0.16f, 0.20f, 0.24f),
            FogColor = new Color(0.045f, 0.062f, 0.085f),
            DomeLightColor = new Color(0.72f, 0.88f, 1.00f),
            AccentLightColor = new Color(0.45f, 0.84f, 1.00f),
            BossLightColor = new Color(0.65f, 0.92f, 1.00f),
            MinCeilingRequired = 32.0f,
            RequiresAxeAnchor = false,
            RequiresOpenSkyAboveCenter = false,
            NonColliderObjectNames = new[]
            {
                "Story_FoxWindSwirls",
                "Story_PreyShells",
            },
            BossPrefabPaths = new[]
            {
                "Assets/_Project/Prefabs/Wildlife/CaoGio (Meshy).prefab",
                "Assets/_Project/Prefabs/Wildlife/CaoGio.prefab",
            },
        };

        [MenuItem("Evolve/Boss Rooms/Build Fox Boss Room")]
        public static void BuildInteractive() => Build(interactiveConfirm: true);

        public static bool Build(bool interactiveConfirm) =>
            BossRoomBuildCommon.BuildRoom(Spec, interactiveConfirm, null);

        [MenuItem("Evolve/Boss Rooms/Validate Fox Boss Room")]
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
