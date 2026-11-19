using System;
using System.Collections.Generic;
using Evolve.Maze;
using NUnit.Framework;

namespace Evolve.Tests.EditMode.Maze
{
    public class MazeGraphTests
    {
        [TestCase(12, 101)]
        [TestCase(14, 2026)]
        [TestCase(16, 9999)]
        public void Generate_RespectsChamberCountAndThreeDepths(int count, int seed)
        {
            var graph = MazeGenerator.Generate(seed, count);

            Assert.AreEqual(count, graph.Chambers.Count);
            Assert.IsTrue(HasDepth(graph, ChamberDepth.Nong), "Phải có tầng Nông.");
            Assert.IsTrue(HasDepth(graph, ChamberDepth.Giua), "Phải có tầng Giữa.");
            Assert.IsTrue(HasDepth(graph, ChamberDepth.Sau), "Phải có tầng Sâu.");
            Assert.AreEqual(ChamberKind.BirthCavern, graph.GetChamber(graph.BirthChamberId).Kind);
            Assert.AreEqual(ChamberKind.SealedCavern, graph.GetChamber(graph.SealedChamberId).Kind);
            Assert.AreEqual(ChamberDepth.Nong, graph.GetChamber(graph.BirthChamberId).Depth);
            Assert.AreEqual(ChamberDepth.Sau, graph.GetChamber(graph.SealedChamberId).Depth);
        }

        [TestCase(12)]
        [TestCase(14)]
        [TestCase(16)]
        public void Generate_AlwaysHasPathFromBirthToSealed_AndIsConnected(int count)
        {
            for (int seed = 1; seed <= 20; seed++)
            {
                var graph = MazeGenerator.Generate(seed, count);
                Assert.IsTrue(graph.HasPath(graph.BirthChamberId, graph.SealedChamberId),
                    $"Seed {seed}: phải luôn có đường từ Hang Khởi Sinh đến Khoang Phong Ấn.");
                Assert.IsTrue(graph.IsFullyConnected(),
                    $"Seed {seed}: mọi khoang phải liên thông từ Hang Khởi Sinh.");
                Assert.IsTrue(HasObstacleFreePath(graph, graph.BirthChamberId, graph.SealedChamberId),
                    $"Seed {seed}: phải có ít nhất một đường thông suốt không chướng ngại tới Khoang Phong Ấn.");
            }
        }

        [Test]
        public void Generate_MainChambersHave2To3Tunnels_DeadEndsHave1Tunnel()
        {
            for (int seed = 10; seed <= 30; seed++)
            {
                var graph = MazeGenerator.Generate(seed, 14);
                int deadEnds = 0;
                int shelters = 0;

                foreach (var c in graph.Chambers)
                {
                    int deg = graph.CountTunnels(c.Id);
                    if (c.Kind == ChamberKind.DeadEnd)
                    {
                        deadEnds++;
                        Assert.AreEqual(1, deg, $"Seed {seed}: Lối giả {c.Id} phải là ngõ cụt (1 hầm).");
                        var edge = graph.GetTunnels(c.Id)[0];
                        Assert.AreEqual(TunnelSlope.Up, edge.SlopeFrom(edge.OtherEnd(c.Id)),
                            $"Seed {seed}: Lối giả {c.Id} phải dẫn lên cao từ khoang nối.");
                    }
                    else
                    {
                        Assert.That(deg, Is.InRange(2, 3),
                            $"Seed {seed}: Khoang {c.Id} ({c.Kind}) phải có 2–3 hầm, thực tế {deg}.");
                    }

                    if (c.Kind == ChamberKind.Shelter) shelters++;
                }

                Assert.GreaterOrEqual(deadEnds, 1, "Phải có ít nhất 1 lối giả (ngõ cụt).");
                Assert.GreaterOrEqual(shelters, 3, "Phải có hang trú rải ở 3 tầng.");
            }
        }

        [Test]
        public void Generate_SameSeedProducesIdenticalMaze_DifferentSeedDiffers()
        {
            var a1 = MazeGenerator.Generate(12345, 14);
            var a2 = MazeGenerator.Generate(12345, 14);
            var b = MazeGenerator.Generate(54321, 14);

            Assert.AreEqual(a1.Tunnels.Count, a2.Tunnels.Count);
            for (int i = 0; i < a1.Tunnels.Count; i++)
            {
                Assert.AreEqual(a1.Tunnels[i].FromId, a2.Tunnels[i].FromId);
                Assert.AreEqual(a1.Tunnels[i].ToId, a2.Tunnels[i].ToId);
                Assert.AreEqual(a1.Tunnels[i].Size, a2.Tunnels[i].Size);
                Assert.AreEqual(a1.Tunnels[i].Obstacle, a2.Tunnels[i].Obstacle);
            }

            bool differs = false;
            int minT = Math.Min(a1.Tunnels.Count, b.Tunnels.Count);
            if (a1.Tunnels.Count != b.Tunnels.Count) differs = true;
            for (int i = 0; i < minT && !differs; i++)
            {
                if (a1.Tunnels[i].FromId != b.Tunnels[i].FromId || a1.Tunnels[i].ToId != b.Tunnels[i].ToId)
                    differs = true;
            }
            Assert.IsTrue(differs, "Hai seed khác nhau phải cho mê cung khác nhau.");
        }

        [Test]
        public void Generate_InvalidChamberCount_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => MazeGenerator.Generate(1, 11));
            Assert.Throws<ArgumentOutOfRangeException>(() => MazeGenerator.Generate(1, 17));
        }

        static bool HasDepth(MazeGraph graph, ChamberDepth depth)
        {
            foreach (var c in graph.Chambers)
                if (c.Depth == depth) return true;
            return false;
        }

        static bool HasObstacleFreePath(MazeGraph graph, int fromId, int toId)
        {
            var visited = new HashSet<int> { fromId };
            var queue = new Queue<int>();
            queue.Enqueue(fromId);
            while (queue.Count > 0)
            {
                int cur = queue.Dequeue();
                if (cur == toId) return true;
                foreach (var t in graph.GetTunnels(cur))
                {
                    if (t.Obstacle != ObstacleKind.None) continue;
                    int next = t.OtherEnd(cur);
                    if (visited.Add(next)) queue.Enqueue(next);
                }
            }
            return false;
        }
    }
}
