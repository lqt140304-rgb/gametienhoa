using UnityEngine;

namespace Evolve.World
{
    /// <summary>Pure data and maths for the per-zone air (fog and particles). Numbers are proposals: tune them here.</summary>
    public static class ZoneAtmosphereMath
    {
        /// <summary>Never more than this many particles at once (the owner asked for few, to avoid lag).</summary>
        public const int MaxParticles = 150;
        public const float BlindFogEnd = 80f;
        /// <summary>How much of each fog colour's tint is washed out toward grey (0 = full colour, 1 = grey). Raise it for subtler zones.</summary>
        public const float FogDesaturation = 0.45f;

        static AtmosphereProfile P(Color fog, float end, ParticleKind kind, Color particle, float density, float size, float speed, Color ambient)
            => new AtmosphereProfile { FogColor = fog, FogEnd = end, Particles = kind, ParticleColor = particle, Density = density, Size = size, Speed = speed, Ambient = ambient };

        /// <summary>Zones 01..06 in <see cref="ZoneLayout.Zones"/> order.</summary>
        public static readonly AtmosphereProfile[] Zones =
        {
            P(new Color(0.10f, 0.30f, 0.32f), 160f, ParticleKind.Droplets, new Color(0.60f, 1.00f, 0.95f, 0.6f), 3f, 0.12f, 0.15f, new Color(0.14f, 0.16f, 0.15f)), // 01 clear, safe
            P(new Color(0.30f, 0.20f, 0.08f),  70f, ParticleKind.Dust,     new Color(1.00f, 0.85f, 0.50f, 0.5f), 3f, 0.08f, 0.20f, new Color(0.08f, 0.09f, 0.08f)), // 02 old, warm
            P(new Color(0.22f, 0.19f, 0.15f),  75f, ParticleKind.Leaves,   new Color(0.55f, 0.40f, 0.22f, 0.45f), 3f, 0.12f, 0.40f, new Color(0.10f, 0.10f, 0.08f)), // 03 rotting, damp (greyer, clearer, dimmer motes: the old brown swallowed the wood)
            P(new Color(0.25f, 0.12f, 0.30f),  90f, ParticleKind.Spores,   new Color(0.85f, 0.60f, 1.00f, 0.7f), 5f, 0.12f, 0.30f, new Color(0.12f, 0.10f, 0.14f)), // 04 dreamlike
            P(new Color(0.22f, 0.30f, 0.34f), 120f, ParticleKind.Mist,     new Color(0.85f, 0.95f, 1.00f, 0.4f), 3f, 0.25f, 0.20f, new Color(0.15f, 0.17f, 0.17f)), // 05 cool, wet
            P(new Color(0.22f, 0.07f, 0.07f),  40f, ParticleKind.Ash,      new Color(0.60f, 0.25f, 0.20f, 0.7f), 2f, 0.15f, 0.30f, new Color(0.20f, 0.19f, 0.16f)), // 06 tense
        };

        /// <summary>Between the zones: dark and quiet, no particles.</summary>
        public static readonly AtmosphereProfile Wild =
            P(new Color(0.08f, 0.10f, 0.10f), 100f, ParticleKind.Dust, new Color(0.7f, 0.7f, 0.7f, 0.3f), 0f, 0.08f, 0.2f, new Color(0.07f, 0.08f, 0.08f));

        /// <summary>Mix the six zones by weight (negative = 0); what the zones do not cover is <see cref="Wild"/>. Weights are normalised.</summary>
        public static AtmosphereProfile Blend(float[] weights)
        {
            int n = Zones.Length;
            var w = new float[n + 1];
            float sum = 0f;
            for (int i = 0; i < n; i++)
            {
                w[i] = weights != null && i < weights.Length ? Mathf.Max(0f, weights[i]) : 0f;
                sum += w[i];
            }
            w[n] = Mathf.Max(0f, 1f - sum);
            float total = sum + w[n];
            int best = 0;
            var result = new AtmosphereProfile();
            for (int i = 0; i <= n; i++)
            {
                float k = w[i] / total;
                var p = i < n ? Zones[i] : Wild;
                result.FogColor += p.FogColor * k;
                result.ParticleColor += p.ParticleColor * k;
                result.Ambient += p.Ambient * k;
                result.FogEnd += p.FogEnd * k;
                result.Density += p.Density * k;
                result.Size += p.Size * k;
                result.Speed += p.Speed * k;
                if (w[i] > w[best]) best = i;
            }
            result.Particles = (best < n ? Zones[best] : Wild).Particles;
            return result;
        }

        /// <summary>Fog colour: black without eyes (the world is dark), the zone's colour with them.</summary>
        public static Color FogColorFor(AtmosphereProfile p, float sight) => Color.Lerp(Color.black, Desaturate(p.FogColor), Mathf.Clamp01(sight));

        /// <summary>Ambient light: black without eyes, the zone's dusk ambient with them (smoothly in between).</summary>
        public static Color AmbientFor(AtmosphereProfile p, float sight) => Color.Lerp(Color.black, p.Ambient, Mathf.Clamp01(sight));

        /// <summary>The colour with <see cref="FogDesaturation"/> of its tint washed out toward its own grey (same brightness).</summary>
        public static Color Desaturate(Color c)
        {
            float grey = c.r * 0.30f + c.g * 0.59f + c.b * 0.11f;
            return Color.Lerp(c, new Color(grey, grey, grey, c.a), FogDesaturation);
        }

        /// <summary>Fog end distance: 80 m blind, the zone's own with eyes, stretched by levelled eyes (sight above 1).</summary>
        public static float FogEndFor(AtmosphereProfile p, float sight)
        {
            float c = Mathf.Clamp01(sight);
            return Mathf.Lerp(BlindFogEnd, p.FogEnd, c * c) * (1f + Mathf.Max(0f, sight - 1f));
        }

        /// <summary>How many particles should be alive at once: none without eyes, never above the cap (Density 5 = 50 particles).</summary>
        public static float ParticleBudget(float sight, float density) => Mathf.Min(MaxParticles, density * 10f * Mathf.Clamp01(sight));
    }
}
