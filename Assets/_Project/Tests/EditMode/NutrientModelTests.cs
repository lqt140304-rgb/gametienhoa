using System;
using Evolve.Survival;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    public class NutrientModelTests
    {
        static NutrientKind Biggest(FoodProfile p)
        {
            NutrientKind best = NutrientKind.Protein;
            foreach (NutrientKind k in Enum.GetValues(typeof(NutrientKind))) if (p.Of(k) > p.Of(best)) best = k;
            return best;
        }

        [Test]
        public void ProfilesAreNonNegativeAndNotEmpty()
        {
            foreach (FoodKind kind in Enum.GetValues(typeof(FoodKind)))
            {
                var p = FoodProfiles.For(kind);
                float sum = 0f;
                foreach (NutrientKind k in Enum.GetValues(typeof(NutrientKind)))
                {
                    Assert.GreaterOrEqual(p.Of(k), 0f, kind + " " + k);
                    sum += p.Of(k);
                }
                Assert.Greater(sum, 0f, kind.ToString());
            }
        }

        [Test]
        public void PlantsGiveTheirOwnNutrient()
        {
            Assert.AreEqual(NutrientKind.Vitamin, Biggest(FoodProfiles.For(FoodKind.GlowMoss)));
            Assert.AreEqual(NutrientKind.Fat, Biggest(FoodProfiles.For(FoodKind.AmberBud)));
            Assert.AreEqual(NutrientKind.Vitamin, Biggest(FoodProfiles.For(FoodKind.SporePod)));
            Assert.AreEqual(NutrientKind.Calcium, Biggest(FoodProfiles.For(FoodKind.RootDew)));
            Assert.AreEqual(NutrientKind.Iron, Biggest(FoodProfiles.For(FoodKind.EmberBerry)));
            Assert.AreEqual(NutrientKind.Protein, Biggest(FoodProfiles.For(FoodKind.PreyMeat)));
        }

        [Test]
        public void RottenCarcassIsPoorer()
        {
            var fresh = FoodProfiles.Carcass(false);
            var rotten = FoodProfiles.Carcass(true);
            foreach (NutrientKind k in Enum.GetValues(typeof(NutrientKind)))
                Assert.AreEqual(fresh.Of(k) * 0.3f, rotten.Of(k), 1e-5f, k.ToString());
        }

        [Test]
        public void EffectsAreOneAboveThreshold()
        {
            foreach (float v in new[] { 40f, 60f, 100f })
            {
                Assert.AreEqual(1f, NutrientEffects.HealMultiplier(v), 1e-5f);
                Assert.AreEqual(1f, NutrientEffects.DamageTakenMultiplier(v), 1e-5f);
                Assert.AreEqual(1f, NutrientEffects.MaxHealthMultiplier(v), 1e-5f);
                Assert.AreEqual(1f, NutrientEffects.EnergyRegenMultiplier(v), 1e-5f);
                Assert.AreEqual(1f, NutrientEffects.TemperatureDriftMultiplier(v), 1e-5f);
                Assert.AreEqual(1f, NutrientEffects.SenseMultiplier(v), 1e-5f);
            }
        }

        [Test]
        public void EffectsBoundedAtZero()
        {
            Assert.AreEqual(0.4f, NutrientEffects.HealMultiplier(0f), 1e-5f);
            Assert.AreEqual(1.4f, NutrientEffects.DamageTakenMultiplier(0f), 1e-5f);
            Assert.AreEqual(0.7f, NutrientEffects.MaxHealthMultiplier(0f), 1e-5f);
            Assert.AreEqual(0.6f, NutrientEffects.EnergyRegenMultiplier(0f), 1e-5f);
            Assert.AreEqual(1.8f, NutrientEffects.TemperatureDriftMultiplier(0f), 1e-5f);
            Assert.AreEqual(0.6f, NutrientEffects.SenseMultiplier(0f), 1e-5f);
        }

        [Test]
        public void EffectsAreMonotonic()
        {
            for (float v = 0f; v < 100f; v += 5f)
            {
                Assert.LessOrEqual(NutrientEffects.HealMultiplier(v), NutrientEffects.HealMultiplier(v + 5f) + 1e-6f);
                Assert.GreaterOrEqual(NutrientEffects.DamageTakenMultiplier(v), NutrientEffects.DamageTakenMultiplier(v + 5f) - 1e-6f);
                Assert.LessOrEqual(NutrientEffects.MaxHealthMultiplier(v), NutrientEffects.MaxHealthMultiplier(v + 5f) + 1e-6f);
                Assert.GreaterOrEqual(NutrientEffects.TemperatureDriftMultiplier(v), NutrientEffects.TemperatureDriftMultiplier(v + 5f) - 1e-6f);
            }
        }

        [Test]
        public void NamesAndColorsExist()
        {
            var names = new System.Collections.Generic.HashSet<string>();
            foreach (NutrientKind k in Enum.GetValues(typeof(NutrientKind)))
            {
                Assert.IsNotEmpty(NutrientKinds.SlimeName(k));
                Assert.IsNotEmpty(NutrientKinds.RealName(k));
                names.Add(NutrientKinds.SlimeName(k));
                Assert.Greater(NutrientKinds.ColorOf(k).a, 0f);
            }
            Assert.AreEqual(NutrientKinds.Count, names.Count);
        }
    }
}
