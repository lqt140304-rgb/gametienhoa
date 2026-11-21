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

        [Test] public void Volley_GetsUpFirst_ThenWalksPullsAndThrowsOnceForEverySpike_ThenIsDone()
        {
            var v = new VolleyFsm(3);
            Assert.AreEqual(VolleyStage.Rise, v.Stage);   // it lands crouched and does not fetch a spike at once
            int starts = 0, picked = 0, threw = 0, order = 0;
            for (int i = 0; i < 2000 && v.Stage != VolleyStage.Done; i++)
            {
                var e = v.Update(0.05f, v.Stage == VolleyStage.Walk && v.StageTime > 0.3f);   // it arrives after 0.3 s of walking
                if (e == VolleyEvent.PullStart) starts++;
                if (e == VolleyEvent.Picked) { picked++; Assert.AreEqual(order, v.Index); }
                if (e == VolleyEvent.Threw) { threw++; order++; }
            }
            Assert.AreEqual(3, starts); Assert.AreEqual(3, picked); Assert.AreEqual(3, threw); Assert.AreEqual(VolleyStage.Done, v.Stage);
            Assert.AreEqual(VolleyEvent.None, v.Update(0.5f, true));
        }

        [Test] public void Volley_NoSpikeIsTouchedDuringTheRise_AndThePullTakesItsTime()
        {
            var v = new VolleyFsm(1);
            Assert.AreEqual(VolleyEvent.None, v.Update(VolleyFsm.RiseSeconds - 0.1f, true));        // still getting up even though "arrived"
            Assert.AreEqual(VolleyStage.Rise, v.Stage);
            v.Update(0.2f, false); Assert.AreEqual(VolleyStage.Walk, v.Stage);
            Assert.AreEqual(VolleyEvent.PullStart, v.Update(0.1f, true)); Assert.AreEqual(VolleyStage.Pull, v.Stage);
            Assert.AreEqual(VolleyEvent.None, v.Update(VolleyFsm.PullSeconds - 0.1f, true));        // pulling slowly
            Assert.AreEqual(VolleyEvent.Picked, v.Update(0.2f, true)); Assert.AreEqual(VolleyStage.Hold, v.Stage);
            Assert.AreEqual(VolleyEvent.None, v.Update(VolleyFsm.HoldSeconds - 0.1f, true));
            Assert.AreEqual(VolleyEvent.Threw, v.Update(0.2f, true)); Assert.AreEqual(VolleyStage.Done, v.Stage);
        }

        [Test] public void Volley_GivesUpWalkingAfterATimeout_AndPullsWhereItStands()
        {
            var v = new VolleyFsm(2);
            v.Update(VolleyFsm.RiseSeconds + 0.01f, false);
            VolleyEvent e = VolleyEvent.None; float t = 0f;
            while (e != VolleyEvent.PullStart && t < 6f) { e = v.Update(0.1f, false); t += 0.1f; }   // a wall in the way: it never arrives
            Assert.AreEqual(VolleyEvent.PullStart, e); Assert.AreEqual(VolleyFsm.WalkTimeout, t, 0.15f);
        }

        [Test] public void Volley_TheWholeKitFitsInsideTheRecoverTime()
        {
            var golem = MovesetSeed.For("Golem Thạch Anh").Single(m => m.Shape == MoveShape.Meteor);
            Assert.LessOrEqual(VolleyFsm.RiseSeconds + golem.SpikeCount * (1.2f + VolleyFsm.PullSeconds + VolleyFsm.HoldSeconds), golem.Recover);   // with walks of about a second and a bit, every rock is thrown before the recover ends
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
