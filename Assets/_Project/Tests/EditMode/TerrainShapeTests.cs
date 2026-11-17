using Evolve.World;
using NUnit.Framework;
using UnityEngine;

namespace Evolve.Tests.EditMode
{
    public class TerrainShapeTests
    {
        [Test]
        public void CrossingTakesAboutFiveMinutes()
        {
            Assert.AreEqual(5f, WorldScale.CrossingMinutes(WorldScale.ZoneSize, WorldScale.CrawlSpeed), 0.01f);
        }

        [Test]
        public void WaterIsCarvedToItsDepth()
        {
            foreach (var pool in ZoneLayout.PoolCenters)
                Assert.AreEqual(-2f, TerrainShape.HeightAt(pool.x, pool.y, 1337), 1e-4f);
            Assert.AreEqual(-0.15f, TerrainShape.HeightAt(128f, 46f, 1337), 1e-4f);
            Assert.AreEqual(-0.4f, TerrainShape.HeightAt(8f, -75f, 1337), 1e-4f);
        }

        [Test]
        public void AreaCentresAreFlat()
        {
            foreach (var pad in ZoneLayout.Biomes)
                for (float dx = -10f; dx <= 10f; dx += 5f)
                    for (float dz = -10f; dz <= 10f; dz += 5f)
                        Assert.AreEqual(0f, TerrainShape.HeightAt(pad.Center.x + dx, pad.Center.y + dz, 1337), 1e-4f, $"{pad.Name} dx={dx} dz={dz}");
        }

        [Test]
        public void HeightsBounded()
        {
            for (float x = -150f; x <= 150f; x += 5f)
                for (float z = -150f; z <= 150f; z += 5f)
                {
                    float h = TerrainShape.HeightAt(x, z, 1337);
                    Assert.GreaterOrEqual(h, -ZoneLayout.PoolDepth - 1e-4f, $"x={x} z={z}");
                    Assert.LessOrEqual(h, TerrainShape.CliffHeight + 1f, $"x={x} z={z}");
                }
        }

        [Test]
        public void InsideIsSmoothAndWalkable()
        {
            const float step = 0.6f, limit = 0.5f, range = 125f; // one heightmap cell; the deep pools fall 2 m over 3 m
            for (float x = -range; x <= range; x += step * 4f)
                for (float z = -range; z <= range; z += step * 4f)
                {
                    float h = TerrainShape.HeightAt(x, z, 1337);
                    Assert.LessOrEqual(Mathf.Abs(TerrainShape.HeightAt(x + step, z, 1337) - h), limit, $"x={x} z={z} (dx)");
                    Assert.LessOrEqual(Mathf.Abs(TerrainShape.HeightAt(x, z + step, 1337) - h), limit, $"x={x} z={z} (dz)");
                }
        }

        [Test]
        public void RimRisesExceptAtTheNorthEntry()
        {
            Assert.GreaterOrEqual(TerrainShape.HeightAt(148f, 0f, 1337), 20f);
            Assert.GreaterOrEqual(TerrainShape.HeightAt(0f, -148f, 1337), 20f);
            Assert.GreaterOrEqual(TerrainShape.HeightAt(-148f, 100f, 1337), 20f);
            Assert.LessOrEqual(TerrainShape.HeightAt(ZoneLayout.NorthEntry.x, 148f, 1337), 3.1f);
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
