using System.Collections.Generic;
using Evolve.Maze;
using NUnit.Framework;

namespace Evolve.Tests.EditMode.Maze
{
    public class ChamberEncounterTests
    {
        [Test]
        public void EvaluateSituation_FollowsFourPhasesInOrder()
        {
            var calm = ChamberEncounterRules.EvaluateSituation(ChamberEncounterKind.AcidSporeRain, 5f);
            Assert.AreEqual(EncounterPhase.Calm, calm.Phase);

            float tWarn = ChamberEncounterRules.CalmSeconds + 1f;
            var warn = ChamberEncounterRules.EvaluateSituation(ChamberEncounterKind.AcidSporeRain, tWarn);
            Assert.AreEqual(EncounterPhase.Telegraph, warn.Phase);
            Assert.IsNotEmpty(warn.Announcement);

            float tActive = ChamberEncounterRules.CalmSeconds + ChamberEncounterRules.TelegraphSeconds + 2f;
            var active = ChamberEncounterRules.EvaluateSituation(ChamberEncounterKind.AcidSporeRain, tActive);
            Assert.AreEqual(EncounterPhase.Active, active.Phase);
            Assert.IsNotEmpty(active.Announcement);

            float tAfter = ChamberEncounterRules.CalmSeconds + ChamberEncounterRules.TelegraphSeconds + ChamberEncounterRules.ActiveSeconds + 1f;
            var after = ChamberEncounterRules.EvaluateSituation(ChamberEncounterKind.AcidSporeRain, tAfter);
            Assert.AreEqual(EncounterPhase.Aftermath, after.Phase);
        }

        [Test]
        public void AcidSporeRain_CorrodesOutsideCover_SafeUnderMushroomCap_RegrowsAftermath()
        {
            float tActive = ChamberEncounterRules.CalmSeconds + ChamberEncounterRules.TelegraphSeconds + 2f;
            var active = ChamberEncounterRules.EvaluateSituation(ChamberEncounterKind.AcidSporeRain, tActive);

            var exposed = ChamberEncounterRules.EvaluatePlayerInSituation(
                ChamberEncounterKind.AcidSporeRain, active, isInSafeCover: false, isInLowBasin: false, isOnGeyserPad: false);
            Assert.Less(exposed.HealthRatePerSecond, 0f);

            var sheltered = ChamberEncounterRules.EvaluatePlayerInSituation(
                ChamberEncounterKind.AcidSporeRain, active, isInSafeCover: true, isInLowBasin: false, isOnGeyserPad: false);
            Assert.AreEqual(0f, sheltered.HealthRatePerSecond, 1e-4f);

            float tAfter = ChamberEncounterRules.CalmSeconds + ChamberEncounterRules.TelegraphSeconds + ChamberEncounterRules.ActiveSeconds + 1f;
            var after = ChamberEncounterRules.EvaluateSituation(ChamberEncounterKind.AcidSporeRain, tAfter);
            var afterEffect = ChamberEncounterRules.EvaluatePlayerInSituation(
                ChamberEncounterKind.AcidSporeRain, after, isInSafeCover: true, isInLowBasin: false, isOnGeyserPad: false);
            Assert.IsTrue(afterEffect.RegrowFoodAndScatterPrey);
        }

        [Test]
        public void CrystalPulseStorm_StunsPredatorsAndPushesExposedPlayer()
        {
            float tActive = ChamberEncounterRules.CalmSeconds + ChamberEncounterRules.TelegraphSeconds + 2f;
            var active = ChamberEncounterRules.EvaluateSituation(ChamberEncounterKind.CrystalPulseStorm, tActive);

            var exposed = ChamberEncounterRules.EvaluatePlayerInSituation(
                ChamberEncounterKind.CrystalPulseStorm, active, isInSafeCover: false, isInLowBasin: false, isOnGeyserPad: false);
            Assert.IsTrue(exposed.StunPredators);
            Assert.Greater(exposed.RadialPushForce, 0f);

            var behindCrystal = ChamberEncounterRules.EvaluatePlayerInSituation(
                ChamberEncounterKind.CrystalPulseStorm, active, isInSafeCover: true, isInLowBasin: false, isOnGeyserPad: false);
            Assert.AreEqual(0f, behindCrystal.RadialPushForce, 1e-4f);
        }

        [Test]
        public void ThermalGeyserSurge_BurnsBasin_LaunchesOnGeyserPad()
        {
            float tActive = ChamberEncounterRules.CalmSeconds + ChamberEncounterRules.TelegraphSeconds + 2f;
            var active = ChamberEncounterRules.EvaluateSituation(ChamberEncounterKind.ThermalGeyserSurge, tActive);

            var inBasin = ChamberEncounterRules.EvaluatePlayerInSituation(
                ChamberEncounterKind.ThermalGeyserSurge, active, isInSafeCover: false, isInLowBasin: true, isOnGeyserPad: false);
            Assert.Less(inBasin.HealthRatePerSecond, 0f);

            var onPad = ChamberEncounterRules.EvaluatePlayerInSituation(
                ChamberEncounterKind.ThermalGeyserSurge, active, isInSafeCover: false, isInLowBasin: true, isOnGeyserPad: true);
            Assert.AreEqual(0f, onPad.HealthRatePerSecond, 1e-4f);
            Assert.Greater(onPad.LaunchImpulseY, 10f);
        }

        [Test]
        public void MazeTerrainMath_HasNaturalHighLedge_WallEnclosure_AndOpenTunnelGaps()
        {
            var angles = new List<float> { 30f, 150f, 270f };
            float radius = MazeTerrainMath.DefaultChamberRadius;
            int seed = 1200;

            Assert.GreaterOrEqual(radius, 45f, "Khoang mới phải rộng >= 45m (lớn hơn Hang Khởi Sinh).");

            var (hx, hz) = MazeTerrainMath.GetHighLedgeCenter(radius, angles, seed);
            float hillY = MazeTerrainMath.GroundHeightAt(hx, hz, seed, radius, angles, hasHighLedge: true);
            Assert.GreaterOrEqual(hillY, 5.5f, "Phải có đồi trú lũ tự nhiên cao hơn đỉnh lũ.");

            float centerY = MazeTerrainMath.GroundHeightAt(0f, 0f, seed, radius, angles, hasHighLedge: true);
            Assert.Less(centerY, 1.0f);

            float gapX = (float)System.Math.Cos(30f * System.Math.PI / 180f) * 39f;
            float gapZ = (float)System.Math.Sin(30f * System.Math.PI / 180f) * 39f;
            float gapY = MazeTerrainMath.GroundHeightAt(gapX, gapZ, seed, radius, angles, hasHighLedge: true);
            Assert.Less(gapY, 2.0f, "Cửa hầm phải được mở khe thấp trên vách.");

            float wallY = MazeTerrainMath.GroundHeightAt(0f, 49f, seed, radius, angles, hasHighLedge: true);
            float ceilEdgeY = MazeTerrainMath.CeilingHeightAt(0f, 49f, seed, radius);
            Assert.GreaterOrEqual(wallY, 18f);
            Assert.LessOrEqual(ceilEdgeY, wallY + 2f);
        }
    }
}
