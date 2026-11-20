using System.Collections.Generic;
using System.Linq;
using Evolve.Combat;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    public class MoveSchedulerTests
    {
        const float Dt = 0.05f;
        static MoveDef L(string name = "L", float cooldown = 2.5f, float min = 0f, float max = 5f, float unlock = 1f, bool armored = false) =>
            new MoveDef { Name = name, Shape = MoveShape.Lunge, Windup = 0.6f, Active = 0.25f, Recover = 1.0f, Cooldown = cooldown, MinRange = min, MaxRange = max, UnlockBelow = unlock, Armored = armored };

        static List<string> Run(MoveScheduler s, float seconds, float dist, float health = 1f, bool target = true)
        {
            var started = new List<string>();
            for (float t = 0f; t < seconds - 1e-4f; t += Dt) { var m = s.Update(Dt, target, dist, health); if (m != null) started.Add(m.Name); }
            return started;
        }

        [Test] public void Idle_UntilATargetIsInRange()
        {
            var s = new MoveScheduler(new[] { L() }, 1);
            Run(s, 3f, 3f, 1f, false); Assert.AreEqual(MovePhase.Idle, s.Phase);
            Run(s, 3f, 6f); Assert.AreEqual(MovePhase.Idle, s.Phase);
            Assert.IsFalse(s.Busy);
        }

        [Test] public void Cycle_WindupActiveRecoverIdle()
        {
            var s = new MoveScheduler(new[] { L() }, 1);
            var seen = new List<MovePhase>(); int started = 0;
            for (float t = 0f; t < 3.0f; t += Dt)
            {
                if (s.Update(Dt, true, 3f, 1f) != null) started++;
                if (seen.Count == 0 || seen[seen.Count - 1] != s.Phase) seen.Add(s.Phase);
            }
            CollectionAssert.AreEqual(new[] { MovePhase.Windup, MovePhase.Active, MovePhase.Recover, MovePhase.Idle }, seen);
            Assert.AreEqual(1, started);
        }

        [Test] public void Armored_OnlyDuringWindupAndActive()
        {
            var s = new MoveScheduler(new[] { L(armored: true) }, 1);
            var armored = new Dictionary<MovePhase, bool>();
            for (float t = 0f; t < 2.0f; t += Dt) { s.Update(Dt, true, 3f, 1f); armored[s.Phase] = s.IsArmored; }
            Assert.IsTrue(armored[MovePhase.Windup]); Assert.IsTrue(armored[MovePhase.Active]);
            Assert.IsFalse(armored[MovePhase.Recover]); Assert.IsFalse(armored[MovePhase.Idle]);
        }

        [Test] public void Recover_TakesMoreDamage()
        {
            var s = new MoveScheduler(new[] { L() }, 1);
            var factor = new Dictionary<MovePhase, float>();
            for (float t = 0f; t < 2.0f; t += Dt) { s.Update(Dt, true, 3f, 1f); factor[s.Phase] = s.DamageTakenFactor; }
            Assert.AreEqual(1.5f, factor[MovePhase.Recover]); Assert.AreEqual(1f, factor[MovePhase.Windup]); Assert.AreEqual(1f, factor[MovePhase.Idle]);
        }

        [Test] public void Cooldown_BlocksTheSameMoveAgain()
        {
            var s = new MoveScheduler(new[] { L() }, 1);
            Run(s, 3.0f, 3f);
            Assert.AreEqual(MovePhase.Idle, s.Phase);   // cooldown (2.5 s from the strike) is still running
            Run(s, 0.4f, 3f);
            Assert.AreNotEqual(MovePhase.Idle, s.Phase);
        }

        [Test] public void HealthGate_UnlocksPhases()
        {
            MoveScheduler Make() => new MoveScheduler(new[] { L("A", 0f), L("B", 0f, unlock: 0.66f) }, 1);
            CollectionAssert.DoesNotContain(Run(Make(), 60f, 3f, 0.9f), "B");
            CollectionAssert.Contains(Run(Make(), 60f, 3f, 0.5f), "B");
        }

        [Test] public void Range_PicksTheMoveThatFits()
        {
            var near = L("N", 0f, 0f, 2.5f); var far = L("F", 0f, 3f, 8f);
            Assert.AreEqual("F", Run(new MoveScheduler(new[] { near, far }, 1), 1.0f, 5f).First());
            Assert.AreEqual("N", Run(new MoveScheduler(new[] { near, far }, 1), 1.0f, 1f).First());
        }

        static float SecondsToActive(float tellScale)
        {
            var s = new MoveScheduler(new[] { L() }, 1) { TellScale = tellScale };
            float t = 0f;
            while (s.Phase != MovePhase.Active && t < 5f) { s.Update(Dt, true, 3f, 1f); t += Dt; }
            return t;
        }

        [Test] public void TellScale_StretchesTheWindup() => Assert.GreaterOrEqual(SecondsToActive(1.3f) - SecondsToActive(1f), 0.1f);

        [Test] public void Stun_GoesToRecoverAndExtends()
        {
            var s = new MoveScheduler(new[] { L() }, 1);
            while (s.Phase != MovePhase.Active) s.Update(Dt, true, 3f, 1f);
            s.Stun(2f);
            Assert.AreEqual(MovePhase.Recover, s.Phase);
            for (float t = 0f; t < 2.9f; t += Dt) s.Update(Dt, true, 3f, 1f);
            Assert.AreEqual(MovePhase.Recover, s.Phase);
            for (float t = 0f; t < 0.3f; t += Dt) s.Update(Dt, true, 3f, 1f);
            Assert.AreNotEqual(MovePhase.Recover, s.Phase);   // recovery (1.0 + 2.0 s) is over; with a target in range the next windup may already have begun
        }

        [Test] public void TargetLost_DuringWindup_ReturnsToIdle()
        {
            var s = new MoveScheduler(new[] { L() }, 1);
            s.Update(Dt, true, 3f, 1f); s.Update(Dt, true, 3f, 1f);
            Assert.AreEqual(MovePhase.Windup, s.Phase);
            s.Update(Dt, false, 3f, 1f);
            Assert.AreEqual(MovePhase.Idle, s.Phase); Assert.IsFalse(s.Busy);
        }

        [Test] public void SameSeed_SameChoices_AndNoImmediateRepeat()
        {
            MoveScheduler Make(int seed) => new MoveScheduler(new[] { L("A", 0f), L("B", 0f) }, seed);
            var a = Run(Make(3), 30f, 3f); var b = Run(Make(3), 30f, 3f);
            CollectionAssert.AreEqual(a, b);
            for (int i = 1; i < a.Count; i++) Assert.AreNotEqual(a[i - 1], a[i]);
        }

        [Test] public void Force_StartsAQuickWindup()
        {
            var s = new MoveScheduler(new[] { L() }, 1);
            Assert.IsTrue(s.Force(L(), 0.2f));
            for (float t = 0f; t < 0.15f; t += Dt) s.Update(Dt, true, 3f, 1f);
            Assert.AreEqual(MovePhase.Windup, s.Phase);
            for (float t = 0f; t < 0.1f; t += Dt) s.Update(Dt, true, 3f, 1f);
            Assert.AreEqual(MovePhase.Active, s.Phase);
        }

        [Test] public void Force_WhenBusy_IsRefused()
        {
            var s = new MoveScheduler(new[] { L() }, 1);
            s.Update(Dt, true, 3f, 1f);
            Assert.AreEqual(MovePhase.Windup, s.Phase);
            Assert.IsFalse(s.Force(L(), 0.2f));
        }

        [Test] public void Force_Ignores_TellScale()
        {
            var s = new MoveScheduler(new[] { L() }, 1) { TellScale = 2f };
            s.Force(L(), 0.2f);
            for (float t = 0f; t < 0.25f; t += Dt) s.Update(Dt, true, 3f, 1f);
            Assert.AreEqual(MovePhase.Active, s.Phase);
        }

        [Test] public void Force_StartsTheCooldown()
        {
            var move = L();
            var s = new MoveScheduler(new[] { move }, 1);
            s.Force(move, 0.2f);
            for (float t = 0f; t < 2.0f; t += Dt) s.Update(Dt, true, 3f, 1f);   // the whole cycle has run; the move's 2.5 s cooldown still holds it
            Assert.AreEqual(MovePhase.Idle, s.Phase);
        }
    }
}
