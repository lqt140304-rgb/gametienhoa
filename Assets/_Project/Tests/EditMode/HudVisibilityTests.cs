using Evolve.Player;
using Evolve.Survival;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    public class HudVisibilityTests
    {
        [Test] public void HudVisibility_Thresholds()
        {
            Assert.IsTrue(Evolve.Survival.HudVisibility.ShowHealth(0.69f));
            Assert.IsFalse(Evolve.Survival.HudVisibility.ShowHealth(0.7f));
            Assert.IsTrue(Evolve.Survival.HudVisibility.ShowEnergy(0.49f));
            Assert.IsFalse(Evolve.Survival.HudVisibility.ShowEnergy(0.5f));
            Assert.IsTrue(Evolve.Survival.HudVisibility.ShowHunger(0.49f));
            Assert.IsFalse(Evolve.Survival.HudVisibility.ShowHunger(0.5f));
            Assert.IsTrue(Evolve.Survival.HudVisibility.ShowThirst(0.49f, true));
            Assert.IsFalse(Evolve.Survival.HudVisibility.ShowThirst(0.5f, true));
            Assert.IsFalse(Evolve.Survival.HudVisibility.ShowThirst(0f, false));
            Assert.IsFalse(Evolve.Survival.HudVisibility.ShowThirst(0f, false, true));
            Assert.IsTrue(Evolve.Survival.HudVisibility.ShowStress(41f));
            Assert.IsFalse(Evolve.Survival.HudVisibility.ShowStress(40f));
            Assert.IsTrue(Evolve.Survival.HudVisibility.ShowTemperature(true));
            Assert.IsFalse(Evolve.Survival.HudVisibility.ShowTemperature(false));
        }

        [Test] public void FullForcesAllVisible()
        {
            Assert.IsTrue(Evolve.Survival.HudVisibility.ShowHealth(1f, true));
            Assert.IsTrue(Evolve.Survival.HudVisibility.ShowEnergy(1f, true));
            Assert.IsTrue(Evolve.Survival.HudVisibility.ShowHunger(1f, true));
            Assert.IsTrue(Evolve.Survival.HudVisibility.ShowThirst(1f, true, true));
            Assert.IsTrue(Evolve.Survival.HudVisibility.ShowStress(0f, true));
            Assert.IsTrue(Evolve.Survival.HudVisibility.ShowTemperature(false, true));
        }

        [Test] public void BodyTintAmounts()
        {
            BodyFeedback.TintAmounts(100, 100, 100, 0, true, out float dry, out float pale);
            Assert.AreEqual(0f, dry, 1e-4f); Assert.AreEqual(0f, pale, 1e-4f);
            BodyFeedback.TintAmounts(0, 100, 0, 100, true, out dry, out pale);
            Assert.AreEqual(1f, dry, 1e-4f); Assert.AreEqual(1f, pale, 1e-4f);
            BodyFeedback.TintAmounts(0, 100, 100, 100, false, out dry, out pale);
            Assert.AreEqual(0f, dry, 1e-4f);
        }
    }
}
