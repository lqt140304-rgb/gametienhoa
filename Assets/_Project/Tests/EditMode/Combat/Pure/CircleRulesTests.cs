using System;
using Evolve.Combat;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    public class CircleRulesTests
    {
        static float Dot((float x, float z) a, float bx, float bz) => a.x * bx + a.z * bz;

        [Test] public void OnTheRing_ItWalksAlongTheCircle_AroundThePrey()
        {
            var d = CircleRules.Steer(CircleRules.Ring, 0f, 1, CircleRules.Ring);          // the prey is straight ahead along +x, at exactly the ring distance
            Assert.AreEqual(0f, Dot(d, 1f, 0f), 1e-4f);                                      // no pull in or out
            Assert.AreEqual(1f, (float)Math.Sqrt(d.x * d.x + d.z * d.z), 1e-4f);
        }

        [Test] public void TooFar_ItDrawsIn_TooClose_ItBacksOff_AndTheSignChoosesTheDirectionAround()
        {
            Assert.Greater(Dot(CircleRules.Steer(2f * CircleRules.Ring, 0f, 1, CircleRules.Ring), 1f, 0f), 0.3f);
            Assert.Less(Dot(CircleRules.Steer(CircleRules.Ring / 3f, 0f, 1, CircleRules.Ring), 1f, 0f), -0.3f);
            var a = CircleRules.Steer(CircleRules.Ring, 0f, 1, CircleRules.Ring); var b = CircleRules.Steer(CircleRules.Ring, 0f, -1, CircleRules.Ring);
            Assert.AreEqual(-a.z, b.z, 1e-4f);                                               // the other way round
            Assert.AreNotEqual(0f, a.z);
        }

        [Test] public void OnlyTheGolemSizesUpItsPrey()
        {
            Assert.Greater(CircleRules.IdleSeconds("Golem Thạch Anh"), 1.5f); Assert.Greater(CircleRules.IdleSeconds("GolemThachAnh (Meshy)(Clone)"), 1.5f);
            Assert.AreEqual(0f, CircleRules.IdleSeconds("Chó Xương")); Assert.AreEqual(0f, CircleRules.IdleSeconds("Cáo Gió"));
        }
    }
}
