using Evolve.World;
using NUnit.Framework;
using UnityEngine;

namespace Evolve.Tests.EditMode
{
    public class SurfaceWorldTests
    {
        [Test]
        public void MeetsTheRimWallAtTheRimLevel()
        {
            // at the rim itself the surface is at the wall's top level, so there is no step where they meet (away from the north gap)
            foreach (var p in new[] { new Vector2(250f, 0f), new Vector2(-250f, 40f), new Vector2(60f, -250f) })
                Assert.AreEqual(TerrainShape.CliffHeight, SurfaceWorld.HeightAt(p.x, p.y, 1337), 0.5f, p.ToString());
        }

        [Test]
        public void HillsStayInRangeAndGrowWithDistance()
        {
            float low = float.MaxValue, high = float.MinValue;
            for (float x = -600f; x <= 600f; x += 25f)
                for (float z = -600f; z <= 600f; z += 25f)
                {
                    if (SurfaceWorld.DistanceFromRim(x, z) < 0f) continue;
                    // away from the north gap, never below the rim level and never above rim + 53 m
                    if (z > 0f && Mathf.Abs(x - ZoneLayout.NorthEntry.x) < ZoneLayout.NorthEntryHalfWidth + 20f) continue;
                    float h = SurfaceWorld.HeightAt(x, z, 1337);
                    low = Mathf.Min(low, h); high = Mathf.Max(high, h);
                }
            Assert.GreaterOrEqual(low, SurfaceWorld.RimLevel - 0.01f);
            Assert.LessOrEqual(high, SurfaceWorld.RimLevel + 53.5f);
            Assert.Greater(high - low, 20f, "the horizon should roll, not be a flat line");
        }

        [Test]
        public void NorthGapRunsOnAsACanyon()
        {
            var e = ZoneLayout.NorthEntry;
            Assert.Less(SurfaceWorld.HeightAt(e.x, 258f, 1337), SurfaceWorld.RimLevel - 30f);
        }

        [Test]
        public void DistanceFromRimIsChebyshev()
        {
            Assert.AreEqual(0f, SurfaceWorld.DistanceFromRim(250f, 10f), 1e-4f);
            Assert.AreEqual(50f, SurfaceWorld.DistanceFromRim(-30f, 300f), 1e-4f);
            Assert.Less(SurfaceWorld.DistanceFromRim(0f, 0f), 0f);
        }

        [Test]
        public void DeterministicBySeed() =>
            Assert.AreEqual(SurfaceWorld.HeightAt(400f, -300f, 5), SurfaceWorld.HeightAt(400f, -300f, 5), 0f);

        [Test]
        public void OpenSkyFogFollowsSight()
        {
            Assert.AreEqual(80f, ZoneAtmosphere.OpenSkyFogEnd(0f), 1e-4f);
            Assert.AreEqual(600f, ZoneAtmosphere.OpenSkyFogEnd(1f), 1e-4f);
            Assert.AreEqual(600f, ZoneAtmosphere.OpenSkyFogEnd(5f), 1e-4f);    // sight beyond 1 is clamped
            Assert.AreEqual(80f, ZoneAtmosphere.OpenSkyFogEnd(-1f), 1e-4f);
            Assert.AreEqual(Color.black, ZoneAtmosphere.OpenSkyFogColor(0f));
            var full = ZoneAtmosphere.OpenSkyFogColor(1f);
            Assert.Greater(full.b, full.r);   // a bluish haze, not black
            Assert.Greater(full.grayscale, 0.5f);
        }
    }
}
