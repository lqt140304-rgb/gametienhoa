using Evolve.World;
using NUnit.Framework;
using UnityEngine;

namespace Evolve.Tests.EditMode
{
    public class VegetationScatterTests
    {
        static bool None(float x, float z) => false;

        [Test]
        public void NeverExceedsCap()
        {
            var pts = VegetationScatter.Points(1, None);
            Assert.LessOrEqual(pts.Count, VegetationScatter.MaxPoints);
            Assert.Greater(pts.Count, 150);
        }

        [Test]
        public void AllPointsInsideReach()
        {
            foreach (var p in VegetationScatter.Points(1, None))
                Assert.LessOrEqual(Mathf.Max(Mathf.Abs(p.Position.x), Mathf.Abs(p.Position.y)), VegetationScatter.Reach + 1e-3f);
        }

        [Test]
        public void SameSeedSameResultDifferentSeedDiffers()
        {
            var a = VegetationScatter.Points(5, None);
            var b = VegetationScatter.Points(5, None);
            var c = VegetationScatter.Points(6, None);
            Assert.AreEqual(a.Count, b.Count);
            for (int i = 0; i < a.Count; i++) Assert.AreEqual(a[i].Position, b[i].Position);
            bool differs = a.Count != c.Count;
            for (int i = 0; i < Mathf.Min(a.Count, c.Count) && !differs; i++) if (a[i].Position != c[i].Position) differs = true;
            Assert.IsTrue(differs);
        }

        [Test]
        public void RespectsMinSpacing()
        {
            var pts = VegetationScatter.Points(1, None);
            for (int i = 0; i < pts.Count; i++)
                for (int j = i + 1; j < pts.Count; j++)
                    Assert.GreaterOrEqual(Vector2.Distance(pts[i].Position, pts[j].Position), VegetationScatter.MinSpacing - 1e-3f);
        }

        [Test]
        public void RespectsBlockedPredicate()
        {
            var centre = new Vector2(170f, 0f);
            var pts = VegetationScatter.Points(1, (x, z) => Vector2.Distance(new Vector2(x, z), centre) < 30f);
            foreach (var p in pts) Assert.GreaterOrEqual(Vector2.Distance(p.Position, centre), 30f);
        }

        [Test]
        public void PathsAreDenserThanWild()
        {
            var pts = VegetationScatter.Points(1, None);
            int pathPoints = 0, wildPoints = 0;
            foreach (var p in pts)
            {
                if (ZoneLayout.DistanceToPath(p.Position.x, p.Position.y) <= VegetationScatter.PathReach) pathPoints++;
                else if (VegetationScatter.DensityAt(p.Position.x, p.Position.y) == 0.04f) wildPoints++;
            }
            int pathCells = 0, wildCells = 0; // area estimate: 5 m cells
            for (float x = -VegetationScatter.Reach; x <= VegetationScatter.Reach; x += 5f)
                for (float z = -VegetationScatter.Reach; z <= VegetationScatter.Reach; z += 5f)
                {
                    if (ZoneLayout.DistanceToPath(x, z) <= VegetationScatter.PathReach) pathCells++;
                    else if (VegetationScatter.DensityAt(x, z) == 0.04f) wildCells++;
                }
            Assert.Greater(pathCells, 0); Assert.Greater(wildCells, 0);
            float pathDensity = pathPoints / (float)pathCells, wildDensity = Mathf.Max(wildPoints, 1) / (float)wildCells;
            Assert.GreaterOrEqual(pathDensity / wildDensity, 5f);
        }

        [Test]
        public void HuntEdgeIsSparserThanHumus()
        {
            var pts = VegetationScatter.Points(1, None);
            int humus = 0, edge = 0;
            foreach (var p in pts)
            {
                if (ZoneLayout.DistanceToPath(p.Position.x, p.Position.y) <= VegetationScatter.PathReach) continue;
                if (Vector2.Distance(p.Position, ZoneLayout.Humus.Center) <= ZoneLayout.Humus.Radius) humus++;
                else if (Vector2.Distance(p.Position, ZoneLayout.HuntEdge.Center) <= ZoneLayout.HuntEdge.Radius) edge++;
            }
            Assert.GreaterOrEqual(humus, 2f * Mathf.Max(edge, 1));
        }

        [Test]
        public void KindsFollowZones()
        {
            Assert.AreEqual(VegetationKind.GlowMoss, VegetationScatter.KindAt(0f, 90f));   // on the north trail
            Assert.AreEqual(VegetationKind.AmberBud, VegetationScatter.KindAt(170f, 30f)); // Humus Bed, off the trails
            Assert.AreEqual(VegetationKind.SporePod, VegetationScatter.KindAt(-170f, 30f));
            Assert.AreEqual(VegetationKind.RootDew, VegetationScatter.KindAt(40f, 170f));
            Assert.AreEqual(VegetationKind.EmberBerry, VegetationScatter.KindAt(190f, -190f));
            Assert.AreEqual(VegetationKind.GlowMoss, VegetationScatter.KindAt(-100f, -150f)); // the wild
        }

        [Test]
        public void DensityIsZeroOutsideReach()
        {
            Assert.AreEqual(0f, VegetationScatter.DensityAt(210f, 0f));
            Assert.AreEqual(0.4f, VegetationScatter.DensityAt(0f, 0f), 1e-5f);
            Assert.AreEqual(1f, VegetationScatter.DensityAt(0f, 90f), 1e-5f);
        }
    }
}
