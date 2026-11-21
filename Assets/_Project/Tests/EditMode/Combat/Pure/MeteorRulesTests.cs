using System;
using System.Linq;
using Evolve.Combat;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    public class MeteorRulesTests
    {
        [Test] public void Leap_CrouchesThenRisesToTheApex_HangsThenDropsFastToTheGround()
        {
            Assert.AreEqual(0f, LeapRules.Height(MovePhase.Windup, 0f)); Assert.AreEqual(0f, LeapRules.Height(MovePhase.Windup, 0.2f));
            float last = 0f;
            for (float p = 0.3f; p <= 0.75f; p += 0.05f) { float h = LeapRules.Height(MovePhase.Windup, p); Assert.GreaterOrEqual(h, last); last = h; }
            Assert.AreEqual(LeapRules.ApexMeters, LeapRules.Height(MovePhase.Windup, 1f), 1e-3f);        // it hangs at the top until the strike
            Assert.AreEqual(LeapRules.ApexMeters, LeapRules.Height(MovePhase.Active, 0f), 1e-3f);
            Assert.AreEqual(0f, LeapRules.Height(MovePhase.Active, 1f), 1e-3f);
            Assert.Less(LeapRules.Height(MovePhase.Active, 0.5f), LeapRules.ApexMeters * 0.5f);          // it speeds up as it falls: under half way at half the time
            Assert.AreEqual(0f, LeapRules.Height(MovePhase.Recover, 0.5f)); Assert.AreEqual(0f, LeapRules.Height(MovePhase.Idle, 0.5f));
        }

        [Test] public void Leap_FliesToTheMarkedSpotOnlyDuringTheFall()
        {
            Assert.AreEqual(0f, LeapRules.Travel(MovePhase.Windup, 1f)); Assert.AreEqual(0f, LeapRules.Travel(MovePhase.Active, 0f));
            Assert.AreEqual(1f, LeapRules.Travel(MovePhase.Active, 1f), 1e-4f); Assert.AreEqual(1f, LeapRules.Travel(MovePhase.Recover, 0f));
            Assert.That(LeapRules.Travel(MovePhase.Active, 0.5f), Is.InRange(0.3f, 0.7f));
        }

        [Test] public void SpikeRing_PutsTheSpikesEvenlyOnACircleAroundTheLandingSpot()
        {
            var ring = SpikeRules.Ring(10f, -4f, 4, 6.5f, 0f);
            Assert.AreEqual(4, ring.Length);
            foreach (var (x, z) in ring) Assert.AreEqual(6.5f, (float)Math.Sqrt((x - 10f) * (x - 10f) + (z + 4f) * (z + 4f)), 1e-3f);
            Assert.AreEqual(16.5f, ring[0].x, 1e-3f); Assert.AreEqual(-4f, ring[0].z, 1e-3f);           // angle 0: straight along +x
            float side = (float)Math.Sqrt(Math.Pow(ring[1].x - ring[0].x, 2) + Math.Pow(ring[1].z - ring[0].z, 2));
            for (int i = 0; i < 4; i++) { var a = ring[i]; var b = ring[(i + 1) % 4]; Assert.AreEqual(side, (float)Math.Sqrt(Math.Pow(b.x - a.x, 2) + Math.Pow(b.z - a.z, 2)), 1e-3f); }
            Assert.AreEqual(0, SpikeRules.Ring(0f, 0f, 0, 5f, 0f).Length);
        }

        [Test] public void Volley_GetsUp_Roars_ThenForEverySpikeWalksPullsThrowsAndReleases_ThenIsDone()
        {
            var v = new VolleyFsm(3);
            Assert.AreEqual(VolleyStage.Rise, v.Stage);   // it lands crouched and does not fetch a spike at once
            int roars = 0, starts = 0, picked = 0, threw = 0, order = 0;
            var seen = new System.Collections.Generic.List<VolleyStage>();
            for (int i = 0; i < 4000 && v.Stage != VolleyStage.Done; i++)
            {
                if (seen.Count == 0 || seen[seen.Count - 1] != v.Stage) seen.Add(v.Stage);
                var e = v.Update(0.05f, v.Stage == VolleyStage.Walk && v.StageTime > 0.3f);   // it arrives after 0.3 s of walking
                if (e == VolleyEvent.Roared) roars++;
                if (e == VolleyEvent.PullStart) starts++;
                if (e == VolleyEvent.Picked) { picked++; Assert.AreEqual(order, v.Index); }
                if (e == VolleyEvent.Threw) { threw++; order++; }
            }
            Assert.AreEqual(1, roars); Assert.AreEqual(3, starts); Assert.AreEqual(3, picked); Assert.AreEqual(3, threw); Assert.AreEqual(VolleyStage.Done, v.Stage);
            Assert.AreEqual(VolleyStage.Rise, seen[0]); Assert.AreEqual(VolleyStage.Roar, seen[1]); Assert.AreEqual(VolleyStage.Walk, seen[2]);   // the roar comes before any spike is touched
            Assert.AreEqual(VolleyEvent.None, v.Update(0.5f, true));
        }

        [Test] public void Volley_EachStageTakesItsTime_ASlowHeavyRhythm()
        {
            var v = new VolleyFsm(1);
            Assert.AreEqual(VolleyEvent.None, v.Update(VolleyFsm.RiseSeconds - 0.1f, true)); Assert.AreEqual(VolleyStage.Rise, v.Stage);   // still getting up even though "arrived"
            Assert.AreEqual(VolleyEvent.Roared, v.Update(0.2f, true)); Assert.AreEqual(VolleyStage.Roar, v.Stage);
            Assert.AreEqual(VolleyEvent.None, v.Update(VolleyFsm.RoarSeconds - 0.1f, true)); Assert.AreEqual(VolleyStage.Roar, v.Stage);
            v.Update(0.2f, true); Assert.AreEqual(VolleyStage.Walk, v.Stage);
            Assert.AreEqual(VolleyEvent.PullStart, v.Update(0.1f, true)); Assert.AreEqual(VolleyStage.Pull, v.Stage);
            Assert.AreEqual(VolleyEvent.None, v.Update(VolleyFsm.PullSeconds - 0.1f, true));        // pulling slowly
            Assert.AreEqual(VolleyEvent.Picked, v.Update(0.2f, true)); Assert.AreEqual(VolleyStage.Hold, v.Stage);
            Assert.AreEqual(VolleyEvent.None, v.Update(VolleyFsm.HoldSeconds - 0.1f, true));
            Assert.AreEqual(VolleyEvent.Threw, v.Update(0.2f, true)); Assert.AreEqual(VolleyStage.Release, v.Stage);   // the follow-through of the throw
            Assert.AreEqual(VolleyEvent.None, v.Update(VolleyFsm.ReleaseSeconds - 0.1f, true)); Assert.AreEqual(VolleyStage.Release, v.Stage);
            v.Update(0.2f, true); Assert.AreEqual(VolleyStage.Done, v.Stage);
        }

        [Test] public void Volley_GivesUpWalkingAfterATimeout_AndPullsWhereItStands()
        {
            var v = new VolleyFsm(2);
            v.Update(VolleyFsm.RiseSeconds + 0.01f, false); v.Update(VolleyFsm.RoarSeconds + 0.01f, false);
            VolleyEvent e = VolleyEvent.None; float t = 0f;
            while (e != VolleyEvent.PullStart && t < 6f) { e = v.Update(0.1f, false); t += 0.1f; }   // a wall in the way: it never arrives
            Assert.AreEqual(VolleyEvent.PullStart, e); Assert.AreEqual(VolleyFsm.WalkTimeout, t, 0.15f);
        }

        [Test] public void Meteor_HoldsItsRecoverForTheWholeVolley_AndThenRests()
        {
            var golem = MovesetSeed.For("Golem Thạch Anh").Single(m => m.Shape == MoveShape.Meteor);
            Assert.LessOrEqual(golem.Recover, 0.5f);                 // the real length comes from the volley (the scheduler is told to hold), not from a fixed time that the last rock could outlast
            Assert.GreaterOrEqual(golem.Rest, 4f);                   // and the big move leaves it winded for a good while
        }

        [Test] public void Meteor_HasARoarThatThrowsThePlayerBack()
        {
            var golem = MovesetSeed.For("Golem Thạch Anh").Single(m => m.Shape == MoveShape.Meteor);
            Assert.GreaterOrEqual(golem.RoarRadius, 10f); Assert.GreaterOrEqual(golem.RoarForce, 10f);
        }

        [Test] public void Pull_StrugglesInFourHeaves_ItDoesNotComeOutInOneStroke()
        {
            Assert.AreEqual(0f, PullRules.Lift(0f), 1e-4f); Assert.AreEqual(1f, PullRules.Lift(1f), 1e-4f);
            float last = 0f;
            for (float t = 0f; t <= 1.0001f; t += 0.01f) { float l = PullRules.Lift(t); Assert.GreaterOrEqual(l, last - 1e-5f); last = l; }   // it never slips back
            Assert.Less(PullRules.Lift(0.5f), 0.45f);                       // half the time in, it is still less than half out
            Assert.Less(PullRules.Lift(0.74f), 0.55f);                      // only the last heave frees it
            Assert.AreEqual(PullRules.Lift(0.05f), PullRules.Lift(0.1f), 0.03f);   // at the start of a heave it only strains: the column barely moves
            Assert.Greater(PullRules.Strain(0.05f), 0.9f); Assert.Less(PullRules.Strain(0.2f), 0.1f);   // straining first, then the heave
            Assert.Greater(PullRules.Heave(0.2f), 0.8f); Assert.AreEqual(0f, PullRules.Heave(0.05f), 1e-4f);
        }

        [Test] public void Charge_StartsSlowAndBuildsUp_AndCoversExactlyTheMovesDistance()
        {
            Assert.Less(ChargeRules.Factor(0f), 0.4f); Assert.Greater(ChargeRules.Factor(1f), 1.5f);
            float last = 0f, area = 0f; const int n = 1000;
            for (int i = 0; i < n; i++) { float f = ChargeRules.Factor((i + 0.5f) / n); Assert.GreaterOrEqual(f, last); last = f; area += f / n; }
            Assert.AreEqual(1f, area, 1e-3f);   // the speeds average to 1: the distance of the move is exactly covered
        }
    }
}
