using Evolve.World;
using NUnit.Framework;
using UnityEngine;

namespace Evolve.Tests.EditMode
{
    public class WaterLookTests
    {
        [Test]
        public void DepthFade_ZeroAtSurfaceOneAtDeep()
        {
            Assert.AreEqual(0f, WaterLook.DepthFade(0f, 4f), 1e-5f);
            Assert.AreEqual(1f, WaterLook.DepthFade(4f, 4f), 1e-5f);
            float mid = WaterLook.DepthFade(2f, 4f);
            Assert.Greater(mid, 0f);
            Assert.Less(mid, 1f);
        }

        [Test]
        public void DepthFade_NeverOutOfRange()
        {
            Assert.AreEqual(0f, WaterLook.DepthFade(-3f, 4f), 1e-5f);
            Assert.AreEqual(1f, WaterLook.DepthFade(1000f, 4f), 1e-5f);
            Assert.AreEqual(1f, WaterLook.DepthFade(1f, 0f), 1e-5f); // zero fade distance: any depth is fully deep
        }

        [Test]
        public void DepthColor_EndpointsAndMid()
        {
            var shallow = new Color(0.2f, 0.8f, 0.8f, 0.4f);
            var deep = new Color(0.0f, 0.2f, 0.3f, 0.9f);
            Assert.AreEqual(shallow.g, WaterLook.DepthColor(shallow, deep, 0f, 4f).g, 1e-5f);
            Assert.AreEqual(deep.a, WaterLook.DepthColor(shallow, deep, 4f, 4f).a, 1e-5f);
            var m = WaterLook.DepthColor(shallow, deep, 2f, 4f);
            Assert.Greater(m.a, shallow.a);
            Assert.Less(m.a, deep.a);
        }

        [Test]
        public void Foam_OneAtEdgeZeroBeyondWidth()
        {
            Assert.AreEqual(1f, WaterLook.FoamAmount(0f, 0.4f), 1e-5f);
            Assert.AreEqual(0f, WaterLook.FoamAmount(0.4f, 0.4f), 1e-5f);
            Assert.AreEqual(0f, WaterLook.FoamAmount(5f, 0.4f), 1e-5f);
            Assert.AreEqual(0f, WaterLook.FoamAmount(1f, 0f), 1e-5f);
        }

        [Test]
        public void FogDensity_ClampedAndDecreasesWithVisibility()
        {
            Assert.Greater(WaterLook.FogDensityFor(50f), WaterLook.FogDensityFor(200f));
            Assert.AreEqual(0.03f, WaterLook.FogDensityFor(5f), 1e-6f);
            Assert.AreEqual(0.002f, WaterLook.FogDensityFor(5000f), 1e-6f);
            Assert.AreEqual(0.0125f, WaterLook.FogDensityFor(200f), 1e-5f);
        }

        [Test]
        public void ColoursAreDarkAndOpaque()
        {
            foreach (var c in new[] { WaterLook.AmbientSky, WaterLook.AmbientEquator, WaterLook.AmbientGround, WaterLook.FogColor })
            {
                Assert.LessOrEqual(Mathf.Max(c.r, c.g, c.b), 0.35f, "ambient and fog stay dark so the cave stays a cave");
                Assert.AreEqual(1f, c.a, 1e-5f);
            }
        }
    }
}
