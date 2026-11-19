using Evolve.World;
using NUnit.Framework;
using UnityEngine;

namespace Evolve.Tests.EditMode
{
    public class CavernMagicMathTests
    {
        [Test]
        public void TheBeamStartsOnTheRoofAndLandsInTheMiddleOfTheLake()
        {
            var top = CavernMagicMath.BeamTop;
            Assert.AreEqual(CavernLayout.CeilingY(top.x, top.z), top.y, 1e-3f, "the top is on the roof");
            var hit = CavernMagicMath.BeamHit;
            Assert.AreEqual(0f, hit.y, 0.05f, "it lands on the water surface (y = 0)");
            Assert.Less(Vector2.Distance(new Vector2(hit.x, hit.z), CavernLayout.LakeCenter), 1f, "in the middle of the lake");
        }

        [Test]
        public void ThePoolOfLightFitsOnTheLake()
        {
            Assert.Greater(CavernMagicMath.PoolRadius, CavernLayout.HoleRadius);
            Assert.LessOrEqual(CavernMagicMath.PoolRadius, CavernLayout.LakeMeanRadius * 0.7f, "the pool stays on the water");
        }

        [Test]
        public void TheFireflyRingIsOnTheFloorBetweenTheShoreAndTheWall()
        {
            Assert.Greater(CavernMagicMath.RingInner, CavernLayout.LakeMeanRadius * 0.5f);
            Assert.Less(CavernMagicMath.RingOuter, CavernLayout.WallTopRadius);
            Assert.AreEqual(CavernMagicMath.RingInner + CavernMagicMath.RingTubeRadius, CavernMagicMath.RingCentreRadius, 1e-4f);
            Assert.Less(CavernMagicMath.RingHeightHigh, CavernLayout.WallHeight);
            Assert.Greater(CavernMagicMath.RingHeightLow, 0f);
        }

        [Test]
        public void TheGlintsStayInsideTheShoreOnEverySide()
        {
            for (float a = 0f; a < Mathf.PI * 2f; a += 0.1f)
                Assert.LessOrEqual(CavernMagicMath.GlintRadius, CavernLayout.LakeRadius(a) + 1e-3f, $"angle {a}");
        }
    }
}
