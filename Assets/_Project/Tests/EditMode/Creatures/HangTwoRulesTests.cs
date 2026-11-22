using Evolve.Creatures;
using Evolve.Combat;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    public class HangTwoRulesTests
    {
        [TestCase(0f, true)]
        [TestCase(9.99f, true)]
        [TestCase(10f, false)]
        public void GlowHasTenSecondWindow(float elapsed, bool expected) =>
            Assert.That(HangTwoRules.GlowActive(elapsed), Is.EqualTo(expected));

        [TestCase(19.99f, false)]
        [TestCase(20f, true)]
        public void GlowCooldown(float now, bool expected) =>
            Assert.That(HangTwoRules.Ready(now, 0f, HangTwoRules.GlowCooldown), Is.EqualTo(expected));

        [TestCase(0f, true)]
        [TestCase(0.349f, true)]
        [TestCase(0.35f, false)]
        public void DodgeWindow(float elapsed, bool expected) =>
            Assert.That(HangTwoRules.Dodging(elapsed), Is.EqualTo(expected));

        [Test]
        public void DodgeCooldownAndDistance()
        {
            Assert.That(HangTwoRules.Ready(5.99f, 0f, HangTwoRules.DodgeCooldown), Is.False);
            Assert.That(HangTwoRules.Ready(6f, 0f, HangTwoRules.DodgeCooldown), Is.True);
            Assert.That(HangTwoRules.DodgeDistance, Is.EqualTo(2f));
            Assert.That(HangTwoRules.DodgeProgress(0f), Is.Zero);
            Assert.That(HangTwoRules.DodgeProgress(0.35f), Is.EqualTo(1f));
        }

        [TestCase(1f)]
        [TestCase(0.2f)]
        public void OreShellAlwaysReducesDamage(float health)
        {
            Assert.That(PassiveRules.Has("BoGiapQuang"), Is.True);
            Assert.That(PassiveRules.DamageFactor("BoGiapQuang", health), Is.EqualTo(0.85f));
        }

        [Test]
        public void OtherSpeciesRetainTheirPassive()
        {
            Assert.That(PassiveRules.DamageFactor("GolemThachAnh", 1f), Is.EqualTo(0.8f));
            Assert.That(PassiveRules.DamageFactor("CocHoGia", 1f), Is.EqualTo(1f));
        }
    }
}
