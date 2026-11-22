using System;
using Evolve.Creatures;

namespace Evolve.Combat
{
    /// <summary>
    /// A boss between two moves does not stand still: it walks round its prey at about <see cref="Ring"/> metres, sizing it up (the golem and the bird do this for <see cref="IdleSeconds"/> after every move),
    /// and now and then reverses direction. <see cref="Steer"/> gives the direction to walk.
    /// </summary>
    public static class CircleRules
    {
        public const float Ring = 6.5f, FlipMin = 2f, FlipMax = 3.5f, Speed = 2.2f;

        /// <summary>The walking speed while it circles its prey: the bird takes it slowly, sizing the player up (1.1 m/s); everything else uses <see cref="Speed"/>.</summary>
        public static float SpeedOf(string name) => SpeciesNames.Key(name) == "chimhangme" ? 1.1f : Speed;
        /// <summary>A factor on the approach speed within 11 m of the prey (beyond that the bird flies in at its full speed): the bird closes in at 0.6 of the pace the others use.</summary>
        public static float ApproachScale(string name, float distance) => SpeciesNames.Key(name) == "chimhangme" && distance <= 11f ? 0.6f : 1f;

        public static float IdleSeconds(string name) { string k = SpeciesNames.Key(name); return k == "golemthachanh" ? 2.5f : k == "chimhangme" ? 2f : k == "bocapthachanh" ? 2f : 0f; }

        /// <summary>The unit direction to walk, for the prey at (<paramref name="toX"/>, <paramref name="toZ"/>) from here: along the circle round it (to the left for sign 1, to the right for -1), drawn in when too far from the ring and pushed out when too close.</summary>
        public static (float x, float z) Steer(float toX, float toZ, int sign, float ring)
        {
            float d = (float)Math.Sqrt(toX * toX + toZ * toZ);
            if (d < 1e-4f) return (1f, 0f);
            float ux = toX / d, uz = toZ / d;
            float radial = Math.Max(-1f, Math.Min(1f, (d - ring) / ring)) * 1.5f;
            float x = -uz * Math.Sign(sign) + ux * radial, z = ux * Math.Sign(sign) + uz * radial;
            float n = (float)Math.Sqrt(x * x + z * z);
            return (x / n, z / n);
        }
    }
}
