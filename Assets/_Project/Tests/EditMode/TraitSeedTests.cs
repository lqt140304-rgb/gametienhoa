using System.Linq;
using Evolve.Organs;
using Evolve.Traits;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    public class TraitSeedTests
    {
        [Test]
        public void TwelveCoresAndTwelveFlaws()
        {
            Assert.AreEqual(12, TraitSeed.Cores.Count);
            Assert.AreEqual(12, TraitSeed.Flaws.Count);
            Assert.AreEqual(12, TraitSeed.Cores.Select(c => c.Id).Distinct().Count());
            Assert.AreEqual(12, TraitSeed.Flaws.Select(f => f.Id).Distinct().Count());
            Assert.IsTrue(TraitSeed.Cores.All(c => !string.IsNullOrEmpty(c.Id) && !string.IsNullOrEmpty(c.Name) && !string.IsNullOrEmpty(c.Summary) && !string.IsNullOrEmpty(c.Detail)));
        }

        [Test]
        public void EveryCoreHasThreeOptionsWithKnownFlaws()
        {
            var known = TraitSeed.Flaws.Select(f => f.Id).ToHashSet();
            foreach (var c in TraitSeed.Cores)
            {
                Assert.AreEqual(3, c.Options.Length, c.Id);
                foreach (var option in c.Options)
                    foreach (var id in option) Assert.IsTrue(known.Contains(id), $"{c.Id} offers unknown flaw {id}");
            }
        }

        [Test]
        public void HeavyCoresUsePairsAndOthersSingles()
        {
            foreach (var c in TraitSeed.Cores)
            {
                bool heavy = c.Id == "core.con_bac" || c.Id == "core.giu_goc";
                Assert.AreEqual(heavy, c.Heavy, c.Id);
                foreach (var option in c.Options)
                {
                    Assert.AreEqual(heavy ? 2 : 1, option.Length, c.Id);
                    Assert.AreEqual(option.Length, option.Distinct().Count(), $"{c.Id} repeats a flaw inside an option");
                }
            }
        }

        [Test]
        public void ReworkedCores_NewNamesSameIds()
        {
            var tham = TraitSeed.Cores.First(c => c.Id == "core.con_bac");
            var goc = TraitSeed.Cores.First(c => c.Id == "core.giu_goc");
            Assert.AreEqual("Dòng Máu Tham Lam", tham.Name);
            Assert.AreEqual("Dạ Dày Bất Diệt", goc.Name);
            Assert.IsTrue(tham.Implemented && goc.Implemented);
            Assert.IsTrue(tham.Detail.Contains("Giải Mã") && goc.Detail.Contains("Túi Không Đáy"));
        }

        [Test]
        public void FlawRulesFromSpec()
        {
            var bamDinh = TraitSeed.Cores.First(c => c.Id == "core.bam_dinh").Options.SelectMany(o => o).ToList();
            CollectionAssert.DoesNotContain(bamDinh, "flaw.bam_bun");
            CollectionAssert.DoesNotContain(bamDinh, "flaw.run_ray");
            var quangHop = TraitSeed.Cores.First(c => c.Id == "core.quang_hop").Options.SelectMany(o => o).ToList();
            CollectionAssert.AreEquivalent(new[] { "flaw.bam_bun", "flaw.vet_mui", "flaw.co_quan_mau_mon" }, quangHop);
            var congSinh = TraitSeed.Cores.First(c => c.Id == "core.cong_sinh").Options.SelectMany(o => o).ToList();
            CollectionAssert.DoesNotContain(congSinh, "flaw.ngai_dong");
            Assert.IsFalse(TraitSeed.Flaws.Any(f => f.Id.StartsWith("flaw.so_")), "the old 'Sợ-' flaws were dropped");
        }

        [Test]
        public void OnlySliceImplemented()
        {
            var cores = TraitSeed.Cores.Where(c => c.Implemented).Select(c => c.Id);
            CollectionAssert.AreEquivalent(new[] { "core.con_bac", "core.giu_goc", "core.quen_dat" }, cores);
            var flaws = TraitSeed.Flaws.Where(f => f.Implemented).Select(f => f.Id);
            CollectionAssert.AreEquivalent(new[] { "flaw.doi_som", "flaw.run_ray", "flaw.luong_cuong", "flaw.da_day_ken" }, flaws);
        }

        [Test]
        public void LuckBonusByTier()
        {
            Assert.AreEqual(0.20f, TraitTuning.LuckBonus(OrganTier.D), 1e-6f);
            Assert.AreEqual(0.16f, TraitTuning.LuckBonus(OrganTier.C), 1e-6f);
            Assert.AreEqual(0.12f, TraitTuning.LuckBonus(OrganTier.B), 1e-6f);
            Assert.AreEqual(0.08f, TraitTuning.LuckBonus(OrganTier.A), 1e-6f);
            Assert.AreEqual(0.04f, TraitTuning.LuckBonus(OrganTier.S), 1e-6f);
        }
    }
}
