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
            for (int i = 0; i < ZoneLayout.Zones.Length; i++)
            {
                if (i == 4) continue;   // the gorge: its middle is the stream
                var pad = ZoneLayout.Zones[i];
                float sum = 0f, sum2 = 0f; int n = 0;
                for (float dx = -20f; dx <= 20f; dx += 4f)
                    for (float dz = -20f; dz <= 20f; dz += 4f)
                    {
                        if (dx * dx + dz * dz > 400f) continue;
                        float h = TerrainShape.HeightAt(pad.Center.x + dx, pad.Center.y + dz, 1337) - ZoneLayout.ZoneLevels[i];
                        Assert.LessOrEqual(Mathf.Abs(h), 2.7f, pad.Name);
                        sum += h; sum2 += h * h; n++;
                    }
                float mean = sum / n;
                Assert.GreaterOrEqual(Mathf.Sqrt(sum2 / n - mean * mean), 0.2f, pad.Name + " is too flat");
            }
        }

        [Test]
        public void HillsRiseInTheWildBetweenAreas()
        {
            float best = 0f;
            for (float x = -170f; x <= 170f; x += 10f)
                for (float z = -170f; z <= 170f; z += 10f)
                    if (ZoneLayout.MaxInfluence(x, z) < 0.1f) best = Mathf.Max(best, TerrainShape.HeightAt(x, z, 1337));
            Assert.GreaterOrEqual(best, 4f);
        }

        [Test]
        public void TrailsAreWalkable()
        {
            const float step = 0.5f;
            foreach (var path in ZoneLayout.Paths)
                for (int j = 0; j + 1 < path.Length; j++)
                {
                    int n = Mathf.CeilToInt(Vector2.Distance(path[j], path[j + 1]) / step);
                    float prev = float.NaN;
                    for (int i = 0; i <= n; i++)
                    {
                        var p = Vector2.Lerp(path[j], path[j + 1], i / (float)n);
                        float h = TerrainShape.HeightAt(p.x, p.y, 1337);
                        if (!float.IsNaN(prev)) Assert.LessOrEqual(Mathf.Abs(h - prev) / step, 0.6f, $"trail at {p}");   // about 31 degrees
                        prev = h;
                    }
                }
        }

        [Test]
        public void ThePlateauHasAWallAwayFromItsTrail()
        {
            // somewhere round the plateau the ground climbs at least 4 m within 6 m: a wall, not a slope
            var c = ZoneLayout.Spore.Center;
            bool wall = false;
            for (int a = 0; a < 32 && !wall; a++)
            {
                Vector2 dir = new Vector2(Mathf.Cos(a * Mathf.PI / 16f), Mathf.Sin(a * Mathf.PI / 16f));
                for (float r = 20f; r < 90f && !wall; r += 1f)
                {
                    Vector2 p = c + dir * r, q = c + dir * (r + 6f);
                    if (TerrainShape.HeightAt(p.x, p.y, 1337) - TerrainShape.HeightAt(q.x, q.y, 1337) >= 4f) wall = true;
                }
            }
            Assert.IsTrue(wall);
        }

        [Test]
        public void RimRisesExceptAtTheNorthEntry()
        {
            Assert.GreaterOrEqual(TerrainShape.HeightAt(245f, 0f, 1337), 15f);
            Assert.GreaterOrEqual(TerrainShape.HeightAt(0f, -245f, 1337), 15f);
            Assert.GreaterOrEqual(TerrainShape.HeightAt(-245f, 0f, 1337), 15f);
            Assert.LessOrEqual(TerrainShape.HeightAt(ZoneLayout.NorthEntry.x, ZoneLayout.NorthEntry.y, 1337), 9f);   // the ridge (6 m) leads out through the gap
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
