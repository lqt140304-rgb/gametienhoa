using System.Linq;
using Evolve.Combat;
using Evolve.Creatures;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    public class GolemRulesTests
    {
        static MoveDef Move(string name) => MovesetSeed.For("Golem Thạch Anh").Single(m => m.Name == name);

        [Test] public void Golem_HasItsFourMoves_ByAnyName()
        {
            foreach (var n in new[] { "Golem Thạch Anh", "GolemThachAnh (Meshy)(Clone)", "golem_thach_anh" }) Assert.AreEqual(4, MovesetSeed.For(n).Count, n);
            Assert.IsTrue(MovesetSeed.Has("Golem Thạch Anh"));
            var slam = Move("Đập Đất"); var rocks = Move("Ném Đá"); var armor = Move("Giáp Tinh Thể"); var blast = Move("Lõi Nổ");
            Assert.AreEqual(MoveShape.Spin, slam.Shape); Assert.AreEqual(1.1f, slam.Windup); Assert.AreEqual(5f, slam.Radius); Assert.AreEqual(26f, slam.Damage); Assert.AreEqual(1f, slam.UnlockBelow); Assert.AreEqual(8f, slam.Cooldown);
            Assert.AreEqual(MoveShape.Throw, rocks.Shape); Assert.AreEqual(5f, rocks.MinRange); Assert.AreEqual(18f, rocks.MaxRange); Assert.AreEqual(16f, rocks.Damage); Assert.AreEqual(0.9f, rocks.FlightSeconds);
            Assert.AreEqual(MoveShape.Harden, armor.Shape); Assert.AreEqual(4f, armor.Active); Assert.AreEqual(0.35f, armor.DamageTakenFactor); Assert.AreEqual(0.66f, armor.UnlockBelow); Assert.AreEqual(18f, armor.Cooldown);
            Assert.AreEqual(MoveShape.Spin, blast.Shape); Assert.AreEqual(7f, blast.Radius); Assert.AreEqual(35f, blast.Damage); Assert.AreEqual(1.8f, blast.Windup); Assert.AreEqual(0.33f, blast.UnlockBelow);
        }

        [Test] public void Golem_PoseIsSoftened_BecauseTheArmsMoveByThemselves()
        {
            Assert.AreEqual(0.35f, MovesetSeed.PoseStrength("Golem Thạch Anh"));
            Assert.AreEqual(0.4f, MovesetSeed.PoseStrength("Chó Xương")); Assert.AreEqual(1f, MovesetSeed.PoseStrength("Cáo Gió"));
        }

        [Test] public void Hardened_TakesLessDamage_OnlyWhileTheArmourHolds()
        {
            var armor = Move("Giáp Tinh Thể");
            var s = new MoveScheduler(new[] { armor }, 1);
            Assert.AreEqual(1f, s.DamageTakenFactor);
            s.Update(0.02f, true, 3f, 0.5f);                       // winds up (health 50%: unlocked)
            Assert.AreEqual(1f, s.DamageTakenFactor);               // the armour is not on yet
            for (int i = 0; i < 200 && s.Phase != MovePhase.Active; i++) s.Update(0.02f, true, 3f, 0.5f);
            Assert.AreEqual(MovePhase.Active, s.Phase);
            Assert.AreEqual(0.35f, s.DamageTakenFactor, 1e-4f);
            Assert.IsTrue(s.IsArmored);
            for (int i = 0; i < 400 && s.Phase != MovePhase.Recover; i++) s.Update(0.02f, true, 3f, 0.5f);
            Assert.AreEqual(MoveScheduler.RecoverDamageFactor, s.DamageTakenFactor, 1e-4f);   // it is open to a counter once the armour drops
        }

        [Test] public void Slam_RaisesBothArmsOverTheHead_ThenDrivesThemDown()
        {
            var up = GolemRules.Evaluate("Slam", MovePhase.Windup, 1f); var down = GolemRules.Evaluate("Slam", MovePhase.Active, 1f);
            Assert.Greater(up.Raise, 120f); Assert.Less(up.Hunch, 0f);
            Assert.Less(down.Raise, 0f); Assert.Greater(down.Hunch, 15f);
            Assert.AreEqual(0f, GolemRules.Evaluate("Slam", MovePhase.Recover, 1f).Raise, 1e-3f);
        }

        [Test] public void Throw_CocksTheRightArmBackThenSwingsItThrough()
        {
            Assert.Greater(GolemRules.Evaluate("Throw", MovePhase.Windup, 1f).Back, 90f);
            Assert.Less(GolemRules.Evaluate("Throw", MovePhase.Active, 1f).Back, 0f);
            Assert.AreEqual(0f, GolemRules.Evaluate("Throw", MovePhase.Windup, 1f).Raise);
        }

        [Test] public void CoreBlast_ThrowsTheArmsWide_AndTheCoreGlowsMoreAndMore()
        {
            float g0 = GolemRules.Evaluate("CoreBlast", MovePhase.Windup, 0.2f).Glow, g1 = GolemRules.Evaluate("CoreBlast", MovePhase.Windup, 0.9f).Glow;
            Assert.Less(g0, g1); Assert.Greater(GolemRules.Evaluate("CoreBlast", MovePhase.Active, 0.5f).Glow, 0.9f);
            Assert.Greater(GolemRules.Evaluate("CoreBlast", MovePhase.Windup, 1f).Spread, 40f);
            Assert.AreEqual(0f, GolemRules.Evaluate("CoreBlast", MovePhase.Recover, 1f).Glow, 1e-3f);
        }

        [Test] public void Harden_HoldsTheGuardAndGlowsWhileItLasts()
        {
            var hold = GolemRules.Evaluate("Harden", MovePhase.Active, 0.5f);
            Assert.Greater(hold.Hunch, 10f); Assert.Greater(hold.Glow, 0.4f); Assert.Greater(hold.Raise, 20f);
        }

        [Test] public void IdleAndUnknownMoves_ArePlainRest()
        {
            Assert.AreEqual(0f, GolemRules.Evaluate("Slam", MovePhase.Idle, 0.5f).Raise); Assert.AreEqual(0f, GolemRules.Evaluate("xyz", MovePhase.Active, 0.5f).Glow);
        }

        [Test] public void NoPoseValue_EverLeavesItsLimits()
        {
            foreach (var a in new[] { "Slam", "Throw", "Harden", "CoreBlast" })
                foreach (var ph in new[] { MovePhase.Windup, MovePhase.Active, MovePhase.Recover })
                    for (float t = 0f; t <= 1f; t += 0.1f)
                    {
                        var p = GolemRules.Evaluate(a, ph, t);
                        Assert.That(p.Glow, Is.InRange(0f, 1f), a); Assert.That(p.Raise, Is.InRange(-60f, 170f), a); Assert.That(p.Back, Is.InRange(-80f, 130f), a); Assert.That(p.Spread, Is.InRange(0f, 90f), a);
                    }
        }
    }
}
