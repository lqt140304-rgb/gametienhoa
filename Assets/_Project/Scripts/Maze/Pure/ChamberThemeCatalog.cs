using System;
using System.Collections.Generic;
using Evolve.Survival;

namespace Evolve.Maze
{
    /// <summary>Màu RGB thuần C# (không phụ thuộc UnityEngine) để dùng được trong bộ luật thuần và cstest.</summary>
    public readonly struct MazeColor
    {
        public readonly float R, G, B;

        public MazeColor(float r, float g, float b)
        {
            R = r;
            G = g;
            B = b;
        }
    }

    /// <summary>Mô tả một loại thức ăn/quặng rải trong khoang.</summary>
    public readonly struct ThemeForageItem
    {
        public readonly string FbxName;
        public readonly string DisplayName;
        public readonly FoodKind Food;
        public readonly bool IsMineral;

        public ThemeForageItem(string fbxName, string displayName, FoodKind food, bool isMineral = false)
        {
            FbxName = fbxName;
            DisplayName = displayName;
            Food = food;
            IsMineral = isMineral;
        }
    }

    /// <summary>Hồ sơ đầy đủ của một chủ đề khoang: vách tường, ánh sáng, thảm thực vật, thức ăn và loài quái.</summary>
    public sealed class ChamberThemeProfile
    {
        public ChamberTheme Theme { get; }
        public string DisplayName { get; }
        public MazeColor FloorColor { get; }
        public MazeColor WallColor { get; }
        public float WallSmoothness { get; }
        public float DomeWobble { get; }
        public MazeColor MainLightColor { get; }
        public float MainLightIntensity { get; }
        public MazeColor AccentLightColor { get; }
        public float AccentLightIntensity { get; }
        public IReadOnlyList<string> FloorFloraModels { get; }
        public IReadOnlyList<string> CeilingOrWallProps { get; }
        public IReadOnlyList<ThemeForageItem> ForageItems { get; }
        public int ForageCount { get; }
        public IReadOnlyList<string> PreyPrefabs { get; }
        public int PreyCount { get; }
        public IReadOnlyList<string> PredatorPrefabs { get; }
        public int PredatorCount { get; }

        public ChamberThemeProfile(
            ChamberTheme theme,
            string displayName,
            MazeColor floorColor,
            MazeColor wallColor,
            float wallSmoothness,
            float domeWobble,
            MazeColor mainLightColor,
            float mainLightIntensity,
            MazeColor accentLightColor,
            float accentLightIntensity,
            string[] floorFloraModels,
            string[] ceilingOrWallProps,
            ThemeForageItem[] forageItems,
            int forageCount,
            string[] preyPrefabs,
            int preyCount,
            string[] predatorPrefabs,
            int predatorCount)
        {
            Theme = theme;
            DisplayName = displayName;
            FloorColor = floorColor;
            WallColor = wallColor;
            WallSmoothness = wallSmoothness;
            DomeWobble = domeWobble;
            MainLightColor = mainLightColor;
            MainLightIntensity = mainLightIntensity;
            AccentLightColor = accentLightColor;
            AccentLightIntensity = accentLightIntensity;
            FloorFloraModels = floorFloraModels ?? Array.Empty<string>();
            CeilingOrWallProps = ceilingOrWallProps ?? Array.Empty<string>();
            ForageItems = forageItems ?? Array.Empty<ThemeForageItem>();
            ForageCount = forageCount;
            PreyPrefabs = preyPrefabs ?? Array.Empty<string>();
            PreyCount = preyCount;
            PredatorPrefabs = predatorPrefabs ?? Array.Empty<string>();
            PredatorCount = predatorCount;
        }
    }

    /// <summary>
    /// Danh mục chủ đề cho từng khoang để mỗi hang có hệ sinh thái, vách đá, ánh sáng và loài quái riêng biệt.
    /// Thuần C#, không phụ thuộc UnityEngine.
    /// </summary>
    public static class ChamberThemeCatalog
    {
        public static readonly ChamberTheme[] NongThemes =
        {
            ChamberTheme.LumaMushroomGrove,
            ChamberTheme.RootCanopy,
            ChamberTheme.DampReedHollow,
            ChamberTheme.AmberPebbleGarden
        };

        public static readonly ChamberTheme[] GiuaThemes =
        {
            ChamberTheme.LimestoneStalactite,
            ChamberTheme.CrystalVein,
            ChamberTheme.SporeCurtain,
            ChamberTheme.DeepRootTangle
        };

