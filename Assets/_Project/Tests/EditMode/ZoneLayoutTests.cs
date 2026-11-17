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
        public void ZonesMatchTheDesign()
        {
            Assert.AreEqual(500f, WorldScale.ZoneSize);
            AssertZone(ZoneLayout.LifeShallows, -8f, -135f, 80f, 70f);
            AssertZone(ZoneLayout.RootFoot, -165f, -15f, 70f, 60f);
            AssertZone(ZoneLayout.Humus, -140f, 135f, 80f, 70f);
            AssertZone(ZoneLayout.Spore, 150f, 125f, 70f, 70f);
            AssertZone(ZoneLayout.WaterVein, 173f, -12f, 90f, 40f);
            AssertZone(ZoneLayout.HuntEdge, 10f, 168f, 80f, 55f);
        }

        [Test]
        public void NeighbouringAreasAreWellApart()
        {
            var z = ZoneLayout.Zones;
            for (int i = 0; i < z.Length; i++)
                for (int j = i + 1; j < z.Length; j++)
                {
                    float dx = Mathf.Max(0f, Mathf.Abs(z[i].Center.x - z[j].Center.x) - (z[i].Size.x + z[j].Size.x) * 0.5f);
                    float dz = Mathf.Max(0f, Mathf.Abs(z[i].Center.y - z[j].Center.y) - (z[i].Size.y + z[j].Size.y) * 0.5f);
                    Assert.GreaterOrEqual(Mathf.Sqrt(dx * dx + dz * dz), 65f, z[i].Name + " / " + z[j].Name);
                }
        }

        [Test]
        public void AreaLevelsMatchTheDesign()
        {
            float[] expected = { 0f, -1f, -6f, 10f, 8f, 6f };   // lake, cave, sinkhole, plateau, gorge rim, ridge
            CollectionAssert.AreEqual(expected, ZoneLayout.ZoneLevels);
            for (int i = 0; i < ZoneLayout.Zones.Length; i++)
            {
                if (i == 4) continue;   // the gorge's centre is its stream, at the lake level
                var c = ZoneLayout.Zones[i].Center;
                Assert.AreEqual(expected[i], ZoneLayout.LevelAt(c.x, c.y), 0.05f, ZoneLayout.Zones[i].Name);
            }
            var s = ZoneLayout.StreamLine[ZoneLayout.StreamLine.Length / 4];
            Assert.AreEqual(0f, ZoneLayout.LevelAt(s.x, s.y), 0.05f, "the stream runs at the bottom of the gorge");
        }

        [Test]
        public void InfluenceIsOneInTheCoreAndFadesOut()
        {
            for (int i = 0; i < ZoneLayout.Zones.Length; i++)
            {
                var pad = ZoneLayout.Zones[i];
                Assert.AreEqual(1f, ZoneLayout.Influence(i, pad.Center.x, pad.Center.y), 1e-5f, pad.Name);
                float far = Mathf.Max(pad.Size.x, pad.Size.y) * 0.5f + 90f;
                for (int a = 0; a < 16; a++)
                {
                    float ang = a * Mathf.PI / 8f;
                    Assert.Less(ZoneLayout.Influence(i, pad.Center.x + Mathf.Cos(ang) * far, pad.Center.y + Mathf.Sin(ang) * far), 0.05f, pad.Name);
                }
            }
        }

        [Test]
        public void DepthsMatchTheDesign()
        {
            float middle = ZoneLayout.DepthAt(ZoneLayout.LakeCenter.x, ZoneLayout.LakeCenter.y);
            Assert.GreaterOrEqual(middle, 1.2f); Assert.LessOrEqual(middle, 1.8f);        // deep enough to swim
            foreach (var pool in ZoneLayout.PoolCenters) Assert.AreEqual(2f, ZoneLayout.DepthAt(pool.x, pool.y), 1e-4f);
            var s = ZoneLayout.StreamLine[ZoneLayout.StreamLine.Length / 4];
            Assert.AreEqual(0.15f, ZoneLayout.DepthAt(s.x, s.y), 1e-4f);
            Assert.AreEqual(0.3f, ZoneLayout.DepthAt(ZoneLayout.PuddleCenter.x, ZoneLayout.PuddleCenter.y), 1e-4f);
            Assert.IsTrue(ZoneLayout.InPuddle(ZoneLayout.PuddleCenter.x, ZoneLayout.PuddleCenter.y));
            Assert.AreEqual(0f, ZoneLayout.CleanDepthAt(ZoneLayout.PuddleCenter.x, ZoneLayout.PuddleCenter.y));
            foreach (var pad in ZoneLayout.Biomes) Assert.AreEqual(0f, ZoneLayout.DepthAt(pad.Center.x, pad.Center.y), pad.Name);
            Assert.LessOrEqual(ZoneLayout.DepthAt(ZoneLayout.LifeShallows.Center.x, ZoneLayout.LifeShallows.Center.y), ZoneLayout.BayDepth + 1e-4f); // the bay is wadeable
        }

        [Test]
        public void StreamWidthWanders()
        {
            float min = float.MaxValue, max = 0f;
            for (float t = 0f; t <= 1f; t += 0.01f)
            {
                float w = ZoneLayout.StreamHalfWidthAt(t) * 2f;
                min = Mathf.Min(min, w); max = Mathf.Max(max, w);
                Assert.GreaterOrEqual(w, 7f - 1e-4f); Assert.LessOrEqual(w, 12f + 1e-4f);
            }
            Assert.Greater(max - min, 2f);
        }

        [Test]
        public void LakeIsNotAnEllipse()
        {
            float min = float.MaxValue, max = 0f;
            for (int i = 0; i < 64; i++)
            {
                float a = i * Mathf.PI * 2f / 64f, c = Mathf.Cos(a), s = Mathf.Sin(a);
                float ellipse = 1f / Mathf.Sqrt(c * c / (ZoneLayout.LakeRadii.x * ZoneLayout.LakeRadii.x) + s * s / (ZoneLayout.LakeRadii.y * ZoneLayout.LakeRadii.y));
                float ratio = ZoneLayout.LakeRadiusAt(a) / ellipse;
                min = Mathf.Min(min, ratio); max = Mathf.Max(max, ratio);
            }
            Assert.GreaterOrEqual(max / min, 1.4f);   // bays and points, not an oval
        }

        [Test]
        public void ShorelinesLieOnTheWaterEdge()
        {
            foreach (var p in ZoneLayout.LakeShore(64))
            {
                Vector2 outward = (p - ZoneLayout.LakeCenter).normalized;
                Assert.Greater(ZoneLayout.DepthAt(p.x - outward.x * 3f, p.y - outward.y * 3f), 0f, $"inside {p}");
            }
            for (int i = 0; i < ZoneLayout.PoolCenters.Length; i++)
                foreach (var p in ZoneLayout.PoolShore(i, 32))
                {
                    Vector2 inward = (ZoneLayout.PoolCenters[i] - p).normalized;
                    Assert.Greater(ZoneLayout.DepthAt(p.x + inward.x * 2f, p.y + inward.y * 2f), 0f, $"pool {i} inside {p}");
                }
        }

        [Test]
        public void TrailsStayOnDryLandOrFordTheStream()
        {
            foreach (var path in ZoneLayout.Paths)
                for (int j = 0; j + 1 < path.Length; j++)
                    for (float t = 0f; t <= 1f; t += 0.025f)
                    {
                        var p = Vector2.Lerp(path[j], path[j + 1], t);
                        Assert.LessOrEqual(ZoneLayout.DepthAt(p.x, p.y), ZoneLayout.StreamDepth + 1e-4f, $"trail point {p}");
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
                        if (pad.DistanceOutside(point) <= 0f) reached = true;
                Assert.IsTrue(reached, pad.Name);
            }
        }

        [Test]
        public void NoiseIsSmoothAndInRange()
        {
            for (float x = -3f; x < 3f; x += 0.37f)
            {
                float n = ZoneLayout.Noise(x, 1.3f);
                Assert.GreaterOrEqual(n, 0f); Assert.LessOrEqual(n, 1f);
                Assert.Less(Mathf.Abs(ZoneLayout.Noise(x + 0.01f, 1.3f) - n), 0.05f);
                Assert.AreEqual(n, ZoneLayout.Noise(x, 1.3f));
            }
        }
    }
}
