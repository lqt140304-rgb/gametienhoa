using UnityEditor;
using UnityEngine;

namespace Evolve.EditorTools
{
    /// <summary>
    /// Menu "Evolve -> Boss Rooms -> Build Bird Boss Room" & "Validate Bird Boss Room":
    /// Dựng Prefab và Scene cho Phòng Trùm 2: Chim Hang Mẹ ("Tổ Kền Kền").
    /// </summary>
    public static class BossRoomBirdBuilder
    {
        internal static BossRoomSpec Spec => new BossRoomSpec
        {
            RoomName = "BossRoom_Bird",
            ModelFbxPath = BossRoomBuildCommon.ModelFolder + "BossRoom_Bird.fbx",
            PrefabPath = BossRoomBuildCommon.PrefabFolder + "/BossRoom_Bird.prefab",
            ScenePath = BossRoomBuildCommon.Root + "/Scenes/BossRoom_Bird.unity",
            ProfileAssetPath = BossRoomBuildCommon.Root + "/Settings/BossRoomBirdLook.asset",
            RockMatName = "BossRoom_Bird_RockVC",
            GlowMatName = "BossRoom_Bird_JadeGlow",
            BossWaitLocal = new Vector3(0f, 0f, 21.0f),
            AmbientColor = new Color(0.14f, 0.22f, 0.24f),
            FogColor = new Color(0.028f, 0.065f, 0.072f),
            DomeLightColor = new Color(0.62f, 0.94f, 0.92f),
            AccentLightColor = new Color(0.22f, 0.88f, 0.82f),
            BossLightColor = new Color(0.26f, 0.92f, 0.86f),
            MinCeilingRequired = 34.0f,
            RequiresAxeAnchor = false,
            RequiresOpenSkyAboveCenter = true,
            NonColliderObjectNames = new[] { "Story_CenterScatteredBones" },
        };

        [MenuItem("Evolve/Boss Rooms/Build Bird Boss Room")]
        public static void BuildInteractive() => Build(interactiveConfirm: true);

        public static bool Build(bool interactiveConfirm) =>
            BossRoomBuildCommon.BuildRoom(Spec, interactiveConfirm, onRoomExtras: null);

        [MenuItem("Evolve/Boss Rooms/Validate Bird Boss Room")]
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
