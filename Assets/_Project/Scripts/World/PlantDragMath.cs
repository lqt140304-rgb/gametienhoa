using UnityEngine;

namespace Evolve.World
{
    /// <summary>How much dense soft plants (ferns, reeds, bushes) slow a slime pushing through them: pure maths (tested).</summary>
    public static class PlantDragMath
    {
        public const float MinSpeed = 0.55f;

        /// <summary>Speed factor for a plant of the given stiffness (0 = none, 1 = a thick bush): 1 at 0, down to <see cref="MinSpeed"/>.</summary>
        public static float SpeedFor(float stiffness) => Mathf.Lerp(1f, MinSpeed, Mathf.Clamp01(stiffness));
    }
}
