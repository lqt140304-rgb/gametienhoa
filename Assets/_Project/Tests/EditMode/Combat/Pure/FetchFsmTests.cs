using Evolve.Combat;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    public class FetchFsmTests
    {
        [Test] public void WalksThenPullsThenIsDone()
        {
            var f = new FetchFsm(3.5f);
            Assert.AreEqual(FetchStage.Walk, f.Stage);
            Assert.AreEqual(FetchEvent.None, f.Update(0.2f, false)); Assert.AreEqual(FetchStage.Walk, f.Stage);
            Assert.AreEqual(FetchEvent.PullStart, f.Update(0.1f, true)); Assert.AreEqual(FetchStage.Pull, f.Stage);   // it arrived: the pull begins
            Assert.AreEqual(FetchEvent.None, f.Update(3.3f, true)); Assert.AreEqual(FetchStage.Pull, f.Stage);          // still straining
            Assert.AreEqual(FetchEvent.Done, f.Update(0.3f, true)); Assert.AreEqual(FetchStage.Done, f.Stage);
        }

        [Test] public void GivesUpWalkingAfterTheTimeout_AndPullsWhereItStands()
        {
            var f = new FetchFsm(3.5f);           // the player vanished or the axe is blocked: it never "arrives"
            FetchEvent e = FetchEvent.None; float t = 0f;
            while (e != FetchEvent.PullStart && t < 10f) { e = f.Update(0.1f, false); t += 0.1f; }
            Assert.AreEqual(FetchEvent.PullStart, e); Assert.AreEqual(FetchFsm.WalkTimeout, t, 0.15f);
        }

        [Test] public void AbortEndsAtOnce_AndNothingHappensAfterwards()
        {
            var f = new FetchFsm(3.5f);
            f.Abort(); Assert.AreEqual(FetchStage.Done, f.Stage);
            Assert.AreEqual(FetchEvent.None, f.Update(5f, true));
        }

        [Test] public void AfterDone_NoMoreEvents()
        {
            var f = new FetchFsm(1f);
            f.Update(0.1f, true); Assert.AreEqual(FetchEvent.Done, f.Update(1.1f, true));
            Assert.AreEqual(FetchEvent.None, f.Update(1f, true)); Assert.AreEqual(FetchStage.Done, f.Stage);
        }

        [Test] public void PullProgress_IsTheShareOfThePullDone()
        {
            var f = new FetchFsm(4f);
            Assert.AreEqual(0f, f.PullProgress01, 1e-4f);
            f.Update(0.1f, true); f.Update(2f, true); Assert.AreEqual(0.5f, f.PullProgress01, 0.02f);
            f.Update(2.1f, true); Assert.AreEqual(1f, f.PullProgress01, 1e-4f);
        }
    }
}
