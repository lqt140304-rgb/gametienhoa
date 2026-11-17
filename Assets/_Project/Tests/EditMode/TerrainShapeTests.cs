using Evolve.World;
using NUnit.Framework;
using UnityEngine;

namespace Evolve.Tests.EditMode
{
    public class TerrainShapeTests
    {
        [Test]
        public void CrossingTakesAboutEightMinutes()
        {
            Assert.AreEqual(8.33f, WorldScale.CrossingMinutes(WorldScale.ZoneSize, WorldScale.CrawlSpeed), 0.01f);
        }

        [Test]
        public void WaterIsCarvedToItsDepth()
        {
            foreach (var pool in ZoneLayout.PoolCenters)
                Assert.AreEqual(-2f, TerrainShape.HeightAt(pool.x, pool.y, 1337), 1e-4f);
            var c = ZoneLayout.LakeCenter;
            Assert.AreEqual(-ZoneLayout.DepthAt(c.x, c.y), TerrainShape.HeightAt(c.x, c.y, 1337), 1e-4f);
        }

        [Test]
        public void AreasAreGentlyBumpyNotFlat()
        {
            foreach (var pad in ZoneLayout.Biomes)
            {
                float sum = 0f, sum2 = 0f; int n = 0;
                for (float dx = -20f; dx <= 20f; dx += 4f)
                    for (float dz = -20f; dz <= 20f; dz += 4f)
                    {
                        if (dx * dx + dz * dz > 400f) continue;
                        float h = TerrainShape.HeightAt(pad.Center.x + dx, pad.Center.y + dz, 1337);
                        Assert.LessOrEqual(Mathf.Abs(h), 2.7f, pad.Name);
                        sum += h; sum2 += h * h; n++;
                    }
                float mean = sum / n;
                Assert.GreaterOrEqual(Mathf.Sqrt(sum2 / n - mean * mean), 0.25f, pad.Name + " is too flat");
            }
        }

        [Test]
        public void HillsRiseBetweenAreas()
        {
            float best = 0f;
            for (float x = -170f; x <= 170f; x += 10f)
                for (float z = -170f; z <= 170f; z += 10f)
                    best = Mathf.Max(best, TerrainShape.HeightAt(x, z, 1337));
            Assert.GreaterOrEqual(best, 4f);
        }

        [Test]
        public void InsideIsWalkable()
        {
            const float step = 0.5f;
            var rnd = new System.Random(1);
            for (int i = 0; i < 3000; i++)
            {
                float x = (float)rnd.NextDouble() * 340f - 170f, z = (float)rnd.NextDouble() * 340f - 170f;
                float h = TerrainShape.HeightAt(x, z, 1337);
                Assert.LessOrEqual(Mathf.Abs(TerrainShape.HeightAt(x + step, z, 1337) - h), 0.4f, $"x={x} z={z}");
                Assert.LessOrEqual(Mathf.Abs(TerrainShape.HeightAt(x, z + step, 1337) - h), 0.4f, $"x={x} z={z}");
            }
        }

        [Test]
        public void RimRisesExceptAtTheNorthEntry()
        {
            Assert.GreaterOrEqual(TerrainShape.HeightAt(245f, 0f, 1337), 15f);
            Assert.GreaterOrEqual(TerrainShape.HeightAt(0f, -245f, 1337), 15f);
            Assert.GreaterOrEqual(TerrainShape.HeightAt(-245f, 100f, 1337), 15f);
            Assert.LessOrEqual(TerrainShape.HeightAt(ZoneLayout.NorthEntry.x, ZoneLayout.NorthEntry.y, 1337), 3f);
        }

        [Test]
        public void HeightsArrayShapeAndRange()
        {
            var h = TerrainShape.Heights(65, 1);
            Assert.AreEqual(65, h.GetLength(0));
            foreach (float v in h) { Assert.GreaterOrEqual(v, 0f); Assert.LessOrEqual(v, 1f); }
        }
    }
}
