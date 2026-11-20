using System.Linq;
using Evolve.Maze;
using NUnit.Framework;

namespace Evolve.Tests.EditMode.Maze
{
    public class CaveEncounterWave3Tests
    {
        [Test]
        public void SporeRing_TriggersOnceAtPeakGlowWhileInside()
        {
            var ring = new SporeRingState(cyclePeriod: 8f, peakWindow: 2f);

            // Chưa tới đỉnh sáng -> không kích hoạt dù đứng trong vòng
            Assert.IsFalse(ring.Tick(3.0f, playerInsideRing: true));
            Assert.IsFalse(ring.IsPeakGlow);

            // Tới pha sáng rực (>= 6s) nhưng đứng ngoài vòng -> không kích hoạt
            Assert.IsFalse(ring.Tick(3.2f, playerInsideRing: false));
            Assert.IsTrue(ring.IsPeakGlow);

            // Bước vào vòng đúng lúc sáng rực -> kích hoạt 1 lần duy nhất
            Assert.IsTrue(ring.Tick(0.2f, playerInsideRing: true));
            Assert.IsTrue(ring.Completed);
            Assert.IsFalse(ring.Tick(0.5f, playerInsideRing: true));
        }

        [Test]
        public void HotSpringTemper_RequiresSurvivingFullSurgeInWater()
        {
            var spring = new HotSpringTemperState(surgeDuration: 4f);

            Assert.IsFalse(spring.Tick(2.0f, playerInWater: true, isHeatSurgeActive: true, playerAlive: true));
            // Rời khỏi nước trước khi hết đợt nóng -> reset tiến trình
            Assert.IsFalse(spring.Tick(0.5f, playerInWater: false, isHeatSurgeActive: true, playerAlive: true));
            Assert.AreEqual(0f, spring.Progress01, 0.001f);

            // Ngâm đủ 4s liên tục trong đợt nóng -> hoàn tất
            Assert.IsFalse(spring.Tick(2.5f, playerInWater: true, isHeatSurgeActive: true, playerAlive: true));
            Assert.IsTrue(spring.Tick(1.6f, playerInWater: true, isHeatSurgeActive: true, playerAlive: true));
            Assert.IsTrue(spring.Completed);
        }

        [Test]
        public void NarrowCrackStash_AllowsOnlySmallSlimeOnce()
        {
            var stash = new NarrowCrackStashState(maxBodyRadius: 0.45f);

            Assert.IsFalse(stash.TryClaim(playerInsideStash: true, playerBodyRadius: 0.70f));
            Assert.IsTrue(stash.TryClaim(playerInsideStash: true, playerBodyRadius: 0.35f));
            Assert.IsTrue(stash.Completed);
            Assert.IsFalse(stash.TryClaim(playerInsideStash: true, playerBodyRadius: 0.35f));
        }

        [Test]
        public void MazeGenerator_IncludesAllFourGiuaCaves()
        {
            var map = MazeGenerator.Generate(seed: 2026);
            var themes = map.Chambers.Select(c => c.Theme).ToHashSet();

            Assert.IsTrue(themes.Contains(ChamberTheme.CrystalVein));
            Assert.IsTrue(themes.Contains(ChamberTheme.GlowMushroomCavern));
            Assert.IsTrue(themes.Contains(ChamberTheme.HotSpringCavern));
            Assert.IsTrue(themes.Contains(ChamberTheme.HowlingWindGorge));
        }
    }
}
