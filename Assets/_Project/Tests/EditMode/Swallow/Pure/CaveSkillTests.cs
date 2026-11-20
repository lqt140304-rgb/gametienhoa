using System.Collections.Generic;
using System.Linq;
using Evolve.Organs;
using Evolve.Swallow;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    public class CaveSkillTests
    {
        static IEnumerable<CaveSkillDef> Wave1 => CaveSkillSeed.All;

        [Test] public void Wave1_Has12Skills_FourPerCave()
        {
            Assert.AreEqual(12, CaveSkillSeed.All.Count);
            foreach (var g in Wave1.GroupBy(d => d.Cave))
            {
                Assert.AreEqual(4, g.Count(), g.Key);
                Assert.AreEqual("MMSC", new string(g.Select(d => d.Role).OrderBy(r => "MSC".IndexOf(r)).ToArray()), g.Key);
            }
        }

        [Test] public void Ids_Unique() => Assert.AreEqual(Wave1.Count(), Wave1.Select(d => d.Id).Distinct().Count());

        [Test] public void EveryRow_HasNamesAndFamily()
        {
            foreach (var d in Wave1)
            {
                Assert.IsNotEmpty(d.Skill, d.Id);
                Assert.IsNotEmpty(d.Species, d.Id);
                Assert.IsNotEmpty(d.Description, d.Id);
                Assert.AreNotEqual(SkillFamily.None, d.Family, d.Id);
            }
        }

        [Test] public void Actives_HaveCooldownAndEnergy()
        {
            foreach (var d in Wave1.Where(d => d.Shape != SkillShape.Passive))
            {
                Assert.Greater(d.Cooldown, 0f, d.Id);
                Assert.Greater(d.Energy, 0f, d.Id);
            }
        }

        [Test] public void Passives_SortAsPassive()
        {
            foreach (var d in Wave1.Where(d => d.Shape == SkillShape.Passive))
            {
                bool other = d.MaxHealthBonus > 0f || d.DamageReduction > 0f || d.SonarRangeMultiplier > 1f;
                Assert.IsTrue(other, d.Id);
                Assert.AreEqual(GeneUse.Passive, SkillRules.UseOf(false, false, 0f, 1f, 0f, other), d.Id);
            }
        }

        [Test] public void SpecNumbers()
        {
            CaveSkillDef Get(string id) => Wave1.Single(d => d.Id == id);
            Assert.AreEqual(10f, Get("skill.da_day").MaxHealthBonus);
            Assert.AreEqual(0.5f, Get("skill.thu_minh").DamageTaken);
            Assert.AreEqual(12f, Get("skill.thu_minh").Cooldown);
            Assert.AreEqual(6f, Get("skill.luoi_dai").Distance);
            Assert.AreEqual(4f, Get("skill.o_chan").Radius);
            Assert.AreEqual(0.15f, Get("skill.vo_cung").DamageReduction);
            Assert.AreEqual(1, Get("skill.khien_quang").BlockHits);
            Assert.AreEqual(1.5f, Get("skill.song_do_manh").SonarRangeMultiplier);
            Assert.AreEqual(20f, Get("skill.cam_gai").Seconds);
            Assert.AreEqual(1f, Get("skill.cham_choang").StunSeconds);
        }

        [Test] public void Incoming_ShieldEatsOneWholeHit()
        {
            int charges = 1;
            Assert.AreEqual(0f, CaveSkillMath.Incoming(30f, 0f, 1f, ref charges));
            Assert.AreEqual(0, charges);
            Assert.AreEqual(30f, CaveSkillMath.Incoming(30f, 0f, 1f, ref charges), 1e-4f);
        }

        [Test] public void Incoming_ReductionAndStateStack()
        {
            int none = 0;
            Assert.AreEqual(8.5f, CaveSkillMath.Incoming(10f, 0.15f, 1f, ref none), 1e-4f);    // Vỏ Cứng
            Assert.AreEqual(4.25f, CaveSkillMath.Incoming(10f, 0.15f, 0.5f, ref none), 1e-4f); // + Thu Mình
            Assert.AreEqual(0f, CaveSkillMath.Incoming(10f, 0f, 0f, ref none));                  // Lách Né
            Assert.AreEqual(2f, CaveSkillMath.Incoming(10f, 5f, 1f, ref none), 1e-4f);          // reduction capped at 80%
            Assert.AreEqual(0f, CaveSkillMath.Incoming(-3f, 0f, 1f, ref none));
        }

        [Test] public void Falloff_FullAtCentre_FortyPercentAtEdge_NoneOutside()
        {
            Assert.AreEqual(1f, CaveSkillMath.Falloff(0f, 4f), 1e-4f);
            Assert.AreEqual(0.4f, CaveSkillMath.Falloff(4f, 4f), 1e-4f);
            Assert.AreEqual(0f, CaveSkillMath.Falloff(4.1f, 4f));
            Assert.AreEqual(0f, CaveSkillMath.Falloff(1f, 0f));
        }

        [Test] public void ChamChoang_StunsWithoutDamage() => Assert.AreEqual(0f, Wave1.Single(d => d.Id == "skill.cham_choang").Damage);

        [Test] public void Energy_ThreeAtOnce_ButNotFour()
        {
            var costs = Wave1.Where(d => d.Shape != SkillShape.Passive).Select(d => d.Energy).OrderBy(e => e).ToList();
            Assert.LessOrEqual(costs.Skip(costs.Count - 3).Sum(), 100f);   // the 3 dearest fit a full bar (MaxEnergy 100)
            Assert.Greater(costs.Take(4).Sum(), 100f);                     // no 4 fit: no spamming
        }

        [Test] public void Actives_HaveAStar3Extra()
        {
            foreach (var d in Wave1.Where(d => d.Shape != SkillShape.Passive)) Assert.IsNotEmpty(d.Star3, d.Id);
        }

        [Test] public void StarPower_SmallSteps()
        {
            Assert.AreEqual(1f, CaveSkillMath.StarPower(1), 1e-4f);
            Assert.AreEqual(1.1f, CaveSkillMath.StarPower(2), 1e-4f);
            Assert.AreEqual(1.2f, CaveSkillMath.StarPower(3), 1e-4f);
            Assert.AreEqual(1.2f, CaveSkillMath.StarPower(9), 1e-4f);
            Assert.AreEqual(12f, CaveSkillMath.StarCooldown(12f, 1), 1e-4f);
            Assert.AreEqual(10.8f, CaveSkillMath.StarCooldown(12f, 3), 1e-4f);
        }

        [Test] public void TierOf_Role()
        {
            Assert.AreEqual(OrganTier.C, CaveSkillMath.TierOf('M'));
            Assert.AreEqual(OrganTier.B, CaveSkillMath.TierOf('S'));
            Assert.AreEqual(OrganTier.A, CaveSkillMath.TierOf('C'));
        }
    }
}
