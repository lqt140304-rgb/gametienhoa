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

        [Test] public void AbilityLevel_FromStars() { Assert.AreEqual(2, SkillRules.AbilityLevel(1)); Assert.AreEqual(2, SkillRules.AbilityLevel(2)); Assert.AreEqual(3, SkillRules.AbilityLevel(3)); }
        [Test] public void SprintSeconds_GrowWithStars() { Assert.AreEqual(4f, SkillRules.SprintSeconds(1)); Assert.AreEqual(5f, SkillRules.SprintSeconds(2)); Assert.AreEqual(6f, SkillRules.SprintSeconds(3)); }
        [Test] public void MaxPassives_Is3() => Assert.AreEqual(3, SkillRules.MaxPassives);

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
    }
}
