using System;

namespace Evolve.Survival
{
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
