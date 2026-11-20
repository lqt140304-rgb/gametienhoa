using System;
using Evolve.Maze;
using NUnit.Framework;

namespace Evolve.Tests.EditMode.Maze
{
    public class MazeActivationTests
    {
        [Test]
        public void GetActiveChamberIds_ContainsOnlyCurrentAndNeighbors()
        {
            var graph = MazeGenerator.Generate(2026, 14);
            int current = graph.BirthChamberId;
            var active = MazeActivationRules.GetActiveChamberIds(graph, current);
            var neighbors = graph.GetNeighborIds(current);

            Assert.AreEqual(1 + neighbors.Count, active.Count);
            Assert.IsTrue(active.Contains(current));
            foreach (int n in neighbors)
                Assert.IsTrue(active.Contains(n));

            // Các khoang không kề không được bật
            foreach (var c in graph.Chambers)
            {
                if (c.Id == current) continue;
                bool isNeighbor = false;
                foreach (int n in neighbors)
                    if (n == c.Id) { isNeighbor = true; break; }
                Assert.AreEqual(isNeighbor, active.Contains(c.Id));
            }
        }

        [Test]
        public void ComputeChamberOrigin_PlacesBirthAtZero_AndSeparatesChambers()
        {
            var (x0, z0) = MazeActivationRules.ComputeChamberOrigin(0);
            Assert.AreEqual(0f, x0, 1e-4f);
            Assert.AreEqual(0f, z0, 1e-4f);

            for (int a = 0; a < 16; a++)
            {
                var (xa, za) = MazeActivationRules.ComputeChamberOrigin(a);
                for (int b = a + 1; b < 16; b++)
                {
                    var (xb, zb) = MazeActivationRules.ComputeChamberOrigin(b);
                    float dist = (float)Math.Sqrt((xa - xb) * (xa - xb) + (za - zb) * (za - zb));
                    Assert.GreaterOrEqual(dist, MazeActivationRules.DefaultChamberSpacing - 1e-3f);
                }
            }
        }
    }
}
