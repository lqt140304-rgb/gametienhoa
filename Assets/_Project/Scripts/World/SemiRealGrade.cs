using UnityEngine;

namespace Evolve.World
{
    /// <summary>The semi-real look (spec 2026-11-18-scanned-flora, section 8): scanned colours a little desaturated and nudged toward the zone's tone,
    /// with the brightness kept; roughness pulled toward matte so photo scans sit next to the painted fantasy pieces.</summary>
    public static class SemiRealGrade
    {
        public const float Desaturate = 0.25f;
        public static readonly Color CaveTint = new Color(0.82f, 0.92f, 1.0f);   // the cool blue-green of the cave (reference 2)

        static float Lum(Color c) => 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;

        public static Color Grade(Color albedo, Color zoneTint, float tintAmount = 0.12f)
        {
            float l = Lum(albedo);
            var grey = new Color(l, l, l, albedo.a);
            var c = Color.Lerp(albedo, grey, Desaturate);
            float lt = Mathf.Max(0.0001f, Lum(zoneTint));
            var tinted = new Color(zoneTint.r * l / lt, zoneTint.g * l / lt, zoneTint.b * l / lt, albedo.a);   // same brightness, the zone's hue
            c = Color.Lerp(c, tinted, tintAmount);
            c.a = albedo.a;
            return c;
        }

        public static float Roughness(float scanned) => Mathf.Lerp(scanned, 0.8f, 0.35f);
    }
}
