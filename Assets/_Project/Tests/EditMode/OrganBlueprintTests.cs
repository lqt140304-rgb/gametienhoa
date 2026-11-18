using Evolve.Organs;
using NUnit.Framework;
using UnityEngine;

namespace Evolve.Tests.EditMode
{
    public class OrganBlueprintTests
    {
        [Test]
        public void ResimulateCost_EqualsAbsorbExp()
        {
            foreach (OrganTier t in System.Enum.GetValues(typeof(OrganTier)))
                Assert.AreEqual(OrganTiers.AbsorbExp(t), OrganRules.ResimulateCost(t));
        }

        [Test]
        public void Book_LearnTwiceReturnsFalseSecondTime()
        {
            var b = new BlueprintBook();
            Assert.IsTrue(b.Learn("Eye"));
            Assert.IsFalse(b.Learn("Eye"));
            Assert.IsTrue(b.Knows("Eye"));
            Assert.IsFalse(b.Knows("Leg"));
        }

        [Test]
        public void Book_NullOrEmptyIgnored()
        {
            var b = new BlueprintBook();
            Assert.IsFalse(b.Learn(null));
            Assert.IsFalse(b.Learn(""));
            Assert.AreEqual(0, b.Names.Count);
        }

        static (GameObject go, OrganHost host, Evolve.Survival.SurvivalStats stats, OrganSO organ) Body()
        {
            var go = new GameObject("body", typeof(Evolve.Survival.SurvivalStats), typeof(OrganHost));
            var organ = ScriptableObject.CreateInstance<OrganSO>();
            organ.name = "TestOrgan"; organ.Slot = OrganSlot.Legs; organ.GrowthExpRequired = 0f;
            var stats = go.GetComponent<Evolve.Survival.SurvivalStats>();
            stats.State.Reset();
            for (int k = 0; k < stats.State.Nutrients.Length; k++) stats.State.Nutrients[k] = 100f;
            return (go, go.GetComponent<OrganHost>(), stats, organ);
        }

        [Test]
        public void TryAttach_LearnsBlueprint_AndKeepsItAfterDetach()
        {
            var (go, host, _, organ) = Body();
            Assert.IsTrue(host.TryAttach(organ));
            host.Detach(organ.Slot);
            Assert.IsTrue(host.Knows(organ));
            Object.DestroyImmediate(go); Object.DestroyImmediate(organ);
        }

        [Test]
        public void Resimulate_UnknownRefused()
        {
            var (go, host, _, organ) = Body();
            Assert.AreEqual(ResimulateResult.Unknown, host.TryResimulate(organ));
            Object.DestroyImmediate(go); Object.DestroyImmediate(organ);
        }

        [Test]
        public void Resimulate_NoExpRefusedAndExpUnchanged()
        {
            var (go, host, stats, organ) = Body();
            host.Learn(organ);
            stats.State.EvolutionExp = 1f;
            Assert.AreEqual(ResimulateResult.NoExp, host.TryResimulate(organ));
            Assert.AreEqual(1f, stats.State.EvolutionExp);
            Assert.IsFalse(host.Has(organ));
            Object.DestroyImmediate(go); Object.DestroyImmediate(organ);
        }

        [Test]
        public void Resimulate_OkSpendsCostAndAttaches()
        {
            var (go, host, stats, organ) = Body();
            host.Learn(organ);
            float cost = OrganRules.ResimulateCost(organ.Tier);
            stats.State.EvolutionExp = cost + 5f;
            Assert.AreEqual(ResimulateResult.Ok, host.TryResimulate(organ));
            Assert.AreEqual(5f, stats.State.EvolutionExp, 1e-4f);
            Assert.IsTrue(host.Has(organ));
            Object.DestroyImmediate(go); Object.DestroyImmediate(organ);
        }

    }
}
