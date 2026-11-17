using Evolve.World;
using NUnit.Framework;
using UnityEngine;

namespace Evolve.Tests.EditMode
{
    public class ZoneTrackingTests
    {
        [SetUp] public void SetUp() => ResetWaterCount();
        [TearDown] public void TearDown() => ResetWaterCount();

        static void ResetWaterCount() => WaterZone.ResetStatics();

        [Test]
        public void SensorsHandleOverlap()
        {
            var s = new ZoneSet();
            var a = new object(); var b = new object();
            s.Enter(a); s.Enter(b); s.Enter(a);
            Assert.AreEqual(2, s.Count);
            s.Exit(a);
            Assert.AreEqual(1, s.Count);
            s.Exit(b);
            Assert.AreEqual(0, s.Count);
            s.Exit(b);
            Assert.AreEqual(0, s.Count);
        }

        [Test]
        public void PruneRemovesDestroyedZones()
        {
            var go = new GameObject("z");
            var keep = new object();
            var s = new ZoneSet();
            s.Enter(go); s.Enter(keep);
            Object.DestroyImmediate(go);
            s.Prune(o => o is Object u && u != null);
            Assert.AreEqual(0, s.Count);
            s.Enter(keep);
            s.Prune(o => true);
            Assert.AreEqual(1, s.Count);
        }

        [Test]
        public void WaterCountResetsOnLoad()
        {
            WaterZone.SetCountForTests(3);
            Assert.IsTrue(WaterZone.AnyInScene);
            WaterZone.ResetStatics();
            Assert.AreEqual(0, WaterZone.Count);
            Assert.IsFalse(WaterZone.AnyInScene);
        }
    }
}
