using UnityEditor;
using UnityEngine;

namespace Evolve.EditorTools
{
    /// <summary>
    /// Menu "Evolve -> Boss Rooms -> Build, Validate & Capture All 3":
    /// Dựng lần lượt GolemAxe_Planted, cả 3 phòng trùm (Scene + Prefab), kiểm tra hợp đồng
    /// bằng raycast thật trong Unity và chụp ảnh nhìn từ Player_Entry vào docs/art/previews/.
    /// </summary>
    public static class BossRoomBatchRunner
    {
        [MenuItem("Evolve/Boss Rooms/Build, Validate & Capture All 3")]
        public static void BuildValidateAndCaptureAll()
        {
            BossRoomGolemBuilder.EnsureGolemAxePlantedPrefab();
            AssetDatabase.Refresh();

            bool okGolem = BossRoomGolemBuilder.Build(interactiveConfirm: false);
            BossRoomBuildCommon.CaptureEntryScreenshot("docs/art/previews/unity_bossroom_golem_entry.png", new Vector3(0f, 2.0f, 10f));
            BossRoomBuildCommon.CaptureDoorScreenshot("docs/art/previews/unity_bossroom_golem_door.png");

            bool okBird = BossRoomBirdBuilder.Build(interactiveConfirm: false);
            BossRoomBuildCommon.CaptureEntryScreenshot("docs/art/previews/unity_bossroom_bird_entry.png", new Vector3(0f, 4.0f, 10f));
            BossRoomBuildCommon.CaptureDoorScreenshot("docs/art/previews/unity_bossroom_bird_door.png");

            bool okScorpion = BossRoomScorpionBuilder.Build(interactiveConfirm: false);
            BossRoomBuildCommon.CaptureEntryScreenshot("docs/art/previews/unity_bossroom_scorpion_entry.png", new Vector3(0f, 1.8f, 10f));
            BossRoomBuildCommon.CaptureDoorScreenshot("docs/art/previews/unity_bossroom_scorpion_door.png");

            Debug.Log($"[BossRoomBatchRunner] Kết quả: Golem={okGolem}, Bird={okBird}, Scorpion={okScorpion}");
            if (!okGolem || !okBird || !okScorpion)
                Debug.LogError("[BossRoomBatchRunner] Có phòng trùm chưa đạt hợp đồng! Xem chi tiết log ở trên.");
        }
    }
}
