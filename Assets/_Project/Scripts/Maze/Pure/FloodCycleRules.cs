using System;
using System.Collections.Generic;

namespace Evolve.Maze
{
    /// <summary>Các giai đoạn trong một Chu Kỳ Lũ (mục 9.3).</summary>
    public enum FloodPhase
    {
        Calm = 0,
        Warning = 1,
        Rising = 2,
        Peak = 3,
        Receding = 4
    }

    /// <summary>Thông số cấu hình Chu Kỳ Lũ (mặc định 12 phút/chu kỳ, cảnh báo trước 1 phút).</summary>
    public readonly struct FloodConfig
    {
        public float CycleDurationSeconds { get; }
        public float WarningLeadSeconds { get; }
        public float RiseDurationSeconds { get; }
        public float PeakDurationSeconds { get; }
        public float RecedeDurationSeconds { get; }
        public float BaseWaterLevel { get; }
        public float PeakWaterLevel { get; }
        public float HighLedgeSafeHeight { get; }
        public float MassLossRatio { get; }
        public float MinMassFloor { get; }

        public static FloodConfig Default => new FloodConfig(
            cycleDurationSeconds: 720f,
            warningLeadSeconds: 60f,
            riseDurationSeconds: 60f,
            peakDurationSeconds: 90f,
            recedeDurationSeconds: 60f,
            baseWaterLevel: 0.06f,
            peakWaterLevel: 4.5f,
            highLedgeSafeHeight: 5.2f,
            massLossRatio: 0.20f,
            minMassFloor: 5f);

        public FloodConfig(
            float cycleDurationSeconds,
            float warningLeadSeconds,
            float riseDurationSeconds,
            float peakDurationSeconds,
            float recedeDurationSeconds,
            float baseWaterLevel,
            float peakWaterLevel,
            float highLedgeSafeHeight,
            float massLossRatio,
            float minMassFloor)
        {
            CycleDurationSeconds = Math.Max(30f, cycleDurationSeconds);
            WarningLeadSeconds = Math.Max(0f, warningLeadSeconds);
            RiseDurationSeconds = Math.Max(1f, riseDurationSeconds);
            PeakDurationSeconds = Math.Max(1f, peakDurationSeconds);
            RecedeDurationSeconds = Math.Max(1f, recedeDurationSeconds);
            BaseWaterLevel = baseWaterLevel;
            PeakWaterLevel = Math.Max(baseWaterLevel, peakWaterLevel);
            HighLedgeSafeHeight = Math.Max(PeakWaterLevel, highLedgeSafeHeight);
            MassLossRatio = Math.Clamp(massLossRatio, 0f, 0.9f);
            MinMassFloor = Math.Max(1f, minMassFloor);
        }
    }

    /// <summary>Trạng thái Chu Kỳ Lũ tại một thời điểm.</summary>
    public readonly struct FloodSnapshot
    {
        public int CycleIndex { get; }
        public float CycleTime { get; }
        public FloodPhase Phase { get; }
        public float WaterLevel { get; }
        public float WarningIntensity { get; }
        public bool IsFloodActive { get; }

        public FloodSnapshot(
            int cycleIndex,
            float cycleTime,
            FloodPhase phase,
            float waterLevel,
            float warningIntensity,
            bool isFloodActive)
        {
            CycleIndex = cycleIndex;
            CycleTime = cycleTime;
            Phase = phase;
            WaterLevel = waterLevel;
            WarningIntensity = warningIntensity;
            IsFloodActive = isFloodActive;
        }
    }

