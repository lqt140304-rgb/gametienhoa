using System.Collections.Generic;
using Evolve.Organs;
using Evolve.Swallow;
using NUnit.Framework;
using UnityEngine;

namespace Evolve.Tests.EditMode
{
    public class AnalysisBookTests
    {
        readonly List<Object> made = new List<Object>();

        OrganSO Organ(string n, OrganTier tier)
        {
            var o = ScriptableObject.CreateInstance<OrganSO>();
            o.name = n; o.Tier = tier; o.Slot = OrganSlot.Legs;
            made.Add(o);
            return o;
        }

        [TearDown] public void Clean() { foreach (var o in made) Object.DestroyImmediate(o); made.Clear(); }

        static List<Gene> One(OrganSO o) => new List<Gene> { new Gene(o, AnalysisRules.FromTier(o.Tier)) };

        [Test]
        public void Genome_SkipsLegendaryUnlessLeader()
        {
            var t = ScriptableObject.CreateInstance<DropTableSO>(); made.Add(t);
            var c = Organ("C", OrganTier.C); var s = Organ("S", OrganTier.S);
            t.Organs.Add(c); t.Organs.Add(s); t.Organs.Add(null); t.Organs.Add(c);
            Assert.AreEqual(1, Genome.From(t, false).Count);
            Assert.AreEqual(2, Genome.From(t, true).Count);
            Assert.AreEqual(0, Genome.From(null, true).Count);
        }

        [Test]
        public void Submit_LearnsOnLuckyRoll()
        {
            var o = Organ("A", OrganTier.C); var book = new AnalysisBook();
            var r = book.Submit(One(o), 1f, () => 0f);
            Assert.AreEqual(GeneOutcome.Learned, r[0].Outcome);
            Assert.AreEqual(1, book.StarsOf(o));
            Assert.IsTrue(book.Knows(o));
        }

        [Test]
        public void Submit_PityAccumulatesAcrossCalls()
        {
            var o = Organ("A", OrganTier.C); var book = new AnalysisBook();
            for (int i = 0; i < 6; i++) Assert.AreEqual(GeneOutcome.Progress, book.Submit(One(o), 1f, () => 0.999f)[0].Outcome);
            Assert.AreEqual(GeneOutcome.Learned, book.Submit(One(o), 1f, () => 0.999f)[0].Outcome);
            Assert.AreEqual(7, book.AnalysesDone);
        }

        [Test]
        public void Submit_StarUpAt3And9_ThenFoodOnly()
        {
            var o = Organ("A", OrganTier.C); var book = new AnalysisBook();
            book.Submit(One(o), 1f, () => 0f);
            GeneResult last = default;
            for (int i = 1; i <= 3; i++) last = book.Submit(One(o), 1f, () => 0f)[0];
            Assert.AreEqual(GeneOutcome.StarUp, last.Outcome); Assert.AreEqual(2, last.Stars);
            for (int i = 4; i <= 9; i++) last = book.Submit(One(o), 1f, () => 0f)[0];
            Assert.AreEqual(GeneOutcome.StarUp, last.Outcome); Assert.AreEqual(3, last.Stars);
            Assert.AreEqual(GeneOutcome.FoodOnly, book.Submit(One(o), 1f, () => 0f)[0].Outcome);
        }

        [Test]
        public void Submit_QualityScalesPity()
        {
            var o = Organ("A", OrganTier.C); var book = new AnalysisBook();
            var r = book.Submit(One(o), 0.5f, () => 0.999f)[0];
            Assert.AreEqual(0.075f, book.PityOf(o), 1e-5f);
            Assert.AreEqual(0.075f, r.Pity, 1e-5f);
        }

        [Test]
        public void Clear_ForgetsEverything()
        {
            var o = Organ("A", OrganTier.C); var book = new AnalysisBook();
            book.Submit(One(o), 1f, () => 0f);
            book.Clear();
            Assert.IsFalse(book.Knows(o)); Assert.AreEqual(0, book.AnalysesDone);
        }
    }
}
