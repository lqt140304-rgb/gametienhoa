using Evolve.World;
using NUnit.Framework;
using UnityEngine;

namespace Evolve.Tests.EditMode
{
    public class ZoneLayoutTests
    {
        [Test]
        public void BiomesStayInsideWalkableRange()
        {
            foreach (var pad in ZoneLayout.Biomes)
                Assert.LessOrEqual(Mathf.Max(Mathf.Abs(pad.Center.x), Mathf.Abs(pad.Center.y)) + ZoneLayout.ContentExtent,
                    TerrainShape.CliffStart, pad.Name);
        }

        [Test]
        public void BiomesDoNotOverlapAndAreFarFromSpawn()
        {
            var b = ZoneLayout.Biomes;
            for (int i = 0; i < b.Length; i++)
                for (int j = i + 1; j < b.Length; j++)
                    Assert.GreaterOrEqual(Vector2.Distance(b[i].Center, b[j].Center), 100f, b[i].Name + " / " + b[j].Name);
            Assert.GreaterOrEqual(ZoneLayout.HuntEdge.Center.magnitude, 200f);
        }

        [Test]
        public void PathsConnectEveryBiome()
        {
            foreach (var pad in ZoneLayout.Biomes)
            {
                int ends = 0;
                foreach (var path in ZoneLayout.Paths)
                    foreach (var end in new[] { path[0], path[path.Length - 1] })
                        if (Vector2.Distance(end, pad.Center) <= pad.Radius) ends++;
                Assert.GreaterOrEqual(ends, 1, pad.Name);
                if (pad.Name == ZoneLayout.HuntEdge.Name) Assert.GreaterOrEqual(ends, 2, "the hunting edge needs an alternative route");
            }
        }

        [Test]
        public void DistanceToPathZeroOnPath()
        {
            var a = ZoneLayout.Paths[0][0];
            var b = ZoneLayout.Paths[0][1];
            var mid = (a + b) * 0.5f;
            Assert.AreEqual(0f, ZoneLayout.DistanceToPath(mid.x, mid.y), 1e-4f);
            Assert.Greater(ZoneLayout.DistanceToPath(60f, 60f), 5f);
        }

        [Test]
        public void PadMaskIsOneOnPadsAndZeroFar()
        {
            Assert.AreEqual(1f, ZoneLayout.PadMask(170f, 0f), 1e-5f);
            Assert.AreEqual(1f, ZoneLayout.PadMask(0f, -100f), 1e-5f); // on the stream
            Assert.AreEqual(0f, ZoneLayout.PadMask(-100f, -150f), 1e-5f); // far from every pad and the stream
        }
    }
}
