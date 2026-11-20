using Evolve.Player;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    public class SlimeStepTests
    {
        [Test] public void SmallRock_Climbs_Wall_DoesNot()
        {
            float max = SlimeStep.MaxStep(1f);
            Assert.AreEqual(0.4f, max, 1e-4f);
            Assert.IsTrue(SlimeStep.CanClimb(0.3f, max));
            Assert.IsFalse(SlimeStep.CanClimb(1.2f, max));   // wall
            Assert.IsFalse(SlimeStep.CanClimb(0.01f, max));  // floor seam: the ground hug's job
        }

        [Test] public void BiggerSlime_BiggerRocks_WithinLimits()
        {
            Assert.Greater(SlimeStep.MaxStep(2f), SlimeStep.MaxStep(1f));
            Assert.AreEqual(0.15f, SlimeStep.MaxStep(0.1f), 1e-4f);
            Assert.AreEqual(1.5f, SlimeStep.MaxStep(10f), 1e-4f);
        }

        [Test] public void ClimbSpeed_Bounded()
        {
            Assert.AreEqual(1f, SlimeStep.ClimbSpeed(0.05f), 1e-4f);
            Assert.AreEqual(2.5f, SlimeStep.ClimbSpeed(3f), 1e-4f);
        }
    }
}
