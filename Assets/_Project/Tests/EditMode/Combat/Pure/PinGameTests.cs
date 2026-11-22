using Evolve.Combat;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    public class PinGameTests
    {
        [Test] public void AlternatingKeys_FillTheBarAndFreeThePlayer()
        {
            var g = new PinGame();
            for (int i = 0; i < 20 && !g.Escaped; i++) g.Press(i % 2 == 0 ? PinKey.A : PinKey.D);
            Assert.IsTrue(g.Escaped); Assert.IsTrue(g.Finished); Assert.IsFalse(g.TimedOut);
        }

        [Test] public void MashingOneKey_DoesNotHelp()
        {
            var g = new PinGame();
            g.Press(PinKey.A);
            float p = g.Progress;
            for (int i = 0; i < 30; i++) g.Press(PinKey.A);
            Assert.LessOrEqual(g.Progress, p + 1e-4f); Assert.IsFalse(g.Escaped);
        }

        [Test] public void TheBarDrains_AndDoesNotGoBelowZero()
        {
            var g = new PinGame();
            g.Press(PinKey.A); g.Press(PinKey.D);
            float p = g.Progress;
            g.Tick(0.5f);
            Assert.Less(g.Progress, p);
            for (int i = 0; i < 20; i++) g.Tick(0.4f);
            Assert.GreaterOrEqual(g.Progress, 0f);
        }

        [Test] public void EveryTickBitesForTheRateTimesTheTime_AndStopsOnceFinished()
        {
            var g = new PinGame();
            Assert.AreEqual(PinGame.DamagePerSecond * 0.5f, g.Tick(0.5f), 1e-4f);
            for (int i = 0; i < 20; i++) g.Press(i % 2 == 0 ? PinKey.A : PinKey.D);
            Assert.IsTrue(g.Escaped);
            Assert.AreEqual(0f, g.Tick(1f));
        }

        [Test] public void LettingGoOnItsOwn_AfterTheTimeout()
        {
            var g = new PinGame();
            float total = 0f;
            for (int i = 0; i < 200; i++) total += g.Tick(0.1f);
            Assert.IsTrue(g.TimedOut); Assert.IsFalse(g.Escaped);
            Assert.AreEqual(PinGame.DamagePerSecond * PinGame.MaxSeconds, total, PinGame.DamagePerSecond * 0.2f);
        }

        [Test] public void AMashingPlayer_BeatsTheDrain()
        {
            var g = new PinGame();
            int presses = 0;
            for (float t = 0f; t < PinGame.MaxSeconds && !g.Finished; t += 0.2f) { g.Press(presses++ % 2 == 0 ? PinKey.A : PinKey.D); g.Tick(0.2f); }   // five presses a second
            Assert.IsTrue(g.Escaped);
        }
    }
}
