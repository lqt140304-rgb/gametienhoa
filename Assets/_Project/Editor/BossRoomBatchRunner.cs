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

        [MenuItem("Evolve/Boss Rooms/Build, Validate & Capture Wave 2")]
        public static void BuildValidateAndCaptureWave2()
        {
            AssetDatabase.Refresh();

            bool okHound = BossRoomHoundBuilder.Build(interactiveConfirm: false);
            BossRoomBuildCommon.CaptureEntryScreenshot("docs/art/previews/unity_bossroom_hound_entry.png", new Vector3(0f, 2.0f, 10f));
            BossRoomBuildCommon.CaptureDoorScreenshot("docs/art/previews/unity_bossroom_hound_door.png");

            bool okFox = BossRoomFoxBuilder.Build(interactiveConfirm: false);
            BossRoomBuildCommon.CaptureEntryScreenshot("docs/art/previews/unity_bossroom_fox_entry.png", new Vector3(0f, 2.2f, 10f));
            BossRoomBuildCommon.CaptureDoorScreenshot("docs/art/previews/unity_bossroom_fox_door.png");

            bool okToad = BossRoomToadBuilder.Build(interactiveConfirm: false);
            BossRoomBuildCommon.CaptureEntryScreenshot("docs/art/previews/unity_bossroom_toad_entry.png", new Vector3(0f, 2.0f, 10f));
            BossRoomBuildCommon.CaptureDoorScreenshot("docs/art/previews/unity_bossroom_toad_door.png");

            bool okSnake = BossRoomSnakeBuilder.Build(interactiveConfirm: false);
            BossRoomBuildCommon.CaptureEntryScreenshot("docs/art/previews/unity_bossroom_snake_entry.png", new Vector3(0f, 2.0f, 10f));
            BossRoomBuildCommon.CaptureDoorScreenshot("docs/art/previews/unity_bossroom_snake_door.png");

            Debug.Log($"[BossRoomBatchRunner] Kết quả Đợt 2: Hound={okHound}, Fox={okFox}, Toad={okToad}, Snake={okSnake}");
            if (!okHound || !okFox || !okToad || !okSnake)
                Debug.LogError("[BossRoomBatchRunner] Có phòng trùm Đợt 2 chưa đạt hợp đồng! Xem chi tiết log ở trên.");
        }
    }
}
