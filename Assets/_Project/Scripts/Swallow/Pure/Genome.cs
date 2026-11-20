using System.Collections.Generic;
using Evolve.Organs;

namespace Evolve.Swallow
{
    /// <summary>One learnable line of a species' genome.</summary>
    public readonly struct Gene
    {
        public readonly OrganSO Organ;
        public readonly GeneRarity Rarity;
        public Gene(OrganSO organ, GeneRarity rarity) { Organ = organ; Rarity = rarity; }
    }

    /// <summary>A species' genome (bảng di truyền), read from the drop table it already has.</summary>
    public static class Genome
    {
        /// <summary>Organs of the table as genes; legendary ones only on a pack leader; nulls and duplicates skipped.</summary>
        public static List<Gene> From(DropTableSO table, bool isLeader)
        {
            var genes = new List<Gene>();
            if (table == null || table.Organs == null) return genes;
            var seen = new HashSet<OrganSO>();
            foreach (var o in table.Organs)
            {
                if (o == null || !seen.Add(o)) continue;
                var rarity = AnalysisRules.FromTier(o.Tier);
                if (rarity == GeneRarity.Legendary && !isLeader) continue;
                genes.Add(new Gene(o, rarity));
            }
            return genes;
        }
    }
}
