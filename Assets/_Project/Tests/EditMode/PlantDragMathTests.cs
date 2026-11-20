using Evolve.World;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    public class PlantDragMathTests
    {
        [Test]
        public void ThickerPlantsSlowMoreButNeverStopTheSlime()
        {
            Assert.AreEqual(1f, PlantDragMath.SpeedFor(0f), 1e-5f);
            Assert.Less(PlantDragMath.SpeedFor(0.8f), PlantDragMath.SpeedFor(0.3f));
            Assert.AreEqual(PlantDragMath.MinSpeed, PlantDragMath.SpeedFor(1f), 1e-5f);
            Assert.AreEqual(PlantDragMath.MinSpeed, PlantDragMath.SpeedFor(5f), 1e-5f, "clamped");
            Assert.Greater(PlantDragMath.MinSpeed, 0.4f, "the slime still moves");
        }
    }
}
