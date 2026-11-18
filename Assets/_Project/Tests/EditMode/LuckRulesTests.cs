using Evolve.Organs;
using Evolve.Traits;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    public class LuckRulesTests
    {
        [Test]
        public void TierMultiplierMatchesSpec()
        {
            Assert.AreEqual(1.20f, LuckRules.TierMultiplier(OrganTier.D, false), 1e-6f);
            Assert.AreEqual(1.16f, LuckRules.TierMultiplier(OrganTier.C, false), 1e-6f);
            Assert.AreEqual(1.12f, LuckRules.TierMultiplier(OrganTier.B, false), 1e-6f);
            Assert.AreEqual(1.08f, LuckRules.TierMultiplier(OrganTier.A, false), 1e-6f);
            Assert.AreEqual(1.04f, LuckRules.TierMultiplier(OrganTier.S, false), 1e-6f);
        }

        [Test]
        public void BossGetsHalf() =>
            Assert.AreEqual(1.10f, LuckRules.TierMultiplier(OrganTier.D, true), 1e-6f);

        [Test]
        public void DropChanceMultiplierIsWeightedAverage()
        {
            var m = LuckRules.DropChanceMultiplier(new[] { (OrganTier.D, 0.5f), (OrganTier.S, 0.5f) }, false);
            Assert.AreEqual(1.12f, m, 1e-6f);
            Assert.AreEqual(1f, LuckRules.DropChanceMultiplier(null, false), 1e-6f);
            Assert.AreEqual(1f, LuckRules.DropChanceMultiplier(new (OrganTier, float)[0], false), 1e-6f);
            Assert.AreEqual(1f, LuckRules.DropChanceMultiplier(new[] { (OrganTier.D, 0f) }, false), 1e-6f);
        }

        [Test]
        public void AdjustedChanceNeverReachesCertainty()
        {
            Assert.LessOrEqual(LuckRules.AdjustedDropChance(0.9f, 1.2f), TraitTuning.DropChanceCap);
            Assert.AreEqual(1f, LuckRules.AdjustedDropChance(1f, 1.2f), 1e-6f);    // already above the cap: never lowered
            Assert.AreEqual(0.6f * 1.16f, LuckRules.AdjustedDropChance(0.6f, 1.16f), 1e-6f);
        }

        [Test]
        public void ZeroChanceStaysZero() =>
            Assert.AreEqual(0f, LuckRules.AdjustedDropChance(0f, 1.2f), 1e-6f);

        [Test]
        public void NoLuckIsIdentity()
        {
            Assert.AreEqual(0.6f, LuckRules.AdjustedDropChance(0.6f, 1f), 1e-6f);
            Assert.AreEqual(0.6f, LuckRules.AdjustedDropChance(0.6f, 0.5f), 1e-6f);   // a multiplier below 1 is ignored
        }
    }
}
