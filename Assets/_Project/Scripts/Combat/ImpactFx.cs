using UnityEngine;

namespace Evolve.Combat
{
    /// <summary>The thump of something heavy: shakes the camera by <paramref name="amplitude"/> scaled by how close the camera is (<see cref="ShakeRules"/>).</summary>
    public static class ImpactFx
    {
        public static void Thump(Vector3 at, float amplitude, float seconds, float radius)
        {
            var cam = Camera.main;
            if (cam == null) return;
            float a = amplitude * ShakeRules.Falloff(Vector3.Distance(cam.transform.position, at), radius);
            if (a > 0.002f) Evolve.Player.ThirdPersonCamera.Thump(a, seconds);
        }
    }
}
