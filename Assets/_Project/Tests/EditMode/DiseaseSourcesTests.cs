using Evolve.Survival;
using Evolve.World;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    public class DiseaseSourcesTests
    {
        [Test]
        public void ContagionRangeAndFlags()
        {
            Assert.IsTrue(ContagionSensor.ShouldTryInfect(true, false, true, 2.9f));
            Assert.IsTrue(ContagionSensor.ShouldTryInfect(true, false, true, 3f));
            Assert.IsFalse(ContagionSensor.ShouldTryInfect(true, false, true, 3.1f));   // too far
            Assert.IsFalse(ContagionSensor.ShouldTryInfect(false, false, true, 1f));    // not another player
            Assert.IsFalse(ContagionSensor.ShouldTryInfect(true, true, true, 1f));      // dead
            Assert.IsFalse(ContagionSensor.ShouldTryInfect(true, false, false, 1f));    // healthy
        }

        [Test]
        public void SporeSenseMultiplier()
        {
            Assert.AreEqual(0.6f, DiseaseRules.SenseMultiplier(true), 1e-5f);
            Assert.AreEqual(1f, DiseaseRules.SenseMultiplier(false), 1e-5f);
        }

        [Test]
        public void SourcesAreAlsoWeakWithGoodVitamin()
        {
            var s = new SurvivalState();
            s.Reset();
            s.Roll = () => 0.05f;
            // 4% per second of dirty water with vitamin 80 (x1): a 5% roll does not hit; with vitamin 0 (x3 = 12%) it does
            Assert.IsFalse(s.TryInfect(DiseaseKind.StagnantWater, DiseaseRules.DirtyWaterPerSecond));
            s.Nutrients[(int)NutrientKind.Vitamin] = 0f;
            Assert.IsTrue(s.TryInfect(DiseaseKind.StagnantWater, DiseaseRules.DirtyWaterPerSecond));
        }
    }
}
