using Evolve.Combat;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    public class AuraRulesTests
    {
        [Test] public void ThePushHasItsPlannedNumbers()
        {
            Assert.AreEqual(4f, PushAura.Radius); Assert.AreEqual(1.5f, PushAura.Cooldown); Assert.AreEqual(6f, PushAura.Damage); Assert.AreEqual(10f, PushAura.Force); Assert.AreEqual(1f, PushAura.Freeze);
        }

        [Test] public void TwoPlayers_HaveTheirOwnCooldown()
        {
            var a = new PushAura();
            Assert.IsTrue(a.TryPush(1, 2f, 0f));
            Assert.IsTrue(a.TryPush(2, 3f, 0.1f));            // the second player is pushed at once: the first one's cooldown is not theirs
            Assert.IsFalse(a.TryPush(1, 2f, 1.0f));           // the first is still cooling down
            Assert.IsTrue(a.TryPush(1, 2f, 1.5f));            // and pushed again after the cooldown
        }

        [Test] public void OutsideTheRadius_NeverPushed_AndDoesNotUseUpTheCooldown()
        {
            var a = new PushAura();
            Assert.IsFalse(a.TryPush(1, 4.5f, 0f));
            Assert.IsTrue(a.TryPush(1, 3.9f, 0.1f));          // stepping in right after is pushed straight away
            Assert.IsTrue(a.TryPush(3, PushAura.Radius, 0f));  // exactly at the radius counts
        }
    }
}
