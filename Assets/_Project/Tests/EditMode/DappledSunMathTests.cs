using Evolve.World;
using NUnit.Framework;
using UnityEngine;

namespace Evolve.Tests.EditMode
{
    public class DappledSunMathTests
    {
        const float E = DappledSunMath.Elevation, Y = DappledSunMath.Yaw, A = DappledSunMath.SwayAmplitude, P = DappledSunMath.SwayPeriod;

        [Test]
        public void Angles_AtTimeZeroIsBasePlusSmallOffset()
        {
            var a = DappledSunMath.Angles(E, Y, 0f, A, P);
            Assert.AreEqual(E, a.x, 1e-4f);
            Assert.AreEqual(Y, a.y, 1.5f);
        }

        [Test]
        public void Angles_StaysWithinAmplitude()
        {
            for (float t = 0f; t <= 140f; t += 0.5f)
            {
                var a = DappledSunMath.Angles(E, Y, t, A, P);
                Assert.LessOrEqual(Mathf.Abs(a.x - E), A + 1e-4f, "pitch at " + t);
                Assert.LessOrEqual(Mathf.Abs(a.y - Y), 0.6f * A + 1e-4f, "yaw at " + t);
            }
        }

        [Test]
        public void Angles_RepeatsEveryPeriodForPitch()
        {
            Assert.AreEqual(DappledSunMath.Angles(E, Y, 13f, A, P).x, DappledSunMath.Angles(E, Y, 13f + P, A, P).x, 1e-3f);
        }

        [Test]
        public void Angles_ZeroPeriodDoesNotSway()
        {
            var a = DappledSunMath.Angles(E, Y, 33f, A, 0f);
            Assert.AreEqual(E, a.x);
            Assert.AreEqual(Y, a.y);
        }

        [Test]
        public void Rotation_PointsDownward()
        {
            var forward = DappledSunMath.Rotation(E, Y, 0f, A, P) * Vector3.forward;
            Assert.Less(forward.y, -0.9f, "the sun shines almost straight down");
        }

        [Test]
        public void Constants_AreSafe()
        {
            Assert.GreaterOrEqual(DappledSunMath.Intensity, 0.4f);
            Assert.LessOrEqual(DappledSunMath.Intensity, 1f);
            Assert.GreaterOrEqual(DappledSunMath.ShadowDistance, 80f);
            Assert.GreaterOrEqual(DappledSunMath.ShadowCascades, 2);
            Assert.LessOrEqual(DappledSunMath.ShadowCascades, 4);
            var c = DappledSunMath.AmbientColor;
            Assert.LessOrEqual(Mathf.Max(c.r, c.g, c.b), 0.12f, "the cave stays dark");
        }
    }
}
