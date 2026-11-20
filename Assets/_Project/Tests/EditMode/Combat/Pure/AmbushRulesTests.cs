using Evolve.Combat;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    public class AmbushRulesTests
    {
        [Test] public void PushSpeed_ByMass()
        {
            Assert.AreEqual(6f, WindRules.PushSpeed(6f, 5f, false), 1e-4f);
            Assert.AreEqual(6f, WindRules.PushSpeed(6f, 10f, false), 1e-4f);
            Assert.AreEqual(4.5f, WindRules.PushSpeed(6f, 25f, false), 1e-4f);
            Assert.AreEqual(3f, WindRules.PushSpeed(6f, 40f, false), 1e-4f);
            Assert.AreEqual(3f, WindRules.PushSpeed(6f, 80f, false), 1e-4f);
            Assert.AreEqual(0f, WindRules.PushSpeed(6f, 10f, true), 1e-4f);
        }

        [Test] public void Ambush_StartsLurking_AndStaysHiddenBeyondFiveMetres()
        {
            var a = new AmbushRules();
            Assert.AreEqual(AmbushState.Lurking, a.State);
            Assert.IsFalse(a.Update(0.1f, 6f)); Assert.AreEqual(AmbushState.Lurking, a.State);
        }

        [Test] public void Ambush_RevealsOnceWithinFive()
        {
            var a = new AmbushRules();
            Assert.IsTrue(a.Update(0.1f, 4f)); Assert.AreEqual(AmbushState.Revealed, a.State);
            Assert.IsFalse(a.Update(0.1f, 4f));
        }

        [Test] public void Ambush_AfterTheStrike_RoamsThenLurksAgainAfterTwentySeconds()
        {
            var a = new AmbushRules(); a.Update(0.1f, 4f); a.StrikeDone();
            Assert.AreEqual(AmbushState.Roaming, a.State);
            for (int i = 0; i < 190; i++) a.Update(0.1f, float.MaxValue);
            Assert.AreEqual(AmbushState.Roaming, a.State);   // 19 s
            for (int i = 0; i < 12; i++) a.Update(0.1f, float.MaxValue);
            Assert.AreEqual(AmbushState.Lurking, a.State);
        }

        [Test] public void Ambush_PlayerNearResetsTheLurkTimer()
        {
            var a = new AmbushRules(); a.Update(0.1f, 4f); a.StrikeDone();
            for (int round = 0; round < 5; round++)
            {
                for (int i = 0; i < 100; i++) a.Update(0.1f, 30f);   // 10 s of nobody
                a.Update(0.1f, 10f);                                  // the player passes within 15 m
            }
            Assert.AreEqual(AmbushState.Roaming, a.State);
        }

        [Test] public void Ambush_RoamingPlayerWithinFiveDoesNotRevealAgain()
        {
            var a = new AmbushRules(); a.Update(0.1f, 4f); a.StrikeDone();
            Assert.IsFalse(a.Update(0.1f, 3f)); Assert.AreEqual(AmbushState.Roaming, a.State);
        }

        [Test] public void Reset_GoesBackToLurking()
        {
            var a = new AmbushRules(); a.Update(0.1f, 4f); a.Reset();
            Assert.AreEqual(AmbushState.Lurking, a.State);
        }
    }
}
