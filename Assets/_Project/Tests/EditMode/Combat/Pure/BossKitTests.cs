using System.Linq;
using Evolve.Combat;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    public class BossKitTests
    {
        // every boss that has been rebuilt to the standard kit; add a species here when its kit is done
        static readonly string[] Kitted = { "Chó Xương", "Golem Thạch Anh" };

        [Test] public void EveryKittedBoss_HasTheStandardKit_TwoNormalOneRangedOneSpecialWithAMiniGameOneUltimateAndAPassive()
        {
            foreach (var boss in Kitted)
            {
                var moves = MovesetSeed.For(boss);
                Assert.AreEqual(2, moves.Count(m => m.Role == MoveRole.Normal), boss + " normal");
                int ranged = moves.Count(m => m.Role == MoveRole.Ranged);
                Assert.IsTrue(ranged == 1 || (ranged == 0 && moves.Single(m => m.Role == MoveRole.Special).SpikeCount > 0), boss + " ranged: one ranged skill, or a special that throws a volley of rocks from afar");
                Assert.AreEqual(1, moves.Count(m => m.Role == MoveRole.Special), boss + " special");
                Assert.AreEqual(1, moves.Count(m => m.Role == MoveRole.Ultimate), boss + " ultimate");
                Assert.LessOrEqual(moves.Count(m => m.Role == MoveRole.Combo), 1, boss + ": at most one extra combo skill (a charge or other connector)");
                Assert.AreEqual(4 + ranged + moves.Count(m => m.Role == MoveRole.Combo), moves.Count, boss + " total");
                var special = moves.Single(m => m.Role == MoveRole.Special);
                Assert.IsTrue(special.Pins || special.Dodge, boss + ": the special skill is a challenge: it holds the player in a mini game (Pins) or sets a dodge puzzle (Dodge)");
                Assert.LessOrEqual(moves.Single(m => m.Role == MoveRole.Ultimate).UnlockBelow, 0.25f + 1e-4f, boss + ": the ultimate comes near the end of its health");
                Assert.IsTrue(moves.Where(m => m.Role != MoveRole.Ultimate).All(m => m.UnlockBelow >= 1f - 1e-4f), boss + ": the other four are always available");
                Assert.IsTrue(moves.Where(m => m.Role == MoveRole.Ranged).All(m => m.MinRange >= 5f), boss + ": the ranged skill is for distance");
                Assert.IsTrue(PassiveRules.Has(boss), boss + " has a passive");
            }
        }

        [Test] public void GolemPassive_TakesLessDamage_AndMoreOnceItsCrystalsGrow()
        {
            Assert.AreEqual(0.8f, PassiveRules.DamageFactor("Golem Thạch Anh", 1f), 1e-4f);
            Assert.AreEqual(0.8f, PassiveRules.DamageFactor("Golem Thạch Anh", 0.7f), 1e-4f);
            Assert.AreEqual(0.65f, PassiveRules.DamageFactor("Golem Thạch Anh", 0.66f), 1e-4f);
            Assert.AreEqual(0.65f, PassiveRules.DamageFactor("GolemThachAnh (Meshy)(Clone)", 0.2f), 1e-4f);
            Assert.IsTrue(PassiveRules.CrystalsOut("Golem Thạch Anh", 0.5f)); Assert.IsFalse(PassiveRules.CrystalsOut("Golem Thạch Anh", 0.9f));
        }

        [Test] public void GolemPassive_MendsItselfAfterAQuietSpell_ButNotWhileFighting()
        {
            Assert.AreEqual(0f, PassiveRules.RegenPerSecond("Golem Thạch Anh", 3f));
            Assert.AreEqual(0f, PassiveRules.RegenPerSecond("Golem Thạch Anh", 7.9f));
            Assert.AreEqual(0.01f, PassiveRules.RegenPerSecond("Golem Thạch Anh", 8f), 1e-5f);
            Assert.AreEqual(0f, PassiveRules.RegenPerSecond("Chó Xương", 30f));
        }

        [Test] public void HoundPassive_BloodLust_RunsFasterWhenHurt()
        {
            Assert.AreEqual(1f, PassiveRules.SpeedFactor("Chó Xương", 1f)); Assert.AreEqual(1f, PassiveRules.SpeedFactor("Chó Xương", 0.51f));
            Assert.AreEqual(1.25f, PassiveRules.SpeedFactor("Chó Xương", 0.5f), 1e-4f); Assert.AreEqual(1.25f, PassiveRules.SpeedFactor("ChoXuong (Meshy)", 0.1f), 1e-4f);
            Assert.AreEqual(1f, PassiveRules.SpeedFactor("Golem Thạch Anh", 0.1f));
            Assert.AreEqual(1f, PassiveRules.DamageFactor("Chó Xương", 0.1f));
        }

        [Test] public void NoPassive_ForOtherCreatures()
        {
            Assert.IsFalse(PassiveRules.Has("Ốc Hồ")); Assert.AreEqual(1f, PassiveRules.DamageFactor("Ốc Hồ", 0.1f)); Assert.AreEqual(1f, PassiveRules.SpeedFactor(null, 0.1f)); Assert.AreEqual(0f, PassiveRules.RegenPerSecond("xyz", 99f));
        }

        [Test] public void GrabPin_NeedsMashing_NotAlternating()
        {
            var g = new PinGame(PinMode.Mash);
            for (int i = 0; i < 40 && !g.Escaped; i++) { g.Press(PinKey.A); g.Tick(0.15f); }   // the same key again and again, about 6.5 presses a second
            Assert.IsTrue(g.Escaped);
        }

        [Test] public void GrabPin_TheBarDrainsFasterThanABite_AndTheSqueezeHurtsMore()
        {
            var bite = new PinGame(); var grab = new PinGame(PinMode.Mash);
            Assert.Greater(grab.DamageRate, bite.DamageRate);
            Assert.AreEqual(PinMode.Mash, grab.Mode); Assert.AreEqual(PinMode.AlternateAD, bite.Mode);
            bite.Press(PinKey.A); bite.Press(PinKey.D); grab.Press(PinKey.A); grab.Press(PinKey.A);
            bite.Tick(1f); grab.Tick(1f);
            Assert.Less(grab.Progress, bite.Progress + 1e-4f + 0.0f);   // two taps lasted less against the grab's heavier drain
        }

        [Test] public void Gait_TwoLegsMoveInOppositePhases_AndTheKneeBendsOnlyWhileTheLegSwingsForward()
        {
            for (float p = 0f; p < 1f; p += 0.05f)
            {
                Assert.AreEqual(-GaitRules.Hip(p, 0, 25f), GaitRules.Hip(p, 1, 25f), 1e-3f);
                Assert.That(GaitRules.Knee(p, 0, 40f), Is.InRange(0f, 40f));
            }
            Assert.AreEqual(25f, GaitRules.Hip(0.25f, 0, 25f), 1e-3f);          // the left foot is farthest forward at a quarter of the cycle: it lands
            Assert.AreEqual(25f, GaitRules.Hip(0.75f, 1, 25f), 1e-3f);          // and the right foot at three quarters
            Assert.AreEqual(0f, GaitRules.Knee(0.75f, 0, 40f), 1e-3f);           // a planted leg is straight
            Assert.Greater(GaitRules.Knee(0.0f, 0, 40f), 39f);                    // mid swing, the knee is at its most bent
        }

        [Test] public void Gait_PhaseFollowsTheDistanceTravelled_SoTheFeetNeverSlide()
        {
            Assert.AreEqual(0.5f, GaitRules.PhaseAdvance(0.95f, 1.9f), 1e-4f);
            Assert.AreEqual(0f, GaitRules.PhaseAdvance(0f, 1.9f)); Assert.AreEqual(0f, GaitRules.PhaseAdvance(1f, 0f));
        }

        [Test] public void Gait_TheBodySinksAtEachFootfall_TwiceACycle()
        {
            Assert.Less(GaitRules.Bob(0.25f, 0.05f), GaitRules.Bob(0.0f, 0.05f)); Assert.AreEqual(GaitRules.Bob(0.25f, 0.05f), GaitRules.Bob(0.75f, 0.05f), 1e-5f);
            Assert.AreEqual(0f, GaitRules.Bob(0.0f, 0.05f), 1e-5f);
        }
    }
}
