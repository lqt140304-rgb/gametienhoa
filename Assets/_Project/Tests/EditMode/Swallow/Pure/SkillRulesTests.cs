using Evolve.Swallow;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    public class SkillRulesTests
    {
        [Test] public void UseOf_Eyes_Passive() => Assert.AreEqual(GeneUse.Passive, SkillRules.UseOf(false, true, 0f, 1f, 0f));
        [Test] public void UseOf_HeatMembrane_Passive() => Assert.AreEqual(GeneUse.Passive, SkillRules.UseOf(false, false, 20f, 1f, 0f));
        [Test] public void UseOf_Legs_ActiveSprint() => Assert.AreEqual(GeneUse.Active, SkillRules.UseOf(false, false, 0f, 2f, 0f));
        [Test] public void UseOf_AbilityWins() => Assert.AreEqual(GeneUse.Active, SkillRules.UseOf(true, false, 0f, 1.7f, 0f));
        [Test] public void UseOf_ToxicSpikes_ActiveWithAbility() => Assert.AreEqual(GeneUse.Active, SkillRules.UseOf(true, false, 0f, 1f, 15f));
        [Test] public void UseOf_SpikesWithoutAbility_Passive() => Assert.AreEqual(GeneUse.Passive, SkillRules.UseOf(false, false, 0f, 1f, 15f));
        [Test] public void UseOf_Plain_None() => Assert.AreEqual(GeneUse.None, SkillRules.UseOf(false, false, 0f, 1f, 0f));

        [Test] public void AbilityLevel_FromStars_StarTwoAlreadyUpgrades() { Assert.AreEqual(2, SkillRules.AbilityLevel(1)); Assert.AreEqual(3, SkillRules.AbilityLevel(2)); Assert.AreEqual(3, SkillRules.AbilityLevel(3)); }
        [Test] public void SprintSeconds_GrowWithStars() { Assert.AreEqual(4f, SkillRules.SprintSeconds(1)); Assert.AreEqual(5f, SkillRules.SprintSeconds(2)); Assert.AreEqual(6f, SkillRules.SprintSeconds(3)); }
        [Test] public void MaxPassives_Is5() => Assert.AreEqual(5, SkillRules.MaxPassives);

        [Test]
        public void VisualScale_GrowsHoldsThenRetracts()
        {
            Assert.AreEqual(0f, SkillRules.VisualScale(0f, 4f), 1e-4f);
            Assert.AreEqual(1f, SkillRules.VisualScale(SkillRules.GrowSeconds, 4f), 1e-4f);
            Assert.AreEqual(1f, SkillRules.VisualScale(2f, 4f), 1e-4f);
            Assert.AreEqual(0.5f, SkillRules.VisualScale(SkillRules.GrowSeconds + 4f + SkillRules.RetractSeconds * 0.5f, 4f), 1e-4f);
            Assert.AreEqual(0f, SkillRules.VisualScale(SkillRules.ShowSeconds(4f), 4f), 1e-4f);
            Assert.AreEqual(0f, SkillRules.VisualScale(99f, 4f), 1e-4f);
        }

        [Test] public void SprintBoost_MaxNotStacked() => Assert.AreEqual(2f, SkillRules.CombineBoost(1.4f, 2f), 1e-4f);

        [Test]
        public void SprintTimer_AppliesThenExpires()
        {
            var t = new SprintTimer();
            Assert.AreEqual(1f, t.Multiplier(0f));
            t.Apply(10f, 2f, 4f);
            Assert.AreEqual(2f, t.Multiplier(10f));
            Assert.AreEqual(2f, t.Multiplier(13.9f));
            Assert.AreEqual(1f, t.Multiplier(14.1f));
        }

        [Test]
        public void SprintTimer_Overlap_StrongerWinsAndExtends()
        {
            var t = new SprintTimer();
            t.Apply(0f, 1.4f, 4f);
            t.Apply(2f, 2f, 4f);                       // until 6, multiplier 2
            Assert.AreEqual(2f, t.Multiplier(3f));
            Assert.AreEqual(2f, t.Multiplier(5.9f));
            t.Apply(3f, 1.4f, 1f);                     // weaker and shorter: changes nothing
            Assert.AreEqual(2f, t.Multiplier(5f));
            Assert.AreEqual(1f, t.Multiplier(6.1f));
        }

        [Test]
        public void SprintTimer_StrongEnds_WeakerContinuesAtItsOwnFactor()
        {
            var t = new SprintTimer();
            t.Apply(0f, 2f, 4f);                       // strong, until 4
            t.Apply(1f, 1.4f, 6f);                     // weaker but longer, until 7
            Assert.AreEqual(2f, t.Multiplier(3f), 1e-5f);
            Assert.AreEqual(1.4f, t.Multiplier(5f), 1e-5f);
            Assert.AreEqual(1f, t.Multiplier(7.1f), 1e-5f);
        }

        [Test]
        public void SprintTimer_Reset_ClearsBoost()
        {
            var t = new SprintTimer();
            t.Apply(0f, 2f, 4f);
            t.Reset();
            Assert.AreEqual(1f, t.Multiplier(1f));
        }

        [Test]
        public void SprintTimer_ExpiredThenNew_StartsFresh()
        {
            var t = new SprintTimer();
            t.Apply(0f, 2f, 4f);
            t.Apply(10f, 1.4f, 4f);                    // the old one ended long ago: a weaker new one is not held up by it
            Assert.AreEqual(1.4f, t.Multiplier(11f), 1e-5f);
        }
    }
}
