using System;
using UnityEngine;

namespace Evolve.Visuals
{
    /// <summary>The parts of the slime that can have their own colour.</summary>
    public enum SlimePart { Body = 0, Eyes = 1, Legs = 2, Back = 3, Skin = 4, Core = 5 }

    /// <summary>
    /// The player's chosen slime colours (kept between sessions). Menu: F3 (SlimeColorMenu).
    /// While "linked" (default) the eye stalks, legs and back organs follow the body colour; the skin film and
    /// the core always have their own colour.
    /// </summary>
    public static class SlimeColor
    {
        public const int PartCount = 6;
        public static readonly Color Default = new Color(0.68f, 0.88f, 0.68f);
        /// <summary>The gel stays a little translucent whatever the colour.</summary>
        public const float Alpha = 0.85f;
        public const float SkinAlpha = 0.22f;

        static readonly Color[] Defaults =
        {
            new Color(0.68f, 0.88f, 0.68f), new Color(0.68f, 0.88f, 0.68f), new Color(0.68f, 0.88f, 0.68f),
            new Color(0.68f, 0.88f, 0.68f), new Color(0.90f, 0.75f, 0.40f), new Color(1.00f, 0.62f, 0.15f)
        };
        const string UnlockKey = "evolve.slimeUnlocked", RainbowKey = "evolve.slimeRainbow";
        const string ColorsKey = "evolve.slimeColors";
        const string LinkedKey = "evolve.slimeLinked";

        static Color[] colors;
        static bool linked = true;
        static bool loaded;
        static ColorUnlocks unlocks = new ColorUnlocks();
        static bool rainbowOn;
        /// <summary>Hue turns a full circle in this many seconds (the unlocked Cầu Vồng finish).</summary>
        public const float RainbowSeconds = 8f;

        public static event Action Changed;

        /// <summary>A special colour won in a cave (kept between sessions and rounds). Returns true when it was new.</summary>
        public static bool Unlock(string id)
        {
            Load();
            if (!unlocks.Add(id)) return false;
            SaveUnlocks();
            Changed?.Invoke();
            return true;
        }

        public static bool IsUnlocked(string id) { Load(); return unlocks.Has(id); }

        /// <summary>The Cầu Vồng finish is switched on (and unlocked): the body colour cycles through every hue.</summary>
        public static bool RainbowOn { get { Load(); return rainbowOn && unlocks.Has(ColorUnlocks.Rainbow); } }

        public static void SetRainbow(bool on)
        {
            Load();
            if (rainbowOn == on) return;
            rainbowOn = on;
            SaveUnlocks();
            Changed?.Invoke();
        }

        static Color RainbowColor() => Color.HSVToRGB(Mathf.Repeat(Time.time / RainbowSeconds, 1f), 0.5f, 0.95f);

        public static bool Linked
        {
            get { Load(); return linked; }
        }

        /// <summary>True for the parts that follow the body colour while linked.</summary>
        public static bool Follows(SlimePart part) => part == SlimePart.Eyes || part == SlimePart.Legs || part == SlimePart.Back;

        /// <summary>The body colour.</summary>
        public static Color Current => Get(SlimePart.Body);

        /// <summary>The colour a part really shows (linked parts show the body colour).</summary>
        public static Color Get(SlimePart part)
        {
            Load();
            if (RainbowOn && (part == SlimePart.Body || (linked && Follows(part)))) return RainbowColor();
            return linked && Follows(part) ? colors[(int)SlimePart.Body] : colors[(int)part];
        }

        /// <summary>The stored colour of a part, ignoring linking (for the menu).</summary>
        public static Color GetRaw(SlimePart part) { Load(); return colors[(int)part]; }

        public static Color[] Resolved()
        {
            var all = new Color[PartCount];
            for (int i = 0; i < PartCount; i++) all[i] = Get((SlimePart)i);
            return all;
        }

        public static void Set(SlimePart part, Color color)
        {
            Load();
            color.a = 1f;
            colors[(int)part] = color;
            Save();
        }

        public static void SetLinked(bool value)
        {
            Load();
            if (linked == value) return;
            linked = value;
            if (!value) // unlinking keeps what you see: the parts keep the body colour they had
                for (int i = 0; i < PartCount; i++) if (Follows((SlimePart)i)) colors[i] = colors[(int)SlimePart.Body];
            Save();
        }

        /// <summary>Back to the starting colours.</summary>
        public static void Reset()
        {
            Load();
            for (int i = 0; i < PartCount; i++) colors[i] = Defaults[i];
            linked = true;
            rainbowOn = false;   // the unlock stays, the finish goes
            SaveUnlocks();
            Save();
        }

        static void SaveUnlocks()
        {
            try
            {
                PlayerPrefs.SetString(UnlockKey, unlocks.Encode());
                PlayerPrefs.SetInt(RainbowKey, rainbowOn ? 1 : 0);
            }
            catch (Exception) { /* not saved, still applied */ }
        }

        static void Save()
        {
            try
            {
                var parts = new string[PartCount];
                for (int i = 0; i < PartCount; i++) parts[i] = ColorUtility.ToHtmlStringRGB(colors[i]);
                PlayerPrefs.SetString(ColorsKey, string.Join(",", parts));
                PlayerPrefs.SetInt(LinkedKey, linked ? 1 : 0);
            }
            catch (Exception) { /* not saved, still applied */ }
            Changed?.Invoke();
        }

        static void Load()
        {
            if (loaded) return;
            loaded = true;
            colors = (Color[])Defaults.Clone();
            try
            {
                string saved = PlayerPrefs.GetString(ColorsKey, "");
                var parts = saved.Split(',');
                if (parts.Length == PartCount)
                    for (int i = 0; i < PartCount; i++)
                        if (ColorUtility.TryParseHtmlString("#" + parts[i], out Color c)) colors[i] = c;
                linked = PlayerPrefs.GetInt(LinkedKey, 1) != 0;
                unlocks = ColorUnlocks.Decode(PlayerPrefs.GetString(UnlockKey, ""));
                rainbowOn = PlayerPrefs.GetInt(RainbowKey, 0) != 0;
            }
            catch (Exception) { /* keep the defaults */ }
        }
    }
}
