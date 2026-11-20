using System.Collections.Generic;

namespace Evolve.Visuals
{
    /// <summary>
    /// The special slime colours a player has unlocked with encounters (kept between sessions). Plain text for saving, tolerant of garbage.
    /// A special colour is a finish the colour sliders cannot make, such as the rainbow that cycles through every hue.
    /// </summary>
    public sealed class ColorUnlocks
    {
        public const string Rainbow = "rainbow";

        readonly HashSet<string> ids = new HashSet<string>();

        public int Count => ids.Count;
        public bool Has(string id) => id != null && ids.Contains(id);

        /// <summary>True when it was new.</summary>
        public bool Add(string id)
        {
            id = id == null ? null : id.Trim();
            if (string.IsNullOrEmpty(id) || id.Contains(",")) return false;
            return ids.Add(id);
        }

        public string Encode()
        {
            var list = new List<string>(ids);
            list.Sort(System.StringComparer.Ordinal);
            return string.Join(",", list);
        }

        /// <summary>Never throws: null, empty or odd text gives what could be read.</summary>
        public static ColorUnlocks Decode(string text)
        {
            var u = new ColorUnlocks();
            if (string.IsNullOrWhiteSpace(text)) return u;
            foreach (var part in text.Split(',')) u.Add(part);
            return u;
        }
    }
}
