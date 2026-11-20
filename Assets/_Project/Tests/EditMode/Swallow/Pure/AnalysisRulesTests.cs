using Evolve.Organs;
using Evolve.Swallow;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    public class AnalysisRulesTests
    {
        static int SamplesUntilLearned(GeneRarity r, float q)
        {
            float p = 0f;
            for (int i = 1; i < 200; i++) if (AnalysisRules.Roll(r, ref p, q, 1f, 0.999f)) return i;
            return -1;
        }

        [Test] public void Roll_HitBelowChance() { float p = 0f; Assert.IsTrue(AnalysisRules.Roll(GeneRarity.Common, ref p, 1f, 1f, 0.24f)); Assert.AreEqual(0f, p); }
        [Test] public void Roll_MissAddsPity() { float p = 0f; Assert.IsFalse(AnalysisRules.Roll(GeneRarity.Rare, ref p, 1f, 1f, 0.99f)); Assert.AreEqual(0.07f, p, 1e-5f); }
        [Test] public void Pity_Common_Guaranteed_At7() => Assert.AreEqual(7, SamplesUntilLearned(GeneRarity.Common, 1f));
        [Test] public void Pity_Rare_Guaranteed_At15() => Assert.AreEqual(15, SamplesUntilLearned(GeneRarity.Rare, 1f));
        [Test] public void Pity_Epic_Guaranteed_At25() => Assert.AreEqual(25, SamplesUntilLearned(GeneRarity.Epic, 1f));
        [Test] public void Pity_Legendary_Guaranteed_At3() => Assert.AreEqual(3, SamplesUntilLearned(GeneRarity.Legendary, 1f));
        [Test] public void Quality_Halves_ChanceAndPity() { float p = 0f; Assert.IsFalse(AnalysisRules.Roll(GeneRarity.Common, ref p, 0.5f, 1f, 0.2f)); Assert.AreEqual(0.075f, p, 1e-5f); }
        [Test] public void ZeroQuality_NeverLearns() => Assert.AreEqual(-1, SamplesUntilLearned(GeneRarity.Common, 0f));
        [Test] public void Stars_Thresholds() { Assert.AreEqual(1, AnalysisRules.Stars(2)); Assert.AreEqual(2, AnalysisRules.Stars(3)); Assert.AreEqual(2, AnalysisRules.Stars(8)); Assert.AreEqual(3, AnalysisRules.Stars(9)); }
        [Test] public void FromTier_Maps() { Assert.AreEqual(GeneRarity.Common, AnalysisRules.FromTier(OrganTier.D)); Assert.AreEqual(GeneRarity.Common, AnalysisRules.FromTier(OrganTier.C)); Assert.AreEqual(GeneRarity.Rare, AnalysisRules.FromTier(OrganTier.B)); Assert.AreEqual(GeneRarity.Epic, AnalysisRules.FromTier(OrganTier.A)); Assert.AreEqual(GeneRarity.Legendary, AnalysisRules.FromTier(OrganTier.S)); }
        [Test] public void DecoderLevel_AndReveal()
        {
            Assert.AreEqual(1, AnalysisRules.DecoderLevel(9)); Assert.AreEqual(2, AnalysisRules.DecoderLevel(10)); Assert.AreEqual(3, AnalysisRules.DecoderLevel(30));
            Assert.IsFalse(AnalysisRules.ShowsName(1)); Assert.IsTrue(AnalysisRules.ShowsName(2)); Assert.IsFalse(AnalysisRules.ShowsChance(2)); Assert.IsTrue(AnalysisRules.ShowsChance(3));
            Assert.AreEqual(1.2f, AnalysisRules.DecoderChanceMultiplier(3), 1e-5f);
        }
    }
}
