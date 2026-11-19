using Evolve.Maze;
using NUnit.Framework;

namespace Evolve.Tests.EditMode.Maze
{
    public class FloodCycleTests
    {
        [Test]
        public void Evaluate_DefaultCycleIs12Minutes_AndWarns1MinuteBeforeRise()
        {
            var cfg = FloodConfig.Default;
            Assert.AreEqual(720f, cfg.CycleDurationSeconds);
            Assert.AreEqual(60f, cfg.WarningLeadSeconds);

            // Phút 0..7.5 (giây 100): Yên bình
            var calm = FloodCycleRules.Evaluate(100f, cfg);
            Assert.AreEqual(FloodPhase.Calm, calm.Phase);
            Assert.AreEqual(cfg.BaseWaterLevel, calm.WaterLevel, 1e-4f);
            Assert.AreEqual(0f, calm.WarningIntensity, 1e-4f);
            Assert.IsFalse(calm.IsFloodActive);

            // Giây 480 (trong 60s trước khi nước dâng ở giây 510): Cảnh báo
            var warn = FloodCycleRules.Evaluate(480f, cfg);
            Assert.AreEqual(FloodPhase.Warning, warn.Phase);
            Assert.Greater(warn.WarningIntensity, 0f);
            Assert.LessOrEqual(warn.WarningIntensity, 1f);
            Assert.IsFalse(warn.IsFloodActive);

            // Giây 540: Nước đang dâng
            var rising = FloodCycleRules.Evaluate(540f, cfg);
            Assert.AreEqual(FloodPhase.Rising, rising.Phase);
            Assert.Greater(rising.WaterLevel, cfg.BaseWaterLevel);
            Assert.Less(rising.WaterLevel, cfg.PeakWaterLevel);
            Assert.IsTrue(rising.IsFloodActive);

            // Giây 600: Đỉnh lũ
            var peak = FloodCycleRules.Evaluate(600f, cfg);
            Assert.AreEqual(FloodPhase.Peak, peak.Phase);
            Assert.AreEqual(cfg.PeakWaterLevel, peak.WaterLevel, 1e-4f);
            Assert.IsTrue(peak.IsFloodActive);

            // Giây 690: Nước rút
            var receding = FloodCycleRules.Evaluate(690f, cfg);
            Assert.AreEqual(FloodPhase.Receding, receding.Phase);
            Assert.Less(receding.WaterLevel, cfg.PeakWaterLevel);

            // Giây 730: Sang chu kỳ mới (CycleIndex = 1)
            var nextCycle = FloodCycleRules.Evaluate(730f, cfg);
            Assert.AreEqual(1, nextCycle.CycleIndex);
            Assert.AreEqual(FloodPhase.Calm, nextCycle.Phase);
        }

        [Test]
        public void IsCaughtByFlood_SafeInShelterOrOnHighLedge_CaughtBelowWater()
        {
            var cfg = FloodConfig.Default;
            var peak = FloodCycleRules.Evaluate(600f, cfg);

            // Ở tầng thấp ngoài trời -> bị bắt
            Assert.IsTrue(FloodCycleRules.IsCaughtByFlood(1.0f, inShelter: false, peak, cfg));

            // Ở trong hang trú -> an toàn dù tọa độ thấp
            Assert.IsFalse(FloodCycleRules.IsCaughtByFlood(1.0f, inShelter: true, peak, cfg));

            // Đứng trên gờ cao -> an toàn
            Assert.IsFalse(FloodCycleRules.IsCaughtByFlood(cfg.HighLedgeSafeHeight, inShelter: false, peak, cfg));

            // Lúc bình thường -> không bị bắt
            var calm = FloodCycleRules.Evaluate(50f, cfg);
            Assert.IsFalse(FloodCycleRules.IsCaughtByFlood(1.0f, inShelter: false, calm, cfg));
        }

        [Test]
        public void CalculateMassLoss_RespectsMinMassFloor()
        {
            var cfg = FloodConfig.Default;
            Assert.AreEqual(2f, FloodCycleRules.CalculateMassLoss(10f, cfg), 1e-4f);
            Assert.AreEqual(1f, FloodCycleRules.CalculateMassLoss(6f, cfg), 1e-4f); // không tụt dưới 5f
            Assert.AreEqual(0f, FloodCycleRules.CalculateMassLoss(5f, cfg), 1e-4f);
        }

        [Test]
        public void PickWashAwayChamber_MovesToAnotherChamber_NeverSealedCavern()
        {
            var graph = MazeGenerator.Generate(777, 14);
            int target = FloodCycleRules.PickWashAwayChamber(graph, graph.BirthChamberId, seed: 777, cycleIndex: 0);

            Assert.AreNotEqual(graph.BirthChamberId, target);
            Assert.AreNotEqual(ChamberKind.SealedCavern, graph.GetChamber(target).Kind);
        }

        [Test]
        public void ComputeCurrentPush_PushesTowardLowDrainDuringFlood_ZeroWhenCalm()
        {
            var cfg = FloodConfig.Default;
            var (cx, cz, cf) = FloodCycleRules.ComputeCurrentPush(0f, 0f, 10f, 0f, cfg.BaseWaterLevel, cfg.BaseWaterLevel, cfg.PeakWaterLevel);
            Assert.AreEqual(0f, cf, 1e-4f);

            var (dx, dz, force) = FloodCycleRules.ComputeCurrentPush(0f, 0f, 10f, 0f, cfg.PeakWaterLevel, cfg.BaseWaterLevel, cfg.PeakWaterLevel, maxForce: 18f);
            Assert.AreEqual(1f, dx, 1e-4f);
            Assert.AreEqual(0f, dz, 1e-4f);
            Assert.AreEqual(18f, force, 1e-4f);
        }
    }
}
