using Evolve.World;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    public class GlowPulseTests
    {
        [Test]
        public void IntensityStaysInRange()
        {
            for (float t = 0f; t < 60f; t += 0.37f)
            {
                float k = GlowPulse.Intensity(t, 1.6f, 0.45f, 2.1f);
                Assert.GreaterOrEqual(k, 1f - 0.45f - 1e-5f);
                Assert.LessOrEqual(k, 1f + 0.45f + 1e-5f);
            }
        }

        [Test]
        public void PhaseOffsetsDiffer()
        {
            Assert.That(GlowPulse.Intensity(3f, 1.6f, 0.45f, 0f), Is.Not.EqualTo(GlowPulse.Intensity(3f, 1.6f, 0.45f, 1.7f)).Within(1e-4f));
        }

        [Test]
        public void ZeroAmountIsSteady()
        {
            Assert.AreEqual(1f, GlowPulse.Intensity(12.3f, 2f, 0f, 0.5f), 1e-6f);
        }
    }
}
