using System;
using System.Collections.Generic;
using Evolve.Organs;

namespace Evolve.Swallow
{
    public enum GeneOutcome { Progress, Learned, StarUp, FoodOnly }

    /// <summary>What one sample did to one gene.</summary>
    public struct GeneResult
    {
        public OrganSO Organ;
        public GeneOutcome Outcome;
        public int Stars;
        public float Pity;
    }

    /// <summary>
    /// What Giải Mã has learned (sổ Giải Mã): pity per gene, learned genes and their stars.
    /// Kept through deaths; cleared only on a new reincarnation.
    /// </summary>
    public class AnalysisBook
    {
        class Entry { public float Pity; public bool Learned; public int SamplesAfter; }

        readonly Dictionary<OrganSO, Entry> entries = new Dictionary<OrganSO, Entry>();
        readonly List<OrganSO> known = new List<OrganSO>();

        public int AnalysesDone { get; private set; }
        public int DecoderLevel => AnalysisRules.DecoderLevel(AnalysesDone);
        public IEnumerable<OrganSO> Known => known;

        /// <summary>Raised for every gene learned or starred up (organ, stars).</summary>
        public event Action<OrganSO, int> StarsChanged;

        public bool Knows(OrganSO o) => o != null && entries.TryGetValue(o, out var e) && e.Learned;
        public int StarsOf(OrganSO o) => o != null && entries.TryGetValue(o, out var e) && e.Learned ? AnalysisRules.Stars(e.SamplesAfter) : 0;
        public float PityOf(OrganSO o) => o != null && entries.TryGetValue(o, out var e) ? e.Pity : 0f;

        /// <summary>One analysed sample: each gene rolls once, in list order.</summary>
        public List<GeneResult> Submit(IReadOnlyList<Gene> genes, float quality, Func<float> roll01)
        {
            var results = new List<GeneResult>();
            if (genes == null) return results;
            float mult = AnalysisRules.DecoderChanceMultiplier(DecoderLevel);
            foreach (var g in genes)
            {
                if (g.Organ == null) continue;
                if (!entries.TryGetValue(g.Organ, out var e)) entries[g.Organ] = e = new Entry();
                var r = new GeneResult { Organ = g.Organ };
                if (!e.Learned)
                {
                    if (AnalysisRules.Roll(g.Rarity, ref e.Pity, quality, mult, roll01()))
                    {
                        e.Learned = true;
                        known.Add(g.Organ);
                        r.Outcome = GeneOutcome.Learned;
                        r.Stars = 1;
                        StarsChanged?.Invoke(g.Organ, 1);
                    }
                    else r.Outcome = GeneOutcome.Progress;
                }
                else
                {
                    int before = AnalysisRules.Stars(e.SamplesAfter);
                    if (before >= 3) r.Outcome = GeneOutcome.FoodOnly;
                    else
                    {
                        e.SamplesAfter++;
                        int after = AnalysisRules.Stars(e.SamplesAfter);
                        r.Outcome = after > before ? GeneOutcome.StarUp : GeneOutcome.Progress;
                        if (after > before) StarsChanged?.Invoke(g.Organ, after);
                    }
                }
                r.Stars = e.Learned ? AnalysisRules.Stars(e.SamplesAfter) : 0;
                r.Pity = e.Pity;
                results.Add(r);
            }
            AnalysesDone++;
            return results;
        }

        public void Clear() { entries.Clear(); known.Clear(); AnalysesDone = 0; }
    }
}
