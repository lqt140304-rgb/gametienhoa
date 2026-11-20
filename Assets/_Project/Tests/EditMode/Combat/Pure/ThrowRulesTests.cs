using System.Collections.Generic;
using Evolve.Combat;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    public class ThrowRulesTests
    {
        [Test] public void Launch_ReachesTheTargetAfterTheFlightTime()
        {
            var v = ThrowRules.Launch(0, 1, 0, 6, 0, 8, 0.9f);
            var end = ThrowRules.PositionAt(0, 1, 0, v, 0.9f);
            Assert.AreEqual(6f, end.x, 0.001f); Assert.AreEqual(0f, end.y, 0.001f); Assert.AreEqual(8f, end.z, 0.001f);
            var start = ThrowRules.PositionAt(0, 1, 0, v, 0f);
            Assert.AreEqual(0f, start.x, 1e-4f); Assert.AreEqual(1f, start.y, 1e-4f); Assert.AreEqual(0f, start.z, 1e-4f);
        }

        [Test] public void Launch_MakesAnArc()
        {
            var v = ThrowRules.Launch(0, 1, 0, 6, 0, 8, 0.9f);
            var mid = ThrowRules.PositionAt(0, 1, 0, v, 0.45f);
            Assert.Greater(mid.y, 0.5f);   // the straight line between 1 and 0 is 0.5 at the half-way time
        }

        [Test] public void Launch_HorizontalVelocityIsDistanceOverTime()
        {
            var v = ThrowRules.Launch(0, 1, 0, 6, 0, 8, 0.9f);
            Assert.AreEqual(6f / 0.9f, v.vx, 1e-3f); Assert.AreEqual(8f / 0.9f, v.vz, 1e-3f);
        }

        [Test] public void Launch_UpwardsToALedge()
        {
            var v = ThrowRules.Launch(0, 0, 0, 3, 2.2f, 4, 0.8f);
            var end = ThrowRules.PositionAt(0, 0, 0, v, 0.8f);
            Assert.AreEqual(2.2f, end.y, 0.001f); Assert.AreEqual(3f, end.x, 0.001f); Assert.AreEqual(4f, end.z, 0.001f);
        }

        [Test] public void Hits_WithinRadiusOnly()
        {
            Assert.IsTrue(ThrowRules.Hits(0.5f, 0, 0, 0, 0, 0, 0.6f));
            Assert.IsFalse(ThrowRules.Hits(0.7f, 0, 0, 0, 0, 0, 0.6f));
        }

        static readonly Ledge[] None = new Ledge[0];

        [Test] public void PickLedge_RiseWindow_AndDistance()
        {
            var ledges = new[] { new Ledge(3, 1f, 0), new Ledge(-3, 2f, 0), new Ledge(0, 4f, 3), new Ledge(9, 2f, 0) };
            Assert.AreEqual(1, PerchRules.PickLedge(ledges, 0, 0, 0, 20, 0));   // only the 2 m high ledge within 7 m is good
            Assert.AreEqual(-1, PerchRules.PickLedge(new[] { new Ledge(3, 1f, 0), new Ledge(0, 4f, 3), new Ledge(9, 2f, 0) }, 0, 0, 0, 20, 0));
        }

        [Test] public void PickLedge_PrefersFarthestFromThePlayer_AndNotTowardsThem()
        {
            // the player is at +x; the monkey at the origin
            var ledges = new[] { new Ledge(4, 2f, 0), new Ledge(-3, 2f, 0), new Ledge(-5, 2f, 0) };
            Assert.AreEqual(2, PerchRules.PickLedge(ledges, 0, 0, 0, 8, 0));   // the farthest from the player, and never the one towards them
            Assert.AreEqual(-1, PerchRules.PickLedge(new[] { new Ledge(4, 2f, 0) }, 0, 0, 0, 8, 0));
        }

        [Test] public void PickLedge_EmptyList_IsMinusOne() => Assert.AreEqual(-1, PerchRules.PickLedge(None, 0, 0, 0, 5, 5));
    }
}
