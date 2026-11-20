using System.Linq;
using Evolve.Maze;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    public class ChamberDensityTests
    {
        [Test] public void Multiplier_StartCavernOne_WideChamberMore_Capped()
        {
            Assert.AreEqual(1f, ChamberDensity.Multiplier(40f), 1e-4f);
            Assert.AreEqual(1f, ChamberDensity.Multiplier(20f), 1e-4f);
            Assert.AreEqual(1.875f, ChamberDensity.Multiplier(75f), 1e-4f);
            Assert.AreEqual(3f, ChamberDensity.Multiplier(400f), 1e-4f);
        }

        [Test] public void Scaled_NeverFewer_NoFloatRoundUp()
        {
            Assert.AreEqual(14, ChamberDensity.Scaled(7, 75f));
            Assert.AreEqual(1, ChamberDensity.Scaled(1, 40f));
            Assert.AreEqual(2, ChamberDensity.Scaled(1, 75f));
            Assert.AreEqual(0, ChamberDensity.Scaled(0, 75f));
            Assert.AreEqual(10, ChamberDensity.Scaled(10, 40f));
        }

        [Test] public void Nests_ByRadius_AndShareAddsUp()
        {
            Assert.AreEqual(1, ChamberDensity.NestsPerSpecies(40f));
            Assert.AreEqual(2, ChamberDensity.NestsPerSpecies(55f));
            Assert.AreEqual(3, ChamberDensity.NestsPerSpecies(75f));
            Assert.AreEqual(14, Enumerable.Range(0, 3).Sum(i => ChamberDensity.Share(14, 3, i)));
            Assert.AreEqual(5, ChamberDensity.Share(14, 3, 0));
            Assert.AreEqual(4, ChamberDensity.Share(14, 3, 2));
        }

        [Test] public void NestPositions_Deterministic_Spread_InsideFloor_AwayFromTunnels()
        {
            var tunnels = new[] { 0f, 120f, 240f };
            var legacy = new[] { (-13f, 8f), (14f, 6f) };
            var a = ChamberDensity.NestPositions(8, 55f, 7, tunnels, legacy);
            var b = ChamberDensity.NestPositions(8, 55f, 7, tunnels, legacy);
            CollectionAssert.AreEqual(a, b);
            Assert.GreaterOrEqual(a.Count, 6);
            foreach (var p in a)
            {
                float r = (float)System.Math.Sqrt(p.X * p.X + p.Z * p.Z);
                Assert.GreaterOrEqual(r, 55f * 0.4f - 0.01f); Assert.LessOrEqual(r, 55f * 0.85f + 0.01f);
                double ang = (System.Math.Atan2(p.Z, p.X) * 180.0 / System.Math.PI + 360.0) % 360.0;
                foreach (float t in tunnels) Assert.Greater(System.Math.Abs(((ang - t) % 360.0 + 540.0) % 360.0 - 180.0), 21.9, "in front of a tunnel mouth");
                foreach (var l in legacy) Assert.GreaterOrEqual(System.Math.Sqrt((p.X - l.Item1) * (p.X - l.Item1) + (p.Z - l.Item2) * (p.Z - l.Item2)), 19.9);
            }
            for (int i = 0; i < a.Count; i++) for (int j = i + 1; j < a.Count; j++)
                Assert.GreaterOrEqual(System.Math.Sqrt((a[i].X - a[j].X) * (a[i].X - a[j].X) + (a[i].Z - a[j].Z) * (a[i].Z - a[j].Z)), 19.9);
        }
    }
}
