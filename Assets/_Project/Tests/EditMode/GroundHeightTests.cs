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
            Assert.IsNull(Terrain.activeTerrain); // EditMode test scene has no Terrain
            Assert.AreEqual(3.5f, GroundHeight.At(10f, 20f, 3.5f));
            Assert.AreEqual(0f, GroundHeight.OffsetOf(new Vector3(1f, 0.4f, 2f)));
            Assert.AreEqual(2f, GroundHeight.Follow(new Vector3(1f, 2f, 3f), 0f).y, 1e-5f);
            Assert.AreEqual(2.2f, GroundHeight.Follow(new Vector3(1f, 2f, 3f), 0.2f).y, 1e-5f);
        }
    }
}
