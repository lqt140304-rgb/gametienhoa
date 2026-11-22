using Evolve.Combat;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    public class AxeRulesTests
    {
        static AxeFsm Armed()   // pulled out, then thrown and landed: the golem is bare-handed with the axe stuck on the ground
        {
            var a = new AxeFsm(); a.Pulled(); a.Thrown(); a.Landed(true); return a;
        }

        [Test] public void StartsStuckInTheRoom_BareHanded()
        {
            var a = new AxeFsm();
            Assert.AreEqual(AxePlace.Anchor, a.Place); Assert.AreEqual(MoveForm.Fist, a.Form); Assert.IsFalse(a.EverArmed);
        }

        [Test] public void Fetches_AtHalfHealthWhenIdle()
        {
            var a = new AxeFsm();
            Assert.IsTrue(a.ShouldFetch(0.5f, true, false)); Assert.IsFalse(a.ShouldFetch(0.51f, true, false)); Assert.IsFalse(a.ShouldFetch(0.4f, false, false));
        }

        [Test] public void WaitsForThePhaseRoar_ThenGoes()
        {
            var a = new AxeFsm();
            Assert.IsFalse(a.ShouldFetch(0.4f, true, true)); Assert.IsTrue(a.ShouldFetch(0.4f, true, false));
        }

        [Test] public void PullingItOutArmsTheGolem_AndOnlyThenIsTheFormAxe()
        {
            var a = new AxeFsm();
            Assert.AreEqual(MoveForm.Fist, a.Form); a.Pulled();
            Assert.AreEqual(AxePlace.Held, a.Place); Assert.AreEqual(MoveForm.Axe, a.Form); Assert.IsTrue(a.EverArmed);
            Assert.IsFalse(a.ShouldFetch(0.1f, true, false));   // it holds it: nothing to fetch
            a.Thrown(); Assert.AreEqual(AxePlace.Flying, a.Place); Assert.AreEqual(MoveForm.Fist, a.Form);
        }

        [Test] public void AfterAThrow_WaitsOneAndAHalfSecondsBareHanded_BeforeFetching()
        {
            var a = Armed();
            Assert.AreEqual(AxePlace.Ground, a.Place); Assert.IsFalse(a.ShouldFetch(0.4f, true, false));
            a.Tick(1.4f); Assert.IsFalse(a.ShouldFetch(0.4f, true, false));
            a.Tick(0.2f); Assert.IsTrue(a.ShouldFetch(0.4f, true, false));
        }

        [Test] public void AnAxeThatLandsOutOfReach_DropsAtTheGolemsFeet()
        {
            var a = new AxeFsm(); a.Pulled(); a.Thrown();
            Assert.AreEqual(AxeLanding.AtGolem, a.Landed(false));
            var b = new AxeFsm(); b.Pulled(); b.Thrown(); Assert.AreEqual(AxeLanding.AtImpact, b.Landed(true));
        }

        [Test] public void StaysInTheAxeFamily_EvenIfTheGolemMendsAboveHalf()
        {
            var a = Armed(); a.Tick(2f);
            Assert.IsTrue(a.ShouldFetch(0.9f, true, false));   // it still goes to get its axe back at 90% health
            Assert.IsTrue(a.EverArmed);
        }

        [Test] public void NothingHappensOutOfOrder()
        {
            var a = new AxeFsm();
            a.Thrown(); Assert.AreEqual(AxePlace.Anchor, a.Place);   // cannot throw what it does not hold
            Assert.AreEqual(AxeLanding.AtImpact, a.Landed(true)); Assert.AreEqual(AxePlace.Anchor, a.Place);   // nothing was flying
        }
    }
}
