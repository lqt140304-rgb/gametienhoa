using System;

namespace Evolve.Survival
{
    public static class HudCatalog
    {
        static string Normalize(string value)
        {
            var result = new System.Text.StringBuilder();
            foreach (char c in (value ?? "").Normalize(System.Text.NormalizationForm.FormD))
                if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
                    result.Append(c == 'đ' || c == 'Đ' ? 'd' : char.ToLowerInvariant(c));
            return result.ToString();
        }
        public static bool Matches(string name, string organ, string query)
            => Normalize((name ?? "") + " " + (organ ?? "")).Contains(Normalize(query).Trim());
        public static int PageCount(int count, int size) => Math.Max(1, (Math.Max(0, count) + Math.Max(1, size) - 1) / Math.Max(1, size));
        public static int ClampPage(int page, int count, int size) => Math.Max(0, Math.Min(page, PageCount(count, size) - 1));
        public static float SpikeTotal(float damage, int count, int level)
            => damage * (level >= 3 ? 1.5f : 1f) * (Math.Max(1, count) + (level >= 3 ? 2 : 0));
        public static float MenuScale(float width,float height,float hudScale)
            => Math.Max(.1f,Math.Min(1.3f,Math.Min((width-32)/1240f,(height-48-196*hudScale)/650f)));
    }
    public enum HudMenu { None, Skills, Organs, Settings, Help }

    /// <summary>Viewport and menu rules shared by the HUD and its checks.</summary>
    public static class HudLayout
    {
        public static float ClampCenter(float desired, float size, float viewport, float margin = 12f)
        {
            if (size + margin * 2 >= viewport) return viewport / 2;
            float half = size / 2;
            return Math.Max(half + margin, Math.Min(viewport - half - margin, desired));
        }

        public static HudMenu Toggle(HudMenu current, HudMenu requested)
            => current == requested ? HudMenu.None : requested;
    }
}
