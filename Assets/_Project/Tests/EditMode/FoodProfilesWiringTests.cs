using System;
using Evolve.Survival;
using Evolve.World;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Evolve.Tests.EditMode
{
    public class FoodProfilesWiringTests
    {
        [Test]
        public void EveryFoodKindHasADistinctOrMixedProfile()
        {
            var seen = new System.Collections.Generic.HashSet<string>();
            foreach (FoodKind kind in Enum.GetValues(typeof(FoodKind)))
            {
                var p = FoodProfiles.For(kind);
                seen.Add($"{p.Protein}/{p.Calcium}/{p.Iron}/{p.Fat}/{p.Vitamin}");
            }
            // Generic is the mixed profile; every other kind is its own recipe
            Assert.AreEqual(Enum.GetValues(typeof(FoodKind)).Length, seen.Count);
        }

        [Test]
        public void NutrientDefaultsToGenericFood()
        {
            var go = new GameObject("Plant", typeof(SphereCollider), typeof(Nutrient));
            try
            {
                var so = new SerializedObject(go.GetComponent<Nutrient>());
                Assert.AreEqual((int)FoodKind.Generic, so.FindProperty("foodKind").enumValueIndex);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        [Test]
        public void EatingEachPlantRaisesItsOwnNutrientMost()
        {
            foreach (var kind in new[] { FoodKind.GlowMoss, FoodKind.AmberBud, FoodKind.RootDew, FoodKind.EmberBerry })
            {
                var state = new SurvivalState();
                state.Reset(); // alive (Feed ignores a dead body)
                for (int k = 0; k < NutrientKinds.Count; k++) state.Nutrients[k] = 0f;
                state.Hunger = 10f;
                state.Feed(12f, FoodProfiles.For(kind));
                NutrientKind best = NutrientKind.Protein;
                for (int k = 0; k < NutrientKinds.Count; k++) if (state.Nutrients[k] > state.Nutrients[(int)best]) best = (NutrientKind)k;
                var expected = kind switch
                {
                    FoodKind.GlowMoss => NutrientKind.Vitamin,
                    FoodKind.AmberBud => NutrientKind.Fat,
                    FoodKind.RootDew => NutrientKind.Calcium,
                    _ => NutrientKind.Iron
                };
                Assert.AreEqual(expected, best, kind.ToString());
            }
        }
    }
}
