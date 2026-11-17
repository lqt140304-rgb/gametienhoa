using UnityEngine;

namespace Evolve.Organs
{
    /// <summary>Rarity tier of an organ (same letters as Dị Năng tiers). Higher = rarer and stronger.</summary>
    public enum OrganTier { C, B, A, S }

    public static class OrganTiers
    {
        public static Color Color(OrganTier t) => t switch
        {
            OrganTier.B => new Color(0.35f, 0.9f, 0.4f),
            OrganTier.A => new Color(0.35f, 0.6f, 1f),
            OrganTier.S => new Color(1f, 0.8f, 0.2f),
            _ => new Color(0.85f, 0.85f, 0.85f)
        };

        /// <summary>"[B]" coloured for IMGUI rich text.</summary>
        public static string Tag(OrganTier t) => $"<color=#{ColorUtility.ToHtmlStringRGB(Color(t))}>[{t}]</color>";
    }
}
