using Evolve.World;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    public class TerrainShapeTests
    {
        [Test]
        public void CrossingMinutesIsEightForZone()
        {
            Assert.AreEqual(8f, WorldScale.CrossingMinutes(WorldScale.ZoneSize, WorldScale.CrawlSpeed), 0.01f);
        }

        [Test]
        public void CoreIsExactlyFlat()
        {
            for (float x = -WorldScale.CoreHalf; x <= WorldScale.CoreHalf; x += 5f)
                for (float z = -WorldScale.CoreHalf; z <= WorldScale.CoreHalf; z += 5f)
                    Assert.AreEqual(0f, TerrainShape.HeightAt(x, z, 1337), 1e-5f, $"x={x} z={z}");
        }

        [Test]
        public void HeightsNeverNegativeAndBounded()
        {
            float half = WorldScale.ZoneSize * 0.5f;
            for (float x = -half; x <= half; x += 10f)
                for (float z = -half; z <= half; z += 10f)
                {
                    float h = TerrainShape.HeightAt(x, z, 1337);
                    Assert.GreaterOrEqual(h, 0f, $"x={x} z={z}");
                    Assert.LessOrEqual(h, TerrainShape.CliffHeight + 1f, $"x={x} z={z}");
                }
        }

        [Test]
        public void SlopesStayWalkable()
        {
            const float step = 2f, limit = 1.2f, range = 210f;
            for (float x = -range; x <= range; x += step)
                for (float z = -range; z <= range; z += step)
                {
                    float h = TerrainShape.HeightAt(x, z, 1337);
                    Assert.LessOrEqual(System.Math.Abs(TerrainShape.HeightAt(x + step, z, 1337) - h), limit, $"x={x} z={z} (dx)");
                    Assert.LessOrEqual(System.Math.Abs(TerrainShape.HeightAt(x, z + step, 1337) - h), limit, $"x={x} z={z} (dz)");
                }
        }

        [Test]
        public void EdgeRisesToCliff()
        {
            Assert.GreaterOrEqual(TerrainShape.HeightAt(235f, 0f, 1337), 30f);
            Assert.GreaterOrEqual(TerrainShape.HeightAt(0f, -239f, 1337), 35f);
        }

        [Test]
        public void SameSeedSameHeightDifferentSeedDiffers()
        {
            Assert.AreEqual(TerrainShape.HeightAt(100f, 100f, 7), TerrainShape.HeightAt(100f, 100f, 7));
            bool differs = false;
            foreach (var p in new[] { 100f, 130f, -150f, 170f })
                if (System.Math.Abs(TerrainShape.HeightAt(p, 100f, 1) - TerrainShape.HeightAt(p, 100f, 2)) > 1e-3f) differs = true;
            Assert.IsTrue(differs);
        }

        [Test]
        public void HeightsArrayShapeAndRange()
        {
            var h = TerrainShape.Heights(65, 1);
            Assert.AreEqual(65, h.GetLength(0));
            Assert.AreEqual(65, h.GetLength(1));
            foreach (float v in h) { Assert.GreaterOrEqual(v, 0f); Assert.LessOrEqual(v, 1f); }
            Assert.AreEqual(TerrainShape.BaseOffset / TerrainShape.TerrainHeight, h[32, 32], 1e-4f);
        }
    }
}
