using System.Collections.Generic;
using Evolve.Organs;
using Evolve.Survival;
using Evolve.Swallow;
using NUnit.Framework;
using UnityEngine;

namespace Evolve.Tests.EditMode
{
    public class StomachTests
    {
        OrganSO organ;
        [SetUp] public void Make() { organ = ScriptableObject.CreateInstance<OrganSO>(); organ.name = "Eye"; organ.Tier = OrganTier.C; }
        [TearDown] public void Clean() => Object.DestroyImmediate(organ);

        StomachItem Sample() => new StomachItem { Label = "Nhện", Mass = 3f, Nutrition = 15f, Food = FoodKind.PreyMeat, Quality = 1f, Genes = new List<Gene> { new Gene(organ, GeneRarity.Common) } };
        static StomachItem Food() => new StomachItem { Label = "Rêu", Mass = 1f, Nutrition = 5f, Food = FoodKind.GlowMoss };

        readonly List<StomachItem> analyzed = new List<StomachItem>(), digested = new List<StomachItem>();
        void Tick(Stomach s, float seconds, bool shelter) { analyzed.Clear(); digested.Clear(); s.Tick(seconds, shelter, analyzed, digested); }

        [Test]
        public void TryAdd_Full_ReturnsFalse()
        {
            var s = new Stomach(2);
            Assert.IsTrue(s.TryAdd(Food())); Assert.IsTrue(s.TryAdd(Food())); Assert.IsFalse(s.TryAdd(Food()));
            Assert.AreEqual(2, s.Items.Count); Assert.IsTrue(s.IsFull);
        }

        [Test]
        public void DefaultModes()
        {
            var s = new Stomach();
            s.TryAdd(Sample()); s.TryAdd(Food());
            Assert.AreEqual(StomachMode.Analyze, s.Items[0].Mode);
            Assert.AreEqual(StomachMode.Digest, s.Items[1].Mode);
            Assert.AreEqual(Stomach.DefaultCapacity, s.Capacity);
        }

        [Test]
        public void Analyze_FourTimesFasterInShelter()
        {
            var a = new Stomach(); a.TryAdd(Sample());
            Tick(a, 20f, true);
            Assert.AreEqual(1, analyzed.Count);
            var b = new Stomach(); b.TryAdd(Sample());
            Tick(b, 20f, false);
            Assert.AreEqual(0, analyzed.Count);
            Assert.AreEqual(0.25f, b.Items[0].Progress, 1e-4f);
        }

        [Test]
        public void Analyzed_ThenDigests()
        {
            var s = new Stomach(); s.TryAdd(Sample());
            Tick(s, 20f, true);
            Assert.AreEqual(StomachMode.Digest, s.Items[0].Mode);
            Assert.AreEqual(0f, s.Items[0].Progress, 1e-4f);
            Tick(s, Stomach.DigestSeconds, true);
            Assert.AreEqual(1, digested.Count);
            Assert.AreEqual(0, s.Items.Count);
        }

        [Test]
        public void Keep_NeverProgresses()
        {
            var s = new Stomach(); s.TryAdd(Sample()); s.SetMode(0, StomachMode.Keep);
            Tick(s, 1000f, true);
            Assert.AreEqual(1, s.Items.Count); Assert.AreEqual(0f, s.Items[0].Progress);
        }

        [Test]
        public void Analyze_OnFood_FallsBackToDigest()
        {
            var s = new Stomach(); s.TryAdd(Food()); s.SetMode(0, StomachMode.Analyze);
            Assert.AreEqual(StomachMode.Digest, s.Items[0].Mode);
        }

        [Test]
        public void Spit_RemovesItem()
        {
            var s = new Stomach(); s.TryAdd(Food()); s.TryAdd(Sample());
            var out1 = s.Spit(1);
            Assert.AreEqual("Nhện", out1.Label); Assert.AreEqual(1, s.Items.Count);
            Assert.IsNull(s.Spit(5));
        }

        [Test]
        public void AnalysisBook_SurvivesStomachClear()
        {
            var s = new Stomach(); var book = new AnalysisBook();
            s.TryAdd(Sample());
            Tick(s, 20f, true);
            foreach (var it in analyzed) book.Submit(it.Genes, it.Quality, () => 0f);
            s.Clear();
            Assert.AreEqual(0, s.Items.Count);
            Assert.IsTrue(book.Knows(organ));
        }
    }
}
