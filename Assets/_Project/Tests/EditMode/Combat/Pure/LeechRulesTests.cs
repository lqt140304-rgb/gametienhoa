using Evolve.Combat;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    public class LeechRulesTests
    {
        static LeechInput In(float dist, float speed = 0f, bool hop = false, bool skill = false, bool hit = false, bool alive = true) =>
            new LeechInput { PlayerDistance = dist, PlayerSpeed = speed, Hopped = hop, SkillUsed = skill, Hit = hit, PlayerAlive = alive };

        static LeechRules Draining()
        {
            var l = new LeechRules();
            l.Update(0.01f, In(1f));                 // notices the player: Hunt
            l.Update(0.5f, In(1f));                  // within reach for longer than the attach windup: Drain
            Assert.AreEqual(LeechState.Drain, l.State);
            return l;
        }

        [Test] public void Idle_NoticesThePlayerInRange()
        {
            var l = new LeechRules();
            l.Update(0.1f, In(20f)); Assert.AreEqual(LeechState.Idle, l.State);
            l.Update(0.1f, In(10f)); Assert.AreEqual(LeechState.Hunt, l.State);
        }

        [Test] public void Hunt_AttachesAfterTheWindup()
        {
            var l = new LeechRules();
            l.Update(0.01f, In(1f)); Assert.AreEqual(LeechState.Hunt, l.State);
            l.Update(0.3f, In(1f)); Assert.AreEqual(LeechState.Hunt, l.State);
            l.Update(0.2f, In(1f)); Assert.AreEqual(LeechState.Drain, l.State);
        }

        [Test] public void Drain_DrainsOnePointFivePerSecond()
        {
            var l = Draining(); float total = 0f;
            for (int i = 0; i < 10; i++) total += l.Update(0.1f, In(1f));
            Assert.AreEqual(1.5f, total, 0.01f);
        }

        [Test] public void Drain_ShakenOffByHop_Skill_Hit()
        {
            var l = Draining(); l.Update(0.1f, In(1f, hop: true)); Assert.AreEqual(LeechState.Scared, l.State);
            l = Draining(); l.Update(0.1f, In(1f, skill: true)); Assert.AreEqual(LeechState.Scared, l.State);
            l = Draining(); l.Update(0.1f, In(1f, hit: true)); Assert.AreEqual(LeechState.Scared, l.State);
        }

        [Test] public void Drain_ShakenOffByRunningFast()
        {
            var l = Draining();
            for (int i = 0; i < 4; i++) l.Update(0.1f, In(1f, 3.5f));
            Assert.AreEqual(LeechState.Drain, l.State);
            l.Update(0.1f, In(1f, 3.5f)); Assert.AreEqual(LeechState.Scared, l.State);

            l = Draining();
            for (int i = 0; i < 3; i++) l.Update(0.1f, In(1f, 3.5f));
            l.Update(0.1f, In(1f, 1f));                                    // slowed down: the count starts again
            for (int i = 0; i < 4; i++) l.Update(0.1f, In(1f, 3.5f));
            Assert.AreEqual(LeechState.Drain, l.State);
        }

        [Test] public void Scared_FliesAway_ThenReturnsOnlyIfThePlayerIsStillNear()
        {
            var l = Draining(); l.Update(0.1f, In(1f, hop: true));
            for (int i = 0; i < 290; i++) l.Update(0.1f, In(10f));
            Assert.AreEqual(LeechState.Scared, l.State);                   // 29 s
            for (int i = 0; i < 12; i++) l.Update(0.1f, In(10f));
            Assert.AreEqual(LeechState.Hunt, l.State);                     // past 30 s, the player is still near

            l = Draining(); l.Update(0.1f, In(1f, hop: true));
            for (int i = 0; i < 310; i++) l.Update(0.1f, In(30f));
            Assert.AreEqual(LeechState.Idle, l.State);                     // the player went away
        }

        [Test] public void Alert_WakesAnIdleMosquito()
        {
            var l = new LeechRules(); l.Alert(); Assert.AreEqual(LeechState.Hunt, l.State);
            l = Draining(); l.Update(0.1f, In(1f, hop: true)); l.Alert(); Assert.AreEqual(LeechState.Scared, l.State);
        }

        [Test] public void PlayerDead_OrFarAway_ReleasesTheDrain()
        {
            var l = Draining(); l.Update(0.1f, In(1f, alive: false)); Assert.AreEqual(LeechState.Hunt, l.State);
            l = Draining(); l.Update(0.1f, In(4f)); Assert.AreEqual(LeechState.Hunt, l.State);
            l.Update(0.1f, In(4f, alive: false)); Assert.AreEqual(LeechState.Idle, l.State);   // nothing left to hunt
        }

        [Test] public void HopWithoutLeech_DoesNothing()
        {
            var l = new LeechRules(); l.Update(0.1f, In(20f, hop: true, skill: true, hit: true)); Assert.AreEqual(LeechState.Idle, l.State);
            l.Update(0.1f, In(10f, hop: true)); Assert.AreEqual(LeechState.Hunt, l.State);
        }
    }
}
