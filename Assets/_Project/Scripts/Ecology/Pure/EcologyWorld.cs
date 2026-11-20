using System.Collections.Generic;
using System.Linq;

namespace Evolve.Ecology
{
    /// <summary>
    /// All the caves' populations and the tunnels between them. Ticks every region (long-range hunters can reach the neighbours),
    /// then lets crowded or starving species send migrants to a neighbouring cave. Pure: the scene reads and writes it.
    /// </summary>
    public sealed class EcologyWorld
    {
        public const float CrowdedFill = 0.9f, HungryMinutes = 3f, MoveShare = 0.2f, CooldownMinutes = 5f;
        public const int MinCapacityToMigrate = 3;

        public readonly SortedDictionary<string, RegionPopulation> Regions = new SortedDictionary<string, RegionPopulation>(System.StringComparer.Ordinal);
        readonly Dictionary<string, List<string>> links = new Dictionary<string, List<string>>();

        public RegionPopulation Add(string id, RegionPopulation region)
        {
            Regions[id] = region;
            region.Resolver = Find;
            return region;
        }

        public RegionPopulation Find(string id) => id != null && Regions.TryGetValue(id, out var r) ? r : null;

        /// <summary>A tunnel between two caves (both ways).</summary>
        public void Link(string a, string b)
        {
            if (!links.TryGetValue(a, out var la)) links[a] = la = new List<string>();
            if (!links.TryGetValue(b, out var lb)) links[b] = lb = new List<string>();
            if (!la.Contains(b)) la.Add(b);
            if (!lb.Contains(a)) lb.Add(a);
        }

        public IReadOnlyList<string> Neighbours(string id) => links.TryGetValue(id, out var l) ? l : (IReadOnlyList<string>)new string[0];

        public void Tick(float minutes)
        {
            if (minutes <= 0f) return;
            foreach (var r in Regions.Values) r.Tick(minutes);
            Migrate(minutes);
        }

        /// <summary>Crowded (90% of capacity) or starving (3 minutes under half fed) species send about 20% of their animals to a neighbour as a guest colony. Returns how many moved.</summary>
        public int Migrate(float minutes)
        {
            int moved = 0;
            foreach (var r in Regions.Values) r.RemoveEmptyGuests();
            foreach (var kv in Regions)
                foreach (var s in kv.Value.Species.ToArray())
                {
                    if (!s.Migrates || s.Guest || s.Capacity < MinCapacityToMigrate) continue;
                    s.MigrateWait = System.Math.Max(0f, s.MigrateWait - minutes);
                    if (s.MigrateWait > 0f || s.Available < 2) continue;
                    bool crowded = s.Count >= CrowdedFill * s.Capacity, hungry = s.HungryMinutes >= HungryMinutes;
                    if (!crowded && !hungry) continue;

                    string best = null; int bestGuests = int.MaxValue;
                    foreach (var n in Neighbours(kv.Key))
                    {
                        var g = Find(n)?.Get(s.Id);
                        int guests = g != null ? g.Count : 0;
                        if (g != null && g.Count >= g.Capacity) continue;   // that cave's colony is full
                        if (guests < bestGuests) { bestGuests = guests; best = n; }
                    }
                    if (best == null) continue;

                    var target = Find(best);
                    var colony = target.Get(s.Id);
                    if (colony == null)
                        colony = target.Add(new SpeciesPopulation
                        {
                            Id = s.Id, Capacity = s.Capacity, Guest = true, MealsPerHeadPerMinute = s.MealsPerHeadPerMinute, HalfSaturation = s.HalfSaturation,
                            StarveRatePerMinute = s.StarveRatePerMinute, BodyFood = s.BodyFood, EscapeChance = s.EscapeChance, Count = 0,
                        });
                    if (colony.Diet.Count == 0) foreach (var d in s.Diet) colony.Diet.Add(d);
                    int n2 = (int)System.Math.Ceiling(MoveShare * s.Available);
                    if (n2 > s.Available - 1) n2 = s.Available - 1;
                    int room = colony.Capacity - colony.Count;
                    if (n2 > room) n2 = room;
                    if (n2 <= 0) continue;
                    s.Count -= n2; colony.Count += n2; moved += n2;
                    s.MigrateWait = CooldownMinutes;
                }
            return moved;
        }
    }
}
