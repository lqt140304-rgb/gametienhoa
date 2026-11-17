using Evolve.World;
using NUnit.Framework;
using UnityEngine;

namespace Evolve.Tests.EditMode
{
    public class GroundHeightTests
    {
        [Test]
        public void NoTerrainReturnsFallback()
        {
            // EditMode tests run in the open scene; with Underroot_Lake open there is a Terrain, so this check does not apply
            if (Terrain.activeTerrain != null) Assert.Ignore("Open a scene without a Terrain (e.g. Prototype_01) to run this test.");
            Assert.AreEqual(3.5f, GroundHeight.At(10f, 20f, 3.5f));
            Assert.AreEqual(0f, GroundHeight.OffsetOf(new Vector3(1f, 0.4f, 2f)));
            Assert.AreEqual(2f, GroundHeight.Follow(new Vector3(1f, 2f, 3f), 0f).y, 1e-5f);
            Assert.AreEqual(2.2f, GroundHeight.Follow(new Vector3(1f, 2f, 3f), 0.2f).y, 1e-5f);
        }
    }
}
