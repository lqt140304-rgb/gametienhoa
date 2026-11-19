using System;
using System.Text;

namespace Evolve.Traits
{
    /// <summary>Typing and garbling of the scene's text (spec 5.3): how many letters are typed so far, and a few letters swapped for strange marks while the connection is bad.</summary>
    public static class GlitchText
    {
        const string Marks = "▓▒░#@%&§¤¦";

        /// <summary>Letters typed after <paramref name="seconds"/> at <paramref name="charsPerSecond"/>, kept in [0, length].</summary>
        public static int TypedCount(string text, float seconds, float charsPerSecond)
        {
            if (string.IsNullOrEmpty(text) || seconds <= 0f) return 0;
            return Math.Min(text.Length, (int)(seconds * charsPerSecond));
        }

        /// <summary>
        /// Swaps some letters for marks, more with a higher instability (at most about a quarter of them, so it stays readable); spaces and full stops
        /// are never touched. The same seed gives the same result; the scene changes the seed a few times a second so the garble flickers.
        /// </summary>
        public static string Corrupt(string text, float instability, int seed)
        {
            if (string.IsNullOrEmpty(text)) return "";
            if (instability <= 0f) return text;
            float chance = Math.Min(0.25f, instability * 0.12f);
            var rnd = new Random(seed);
            var sb = new StringBuilder(text.Length);
            foreach (char c in text)
            {
                bool keep = char.IsWhiteSpace(c) || c == '.' || rnd.NextDouble() >= chance;
                sb.Append(keep ? c : Marks[rnd.Next(Marks.Length)]);
            }
            return sb.ToString();
        }
    }
}
