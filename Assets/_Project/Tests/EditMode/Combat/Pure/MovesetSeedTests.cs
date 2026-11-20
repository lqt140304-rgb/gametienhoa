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
    }
}
