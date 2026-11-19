using System.Collections.Generic;
using System.Linq;
using Evolve.Survival;
using Evolve.World;
using NUnit.Framework;
using UnityEngine;

namespace Evolve.Tests.EditMode
{
    public class CavernForageTests
    {
        [Test]
        public void ADozenDistinctKindsEachWithItsOwnFood()
        {
            Assert.AreEqual(12, CavernForageData.Specs.Count);
            Assert.AreEqual(12, CavernForageData.Specs.Select(s => s.Food).Distinct().Count());
            Assert.AreEqual(12, CavernForageData.Specs.Select(s => s.Id).Distinct().Count());
            Assert.AreEqual(4, CavernForageData.Specs.Count(s => s.Mineral));
        }

        [Test]
        public void TheKindsFeedAllFiveNutrientsAndMostlyDifferently()
        {
            // every nutrient is the main thing some food gives, and the recipes are not all alike
            var dominant = new HashSet<NutrientKind>();
            var recipes = new HashSet<string>();
            foreach (var spec in CavernForageData.Specs)
            {
                var p = FoodProfiles.For(spec.Food);
                NutrientKind best = NutrientKind.Protein; float bestV = -1f;
                foreach (NutrientKind k in System.Enum.GetValues(typeof(NutrientKind)))
                    if (p.Of(k) > bestV) { bestV = p.Of(k); best = k; }
                dominant.Add(best);
                recipes.Add($"{p.Of(NutrientKind.Protein)}|{p.Of(NutrientKind.Calcium)}|{p.Of(NutrientKind.Iron)}|{p.Of(NutrientKind.Fat)}|{p.Of(NutrientKind.Vitamin)}");
                Assert.AreNotEqual(FoodKind.Generic, spec.Food);
            }
            Assert.AreEqual(5, dominant.Count, "every nutrient is somebody's main food");
            Assert.AreEqual(12, recipes.Count, "no two kinds share a recipe");
        }

        [Test]
        public void MineralsFeedMoreButSlowerAndComeBackLater()
        {
            var plant = CavernForageData.Specs.First(s => !s.Mineral);
            foreach (var m in CavernForageData.Specs.Where(s => s.Mineral))
            {
                Assert.Greater(m.Nutrition, plant.Nutrition);
                Assert.Less(m.AbsorbRate, plant.AbsorbRate);
                Assert.Greater(m.Respawn, plant.Respawn);
            }
        }

        [Test]
        public void PlacementKeepsToHabitatsAndIsNeverInWaterExceptLilies()
        {
            var all = CavernForageData.PlaceAll();
            foreach (var (p, spec) in all)
            {
                float depth = CavernLayout.DepthAt(p.x, p.z);
                switch (spec.Habitat)
                {
                    case ForageHabitat.Shallows: Assert.That(depth, Is.InRange(0.08f, 0.42f)); break;
                    case ForageHabitat.ShoreWater: Assert.LessOrEqual(depth, 0.35f); break;
                    default: Assert.AreEqual(0f, depth, 1e-5f, spec.Id); break;
                }
                if (spec.Habitat == ForageHabitat.WallFoot) Assert.GreaterOrEqual(CavernLayout.RadiusFromCenter(p.x, p.z), 24f);
                if (spec.Habitat == ForageHabitat.ShoreBand) Assert.GreaterOrEqual(CavernLayout.ShoreOffset(p.x, p.z), 3f);
            }
        }

        [Test]
        public void EveryKindIsFoundAndNothingCrowds()
        {
            var all = CavernForageData.PlaceAll();
            foreach (var spec in CavernForageData.Specs)
            {
                int n = all.Count(a => a.spec == spec);
                if (spec.Mineral) Assert.GreaterOrEqual(n, 1, spec.Id);
                else Assert.GreaterOrEqual(n, spec.Count / 2, $"{spec.Id} found only {n}");
                Assert.LessOrEqual(n, spec.Count);
            }
            Assert.Greater(all.Count, 80);
            Assert.LessOrEqual(all.Count, FoodSyncLimit.MaxBlobs);   // all of it can be synced in co-op
            for (int i = 0; i < all.Count; i++)
                for (int j = i + 1; j < all.Count; j++)
                {
                    float d = Vector2.Distance(new Vector2(all[i].pos.x, all[i].pos.z), new Vector2(all[j].pos.x, all[j].pos.z));
                    Assert.GreaterOrEqual(d, 2.2f - 1e-3f, $"{all[i].spec.Id} and {all[j].spec.Id} are {d} m apart");
                }
        }

        [Test]
        public void OreSitesGetAtLeastThreeDifferentMinerals()
        {
            var ore = CavernForageData.PlaceOre();
            Assert.GreaterOrEqual(ore.Select(o => o.spec.Id).Distinct().Count(), 3);
            Assert.IsTrue(ore.All(o => o.spec.Mineral));
        }

        [Test]
        public void PlacementIsDeterministic()
        {
            var a = CavernForageData.PlaceAll(7);
            var b = CavernForageData.PlaceAll(7);
            Assert.AreEqual(a.Count, b.Count);
            for (int i = 0; i < a.Count; i++) { Assert.AreEqual(a[i].pos, b[i].pos); Assert.AreSame(a[i].spec, b[i].spec); }
        }

        [Test]
        public void ByFoodFindsTheSpec()
        {
            Assert.AreEqual("lily", CavernForageData.ByFood(FoodKind.WaterLily).Id);
            Assert.IsNull(CavernForageData.ByFood(FoodKind.PreyMeat));
        }
    }
}
