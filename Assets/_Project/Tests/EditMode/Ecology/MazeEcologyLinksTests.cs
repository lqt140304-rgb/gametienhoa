using System.Collections.Generic;
using System.Linq;
using Evolve.Ecology;
using Evolve.Maze;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    public class MazeEcologyLinksTests
    {
        sealed class Adj : IMazeAdjacency
        {
            readonly Dictionary<int, List<int>> m = new Dictionary<int, List<int>>();
            public Adj Edge(int a, int b) { Add(a, b); Add(b, a); return this; }
            void Add(int a, int b) { if (!m.TryGetValue(a, out var l)) m[a] = l = new List<int>(); l.Add(b); }
            public IReadOnlyList<int> GetAdjacentChamberIds(int id) => m.TryGetValue(id, out var l) ? l : new List<int>();
            public bool AreAdjacent(int a, int b) => GetAdjacentChamberIds(a).Contains(b);
        }

        [Test] public void Links_FollowTheMaze_ThroughEmptyChambers()
        {
            var w = CaveEcologySeed.Build();
            // 0 khoi_sinh - 1 (shelter) - 2 vuc_nut_tim - 3 khe_gio_hu ; 4 reu_tho hangs off nowhere
            var chambers = new List<(int, string)> { (0, "khoi_sinh"), (1, null), (2, "vuc_nut_tim"), (3, "khe_gio_hu"), (4, "reu_tho") };
            int n = MazeEcologyLinks.Apply(w, chambers, new Adj().Edge(0, 1).Edge(1, 2).Edge(2, 3));
            Assert.AreEqual(2, n);
            CollectionAssert.AreEquivalent(new[] { "vuc_nut_tim" }, w.Neighbours("khoi_sinh"));
            CollectionAssert.AreEquivalent(new[] { "khoi_sinh", "khe_gio_hu" }, w.Neighbours("vuc_nut_tim"));
            Assert.AreEqual(0, w.Neighbours("reu_tho").Count);   // the placeholder links are gone
        }

        [Test] public void RealGeneratedMaze_LinksOnlyKnownCaves()
        {
            var g = MazeGenerator.Generate(2026, MazeGenerator.DefaultChambers);
            var w = CaveEcologySeed.Build();
            MazeEcologyLinks.Apply(w, g);
            var inMaze = new HashSet<string>(g.Chambers.Select(c => MazeEcologyLinks.CaveOf(c.Theme)).Where(c => c != null));
            Assert.Greater(inMaze.Count, 1);
            foreach (var cave in inMaze) Assert.Greater(w.Neighbours(cave).Count, 0, cave);   // the maze is connected
            foreach (var cave in CaveEcologySeed.CaveIds.Where(c => !inMaze.Contains(c))) Assert.AreEqual(0, w.Neighbours(cave).Count, cave);
        }

        [Test] public void CaveOfSpecies_ByPrefabName()
        {
            Assert.AreEqual("khoi_sinh", CaveEcologySeed.CaveOfSpecies(CaveEcologySeed.IdByName("ThanLanBoHo")));
            Assert.AreEqual("vuc_nut_tim", CaveEcologySeed.CaveOfSpecies("nhen_tinh_the"));
            Assert.IsNull(CaveEcologySeed.CaveOfSpecies("khong_co"));
            Assert.IsNull(CaveEcologySeed.CaveOfSpecies(null));
        }
    }
}
