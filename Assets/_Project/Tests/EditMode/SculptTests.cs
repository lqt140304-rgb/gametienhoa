using System.Collections.Generic;
using Evolve.Organs;
using Evolve.Survival;
using Evolve.Swallow;
using NUnit.Framework;
using UnityEngine;

namespace Evolve.Tests.EditMode
{
    /// <summary>Biến Hình: organs learned by Giải Mã, sculpted from body mass, only in a shelter.</summary>
    public class SculptTests
    {
        GameObject go;
        OrganHost host;
        SlimeBody body;
        Swallower swallower;
        readonly List<Object> made = new List<Object>();

        [SetUp]
        public void Make()
        {
            go = new GameObject("body", typeof(SurvivalStats), typeof(SlimeBody), typeof(Swallower), typeof(OrganHost));
            var stats = go.GetComponent<SurvivalStats>();
            stats.State.Reset();
            for (int k = 0; k < stats.State.Nutrients.Length; k++) stats.State.Nutrients[k] = 100f;
            host = go.GetComponent<OrganHost>();
            body = go.GetComponent<SlimeBody>();
            swallower = go.GetComponent<Swallower>();
            body.SetMass(10f);
        }

        [TearDown]
        public void Clean()
        {
            Object.DestroyImmediate(go);
            foreach (var o in made) Object.DestroyImmediate(o);
            made.Clear();
        }

        OrganSO Organ(string n, OrganTier tier, OrganSlot slot = OrganSlot.Legs)
        {
            var o = ScriptableObject.CreateInstance<OrganSO>();
            o.name = n; o.DisplayName = n; o.Tier = tier; o.Slot = slot;
            made.Add(o);
            return o;
        }

        void Learn(OrganSO o, int extraSamples = 0)
        {
            var genes = new List<Gene> { new Gene(o, AnalysisRules.FromTier(o.Tier)) };
            for (int i = 0; i <= extraSamples; i++) swallower.Book.Submit(genes, 1f, () => 0f);
        }

        [Test]
        public void TrySculpt_OutsideShelter_NotInShelter()
        {
            var o = Organ("Leg", OrganTier.C); Learn(o);
            Assert.AreEqual(SculptResult.NotInShelter, host.TrySculpt(o, swallower.Book, body, false));
            Assert.IsFalse(host.Has(o));
        }

        [Test]
        public void TrySculpt_Unknown_Unknown()
        {
            var o = Organ("Leg", OrganTier.C);
            Assert.AreEqual(SculptResult.Unknown, host.TrySculpt(o, swallower.Book, body, true));
        }

        [Test]
        public void TrySculpt_Ok_SpendsMass_LevelEqualsStars()
        {
            var o = Organ("Leg", OrganTier.C); Learn(o);
            Assert.AreEqual(SculptResult.Ok, host.TrySculpt(o, swallower.Book, body, true));
            Assert.AreEqual(8f, body.Mass, 1e-4f);
            Assert.AreEqual(1, host.GetLevel(o));
        }

        [Test]
        public void TrySculpt_NoMass_KeepsMass()
        {
            body.SetMass(6f);
            var o = Organ("Leg", OrganTier.B); Learn(o);
            Assert.AreEqual(SculptResult.NoMass, host.TrySculpt(o, swallower.Book, body, true));
            Assert.AreEqual(6f, body.Mass, 1e-4f);
            Assert.IsFalse(host.Has(o));
        }

        [Test]
        public void TrySculpt_ReplacesSlot_RefundsOld()
        {
            var a = Organ("LegA", OrganTier.B); var b = Organ("LegB", OrganTier.C);
            Learn(a); Learn(b);
            host.TrySculpt(a, swallower.Book, body, true);          // 10 - 4 = 6
            Assert.AreEqual(SculptResult.Ok, host.TrySculpt(b, swallower.Book, body, true)); // 6 + 2 - 2 = 6
            Assert.AreEqual(6f, body.Mass, 1e-4f);
            Assert.IsTrue(host.Has(b)); Assert.IsFalse(host.Has(a));
        }

        [Test]
        public void Remove_RefundsHalf_OnlyInShelter()
        {
            var o = Organ("Leg", OrganTier.B); Learn(o);
            host.TrySculpt(o, swallower.Book, body, true);           // 6
            Assert.IsFalse(host.Remove(o.Slot, body, false));
            Assert.IsTrue(host.Remove(o.Slot, body, true));          // +2
            Assert.AreEqual(8f, body.Mass, 1e-4f);
            Assert.IsTrue(host.Knows(o));
        }

        [Test]
        public void SyncStars_RaisesLevel()
        {
            var o = Organ("Leg", OrganTier.C); Learn(o);
            host.TrySculpt(o, swallower.Book, body, true);
            Learn(o, 2);                                             // 3 more samples → ★2
            host.SyncStars(swallower.Book);
            Assert.AreEqual(2, host.GetLevel(o));
        }

        [Test]
        public void TryAttach_LackNutrient_StillRefused()
        {
            var stats = go.GetComponent<SurvivalStats>();
            stats.State.Config = ScriptableObject.CreateInstance<SurvivalConfig>(); made.Add(stats.State.Config);
            var eyes = Organ("Eye", OrganTier.C, OrganSlot.Eyes);
            stats.State.Nutrients[(int)NutrientKind.Vitamin] = 10f;
            Learn(eyes);
            Assert.AreEqual(SculptResult.Blocked, host.TrySculpt(eyes, swallower.Book, body, true));
        }
    }
}
