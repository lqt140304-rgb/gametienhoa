using System;
using System.Collections.Generic;

namespace Evolve.Maze
{
    /// <summary>
    /// Toán học địa hình tự nhiên thuần C# cho từng khoang mê cung:
    /// lòng trũng ngập lũ, gò đồi nhấp nhô, đồi trú lũ tự nhiên nối sườn dốc, vách bao 22m và khe mở miệng hầm.
    /// </summary>
    public static class MazeTerrainMath
    {
        public const float DefaultChamberRadius = 45f;
        public const float WallInnerRadius = 37f;
        public const float WallOuterRadius = 49f;
        public const float WallHeight = 22f;
        public const float HighLedgePeakY = 6.1f;

        /// <summary>Chọn góc (độ) đặt đồi trú lũ tự nhiên sao cho nằm xa các miệng hầm nhất.</summary>
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

        /// <summary>Vị trí tâm đồi trú lũ tự nhiên trong tọa độ cục bộ của khoang.</summary>
        public static (float x, float z) GetHighLedgeCenter(float chamberRadius, IReadOnlyList<float> tunnelAnglesDeg, int seed)
        {
            float angleRad = PickHighLedgeAngleDeg(tunnelAnglesDeg, seed) * ((float)Math.PI / 180f);
            float dist = chamberRadius * 0.44f; // ~20m so với tâm (vẫn trong vùng sàn 37m)
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

            // 1. Nền gò đồi & lòng trũng uốn lượn tự nhiên
            float hills = 1.15f * (float)Math.Sin(localX * 0.14f + p1) * (float)Math.Cos(localZ * 0.13f + p2)
                        + 0.65f * (float)Math.Sin((localX + localZ) * 0.22f + p2)
                        + 0.30f * (float)Math.Cos((localX - localZ) * 0.35f + p1);

            // Lòng trũng ở phía đối diện đồi trú lũ để nước lũ đọng trước
            float basin = -1.25f * (float)Math.Exp(-(r * r) / (13f * 13f));
            float floorY = 0.7f + hills * 0.85f + basin;

            // Giữ vùng đặt chân chính (gần ArrivalPoint bán kính 3m) khô nhẹ
            if (r < 4.5f)
            {
                float tCenter = Smooth01((r - 1.5f) / 3.0f);
                floorY = Lerp(0.15f, floorY, tCenter);
            }

            // 2. Đồi trú lũ tự nhiên (>= 5.6m) có sườn dốc thoai thoải nối xuống sàn
            if (hasHighLedge)
            {
                var (lx, lz) = GetHighLedgeCenter(chamberRadius, tunnelAnglesDeg, seed);
                float dx = localX - lx;
                float dz = localZ - lz;
                float dLedge = (float)Math.Sqrt(dx * dx + dz * dz);
                // Đỉnh đồi phẳng nhẹ bán kính ~5m, sườn thoai thoải tỏa ra tới bán kính 15m
                float hillFactor = 1f - Smooth01((dLedge - 4.8f) / 10.5f);
                float ledgeY = HighLedgePeakY * hillFactor;
                if (ledgeY > floorY) floorY = ledgeY;
            }

            // 3. Vách đá bao quanh khoang (từ WallInnerRadius dâng lên 22m và chắc chắn đạt đỉnh trước WallOuterRadius)
            float wallStart = chamberRadius * (WallInnerRadius / DefaultChamberRadius);
            float wallEnd = chamberRadius * (43.8f / DefaultChamberRadius);
            float angleDeg = (float)(Math.Atan2(localZ, localX) * (180.0 / Math.PI));
            float wobbleR = 3.2f * (float)Math.Sin(angleDeg * 0.087f + p1) + 1.8f * (float)Math.Cos(angleDeg * 0.174f + p2);
            float wallT = Smooth01((r + wobbleR - wallStart) / Math.Max(1f, wallEnd - wallStart));
            float wallY = WallHeight * wallT;

            // Hạ thấp vách tại các góc có đường hầm để mở hẻm đá đi thẳng ra TunnelPortal
            if (tunnelAnglesDeg != null && r > wallStart - 4f)
            {
                float minAngleDiff = 180f;
                for (int i = 0; i < tunnelAnglesDeg.Count; i++)
                {
                    float diff = Math.Abs(DeltaAngle(angleDeg, tunnelAnglesDeg[i]));
                    if (diff < minAngleDiff) minAngleDiff = diff;
                }
                if (minAngleDiff < 14f)
                {
                    float cutFactor = Smooth01((minAngleDiff - 5.5f) / 8.5f);
                    wallY *= cutFactor;
                    floorY = Lerp(Math.Min(floorY, 0.5f), floorY, cutFactor);
                }
            }

            return Math.Max(floorY, wallY);
        }

        /// <summary>Độ cao vòm trần tại (localX, localZ) để khép kín 100% và gắn rễ/nhũ đá bám chắc vào trần.</summary>
        public static float CeilingHeightAt(float localX, float localZ, int seed, float chamberRadius)
        {
            float r = (float)Math.Sqrt(localX * localX + localZ * localZ);
            float u = Math.Clamp(r / (chamberRadius * 1.08f), 0f, 1f);
            float baseRoof = Lerp(22.5f, 18.0f, u * u);
            float p1 = (seed & 0xFF) * 0.19f;
            float lump = 1.1f * (float)Math.Sin(localX * 0.16f + p1) * (float)Math.Cos(localZ * 0.16f - p1);
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
