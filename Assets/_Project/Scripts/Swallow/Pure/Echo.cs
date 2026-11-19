using System.Collections.Generic;
using UnityEngine;

namespace Evolve.Swallow
{
    /// <summary>
    /// Tiếng Vọng: the voice of the world announcing what happens to the slime.
    /// A small queue the HUD reads one line at a time (the full birth scene is step 2).
    /// </summary>
    public static class Echo
    {
        public const int MaxQueued = 8;

        static readonly Queue<string> lines = new Queue<string>();

        public static void Say(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            lines.Enqueue(text);
            while (lines.Count > MaxQueued) lines.Dequeue();
        }

        public static bool TryNext(out string text)
        {
            if (lines.Count == 0) { text = null; return false; }
            text = lines.Dequeue();
            return true;
        }

        public static void Clear() => lines.Clear();

        /// <summary>What Tiếng Vọng says about one gene after an analysis; null when nothing is worth saying.</summary>
        public static string LearnedLine(GeneResult r, bool showName)
        {
            string name = r.Organ == null ? "???" : !showName ? "???" : string.IsNullOrEmpty(r.Organ.DisplayName) ? r.Organ.name : r.Organ.DisplayName;
            switch (r.Outcome)
            {
                case GeneOutcome.Learned: return $"«Đã phân tích xong. Học được cấu trúc: [{name}]»";
                case GeneOutcome.StarUp: return $"«[{name}] thành thạo hơn: ★{r.Stars}»";
                case GeneOutcome.Progress:
                    if (r.Stars > 0) return null;   // a learned gene counting toward its next star: silent
                    return $"«Phân tích [{name}] hoàn tất {Mathf.RoundToInt(r.Pity * 100f)}%…»";
                default: return null;
            }
        }
    }
}
