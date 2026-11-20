using UnityEngine;

namespace Evolve.Player
{
    /// <summary>Body size curve: grows with body mass (mass 10 = scale 1), clamped to 0.6..2.</summary>
    public static class BodySize
    {
        public static float ScaleForMass(float mass) => Mathf.Clamp(0.6f + 0.04f * mass, 0.6f, 2f);
    }
}
