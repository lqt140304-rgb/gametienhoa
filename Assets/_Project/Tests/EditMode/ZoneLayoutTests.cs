using Evolve.World;
using NUnit.Framework;
using UnityEngine;

namespace Evolve.Tests.EditMode
{
    public class ZoneLayoutTests
    {
        static void AssertZone(Pad pad, float x, float z, float w, float d)
        {
            Assert.AreEqual(new Vector2(x, z), pad.Center, pad.Name);
            Assert.AreEqual(new Vector2(w, d), pad.Size, pad.Name);
        }

        [Test]
        public void ZonesMatchTheConceptMap()
        {
            AssertZone(ZoneLayout.LifeShallows, 8f, -80f, 45f, 37.5f);   // concept 30 x 25 m, x 1.5
            AssertZone(ZoneLayout.RootFoot, -85f, -15f, 37.5f, 30f);
            AssertZone(ZoneLayout.Humus, -70f, 68f, 45f, 37.5f);
            AssertZone(ZoneLayout.Spore, 98f, 65f, 37.5f, 37.5f);
            AssertZone(ZoneLayout.WaterVein, 122f, -10f, 22.5f, 52.5f);
            AssertZone(ZoneLayout.HuntEdge, 20f, 90f, 45f, 30f);
            Assert.AreEqual(300f, WorldScale.ZoneSize);
        }

        [Test]
        public void ZonesStayInsideTheRim()
        {
            foreach (var pad in ZoneLayout.Zones)
            {
                Assert.LessOrEqual(Mathf.Abs(pad.Center.x) + pad.Size.x * 0.5f, 150f, pad.Name);
                Assert.LessOrEqual(Mathf.Abs(pad.Center.y) + pad.Size.y * 0.5f, 150f, pad.Name);
            }
        }

        [Test]
        public void ZoneCentresAreWellApart()
        {
            var z = ZoneLayout.Zones;
            for (int i = 0; i < z.Length; i++)
                for (int j = i + 1; j < z.Length; j++)
                    Assert.GreaterOrEqual(Vector2.Distance(z[i].Center, z[j].Center), 60f, z[i].Name + " / " + z[j].Name);
        }

        [Test]
        public void DepthsMatchTheDesign()
        {
            foreach (var pool in ZoneLayout.PoolCenters)
                Assert.AreEqual(2f, ZoneLayout.DepthAt(pool.x, pool.y), 1e-4f);
            Assert.AreEqual(0.15f, ZoneLayout.DepthAt(128f, 46f), 1e-4f);       // the stream, between the spring and the first pool
            Assert.AreEqual(0.4f, ZoneLayout.DepthAt(8f, -75f), 1e-4f);         // the Life Shallows
            Assert.AreEqual(1.6f, ZoneLayout.DepthAt(25f, -5f), 1e-4f);         // the middle of the lake
            foreach (var pad in ZoneLayout.Biomes)
                Assert.AreEqual(0f, ZoneLayout.DepthAt(pad.Center.x, pad.Center.y), pad.Name);
        }

        [Test]
        public void ShorelinesLieOnTheWaterEdge()
        {
            foreach (var p in ZoneLayout.LakeShore(64))
            {
                Vector2 inward = (ZoneLayout.LakeCenter - p).normalized;
                if (p.y > ZoneLayout.LakeCenter.y) // the south shore meets the bay and the stream
                    Assert.AreEqual(0f, ZoneLayout.DepthAt(p.x - inward.x * 0.5f, p.y - inward.y * 0.5f), 1e-4f, $"just outside {p}");
                Assert.Greater(ZoneLayout.DepthAt(p.x + inward.x * 3f, p.y + inward.y * 3f), 0f, $"inside {p}");
            }
            foreach (var p in ZoneLayout.ShallowsShore(64))
            {
                Vector2 inward = (ZoneLayout.ShallowsCenter - p).normalized;
                Assert.Greater(ZoneLayout.DepthAt(p.x + inward.x * 3f, p.y + inward.y * 3f), 0f, $"inside {p}");
            }
        }

        [Test]
        public void TrailsStayOnDryLandOrFordTheStream()
        {
            foreach (var path in ZoneLayout.Paths)
                for (int j = 0; j + 1 < path.Length; j++)
                    for (float t = 0f; t <= 1f; t += 0.05f)
                    {
                        var p = Vector2.Lerp(path[j], path[j + 1], t);
                        Assert.LessOrEqual(ZoneLayout.DepthAt(p.x, p.y), ZoneLayout.StreamDepth + 1e-4f, $"trail point {p}"); // dry, or a ford over the stream
                    }
        }

        [Test]
        public void TrailsReachEveryArea()
        {
            foreach (var pad in ZoneLayout.Zones)
            {
                bool reached = false;
                foreach (var path in ZoneLayout.Paths)
                    foreach (var point in path)
                        if (pad.DistanceOutside(point) <= 12f) reached = true; // the Life Shallows is mostly water: trails stop at its shore
                Assert.IsTrue(reached, pad.Name);
            }
        }

        [Test]
        public void DistanceToPathZeroOnPath()
        {
            var a = ZoneLayout.Paths[0][0];
            var b = ZoneLayout.Paths[0][1];
            var mid = (a + b) * 0.5f;
            Assert.AreEqual(0f, ZoneLayout.DistanceToPath(mid.x, mid.y), 1e-4f);
        }

        [Test]
        public void FlatMaskIsOneInAreasAndOnWater()
        {
            foreach (var pad in ZoneLayout.Zones)
                Assert.AreEqual(1f, ZoneLayout.FlatMask(pad.Center.x, pad.Center.y), 1e-5f, pad.Name);
            Assert.AreEqual(1f, ZoneLayout.FlatMask(25f, -5f), 1e-5f);
        }
    }
}
