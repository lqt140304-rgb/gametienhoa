using Evolve.Ecology;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    public class CarcassDecayTests
    {
        [Test]
        public void StagesAdvanceInOrder()
        {
            var d = new CarcassDecay();
            Assert.AreEqual(CarcassStage.Fresh, d.Stage);
            d.Tick(3.01f);
            Assert.AreEqual(CarcassStage.Rotten, d.Stage);
            d.Tick(5f);
            Assert.AreEqual(CarcassStage.Humus, d.Stage);
            d.Tick(4f);
            Assert.AreEqual(CarcassStage.Gone, d.Stage);
        }

        [Test]
        public void EatingReducesFood()
        {
            var d = new CarcassDecay();
            Assert.AreEqual(10f, d.Eat(10f), 0.001f);
            Assert.AreEqual(20f, d.FoodLeft, 0.001f);
        }

        [Test]
        public void CannotEatHumus()
        {
            var d = new CarcassDecay();
            d.Tick(8.1f);
            Assert.AreEqual(CarcassStage.Humus, d.Stage);
            Assert.AreEqual(0f, d.Eat(10f), 0.001f);
        }

        [Test]
        public void HumusReleasesFoodToRegion()
        {
            var d = new CarcassDecay();
            d.Tick(12.1f);
            Assert.AreEqual(CarcassStage.Gone, d.Stage);
            Assert.AreEqual(40f, d.HumusFoodReleased, 0.001f);
        }

        [Test]
        public void EatNeverNegative()
        {
            var d = new CarcassDecay();
            Assert.AreEqual(30f, d.Eat(100f), 0.001f);
            Assert.AreEqual(0f, d.Eat(1f), 0.001f);
        }
    }
}
