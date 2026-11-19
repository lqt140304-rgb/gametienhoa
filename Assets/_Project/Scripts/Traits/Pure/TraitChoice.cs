using System.Collections.Generic;

namespace Evolve.Traits
{
    /// <summary>Checks a core and flaw pick (what a co-op client reports to the host) against what the core really offers.</summary>
    public static class TraitChoice
    {
        /// <summary>True when <paramref name="flawIds"/> is exactly one of the core's flaw options (order does not matter).</summary>
        public static bool IsValid(IReadOnlyList<string[]> options, string[] flawIds)
        {
            if (options == null || flawIds == null || flawIds.Length == 0) return false;
            foreach (var option in options)
            {
                if (option == null || option.Length != flawIds.Length) continue;
                var remaining = new List<string>(option);
                bool all = true;
                foreach (var id in flawIds)
                    if (!remaining.Remove(id)) { all = false; break; }
                if (all) return true;
            }
            return false;
        }

        /// <summary>True when the core is in the pool and the flaws are exactly one of its options. The host checks nothing else (not the rarity or the seed).</summary>
        public static bool IsKnownPick(IReadOnlyList<FateCard> pool, string coreId, string[] flawIds)
        {
            if (pool == null || string.IsNullOrEmpty(coreId)) return false;
            foreach (var card in pool)
                if (card.Id == coreId) return IsValid(card.Options, flawIds);
            return false;
        }
    }
}
