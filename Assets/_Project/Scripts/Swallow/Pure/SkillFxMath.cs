using UnityEngine;

namespace Evolve.Swallow
{
    /// <summary>Pure numbers behind the skill visuals (colours per family, timing curves), so they can be tested without Unity.</summary>
    public static class SkillFxMath
    {
        /// <summary>Colour of a family's effects (soft, a little pastel: they glow, they do not blind).</summary>
        public static Color FamilyColor(SkillFamily f) => f switch
        {
            SkillFamily.Agility => new Color(0.75f, 0.95f, 1.00f),
            SkillFamily.Stone => new Color(0.68f, 0.55f, 1.00f),
            SkillFamily.Poison => new Color(0.55f, 1.00f, 0.45f),
            SkillFamily.Shadow => new Color(0.50f, 0.62f, 1.00f),
            SkillFamily.Vitality => new Color(1.00f, 0.55f, 0.72f),
            SkillFamily.Heat => new Color(1.00f, 0.55f, 0.20f),
            _ => new Color(0.85f, 0.95f, 1.00f),
        };

        /// <summary>Phát Sáng is warm lamplight, whatever its family.</summary>
        public static readonly Color GlowColor = new Color(1.00f, 0.90f, 0.55f);

        /// <summary>0 at the start and the end, 1 in the middle: rises over fadeIn, falls over the last fadeOut seconds.</summary>
        public static float Envelope(float t, float total, float fadeIn, float fadeOut)
        {
            if (total <= 0f || t <= 0f || t >= total) return 0f;
            float a = fadeIn > 0f ? Mathf.Clamp01(t / fadeIn) : 1f;
            float b = fadeOut > 0f ? Mathf.Clamp01((total - t) / fadeOut) : 1f;
            return Mathf.Min(a, b);
        }

        /// <summary>Grows from 0 to 1 and overshoots a little on the way (spikes pushing out of the ground).</summary>
        public static float EaseOutBack(float k)
        {
            k = Mathf.Clamp01(k);
            const float c1 = 1.70158f, c3 = c1 + 1f;
            float m = k - 1f;
            return 1f + c3 * m * m * m + c1 * m * m;
        }

        /// <summary>Fast at the start, slow at the end (a shockwave ring).</summary>
        public static float EaseOutCubic(float k)
        {
            float m = 1f - Mathf.Clamp01(k);
            return 1f - m * m * m;
        }

        /// <summary>
        /// Thickness of a gel limb at fraction f (0 at the body, 1 at the tip): tapers from baseR to tipR, flares out where it leaves the body
        /// (flare), swells into a knob near the tip (bulb), and closes to a round point over the last cap of its length.
        /// </summary>
        public static float PseudopodRadius(float f, float baseR, float tipR, float flare, float bulb, float cap)
        {
            f = Mathf.Clamp01(f);
            float root = Mathf.Clamp01(1f - f / 0.18f);
            float knob = Mathf.Clamp01((f - 0.78f) / 0.1f);
            float r = (baseR + (tipR - baseR) * f) * (1f + flare * root * root) * (1f + bulb * knob);
            if (cap > 0f && f > 1f - cap)
            {
                float u = (f - (1f - cap)) / cap;
                r *= (float)System.Math.Sqrt(System.Math.Max(0.0, 1.0 - u * u));
            }
            return r;
        }

        /// <summary>How far a limb has reached (0..1): shoots out over outSeconds, stays, draws back over the last backSeconds. Fast out, soft landing.</summary>
        public static float Reach(float t, float total, float outSeconds, float backSeconds) =>
            EaseOutCubic(Envelope(t, total, outSeconds, backSeconds));
    }
}
