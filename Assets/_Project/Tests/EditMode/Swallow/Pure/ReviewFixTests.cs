using Evolve.Player;
using Evolve.Swallow;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    /// <summary>Fixes from the whole-branch review.</summary>
    public class ReviewFixTests
    {
        // Big slimes must still fit the old shelters (ShelterZone.MaxBodyScale 1.3) until step 3 rebuilds them.
        [Test] public void ShelterFit_BigSlimeSqueezesIn() => Assert.LessOrEqual(MassRules.ShelterFitScale(BodySize.ScaleForMass(40f)), 1.3f);
        [Test] public void ShelterFit_SmallSlimeUnchanged() => Assert.AreEqual(0.8f, MassRules.ShelterFitScale(0.8f), 1e-5f);

        // A co-op client cannot drop a carcass into the host's world: spitting must not destroy the item.
        [Test] public void CanSpit_OnlyWhenNotClient() { Assert.IsTrue(SwallowRules.CanSpit(false)); Assert.IsFalse(SwallowRules.CanSpit(true)); }

        // The sculpt button is locked with its reason before the click.
        [Test] public void SculptBlock_OutsideShelter() => StringAssert.Contains("hang trú", MassRules.SculptBlockReason(false, 50f, 0f, GeneRarity.Common));
        [Test] public void SculptBlock_ShortOfMass() => StringAssert.Contains("Không đủ khối", MassRules.SculptBlockReason(true, 8f, 0f, GeneRarity.Rare));
        [Test] public void SculptBlock_RefundCounts() => Assert.IsNull(MassRules.SculptBlockReason(true, 8f, 2f, GeneRarity.Rare));
        [Test] public void SculptBlock_Ok() => Assert.IsNull(MassRules.SculptBlockReason(true, 10f, 0f, GeneRarity.Rare));
    }
}
