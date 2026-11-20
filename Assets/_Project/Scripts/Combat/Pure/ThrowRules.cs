namespace Evolve.Combat
{
    /// <summary>A thrown or leaped body on a ballistic arc: the launch velocity that lands exactly on the target after a chosen flight time, and where it is along the way.</summary>
    public static class ThrowRules
    {
        public static (float vx, float vy, float vz) Launch(float fx, float fy, float fz, float tx, float ty, float tz, float flightSeconds, float gravity = 9.81f)
        {
            float t = flightSeconds < 0.01f ? 0.01f : flightSeconds;
            return ((tx - fx) / t, (ty - fy + gravity * t * t * 0.5f) / t, (tz - fz) / t);
        }

        public static (float x, float y, float z) PositionAt(float fx, float fy, float fz, (float vx, float vy, float vz) v, float t, float gravity = 9.81f) =>
            (fx + v.vx * t, fy + v.vy * t - gravity * t * t * 0.5f, fz + v.vz * t);

        public static bool Hits(float px, float py, float pz, float tx, float ty, float tz, float radius)
        {
            float dx = px - tx, dy = py - ty, dz = pz - tz;
            return dx * dx + dy * dy + dz * dz <= radius * radius;
        }
    }
}
