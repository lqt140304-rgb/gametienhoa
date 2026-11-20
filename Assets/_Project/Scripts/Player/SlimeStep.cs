using System;

namespace Evolve.Player
{
    /// <summary>
    /// Pure rules for the slime crawling over things in its way: a rock or ledge up to about 40% of its body width it oozes
    /// up and over (bigger slimes take bigger rocks); anything taller is a wall it cannot climb.
    /// </summary>
    public static class SlimeStep
    {
        public static float MaxStep(float bodyWidth) => Math.Clamp(bodyWidth * 0.4f, 0.15f, 1.5f);

        /// <summary>Too low to matter (the ground hug handles it) or too tall (a wall) means no climbing.</summary>
        public static bool CanClimb(float height, float maxStep) => height > 0.03f && height <= maxStep;

        /// <summary>Upward speed while oozing over: slow and steady, a little quicker for a taller lip.</summary>
        public static float ClimbSpeed(float height) => Math.Clamp(height * 4f, 1f, 2.5f);

        /// <summary>Forward speed is cut while climbing so it reads as crawling up, not jumping.</summary>
        public const float ClimbForwardScale = 0.6f;
    }
}
