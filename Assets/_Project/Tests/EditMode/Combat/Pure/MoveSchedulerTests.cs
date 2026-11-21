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

        [Test] public void Allow_FiltersOutMoves()
        {
            var s = new MoveScheduler(new[] { L("A", 0f), L("B", 0f) }, 1) { Allow = m => m.Name != "B" };
            CollectionAssert.DoesNotContain(Run(s, 30f, 3f), "B");
        }

        [Test] public void Allow_Null_ChangesNothing()
        {
            var s = new MoveScheduler(new[] { L() }, 1);
            Assert.IsNull(s.Allow);
            Assert.AreEqual(1, Run(s, 3f, 3f).Count);
        }

        [Test] public void AMoveWithAbortBeyond_DropsItsWindup_WhenTheTargetSlipsOutOfReach_WithoutSpendingItsCooldown()
        {
            var m = new MoveDef { Name = "Vồ", Shape = MoveShape.Lunge, Windup = 1f, Active = 0.5f, Recover = 1f, Cooldown = 5f, MinRange = 3f, MaxRange = 5.5f, AbortBeyond = 7.5f, Distance = 5.5f };
            var s = new MoveScheduler(new[] { m }, 1);
            s.Update(0.02f, true, 4f, 1f);
            Assert.AreEqual(MovePhase.Windup, s.Phase);
            s.Update(0.3f, true, 9f, 1f);   // it ran away during the tell
            Assert.AreEqual(MovePhase.Idle, s.Phase);
            s.Update(0.02f, true, 4f, 1f);   // no cooldown was spent: it can try again at once
            Assert.AreEqual(MovePhase.Windup, s.Phase);
        }

        [Test] public void AMoveWithAbortBeyond_StillStrikes_WhileTheTargetStaysClose()
        {
            var m = new MoveDef { Name = "Vồ", Shape = MoveShape.Lunge, Windup = 0.5f, Active = 0.5f, Recover = 1f, Cooldown = 5f, MinRange = 3f, MaxRange = 5.5f, AbortBeyond = 7.5f, Distance = 5.5f };
            var s = new MoveScheduler(new[] { m }, 1);
            s.Update(0.02f, true, 4f, 1f);
            MoveDef started = null;
            for (int i = 0; i < 40 && started == null; i++) started = s.Update(0.02f, true, 7f, 1f);   // drifted back a little, still inside the abort limit
            Assert.AreSame(m, started);
        }

        [Test] public void ARestAfterAMove_IsAddedToTheRecover_AndOnlyTheLastPartIsResting()
        {
            var m = new MoveDef { Name = "x", Windup = 0.1f, Active = 0.1f, Recover = 1f, Rest = 3f, MaxRange = 99f };
            var s = new MoveScheduler(new[] { m }, 1);
            s.Update(0.01f, true, 5f, 1f);                       // picks it
            s.Update(0.2f, true, 5f, 1f); s.Update(0.2f, true, 5f, 1f);   // windup, active
            Assert.AreEqual(MovePhase.Recover, s.Phase); Assert.IsFalse(s.Resting);
            s.Update(1.1f, true, 5f, 1f); Assert.AreEqual(MovePhase.Recover, s.Phase); Assert.IsTrue(s.Resting);   // the recover proper is over: it is catching its breath
            Assert.AreEqual(3f, s.RestLength); Assert.That(s.RestTime, Is.InRange(0f, 0.3f));
            s.Update(2.7f, true, 5f, 1f); Assert.AreEqual(MovePhase.Recover, s.Phase);
            s.Update(0.4f, true, 5f, 1f); Assert.AreEqual(MovePhase.Idle, s.Phase);
        }

        [Test] public void HeldRecover_DoesNotEnd_UntilTheHoldIsReleased_ThenTheRestStarts()
        {
            var m = new MoveDef { Name = "x", Windup = 0.1f, Active = 0.1f, Recover = 0.2f, Rest = 2f, MaxRange = 99f };
            var s = new MoveScheduler(new[] { m }, 1);
            s.Update(0.01f, true, 5f, 1f); s.Update(0.2f, true, 5f, 1f); s.Update(0.2f, true, 5f, 1f);
            Assert.AreEqual(MovePhase.Recover, s.Phase);
            s.HoldRecover = true;
            for (int i = 0; i < 1000; i++) s.Update(0.1f, true, 5f, 1f);     // 100 s: the golem is busy with its rock volley
            Assert.AreEqual(MovePhase.Recover, s.Phase); Assert.IsFalse(s.Resting);
            s.HoldRecover = false;
            s.Update(0.25f, true, 5f, 1f); Assert.IsTrue(s.Resting); Assert.AreEqual(MovePhase.Recover, s.Phase);
            s.Update(2.1f, true, 5f, 1f); Assert.AreEqual(MovePhase.Idle, s.Phase);
        }

        [Test] public void AStun_IsNotARest()
        {
            var m = new MoveDef { Name = "x", Windup = 0.1f, Active = 0.1f, Recover = 0.5f, Rest = 3f, MaxRange = 99f };
            var s = new MoveScheduler(new[] { m }, 1);
            s.Update(0.01f, true, 5f, 1f); s.Update(0.2f, true, 5f, 1f);
            s.Stun(2f); s.Update(1f, true, 5f, 1f);
            Assert.IsFalse(s.Resting);
        }

        static MoveScheduler Chain(out MoveDef rush, out MoveDef a, out MoveDef b)
        {
            a = new MoveDef { Name = "A", Windup = 0.5f, Active = 0.1f, Recover = 0.6f, Rest = 2f, MinRange = 0f, MaxRange = 3f, Cooldown = 9f };
            b = new MoveDef { Name = "B", Windup = 0.5f, Active = 0.1f, Recover = 0.6f, Rest = 2.5f, MinRange = 0f, MaxRange = 3f, Cooldown = 9f };
            rush = new MoveDef { Name = "Rush", Windup = 0.2f, Active = 0.2f, Recover = 1.5f, Rest = 4f, MinRange = 4f, MaxRange = 14f, Combo = new[] { "A", "B" } };
            return new MoveScheduler(new[] { rush, a, b }, 1);
        }

        static void Step(MoveScheduler s, float seconds, float dist) { for (float t = 0f; t < seconds; t += 0.05f) s.Update(0.05f, true, dist, 1f); }

        [Test] public void Combo_AfterTheCharge_RunsTheNamedMovesBackToBack_AndOnlyThenRests()
        {
            var s = Chain(out var rush, out var a, out var b);
            Step(s, 0.1f, 8f);                                   // picks the charge (only move in range at 8 m)
            Assert.AreSame(rush, s.Current);
            var seen = new System.Collections.Generic.List<string>(); bool restedBetween = false;
            for (int i = 0; i < 400 && s.Busy; i++)
            {
                s.Update(0.05f, true, 2f, 1f);                   // the player is right there after the charge
                if (seen.Count == 0 || seen[seen.Count - 1] != s.Current?.Name) seen.Add(s.Current?.Name);
                if (s.Current != null && s.Current.Name != "B" && s.Resting) restedBetween = true;
            }
            CollectionAssert.AreEqual(new[] { "Rush", "A", "B" }, seen.Where(n => n != null).ToList());
            Assert.IsFalse(restedBetween);                       // it did not stop to breathe in the middle of the combo
        }

        [Test] public void Combo_TheRestAfterTheChain_IsTheLongestRestOfTheChain()
        {
            var s = Chain(out _, out _, out _);
            Step(s, 0.1f, 8f);
            while (s.Busy && !(s.Current != null && s.Current.Name == "B" && s.Phase == MovePhase.Recover)) s.Update(0.05f, true, 2f, 1f);
            while (s.Busy && !s.Resting) s.Update(0.05f, true, 2f, 1f);
            Assert.IsTrue(s.Resting); Assert.AreEqual(4f, s.RestLength, 1e-3f);   // the charge's 4 s, not just the last move's 2.5
        }

        [Test] public void Combo_StopsAndRestsAtOnce_WhenThePlayerIsOutOfReachOfTheNextMove()
        {
            var s = Chain(out _, out _, out _);
            Step(s, 0.1f, 8f);
            var names = new System.Collections.Generic.HashSet<string>(); bool rested = false;
            for (int i = 0; i < 400 && s.Busy; i++) { s.Update(0.05f, true, 9f, 1f); names.Add(s.Current?.Name ?? ""); rested |= s.Resting; }   // the player dodged away
            CollectionAssert.DoesNotContain(names, "A"); Assert.IsTrue(rested); Assert.IsFalse(s.Busy);
        }

        [Test] public void Combo_AStunnedChargeEndsTheCombo()
        {
            var s = Chain(out _, out _, out _);
            Step(s, 0.5f, 8f); s.Stun(2f);
            var names = new System.Collections.Generic.HashSet<string>();
            for (int i = 0; i < 400 && s.Busy; i++) { s.Update(0.05f, true, 2f, 1f); names.Add(s.Current?.Name ?? ""); }
            CollectionAssert.DoesNotContain(names, "A");
        }

        [Test] public void MinIdle_MakesItCircleBetweenMoves_BeforeItPicksTheNextOne()
        {
            var m = new MoveDef { Name = "x", Windup = 0.1f, Active = 0.1f, Recover = 0.1f, Cooldown = 0f, MaxRange = 99f };
            var s = new MoveScheduler(new[] { m }, 1) { MinIdle = 2f };
            Assert.IsFalse(s.Circling);
            while (!s.Busy) s.Update(0.05f, true, 5f, 1f);                   // the first move is picked at once
            while (s.Busy) s.Update(0.05f, true, 5f, 1f);
            Assert.IsTrue(s.Circling);                                        // the move is over: it circles the prey and sizes it up
            float t = 0f;
            while (!s.Busy && t < 10f) { s.Update(0.05f, true, 5f, 1f); t += 0.05f; }
            Assert.That(t, Is.InRange(1.9f, 3.8f));                           // 2 s up to 1.8 times that, then the next move
            Assert.IsFalse(s.Circling);
        }

        [Test] public void NoMinIdle_ChangesNothing()
        {
            var m = new MoveDef { Name = "x", Windup = 0.1f, Active = 0.1f, Recover = 0.1f, Cooldown = 0f, MaxRange = 99f };
            var s = new MoveScheduler(new[] { m }, 1);
            while (!s.Busy) s.Update(0.05f, true, 5f, 1f);
            while (s.Busy) s.Update(0.05f, true, 5f, 1f);
            Assert.IsFalse(s.Circling); s.Update(0.05f, true, 5f, 1f); Assert.IsTrue(s.Busy);
        }
    }
}