    /// <summary>
    /// Luật thuần của Chu Kỳ Lũ (12 phút một chu kỳ, cảnh báo 1 phút trước, mực nước dâng, mất khối và trôi sang khoang khác).
    /// </summary>
    public static class FloodCycleRules
    {
        public static FloodSnapshot Evaluate(float totalElapsedSeconds, FloodConfig config)
        {
            if (totalElapsedSeconds < 0f) totalElapsedSeconds = 0f;

            float cycle = config.CycleDurationSeconds;
            int cycleIndex = (int)(totalElapsedSeconds / cycle);
            float t = totalElapsedSeconds - cycleIndex * cycle;

            float floodTotal = config.RiseDurationSeconds + config.PeakDurationSeconds + config.RecedeDurationSeconds;
            float riseStart = Math.Max(0f, cycle - floodTotal);
            float warnStart = Math.Max(0f, riseStart - config.WarningLeadSeconds);
            float peakStart = riseStart + config.RiseDurationSeconds;
            float recedeStart = peakStart + config.PeakDurationSeconds;

            if (t < warnStart)
            {
                return new FloodSnapshot(cycleIndex, t, FloodPhase.Calm, config.BaseWaterLevel, 0f, false);
            }

            if (t < riseStart)
            {
                float span = Math.Max(0.001f, riseStart - warnStart);
                float intensity = Math.Clamp((t - warnStart) / span, 0f, 1f);
                return new FloodSnapshot(cycleIndex, t, FloodPhase.Warning, config.BaseWaterLevel, intensity, false);
            }

            if (t < peakStart)
            {
                float u = Smooth01((t - riseStart) / config.RiseDurationSeconds);
                float level = Lerp(config.BaseWaterLevel, config.PeakWaterLevel, u);
                return new FloodSnapshot(cycleIndex, t, FloodPhase.Rising, level, 1f, level > config.BaseWaterLevel + 0.25f);
            }

            if (t < recedeStart)
            {
                return new FloodSnapshot(cycleIndex, t, FloodPhase.Peak, config.PeakWaterLevel, 0.5f, true);
            }

            float recedeU = Smooth01((t - recedeStart) / config.RecedeDurationSeconds);
            float recedeLevel = Lerp(config.PeakWaterLevel, config.BaseWaterLevel, recedeU);
            return new FloodSnapshot(cycleIndex, t, FloodPhase.Receding, recedeLevel, 0f, recedeLevel > config.BaseWaterLevel + 0.25f);
        }

        /// <summary>Kiểm tra slime có bị lũ bắt tại độ cao <paramref name="worldY"/> hay không.</summary>
        public static bool IsCaughtByFlood(float worldY, bool inShelter, FloodSnapshot snapshot, FloodConfig config)
        {
            if (inShelter) return false;
            if (!snapshot.IsFloodActive) return false;
            if (worldY >= config.HighLedgeSafeHeight) return false;
            return worldY < snapshot.WaterLevel - 0.1f;
        }

        /// <summary>Tính lượng khối bị mất khi bị lũ cuốn (không bao giờ kéo khối xuống dưới MinMassFloor).</summary>
        public static float CalculateMassLoss(float currentMass, FloodConfig config)
        {
            if (currentMass <= config.MinMassFloor) return 0f;
            float rawLoss = currentMass * config.MassLossRatio;
            float maxSpendable = currentMass - config.MinMassFloor;
            return Math.Min(rawLoss, maxSpendable);
        }

        /// <summary>Chọn khoang bị trôi dạt tới khi bị lũ cuốn ngoài chỗ cao.</summary>
        public static int PickWashAwayChamber(MazeGraph graph, int currentChamberId, int seed, int cycleIndex)
        {
            if (graph == null) throw new ArgumentNullException(nameof(graph));
            if (graph.Chambers.Count <= 1) return currentChamberId;

            var neighbors = graph.GetNeighborIds(currentChamberId);
            var candidates = new List<int>();
            for (int i = 0; i < neighbors.Count; i++)
            {
                int id = neighbors[i];
                var node = graph.GetChamber(id);
                if (node.Kind != ChamberKind.SealedCavern && id != currentChamberId)
                    candidates.Add(id);
            }

            if (candidates.Count == 0)
            {
                for (int i = 0; i < graph.Chambers.Count; i++)
                {
                    var node = graph.Chambers[i];
                    if (node.Id != currentChamberId && node.Kind != ChamberKind.SealedCavern)
                        candidates.Add(node.Id);
                }
            }

            if (candidates.Count == 0) return currentChamberId;
            var rng = new MazeRng(seed ^ (cycleIndex * 7919) ^ (currentChamberId * 104729));
            return candidates[rng.Range(0, candidates.Count)];
        }

        /// <summary>
        /// Tính hướng (đơn vị XZ) và cường độ lực dòng nước đẩy slime về phía hầm thấp khi lũ dâng.
        /// </summary>
        public static (float dirX, float dirZ, float force) ComputeCurrentPush(
            float playerX,
            float playerZ,
            float drainX,
            float drainZ,
            float waterLevel,
            float baseWaterLevel,
            float peakWaterLevel,
            float maxForce = 18f)
        {
            if (waterLevel <= baseWaterLevel + 0.25f) return (0f, 0f, 0f);
            float dx = drainX - playerX;
            float dz = drainZ - playerZ;
            float len = (float)Math.Sqrt(dx * dx + dz * dz);
            if (len < 0.001f) return (0f, 0f, 0f);

            float span = Math.Max(0.1f, peakWaterLevel - baseWaterLevel);
            float t = Math.Clamp((waterLevel - baseWaterLevel) / span, 0f, 1f);
            return (dx / len, dz / len, maxForce * t);
        }

        static float Smooth01(float t)
        {
            t = Math.Clamp(t, 0f, 1f);
            return t * t * (3f - 2f * t);
        }

        static float Lerp(float a, float b, float t) => a + (b - a) * t;
    }
}
