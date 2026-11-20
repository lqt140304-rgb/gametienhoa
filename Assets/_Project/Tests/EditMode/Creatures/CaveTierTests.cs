using Evolve.Creatures;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    public class CaveTierTests
    {
        [TearDown] public void Reset() { CaveTier.TierOverride = -1; CaveTier.SizeScale = 1f; }

        [Test] public void Tiers_CoverEveryCave_AndUnknownIsMiddle()
        {
            Assert.AreEqual(0, CaveTier.Of("khoi_sinh"));
            foreach (var id in new[] { "vuon_quang", "reu_tho", "to_bo_hoang" }) Assert.AreEqual(1, CaveTier.Of(id), id);
            foreach (var id in new[] { "vuc_nut_tim", "nam_huynh_quang", "suoi_nuoc_nong", "khe_gio_hu" }) Assert.AreEqual(2, CaveTier.Of(id), id);
            foreach (var id in new[] { "nghia_dia", "hoa_thach", "tieng_tho" }) Assert.AreEqual(3, CaveTier.Of(id), id);
            Assert.AreEqual(2, CaveTier.Of("la_hoi"));
            Assert.AreEqual(2, CaveTier.Of(""));
            Assert.AreEqual(2, CaveTier.Of(null));
        }

        [Test] public void Danger_Table_MatchesSpec()
        {
            var d0 = CaveTier.DangerOf(0); var d1 = CaveTier.DangerOf(1); var d2 = CaveTier.DangerOf(2); var d3 = CaveTier.DangerOf(3);
            Assert.AreEqual(0.6f, d0.Damage); Assert.AreEqual(0.6f, d0.Detect); Assert.AreEqual(0.5f, d0.HunterCount); Assert.IsTrue(d0.LordGentle);
            Assert.AreEqual(0.8f, d1.Damage); Assert.AreEqual(0.8f, d1.Detect); Assert.AreEqual(1f, d1.HunterCount); Assert.AreEqual(0.85f, d1.LordSpeed);
            Assert.AreEqual(1f, d2.Damage); Assert.AreEqual(1f, d2.Detect); Assert.AreEqual(1f, d2.HunterCount); Assert.AreEqual(1f, d2.LordSpeed);
            Assert.AreEqual(1.4f, d3.Damage); Assert.AreEqual(1.4f, d3.Detect); Assert.AreEqual(1.5f, d3.HunterCount); Assert.AreEqual(1.15f, d3.LordSpeed);
            Assert.IsFalse(d1.LordGentle || d2.LordGentle || d3.LordGentle);
        }

        [Test] public void Danger_RisesWithDepth()
        {
            for (int t = 1; t <= 3; t++)
            {
                Assert.GreaterOrEqual(CaveTier.DangerOf(t).Damage, CaveTier.DangerOf(t - 1).Damage);
                Assert.GreaterOrEqual(CaveTier.DangerOf(t).Detect, CaveTier.DangerOf(t - 1).Detect);
            }
        }

        [Test] public void SizeFactor_MatchesSpec()
        {
            for (int t = 0; t <= 3; t++) Assert.AreEqual(0.85f, CaveTier.SizeFactor(CreatureRole.Prey, t), 1e-4f);
            var hunter = new[] { 1.2f, 1.3f, 1.4f, 1.5f }; var lord = new[] { 2.4f, 2.6f, 2.9f, 3.2f };
            for (int t = 0; t <= 3; t++)
            {
                Assert.AreEqual(hunter[t], CaveTier.SizeFactor(CreatureRole.Hunter, t), 1e-4f);
                Assert.AreEqual(lord[t], CaveTier.SizeFactor(CreatureRole.Lord, t), 1e-4f);
            }
        }

        [Test] public void EveryLord_AtLeast3m_AfterFactor()
        {
            foreach (float width in new[] { 1.56f, 1.84f, 1.76f })   // Cóc Hồ Già, Rùa Pha Lê, Bọ Cạp Thạch Anh
                for (int t = 0; t <= 3; t++) Assert.GreaterOrEqual(width * CaveTier.SizeFactor(CreatureRole.Lord, t), CaveTier.LordMinWidth, $"{width} tier {t}");
            Assert.AreEqual(2f, CaveTier.SlimeMaxWidth);
        }

        [Test] public void RoleOf_ByNameOrId()
        {
            Assert.AreEqual(CreatureRole.Lord, CaveTier.RoleOf("Cóc Hồ Già"));
            Assert.AreEqual(CreatureRole.Hunter, CaveTier.RoleOf("ThanLanBoHo"));
            Assert.AreEqual(CreatureRole.Prey, CaveTier.RoleOf("Ốc Hồ"));
            Assert.AreEqual(CreatureRole.Prey, CaveTier.RoleOf("xyz"));
        }

        [Test] public void IsCalm_OnlyUntilProvoked()
        {
            Assert.IsTrue(CaveTier.IsCalm(true, 0f, 10f));
            Assert.IsFalse(CaveTier.IsCalm(true, 40f, 10f));
            Assert.IsTrue(CaveTier.IsCalm(true, 40f, 41f));
            Assert.IsFalse(CaveTier.IsCalm(false, 0f, 10f));
            Assert.AreEqual(30f, CaveTier.ProvokeSeconds);
        }

        [Test] public void InZone_IgnoresHeight_EdgeInclusive()
        {
            Assert.IsTrue(CaveTier.InZone(3f, 4f, 0f, 0f, 5f));    // exactly on the edge
            Assert.IsFalse(CaveTier.InZone(3f, 4.1f, 0f, 0f, 5f));
            Assert.IsTrue(CaveTier.InZone(10f, 10f, 10f, 10f, 0f));
        }

        [Test] public void Overrides_AffectOutputsAndReset()
        {
            CaveTier.TierOverride = 3;
            Assert.AreEqual(3, CaveTier.Of("khoi_sinh"));
            CaveTier.TierOverride = -1;
            Assert.AreEqual(0, CaveTier.Of("khoi_sinh"));
            CaveTier.SizeScale = 2f;
            Assert.AreEqual(1.7f, CaveTier.SizeFactor(CreatureRole.Prey, 0), 1e-4f);
            CaveTier.SizeScale = 1f;
            Assert.AreEqual(0.85f, CaveTier.SizeFactor(CreatureRole.Prey, 0), 1e-4f);
        }
    }
}
