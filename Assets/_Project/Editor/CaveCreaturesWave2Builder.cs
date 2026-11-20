using UnityEditor;
using UnityEngine;

namespace Evolve.EditorTools
{
    /// <summary>
    /// Tạo AnimatorController, Prefab (Prey/Predator để trống ô drops) và SpeciesSO cho 7 loài sinh vật Tầng Nông Đợt 2
    /// (Hang Rêu Thở & Tổ Bỏ Hoang). Trứng Lạ là vật phẩm kỳ ngộ riêng, không gắn Prey/Predator.
    /// </summary>
    public static class CaveCreaturesWave2Builder
    {
        public static readonly CaveCreaturesWave1Builder.CreatureSpec[] Specs =
        {
            // Hang 3: Hang Rêu Thở
            new CaveCreaturesWave1Builder.CreatureSpec("SenReu", "Sên Rêu", "BreathingMoss", false, false, "Action", 0.30f, 0.70f, 0.28f, 0.65f, 14f, 3f, 16f, 0f, 8, 22),
            new CaveCreaturesWave1Builder.CreatureSpec("MuoiReu", "Muỗi Rêu", "BreathingMoss", false, false, "Action", 0.26f, 0.70f, 0.95f, 2.4f, 10f, 2f, 14f, 0f, 8, 24),
            new CaveCreaturesWave1Builder.CreatureSpec("RetReu", "Rết Rêu", "BreathingMoss", true, false, "Attack", 0.36f, 1.45f, 1.15f, 2.6f, 38f, 12f, 30f, 12f, 3, 8, "SenReu"),
            new CaveCreaturesWave1Builder.CreatureSpec("ReuMe", "Rêu Mẹ", "BreathingMoss", true, true, "Action", 0.90f, 1.85f, 0.40f, 0.95f, 120f, 44f, 68f, 16f, 1, 2, "RetReu"),

            // Hang 4: Tổ Bỏ Hoang
            new CaveCreaturesWave1Builder.CreatureSpec("ChimNon", "Chim Non", "AbandonedNest", false, false, "Hit", 0.26f, 0.55f, 0.75f, 1.8f, 12f, 2.5f, 15f, 0f, 8, 22),
            new CaveCreaturesWave1Builder.CreatureSpec("RanTromTrung", "Rắn Trộm Trứng", "AbandonedNest", true, false, "Attack", 0.34f, 1.40f, 1.05f, 2.5f, 40f, 11f, 32f, 14f, 3, 8, "ChimNon"),
            new CaveCreaturesWave1Builder.CreatureSpec("ChimHangMe", "Chim Hang Mẹ", "AbandonedNest", true, true, "Attack", 0.72f, 1.65f, 1.25f, 3.0f, 110f, 36f, 60f, 22f, 1, 2, "RanTromTrung"),
        };

        [MenuItem("Evolve/Build Wave 2 Nong Creatures & Prefabs")]
        public static void BuildMenu()
        {
            EnsureAll();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Wave2Creatures] Đã dựng 7 AnimatorController, Prefab (để trống drops) và SpeciesSO cho Hang Rêu Thở & Tổ Bỏ Hoang.");
        }

        public static void EnsureAll() => CaveCreaturesWave1Builder.BuildBatch(Specs);
    }
}
