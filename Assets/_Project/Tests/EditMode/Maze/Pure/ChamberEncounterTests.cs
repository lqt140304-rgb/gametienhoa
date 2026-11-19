using System.Collections.Generic;
using Evolve.Maze;
using NUnit.Framework;

namespace Evolve.Tests.EditMode.Maze
{
    public class ChamberEncounterTests
    {
        [Test]
        public void AncientFont_HealsFullAndAddsMassOncePerCycle()
        {
            var first = ChamberEncounterRules.Evaluate(
                ChamberEncounterKind.AncientFont, currentHealth: 40f, maxHealth: 100f, currentMass: 10f, alreadyConsumed: false, timeSeconds: 10f);

            Assert.IsTrue(first.Triggered);
            Assert.IsTrue(first.Consumed);
            Assert.AreEqual(60f, first.HealthDelta, 1e-3f);
            Assert.AreEqual(1.5f, first.MassGain, 1e-3f);

            var second = ChamberEncounterRules.Evaluate(
                ChamberEncounterKind.AncientFont, currentHealth: 40f, maxHealth: 100f, currentMass: 11.5f, alreadyConsumed: true, timeSeconds: 12f);
            Assert.IsFalse(second.Triggered);
        }

        [Test]
        public void ResonanceCrystal_TradesHealthForMass_ProtectsLowHealth()
        {
            var ok = ChamberEncounterRules.Evaluate(
                ChamberEncounterKind.ResonanceCrystal, currentHealth: 100f, maxHealth: 100f, currentMass: 10f, alreadyConsumed: false, timeSeconds: 10f);
            Assert.IsTrue(ok.Triggered);
            Assert.Less(ok.HealthDelta, 0f);
            Assert.AreEqual(2.5f, ok.MassGain, 1e-3f);

            var low = ChamberEncounterRules.Evaluate(
                ChamberEncounterKind.ResonanceCrystal, currentHealth: 12f, maxHealth: 100f, currentMass: 10f, alreadyConsumed: false, timeSeconds: 10f);
            Assert.IsFalse(low.Triggered);
        }

        [Test]
        public void AmberShrine_RequiresMinMass11()
        {
            var tooLight = ChamberEncounterRules.Evaluate(
                ChamberEncounterKind.AmberShrine, currentHealth: 80f, maxHealth: 100f, currentMass: 10f, alreadyConsumed: false, timeSeconds: 5f);
            Assert.IsFalse(tooLight.Triggered);

            var heavy = ChamberEncounterRules.Evaluate(
                ChamberEncounterKind.AmberShrine, currentHealth: 80f, maxHealth: 100f, currentMass: 12f, alreadyConsumed: false, timeSeconds: 5f);
            Assert.IsTrue(heavy.Triggered);
            Assert.AreEqual(2.0f, heavy.MassGain, 1e-3f);
        }

        [Test]
        public void UndergroundGeyserAndThermalVent_FollowPeriodicCycles()
        {
            Assert.IsTrue(ChamberEncounterRules.IsCycleActive(ChamberEncounterKind.UndergroundGeyser, 0.5f));
            Assert.IsFalse(ChamberEncounterRules.IsCycleActive(ChamberEncounterKind.UndergroundGeyser, 3.0f));

            var launch = ChamberEncounterRules.Evaluate(
                ChamberEncounterKind.UndergroundGeyser, 80f, 100f, 10f, false, 0.5f);
            Assert.IsTrue(launch.Triggered);
            Assert.Greater(launch.ImpulseY, 10f);

            var idle = ChamberEncounterRules.Evaluate(
                ChamberEncounterKind.UndergroundGeyser, 80f, 100f, 10f, false, 3.0f);
            Assert.IsFalse(idle.Triggered);
        }

        [Test]
        public void MazeTerrainMath_HasNaturalHighLedge_WallEnclosure_AndOpenTunnelGaps()
        {
            var angles = new List<float> { 30f, 150f, 270f };
            float radius = MazeTerrainMath.DefaultChamberRadius;
            int seed = 1200;

            Assert.GreaterOrEqual(radius, 45f, "Khoang mới phải rộng >= 45m (lớn hơn Hang Khởi Sinh).");

            // 1. Có đồi trú lũ tự nhiên cao >= 5.5m (an toàn trước đỉnh lũ 4.5m)
            var (hx, hz) = MazeTerrainMath.GetHighLedgeCenter(radius, angles, seed);
            float hillY = MazeTerrainMath.GroundHeightAt(hx, hz, seed, radius, angles, hasHighLedge: true);
            Assert.GreaterOrEqual(hillY, 5.5f, "Phải có đồi trú lũ tự nhiên cao hơn đỉnh lũ.");

            // 2. Tâm khoang là vùng trũng thấp (< 1m)
            float centerY = MazeTerrainMath.GroundHeightAt(0f, 0f, seed, radius, angles, hasHighLedge: true);
            Assert.Less(centerY, 1.0f);

            // 3. Tại cửa hầm (góc 30 độ, bán kính 39m), vách mở khe thấp để đi qua được
            float gapX = (float)System.Math.Cos(30f * System.Math.PI / 180f) * 39f;
            float gapZ = (float)System.Math.Sin(30f * System.Math.PI / 180f) * 39f;
            float gapY = MazeTerrainMath.GroundHeightAt(gapX, gapZ, seed, radius, angles, hasHighLedge: true);
            Assert.Less(gapY, 2.0f, "Cửa hầm phải được mở khe thấp trên vách.");

            // 4. Tại vách kín (góc 90 độ, bán kính 49m), tường dâng cao tới 22m và trần hạ xuống khép kín
            float wallY = MazeTerrainMath.GroundHeightAt(0f, 49f, seed, radius, angles, hasHighLedge: true);
            float ceilEdgeY = MazeTerrainMath.CeilingHeightAt(0f, 49f, seed, radius);
            Assert.GreaterOrEqual(wallY, 18f);
            Assert.LessOrEqual(ceilEdgeY, wallY + 2f);
        }
    }
}
