using UnityEngine;

namespace Evolve.World
{
    /// <summary>
    /// Where each kind of cave vegetation (the plants that cannot be eaten) grows in the Slime Cavern: pure habitat rules from the shore, the wet, the shade of the
    /// wall's foot, the ground's height and patchy noise, so each kind has its own place (reeds in the shallows, ferns and moss in the damp, bushes on the dry rises,
    /// mushrooms in the shade, roots at the wall's foot, vines hanging round the hole of light). Density is 0..1; same input, same answer.
    /// </summary>
    public static class CavernFloraMath
    {
        public enum Kind { Fern, Reed, Moss, Bush, Mushrooms, RootArc, Vine }

        static float Smooth01(float t) { t = Mathf.Clamp01(t); return t * t * (3f - 2f * t); }
        static float Band(float v, float lo, float hi, float soft) => Smooth01((v - lo) / soft) * (1f - Smooth01((v - hi) / soft));
        static float Patch(float x, float z, float size, float seed, float threshold = 0.5f, float width = 0.25f) =>
            Smooth01((ZoneLayout.Noise(x / size + seed, z / size + seed * 1.7f) - threshold) / width);

        /// <summary>0..1 chance of a plant of this kind at the ground point (x, z). Vines are for the roof: see <see cref="VineDensity"/>.</summary>
        public static float Density(Kind kind, float x, float z)
        {
            if (kind == Kind.Vine) return 0f;
            float depth = CavernLayout.DepthAt(x, z);
            float d = CavernLayout.ShoreOffset(x, z);            // metres beyond the shore (negative in the water)
            float r = CavernLayout.RadiusFromCenter(x, z);
            float h = CavernLayout.GroundHeight(x, z);
            float wet = Mathf.Exp(-Mathf.Max(0f, d) / 7f);
            float shade = Smooth01((r - (CavernLayout.FloorRadius - 10f)) / 8f);
            bool dry = depth <= 0f;
            switch (kind)
            {
                case Kind.Reed:      // standing in the shallows and on the wet rim
                    if (depth > 0.35f || d > 3.5f) return 0f;
                    return Band(d, -1.0f, 3.0f, 0.8f) * (0.35f + 0.65f * Patch(x, z, 6f, 31f, 0.42f));
                case Kind.Fern:      // damp ground a little back from the water, thicker in the shade
                    if (!dry || h > 2.4f) return 0f;
                    return Band(d, 2f, 22f, 3f) * (0.3f + 0.4f * shade + 0.3f * wet) * Patch(x, z, 9f, 11f, 0.45f);
                case Kind.Moss:      // cushions on the wet and shaded ground
                    if (!dry || h > 3f) return 0f;
                    return Band(d, 0.8f, 18f, 2f) * Mathf.Clamp01(0.35f + wet + 0.4f * shade) * Patch(x, z, 7f, 53f, 0.48f);
                case Kind.Bush:      // leafy bushes on the drier rises
                    if (!dry || h > 2.6f) return 0f;
                    return Band(d, 7f, 26f, 3f) * Patch(x, z, 11f, 77f, 0.52f) * (0.5f + 0.5f * (1f - wet));
                case Kind.Mushrooms: // little clusters in the shade and on the wet rim
                    if (!dry || h > 4f) return 0f;
                    return Mathf.Max(Band(d, 0.5f, 6f, 1.5f) * 0.6f, shade) * Mathf.Clamp01((ZoneLayout.Noise(x / 5f + 90f, z / 5f + 14f) - 0.56f) * 4f);
                case Kind.RootArc:   // roots breaking out of the ground at the wall's foot
                    if (!dry || h > 12f) return 0f;
                    return Band(r, CavernLayout.FloorRadius - 4f, CavernLayout.FloorRadius + 7f, 2f) * Patch(x, z, 8f, 23f, 0.5f);
            }
            return 0f;
        }

        /// <summary>Vines hang from the roof in a ring round the hole of light, where there is light to climb to: thickest at the hole's rim, gone beyond about 9 m from it.</summary>
        public static float VineDensity(float x, float z)
        {
            var hole = CavernLayout.HoleCenter();
            float dist = Vector2.Distance(new Vector2(x, z), hole);
            if (dist < CavernLayout.HoleRadius + 1.2f) return 0f;      // not across the hole itself
            float falloff = 1f - Smooth01((dist - (CavernLayout.HoleRadius + 1.2f)) / 9f);
            return falloff * (0.35f + 0.65f * Patch(x, z, 5f, 61f, 0.4f));
        }
    }
}
