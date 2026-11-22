using Evolve.Combat;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    public class ShakeRulesTests
    {
        [Test] public void Strength_FallsWithDistance_AndIsZeroOutsideTheRadius()
        {
            Assert.AreEqual(1f, ShakeRules.Falloff(0f, 20f), 1e-4f);
            Assert.AreEqual(0.5f, ShakeRules.Falloff(10f, 20f), 1e-4f);
            Assert.AreEqual(0f, ShakeRules.Falloff(20f, 20f)); Assert.AreEqual(0f, ShakeRules.Falloff(35f, 20f)); Assert.AreEqual(0f, ShakeRules.Falloff(1f, 0f));
        }

        [Test] public void Shake_DiesAway_BeforeItsDurationEnds_AndNeverGoesNegative()
        {
            Assert.AreEqual(0.4f, ShakeRules.Amplitude(0.4f, 0f, 1f), 1e-4f);
            Assert.Less(ShakeRules.Amplitude(0.4f, 0.5f, 1f), 0.4f * 0.3f);   // a sharp thump: most of it is gone by the middle
            Assert.AreEqual(0f, ShakeRules.Amplitude(0.4f, 1f, 1f)); Assert.AreEqual(0f, ShakeRules.Amplitude(0.4f, 3f, 1f)); Assert.AreEqual(0f, ShakeRules.Amplitude(0.4f, 0.1f, 0f));
        }

        [Test] public void TwoShakes_AddUp_ButStayWithinACap()
        {
            Assert.AreEqual(0.3f, ShakeRules.Combine(0.1f, 0.2f), 1e-4f);
            Assert.AreEqual(ShakeRules.MaxAmplitude, ShakeRules.Combine(0.4f, 0.4f), 1e-4f);
        }

        [Test] public void FootstepStrength_GrowsWithTheBodyScale_AndIsCapped()
        {
            Assert.Less(ShakeRules.StepAmplitude(1f), ShakeRules.StepAmplitude(3f));
            Assert.AreEqual(ShakeRules.StepAmplitude(6f), ShakeRules.StepAmplitude(20f), 1e-5f);
        }

        [Test] public void FootContacts_CrossedBetweenTwoFrames_AreDetectedOncePerCycle()
        {
            var phases = new[] { 0.04f, 0.54f };
            Assert.AreEqual(1, FootstepRules.Contacts(0.50f, 0.60f, phases));          // the second foot lands
            Assert.AreEqual(0, FootstepRules.Contacts(0.10f, 0.40f, phases));          // nothing between
            Assert.AreEqual(1, FootstepRules.Contacts(0.95f, 1.06f, phases));          // the cycle wraps: the first foot lands again
            Assert.AreEqual(2, FootstepRules.Contacts(0.0f, 0.9f, phases));            // a long frame covers both
            Assert.AreEqual(0, FootstepRules.Contacts(0.60f, 0.50f, phases));          // time never runs backwards: ignored
        }
    }
}
