using System;
using System.Linq;
using Evolve.Combat;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    public class BirdRulesTests
    {
        static MoveDef Move(string name) => MovesetSeed.For("Chim Hang Mẹ").Single(m => m.Name == name);

        [Test] public void Bird_HasItsFourMoves_ByAnyName()
        {
            foreach (var n in new[] { "Chim Hang Mẹ", "ChimHangMe (Meshy)(Clone)", "chim_hang_me" }) Assert.AreEqual(4, MovesetSeed.For(n).Count, n);
            var peck = Move("Mổ"); var slash = Move("Chém Gió"); var swoop = Move("Bổ Nhào"); var talon = Move("Quắp");
            Assert.AreEqual(MoveShape.Lunge, peck.Shape); Assert.AreEqual(MoveRole.Normal, peck.Role); Assert.AreEqual(0f, peck.MinRange);
            Assert.Greater(peck.RetreatDistance, 0f);   // pecks and flutters back, so it does not hug the player where none of the sky moves (3-5 m and up) can be used
            Assert.Greater(peck.RetreatDistance + peck.MaxRange, new[] { slash.MinRange, swoop.MinRange, talon.MinRange }.Max());
            Assert.AreEqual(MoveShape.WindSlash, slash.Shape); Assert.AreEqual("Slash", slash.Anim); Assert.Greater(slash.SpearSpeed, 0f); Assert.Greater(slash.Damage, 0f);
            Assert.AreEqual(MoveShape.Swoop, swoop.Shape); Assert.AreEqual("Swoop", swoop.Anim); Assert.Greater(swoop.Radius, 0f); Assert.Greater(swoop.MinRange, 0f);
            Assert.AreEqual(MoveShape.Talon, talon.Shape); Assert.AreEqual("Talon", talon.Anim); Assert.Greater(talon.Damage, 0f); Assert.Greater(talon.MissileDamage, 0f);   // Damage is the catch, MissileDamage one rake
            foreach (var m in new[] { slash, swoop, talon }) { Assert.Greater(m.Windup, 0.5f, m.Name); Assert.Greater(m.Cooldown, 2f, m.Name); Assert.AreEqual(1f, m.UnlockBelow, m.Name); }   // a tell to read, a pause between uses, no health gate
            Assert.AreEqual(0f, MovesetSeed.PoseStrength("Chim Hang Mẹ"));   // the bird turns its own bones: the stand-in whole-body squash would only fight the dive
        }

        [Test] public void Bird_IsUnhurried_RestsAfterTheBigMoves_CirclesBetweenMoves_AndNoMoveBurstsOver40Damage()
        {
            var swoop = Move("Bổ Nhào"); var talon = Move("Quắp"); var slash = Move("Chém Gió"); var peck = Move("Mổ");
            Assert.GreaterOrEqual(swoop.Rest, 1f); Assert.GreaterOrEqual(talon.Rest, 1f);              // catches its breath after the two big ones
            Assert.Greater(CircleRules.IdleSeconds("Chim Hang Mẹ"), 1f);                               // and walks round its prey between any two moves instead of chaining them
            Assert.GreaterOrEqual(talon.Active, 0.7f); Assert.GreaterOrEqual(swoop.Active, 0.7f);       // a strike that can be seen
            Assert.GreaterOrEqual(talon.Cooldown, 20f); Assert.GreaterOrEqual(swoop.Cooldown, 15f); Assert.GreaterOrEqual(slash.Cooldown, 6f);
            Assert.GreaterOrEqual(peck.Cooldown, peck.Recover);                                         // never pecks again the moment it has recovered
            Assert.LessOrEqual(slash.Damage * 4f, 40f);                                                 // four blades
            Assert.LessOrEqual(talon.Damage + talon.MissileDamage * TalonFsm.RakeCount, 40f);          // the catch plus every rake
            Assert.GreaterOrEqual(TalonFsm.CarrySeconds, 1.4f); Assert.GreaterOrEqual(TalonFsm.RakePass, 0.6f);
        }

        [Test] public void Bird_TheMovesCoverEveryRange_SoItIsNeverIdleWithAPlayerInReach()
        {
            var moves = MovesetSeed.For("Chim Hang Mẹ");
            for (float d = 0f; d <= 16f; d += 0.5f) Assert.IsTrue(moves.Any(m => d >= m.MinRange && d <= m.MaxRange), "range " + d);
        }

        // ---------- wings ----------

        [Test] public void UpFold_IsZeroOnTheDownStroke_AndPeaksOnTheUpStroke()
        {
            // phase 0 is the middle of the up stroke (the sine is rising), pi the middle of the down stroke
            Assert.AreEqual(0f, WingRules.UpFold((float)Math.PI), 1e-4f);
            Assert.AreEqual(0f, WingRules.UpFold((float)Math.PI * 0.9f), 1e-4f);
            Assert.Greater(WingRules.UpFold(0.35f), 0.99f);
            Assert.Greater(WingRules.UpFold(0f), 0.5f);
            float best = 0f; for (float ph = 0f; ph < 6.283f; ph += 0.05f) best = Math.Max(best, WingRules.UpFold(ph));
            Assert.LessOrEqual(best, 1f + 1e-4f);
        }

        [Test] public void Wing_FlapsAndFoldsAtTheSameTime_TheElbowDrawsInWhileTheWingRises()
        {
            var open = new BirdPose { Spread = 1f, Beat = 1f };
            WingRules.Segment(1, 0.35f, open, 0f, out float liftUp, out float sweepUp);               // up stroke
            WingRules.Segment(1, (float)Math.PI + 0.35f, open, 0f, out float liftDown, out float sweepDown);   // the same moment of the down stroke
            Assert.Greater(sweepUp, sweepDown + 20f);        // drawn in on the way up, open on the way down
            Assert.Less(sweepDown, 1f);
            Assert.Less(liftUp, 0f);                          // the elbow droops on the up stroke so the tip trails
            WingRules.Segment(2, 0.35f + WingRules.FoldLag * 2f, open, 0f, out _, out float wristSweep);
            Assert.Greater(wristSweep, sweepUp);              // the wrist folds more than the elbow
            // the shoulder itself never folds on a beat (only the elbow and the wrist do)
            for (float ph = 0f; ph < 6.283f; ph += 0.2f) { WingRules.Segment(0, ph, open, 0f, out _, out float s0); Assert.AreEqual(6f, s0, 1e-3f); }
        }

        [Test] public void Wing_ShoulderBeats_FastDownStrokeFurtherThanTheUpStroke()
        {
            var open = new BirdPose { Spread = 1f, Beat = 1f };
            float max = float.MinValue, min = float.MaxValue;
            for (float ph = 0f; ph < 6.283f; ph += 0.02f) { WingRules.Segment(0, ph, open, 0f, out float l, out _); max = Math.Max(max, l); min = Math.Min(min, l); }
            Assert.AreEqual(WingRules.Amplitude(1f, 0f), max, 0.5f);
            Assert.AreEqual(-WingRules.DownFactor * WingRules.Amplitude(1f, 0f), min, 0.5f);
            Assert.Greater(WingRules.Frequency(1f, 0f), WingRules.Frequency(0f, 0f) + 1.5f);   // a fight is a much quicker beat than the idle flutter
            Assert.Greater(WingRules.DownFactor, 1f);
        }

        [Test] public void Wing_FoldedAtRest_OpenWhenSpread_AndTheTipNeverSwingsAbsurdly()
        {
            WingRules.Segment(1, 0f, new BirdPose(), 0f, out _, out float folded);
            Assert.AreEqual(70f, folded, 1e-3f);              // the elbow folded right back
            WingRules.Segment(1, 0f, new BirdPose { Spread = 1f }, 0f, out _, out float open);
            Assert.AreEqual(0f, open, 1e-3f);
            var full = new BirdPose { Spread = 1f, Beat = 1f, Raise = 1f };
            for (float ph = 0f; ph < 6.283f; ph += 0.05f)
            {
                float tip = 0f;
                for (int i = 0; i < 3; i++) { WingRules.Segment(i, ph, full, 1f, out float l, out _); tip += l; }
                Assert.Less(Math.Abs(tip), 200f, "phase " + ph);   // the three segments add up: still within a believable wing
            }
        }

        [Test] public void Wing_RaiseHoldsTheWingsUp_ForTheSlash()
        {
            WingRules.Segment(0, 1f, new BirdPose { Spread = 1f, Raise = 1f }, 0f, out float up, out _);
            WingRules.Segment(0, 1f, new BirdPose { Spread = 1f, Raise = -1f }, 0f, out float down, out _);
            Assert.Greater(up, down + 100f);
            Assert.AreEqual(1.0f, BirdPoseRules.SlashRaise(0f), 1e-4f);          // up before the first chop
            Assert.AreEqual(-0.8f, BirdPoseRules.SlashRaise(0.25f), 1e-4f);      // down in the middle of the first chop
            Assert.AreEqual(1.0f, BirdPoseRules.SlashRaise(0.5f), 1e-4f);        // up again between them
            Assert.AreEqual(-0.8f, BirdPoseRules.SlashRaise(0.75f), 1e-4f);      // down in the second
        }

        // ---------- Bổ Nhào ----------

        [Test] public void Swoop_ClimbsDuringTheWindup_ThenDropsFasterAndFasterToTheGround()
        {
            Assert.AreEqual(0f, SwoopRules.Rise(0f), 1e-4f);
            Assert.AreEqual(1f, SwoopRules.Rise(SwoopRules.ClimbShare), 1e-4f);
            Assert.AreEqual(1f, SwoopRules.Rise(1f), 1e-4f);                       // it hangs at the top for the last fifth
            float last = 0f; for (float p = 0f; p <= 1f; p += 0.05f) { float r = SwoopRules.Rise(p); Assert.GreaterOrEqual(r, last - 1e-5f); last = r; }
            Assert.AreEqual(1f, SwoopRules.Fall(0f), 1e-4f);
            Assert.AreEqual(0f, SwoopRules.Fall(1f), 1e-4f);
            Assert.Greater(SwoopRules.Fall(0.5f), 0.5f);                          // it speeds up: more than half the height is still left at half the time
            float v0 = SwoopRules.Fall(0f) - SwoopRules.Fall(0.1f), v1 = SwoopRules.Fall(0.9f) - SwoopRules.Fall(1f);
            Assert.Greater(v1, v0 * 5f);
            Assert.Less(SwoopRules.MarkAt, 1f); Assert.Greater(SwoopRules.MarkAt, SwoopRules.ClimbShare - 0.25f);   // the ring appears while it is still at the top
            Assert.Less(SwoopRules.ImpactAt, 1f); Assert.Greater(SwoopRules.ImpactAt, 0.85f);
        }

        // ---------- Quắp ----------

        static TalonFsm CaughtFsm() { var f = new TalonFsm(); f.Grab(); return f; }

        [Test] public void Talon_StartsDashing_AndNothingHappensUntilTheCatchOrTheMiss()
        {
            var f = new TalonFsm();
            Assert.AreEqual(TalonStage.Dash, f.Stage); Assert.IsFalse(f.Caught);
            for (int i = 0; i < 200; i++) Assert.AreEqual(TalonEvent.None, f.Update(0.05f));
            Assert.AreEqual(TalonStage.Dash, f.Stage);
        }

        [Test] public void Talon_AMissEndsItAtOnce()
        {
            var f = new TalonFsm();
            f.Miss();
            Assert.AreEqual(TalonStage.Done, f.Stage); Assert.IsTrue(f.Finished); Assert.IsFalse(f.Caught);
            f.Grab(); Assert.AreEqual(TalonStage.Done, f.Stage);   // too late
        }

        [Test] public void Talon_CatchCarriesThenTossesThenRakesFourTimesThenComesDown()
        {
            var f = CaughtFsm();
            Assert.AreEqual(TalonStage.Carry, f.Stage); Assert.IsTrue(f.Caught);
            int tossed = 0, raked = 0, finished = 0; float t = 0f; const float dt = 0.02f;
            var order = new System.Collections.Generic.List<TalonStage> { f.Stage };
            int rakeAtToss = -1;
            while (!f.Finished && t < 30f)
            {
                var e = f.Update(dt); t += dt;
                if (e == TalonEvent.Tossed) { tossed++; rakeAtToss = raked; }
                if (e == TalonEvent.Raked) raked++;
                if (e == TalonEvent.Finished) finished++;
                if (order[order.Count - 1] != f.Stage) order.Add(f.Stage);
            }
            Assert.AreEqual(1, tossed); Assert.AreEqual(0, rakeAtToss);
            Assert.AreEqual(TalonFsm.RakeCount, raked); Assert.AreEqual(1, finished);
            CollectionAssert.AreEqual(new[] { TalonStage.Carry, TalonStage.Toss, TalonStage.Rake, TalonStage.Leave, TalonStage.Done }, order);
            float total = TalonFsm.CarrySeconds + TalonFsm.TossSeconds + TalonFsm.RakeCount * TalonFsm.RakePass + TalonFsm.LeaveSeconds;
            Assert.AreEqual(total, t, 0.1f);
        }

        [Test] public void Talon_EachRakeScoresOnceInTheMiddleOfItsPass()
        {
            var f = CaughtFsm();
            while (f.Stage != TalonStage.Rake) f.Update(0.01f);
            int raked = 0; float firstScoreAt = -1f;
            while (f.Stage == TalonStage.Rake)
            {
                if (f.Update(0.01f) == TalonEvent.Raked) { raked++; if (firstScoreAt < 0f) firstScoreAt = f.StageTime; }
            }
            Assert.AreEqual(TalonFsm.RakeCount, raked);
            Assert.AreEqual(TalonFsm.RakePass * 0.5f, firstScoreAt, 0.03f);
        }

        [Test] public void Talon_SwingsThroughThePlayerInTheMiddleOfEveryPass_AndNeverJumpsBetweenPasses()
        {
            float P = TalonFsm.RakePass;
            Assert.AreEqual(1f, TalonFsm.SwingOffset(0f), 1e-4f); Assert.AreEqual(0f, TalonFsm.SwingOffset(P * 0.5f), 1e-4f); Assert.AreEqual(-1f, TalonFsm.SwingOffset(P), 1e-4f);
            Assert.AreEqual(0f, TalonFsm.SwingOffset(P * 1.5f), 1e-4f); Assert.AreEqual(1f, TalonFsm.SwingOffset(P * 2f), 1e-4f);   // back where it started after two passes
            // the score comes at the middle of a pass: right on the player
            var f = CaughtFsm();
            while (f.Stage != TalonStage.Rake) f.Update(0.005f);
            float scoredAt = -1f;
            while (f.Stage == TalonStage.Rake && scoredAt < 0f) if (f.Update(0.005f) == TalonEvent.Raked) scoredAt = f.StageTime;
            Assert.AreEqual(0f, TalonFsm.SwingOffset(scoredAt), 0.05f);   // the event comes in the first step past the middle (a step is 5 ms here)
            // no jump: neighbouring samples are close, even across the pass boundary, and it is slowest at the turn
            float last = TalonFsm.SwingOffset(0f), maxStep = 0f;
            for (float t = 0.01f; t <= P * TalonFsm.RakeCount; t += 0.01f) { float o = TalonFsm.SwingOffset(t); maxStep = Math.Max(maxStep, Math.Abs(o - last)); last = o; }
            Assert.Less(maxStep, 0.07f);
            Assert.Less(Math.Abs(TalonFsm.SwingOffset(P) - TalonFsm.SwingOffset(P - 0.01f)), Math.Abs(TalonFsm.SwingOffset(P * 0.5f) - TalonFsm.SwingOffset(P * 0.5f - 0.01f)) * 0.1f);
        }

        [Test] public void Talon_GiveUpSkipsToTheDescent_AndAbortStopsEverything()
        {
            var f = CaughtFsm(); f.Update(0.3f);
            f.GiveUp(); Assert.AreEqual(TalonStage.Leave, f.Stage);
            float t = 0f; while (!f.Finished && t < 10f) { f.Update(0.02f); t += 0.02f; }
            Assert.AreEqual(TalonFsm.LeaveSeconds, t, 0.1f);
            var g = CaughtFsm(); g.Abort(); Assert.IsTrue(g.Finished); Assert.AreEqual(TalonEvent.None, g.Update(1f));
            var d = new TalonFsm(); d.GiveUp(); Assert.AreEqual(TalonStage.Dash, d.Stage);   // nothing to give up before the catch
        }

        // ---------- poses ----------

        [Test] public void Poses_TalonsReachOutForTheGrab_AndTheLegsTuckInTheClimb()
        {
            Assert.AreEqual(-1f, BirdPoseRules.Evaluate("Talon", MovePhase.Active, 0.5f).Legs);            // the dash: talons right out
            Assert.Greater(BirdPoseRules.Evaluate("Swoop", MovePhase.Windup, 1f).Legs, 0.7f);              // climbing: legs drawn up
            Assert.Less(BirdPoseRules.Evaluate("Swoop", MovePhase.Active, 1f).Legs, -0.6f);                // the stoop ends with the talons forward
            Assert.Greater(BirdPoseRules.Cruise(1f).Legs, 0.8f);                                           // flying along: tucked
            Assert.Less(BirdPoseRules.Talon(TalonStage.Rake, TalonFsm.RakePass * 0.5f).Legs, -0.9f);       // the middle of a rake
            Assert.Greater(BirdPoseRules.Talon(TalonStage.Rake, 0f).Legs, BirdPoseRules.Talon(TalonStage.Rake, TalonFsm.RakePass * 0.5f).Legs);
        }

        [Test] public void Poses_TheDiveSweepsTheWingsBackAndPutsTheHeadDown_TheClimbOpensAndBeatsThem()
        {
            var climb = BirdPoseRules.Evaluate("Swoop", MovePhase.Windup, 0.5f);
            var dive = BirdPoseRules.Evaluate("Swoop", MovePhase.Active, 0.5f);
            Assert.AreEqual(1f, climb.Spread); Assert.AreEqual(1f, climb.Beat); Assert.Less(climb.Pitch, 0f);   // nose up
            Assert.Less(dive.Spread, 0.2f); Assert.AreEqual(0f, dive.Beat); Assert.Greater(dive.Pitch, 45f);    // nose down
        }

        [Test] public void Poses_EveryPhaseEndsFoldedOrLevel_SoTheBirdSettlesAfterAMove()
        {
            foreach (var anim in new[] { "Swoop", "Slash", "Talon", "Peck" })
            {
                var end = BirdPoseRules.Evaluate(anim, MovePhase.Recover, 1f);
                Assert.AreEqual(0f, end.Spread, 1e-4f, anim); Assert.AreEqual(0f, end.Beat, 1e-4f, anim); Assert.AreEqual(0f, end.Pitch, 1e-4f, anim); Assert.AreEqual(0f, end.Legs, 1e-4f, anim);
                var idle = BirdPoseRules.Evaluate(anim, MovePhase.Idle, 0f);
                Assert.AreEqual(0f, idle.Spread); Assert.AreEqual(0f, idle.Pitch);
            }
        }

        [Test] public void Poses_AreSmoothAcrossStageBoundaries_NoJumpsLargeEnoughToSnapTheBones()
        {
            // the rig smooths them too, but a stage that hands over to the next one should start close to where the last ended
            var carryEnd = BirdPoseRules.Talon(TalonStage.Carry, TalonFsm.CarrySeconds); var tossStart = BirdPoseRules.Talon(TalonStage.Toss, 0f);
            Assert.Less(Math.Abs(carryEnd.Pitch - tossStart.Pitch), 12f); Assert.Less(Math.Abs(carryEnd.Legs - tossStart.Legs), 0.1f);
            var tossEnd = BirdPoseRules.Talon(TalonStage.Toss, TalonFsm.TossSeconds); var rakeStart = BirdPoseRules.Talon(TalonStage.Rake, 0f);
            Assert.Less(Math.Abs(tossEnd.Spread - rakeStart.Spread), 0.2f);
        }

        [Test] public void BirdPose_LerpsEveryField()
        {
            var a = new BirdPose { Spread = 0f, Beat = 0f, Raise = -1f, Legs = -1f, Pitch = 0f, Beak = 0f };
            var b = new BirdPose { Spread = 1f, Beat = 1f, Raise = 1f, Legs = 1f, Pitch = 60f, Beak = 1f };
            var m = BirdPose.Lerp(a, b, 0.5f);
            Assert.AreEqual(0.5f, m.Spread); Assert.AreEqual(0.5f, m.Beat); Assert.AreEqual(0f, m.Raise); Assert.AreEqual(0f, m.Legs); Assert.AreEqual(30f, m.Pitch); Assert.AreEqual(0.5f, m.Beak);
        }
    }
}
