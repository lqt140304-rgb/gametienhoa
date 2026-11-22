using System.Linq;
using Evolve.Combat;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    public class PhaseRulesTests
    {
        [Test] public void Phases_ChangeAt66And33Percent()
        {
            Assert.AreEqual(1, PhaseRules.PhaseOf(1f)); Assert.AreEqual(1, PhaseRules.PhaseOf(0.67f));
            Assert.AreEqual(2, PhaseRules.PhaseOf(0.66f)); Assert.AreEqual(2, PhaseRules.PhaseOf(0.34f));
            Assert.AreEqual(3, PhaseRules.PhaseOf(0.33f)); Assert.AreEqual(3, PhaseRules.PhaseOf(0.01f));
        }

        [Test] public void Tracker_AnnouncesEachNewPhaseOnce_AndNeverGoesBack_EvenIfTheBossMends()
        {
            var t = new PhaseTracker();
            Assert.IsFalse(t.Update(0.9f)); Assert.AreEqual(1, t.Phase);
            Assert.IsTrue(t.Update(0.6f)); Assert.AreEqual(2, t.Phase); Assert.IsTrue(t.Pending);
            Assert.IsFalse(t.Update(0.55f));                                   // the same phase is not announced twice
            t.Ack(); Assert.IsFalse(t.Pending);
            Assert.IsFalse(t.Update(0.9f)); Assert.AreEqual(2, t.Phase);       // it mended back above 66%: still phase 2
            Assert.IsTrue(t.Update(0.1f)); Assert.AreEqual(3, t.Phase);        // a jump past a phase goes straight to the last
            Assert.IsTrue(new PhaseTracker().Update(0.2f));
        }

        [Test] public void OnlyTheKittedBossesHavePhases_AndEachPhaseIsHarderThanTheLast()
        {
            foreach (var n in new[] { "Golem Thạch Anh", "Chim Hang Mẹ", "Chó Xương" })
            {
                Assert.IsTrue(PhaseRules.Has(n), n);
                var a = PhaseRules.For(n, 1); var b = PhaseRules.For(n, 2); var c = PhaseRules.For(n, 3);
                Assert.AreEqual(1f, a.Cooldown); Assert.AreEqual(1f, a.Speed); Assert.AreEqual(1f, a.Damage); Assert.AreEqual(0, a.ExtraSpikes + a.ComboBonus);   // phase 1 is the boss as it was
                Assert.LessOrEqual(b.Cooldown, a.Cooldown); Assert.LessOrEqual(c.Cooldown, b.Cooldown); Assert.Less(c.Cooldown, 0.8f);
                Assert.GreaterOrEqual(b.Damage, a.Damage); Assert.GreaterOrEqual(c.Damage, b.Damage); Assert.Greater(c.Damage, 1f);
                Assert.GreaterOrEqual(c.Speed, b.Speed); Assert.Greater(c.Speed, 1f);
                Assert.LessOrEqual(c.Idle, b.Idle);                           // later it circles less and presses more
                Assert.LessOrEqual(c.Damage, 1.3f); Assert.LessOrEqual(c.Speed, 1.3f);   // but never absurdly
            }
            Assert.IsFalse(PhaseRules.Has("Cáo Gió")); Assert.IsNull(PhaseRules.TransitionMove("Cáo Gió"));
            Assert.AreEqual(1f, PhaseRules.For("Cáo Gió", 3).Cooldown);
        }

        [Test] public void Golem_GetsMoreSpikesAndLongerCombosAsItWeakens()
        {
            Assert.AreEqual(0, PhaseRules.For("Golem Thạch Anh", 1).ExtraSpikes);
            Assert.GreaterOrEqual(PhaseRules.For("Golem Thạch Anh", 2).ExtraSpikes, 1); Assert.Greater(PhaseRules.For("Golem Thạch Anh", 3).ExtraSpikes, PhaseRules.For("Golem Thạch Anh", 2).ExtraSpikes);
            Assert.Greater(PhaseRules.For("Golem Thạch Anh", 3).ComboBonus, PhaseRules.For("Golem Thạch Anh", 1).ComboBonus);
        }

        [Test] public void TheTransitionMove_IsARoarTheBossCannotBeHurtDuring_AndNotInItsMoveSet()
        {
            foreach (var n in new[] { "Golem Thạch Anh", "Chim Hang Mẹ", "Chó Xương" })
            {
                var m = PhaseRules.TransitionMove(n);
                Assert.IsTrue(m.Invulnerable); Assert.IsTrue(m.Armored); Assert.AreEqual("PhaseRoar", m.Anim); Assert.AreEqual(MoveShape.Spin, m.Shape);
                Assert.Greater(m.Force, 5f); Assert.Greater(m.Radius, 5f); Assert.Less(m.Damage, 10f);   // a shove, not an attack
                Assert.IsFalse(MovesetSeed.For(n).Any(x => x.Anim == "PhaseRoar"));
            }
        }
    }
}
