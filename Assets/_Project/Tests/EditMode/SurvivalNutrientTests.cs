using Evolve.Survival;
using NUnit.Framework;
using UnityEngine;

namespace Evolve.Tests.EditMode
{
    public class SurvivalNutrientTests
    {
        SurvivalState s;
        SurvivalConfig cfg;

        [SetUp]
        public void SetUp()
        {
            cfg = ScriptableObject.CreateInstance<SurvivalConfig>();
            s = new SurvivalState();
            s.Reset();
        }

        [TearDown] public void TearDown() => Object.DestroyImmediate(cfg);

        void SetAll(float v) { for (int k = 0; k < NutrientKinds.Count; k++) s.Nutrients[k] = v; }

        [Test]
        public void FullStoresChangeNothing()
        {
            SetAll(100f);
            s.Health = 50f; s.Energy = 0f; s.Ambient = 30f; s.Temperature = 22f;
            s.Tick(2f, cfg, false, false);
            Assert.AreEqual(53f, s.Health, 1e-3f);        // 1.5 per second healing, as before the stores existed
            Assert.AreEqual(20f, s.Energy, 1e-3f);        // 10 per second resting
            Assert.AreEqual(25f, s.Temperature, 1e-3f);   // 1.5 degrees per second
        }

        [Test]
        public void FeedWithProfileRaisesMatchingStores()
        {
            SetAll(0f);
            s.Hunger = 50f;
            s.Feed(10f, FoodProfiles.For(FoodKind.PreyMeat));
            Assert.AreEqual(10f, s.Nutrient(NutrientKind.Protein), 1e-4f);
            Assert.AreEqual(0f, s.Nutrient(NutrientKind.Calcium), 1e-4f);
            Assert.AreEqual(4f, s.Nutrient(NutrientKind.Iron), 1e-4f);
            Assert.AreEqual(3f, s.Nutrient(NutrientKind.Fat), 1e-4f);
            Assert.AreEqual(0f, s.Nutrient(NutrientKind.Vitamin), 1e-4f);
            Assert.AreEqual(60f, s.Hunger, 1e-4f);
        }

        [Test]
        public void FeedClampsAtMax()
        {
            SetAll(95f);
            s.Feed(100f, FoodProfiles.For(FoodKind.PredatorMeat));
            for (int k = 0; k < NutrientKinds.Count; k++) Assert.LessOrEqual(s.Nutrients[k], SurvivalState.MaxNutrient);
        }

        [Test]
        public void PlainFeedUsesMixed()
        {
            SetAll(0f);
            s.Feed(10f);
            for (int k = 0; k < NutrientKinds.Count; k++) Assert.AreEqual(2.5f, s.Nutrients[k], 1e-4f);
        }

        [Test]
        public void StoresDecayWithOrganLoad()
        {
            SetAll(100f);
            s.NutrientLoad[(int)NutrientKind.Protein] = 2;
            s.Tick(10f, cfg, false, false);
            Assert.AreEqual(100f - (0.04f + 2 * 0.03f) * 10f, s.Nutrient(NutrientKind.Protein), 1e-3f);
            Assert.AreEqual(100f - 0.04f * 10f, s.Nutrient(NutrientKind.Calcium), 1e-3f);
        }

        [Test]
        public void LowProteinSlowsHealing()
        {
            SetAll(100f);
            s.Nutrients[(int)NutrientKind.Protein] = 0f;
            s.Health = 50f;
            s.Tick(10f, cfg, false, false);
            Assert.AreEqual(56f, s.Health, 1e-3f); // 1.5 * 0.4 * 10
        }

        [Test]
        public void LowCalciumRaisesDamageButNotStarvation()
        {
            s.Config = cfg;
            SetAll(100f);
            s.Nutrients[(int)NutrientKind.Calcium] = 0f;
            s.Damage(10f);
            Assert.AreEqual(86f, s.Health, 1e-3f);  // 10 * 1.4
            Assert.AreEqual(5f, s.Stress, 1e-3f);   // stress follows the blow, not the multiplier

            s.Reset();
            SetAll(100f);
            s.Nutrients[(int)NutrientKind.Calcium] = 0f;
            s.Hunger = 0f;
            s.Tick(1f, cfg, false, false);
            Assert.AreEqual(96f, s.Health, 1e-3f);  // starvation damage is not multiplied
        }

        [Test]
        public void LowIronCapsHealthAndEnergyRegen()
        {
            SetAll(100f);
            s.Nutrients[(int)NutrientKind.Iron] = 0f;
            s.Energy = 0f;
            s.Tick(2f, cfg, false, false);
            Assert.AreEqual(70f, s.Health, 1e-3f);  // 100 * 0.7
            Assert.AreEqual(12f, s.Energy, 1e-3f);  // 10 * 0.6 * 2
        }

        [Test]
        public void LowFatSpeedsTemperatureDrift()
        {
            SetAll(100f);
            s.Nutrients[(int)NutrientKind.Fat] = 0f;
            s.Ambient = 30f; s.Temperature = 22f;
            s.Tick(1f, cfg, false, false);
            Assert.AreEqual(24.7f, s.Temperature, 1e-3f); // 1.5 * 1.8
        }

        [Test]
        public void ResetRefillsNutrients()
        {
            SetAll(0f);
            s.Reset();
            for (int k = 0; k < NutrientKinds.Count; k++) Assert.AreEqual(80f, s.Nutrients[k], 1e-4f);
        }

        [Test]
        public void ReviveGivesNutrientFloor()
        {
            SetAll(10f);
            s.Health = 0f; s.Lives = 0;
            s.Revive(1f);
            for (int k = 0; k < NutrientKinds.Count; k++) Assert.AreEqual(50f, s.Nutrients[k], 1e-4f);
        }

        [Test]
        public void LowestNutrientPicksSmallest()
        {
            SetAll(60f);
            s.Nutrients[(int)NutrientKind.Iron] = 12f;
            Assert.AreEqual(NutrientKind.Iron, s.LowestNutrient());
        }

        [Test]
        public void LackingAndWarningThresholds()
        {
            s.Config = cfg;
            SetAll(100f);
            s.Nutrients[(int)NutrientKind.Fat] = 39f;
            s.Nutrients[(int)NutrientKind.Calcium] = 24f;
            Assert.IsTrue(s.IsLacking(NutrientKind.Fat));
            Assert.IsFalse(s.IsWarning(NutrientKind.Fat));
            Assert.IsTrue(s.IsLacking(NutrientKind.Calcium));
            Assert.IsTrue(s.IsWarning(NutrientKind.Calcium));
            Assert.IsFalse(s.IsLacking(NutrientKind.Iron));
        }
    }
}