        public static readonly ChamberTheme[] SauThemes =
        {
            ChamberTheme.EmberBasalt,
            ChamberTheme.SaltAbyss,
            ChamberTheme.FossilBoneyard
        };

        static readonly Dictionary<ChamberTheme, ChamberThemeProfile> Profiles = new Dictionary<ChamberTheme, ChamberThemeProfile>
        {
            [ChamberTheme.BirthLake] = new ChamberThemeProfile(
                ChamberTheme.BirthLake,
                "Hang Khởi Sinh",
                new MazeColor(0.24f, 0.28f, 0.20f),
                new MazeColor(0.26f, 0.24f, 0.22f),
                0.10f, 0.14f,
                new MazeColor(0.75f, 0.90f, 0.85f), 2.5f,
                new MazeColor(0.45f, 0.95f, 0.70f), 1.8f,
                new[] { "Flora_Fern_0", "Flora_Moss_0" },
                Array.Empty<string>(),
                new[] { new ThemeForageItem("Forage_MossClump", "Rêu Sáng", FoodKind.GlowMoss) },
                0,
                new[] { "Prey" }, 0,
                new[] { "Predator" }, 0),

            [ChamberTheme.MossSanctuary] = new ChamberThemeProfile(
                ChamberTheme.MossSanctuary,
                "Hang Trú Rêu Ẩm",
                new MazeColor(0.18f, 0.34f, 0.22f),
                new MazeColor(0.22f, 0.28f, 0.24f),
                0.15f, 0.12f,
                new MazeColor(0.50f, 0.95f, 0.65f), 2.4f,
                new MazeColor(0.35f, 0.85f, 0.90f), 1.6f,
                new[] { "Flora_Moss_0", "Flora_Moss_1", "Flora_Clover_0", "Flora_Fern_0", "Flora_Flower_0" },
                new[] { "Flora_Vine_0", "HangRoot_0" },
                new[]
                {
                    new ThemeForageItem("Forage_MossClump", "Rêu Sáng", FoodKind.GlowMoss),
                    new ThemeForageItem("Forage_SweetRoot", "Cỏ Ngọt", FoodKind.SweetRoot)
                },
                5,
                new[] { "Prey" }, 2,
                Array.Empty<string>(), 0), // Hang trú an toàn: không có kẻ săn mồi

            [ChamberTheme.MirageDeadEnd] = new ChamberThemeProfile(
                ChamberTheme.MirageDeadEnd,
                "Lối Giả Gió Rít",
                new MazeColor(0.28f, 0.26f, 0.23f),
                new MazeColor(0.32f, 0.29f, 0.26f),
                0.08f, 0.20f,
                new MazeColor(0.90f, 0.85f, 0.68f), 2.0f,
                new MazeColor(0.55f, 0.65f, 0.70f), 1.0f,
                new[] { "Flora_Pebbles_0", "Flora_Twigs_0", "Flora_Lichen_0" },
                new[] { "VaultStalactite_0", "Cliff_0" },
                new[] { new ThemeForageItem("Forage_LimeCrystal", "Đá Vôi", FoodKind.LimeCrystal, isMineral: true) },
                2,
                new[] { "Prey_C", "Prey" }, 2,
                Array.Empty<string>(), 0),

            [ChamberTheme.SealedDragonTomb] = new ChamberThemeProfile(
                ChamberTheme.SealedDragonTomb,
                "Khoang Phong Ấn Cổ Long",
                new MazeColor(0.14f, 0.11f, 0.11f),
                new MazeColor(0.18f, 0.14f, 0.13f),
                0.18f, 0.22f,
                new MazeColor(1.00f, 0.68f, 0.30f), 2.2f,
                new MazeColor(0.85f, 0.28f, 0.22f), 1.8f,
                new[] { "Rib_Bend_0", "Rib_Tip_0", "Flora_Pebbles_2", "CrystalCluster_2" },
                new[] { "VaultStalactite_2", "VaultCurtain_1" },
                new[]
                {
                    new ThemeForageItem("Forage_Quartz", "Thạch Anh Sáng", FoodKind.GlowQuartz, isMineral: true),
                    new ThemeForageItem("Forage_IronNodule", "Quặng Sắt", FoodKind.IronNodule, isMineral: true)
                },
                3,
                new[] { "Prey_C" }, 1,
                new[] { "Predator" }, 2),

            // ---------------- TẦNG NÔNG ----------------
            [ChamberTheme.LumaMushroomGrove] = new ChamberThemeProfile(
                ChamberTheme.LumaMushroomGrove,
                "Khoang Rừng Nấm Lam",
                new MazeColor(0.18f, 0.27f, 0.24f),
                new MazeColor(0.20f, 0.26f, 0.28f),
                0.14f, 0.15f,
                new MazeColor(0.45f, 0.92f, 0.88f), 2.8f,
                new MazeColor(0.55f, 0.65f, 1.00f), 2.0f,
                new[] { "LumaShroom_0", "LumaShroom_1", "Mushroom_Giant_0", "Mushroom_Small_0", "Flora_Mushrooms_0", "Flora_Moss_1" },
                new[] { "Flora_Vine_0", "VaultPod_0" },
                new[]
                {
                    new ThemeForageItem("Forage_FatMushroom", "Nấm Béo", FoodKind.FatMushroom),
                    new ThemeForageItem("Forage_SporePod", "Túi Bào Tử", FoodKind.SporePod),
                    new ThemeForageItem("Forage_MossClump", "Rêu Sáng", FoodKind.GlowMoss)
                },
                10,
                new[] { "Prey_C", "Prey" }, 4,
                new[] { "Predator" }, 1),

            [ChamberTheme.RootCanopy] = new ChamberThemeProfile(
                ChamberTheme.RootCanopy,
                "Khoang Vòm Rễ Sáng",
                new MazeColor(0.24f, 0.21f, 0.15f),
                new MazeColor(0.26f, 0.22f, 0.17f),
                0.10f, 0.17f,
                new MazeColor(0.72f, 0.92f, 0.55f), 2.6f,
                new MazeColor(0.95f, 0.78f, 0.40f), 1.6f,
                new[] { "Flora_RootArc_0", "Flora_RootArc_1", "RootArch", "Flora_Fern_1", "Flora_Clover_1", "Flora_Leaves_0" },
                new[] { "HangRoot_0", "HangRoot_1", "VaultRoot_0", "Flora_Vine_1" },
                new[]
                {
                    new ThemeForageItem("Forage_SweetRoot", "Cỏ Ngọt", FoodKind.SweetRoot),
                    new ThemeForageItem("Forage_BitterFern", "Dương Xỉ Đắng", FoodKind.BitterFern)
                },
                10,
                new[] { "Prey" }, 4,
                new[] { "Predator" }, 1),

            [ChamberTheme.DampReedHollow] = new ChamberThemeProfile(
                ChamberTheme.DampReedHollow,
                "Khoang Hốc Lau Ẩm",
                new MazeColor(0.16f, 0.25f, 0.20f),
                new MazeColor(0.21f, 0.25f, 0.23f),
                0.22f, 0.13f,
                new MazeColor(0.55f, 0.90f, 0.78f), 2.5f,
                new MazeColor(0.35f, 0.75f, 0.95f), 1.7f,
                new[] { "Flora_Reed_0", "Flora_Reed_1", "Flora_Reed_2", "Flora_Fern_0", "Flora_Moss_2", "BulbPlant_0" },
                new[] { "Flora_Vine_0", "HangRoot_2" },
                new[]
                {
                    new ThemeForageItem("Forage_Cattail", "Lau Ven Hồ", FoodKind.Cattail),
                    new ThemeForageItem("Forage_WaterLily", "Súng Nước", FoodKind.WaterLily),
                    new ThemeForageItem("Forage_MossClump", "Rêu Sáng", FoodKind.GlowMoss)
                },
                11,
                new[] { "Prey" }, 5,
                new[] { "Predator" }, 1),

            [ChamberTheme.AmberPebbleGarden] = new ChamberThemeProfile(
                ChamberTheme.AmberPebbleGarden,
                "Khoang Cuội Hổ Phách",
                new MazeColor(0.28f, 0.24f, 0.16f),
                new MazeColor(0.30f, 0.26f, 0.20f),
                0.16f, 0.16f,
                new MazeColor(1.00f, 0.80f, 0.45f), 2.5f,
                new MazeColor(0.60f, 0.95f, 0.65f), 1.4f,
                new[] { "Flora_Pebbles_0", "Flora_Pebbles_1", "Flora_Flower_1", "Flora_Bush_0", "Flora_Twigs_1" },
                new[] { "Cliff_1", "RootStub_0" },
                new[]
                {
                    new ThemeForageItem("Forage_SweetRoot", "Cỏ Ngọt", FoodKind.SweetRoot),
                    new ThemeForageItem("Forage_Quartz", "Thạch Anh Sáng", FoodKind.GlowQuartz, isMineral: true)
                },
                9,
                new[] { "Prey_C", "Prey" }, 4,
                new[] { "Predator" }, 1),

            // ---------------- TẦNG GIỮA ----------------
            [ChamberTheme.LimestoneStalactite] = new ChamberThemeProfile(
                ChamberTheme.LimestoneStalactite,
                "Động Thạch Nhũ Vôi",
                new MazeColor(0.25f, 0.26f, 0.28f),
                new MazeColor(0.28f, 0.29f, 0.31f),
                0.24f, 0.20f,
                new MazeColor(0.65f, 0.82f, 1.00f), 1.9f,
                new MazeColor(0.45f, 0.90f, 0.85f), 1.4f,
                new[] { "Flora_Pebbles_2", "Flora_Lichen_0", "Flora_Lichen_1", "Cliff_2" },
                new[] { "VaultStalactite_0", "VaultStalactite_1", "VaultStalactite_2" },
                new[]
                {
                    new ThemeForageItem("Forage_LimeCrystal", "Đá Vôi", FoodKind.LimeCrystal, isMineral: true),
                    new ThemeForageItem("Forage_BitterFern", "Dương Xỉ Đắng", FoodKind.BitterFern)
                },
                7,
                new[] { "Prey" }, 3,
                new[] { "Predator" }, 2),

            [ChamberTheme.CrystalVein] = new ChamberThemeProfile(
                ChamberTheme.CrystalVein,
                "Khoang Mạch Tinh Thể",
                new MazeColor(0.14f, 0.19f, 0.26f),
                new MazeColor(0.17f, 0.21f, 0.29f),
                0.35f, 0.18f,
                new MazeColor(0.42f, 0.80f, 1.00f), 2.1f,
                new MazeColor(0.75f, 0.45f, 1.00f), 1.9f,
                new[] { "CrystalCluster_0", "CrystalCluster_1", "CrystalCluster_2", "Flora_Pebbles_1" },
                new[] { "VaultStalactite_1", "Cliff_0" },
                new[]
                {
                    new ThemeForageItem("Forage_Quartz", "Thạch Anh Sáng", FoodKind.GlowQuartz, isMineral: true),
                    new ThemeForageItem("Forage_IceBloom", "Hoa Băng Thạch", FoodKind.IceBloom),
                    new ThemeForageItem("Forage_SaltCrust", "Muối Đá", FoodKind.SaltCrust, isMineral: true)
                },
                7,
                new[] { "Prey_C" }, 3,
                new[] { "Predator" }, 2),

            [ChamberTheme.SporeCurtain] = new ChamberThemeProfile(
                ChamberTheme.SporeCurtain,
                "Khoang Màn Bào Tử",
                new MazeColor(0.18f, 0.22f, 0.25f),
                new MazeColor(0.22f, 0.20f, 0.28f),
                0.15f, 0.16f,
                new MazeColor(0.65f, 0.55f, 1.00f), 1.9f,
                new MazeColor(0.35f, 0.95f, 0.80f), 1.8f,
                new[] { "CaveShroom_0", "CaveShroom_1", "CaveShroom_2", "Mushroom_Giant_1", "Flora_Mushrooms_1" },
                new[] { "VaultCurtain_0", "VaultCurtain_1", "VaultPod_0", "VaultPod_1" },
                new[]
                {
                    new ThemeForageItem("Forage_SporePod", "Túi Bào Tử", FoodKind.SporePod),
                    new ThemeForageItem("Forage_FatCap", "Nấm Mũ Dày", FoodKind.FatMushroom)
                },
                8,
                new[] { "Prey_C", "Prey" }, 3,
                new[] { "Predator" }, 2),

            [ChamberTheme.DeepRootTangle] = new ChamberThemeProfile(
                ChamberTheme.DeepRootTangle,
                "Khoang Rễ Xoắn Ngầm",
                new MazeColor(0.21f, 0.17f, 0.14f),
                new MazeColor(0.24f, 0.20f, 0.16f),
                0.08f, 0.21f,
                new MazeColor(0.55f, 0.78f, 0.65f), 1.7f,
                new MazeColor(0.85f, 0.58f, 0.35f), 1.4f,
                new[] { "Log_Hollow", "RootStub_0", "RootStub_1", "Flora_RootArc_1", "Flora_Twigs_0", "Flora_Fern_2" },
                new[] { "VaultRoot_0", "VaultRoot_1", "VaultRoot_2", "VaultRoot_3", "HangRoot_3" },
                new[]
                {
                    new ThemeForageItem("Forage_BitterFern", "Dương Xỉ Đắng", FoodKind.BitterFern),
                    new ThemeForageItem("Forage_SweetRoot", "Cỏ Ngọt", FoodKind.SweetRoot)
                },
                7,
                new[] { "Prey_C", "Prey" }, 3,
                new[] { "Predator" }, 2),

            // ---------------- TẦNG SÂU ----------------
            [ChamberTheme.EmberBasalt] = new ChamberThemeProfile(
                ChamberTheme.EmberBasalt,
                "Khoang Hốc Đá Nóng",
                new MazeColor(0.16f, 0.11f, 0.10f),
                new MazeColor(0.14f, 0.10f, 0.10f),
                0.18f, 0.22f,
                new MazeColor(1.00f, 0.44f, 0.22f), 1.5f,
                new MazeColor(0.90f, 0.22f, 0.15f), 1.4f,
                new[] { "Cliff_3", "Flora_Pebbles_2", "BulbPlant_1" },
                new[] { "VaultStalactite_2", "VaultCurtain_1" },
                new[]
                {
                    new ThemeForageItem("Forage_EmberBerry", "Quả Than Hồng", FoodKind.EmberBerry),
                    new ThemeForageItem("Forage_IronNodule", "Quặng Sắt", FoodKind.IronNodule, isMineral: true)
                },
                4,
                new[] { "Prey_C" }, 2,
                new[] { "Predator" }, 2),

            [ChamberTheme.SaltAbyss] = new ChamberThemeProfile(
                ChamberTheme.SaltAbyss,
                "Khoang Vực Muối Đen",
                new MazeColor(0.12f, 0.13f, 0.16f),
                new MazeColor(0.14f, 0.15f, 0.18f),
                0.30f, 0.19f,
                new MazeColor(0.62f, 0.72f, 0.90f), 1.3f,
                new MazeColor(0.45f, 0.85f, 0.95f), 1.1f,
                new[] { "CrystalCluster_1", "Flora_Pebbles_0", "CaveShroom_3" },
                new[] { "VaultStalactite_0", "VaultPod_2" },
                new[]
                {
                    new ThemeForageItem("Forage_SaltCrust", "Muối Đá", FoodKind.SaltCrust, isMineral: true),
                    new ThemeForageItem("Forage_LimeCrystal", "Đá Vôi", FoodKind.LimeCrystal, isMineral: true)
                },
                4,
                new[] { "Prey" }, 2,
                new[] { "Predator" }, 3),

            [ChamberTheme.FossilBoneyard] = new ChamberThemeProfile(
                ChamberTheme.FossilBoneyard,
                "Khoang Nghĩa Địa Xương Cổ",
                new MazeColor(0.17f, 0.15f, 0.13f),
                new MazeColor(0.15f, 0.13f, 0.12f),
                0.10f, 0.24f,
                new MazeColor(0.85f, 0.72f, 0.50f), 1.4f,
                new MazeColor(0.65f, 0.35f, 0.28f), 1.2f,
                new[] { "Rib_Straight_0", "Rib_Straight_1", "Rib_Bend_0", "Rib_Fork_0", "Rib_Foot_0" },
                new[] { "Rib_Tip_0", "VaultStalactite_1" },
                new[]
                {
                    new ThemeForageItem("Forage_LimeCrystal", "Đá Vôi", FoodKind.LimeCrystal, isMineral: true),
                    new ThemeForageItem("Forage_IronNodule", "Quặng Sắt", FoodKind.IronNodule, isMineral: true)
                },
                4,
                new[] { "Prey_C" }, 2,
                new[] { "Predator" }, 3)
        };

        public static ChamberThemeProfile Get(ChamberTheme theme)
        {
            if (Profiles.TryGetValue(theme, out var p)) return p;
            return Profiles[ChamberTheme.LumaMushroomGrove];
        }
    }
}
