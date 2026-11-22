using Evolve.Combat;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    public class HoundRulesTests
    {
        [Test] public void Idle_IsRest() => Assert.AreEqual(0f, HoundRules.Evaluate("Roar", MovePhase.Idle, 0.5f).NeckPitch);

        [Test] public void Roar_ThrowsTheHeadBackAndOpensTheJaw_AndQuiversInTheStrike()
        {
            var peak = HoundRules.Evaluate("Roar", MovePhase.Active, 0.5f);
            Assert.Greater(peak.NeckPitch, 30f); Assert.Greater(peak.JawDeg, 20f); Assert.Greater(peak.Quiver, 0.4f);
            var halfway = HoundRules.Evaluate("Roar", MovePhase.Windup, 0.5f);
            Assert.Greater(halfway.NeckPitch, 0f); Assert.Less(halfway.NeckPitch, peak.NeckPitch);
            Assert.AreEqual(0f, HoundRules.Evaluate("Roar", MovePhase.Recover, 1f).NeckPitch, 1e-4f);
        }

        [Test] public void Bite_OpensWideThenSnapsShut()
        {
            Assert.AreEqual(HoundRules.MaxJawDeg, HoundRules.Evaluate("Bite", MovePhase.Windup, 1f).JawDeg, 1e-3f);
            Assert.AreEqual(0f, HoundRules.Evaluate("Bite", MovePhase.Active, 0.5f).JawDeg, 1e-3f);   // already shut by the middle of the strike
            Assert.Greater(HoundRules.Evaluate("Bite", MovePhase.Active, 0.05f).JawDeg, 15f);
        }

        [Test] public void Pounce_LowersTheHeadInTheTell_AndLiftsItInTheLeap()
        {
            Assert.Less(HoundRules.Evaluate("Pounce", MovePhase.Windup, 1f).NeckPitch, -10f);
            Assert.Greater(HoundRules.Evaluate("Pounce", MovePhase.Active, 1f).NeckPitch, 5f);
        }

        [Test] public void JawNeverExceedsTheLimit_ForAnyMove()
        {
            foreach (var a in new[] { "Pounce", "Bite", "Roar", "Howl", "Leap", "xyz" })
                foreach (var ph in new[] { MovePhase.Windup, MovePhase.Active, MovePhase.Recover })
                    for (float t = 0f; t <= 1f; t += 0.1f) Assert.LessOrEqual(HoundRules.Evaluate(a, ph, t).JawDeg, HoundRules.MaxJawDeg + 1e-3f, a + ph);
        }

        [Test] public void Flinch_DecaysToZero_AndAHeavyOneLastsLonger()
        {
            Assert.AreEqual(1f, HoundRules.Flinch(0f, false), 1e-4f);
            Assert.AreEqual(0f, HoundRules.Flinch(HoundRules.FlinchSeconds, false)); Assert.AreEqual(0f, HoundRules.Flinch(-1f, false));
            Assert.AreEqual(0f, HoundRules.Flinch(0.3f, false)); Assert.Greater(HoundRules.Flinch(0.3f, true), 0f);
            Assert.Less(HoundRules.Flinch(0.1f, false), HoundRules.Flinch(0.05f, false));
        }

        [Test] public void HeavyHit_IsAtLeastTwelvePercentOfMaxHealth()
        {
            Assert.IsTrue(HoundRules.IsHeavy(24f, 190f)); Assert.IsFalse(HoundRules.IsHeavy(10f, 190f)); Assert.IsFalse(HoundRules.IsHeavy(5f, 0f));
        }

        [Test] public void Claw_RaisesAForepawThenRakesItDown()
        {
            Assert.Greater(HoundRules.Evaluate("Claw", MovePhase.Windup, 1f).PawDeg, 40f);
            Assert.Less(HoundRules.Evaluate("Claw", MovePhase.Active, 1f).PawDeg, 0f);
            Assert.AreEqual(0f, HoundRules.Evaluate("Claw", MovePhase.Recover, 1f).PawDeg, 1e-4f);
            Assert.AreEqual(0f, HoundRules.Evaluate("Bite", MovePhase.Windup, 1f).PawDeg);
        }

        [Test] public void Pounce_StretchesTheLegsOut_FrontForwardAndHindBack_OnlyWhileLeaping()
        {
            Assert.Greater(HoundRules.LegSwingDeg("Pounce", MovePhase.Active, false, true), 40f);
            Assert.Less(HoundRules.LegSwingDeg("Pounce", MovePhase.Active, false, false), -40f);
            Assert.AreEqual(0f, HoundRules.LegSwingDeg("Pounce", MovePhase.Windup, false, true));
            Assert.AreEqual(0f, HoundRules.LegSwingDeg("Bite", MovePhase.Active, false, true));
        }

        [Test] public void WhenPinning_TheFrontPawsReachOutOverTheCatch()
        {
            Assert.Greater(HoundRules.LegSwingDeg(null, MovePhase.Idle, true, true), 30f);
            Assert.AreEqual(0f, HoundRules.LegSwingDeg(null, MovePhase.Idle, true, false));
        }

        [Test] public void Faint_RollsOntoItsSide_HoldsAndGetsBackUp()
        {
            Assert.AreEqual(0f, HoundRules.FaintAngle(0f, 2f), 1e-3f);
            Assert.Greater(HoundRules.FaintAngle(0.3f, 2f), 70f);
            Assert.AreEqual(HoundRules.FaintRollDeg, HoundRules.FaintAngle(1f, 2f), 1e-3f);
            Assert.Less(HoundRules.FaintAngle(1.9f, 2f), HoundRules.FaintRollDeg);
            Assert.AreEqual(0f, HoundRules.FaintAngle(2f, 2f), 1e-3f); Assert.AreEqual(0f, HoundRules.FaintAngle(-1f, 2f));
        }

        [Test] public void LungeDistance_ReachesTheTarget_NotMore_AndNeverBeyondTheMoveLimit()
        {
            Assert.AreEqual(4.5f, HoundRules.LungeDistance(4f, 5.5f), 1e-4f);   // the target 4 m away: 4 m plus a little
            Assert.AreEqual(5.5f, HoundRules.LungeDistance(9f, 5.5f), 1e-4f);   // far: the most the move can cover
            Assert.AreEqual(1f, HoundRules.LungeDistance(0.2f, 5.5f), 1e-4f);  // close: still a real hop
        }

        [Test] public void Spit_ThrowsTheHeadBackThenSnapsItForward()
        {
            Assert.Greater(HoundRules.Evaluate("Spit", MovePhase.Windup, 1f).NeckPitch, 30f);
            Assert.Greater(HoundRules.Evaluate("Spit", MovePhase.Windup, 1f).JawDeg, 15f);
            Assert.Less(HoundRules.Evaluate("Spit", MovePhase.Active, 1f).NeckPitch, 0f);
        }
    }
}
