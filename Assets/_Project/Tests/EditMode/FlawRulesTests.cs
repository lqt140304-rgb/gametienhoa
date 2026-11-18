using Evolve.Traits;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    public class FlawRulesTests
    {
        [Test]
        public void HungerBurnDefaultsToOneWithoutFlaw() =>
            Assert.AreEqual(1f, FlawRules.HungerBurnMultiplier(0f), 1e-6f);

        [Test]
        public void HungerBurnFullStrength() =>
            Assert.AreEqual(1.10f, FlawRules.HungerBurnMultiplier(1f), 1e-6f);

        [Test]
        public void HitFreezeScalesWithStrength()
        {
            Assert.AreEqual(0.7f, FlawRules.HitFreezeSeconds(1f), 1e-6f);
            Assert.AreEqual(0f, FlawRules.HitFreezeSeconds(0f), 1e-6f);
            Assert.AreEqual(0.35f, FlawRules.HitFreezeSeconds(0.5f), 1e-6f);
        }

        [Test]
        public void TurnRateNormalAboveThresholdAndWithoutFlaw()
        {
            Assert.AreEqual(1f, FlawRules.TurnRateMultiplier(0.25f, 1f), 1e-6f);
            Assert.AreEqual(1f, FlawRules.TurnRateMultiplier(0.9f, 1f), 1e-6f);
            Assert.AreEqual(1f, FlawRules.TurnRateMultiplier(0.05f, 0f), 1e-6f);
        }

        [Test]
        public void TurnRateSlowerBelowThresholdButNeverBelowHalf()
        {
            float low = FlawRules.TurnRateMultiplier(0.1f, 1f);
            Assert.Less(low, 1f);
            Assert.GreaterOrEqual(low, 0.5f);
            Assert.AreEqual(0.5f, FlawRules.TurnRateMultiplier(0f, 1f), 1e-6f);
            Assert.GreaterOrEqual(FlawRules.TurnRateMultiplier(-1f, 1f), 0.5f);   // bad input
            Assert.Greater(FlawRules.TurnRateMultiplier(0f, 0.3f), 0.5f);        // compensated: milder
        }

        [Test]
        public void SteerRateGrowsContinuouslyWithTurnMultiplier()
        {
            Assert.AreEqual(3f, FlawRules.SteerRate(0.5f, 6f), 1e-4f);
            Assert.Greater(FlawRules.SteerRate(0.85f, 6f), 10f);       // a mostly compensated flaw is nearly instant
            Assert.Greater(FlawRules.SteerRate(0.999f, 6f), 100f);     // no visible jump at the threshold
            float previous = 0f;
            for (float t = 0.5f; t <= 0.999f; t += 0.05f) { float r = FlawRules.SteerRate(t, 6f); Assert.Greater(r, previous); previous = r; }
        }

        [Test]
        public void AbsorbOnlyWithinWindow()
        {
            Assert.AreEqual(1f, FlawRules.AbsorbMultiplier(-1f, 1f), 1e-6f);
            Assert.AreEqual(1f, FlawRules.AbsorbMultiplier(20f, 1f), 1e-6f);
            Assert.AreEqual(1f, FlawRules.AbsorbMultiplier(500f, 1f), 1e-6f);
            Assert.AreEqual(1f, FlawRules.AbsorbMultiplier(5f, 0f), 1e-6f);
        }

        [Test]
        public void AbsorbSeventyPercentAtFullStrength()
        {
            Assert.AreEqual(0.70f, FlawRules.AbsorbMultiplier(0f, 1f), 1e-6f);
            Assert.AreEqual(0.70f, FlawRules.AbsorbMultiplier(19.9f, 1f), 1e-6f);
        }
    }
}
