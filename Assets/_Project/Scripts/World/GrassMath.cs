using System.Collections.Generic;
using UnityEngine;

namespace Evolve.World
{
    /// <summary>
    /// Pure maths of the grass carpet: where grass grows (round the water, off the trails, patchy with clearings) and which walkers
    /// push it aside (the nearest few to the camera; the shader bends the blades away from each one).
    /// </summary>
    public static class GrassMath
    {
        public const int MaxPushers = 8;
        public const float PusherReach = 45f;   // walkers farther than this from the camera are not worth bending grass for
        public const float Rim = 205f;          // no grass up the rim cliffs

        /// <summary>0..1: how thick the grass is at (x, z). Full within about 30 m of the water, gone by 80 m; none in water, in the hall or on the trail's middle.</summary>
        public static float Density(float x, float z)
        {
            if (Mathf.Max(Mathf.Abs(x), Mathf.Abs(z)) > Rim) return 0f;
            if (ZoneLayout.CleanDepthAt(x, z) > 0f) return 0f;
            if (ZoneLayout.InsideCave(x, z, 1.5f)) return 0f;
            float trail = Mathf.Clamp01((ZoneLayout.DistanceToPath(x, z) - 1.2f) / 2f);
            float near = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((ZoneLayout.DistanceToWater(x, z) - 30f) / 50f));
            float patch = 0.6f + 0.4f * ZoneLayout.Noise(x / 11f + 3f, z / 11f + 8f);
            float clearing = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((ZoneLayout.Noise(x / 23f + 40f, z / 23f + 7f) - 0.22f) / 0.2f));
            return trail * near * patch * clearing;
        }

        /// <summary>The up to <see cref="MaxPushers"/> candidates (x, y, z = position, w = push radius) nearest to the camera, within <see cref="PusherReach"/>; nearest first.</summary>
        public static void SelectPushers(IList<Vector4> candidates, Vector3 camera, List<Vector4> result)
        {
            result.Clear();
            var scored = new List<(float d2, Vector4 v)>();
            float reach2 = PusherReach * PusherReach;
            foreach (var c in candidates)
            {
                float dx = c.x - camera.x, dy = c.y - camera.y, dz = c.z - camera.z;
                float d2 = dx * dx + dy * dy + dz * dz;
                if (d2 <= reach2 && c.w > 0f) scored.Add((d2, c));
            }
            scored.Sort((a, b) => a.d2.CompareTo(b.d2));
            for (int i = 0; i < scored.Count && i < MaxPushers; i++) result.Add(scored[i].v);
        }
    }
}
