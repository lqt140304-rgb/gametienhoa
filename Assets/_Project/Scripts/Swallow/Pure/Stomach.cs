using System.Collections.Generic;
using Evolve.Survival;

namespace Evolve.Swallow
{
    public enum StomachMode { Digest, Analyze, Keep }

    /// <summary>Something inside the stomach: plain food or a sample carrying genes.</summary>
    public class StomachItem
    {
        public string Label;
        /// <summary>Body mass of the swallowed thing (part of it becomes the slime's mass when digested).</summary>
        public float Mass;
        public float Nutrition;
        public FoodKind Food;
        public List<Gene> Genes = new List<Gene>();
        public float Quality = 1f;
        public StomachMode Mode;
        /// <summary>0..1 toward finishing the current mode.</summary>
        public float Progress;
        public bool IsSample => Genes != null && Genes.Count > 0;
    }

    /// <summary>
    /// Túi Không Đáy: a few slots; each item is digested (survive), analysed (learn) or kept (a tool to spit out).
    /// Analysis is slow on the move and four times faster in a shelter.
    /// </summary>
    public class Stomach
    {
        public const int DefaultCapacity = 6;
        public const float AnalyzeSeconds = 20f;
        public const float OutsideSpeed = 0.25f;
        public const float DigestSeconds = 8f;

        readonly List<StomachItem> items = new List<StomachItem>();

        public Stomach(int capacity = DefaultCapacity) { Capacity = capacity; }

        public int Capacity { get; set; }
        public IReadOnlyList<StomachItem> Items => items;
        public bool IsFull => items.Count >= Capacity;

        /// <summary>False when full. Samples start analysing, plain food starts digesting.</summary>
        public bool TryAdd(StomachItem item)
        {
            if (item == null || IsFull) return false;
            item.Mode = item.IsSample ? StomachMode.Analyze : StomachMode.Digest;
            item.Progress = 0f;
            items.Add(item);
            return true;
        }

        /// <summary>Change what an item is used for; progress restarts. Plain food cannot be analysed.</summary>
        public void SetMode(int index, StomachMode mode)
        {
            if (index < 0 || index >= items.Count) return;
            var it = items[index];
            if (mode == StomachMode.Analyze && !it.IsSample) mode = StomachMode.Digest;
            if (it.Mode == mode) return;
            it.Mode = mode;
            it.Progress = 0f;
        }

        /// <summary>Take an item out (to spit it); null for a bad index.</summary>
        public StomachItem Spit(int index)
        {
            if (index < 0 || index >= items.Count) return null;
            var it = items[index];
            items.RemoveAt(index);
            return it;
        }

        /// <summary>
        /// Advance every item. Finished analyses are reported in <paramref name="analyzed"/> and switch to digesting;
        /// finished digestion leaves the stomach and is reported in <paramref name="digested"/>.
        /// </summary>
        public void Tick(float dt, bool inShelter, List<StomachItem> analyzed, List<StomachItem> digested)
        {
            if (dt <= 0f) return;
            for (int i = items.Count - 1; i >= 0; i--)
            {
                var it = items[i];
                switch (it.Mode)
                {
                    case StomachMode.Analyze:
                        it.Progress += dt / AnalyzeSeconds * (inShelter ? 1f : OutsideSpeed);
                        if (it.Progress >= 0.9999f)
                        {
                            analyzed?.Add(it);
                            it.Mode = StomachMode.Digest;
                            it.Progress = 0f;
                        }
                        break;
                    case StomachMode.Digest:
                        it.Progress += dt / DigestSeconds;
                        if (it.Progress >= 0.9999f)
                        {
                            items.RemoveAt(i);
                            digested?.Add(it);
                        }
                        break;
                }
            }
        }

        public void Clear() => items.Clear();
    }
}
