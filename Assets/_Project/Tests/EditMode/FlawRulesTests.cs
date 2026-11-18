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
