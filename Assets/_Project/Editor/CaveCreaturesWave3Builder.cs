using UnityEditor;
using UnityEngine;

namespace Evolve.EditorTools
{
    /// <summary>
    /// Tạo AnimatorController, Prefab (Prey/Predator để trống ô drops) và SpeciesSO cho 12 loài sinh vật Tầng Giữa Đợt 3
    /// (Hang Nấm Huỳnh Quang, Suối Nước Nóng, Khe Gió Hú).
    /// </summary>
    public static class CaveCreaturesWave3Builder
    {
        public static readonly CaveCreaturesWave1Builder.CreatureSpec[] Specs =
        {
            // Hang 6: Hang Nấm Huỳnh Quang
            new CaveCreaturesWave1Builder.CreatureSpec("NamDiBo", "Nấm Đi Bộ", "GlowMushroomCavern", false, false, "Hit", 0.30f, 0.72f, 0.45f, 1.05f, 18f, 3f, 20f, 0f, 6, 18),
            new CaveCreaturesWave1Builder.CreatureSpec("BuomBaoTu", "Bướm Bào Tử", "GlowMushroomCavern", false, false, "Action", 0.28f, 0.75f, 0.90f, 2.2f, 14f, 2f, 16f, 0f, 6, 18),
            new CaveCreaturesWave1Builder.CreatureSpec("EchDocDom", "Ếch Độc Đốm", "GlowMushroomCavern", true, false, "Attack", 0.40f, 1.25f, 1.10f, 2.5f, 52f, 14f, 36f, 15f, 3, 7, "BuomBaoTu"),
            new CaveCreaturesWave1Builder.CreatureSpec("NamKhongLo", "Nấm Khổng Lồ", "GlowMushroomCavern", true, true, "Action", 1.15f, 2.35f, 0.42f, 0.95f, 150f, 48f, 75f, 20f, 1, 2, "EchDocDom"),

            // Hang 7: Suối Nước Nóng
            new CaveCreaturesWave1Builder.CreatureSpec("OcLuuHuynh", "Ốc Lưu Huỳnh", "HotSpringCavern", false, false, "Action", 0.30f, 0.70f, 0.32f, 0.75f, 22f, 3f, 18f, 0f, 6, 16),
            new CaveCreaturesWave1Builder.CreatureSpec("CuaDaNong", "Cua Đá Nóng", "HotSpringCavern", false, false, "Hit", 0.34f, 0.80f, 0.72f, 1.6f, 28f, 5f, 22f, 4f, 5, 14),
            new CaveCreaturesWave1Builder.CreatureSpec("KyNhongLua", "Kỳ Nhông Lửa", "HotSpringCavern", true, false, "Attack", 0.42f, 1.45f, 1.15f, 2.6f, 56f, 16f, 38f, 16f, 3, 7, "CuaDaNong"),
            new CaveCreaturesWave1Builder.CreatureSpec("RanDungNham", "Rắn Dung Nham", "HotSpringCavern", true, true, "Attack", 1.10f, 2.50f, 1.05f, 2.4f, 165f, 54f, 82f, 24f, 1, 2, "KyNhongLua"),

            // Hang 8: Khe Gió Hú
            new CaveCreaturesWave1Builder.CreatureSpec("DeGio", "Dế Gió", "HowlingWindGorge", false, false, "Hit", 0.28f, 0.68f, 0.92f, 2.3f, 14f, 3f, 16f, 0f, 6, 18),
            new CaveCreaturesWave1Builder.CreatureSpec("ChuotGio", "Chuột Gió", "HowlingWindGorge", false, false, "Hit", 0.25f, 0.58f, 1.25f, 2.9f, 14f, 2f, 16f, 0f, 6, 18),
            new CaveCreaturesWave1Builder.CreatureSpec("ChonKhe", "Chồn Khe", "HowlingWindGorge", true, false, "Attack", 0.38f, 1.40f, 1.35f, 3.1f, 54f, 16f, 36f, 16f, 3, 7, "ChuotGio"),
            new CaveCreaturesWave1Builder.CreatureSpec("CaoGio", "Cáo Gió", "HowlingWindGorge", true, true, "Action", 0.95f, 2.15f, 1.40f, 3.2f, 155f, 48f, 76f, 22f, 1, 2, "ChonKhe"),
        };

        [MenuItem("Evolve/Build Wave 3 Giua Creatures & Prefabs")]
        public static void BuildMenu()
        {
            EnsureAll();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Wave3Creatures] Đã dựng 12 AnimatorController, Prefab (để trống drops) và SpeciesSO cho Tầng Giữa.");
        }

        public static void EnsureAll() => CaveCreaturesWave1Builder.BuildBatch(Specs);
    }
}
