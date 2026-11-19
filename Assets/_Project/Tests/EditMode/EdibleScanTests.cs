using Evolve.Survival;
using Evolve.World;
using NUnit.Framework;
using UnityEngine;

namespace Evolve.Tests.EditMode
{
    public class EdibleScanTests
    {
        [Test]
        public void EachFamilyOfFoodHasItsOwnTint()
        {
            var plant = EdibleScan.TintFor(FoodKind.Cattail);
            var mushroom = EdibleScan.TintFor(FoodKind.FatMushroom);
            var mineral = EdibleScan.TintFor(FoodKind.IronNodule);
            var meat = EdibleScan.TintFor(FoodKind.PreyMeat);
            Assert.AreNotEqual(plant, mushroom);
            Assert.AreNotEqual(mushroom, mineral);
            Assert.AreNotEqual(mineral, meat);
            Assert.Greater(plant.g, plant.r, "plants read green");
            Assert.Greater(meat.r, meat.g, "meat reads red");
            foreach (FoodKind k in System.Enum.GetValues(typeof(FoodKind)))
                Assert.AreEqual(1f, EdibleScan.TintFor(k).a, "alpha 1 means 'this is food' to the shader");
        }

        [Test]
        public void TheWaveReachIsHalfTheOldOne()
        {
            Assert.AreEqual(0.5f, VibrationSense.ScanRadiusFactor, 1e-5f);
            Assert.GreaterOrEqual(VibrationSense.ExpandFactor, 2f, "the wave is slower so it can be followed");
        }
    }
}
