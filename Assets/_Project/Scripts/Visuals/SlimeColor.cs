using System;
using UnityEngine;

namespace Evolve.Visuals
{
    /// <summary>The player's chosen slime colour (kept between sessions). Menu: F3 (SlimeColorMenu).</summary>
    public static class SlimeColor
    {
        public static readonly Color Default = new Color(0.68f, 0.88f, 0.68f);
        /// <summary>The gel stays a little translucent whatever the colour.</summary>
        public const float Alpha = 0.85f;
        const string Key = "evolve.slimeColor";

        static Color current;
        static bool loaded;

        public static event Action Changed;

        public static Color Current
        {
            get { Load(); return current; }
        }

        public static void Set(Color color)
        {
            Load();
            color.a = 1f;
            current = color;
            try { PlayerPrefs.SetString(Key, ColorUtility.ToHtmlStringRGB(color)); } catch (Exception) { /* not saved, still applied */ }
            Changed?.Invoke();
        }

        static void Load()
        {
            if (loaded) return;
            loaded = true;
            current = Default;
            try
            {
                string saved = PlayerPrefs.GetString(Key, "");
                if (saved.Length > 0 && ColorUtility.TryParseHtmlString("#" + saved, out Color c)) current = c;
            }
            catch (Exception) { /* keep the default */ }
        }
    }
}
