using UnityEngine;

namespace Evolve.Player
{
    /// <summary>
    /// Swimming rules (spec 2026-11-17-lake-map-300m-design): water at least <see cref="SwimDepth"/> deep is swum, not
    /// waded. A swimming slime has no gravity: Space rises, Ctrl dives, and with neither it drifts slowly up to the surface.
    /// Pure maths so it can be tested without a scene.
    /// </summary>
    public static class SwimLogic
    {
        /// <summary>Water this deep (m) or deeper is swum; shallower water is waded.</summary>
        public const float SwimDepth = 0.5f;
        /// <summary>Speed multiplier while swimming or wading.</summary>
        public const float SpeedScale = 0.65f;
        /// <summary>Up/down speed (m/s) with Space or Ctrl held.</summary>
        public const float VerticalSpeed = 1.2f;
        /// <summary>Upward drift (m/s) with no vertical input.</summary>
        public const float Buoyancy = 0.25f;

        public static bool IsSwimming(float waterDepth) => waterDepth >= SwimDepth;

        /// <summary>
        /// Target velocity while swimming: <paramref name="moveDir"/> (horizontal, length 0..1) times <paramref name="speed"/>,
        /// plus <paramref name="vertical"/> (-1 dive .. +1 rise) times <see cref="VerticalSpeed"/>, or the buoyancy drift when 0.
        /// </summary>
        public static Vector3 Velocity(Vector3 moveDir, float vertical, float speed)
        {
            float up = Mathf.Abs(vertical) > 0.01f ? Mathf.Clamp(vertical, -1f, 1f) * VerticalSpeed : Buoyancy;
            return new Vector3(moveDir.x * speed, up, moveDir.z * speed);
        }
    }
}
