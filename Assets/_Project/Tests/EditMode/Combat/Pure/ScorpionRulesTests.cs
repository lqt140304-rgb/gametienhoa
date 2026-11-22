using System.Linq;
using Evolve.Combat;
using Evolve.Creatures;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    /// <summary>Bọ Cạp Thạch Anh on the standard boss kit (spec 2026-11-22-bo-cap-thach-anh-moveset-design): the moves, the passive and the rules the rig and the moves share.</summary>
    public class ScorpionRulesTests
    {
        const string Boss = "Bọ Cạp Thạch Anh";
        static MoveDef Move(string name) => MovesetSeed.For(Boss).Single(m => m.Name == name);

        [Test] public void Scorpion_HasItsSixMoves_ByAnyName()
        {
            foreach (var n in new[] { Boss, "BoCapThachAnh (Meshy)(Clone)", "bo_cap_thach_anh" }) Assert.AreEqual(6, MovesetSeed.For(n).Count, n);
            Assert.IsTrue(MovesetSeed.Has(Boss));
            CollectionAssert.AreEquivalent(new[] { "Châm Choáng", "Giáng Đuôi", "Phóng Gai Tinh Thể", "Đào Chui Trồi Lên", "Lao Kẹp", "Bùng Pha Lê" }, MovesetSeed.For(Boss).Select(m => m.Name));
        }

        [Test] public void Scorpion_Roles_AreTheStandardKitPlusOneCombo()
        {
            var moves = MovesetSeed.For(Boss);
            Assert.AreEqual(MoveRole.Normal, Move("Châm Choáng").Role); Assert.AreEqual(MoveRole.Normal, Move("Giáng Đuôi").Role);
            Assert.AreEqual(MoveRole.Ranged, Move("Phóng Gai Tinh Thể").Role); Assert.AreEqual(MoveRole.Special, Move("Đào Chui Trồi Lên").Role);
            Assert.AreEqual(MoveRole.Combo, Move("Lao Kẹp").Role); Assert.AreEqual(MoveRole.Ultimate, Move("Bùng Pha Lê").Role);
            Assert.AreEqual(1, moves.Count(m => m.Role == MoveRole.Combo));
            Assert.IsTrue(Move("Đào Chui Trồi Lên").Dodge); Assert.IsFalse(Move("Đào Chui Trồi Lên").Pins);   // a dodge puzzle, no hold and no mini game
        }

        [Test] public void Scorpion_EveryMoveHasThreePhases_ChargeUpCastAndAfterCast_AndItsOwnAnimationName()
        {
            var anims = MovesetSeed.For(Boss).Select(m => m.Anim).ToArray();
            CollectionAssert.AreEquivalent(new[] { "Sting", "TailSlam", "Volley", "Burrow", "Pinch", "Burst" }, anims);   // the clips Tell_/Strike_/Recover_ + this name exist in the FBX
            foreach (var m in MovesetSeed.For(Boss))
            {
                Assert.Greater(m.Windup, 0f, m.Name + " charge-up"); Assert.Greater(m.Active, 0f, m.Name + " cast"); Assert.Greater(m.Recover, 0f, m.Name + " after-cast");
            }
        }

        [Test] public void Scorpion_Sting_IsAClosePierceThatStuns()
        {
            var m = Move("Châm Choáng");
            Assert.AreEqual(MoveShape.Lunge, m.Shape); Assert.AreEqual(0f, m.MinRange); Assert.AreEqual(3.2f, m.MaxRange); Assert.AreEqual(18f, m.Damage); Assert.AreEqual(1f, m.Freeze); Assert.AreEqual(0.6f, m.Windup);
        }

        [Test] public void Scorpion_TailSlam_IsAWideCloseSmash_WithALongTell()
        {
            var m = Move("Giáng Đuôi");
            Assert.AreEqual(MoveShape.Spin, m.Shape); Assert.AreEqual(0f, m.MinRange); Assert.LessOrEqual(m.MaxRange, 3.6f); Assert.AreEqual(26f, m.Damage); Assert.AreEqual(6f, m.Force); Assert.AreEqual(0.9f, m.Windup);
        }

        [Test] public void Scorpion_Volley_ThreeCrystalSpikesFromAfar()
        {
            var m = Move("Phóng Gai Tinh Thể");
            Assert.AreEqual(MoveShape.Spears, m.Shape); Assert.GreaterOrEqual(m.MinRange, 5f); Assert.AreEqual(16f, m.MaxRange); Assert.AreEqual(3, m.SpearCount); Assert.AreEqual(9f, m.Damage); Assert.AreEqual(20f, m.SpearSpeed); Assert.AreEqual(1.0f, m.Windup);
            Assert.LessOrEqual(m.SpearCount * m.Damage, 30f);
        }

        [Test] public void Scorpion_Burrow_TakesItsTimesFromBurrowRules()
        {
            var m = Move("Đào Chui Trồi Lên");
            Assert.AreEqual(MoveShape.Burrow, m.Shape); Assert.AreEqual(BurrowRules.Dig, m.Windup, 1e-4f); Assert.AreEqual(BurrowRules.Travel + BurrowRules.Marked + BurrowRules.Emerge, m.Active, 1e-4f);
            Assert.AreEqual(5f, m.MinRange); Assert.AreEqual(18f, m.MaxRange); Assert.AreEqual(4f, m.Radius); Assert.AreEqual(30f, m.Damage); Assert.AreEqual(20f, m.Cooldown); Assert.AreEqual(1.5f, m.RecoverFactor);   // open to a counter after
        }

        [Test] public void Scorpion_Pinch_IsTheComboThatClosesTheGap_AndChainsIntoTheSting()
        {
            var m = Move("Lao Kẹp");
            Assert.AreEqual(MoveShape.Charge, m.Shape); Assert.GreaterOrEqual(m.MinRange, 4f); Assert.AreEqual(12f, m.MaxRange); Assert.AreEqual(20f, m.Damage); Assert.AreEqual(14f, m.Cooldown);
            CollectionAssert.AreEqual(new[] { "Châm Choáng", "Giáng Đuôi" }, m.Combo);   // charge, stab, then the tail smash: three blows, then it lies open
        }

        [Test] public void Scorpion_Rhythm_ItCirclesBetweenMoves_AndLiesOpenAfterTheBigOnes_NoPanting()
        {
            Assert.AreEqual(2f, CircleRules.IdleSeconds(Boss));
            Assert.AreEqual(0.6f, Move("Châm Choáng").Rest); Assert.AreEqual(1.2f, Move("Giáng Đuôi").Rest); Assert.AreEqual(1.5f, Move("Lao Kẹp").Rest); Assert.AreEqual(1.5f, Move("Đào Chui Trồi Lên").Rest);
            Assert.AreEqual(0f, Move("Phóng Gai Tinh Thể").Rest);   // the ranged skill leaves no opening: it is the one you answer by dodging
            Assert.IsTrue(MovesetSeed.For(Boss).Where(m => m.Role != MoveRole.Ultimate).All(m => m.RecoverFactor >= 1.5f - 1e-4f), "open to a counter (x1.5) whenever it recovers");
            CollectionAssert.AreEqual(new[] { "Giáng Đuôi" }, Move("Giáng Đuôi").Combo);   // Đập Đôi: a second smash if the player is still in reach
        }

        [Test] public void Scorpion_Burst_IsTheUltimate_ThreeRingsOfSpikes()
        {
            var m = Move("Bùng Pha Lê");
            Assert.AreEqual(MoveShape.Shatter, m.Shape); Assert.AreEqual(0.25f, m.UnlockBelow); Assert.GreaterOrEqual(m.Windup, 1.8f); Assert.AreEqual(10f, m.Radius); Assert.IsTrue(m.Armored);
            Assert.AreEqual(ShatterRules.RingCount * ShatterRules.RingDamage, m.Damage, 1e-4f);
            Assert.GreaterOrEqual(m.Damage, 36f);
            Assert.AreEqual(ShatterRules.FlickCount, m.SpikeCount); Assert.AreEqual(9f, m.MissileDamage); Assert.AreEqual(ShatterRules.FlickEnd, m.Active, 1e-4f);   // after the rings, three crystals flicked at the player by the tail
        }

        [Test] public void Shatter_AfterTheRings_TheTailFlicksThreeCrystals_OneAfterAnother()
        {
            Assert.AreEqual(3, ShatterRules.FlickCount);
            Assert.AreEqual(ShatterRules.RingStart(ShatterRules.RingCount - 1) + ShatterRules.MaxRadius / ShatterRules.RingSpeed, ShatterRules.FlickStart, 1e-4f);   // the last ring has run out
            Assert.AreEqual(ShatterRules.FlickStart, ShatterRules.FlickTime(0), 1e-4f); Assert.AreEqual(ShatterRules.FlickStart + 2 * ShatterRules.FlickInterval, ShatterRules.FlickTime(2), 1e-4f);
            Assert.AreEqual(ShatterRules.FlickTime(ShatterRules.FlickCount - 1) + ShatterRules.FlickInterval, ShatterRules.FlickEnd, 1e-4f);
            Assert.Less(ShatterRules.FlickThrow, ShatterRules.FlickInterval);   // the crystal leaves the tail before the next one rises
            Assert.AreEqual(-25f, ShatterRules.FlickAngle(0), 1e-4f); Assert.AreEqual(0f, ShatterRules.FlickAngle(1), 1e-4f); Assert.AreEqual(25f, ShatterRules.FlickAngle(2), 1e-4f);
        }

        [Test] public void Burrow_Double_OnlyAtTwoThirdsHealthOrLess_AndHasItsOwnQuickerClock()
        {
            Assert.IsTrue(BurrowRules.WantsDouble(0.66f)); Assert.IsTrue(BurrowRules.WantsDouble(0.1f)); Assert.IsFalse(BurrowRules.WantsDouble(0.67f)); Assert.IsFalse(BurrowRules.WantsDouble(1f));
            Assert.AreEqual(BurrowRules.SecondDive + BurrowRules.SecondTravel + BurrowRules.SecondMarked + BurrowRules.SecondEmerge, BurrowRules.SecondTotal, 1e-4f);
            Assert.Less(BurrowRules.SecondTotal, BurrowRules.Total); Assert.Less(BurrowRules.SecondRadius, 4f); Assert.GreaterOrEqual(BurrowRules.SecondMarked, 0.7f);   // smaller circle, but still time to run
            Assert.AreEqual(BurrowStage.Dig, BurrowRules.SecondStageAt(0.1f));
            Assert.AreEqual(BurrowStage.Travel, BurrowRules.SecondStageAt(BurrowRules.SecondDive + 0.1f));
            Assert.AreEqual(BurrowStage.Marked, BurrowRules.SecondStageAt(BurrowRules.SecondMarkTime + 0.1f));
            Assert.AreEqual(BurrowStage.Emerge, BurrowRules.SecondStageAt(BurrowRules.SecondEmergeTime + 0.05f));
            Assert.AreEqual(BurrowStage.Done, BurrowRules.SecondStageAt(BurrowRules.SecondTotal + 0.01f));
            Assert.IsTrue(BurrowRules.CircleFollowsSecond(BurrowRules.SecondMarkTime - 0.01f)); Assert.IsFalse(BurrowRules.CircleFollowsSecond(BurrowRules.SecondMarkTime + 0.01f));
        }

        [Test] public void Environment_ColumnsFromTheVolley_AppearWhereTheSpikeLands_AndLastFiveSeconds()
        {
            Assert.AreEqual(5f, ScorpionRules.ColumnLife); Assert.AreEqual(1.2f, ScorpionRules.StuckSeconds);
            Assert.AreEqual(0.5f, ScorpionRules.ColumnDelay(10f, 20f), 1e-4f); Assert.AreEqual(0f, ScorpionRules.ColumnDelay(10f, 0f)); Assert.AreEqual(0f, ScorpionRules.ColumnDelay(-1f, 20f));
        }

        [Test] public void Arena_ByDefaultItIsDiggableEverywhere_AndARoomCanSayOtherwise()
        {
            Assert.IsTrue(ScorpionArena.CanBurrowAt(3f, 4f));
            ScorpionArena.CanBurrowAt = (x, z) => x < 0f;
            try { Assert.IsFalse(ScorpionArena.CanBurrowAt(3f, 4f)); Assert.IsTrue(ScorpionArena.CanBurrowAt(-3f, 4f)); }
            finally { ScorpionArena.Reset(); }
            Assert.IsTrue(ScorpionArena.CanBurrowAt(3f, 4f));
        }


        [Test] public void Scorpion_UsesItsOwnClips_SoNoStandInBodyPose()
        {
            Assert.IsTrue(MovesetSeed.AuthoredClips.Contains("bocapthachanh"));
            Assert.AreEqual(0f, MovesetSeed.PoseStrength(Boss));
        }

        // ---------- passive: Vỏ Pha Lê ----------
        [Test] public void Passive_CrystalShell_TakesAQuarterLessFromTheFront_NotFromTheSides()
        {
            Assert.IsTrue(PassiveRules.Has(Boss)); Assert.IsTrue(PassiveRules.Has("BoCapThachAnh (Meshy)"));
            Assert.AreEqual(0.75f, PassiveRules.ShellFactor(Boss, 0f), 1e-4f);
            Assert.AreEqual(0.75f, PassiveRules.ShellFactor(Boss, 59f), 1e-4f); Assert.AreEqual(0.75f, PassiveRules.ShellFactor(Boss, -60f), 1e-4f);
            Assert.AreEqual(1f, PassiveRules.ShellFactor(Boss, 61f), 1e-4f); Assert.AreEqual(1f, PassiveRules.ShellFactor(Boss, 180f), 1e-4f); Assert.AreEqual(1f, PassiveRules.ShellFactor(Boss, -120f), 1e-4f);
            Assert.AreEqual(1f, PassiveRules.ShellFactor("Golem Thạch Anh", 0f));   // only the scorpion has a shell
        }

        [Test] public void Passive_CrystalShell_NeverStuns_ButItsHealthDoesNotChangeTheDamage()
        {
            Assert.IsTrue(PassiveRules.StunImmune(Boss)); Assert.IsFalse(PassiveRules.StunImmune("Golem Thạch Anh")); Assert.IsFalse(PassiveRules.StunImmune("Chó Xương"));
            Assert.AreEqual(1f, PassiveRules.DamageFactor(Boss, 0.1f)); Assert.AreEqual(1f, PassiveRules.SpeedFactor(Boss, 0.1f)); Assert.AreEqual(0f, PassiveRules.RegenPerSecond(Boss, 60f));
            Assert.IsFalse(PassiveRules.NoKnockback(Boss));
        }

        [Test] public void ShellAngle_IsTheAngleBetweenFacingAndTheAttacker_WrappedAndSigned()
        {
            Assert.AreEqual(0f, ScorpionRules.ShellAngle(90f, 90f), 1e-4f);
            Assert.AreEqual(-170f, ScorpionRules.ShellAngle(350f, 180f), 1e-3f);
            Assert.AreEqual(20f, ScorpionRules.ShellAngle(350f, 10f), 1e-3f);   // across the 0/360 seam
            Assert.AreEqual(180f, Mathf2.Abs(ScorpionRules.ShellAngle(0f, 180f)), 1e-3f);
        }

        // ---------- rig and animator ----------
        [Test] public void WalkClipSpeed_MatchesTheBakedStride_AndTheAnimatorSpeedIsClamped()
        {
            Assert.AreEqual(1f, ScorpionRules.ClipSpeed, 1e-3f);   // rig_scorpion.py: STRIDE 0.5 m per half cycle of 30 frames at 30 fps
            Assert.AreEqual(1f, ScorpionRules.WalkAnimSpeed(1f), 1e-3f); Assert.AreEqual(2f, ScorpionRules.WalkAnimSpeed(2f), 1e-3f);
            Assert.AreEqual(0.6f, ScorpionRules.WalkAnimSpeed(0.1f)); Assert.AreEqual(3f, ScorpionRules.WalkAnimSpeed(50f));
        }

        [Test] public void EveryMoveAndWalkClip_Has30Keys_SoAClipLastsTwentyNineFrames()
        {
            Assert.AreEqual(30, ScorpionRules.ClipKeys);
            Assert.AreEqual(29f / 30f, ScorpionRules.ClipSeconds, 1e-5f);   // 30 keys at 30 fps span 29 frames
            Assert.AreEqual(1f, ScorpionRules.PhaseAnimSpeed(ScorpionRules.ClipSeconds, ScorpionRules.ClipSeconds), 1e-4f);
        }

        [Test] public void PhaseAnimSpeed_StretchesAClipToThePhasesSeconds()
        {
            Assert.AreEqual(1f, ScorpionRules.PhaseAnimSpeed(0.8f, 0.8f), 1e-4f);
            Assert.AreEqual(0.8f / 1.6f, ScorpionRules.PhaseAnimSpeed(0.8f, 1.6f), 1e-4f);
            Assert.AreEqual(0.4f / 0.2f, ScorpionRules.PhaseAnimSpeed(0.4f, 0.2f), 1e-4f);
            Assert.AreEqual(4f, ScorpionRules.PhaseAnimSpeed(0.8f, 0.01f)); Assert.AreEqual(0.2f, ScorpionRules.PhaseAnimSpeed(0.8f, 99f)); Assert.AreEqual(1f, ScorpionRules.PhaseAnimSpeed(0.8f, 0f));
        }

        [Test] public void LocalMove_TurnsAWorldVelocityIntoForwardAndSideways_ForTheFourWalkClips()
        {
            ScorpionRules.LocalMove(0f, 0f, 2f, out float x, out float y);   // facing +Z (yaw 0), moving +Z: straight ahead
            Assert.AreEqual(0f, x, 1e-4f); Assert.AreEqual(2f, y, 1e-4f);
            ScorpionRules.LocalMove(0f, 0f, -2f, out x, out y); Assert.AreEqual(0f, x, 1e-4f); Assert.AreEqual(-2f, y, 1e-4f);   // backing up
            ScorpionRules.LocalMove(0f, 3f, 0f, out x, out y); Assert.AreEqual(3f, x, 1e-4f); Assert.AreEqual(0f, y, 1e-4f);     // +X is its right
            ScorpionRules.LocalMove(0f, -3f, 0f, out x, out y); Assert.AreEqual(-3f, x, 1e-4f);                                   // strafing left
            ScorpionRules.LocalMove(90f, 2f, 0f, out x, out y); Assert.AreEqual(0f, x, 1e-4f); Assert.AreEqual(2f, y, 1e-4f);     // turned right: +X is now ahead
            ScorpionRules.LocalMove(180f, 0f, 2f, out x, out y); Assert.AreEqual(-2f, y, 1e-4f);
        }

        // ---------- the moves' own rules ----------
        [Test] public void Fan_SpreadsTheSpikesEvenlyAroundTheAim()
        {
            Assert.AreEqual(0f, ScorpionRules.FanAngle(0, 1, 24f)); Assert.AreEqual(0f, ScorpionRules.FanAngle(1, 3, 24f), 1e-4f);
            Assert.AreEqual(-12f, ScorpionRules.FanAngle(0, 3, 24f), 1e-4f); Assert.AreEqual(12f, ScorpionRules.FanAngle(2, 3, 24f), 1e-4f);
            Assert.AreEqual(-18f, ScorpionRules.FanAngle(0, 4, 36f), 1e-4f); Assert.AreEqual(6f, ScorpionRules.FanAngle(2, 4, 36f), 1e-4f);
        }

        [Test] public void Burrow_StagesFollowTheClock_AndTheMoundStopsAtTheTarget()
        {
            Assert.AreEqual(BurrowStage.Dig, BurrowRules.StageAt(0.5f));
            Assert.AreEqual(BurrowStage.Travel, BurrowRules.StageAt(BurrowRules.Dig + 0.1f));
            Assert.AreEqual(BurrowStage.Marked, BurrowRules.StageAt(BurrowRules.Dig + BurrowRules.Travel + 0.1f));   // the red circle is out and no longer follows
            Assert.AreEqual(BurrowStage.Emerge, BurrowRules.StageAt(BurrowRules.Dig + BurrowRules.Travel + BurrowRules.Marked + 0.05f));
            Assert.AreEqual(BurrowStage.Done, BurrowRules.StageAt(BurrowRules.Total + 0.01f));
            Assert.AreEqual(0f, BurrowRules.MoundDistance(0.5f, 12f)); Assert.AreEqual(0f, BurrowRules.MoundDistance(BurrowRules.Dig, 12f));
            Assert.AreEqual(BurrowRules.MoundSpeed * 0.5f, BurrowRules.MoundDistance(BurrowRules.Dig + 0.5f, 99f), 1e-4f);
            Assert.AreEqual(5f, BurrowRules.MoundDistance(BurrowRules.Total, 5f), 1e-4f);   // it never goes past the spot
            Assert.AreEqual(BurrowRules.Dig + BurrowRules.Travel, BurrowRules.MarkTime, 1e-4f); Assert.AreEqual(BurrowRules.Total - BurrowRules.Emerge, BurrowRules.EmergeTime, 1e-4f);
        }

        [Test] public void Burrow_TheCircleIsLockedBeforeItEmerges_SoStandingStillIsTheMistake()
        {
            Assert.GreaterOrEqual(BurrowRules.Marked, 0.9f);   // long enough to run out of a 4 m circle at a walking pace
            Assert.IsFalse(BurrowRules.CircleFollows(BurrowRules.MarkTime + 0.01f)); Assert.IsTrue(BurrowRules.CircleFollows(BurrowRules.MarkTime - 0.01f));
            Assert.IsTrue(BurrowRules.Hits(3.9f, 4f)); Assert.IsFalse(BurrowRules.Hits(4.1f, 4f));
            Assert.AreEqual(1.5f, BurrowRules.ExposedFactor(true)); Assert.AreEqual(1f, BurrowRules.ExposedFactor(false));   // it lies open after a miss
        }

        [Test] public void Shatter_ThreeRingsTravelOutward_WithAGapThatMovesAroundEachTime()
        {
            Assert.AreEqual(3, ShatterRules.RingCount);
            Assert.AreEqual(0f, ShatterRules.RingStart(0)); Assert.AreEqual(ShatterRules.RingInterval, ShatterRules.RingStart(1), 1e-4f);
            Assert.AreEqual(0f, ShatterRules.RingRadius(0, 0f)); Assert.AreEqual(ShatterRules.RingSpeed * 0.5f, ShatterRules.RingRadius(0, 0.5f), 1e-4f);
            Assert.AreEqual(0f, ShatterRules.RingRadius(1, 0.1f));   // the second ring has not been born yet
            Assert.AreEqual(10f, ShatterRules.RingRadius(0, 99f)); Assert.AreEqual(10f, ShatterRules.MaxRadius);
            Assert.AreEqual(0f, ShatterRules.GapCenter(0, 40f) - 40f, 1e-4f);   // the first gap is where the player stood at the start: the lesson
            Assert.AreEqual(120f, ShatterRules.GapCenter(1, 0f), 1e-3f); Assert.AreEqual(240f, ShatterRules.GapCenter(2, 0f), 1e-3f);
            Assert.IsTrue(ShatterRules.InGap(0, 30f, 0f)); Assert.IsFalse(ShatterRules.InGap(1, 30f, 0f)); Assert.IsTrue(ShatterRules.InGap(1, 120f + 20f, 0f));
            Assert.IsTrue(ShatterRules.InGap(2, -120f, 0f));   // 240 and -120 are the same bearing
        }

        [Test] public void Shatter_RingHitsAPlayerWhoseDistanceItCrosses_UnlessTheyStandInTheGap()
        {
            Assert.IsTrue(ShatterRules.RingHits(0, 1.0f, 5f, 180f, 0f, false));   // after 1 s the ring is 9 m out: it has crossed the player at 5 m
            Assert.IsFalse(ShatterRules.RingHits(0, 0.2f, 5f, 180f, 0f, false));   // not yet there
            Assert.IsFalse(ShatterRules.RingHits(0, 1.0f, 5f, 0f, 0f, false));    // in the gap
            Assert.IsFalse(ShatterRules.RingHits(0, 1.0f, 5f, 180f, 0f, true));   // already hit by this ring: once per ring
            Assert.IsFalse(ShatterRules.RingHits(0, 1.0f, 11f, 180f, 0f, false)); // beyond the ring's reach
            Assert.AreEqual(13f, ShatterRules.RingDamage);
        }

        [Test] public void Room_TheMoundCannotDigThroughARockOfTheRoom_ButAnywhereElse()
        {
            var rocks = new[] { new ScorpionRock(10f, 0f, 1.5f), new ScorpionRock(-6f, 4f, 1f) };
            Assert.IsTrue(ScorpionRoomRules.CanDig(0f, 0f, rocks));
            Assert.IsFalse(ScorpionRoomRules.CanDig(10f, 0f, rocks));   // on the rock
            Assert.IsFalse(ScorpionRoomRules.CanDig(10f + 1.5f + ScorpionRoomRules.RockMargin - 0.05f, 0f, rocks));   // against it
            Assert.IsTrue(ScorpionRoomRules.CanDig(10f + 1.5f + ScorpionRoomRules.RockMargin + 0.05f, 0f, rocks));
            Assert.IsFalse(ScorpionRoomRules.CanDig(-6f, 4f + 1f + ScorpionRoomRules.RockMargin - 0.05f, rocks));
            Assert.IsTrue(ScorpionRoomRules.CanDig(3f, 3f, new ScorpionRock[0])); Assert.IsTrue(ScorpionRoomRules.CanDig(3f, 3f, null));
        }

        static class Mathf2 { public static float Abs(float v) => v < 0 ? -v : v; }
    }
}
