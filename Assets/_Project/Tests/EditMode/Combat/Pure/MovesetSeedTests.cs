using System;
using System.Linq;
using Evolve.Combat;
using Evolve.Creatures;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    public class MovesetSeedTests
    {
        static MoveDef One(string species, string move) => MovesetSeed.For(species).Single(m => m.Name == move);

        [Test] public void Seed_HasTheFourSpecies_ByAnyName()
        {
            Assert.AreEqual("Rình-Vọt", MovesetSeed.For("Thằn Lằn Bờ Hồ").Single().Name);
            Assert.AreEqual("Rình-Vọt", MovesetSeed.For("ThanLanBoHo(Clone)").Single().Name);
            Assert.IsTrue(MovesetSeed.For("BoGiapQuang").Single().Armored);
            Assert.AreEqual(MoveShape.Drain, MovesetSeed.For("MuoiReu").Single().Shape);
            Assert.AreEqual(4, MovesetSeed.For("RuaPhaLe").Count);
            foreach (var other in new[] { "Ốc Hồ", "xyz", "", null }) { Assert.AreEqual(0, MovesetSeed.For(other).Count); Assert.IsFalse(MovesetSeed.Has(other)); }
            Assert.IsTrue(MovesetSeed.Has("Rùa Pha Lê"));
        }

        [Test] public void Numbers_MatchTheSpec()
        {
            var lunge = One("ThanLanBoHo", "Rình-Vọt");
            Assert.AreEqual(0.6f, lunge.Windup); Assert.AreEqual(0.25f, lunge.Active); Assert.AreEqual(1.0f, lunge.Recover); Assert.AreEqual(3f, lunge.Distance); Assert.AreEqual(10f, lunge.Damage);
            var ram = One("BoGiapQuang", "Húc");
            Assert.AreEqual(0.9f, ram.Windup); Assert.AreEqual(1.6f, ram.Recover); Assert.AreEqual(7f, ram.Distance); Assert.AreEqual(14f, ram.Damage);
            var leech = One("MuoiReu", "Chích-Hút");
            Assert.AreEqual(0.4f, leech.Windup); Assert.AreEqual(0.8f, leech.Recover);
            var bite = One("RuaPhaLe", "Cắn Cổ"); var roll = One("RuaPhaLe", "Rụt Mai–Lao"); var spin = One("RuaPhaLe", "Xoay Mai"); var call = One("RuaPhaLe", "Gọi Bầy");
            Assert.AreEqual(0.5f, bite.Windup); Assert.AreEqual(22f, bite.Damage); Assert.AreEqual(1f, bite.UnlockBelow);
            Assert.AreEqual(0.8f, roll.Windup); Assert.AreEqual(18f, roll.Damage); Assert.IsTrue(roll.Armored);
            Assert.AreEqual(0.66f, spin.UnlockBelow); Assert.AreEqual(4f, spin.Radius);
            Assert.AreEqual(0.33f, call.UnlockBelow); Assert.AreEqual(25f, call.Radius); Assert.AreEqual(SpeciesNames.Key("Bọ Giáp Quặng"), call.TargetSpecies); Assert.AreEqual(1.0f, call.Windup);
            foreach (var m in MovesetSeed.For("RuaPhaLe")) Assert.AreEqual(1.5f, m.Recover, m.Name);
        }

        [Test] public void Pose_IsIdentityWhenIdle_AndGrowsDuringWindup()
        {
            var idle = TellPose.Evaluate(PoseKind.Crouch, MovePhase.Idle, 0f);
            Assert.AreEqual(1f, idle.ScaleY); Assert.AreEqual(0f, idle.PitchDeg);
            var half = TellPose.Evaluate(PoseKind.Crouch, MovePhase.Windup, 0.5f); var peak = TellPose.Evaluate(PoseKind.Crouch, MovePhase.Windup, 1f);
            Assert.Less(peak.ScaleY, 0.8f);
            Assert.Greater(Math.Abs(1f - peak.ScaleY), Math.Abs(1f - half.ScaleY));
            var end = TellPose.Evaluate(PoseKind.Crouch, MovePhase.Recover, 1f);
            Assert.AreEqual(1f, end.ScaleY, 0.02f); Assert.AreEqual(1f, end.ScaleZ, 0.02f); Assert.AreEqual(0f, end.PitchDeg, 0.5f);
        }

        [Test] public void Pose_EveryKind_IsVisible_AndFinite()
        {
            foreach (PoseKind k in Enum.GetValues(typeof(PoseKind)))
            {
                var p = TellPose.Evaluate(k, MovePhase.Windup, 1f);
                float biggest = new[] { Math.Abs(p.ScaleX - 1f), Math.Abs(p.ScaleY - 1f), Math.Abs(p.ScaleZ - 1f), Math.Abs(p.PitchDeg) / 90f, Math.Abs(p.OffsetY), Math.Abs(p.OffsetZ) }.Max();
                Assert.Greater(biggest, 0.12f, k.ToString());
                foreach (float v in new[] { p.ScaleX, p.ScaleY, p.ScaleZ, p.PitchDeg, p.OffsetY, p.OffsetZ, p.Shake }) Assert.IsFalse(float.IsNaN(v) || float.IsInfinity(v), k.ToString());
            }
        }

        [Test] public void PoseOf_ByShape_AndWithdrawForTheShellMoves()
        {
            Assert.AreEqual(PoseKind.Crouch, MovesetSeed.PoseOf(One("ThanLanBoHo", "Rình-Vọt")));
            Assert.AreEqual(PoseKind.HornDown, MovesetSeed.PoseOf(One("BoGiapQuang", "Húc")));
            Assert.AreEqual(PoseKind.Withdraw, MovesetSeed.PoseOf(One("RuaPhaLe", "Rụt Mai–Lao")));
            Assert.AreEqual(PoseKind.Hover, MovesetSeed.PoseOf(One("MuoiReu", "Chích-Hút")));
            Assert.AreEqual(PoseKind.Spin, MovesetSeed.PoseOf(One("RuaPhaLe", "Xoay Mai")));
            Assert.AreEqual(PoseKind.Roar, MovesetSeed.PoseOf(One("RuaPhaLe", "Gọi Bầy")));
        }

        [Test] public void Slice2_Seed_ByAnyName()
        {
            Assert.AreEqual("Vồ Xa", MovesetSeed.For("Chồn Khe").Single().Name);
            Assert.AreEqual("Vồ Xa", MovesetSeed.For("ChonKhe(Clone)").Single().Name);
            foreach (var n in new[] { "Cáo Gió", "CaoGio" })
                CollectionAssert.AreEqual(new[] { "Vồ Cắn", "Giáo Gió", "Gọi Bầy" }, MovesetSeed.For(n).Select(m => m.Name).ToArray());
        }

        [Test] public void Slice2_Numbers_MatchTheSpec()
        {
            var leap = One("ChonKhe", "Vồ Xa");
            Assert.AreEqual(0.35f, leap.Windup); Assert.AreEqual(0.5f, leap.Active); Assert.AreEqual(1.5f, leap.Recover); Assert.AreEqual(8f, leap.Distance);
            Assert.AreEqual(16f, leap.Damage); Assert.AreEqual(3f, leap.MinRange); Assert.AreEqual(9f, leap.MaxRange); Assert.AreEqual(6f, leap.RetreatDistance); Assert.AreEqual(4f, leap.Cooldown);
            var bite = One("CaoGio", "Vồ Cắn"); var call = One("CaoGio", "Gọi Bầy");
            Assert.AreEqual(0.5f, bite.Windup); Assert.AreEqual(4f, bite.Distance); Assert.AreEqual(22f, bite.Damage);
            Assert.AreEqual(1.0f, call.Windup); Assert.AreEqual(30f, call.Radius); Assert.AreEqual(0.33f, call.UnlockBelow); Assert.AreEqual(SpeciesNames.Key("Chồn Khe"), call.TargetSpecies);
            foreach (var m in MovesetSeed.For("CaoGio")) Assert.AreEqual(1.5f, m.Recover, m.Name == "Giáo Gió" ? 0.11f : 0f);   // Giáo Gió recovers a little longer (1.6)
        }

        [Test] public void Spears_Numbers_MatchTheSpec()
        {
            var sp = One("CaoGio", "Giáo Gió");
            Assert.AreEqual(MoveShape.Spears, sp.Shape);
            Assert.AreEqual(3, sp.SpearCount); Assert.AreEqual(26f, sp.SpearSpeed); Assert.AreEqual(0.3f, sp.LaunchInterval, 1e-5f); Assert.AreEqual(9f, sp.Force); Assert.AreEqual(26f, sp.Damage);
            Assert.AreEqual(1.3f, sp.Windup); Assert.AreEqual(0.9f, sp.Active); Assert.AreEqual(1.6f, sp.Recover); Assert.AreEqual(6f, sp.Cooldown);
            Assert.AreEqual(4f, sp.MinRange); Assert.AreEqual(16f, sp.MaxRange); Assert.AreEqual(1f, sp.UnlockBelow);
        }

        [Test] public void Spears_AllLaunchesFitInTheActiveTime()
        {
            var sp = One("CaoGio", "Giáo Gió");
            Assert.LessOrEqual((sp.SpearCount - 1) * sp.LaunchInterval, sp.Active);
        }

        [Test] public void Fox_HasNoIceDropOrGust()
        {
            foreach (var m in MovesetSeed.For("CaoGio")) { Assert.AreNotEqual(MoveShape.IceDrop, m.Shape); Assert.AreNotEqual(MoveShape.Gust, m.Shape); }
        }

        [Test] public void Ambusher_OnlyTheWeasel()
        {
            Assert.IsTrue(MovesetSeed.IsAmbusher("Chồn Khe")); Assert.IsTrue(MovesetSeed.IsAmbusher("ChonKhe(Clone)"));
            Assert.IsFalse(MovesetSeed.IsAmbusher("Thằn Lằn Bờ Hồ")); Assert.IsFalse(MovesetSeed.IsAmbusher("Cáo Gió")); Assert.IsFalse(MovesetSeed.IsAmbusher(null));
            Assert.AreEqual("Vồ Xa", MovesetSeed.AmbushMove("Chồn Khe").Name);
            Assert.IsNull(MovesetSeed.AmbushMove("Cáo Gió"));
        }

        [Test] public void PoseOf_Spears_IsCast() => Assert.AreEqual(PoseKind.Cast, MovesetSeed.PoseOf(One("CaoGio", "Giáo Gió")));

        [Test] public void Monkey_Seed_ByAnyName()
        {
            foreach (var n in new[] { "Khỉ Hang", "KhiHang(Clone)" })
                CollectionAssert.AreEqual(new[] { "Ném Đá", "Cắn Vặt", "Nhảy Bục", "Hú Báo Động" }, MovesetSeed.For(n).Select(m => m.Name).ToArray());
        }

        [Test] public void Monkey_Numbers_MatchTheSpec()
        {
            var rock = One("KhiHang", "Ném Đá"); var bite = One("KhiHang", "Cắn Vặt"); var perch = One("KhiHang", "Nhảy Bục"); var alarm = One("KhiHang", "Hú Báo Động");
            Assert.AreEqual(MoveShape.Throw, rock.Shape);
            Assert.AreEqual(0.9f, rock.Windup); Assert.AreEqual(0.1f, rock.Active); Assert.AreEqual(1.0f, rock.Recover); Assert.AreEqual(3f, rock.Cooldown);
            Assert.AreEqual(5f, rock.MinRange); Assert.AreEqual(14f, rock.MaxRange); Assert.AreEqual(0.9f, rock.FlightSeconds); Assert.AreEqual(9f, rock.Damage);
            Assert.AreEqual(0.4f, bite.Windup); Assert.AreEqual(0.2f, bite.Active); Assert.AreEqual(0.8f, bite.Recover); Assert.AreEqual(2f, bite.Distance);
            Assert.AreEqual(8f, bite.Damage); Assert.AreEqual(2.5f, bite.MaxRange); Assert.AreEqual(4f, bite.RetreatDistance); Assert.AreEqual(2f, bite.Cooldown);
            Assert.AreEqual(MoveShape.Perch, perch.Shape);
            Assert.AreEqual(0.5f, perch.Windup); Assert.AreEqual(0.8f, perch.Active); Assert.AreEqual(8f, perch.PerchSeconds); Assert.AreEqual(1.5f, perch.MinHeight);
            Assert.AreEqual(3f, perch.MaxHeight); Assert.AreEqual(7f, perch.Distance); Assert.AreEqual(6f, perch.MaxRange); Assert.AreEqual(15f, perch.Cooldown);
            Assert.AreEqual(0.8f, alarm.Windup); Assert.AreEqual(0.1f, alarm.Active); Assert.AreEqual(1.0f, alarm.Recover); Assert.AreEqual(30f, alarm.Radius);
            Assert.AreEqual(0.66f, alarm.UnlockBelow); Assert.AreEqual(20f, alarm.Cooldown); Assert.AreEqual(SpeciesNames.Key("Khỉ Hang"), alarm.TargetSpecies);
        }

        [Test] public void PoseOf_ThrowAndPerch()
        {
            Assert.AreEqual(PoseKind.Throw, MovesetSeed.PoseOf(One("KhiHang", "Ném Đá")));
            Assert.AreEqual(PoseKind.Crouch, MovesetSeed.PoseOf(One("KhiHang", "Nhảy Bục")));
        }

        [Test] public void Throw_RangeGate_FiveToFourteen()
        {
            var rock = One("KhiHang", "Ném Đá");
            foreach (var (dist, starts) in new[] { (3f, false), (8f, true), (15f, false) })
            {
                var sched = new MoveScheduler(new[] { rock }, 1); bool started = false;
                for (float t = 0f; t < 2f; t += 0.05f) { sched.Update(0.05f, true, dist, 1f); started |= sched.Busy; }
                Assert.AreEqual(starts, started, "distance " + dist);
            }
        }

        [Test] public void ChoXuong_HasItsSixMoves_ByAnyName()
        {
            foreach (var n in new[] { "Chó Xương", "ChoXuong (Meshy)(Clone)", "choxuong" }) Assert.AreEqual(6, MovesetSeed.For(n).Count, n);
            Assert.IsTrue(MovesetSeed.Has("Chó Xương"));
            var pounce = One("ChoXuong", "Vồ"); var bite = One("ChoXuong", "Cắn Nghiền"); var leap = One("ChoXuong", "Nhảy Vượt"); var roar = One("ChoXuong", "Gầm"); var howl = One("ChoXuong", "Hú Gọi Bầy");
            Assert.AreEqual(0.6f, pounce.Windup); Assert.AreEqual(0.45f, pounce.Active); Assert.AreEqual(24f, pounce.Damage); Assert.AreEqual(5f, pounce.Distance); Assert.AreEqual(1.5f, pounce.Recover);
            Assert.IsTrue(pounce.Pins); Assert.IsFalse(bite.Pins); Assert.IsFalse(One("ChoXuong", "Cào Xé").Pins);   // only the pounce pins its prey
            Assert.AreEqual(0.35f, bite.Windup); Assert.AreEqual(20f, bite.Damage); Assert.AreEqual(2.5f, bite.MaxRange);
            Assert.AreEqual(MoveShape.Perch, leap.Shape); Assert.AreEqual(6f, leap.Distance); Assert.AreEqual(1.5f, leap.MaxHeight); Assert.AreEqual(12f, leap.Cooldown);
            Assert.AreEqual(MoveShape.Roar, roar.Shape); Assert.AreEqual(0.9f, roar.Windup); Assert.AreEqual(8f, roar.Radius); Assert.AreEqual(4f, roar.FearSeconds); Assert.AreEqual(0.8f, roar.Freeze); Assert.AreEqual(20f, roar.Cooldown); Assert.AreEqual(0.66f, roar.UnlockBelow);
            Assert.AreEqual(MoveShape.Call, howl.Shape); Assert.AreEqual(SpeciesNames.Key("Bọ Ăn Xác"), howl.TargetSpecies); Assert.AreEqual(0.66f, howl.UnlockBelow); Assert.AreEqual(20f, howl.Cooldown);
            var claw = One("ChoXuong", "Cào Xé");
            Assert.AreEqual(0.55f, claw.Windup); Assert.AreEqual(16f, claw.Damage); Assert.AreEqual(3.5f, claw.MaxRange); Assert.AreEqual(1f, claw.UnlockBelow); Assert.AreEqual(PoseKind.Crouch, MovesetSeed.PoseOf(claw));
            Assert.AreEqual(0.4f, MovesetSeed.PoseStrength("Chó Xương")); Assert.AreEqual(1f, MovesetSeed.PoseStrength("Cáo Gió")); Assert.AreEqual(1f, MovesetSeed.PoseStrength(null));
            Assert.AreEqual(1f, pounce.UnlockBelow); Assert.AreEqual(1f, bite.UnlockBelow); Assert.AreEqual(1f, leap.UnlockBelow);
        }

        [Test] public void ChoXuong_RoarAndHowl_ShowTheRoarPose_AndTheMovesUnlockWithHealth()
        {
            Assert.AreEqual(PoseKind.Roar, MovesetSeed.PoseOf(One("ChoXuong", "Gầm")));
            Assert.AreEqual(PoseKind.Roar, MovesetSeed.PoseOf(One("ChoXuong", "Hú Gọi Bầy")));
            Assert.AreEqual(PoseKind.Crouch, MovesetSeed.PoseOf(One("ChoXuong", "Vồ")));
            Assert.IsFalse(MovesetSeed.For("ChoXuong").Any(m => m.UnlockBelow >= 1f && m.Shape == MoveShape.Roar));
        }
    }
}
