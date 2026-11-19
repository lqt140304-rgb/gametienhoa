using Evolve.Maze;
using NUnit.Framework;
using UnityEngine;

namespace Evolve.Tests.Maze
{
    [TestFixture]
    public class InteractiveFloraMathTests
    {
        [Test]
        public void Deflection_WhenIntruderOutsidePushRadius_ReturnsFalseAndZeroAngle()
        {
            Vector3 plant = new Vector3(5f, 2f, 5f);
            Vector3 intruder = new Vector3(15f, 2f, 5f);

            bool hit = InteractiveFloraMath.ComputeHorizontalDeflection(
                plant,
                intruder,
                InteractiveFloraMath.DefaultPushRadius,
                InteractiveFloraMath.DefaultMaxBendAngle,
                out Vector3 axis,
                out float angle);

            Assert.IsFalse(hit);
            Assert.AreEqual(0f, angle, 0.001f);
        }

        [Test]
        public void Deflection_WhenIntruderInside_ReturnsTrueAndPositiveAngle()
        {
            Vector3 plant = new Vector3(0f, 0f, 0f);
            Vector3 intruder = new Vector3(0.5f, 0f, 0f); // Tiếp cận từ bên phải

            bool hit = InteractiveFloraMath.ComputeHorizontalDeflection(
                plant,
                intruder,
                2.0f,
                40.0f,
                out Vector3 axis,
                out float angle);

            Assert.IsTrue(hit);
            Assert.Greater(angle, 0f);
            Assert.LessOrEqual(angle, 40.0f);
            Assert.Greater(axis.magnitude, 0.9f); // Trục xoay chuẩn hóa
        }

        [Test]
        public void Deflection_CloserIntruder_ProducesLargerBendAngle()
        {
            Vector3 plant = Vector3.zero;
            Vector3 nearIntruder = new Vector3(0.3f, 0f, 0f);
            Vector3 farIntruder = new Vector3(1.2f, 0f, 0f);

            InteractiveFloraMath.ComputeHorizontalDeflection(plant, nearIntruder, 2.0f, 40f, out _, out float nearAngle);
            InteractiveFloraMath.ComputeHorizontalDeflection(plant, farIntruder, 2.0f, 40f, out _, out float farAngle);

            Assert.Greater(nearAngle, farAngle, "Tiếp xúc gần tâm hơn phải làm cây rẽ ngang mạnh hơn");
        }

        [Test]
        public void IdleVibration_StaysWithinAmplitudeBounds()
        {
            float amp = 3.5f;
            for (float t = 0f; t < 5f; t += 0.25f)
            {
                Vector2 vib = InteractiveFloraMath.ComputeIdleVibration(t, 2.0f, amp, 0.5f);
                Assert.LessOrEqual(Mathf.Abs(vib.x), amp + 0.001f);
                Assert.LessOrEqual(Mathf.Abs(vib.y), amp + 0.001f);
            }
        }

        [Test]
        public void StepAngle_SmoothlyApproachesTarget()
        {
            float current = 0f;
            float target = 35f;
            float vel = 0f;

            for (int i = 0; i < 10; i++)
            {
                current = InteractiveFloraMath.StepAngle(current, target, ref vel, 0.2f, 0.02f);
            }

            Assert.Greater(current, 0f);
            Assert.Less(current, target);
        }
    }
}
