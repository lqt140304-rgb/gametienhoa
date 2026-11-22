using UnityEditor;
using UnityEngine;

namespace Evolve.EditorTools
{
    /// <summary>
    /// Menu "Evolve -> Boss Rooms -> Build Golem Boss Room" & "Validate Golem Boss Room":
    /// Dựng Prefab và Scene cho Phòng Trùm 1: Golem Thạch Anh ("Lò Tinh Thể").
    /// Đặt prefab Assets/_Project/Resources/Props/GolemAxe_Planted.prefab vào Axe_Anchor (không collider).
    /// </summary>
    public static class BossRoomGolemBuilder
    {
        const string AxePlantedPrefabPath = "Assets/_Project/Resources/Props/GolemAxe_Planted.prefab";
        static readonly Vector3 AxeAnchorLocal = new Vector3(0f, 0.06f, 0f); // chạm đúng mặt trên khối đá lõi (y = 0.06 m)

        internal static BossRoomSpec Spec => new BossRoomSpec
        {
            RoomName = "BossRoom_Golem",
            ModelFbxPath = BossRoomBuildCommon.ModelFolder + "BossRoom_Golem.fbx",
            PrefabPath = BossRoomBuildCommon.PrefabFolder + "/BossRoom_Golem.prefab",
            ScenePath = BossRoomBuildCommon.Root + "/Scenes/BossRoom_Golem.unity",
            ProfileAssetPath = BossRoomBuildCommon.Root + "/Settings/BossRoomGolemLook.asset",
            RockMatName = "BossRoom_Golem_RockVC",
            GlowMatName = "BossRoom_Golem_CrystalGlow",
            BossWaitLocal = new Vector3(0f, 0f, 23.0f),
            AmbientColor = new Color(0.24f, 0.17f, 0.13f),
            FogColor = new Color(0.085f, 0.045f, 0.024f),
            DomeLightColor = new Color(0.95f, 0.70f, 0.45f),
            AccentLightColor = new Color(1.00f, 0.54f, 0.14f),
            BossLightColor = new Color(1.00f, 0.38f, 0.08f),
            MinCeilingRequired = 30.0f,
            RequiresAxeAnchor = true,
            RequiresOpenSkyAboveCenter = false,
            NonColliderObjectNames = new[] { "Story_BrokenRailsAndCart", "Story_CartCutOre_Glow", "Axe_CoreCracks_Glow" },
        };

        [MenuItem("Evolve/Boss Rooms/Build Golem Boss Room")]
        public static void BuildInteractive() => Build(interactiveConfirm: true);

        public static bool Build(bool interactiveConfirm)
        {
            EnsureGolemAxePlantedPrefab();
            return BossRoomBuildCommon.BuildRoom(Spec, interactiveConfirm, AttachAxeAnchor);
        }

        [MenuItem("Evolve/Boss Rooms/Validate Golem Boss Room")]
        public static void Validate()
        {
            var go = GameObject.Find(Spec.RoomName);
            if (go == null) go = AssetDatabase.LoadAssetAtPath<GameObject>(Spec.PrefabPath);
            if (go == null) { Debug.LogError($"[{Spec.RoomName}] Chưa có scene hoặc prefab."); return; }
            bool ok = BossRoomBuildCommon.ValidateContract(go, Spec, out string report);
            if (ok) Debug.Log($"[{Spec.RoomName}] {report}");
            else Debug.LogError($"[{Spec.RoomName}] {report}");
        }

        internal static void EnsureGolemAxePlantedPrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(AxePlantedPrefabPath) != null) return;
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[BossRoom_Golem] Dừng Play trước để tạo GolemAxe_Planted.prefab.");
                return;
            }
            if (!EditorApplication.ExecuteMenuItem("Evolve/Build Golem Axe Prefabs"))
            {
                var method = typeof(GolemAxeBuilder).GetMethod("Build", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                method?.Invoke(null, null);
            }
            AssetDatabase.Refresh();
        }

        static void AttachAxeAnchor(GameObject roomRoot, Material _rockMat, Material _glowMat)
        {
            var axeAnchor = BossRoomBuildCommon.CreateMarker(roomRoot.transform, "Axe_Anchor", AxeAnchorLocal, Quaternion.identity);
            var plantedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AxePlantedPrefabPath);
            if (plantedPrefab == null)
            {
                Debug.LogError($"[BossRoom_Golem] Không tìm thấy {AxePlantedPrefabPath}. Chạy menu Evolve -> Build Golem Axe Prefabs (dừng Play trước).");
                return;
            }

            var axeGo = (GameObject)PrefabUtility.InstantiatePrefab(plantedPrefab, axeAnchor);
            axeGo.name = "GolemAxe_Planted";
            axeGo.transform.localPosition = Vector3.zero;
            axeGo.transform.localRotation = Quaternion.identity;
            axeGo.transform.localScale = Vector3.one;

            // Hợp đồng: rìu không có collider để người chơi không chạm hay kẹt lên được
            foreach (var col in axeGo.GetComponentsInChildren<Collider>(true))
                Object.DestroyImmediate(col);
        }
    }
}
