using Evolve.Survival;
using NUnit.Framework;
using UnityEngine;

namespace Evolve.Tests.EditMode
{
    public class SurvivalDiseaseTests
    {
        SurvivalState s;
        SurvivalConfig cfg;

        [SetUp]
        public void SetUp()
        {
            cfg = ScriptableObject.CreateInstance<SurvivalConfig>();
            s = new SurvivalState();
            s.Reset();
            s.Roll = () => 0f; // every roll hits unless a test says otherwise
        }

        [TearDown] public void TearDown() => Object.DestroyImmediate(cfg);

        [Test]
        public void HealthyChangesNothing()
        {
            s.Health = 50f; s.Energy = 0f; s.Ambient = 30f; s.Temperature = 22f;
            s.Tick(2f, cfg, false, false);
            Assert.AreEqual(53f, s.Health, 1e-3f);
            Assert.AreEqual(20f, s.Energy, 1e-3f);
            Assert.AreEqual(25f, s.Temperature, 1e-3f);
            Assert.AreEqual(0, s.DiseaseMask);
        }

        [Test]
        public void InfectAndMask()
        {
            s.Infect(DiseaseKind.StagnantWater);
            s.Infect(DiseaseKind.SporeBlur);
            Assert.AreEqual(180f, s.DiseaseTime[(int)DiseaseKind.StagnantWater], 1e-4f);
            Assert.AreEqual(150f, s.DiseaseTime[(int)DiseaseKind.SporeBlur], 1e-4f);
            Assert.AreEqual(5, s.DiseaseMask); // bits 0 and 2
            s.SetDiseaseMask(2);
            Assert.IsTrue(s.Has(DiseaseKind.RottenGel));
            Assert.IsFalse(s.Has(DiseaseKind.StagnantWater));
            Assert.IsFalse(s.Has(DiseaseKind.SporeBlur));
        }

        [Test]
        public void TryInfectUsesRollAndVitamin()
        {
            s.Roll = () => 0.5f;
            Assert.IsFalse(s.TryInfect(DiseaseKind.SporeBlur, 0.3f));          // 0.5 >= 0.3
            s.Nutrients[(int)NutrientKind.Vitamin] = 0f;                        // vitamin 0: chance x3 = 0.9
            Assert.IsTrue(s.TryInfect(DiseaseKind.SporeBlur, 0.3f));
            Assert.IsTrue(s.Has(DiseaseKind.SporeBlur));
            Assert.IsFalse(s.TryInfect(DiseaseKind.SporeBlur, 0.3f));          // already sick
            s.Health = 0f;
            Assert.IsFalse(s.TryInfect(DiseaseKind.RottenGel, 1f));            // dead
        }

        [Test]
        public void StagnantWaterDrainsThirstAndSlowsHealing()
        {
            s.Infect(DiseaseKind.StagnantWater);
            s.ThirstEnabled = true;
            s.Health = 50f;
            s.Tick(10f, cfg, false, false);
            Assert.AreEqual(100f - 0.35f * 1.6f * 10f, s.Thirst, 1e-3f);   // 94.4
            Assert.AreEqual(50f + 1.5f * 0.6f * 10f, s.Health, 1e-3f);      // 59
        }

        [Test]
        public void RottenGelStopsHealingAndDrainsHealthButNotBelowFloor()
        {
            s.Infect(DiseaseKind.RottenGel);
            s.Health = 11f;
            for (int i = 0; i < 40; i++) { s.Tick(1f, cfg, false, false); s.Infect(DiseaseKind.RottenGel); }
            Assert.IsFalse(s.IsDead);
            Assert.GreaterOrEqual(s.Health, 9.5f);
            Assert.LessOrEqual(s.Health, 10.3f);
        }

        [Test]
        public void RottenGelDoesNotKill()
        {
            s.Health = 100f;
            for (int i = 0; i < 1200; i++)
            {
                if (!s.Has(DiseaseKind.RottenGel)) s.Infect(DiseaseKind.RottenGel);
                s.Tick(1f, cfg, false, false);
            }
            Assert.IsFalse(s.IsDead);
            Assert.GreaterOrEqual(s.Health, 9.5f);
        }

        [Test]
        public void SporeBlurRaisesStress()
        {
            s.Infect(DiseaseKind.SporeBlur);
            s.Tick(10f, cfg, false, false);
            Assert.AreEqual((0.5f - 0.3f) * 10f, s.Stress, 1e-3f); // +0.5 from spores, -0.3 calm
        }

        [Test]
        public void DiseasesRunOutByThemselves()
        {
            s.Infect(DiseaseKind.StagnantWater); s.Infect(DiseaseKind.RottenGel); s.Infect(DiseaseKind.SporeBlur);
            for (int i = 0; i < 300; i++) s.Tick(1f, cfg, false, false);
            Assert.AreEqual(0, s.DiseaseMask);
            Assert.IsFalse(s.IsDead);
        }

        [Test]
        public void ShelterAndCleanWaterSpeedRecovery()
        {
            s.Infect(DiseaseKind.StagnantWater);
            s.InShelter = true; s.InCleanWater = true;
            s.Tick(10f, cfg, false, false);
            Assert.AreEqual(180f - 6f * 10f, s.DiseaseTime[(int)DiseaseKind.StagnantWater], 1e-3f);

            s.Reset();
            s.Infect(DiseaseKind.RottenGel);
            s.InShelter = true; s.InCleanWater = true;
            s.Tick(10f, cfg, false, false);
            Assert.AreEqual(240f - 2f * 10f, s.DiseaseTime[(int)DiseaseKind.RottenGel], 1e-3f); // clean water does not help gel rot
        }

        [Test]
        public void VitaminCuresRottenGel()
        {
            s.Infect(DiseaseKind.RottenGel);
            s.Feed(10f, new FoodProfile(0f, 0f, 0f, 0f, 1f));
            Assert.AreEqual(240f - 60f, s.DiseaseTime[(int)DiseaseKind.RottenGel], 1e-3f);
        }

        [Test]
        public void ExternalHitCanInfect()
        {
            s.Damage(5f);
            Assert.IsTrue(s.Has(DiseaseKind.RottenGel));
            s.Reset();
            s.Damage(2f);
            Assert.IsFalse(s.Has(DiseaseKind.RottenGel));
        }

        [Test]
        public void ResetClearsDiseases()
        {
            s.Infect(DiseaseKind.StagnantWater); s.Infect(DiseaseKind.RottenGel); s.Infect(DiseaseKind.SporeBlur);
            s.Reset();
            Assert.AreEqual(0, s.DiseaseMask);
        }
    }
}
