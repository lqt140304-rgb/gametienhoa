using System;

namespace Evolve.Maze
{
    /// <summary>Trạng thái hiện tại của tình huống môi trường trong khoang.</summary>
    public readonly struct EncounterSituationSnapshot
    {
        public int CycleIndex { get; }
        public EncounterPhase Phase { get; }
        public float PhaseProgress { get; }
        public string Announcement { get; }

        public EncounterSituationSnapshot(int cycleIndex, EncounterPhase phase, float phaseProgress, string announcement)
        {
            CycleIndex = cycleIndex;
            Phase = phase;
            PhaseProgress = phaseProgress;
            Announcement = announcement;
        }
    }

    /// <summary>Hệ quả vật lý và sinh thái lên slime/quái/thức ăn tại một thời điểm trong tình huống môi trường.</summary>
    public readonly struct SituationEffect
    {
        public float HealthRatePerSecond { get; }
        public float LaunchImpulseY { get; }
        public float RadialPushForce { get; }
        public bool StunPredators { get; }
        public bool RegrowFoodAndScatterPrey { get; }

        public SituationEffect(
            float healthRatePerSecond,
            float launchImpulseY,
            float radialPushForce,
            bool stunPredators,
            bool regrowFoodAndScatterPrey)
        {
            HealthRatePerSecond = healthRatePerSecond;
            LaunchImpulseY = launchImpulseY;
            RadialPushForce = radialPushForce;
            StunPredators = stunPredators;
            RegrowFoodAndScatterPrey = regrowFoodAndScatterPrey;
        }

        public static SituationEffect None => new SituationEffect(0f, 0f, 0f, false, false);
    }

    /// <summary>
    /// Luật thuần cho Kỳ Ngộ Tình Huống Môi Trường Động (không phải bục tăng chỉ số tĩnh,
    /// mà là biến cố môi trường buộc slime thích nghi: trú dưới tán nấm, nấp sau tinh thể, cưỡi mạch phun...).
    /// </summary>
    public static class ChamberEncounterRules
    {
        public const float CalmSeconds = 22f;
        public const float TelegraphSeconds = 5f;
        public const float ActiveSeconds = 10f;
        public const float AftermathSeconds = 5f;
        public const float CyclePeriodSeconds = CalmSeconds + TelegraphSeconds + ActiveSeconds + AftermathSeconds;

        public static ChamberEncounterKind PickForChamber(
            ChamberKind kind,
            ChamberDepth depth,
            ChamberTheme theme,
            ref MazeRng rng)
        {
            if (kind == ChamberKind.BirthCavern || kind == ChamberKind.Shelter)
                return ChamberEncounterKind.None;
            if (kind == ChamberKind.SealedCavern)
                return ChamberEncounterKind.AncientDragonBreath;

            return theme switch
            {
                ChamberTheme.LumaMushroomGrove => ChamberEncounterKind.AcidSporeRain,
                ChamberTheme.SporeCurtain => ChamberEncounterKind.AcidSporeRain,
                ChamberTheme.CrystalVein => ChamberEncounterKind.CrystalPulseStorm,
                ChamberTheme.LimestoneStalactite => ChamberEncounterKind.CrystalPulseStorm,
                ChamberTheme.EmberBasalt => ChamberEncounterKind.ThermalGeyserSurge,
                ChamberTheme.SaltAbyss => ChamberEncounterKind.ThermalGeyserSurge,
                ChamberTheme.FossilBoneyard => ChamberEncounterKind.ThermalGeyserSurge,
                ChamberTheme.RootCanopy => ChamberEncounterKind.AwakeningRoots,
                ChamberTheme.DeepRootTangle => ChamberEncounterKind.AwakeningRoots,
                ChamberTheme.DampReedHollow => ChamberEncounterKind.AcidSporeRain,
                ChamberTheme.AmberPebbleGarden => ChamberEncounterKind.AwakeningRoots,
                ChamberTheme.MirageDeadEnd => rng.NextFloat() < 0.5f ? ChamberEncounterKind.ThermalGeyserSurge : ChamberEncounterKind.CrystalPulseStorm,
                _ => depth == ChamberDepth.Sau ? ChamberEncounterKind.ThermalGeyserSurge : ChamberEncounterKind.AcidSporeRain
            };
        }

        public static EncounterSituationSnapshot EvaluateSituation(ChamberEncounterKind kind, float elapsedSeconds)
        {
            if (kind == ChamberEncounterKind.None)
                return new EncounterSituationSnapshot(0, EncounterPhase.Calm, 0f, null);

            if (elapsedSeconds < 0f) elapsedSeconds = 0f;
            int cycle = (int)(elapsedSeconds / CyclePeriodSeconds);
            float t = elapsedSeconds - cycle * CyclePeriodSeconds;

            if (t < CalmSeconds)
                return new EncounterSituationSnapshot(cycle, EncounterPhase.Calm, t / CalmSeconds, null);

            if (t < CalmSeconds + TelegraphSeconds)
            {
                float p = (t - CalmSeconds) / TelegraphSeconds;
                return new EncounterSituationSnapshot(cycle, EncounterPhase.Telegraph, p, TelegraphText(kind));
            }

            if (t < CalmSeconds + TelegraphSeconds + ActiveSeconds)
            {
                float p = (t - CalmSeconds - TelegraphSeconds) / ActiveSeconds;
                return new EncounterSituationSnapshot(cycle, EncounterPhase.Active, p, ActiveText(kind));
            }

            float afterP = (t - CalmSeconds - TelegraphSeconds - ActiveSeconds) / AftermathSeconds;
            return new EncounterSituationSnapshot(cycle, EncounterPhase.Aftermath, afterP, AftermathText(kind));
        }

