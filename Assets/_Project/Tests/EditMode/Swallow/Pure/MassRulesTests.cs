using Evolve.Swallow;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    public class MassRulesTests
    {
        [Test] public void CanSwallowWhole_At80Percent() { Assert.IsTrue(MassRules.CanSwallowWhole(10f, 8f)); Assert.IsFalse(MassRules.CanSwallowWhole(10f, 8.1f)); }
        [Test] public void DissolveSeconds_Scales() => Assert.AreEqual(6f, MassRules.DissolveSeconds(10f, 16f), 1e-4f);
        [Test] public void CanSculpt_OutsideShelter_False() => Assert.IsFalse(MassRules.CanSculpt(false, true, 50f, GeneRarity.Common));
        [Test] public void CanSculpt_BelowMinMass_False() => Assert.IsFalse(MassRules.CanSculpt(true, true, 8f, GeneRarity.Rare));
        [Test] public void CanSculpt_Unknown_False() => Assert.IsFalse(MassRules.CanSculpt(true, false, 50f, GeneRarity.Common));
        [Test] public void CanSculpt_Ok() => Assert.IsTrue(MassRules.CanSculpt(true, true, 9f, GeneRarity.Rare));
        [Test] public void Costs_AndRefund() { Assert.AreEqual(2f, MassRules.SculptCost(GeneRarity.Common)); Assert.AreEqual(12f, MassRules.SculptCost(GeneRarity.Legendary)); Assert.AreEqual(3.5f, MassRules.Refund(GeneRarity.Epic), 1e-4f); }
        [Test] public void MaxHealth_TenPerMass() => Assert.AreEqual(100f, MassRules.MaxHealth(MassRules.StartMass), 1e-4f);
        [Test] public void Spend_NeverBelowMin() { Assert.AreEqual(5f, MassRules.AfterSpend(10f, 5f)); Assert.AreEqual(-1f, MassRules.AfterSpend(10f, 5.1f)); }
    }
}
