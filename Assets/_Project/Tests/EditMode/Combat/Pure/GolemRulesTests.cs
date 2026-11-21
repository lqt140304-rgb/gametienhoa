using System.Linq;
using Evolve.Combat;
using Evolve.Creatures;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    public class GolemRulesTests
    {
        static MoveDef Move(string name) => MovesetSeed.For("Golem Thạch Anh").Single(m => m.Name == name);

        [Test] public void Golem_HasItsFiveMoves_ByAnyName()
        {
            foreach (var n in new[] { "Golem Thạch Anh", "GolemThachAnh (Meshy)(Clone)", "golem_thach_anh" }) Assert.AreEqual(5, MovesetSeed.For(n).Count, n);
            Assert.IsTrue(MovesetSeed.Has("Golem Thạch Anh"));
            var punch = Move("Nện Đấm"); var slam = Move("Đập Đất"); var rocks = Move("Ném Đá"); var grab = Move("Chộp Nghiền"); var blast = Move("Lõi Nổ");
            // heavy: slow tells, long recoveries, hard knock-back and a short stun
            Assert.AreEqual(MoveShape.Lunge, punch.Shape); Assert.AreEqual(0.9f, punch.Windup); Assert.AreEqual(3.5f, punch.MaxRange); Assert.AreEqual(28f, punch.Damage); Assert.AreEqual(6f, punch.Force);
            Assert.AreEqual(MoveShape.Spin, slam.Shape); Assert.AreEqual(1.5f, slam.Windup); Assert.AreEqual(2.4f, slam.Recover); Assert.AreEqual(5.5f, slam.Radius); Assert.AreEqual(32f, slam.Damage); Assert.AreEqual(9f, slam.Force); Assert.AreEqual(0.6f, slam.Freeze); Assert.AreEqual(9f, slam.Cooldown);
            Assert.AreEqual(MoveShape.Throw, rocks.Shape); Assert.AreEqual(1.3f, rocks.Windup); Assert.AreEqual(5f, rocks.MinRange); Assert.AreEqual(18f, rocks.MaxRange); Assert.AreEqual(20f, rocks.Damage); Assert.AreEqual(1.1f, rocks.FlightSeconds); Assert.AreEqual(1f, rocks.MissileSize); Assert.AreEqual(8f, rocks.Cooldown);
            Assert.IsTrue(grab.Pins); Assert.IsTrue(grab.Grab); Assert.AreEqual(3f, grab.MinRange); Assert.AreEqual(7f, grab.MaxRange); Assert.AreEqual(9f, grab.AbortBeyond); Assert.AreEqual(1.1f, grab.Windup); Assert.AreEqual(10f, grab.Cooldown);
            Assert.AreEqual(MoveShape.Spin, blast.Shape); Assert.AreEqual(2.4f, blast.Windup); Assert.AreEqual(2.6f, blast.Recover); Assert.AreEqual(8f, blast.Radius); Assert.AreEqual(40f, blast.Damage); Assert.AreEqual(12f, blast.Force); Assert.AreEqual(0.8f, blast.Freeze); Assert.AreEqual(0.25f, blast.UnlockBelow); Assert.AreEqual(40f, blast.Cooldown);
        }

        [Test] public void Golem_PoseIsSoftened_BecauseTheArmsMoveByThemselves()
        {
            Assert.AreEqual(0.35f, MovesetSeed.PoseStrength("Golem Thạch Anh"));
            Assert.AreEqual(0.4f, MovesetSeed.PoseStrength("Chó Xương")); Assert.AreEqual(1f, MovesetSeed.PoseStrength("Cáo Gió"));
        }

        [Test] public void AHardenMove_TakesLessDamage_OnlyWhileTheArmourHolds()
        {
            var armor = new MoveDef { Name = "Giáp", Shape = MoveShape.Harden, Windup = 0.8f, Active = 4f, Recover = 1f, Cooldown = 18f, MinRange = 0f, MaxRange = 30f, DamageTakenFactor = 0.35f, Anim = "Harden" };
            var s = new MoveScheduler(new[] { armor }, 1);
            Assert.AreEqual(1f, s.DamageTakenFactor);
            s.Update(0.02f, true, 3f, 0.5f);
            Assert.AreEqual(1f, s.DamageTakenFactor);
            for (int i = 0; i < 200 && s.Phase != MovePhase.Active; i++) s.Update(0.02f, true, 3f, 0.5f);
            Assert.AreEqual(MovePhase.Active, s.Phase); Assert.AreEqual(0.35f, s.DamageTakenFactor, 1e-4f); Assert.IsTrue(s.IsArmored);
            for (int i = 0; i < 400 && s.Phase != MovePhase.Recover; i++) s.Update(0.02f, true, 3f, 0.5f);
            Assert.AreEqual(MoveScheduler.RecoverDamageFactor, s.DamageTakenFactor, 1e-4f);
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
            foreach (var a in new[] { "Slam", "Throw", "Harden", "CoreBlast", "Punch", "Grab" })
                foreach (var ph in new[] { MovePhase.Windup, MovePhase.Active, MovePhase.Recover })
                    for (float t = 0f; t <= 1f; t += 0.1f)
                    {
                        var p = GolemRules.Evaluate(a, ph, t);
                        Assert.That(p.Glow, Is.InRange(0f, 1f), a); Assert.That(p.Raise, Is.InRange(-60f, 170f), a); Assert.That(p.Back, Is.InRange(-80f, 130f), a); Assert.That(p.Spread, Is.InRange(0f, 90f), a);
                    }
        }

        [Test] public void Punch_CocksTheRightArmBackThenDrivesItThrough_AndGrabReachesBothArmsOut()
        {
            Assert.Greater(GolemRules.Evaluate("Punch", MovePhase.Windup, 1f).Back, 100f); Assert.Less(GolemRules.Evaluate("Punch", MovePhase.Active, 1f).Back, 0f); Assert.Greater(GolemRules.Evaluate("Punch", MovePhase.Active, 1f).Hunch, 15f);
            Assert.Greater(GolemRules.Evaluate("Grab", MovePhase.Windup, 1f).Raise, 60f); Assert.Greater(GolemRules.Evaluate("Grab", MovePhase.Active, 1f).Hunch, 15f);
        }
    }
}