        /// <summary>Tính tác động tình huống môi trường tại vị trí slime theo trạng thái nấp/vùng trũng/mạch phun.</summary>
        public static SituationEffect EvaluatePlayerInSituation(
            ChamberEncounterKind kind,
            EncounterSituationSnapshot snapshot,
            bool isInSafeCover,
            bool isInLowBasin,
            bool isOnGeyserPad)
        {
            if (kind == ChamberEncounterKind.None) return SituationEffect.None;

            if (snapshot.Phase == EncounterPhase.Aftermath)
            {
                bool regrow = kind == ChamberEncounterKind.AcidSporeRain || kind == ChamberEncounterKind.AwakeningRoots;
                return new SituationEffect(0f, 0f, 0f, false, regrow);
            }

            if (snapshot.Phase != EncounterPhase.Active)
                return SituationEffect.None;

            switch (kind)
            {
                case ChamberEncounterKind.AcidSporeRain:
                    // Phơi mình ngoài trời bị axit ăn mòn; nấp dưới tán nấm khổng lồ thì an toàn tuyệt đối
                    float acidDps = isInSafeCover ? 0f : -6.5f;
                    return new SituationEffect(acidDps, 0f, 0f, false, false);

                case ChamberEncounterKind.CrystalPulseStorm:
                    // Bão xung kích làm tê liệt toàn bộ quái săn; slime không nấp sau cụm tinh thể sẽ bị đẩy dạt
                    float push = isInSafeCover ? 0f : 14f;
                    return new SituationEffect(0f, 0f, push, stunPredators: true, false);

                case ChamberEncounterKind.ThermalGeyserSurge:
                    // Vùng trũng bốc nhiệt bỏng rát; nhưng bước vào mạch phun ngầm sẽ bay vọt lên đồi cao
                    float heatDps = isInLowBasin && !isOnGeyserPad ? -7.5f : 0f;
                    float launch = isOnGeyserPad ? 15f : 0f;
                    return new SituationEffect(heatDps, launch, 0f, false, false);

                case ChamberEncounterKind.AwakeningRoots:
                    // Rễ quất gần gốc rễ làm bay văng nếu đứng sát mạch phun; làm rụng quả & con mồi hoảng loạn
                    float rootLaunch = isOnGeyserPad ? 9.5f : 0f;
                    float rootPush = isOnGeyserPad ? 10f : 0f;
                    return new SituationEffect(0f, rootLaunch, rootPush, false, regrowFoodAndScatterPrey: true);

                case ChamberEncounterKind.AncientDragonBreath:
                    // Lồng xương rồng cổ che chắn gió bão và hồi nhẹ sinh lực; đứng ngoài bị gió thổi dạt mạnh
                    float wind = isInSafeCover ? 0f : 18f;
                    float heal = isInSafeCover ? 2.5f : 0f;
                    return new SituationEffect(heal, 0f, wind, false, false);

                default:
                    return SituationEffect.None;
            }
        }

        static string TelegraphText(ChamberEncounterKind kind) => kind switch
        {
            ChamberEncounterKind.AcidSporeRain => "Sương axit trên trần bắt đầu nhỏ giọt — hãy nấp dưới tán Nấm Khổng Lồ!",
            ChamberEncounterKind.CrystalPulseStorm => "Các mạch tinh thể đang ngân vang dữ dội — chuẩn bị nấp sau cụm tinh thể!",
            ChamberEncounterKind.ThermalGeyserSurge => "Vùng trũng bốc hơi nóng — các mạch phun sắp bùng phát, sẵn sàng phóng lên cao!",
            ChamberEncounterKind.AwakeningRoots => "Rễ đại thụ trên trần đang rung chuyển mạnh!",
            ChamberEncounterKind.AncientDragonBreath => "Xương Cổ Long rít gió — hãy chui vào lồng xương sườn!",
            _ => null
        };

        static string ActiveText(ChamberEncounterKind kind) => kind switch
        {
            ChamberEncounterKind.AcidSporeRain => "Mưa Axit Bào Tử đang rơi (10s)! Trú dưới tán nấm lớn để tránh bị ăn mòn.",
            ChamberEncounterKind.CrystalPulseStorm => "Bão Xung Kích Tinh Thể! Quái săn đang bị tê liệt — chớp thời cơ săn mồi hoặc vượt qua!",
            ChamberEncounterKind.ThermalGeyserSurge => "Triều Nhiệt Bùng Phát! Rời khỏi vùng trũng hoặc cưỡi mạch phun lên đồi cao!",
            ChamberEncounterKind.AwakeningRoots => "Rễ Đại Thụ Thức Tỉnh! Quả ngọt rụng xuống và bầy con mồi đang chạy tán loạn!",
            ChamberEncounterKind.AncientDragonBreath => "Hơi Thở Cổ Long càn quét! Ẩn trong lồng xương rồng để được che chắn.",
            _ => null
        };

        static string AftermathText(ChamberEncounterKind kind) => kind switch
        {
            ChamberEncounterKind.AcidSporeRain => "Mưa axit đã tạnh — nhiều nấm và bào tử mới vừa mọc lên khắp khoang.",
            ChamberEncounterKind.AwakeningRoots => "Mặt đất lắng xuống, thức ăn mới rơi rụng quanh các gốc rễ.",
            _ => null
        };
    }
}
