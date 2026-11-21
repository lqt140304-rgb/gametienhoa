using NUnit.Framework;

namespace Evolve.Combat.Tests
{
    public class LatchGelTimingTests
    {
        [Test]
        public void GripCannotCoverTheWholeCreature()
        {
            Assert.That(LatchGelTiming.Coverage(0f), Is.EqualTo(0.18f));
            Assert.That(LatchGelTiming.Coverage(0.5f), Is.GreaterThan(0.18f));
            Assert.That(LatchGelTiming.Coverage(2f), Is.EqualTo(0.82f).Within(0.0001f));
        }

        [Test]
        public void SilhouetteHoldsAfterTheCoatCloses()
        {
            Assert.That(LatchGelTiming.Dissolve(0.7f), Is.Zero);
            Assert.That(LatchGelTiming.Dissolve(1.1f), Is.InRange(0.1f, 0.9f));
            Assert.That(LatchGelTiming.Dissolve(2f), Is.EqualTo(1f));
        }
    }
}
