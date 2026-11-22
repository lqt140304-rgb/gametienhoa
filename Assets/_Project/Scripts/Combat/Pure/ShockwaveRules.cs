using System;

namespace Evolve.Combat
{
    /// <summary>The shock wave of the axe's Bổ Đất: a band of <c>width</c> metres that runs straight out from the golem along a direction, its front at <see cref="Front"/>, up to <c>length</c>. Flat maths, pure.</summary>
    public static class ShockwaveRules
    {
        /// <summary>Metres from the origin the front has reached after <paramref name="t"/> seconds at <paramref name="speed"/>, stopping at <paramref name="length"/>.</summary>
        public static float Front(float speed, float t, float length) => Math.Min(length, Math.Max(0f, speed * t));

        /// <summary>Is the point (<paramref name="px"/>, <paramref name="pz"/>) in the wave's front (within <paramref name="depth"/> of <paramref name="front"/> along the way, and within half the <paramref name="width"/> of its line)?</summary>
        public static bool Hits(float ox, float oz, float dx, float dz, float length, float width, float front, float depth, float px, float pz)
        {
            float n = (float)Math.Sqrt(dx * dx + dz * dz);
            if (n < 1e-6f) return false;
            dx /= n; dz /= n;
            float rx = px - ox, rz = pz - oz;
            float along = rx * dx + rz * dz, lateral = Math.Abs(rx * dz - rz * dx);
            return along >= 0f && along <= length && lateral <= width * 0.5f && Math.Abs(along - front) <= depth * 0.5f;
        }
    }
}
