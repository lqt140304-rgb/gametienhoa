using System;

namespace Evolve.Maze
{
    /// <summary>Kết quả tương tác thuần khi slime kích hoạt một Kỳ Ngộ hoặc dính Bẫy môi trường.</summary>
    public readonly struct EncounterOutcome
    {
        public bool Triggered { get; }
        public bool Consumed { get; }
        public float HealthDelta { get; }
        public float MassGain { get; }
        public float ImpulseY { get; }
        public float ImpulseRadial { get; }
        public string Message { get; }

        public EncounterOutcome(
            bool triggered,
            bool consumed,
            float healthDelta,
            float massGain,
            float impulseY,
            float impulseRadial,
            string message)
        {
            Triggered = triggered;
            Consumed = consumed;
            HealthDelta = healthDelta;
            MassGain = massGain;
            ImpulseY = impulseY;
            ImpulseRadial = impulseRadial;
            Message = message;
        }

        public static EncounterOutcome Ignored(string message = null) =>
            new EncounterOutcome(false, false, 0f, 0f, 0f, 0f, message);
    }

    /// <summary>
    /// Luật thuần cho Kỳ Ngộ & Bẫy môi trường trong mỗi khoang (mỗi khoang một cơ chế đặc trưng).
    /// </summary>
    public static class ChamberEncounterRules
    {
        public const float AmberMinMassRequired = 11f;
        public const float MinSafeHealthAfterSacrifice = 10f;

        /// <summary>Chọn cơ chế Kỳ Ngộ hoặc Bẫy phù hợp với loại, độ sâu và chủ đề của khoang.</summary>
        public static ChamberEncounterKind PickForChamber(
            ChamberKind kind,
            ChamberDepth depth,
            ChamberTheme theme,
            ref MazeRng rng)
        {
            if (kind == ChamberKind.BirthCavern) return ChamberEncounterKind.None;
            if (kind == ChamberKind.Shelter) return ChamberEncounterKind.AncientFont;
            if (kind == ChamberKind.SealedCavern) return ChamberEncounterKind.ResonanceCrystal;
            if (kind == ChamberKind.DeadEnd)
                return rng.NextFloat() < 0.5f ? ChamberEncounterKind.AmberShrine : ChamberEncounterKind.SporeGeyserTrap;

            return theme switch
            {
                ChamberTheme.LumaMushroomGrove => ChamberEncounterKind.AncientFont,
                ChamberTheme.RootCanopy => ChamberEncounterKind.UndergroundGeyser,
                ChamberTheme.DampReedHollow => ChamberEncounterKind.UndergroundGeyser,
                ChamberTheme.AmberPebbleGarden => ChamberEncounterKind.AmberShrine,
                ChamberTheme.LimestoneStalactite => ChamberEncounterKind.UndergroundGeyser,
                ChamberTheme.CrystalVein => ChamberEncounterKind.ResonanceCrystal,
                ChamberTheme.SporeCurtain => ChamberEncounterKind.SporeGeyserTrap,
                ChamberTheme.DeepRootTangle => ChamberEncounterKind.SporeGeyserTrap,
                ChamberTheme.EmberBasalt => ChamberEncounterKind.ThermalVentTrap,
                ChamberTheme.SaltAbyss => ChamberEncounterKind.ThermalVentTrap,
                ChamberTheme.FossilBoneyard => ChamberEncounterKind.ResonanceCrystal,
                _ => depth == ChamberDepth.Sau ? ChamberEncounterKind.ThermalVentTrap : ChamberEncounterKind.UndergroundGeyser
            };
        }

        /// <summary>Kiểm tra bẫy định kỳ (Mạch Phun Nước / Khe Hơi Nóng) có đang phun tại thời điểm <paramref name="timeSeconds"/> không.</summary>
        public static bool IsCycleActive(ChamberEncounterKind kind, float timeSeconds)
        {
            if (kind != ChamberEncounterKind.UndergroundGeyser && kind != ChamberEncounterKind.ThermalVentTrap)
                return true;

            float period = kind == ChamberEncounterKind.UndergroundGeyser ? 5.5f : 6.0f;
            float activeWindow = kind == ChamberEncounterKind.UndergroundGeyser ? 1.8f : 2.2f;
            float phase = timeSeconds - (float)Math.Floor(timeSeconds / period) * period;
            return phase < activeWindow;
        }

