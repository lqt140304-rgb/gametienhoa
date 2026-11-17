using System;
using Evolve.Survival;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    public class DiseaseRulesTests
    {
        [Test]
        public void NamesAndCuresExist()
        {
            var names = new System.Collections.Generic.HashSet<string>();
            foreach (DiseaseKind k in Enum.GetValues(typeof(DiseaseKind)))
            {
                Assert.IsNotEmpty(DiseaseRules.Name(k));
                Assert.IsNotEmpty(DiseaseRules.Cure(k));
                names.Add(DiseaseRules.Name(k));
            }
            Assert.AreEqual(DiseaseRules.Count, names.Count);
            Assert.AreEqual(DiseaseRules.Count, Enum.GetValues(typeof(DiseaseKind)).Length);
        }

        [Test]
        public void DurationsPositive()
        {
            foreach (DiseaseKind k in Enum.GetValues(typeof(DiseaseKind))) Assert.Greater(DiseaseRules.Duration(k), 0f);
            Assert.AreEqual(180f, DiseaseRules.Duration(DiseaseKind.StagnantWater));
            Assert.AreEqual(240f, DiseaseRules.Duration(DiseaseKind.RottenGel));
            Assert.AreEqual(150f, DiseaseRules.Duration(DiseaseKind.SporeBlur));
        }

        [Test]
        public void OnlyRottenGelIsContagious()
        {
            Assert.IsFalse(DiseaseRules.Contagious(DiseaseKind.StagnantWater));
            Assert.IsTrue(DiseaseRules.Contagious(DiseaseKind.RottenGel));
            Assert.IsFalse(DiseaseRules.Contagious(DiseaseKind.SporeBlur));
        }

        [Test]
        public void InfectionMultiplierCurve()
        {
            Assert.AreEqual(1f, DiseaseRules.InfectionMultiplier(40f), 1e-5f);
            Assert.AreEqual(1f, DiseaseRules.InfectionMultiplier(100f), 1e-5f);
            Assert.AreEqual(3f, DiseaseRules.InfectionMultiplier(0f), 1e-5f);
            Assert.AreEqual(2f, DiseaseRules.InfectionMultiplier(20f), 1e-5f);
        }

        [Test]
        public void RecoveryRates()
        {
            Assert.AreEqual(1f, DiseaseRules.RecoveryRate(DiseaseKind.RottenGel, false, false, 50f), 1e-5f);
            Assert.AreEqual(2f, DiseaseRules.RecoveryRate(DiseaseKind.RottenGel, true, false, 50f), 1e-5f);
            Assert.AreEqual(1f, DiseaseRules.RecoveryRate(DiseaseKind.RottenGel, false, true, 50f), 1e-5f);        // clean water does not cure gel rot
            Assert.AreEqual(3f, DiseaseRules.RecoveryRate(DiseaseKind.StagnantWater, false, true, 0f), 1e-5f);
            Assert.AreEqual(6f, DiseaseRules.RecoveryRate(DiseaseKind.StagnantWater, true, true, 0f), 1e-5f);
            Assert.AreEqual(1f, DiseaseRules.RecoveryRate(DiseaseKind.StagnantWater, false, false, 100f), 1e-5f); // vitamin does not help here
            Assert.AreEqual(2f, DiseaseRules.RecoveryRate(DiseaseKind.SporeBlur, false, false, 60f), 1e-5f);
            Assert.AreEqual(1f, DiseaseRules.RecoveryRate(DiseaseKind.SporeBlur, false, false, 59f), 1e-5f);
        }

        [Test]
        public void VitaminCuresRottenGel()
        {
            Assert.AreEqual(60f, DiseaseRules.VitaminCure(DiseaseKind.RottenGel, 10f), 1e-4f);
            Assert.AreEqual(0f, DiseaseRules.VitaminCure(DiseaseKind.StagnantWater, 10f), 1e-4f);
            Assert.AreEqual(0f, DiseaseRules.VitaminCure(DiseaseKind.SporeBlur, 10f), 1e-4f);
            Assert.AreEqual(0f, DiseaseRules.VitaminCure(DiseaseKind.RottenGel, -5f), 1e-4f);
        }

        [Test]
        public void SymptomMultipliersAreNeutralWhenHealthy()
        {
            Assert.AreEqual(1f, DiseaseRules.ThirstDrainMultiplier(false));
            Assert.AreEqual(1f, DiseaseRules.HealMultiplier(false));
            Assert.AreEqual(0f, DiseaseRules.HealthDrainPerSecond(false));
            Assert.AreEqual(1f, DiseaseRules.OrganRecoverMultiplier(false));
            Assert.AreEqual(1f, DiseaseRules.SenseMultiplier(false));
            Assert.AreEqual(0f, DiseaseRules.StressPerSecond(false));
            Assert.AreEqual(1.6f, DiseaseRules.ThirstDrainMultiplier(true), 1e-5f);
            Assert.AreEqual(0.6f, DiseaseRules.HealMultiplier(true), 1e-5f);
            Assert.AreEqual(0.3f, DiseaseRules.HealthDrainPerSecond(true), 1e-5f);
            Assert.AreEqual(0.5f, DiseaseRules.OrganRecoverMultiplier(true), 1e-5f);
            Assert.AreEqual(0.6f, DiseaseRules.SenseMultiplier(true), 1e-5f);
            Assert.AreEqual(0.5f, DiseaseRules.StressPerSecond(true), 1e-5f);
        }
    }
}
