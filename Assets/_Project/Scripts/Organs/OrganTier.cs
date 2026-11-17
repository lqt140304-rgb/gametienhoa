using UnityEngine;

namespace Evolve.Organs
{
    /// <summary>Rarity tier of an organ (same letters as Dị Năng tiers). Higher = rarer and stronger.</summary>
    public enum OrganTier { D = -1, C = 0, B = 1, A = 2, S = 3 }

    public static class OrganTiers
    {
        public static Color Color(OrganTier t) => t switch
        {
            OrganTier.D => new Color(0.45f, 0.45f, 0.45f),
            OrganTier.B => new Color(0.35f, 0.9f, 0.4f),
            OrganTier.A => new Color(0.35f, 0.6f, 1f),
            OrganTier.S => new Color(1f, 0.8f, 0.2f),
            _ => new Color(0.85f, 0.85f, 0.85f)
        };

        /// <summary>Evolution EXP gained by absorbing an organ of this tier.</summary>
        public static float AbsorbExp(OrganTier t) => t switch
        {
            OrganTier.D => 8f,
            OrganTier.B => 30f,
            OrganTier.A => 60f,
            OrganTier.S => 120f,
            _ => 15f
        };

        /// <summary>"[B]" coloured for IMGUI rich text.</summary>
        public static string Tag(OrganTier t) => $"<color=#{ColorUtility.ToHtmlStringRGB(Color(t))}>[{t}]</color>";
    }
}
