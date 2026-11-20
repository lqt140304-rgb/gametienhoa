using Evolve.Traits;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    public class DeathLossRulesTests
    {
        [Test] public void KeepsOnFirstDeath() => Assert.IsTrue(DeathLossRules.KeepsStomach(true, 2));
        [Test] public void KeepsOnSecondDeath() => Assert.IsTrue(DeathLossRules.KeepsStomach(true, 1));
        [Test] public void LosesOnThirdDeath() => Assert.IsFalse(DeathLossRules.KeepsStomach(true, 0));
        [Test] public void NoCoreNeverKeeps() { Assert.IsFalse(DeathLossRules.KeepsStomach(false, 2)); Assert.IsFalse(DeathLossRules.KeepsStomach(false, 1)); }
        [Test] public void NegativeLivesLoses() => Assert.IsFalse(DeathLossRules.KeepsStomach(true, -1));
    }
}
