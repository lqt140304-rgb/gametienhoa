using System.Collections.Generic;

namespace Evolve.Creatures
{
    /// <summary>
    /// The defence of each of the 21 prey species (spec 4, owner-approved table). Escape chance and counter strength follow the difficulty:
    /// 1 = 25% / at most 3 damage, 2 = 40% / at most 8, 3 = 50% / at most 15. Each escape matches the skill the slime learns from the species.
    /// </summary>
    public static class PreyDefenseSeed
    {
        public static readonly float[] EscapeChanceByDifficulty = { 0f, 0.25f, 0.40f, 0.50f };
        public static readonly float[] MaxCounterDamageByDifficulty = { 0f, 3f, 8f, 15f };

        static PreyDefenseDef D(string id, string species, string cave, int difficulty, PreyDefenseDef d)
        {
            d.Id = id; d.Species = species; d.Cave = cave; d.Difficulty = difficulty;
            d.EscapeChance = EscapeChanceByDifficulty[difficulty];
            return d;
        }

        public static IReadOnlyList<PreyDefenseDef> All { get; } = new List<PreyDefenseDef>
        {
            // Hang Khởi Sinh
            D("bo_canh_cung", "Bọ Cánh Cứng Nhỏ", "Hang Khởi Sinh", 1, new PreyDefenseDef { AlertRange = 2f, EscapeCooldown = 8f, GlideWindup = 0.35f, CounterDamage = 2f, CounterCooldown = 3f }),
            D("oc_ho", "Ốc Hồ", "Hang Khởi Sinh", 1, new PreyDefenseDef { AlertRange = 3f, EscapeCooldown = 12f, GuardSeconds = 3f, GuardDamageTaken = 0.5f, GuardRecovery = 1f, GuardRecoveryDamage = 1.25f, BlocksCatch = true, CounterDamage = 2f, CounterCooldown = 4f }),
            // Vườn Quặng Cầu Vồng
            D("dom_dom", "Đom Đóm Sắc Màu", "Vườn Quặng Cầu Vồng", 1, new PreyDefenseDef { EscapeCooldown = 20f, SpeedBoost = 1.5f, SpeedSeconds = 2f, ObscureRadius = 4f, ObscureSeconds = 1f }),
            D("tho_la", "Thỏ Lá", "Vườn Quặng Cầu Vồng", 2, new PreyDefenseDef { EscapeCooldown = 6f, SpeedBoost = 1.8f, SpeedSeconds = 2.5f, Zigzag = true, CounterPush = 5f, CounterCooldown = 5f }),
            // Hang Rêu Thở
            D("sen_reu", "Sên Rêu", "Hang Rêu Thở", 1, new PreyDefenseDef { AlertRange = 4f, EscapeCooldown = 15f, SlowRadius = 2.5f, SlowFraction = 0.4f, SlowSeconds = 6f, CounterSlow = 0.4f, CounterSlowSeconds = 3f, CounterCooldown = 6f }),
            D("muoi_reu", "Muỗi Rêu", "Hang Rêu Thở", 2, new PreyDefenseDef { EscapeCooldown = 5f, SpeedBoost = 1.4f, SpeedSeconds = 2f, CounterDamage = 2f, CounterDrain = 1f, CounterCooldown = 2f }),
            // Tổ Bỏ Hoang (Trứng Lạ là kỳ ngộ: không có phòng vệ)
            D("chim_non", "Chim Non", "Tổ Bỏ Hoang", 2, new PreyDefenseDef { EscapeCooldown = 8f, SpeedBoost = 1.7f, SpeedSeconds = 2f, CallHelpRadius = 25f, HelperName = "Chim Hang Mẹ" }),
            // Vực Nứt Tím
            D("doi_am_thoa", "Dơi Âm Thoa", "Vực Nứt Tím", 2, new PreyDefenseDef { AlertRange = 8f, EscapeCooldown = 8f, SpeedBoost = 2f, SpeedSeconds = 3f, CounterStun = 0.5f, CounterPush = 3f, CounterCooldown = 10f }),
            D("bo_gai_tim", "Bọ Gai Tím", "Vực Nứt Tím", 3, new PreyDefenseDef { EscapeCooldown = 15f, SpeedBoost = 1.4f, SpeedSeconds = 2f, TrapDamage = 12f, TrapSeconds = 12f, CounterDamage = 6f, CounterCooldown = 2f }),
            // Hang Nấm Huỳnh Quang
            D("nam_di_bo", "Nấm Đi Bộ", "Hang Nấm Huỳnh Quang", 2, new PreyDefenseDef { AlertRange = 4f, EscapeCooldown = 12f, GuardSeconds = 4f, ObscureRadius = 6f, ObscureSeconds = 4f, CounterDamage = 1f, CounterInfect = InfectKind.Spore, CounterCooldown = 8f }),
            D("buom_bao_tu", "Bướm Bào Tử", "Hang Nấm Huỳnh Quang", 1, new PreyDefenseDef { EscapeCooldown = 20f, SpeedBoost = 1.5f, SpeedSeconds = 3f, ObscureRadius = 4f, ObscureSeconds = 5f }),
            // Suối Nước Nóng
            D("oc_luu_huynh", "Ốc Lưu Huỳnh", "Suối Nước Nóng", 2, new PreyDefenseDef { AlertRange = 4f, EscapeCooldown = 15f, ObscureRadius = 4f, ObscureSeconds = 5f, CounterDamage = 3f, CounterInfect = InfectKind.Burn, CounterCooldown = 6f }),
            D("cua_da_nong", "Cua Đá Nóng", "Suối Nước Nóng", 3, new PreyDefenseDef { AlertRange = 4f, EscapeCooldown = 14f, GuardSeconds = 5f, ObscureRadius = 6f, ObscureSeconds = 5f, CounterDamage = 10f, CounterInfect = InfectKind.Burn, CounterCooldown = 4f }),
            // Khe Gió Hú
            D("de_gio", "Dế Gió", "Khe Gió Hú", 1, new PreyDefenseDef { AlertRange = 15f, EscapeCooldown = 5f, SpeedBoost = 1.7f, SpeedSeconds = 2f, AlarmRadius = 12f }),
            D("chuot_gio", "Chuột Gió", "Khe Gió Hú", 2, new PreyDefenseDef { AlertRange = 5f, EscapeCooldown = 12f, GuardSeconds = 6f, BlocksCatch = true, ObscureRadius = 5f, ObscureSeconds = 6f, CounterDamage = 5f, CounterCooldown = 3f }),
            // Nghĩa Địa Cự Thú
            D("gioi_trang", "Giòi Trắng", "Nghĩa Địa Cự Thú", 2, new PreyDefenseDef { AlertRange = 4f, EscapeCooldown = 12f, GuardSeconds = 4f, BlocksCatch = true, ObscureRadius = 5f, ObscureSeconds = 4f, CounterDamage = 1f, CounterInfect = InfectKind.Rot, CounterCooldown = 8f }),
            D("ken_ken_hang", "Kền Kền Hang", "Nghĩa Địa Cự Thú", 2, new PreyDefenseDef { AlertRange = 8f, EscapeCooldown = 8f, SpeedBoost = 2f, SpeedSeconds = 3f, CounterDamage = 7f, CounterCooldown = 4f }),
            // Hang Hóa Thạch Slime Cổ
            D("bo_da", "Bọ Đá", "Hang Hóa Thạch Slime Cổ", 2, new PreyDefenseDef { EscapeCooldown = 8f, SpeedBoost = 2.2f, SpeedSeconds = 2f, CounterDamage = 3f, CounterPush = 6f, CounterCooldown = 6f }),
            D("tac_ke_da", "Tắc Kè Đá", "Hang Hóa Thạch Slime Cổ", 2, new PreyDefenseDef { EscapeCooldown = 25f, SpeedBoost = 1.5f, SpeedSeconds = 2f, Decoy = true, EscapesGrab = true }),
            // Hang Tiếng Thở
            D("buom_dem", "Bướm Đêm", "Hang Tiếng Thở", 1, new PreyDefenseDef { EscapeCooldown = 8f, SpeedBoost = 1.6f, SpeedSeconds = 3f, Silent = true }),
            D("ret_mu", "Rết Mù", "Hang Tiếng Thở", 2, new PreyDefenseDef { AlertRange = 10f, EscapeCooldown = 6f, SpeedBoost = 1.8f, SpeedSeconds = 2f, CounterDamage = 3f, CounterInfect = InfectKind.Spore, CounterCooldown = 5f }),
        };

        static readonly Dictionary<string, PreyDefenseDef> byId = Index();

        static Dictionary<string, PreyDefenseDef> Index()
        {
            var d = new Dictionary<string, PreyDefenseDef>();
            foreach (var x in All) d[x.Id] = x;
            return d;
        }

        /// <summary>The defence of a species by its name in any spelling ("Ốc Hồ", "OcHo", "OcHo(Clone)"), or null.</summary>
        public static PreyDefenseDef FindBySpecies(string name)
        {
            string key = SpeciesNames.Key(name);
            if (key.Length == 0) return null;
            foreach (var d in All) if (SpeciesNames.Key(d.Species) == key) return d;
            return null;
        }

        /// <summary>The defence with this id, or null.</summary>
        public static PreyDefenseDef Get(string id) => id != null && byId.TryGetValue(id, out var d) ? d : null;
    }
}
