using Evolve.Organs;
using Evolve.Survival;
using NUnit.Framework;
using UnityEngine;

namespace Evolve.Tests.EditMode
{
    public class OrganNutritionTests
    {
        const float Equip = 40f, Warn = 25f, Dissolve = 300f, Recover = 600f;

        [Test]
        public void DefaultNutrientBySlot()
        {
            Assert.AreEqual(NutrientKind.Vitamin, OrganNutrients.DefaultFor(OrganSlot.Eyes));
            Assert.AreEqual(NutrientKind.Protein, OrganNutrients.DefaultFor(OrganSlot.Legs));
            Assert.AreEqual(NutrientKind.Fat, OrganNutrients.DefaultFor(OrganSlot.Skin));
            Assert.AreEqual(NutrientKind.Calcium, OrganNutrients.DefaultFor(OrganSlot.Back));
        }

        [Test]
        public void AutoVersusExplicit()
        {
            var organ = ScriptableObject.CreateInstance<OrganSO>();
            organ.Slot = OrganSlot.Back;
            Assert.AreEqual(NutrientKind.Calcium, organ.EffectiveNutrient);        // Auto: by slot
            organ.NutrientNeed = OrganNutrient.Iron;
            Assert.AreEqual(NutrientKind.Iron, organ.EffectiveNutrient);           // explicit wins
            organ.NutrientNeed = OrganNutrient.Vitamin;
            Assert.AreEqual(NutrientKind.Vitamin, organ.EffectiveNutrient);
            Object.DestroyImmediate(organ);
        }

        [Test]
        public void OrganDecayStepsAndRecovers()
        {
            Assert.AreEqual(0.1f, OrganDecay.Step(0f, 10f, Equip, Warn, Dissolve, Recover, 30f), 1e-5f);   // starved: 30/300
            Assert.AreEqual(0.4f, OrganDecay.Step(0.5f, 50f, Equip, Warn, Dissolve, Recover, 60f), 1e-5f); // fed: -60/600
            Assert.AreEqual(0.5f, OrganDecay.Step(0.5f, 30f, Equip, Warn, Dissolve, Recover, 60f), 1e-5f); // between: unchanged
            Assert.AreEqual(1f, OrganDecay.Step(0.95f, 0f, Equip, Warn, Dissolve, Recover, 100f), 1e-5f);  // clamped
            Assert.AreEqual(0f, OrganDecay.Step(0.01f, 100f, Equip, Warn, Dissolve, Recover, 100f), 1e-5f);
        }

        [Test]
        public void NeverDissolvesWhileSupplied()
        {
            float decay = 0f;
            for (int i = 0; i < 3600; i++) decay = OrganDecay.Step(decay, 40f, Equip, Warn, Dissolve, Recover, 1f);
            Assert.AreEqual(0f, decay, 1e-6f);
        }

        [Test]
        public void StrengthFactorBounds()
        {
            Assert.AreEqual(1f, OrganDecay.StrengthFactor(0f), 1e-6f);
            Assert.AreEqual(0.2f, OrganDecay.StrengthFactor(1f), 1e-6f);
        }

        [Test]
        public void LackNutrientBlocksOnlyAttach()
        {
            var go = new GameObject("Slime", typeof(SurvivalStats), typeof(OrganHost));
            var stats = go.GetComponent<SurvivalStats>();
            var host = go.GetComponent<OrganHost>();
            var eyes = ScriptableObject.CreateInstance<OrganSO>();
            eyes.Slot = OrganSlot.Eyes; // needs Tinh Chất Sống by default
            try
            {
                stats.State.Config = ScriptableObject.CreateInstance<SurvivalConfig>();
                stats.State.Nutrients[(int)NutrientKind.Vitamin] = 10f;
                Assert.AreEqual(AttachCheck.LackNutrient, host.Check(eyes));
                Assert.IsFalse(host.TryAttach(eyes));

                stats.State.Nutrients[(int)NutrientKind.Vitamin] = 80f;
                Assert.AreEqual(AttachCheck.Ok, host.Check(eyes));
            }
            finally
            {
                Object.DestroyImmediate(stats.State.Config);
                Object.DestroyImmediate(eyes);
                Object.DestroyImmediate(go);
            }
        }
    }
}
