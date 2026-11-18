using Evolve.World;
using NUnit.Framework;
using UnityEngine;

namespace Evolve.Tests.EditMode
{
    public class LandmarkLayoutTests
    {
        [Test] public void TheRootBridgeSpansFromThePlateauToTheRidge()
        {
            Assert.IsTrue(LandmarkLayout.RootBridge(1337, out var a, out var b));
            Assert.GreaterOrEqual(a.y, 9f, "starts on the plateau");
            Assert.GreaterOrEqual(b.y, 5.5f, "ends on the ridge");
            float span = Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
            Assert.GreaterOrEqual(span, 15f);
            Assert.LessOrEqual(span, 80f);
        }

        [Test] public void TheLogLiesOnTheDryFloorOfTheSinkhole()
        {
            var c = LandmarkLayout.LogCenter;
            Assert.AreEqual(0f, ZoneLayout.DepthAt(c.x, c.y));
            Assert.AreEqual(1f, ZoneLayout.Influence(2, c.x, c.y), 0.05f, "inside the humus bed");
            Assert.Greater(ZoneLayout.DistanceToPath(c.x, c.y), LandmarkLayout.LogLength * 0.5f + 1f, "off the trails");
        }
    }
}
