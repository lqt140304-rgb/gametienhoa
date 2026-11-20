using System;

namespace Evolve.Combat
{
    /// <summary>The wind spears of Cáo Gió's Giáo Gió: where each one circles above the tails while the fox winds up, and when each is thrown.</summary>
    public static class SpearRules
    {
        static float AngleDeg(int index, int count, float time, float degPerSecond) => index * 360f / Math.Max(1, count) + time * degPerSecond;

        /// <summary>Offset of spear <paramref name="index"/> from the circle's centre: spread evenly round a circle of <paramref name="radius"/> that turns at <paramref name="degPerSecond"/>, bobbing a little around <paramref name="height"/>.</summary>
        public static (float x, float y, float z) OrbitOffset(int index, int count, float time, float radius, float degPerSecond, float height)
        {
            float a = AngleDeg(index, count, time, degPerSecond) * (float)Math.PI / 180f;
            return ((float)Math.Cos(a) * radius, height + 0.1f * (float)Math.Sin(time * 4f + index * 2.1f), (float)Math.Sin(a) * radius);
        }

        /// <summary>The way the spear's point faces: along the circle (the angle of its position plus 90 degrees).</summary>
        public static float OrbitHeadingDeg(int index, int count, float time, float degPerSecond) => AngleDeg(index, count, time, degPerSecond) + 90f;

        /// <summary>Seconds after the strike starts at which spear <paramref name="index"/> is thrown.</summary>
        public static float LaunchTime(int index, float interval) => index * interval;
    }
}
