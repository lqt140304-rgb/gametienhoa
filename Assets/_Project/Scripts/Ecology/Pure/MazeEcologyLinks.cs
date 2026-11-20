using System.Collections.Generic;
using System.Linq;
using Evolve.Maze;

namespace Evolve.Ecology
{
    /// <summary>
    /// Lays the ecology's tunnels along the real maze (<see cref="IMazeAdjacency"/>) instead of the placeholder chain in
    /// <see cref="CaveEcologySeed.Build"/>: two caves with creatures are neighbours when one can walk from one to the other
    /// through chambers that hold no ecology of their own (shelters, dead ends, plain halls).
    /// </summary>
    public static class MazeEcologyLinks
    {
        /// <summary>Ecology cave id of a chamber theme, or null when that chamber has no creature population.</summary>
        public static string CaveOf(ChamberTheme t) => t switch
        {
            ChamberTheme.BirthLake => "khoi_sinh",
            ChamberTheme.LumaMushroomGrove => "vuon_quang",
            ChamberTheme.BreathingMoss => "reu_tho",
            ChamberTheme.AbandonedNest => "to_bo_hoang",
            ChamberTheme.CrystalVein => "vuc_nut_tim",
            ChamberTheme.GlowMushroomCavern => "nam_huynh_quang",
            ChamberTheme.HotSpringCavern => "suoi_nuoc_nong",
            ChamberTheme.HowlingWindGorge => "khe_gio_hu",
            ChamberTheme.FossilBoneyard => "nghia_dia",
            _ => null,   // hoa_thach and tieng_tho have no chamber theme yet
        };

        public static int Apply(EcologyWorld w, MazeGraph g) => Apply(w, g.Chambers.Select(c => (c.Id, CaveOf(c.Theme))).ToList(), g);

        /// <summary>Replaces the world's tunnels with the maze's. Returns how many cave pairs got linked.</summary>
        public static int Apply(EcologyWorld w, IReadOnlyList<(int Id, string Cave)> chambers, IMazeAdjacency adj)
        {
            var caveOf = new Dictionary<int, string>();
            foreach (var c in chambers) if (c.Cave != null && w.Find(c.Cave) != null) caveOf[c.Id] = c.Cave;
            w.ClearLinks();
            var pairs = new HashSet<(string, string)>();
            foreach (var start in caveOf)
            {
                // walk out through creature-less chambers until hitting another cave
                var seen = new HashSet<int> { start.Key };
                var open = new Queue<int>(); open.Enqueue(start.Key);
                while (open.Count > 0)
                {
                    foreach (int n in adj.GetAdjacentChamberIds(open.Dequeue()))
                    {
                        if (!seen.Add(n)) continue;
                        if (caveOf.TryGetValue(n, out var other))
                        {
                            if (other == start.Value) continue;
                            var key = string.CompareOrdinal(start.Value, other) < 0 ? (start.Value, other) : (other, start.Value);
                            if (pairs.Add(key)) w.Link(key.Item1, key.Item2);
                        }
                        else open.Enqueue(n);
                    }
                }
            }
            return pairs.Count;
        }
    }
}
