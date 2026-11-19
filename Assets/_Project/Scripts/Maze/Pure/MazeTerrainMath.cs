using System;
using System.Collections.Generic;

namespace Evolve.Maze
{
    /// <summary>
    /// Toán học địa hình tự nhiên thuần C# cho từng khoang mê cung đồ sộ (bán kính 75m = đường kính 150m):
    /// thung lũng trũng, đồi đá đa tầng nhấp nhô, vách bao cao 30m và khe mở rộng cho miệng hang.
    /// </summary>
    public static class MazeTerrainMath
    {
        public const float DefaultChamberRadius = 75f;
        public const float WallInnerRadius = 60f;
        public const float WallOuterRadius = 78f;
        public const float WallHeight = 30f;
        public const float HighLedgePeakY = 6.2f;

        /// <summary>Chọn góc (độ) đặt đồi đá cao tự nhiên sao cho nằm xa các miệng hầm nhất.</summary>
        public static float PickHighLedgeAngleDeg(IReadOnlyList<float> tunnelAnglesDeg, int seed)
        {
            if (tunnelAnglesDeg == null || tunnelAnglesDeg.Count == 0) return 45f;

            float bestAngle = 0f;
            float bestMinDist = -1f;
            for (int deg = 0; deg < 360; deg += 10)
            {
                float minDist = 180f;
                for (int i = 0; i < tunnelAnglesDeg.Count; i++)
                {
                    float d = Math.Abs(DeltaAngle(deg, tunnelAnglesDeg[i]));
                    if (d < minDist) minDist = d;
                }
                if (minDist > bestMinDist)
                {
                    bestMinDist = minDist;
                    bestAngle = deg;
                }
            }
            return bestAngle;
        }

        /// <summary>Vị trí tâm dãy đồi cao tự nhiên trong tọa độ cục bộ của khoang.</summary>
        public static (float x, float z) GetHighLedgeCenter(float chamberRadius, IReadOnlyList<float> tunnelAnglesDeg, int seed)
        {
            float angleRad = PickHighLedgeAngleDeg(tunnelAnglesDeg, seed) * ((float)Math.PI / 180f);
            float dist = chamberRadius * 0.42f;
            return ((float)Math.Cos(angleRad) * dist, (float)Math.Sin(angleRad) * dist);
        }

        /// <summary>Độ cao mặt đất tại (localX, localZ) trong khoang.</summary>
        public static float GroundHeightAt(
            float localX,
            float localZ,
            int seed,
            float chamberRadius,
            IReadOnlyList<float> tunnelAnglesDeg,
            bool hasHighLedge = true)
        {
            float r = (float)Math.Sqrt(localX * localX + localZ * localZ);
            float p1 = (seed & 0xFF) * 0.13f;
            float p2 = ((seed >> 8) & 0xFF) * 0.17f;

            // 1. Địa hình đồi thung lũng đa tầng quy mô lớn (chênh lệch rõ rệt, không phẳng lì)
            float hills = 2.1f * (float)Math.Sin(localX * 0.075f + p1) * (float)Math.Cos(localZ * 0.070f + p2)
                        + 1.35f * (float)Math.Sin((localX + localZ) * 0.11f + p2)
                        + 0.75f * (float)Math.Cos((localX - localZ) * 0.19f + p1);

            float basin = -1.6f * (float)Math.Exp(-(r * r) / (18f * 18f));
            float floorY = 1.1f + hills + basin;

            // Vùng đặt chân trung tâm (r < 6m) phẳng nhẹ vừa phải để arrival an toàn
            if (r < 6.5f)
            {
                float tCenter = Smooth01((r - 2.0f) / 4.5f);
                floorY = Lerp(0.25f, floorY, tCenter);
            }

            // 2. Dãy đồi tự nhiên cao >= 6m (tạo địa hình nhiều tầng độ cao)
            if (hasHighLedge)
            {
                var (lx, lz) = GetHighLedgeCenter(chamberRadius, tunnelAnglesDeg, seed);
                float dx = localX - lx;
                float dz = localZ - lz;
                float dLedge = (float)Math.Sqrt(dx * dx + dz * dz);
                float hillFactor = 1f - Smooth01((dLedge - 7.5f) / 16f);
                float ledgeY = HighLedgePeakY * hillFactor;
                if (ledgeY > floorY) floorY = ledgeY;
            }

            // 3. Vách đá bao quanh khoang (từ WallInnerRadius dâng lên 30m, đạt đỉnh trước WallOuterRadius)
            float wallStart = chamberRadius * (WallInnerRadius / DefaultChamberRadius);
            float wallEnd = chamberRadius * (73.5f / DefaultChamberRadius);
            float angleDeg = (float)(Math.Atan2(localZ, localX) * (180.0 / Math.PI));
            float wobbleR = 3.2f * (float)Math.Sin(angleDeg * 0.087f + p1) + 1.8f * (float)Math.Cos(angleDeg * 0.174f + p2);
            float wallT = Smooth01((r + wobbleR - wallStart) / Math.Max(1f, wallEnd - wallStart));
            float wallY = WallHeight * wallT;

            // Hạ thấp vách rộng rãi tại các góc có miệng hang để lối vào không bao giờ bị vách che lấp
            if (tunnelAnglesDeg != null && r > wallStart - 10f)
            {
                float minAngleDiff = 180f;
                for (int i = 0; i < tunnelAnglesDeg.Count; i++)
                {
                    float diff = Math.Abs(DeltaAngle(angleDeg, tunnelAnglesDeg[i]));
                    if (diff < minAngleDiff) minAngleDiff = diff;
                }
                if (minAngleDiff < 17f)
                {
                    float cutFactor = Smooth01((minAngleDiff - 8.5f) / 8.5f);
                    wallY *= cutFactor;
                    floorY = Lerp(Math.Min(floorY, 0.4f), floorY, cutFactor);
                }
            }

            return Math.Max(floorY, wallY);
        }

        /// <summary>Độ cao vòm trần tại (localX, localZ) — cao 32m ở giữa và khép kín vào đỉnh vách 30m ở rìa.</summary>
        public static float CeilingHeightAt(float localX, float localZ, int seed, float chamberRadius)
        {
            float r = (float)Math.Sqrt(localX * localX + localZ * localZ);
            float u = Math.Clamp(r / (chamberRadius * 1.05f), 0f, 1f);
            float baseRoof = Lerp(32.5f, 25.0f, u * u);
            float p1 = (seed & 0xFF) * 0.19f;
            float lump = 1.6f * (float)Math.Sin(localX * 0.10f + p1) * (float)Math.Cos(localZ * 0.10f - p1);
            return baseRoof + lump;
        }

        static float DeltaAngle(float a, float b)
        {
            float d = (b - a) % 360f;
            if (d > 180f) d -= 360f;
            if (d < -180f) d += 360f;
            return d;
        }

        static float Smooth01(float t)
        {
            t = Math.Clamp(t, 0f, 1f);
            return t * t * (3f - 2f * t);
        }

        static float Lerp(float a, float b, float t) => a + (b - a) * t;
    }
}
