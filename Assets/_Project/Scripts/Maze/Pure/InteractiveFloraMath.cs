using UnityEngine;

namespace Evolve.Maze
{
    /// <summary>
    /// Thuật toán thuần túy tính toán vật lý tương tác rẽ ngang khi slime / sinh vật đi qua
    /// và dao động rung động nhẹ tự nhiên trong môi trường ngầm (100% testable không phụ thuộc scene).
    /// </summary>
    public static class InteractiveFloraMath
    {
        public const float DefaultPushRadius = 1.6f;
        public const float DefaultMaxBendAngle = 38.0f;
        public const float DefaultIdleFrequency = 2.4f;
        public const float DefaultIdleAmplitude = 2.2f;

        /// <summary>
        /// Tính toán hướng và góc rẽ ngang khi có vật thể/slime tiếp xúc trong phạm vi bán kính.
        /// Khi slime tiến vào tâm bụi cây, bụi cây sẽ rẽ ngang sang hai bên theo góc nghiêng tăng dần.
        /// </summary>
        public static bool ComputeHorizontalDeflection(
            Vector3 plantPos,
            Vector3 intruderPos,
            float pushRadius,
            float maxBendAngle,
            out Vector3 bendAxis,
            out float bendAngle)
        {
            if (pushRadius <= 0f)
            {
                bendAxis = Vector3.forward;
                bendAngle = 0f;
                return false;
            }

            Vector3 offset = plantPos - intruderPos;
            offset.y = 0f; // Chỉ xét lực đẩy trên mặt phẳng nằm ngang
            float dist = offset.magnitude;

            if (dist >= pushRadius)
            {
                bendAxis = Vector3.forward;
                bendAngle = 0f;
                return false;
            }

            Vector3 pushDir;
            if (dist < 0.001f)
            {
                pushDir = Vector3.right;
            }
            else
            {
                pushDir = offset / dist;
            }

            // Lực đẩy phi tuyến (cong mượt): tiếp xúc êm, đẩy dứt khoát ở gần tâm
            float t = 1f - Mathf.Clamp01(dist / pushRadius);
            float strength = t * (2f - t); // Quad ease-out
            bendAngle = strength * maxBendAngle;

            // Trục xoay vuông góc với hướng đẩy và phương thẳng đứng
            bendAxis = Vector3.Cross(Vector3.up, pushDir).normalized;
            if (bendAxis.sqrMagnitude < 0.001f)
                bendAxis = Vector3.forward;

            return true;
        }

        /// <summary>
        /// Tính toán góc rung động nhẹ ở trạng thái tĩnh (nhịp thở sinh học / luồng gió bào tử ngầm).
        /// </summary>
        public static Vector2 ComputeIdleVibration(
            float time,
            float frequency,
            float amplitude,
            float phaseOffset)
        {
            if (frequency <= 0f || amplitude <= 0f) return Vector2.zero;

            float x = Mathf.Sin(time * frequency + phaseOffset) * amplitude;
            float z = Mathf.Cos(time * (frequency * 1.37f) + phaseOffset * 1.6f) * (amplitude * 0.65f);
            return new Vector2(x, z);
        }

        /// <summary>
        /// Hồi phục đàn hồi góc nghiêng mượt mà có quán tính nhẹ về vị trí cân bằng.
        /// </summary>
        public static float StepAngle(
            float current,
            float target,
            ref float velocity,
            float smoothTime,
            float deltaTime)
        {
            if (deltaTime <= 0f) return current;
            float st = Mathf.Max(0.01f, smoothTime);
            return Mathf.SmoothDamp(current, target, ref velocity, st, float.MaxValue, deltaTime);
        }
    }
}
