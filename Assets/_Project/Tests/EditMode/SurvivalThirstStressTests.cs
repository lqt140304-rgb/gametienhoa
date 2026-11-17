using Evolve.Survival;
using NUnit.Framework;
using UnityEngine;

namespace Evolve.Tests.EditMode
{
    public class SurvivalThirstStressTests
    {
        SurvivalConfig cfg;
        SurvivalState s;

        [SetUp]
        public void SetUp()
        {
            cfg = ScriptableObject.CreateInstance<SurvivalConfig>();
            s = new SurvivalState { Ambient = 22f, Config = cfg };
            s.Reset();
            s.ResetLives();
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(cfg);

        [Test]
        public void NoThirstWhenDisabled()
        {
            s.Tick(100f, cfg, false, false);
            Assert.AreEqual(100f, s.Thirst, 0.001f);
        }

        [Test]
        public void ThirstDecaysWhenEnabled()
        {
            s.ThirstEnabled = true;
            s.Tick(10f, cfg, false, false);
            Assert.AreEqual(100f - 3.5f, s.Thirst, 0.001f);
        }

        [Test]
        public void SprintAndHeatSpeedThirst()
        {
            s.ThirstEnabled = true;
            s.Tick(10f, cfg, false, true);
            Assert.AreEqual(100f - 13.5f, s.Thirst, 0.001f);

            s.Thirst = 100f;
            s.Ambient = 34f;
            s.Temperature = 34f;
            s.Tick(10f, cfg, false, false);
            Assert.AreEqual(100f - 6.5f, s.Thirst, 0.001f);
        }

        [Test]
        public void DrinkRefillsAndClamps()
        {
            s.Thirst = 10f;
            s.Drink(1000f);
            Assert.AreEqual(100f, s.Thirst, 0.001f);
            s.Thirst = 40f;
            s.Drink(-5f);
            Assert.AreEqual(40f, s.Thirst, 0.001f);
        }

        [Test]
        public void DehydrationDamagesAtZero()
        {
            s.ThirstEnabled = true;
            s.Thirst = 0f;
            s.Tick(1f, cfg, false, false);
            Assert.AreEqual(100f - 3f, s.Health, 0.001f);
        }

        [Test]
        public void NoHealWhenThirsty()
        {
            s.ThirstEnabled = true;
            s.Hunger = 100f;
            s.Thirst = 20f;
            s.Health = 50f;
            s.Tick(1f, cfg, false, false);
            Assert.AreEqual(50f, s.Health, 0.001f);
        }

        [Test]
        public void HitRaisesStressButStarvationDoesNot()
        {
            s.Damage(10f);
            Assert.AreEqual(5f, s.Stress, 0.001f);

            s.Reset();
            s.Hunger = 0f;
            s.Tick(1f, cfg, false, false);
            Assert.Less(s.Health, 100f);
            Assert.AreEqual(cfg.StressLowNeedsPerSec, s.Stress, 0.001f);
        }

        [Test]
        public void StressRisesWhenNeedsLowAndFallsInShelter()
        {
            s.Hunger = 10f;
            s.Tick(10f, cfg, false, false);
            Assert.AreEqual(4f, s.Stress, 0.001f);

            s.InShelter = true;
            s.Tick(1f, cfg, false, false);
            // +0.4 low needs - 2 shelter = -1.6
            Assert.AreEqual(2.4f, s.Stress, 0.001f);
        }

        [Test]
        public void StressAtMaxHasNoDirectDamage()
        {
            s.Stress = 100f;
            s.Tick(10f, cfg, false, false);
            Assert.AreEqual(100f, s.Health, 0.001f);
        }

        [Test]
        public void StressEffectCurves()
        {
            Assert.AreEqual(0f, StressEffects.CameraShake(30f, cfg), 0.0001f);
            Assert.AreEqual(0.05f, StressEffects.CameraShake(100f, cfg), 0.0001f);
            Assert.AreEqual(1f, StressEffects.SenseRadiusMultiplier(60f, cfg), 0.0001f);
            Assert.AreEqual(0.6f, StressEffects.SenseRadiusMultiplier(100f, cfg), 0.0001f);
            Assert.AreEqual(1f, StressEffects.EnergyRegenMultiplier(79f, cfg), 0.0001f);
            Assert.AreEqual(0.5f, StressEffects.EnergyRegenMultiplier(80f, cfg), 0.0001f);
        }

        [Test]
        public void ShelterBoostsEnergyRegen()
        {
            s.Energy = 0f;
            s.Tick(1f, cfg, false, false);
            float normal = s.Energy;
            s.Energy = 0f;
            s.InShelter = true;
            s.Tick(1f, cfg, false, false);
            Assert.AreEqual(normal * 2f, s.Energy, 0.001f);
        }

        [Test]
        public void ResetRestoresThirstAndStress()
        {
            s.Thirst = 0f;
            s.Stress = 90f;
            s.Reset();
            Assert.AreEqual(100f, s.Thirst, 0.001f);
            Assert.AreEqual(0f, s.Stress, 0.001f);
        }
    }
}