        /// <summary>Tính toán hệ quả khi slime tiếp xúc với Kỳ Ngộ hoặc Bẫy.</summary>
        public static EncounterOutcome Evaluate(
            ChamberEncounterKind kind,
            float currentHealth,
            float maxHealth,
            float currentMass,
            bool alreadyConsumed,
            float timeSeconds)
        {
            switch (kind)
            {
                case ChamberEncounterKind.AncientFont:
                    if (alreadyConsumed) return EncounterOutcome.Ignored("Suối Nguồn đã cạn, sẽ dâng lại sau đợt lũ kế tiếp.");
                    float heal = Math.Max(0f, maxHealth - currentHealth);
                    return new EncounterOutcome(
                        triggered: true,
                        consumed: true,
                        healthDelta: heal,
                        massGain: 1.5f,
                        impulseY: 0f,
                        impulseRadial: 0f,
                        message: "Kỳ Ngộ — Suối Nguồn Cổ Đại: Hồi phục hoàn toàn sinh lực và tăng +1.5 khối lượng!");

                case ChamberEncounterKind.ResonanceCrystal:
                    if (alreadyConsumed) return EncounterOutcome.Ignored("Tinh Thể Cộng Hưởng đang ngủ yên chờ chu kỳ lũ mới.");
                    if (currentHealth <= MinSafeHealthAfterSacrifice + 5f)
                        return EncounterOutcome.Ignored("Sinh lực quá yếu, chưa thể hiến tế cho Tinh Thể Cộng Hưởng.");
                    float sacrifice = Math.Min(currentHealth * 0.25f, currentHealth - MinSafeHealthAfterSacrifice);
                    return new EncounterOutcome(
                        triggered: true,
                        consumed: true,
                        healthDelta: -sacrifice,
                        massGain: 2.5f,
                        impulseY: 3.0f,
                        impulseRadial: 8.0f,
                        message: $"Kỳ Ngộ — Tinh Thể Cộng Hưởng: Hiến {sacrifice:0.#} máu để nhận +2.5 khối lượng!");

                case ChamberEncounterKind.AmberShrine:
                    if (alreadyConsumed) return EncounterOutcome.Ignored("Khối Hổ Phách Tiền Sử đã được thu hoạch.");
                    if (currentMass < AmberMinMassRequired)
                        return EncounterOutcome.Ignored($"Kỳ Ngộ — Hổ Phách Tiền Sử: Cần ít nhất {AmberMinMassRequired:0} khối lượng để nghiền vỡ.");
                    return new EncounterOutcome(
                        triggered: true,
                        consumed: true,
                        healthDelta: maxHealth * 0.25f,
                        massGain: 2.0f,
                        impulseY: 0f,
                        impulseRadial: 0f,
                        message: "Kỳ Ngộ — Hổ Phách Tiền Sử: Phá vỡ lớp hổ phách, nhận +2.0 khối lượng và hồi máu!");

                case ChamberEncounterKind.UndergroundGeyser:
                    if (!IsCycleActive(kind, timeSeconds))
                        return EncounterOutcome.Ignored();
                    return new EncounterOutcome(
                        triggered: true,
                        consumed: false,
                        healthDelta: 0f,
                        massGain: 0f,
                        impulseY: 13.5f,
                        impulseRadial: 3.5f,
                        message: "Mạch Phun Nước Ngầm đẩy bạn bay lên gờ cao!");

                case ChamberEncounterKind.SporeGeyserTrap:
                    if (alreadyConsumed) return EncounterOutcome.Ignored();
                    return new EncounterOutcome(
                        triggered: true,
                        consumed: true,
                        healthDelta: -16f,
                        massGain: 0f,
                        impulseY: 6.5f,
                        impulseRadial: 11f,
                        message: "Bẫy Túi Bào Tử phát nổ! Bị hất văng và mất 16 máu.");

                case ChamberEncounterKind.ThermalVentTrap:
                    if (!IsCycleActive(kind, timeSeconds))
                        return EncounterOutcome.Ignored();
                    return new EncounterOutcome(
                        triggered: true,
                        consumed: false,
                        healthDelta: -14f,
                        massGain: 0f,
                        impulseY: 5.0f,
                        impulseRadial: 6.0f,
                        message: "Khe Hơi Nóng đang phun trào! Tránh ra khỏi vùng trũng.");

                default:
                    return EncounterOutcome.Ignored();
            }
        }
    }
}
