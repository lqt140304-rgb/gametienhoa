using Evolve.World;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    public class SonarWaveMathTests
    {
        [Test]
        public void TheFrontSpreadsOutAndStopsAtTheFullRadius()
        {
            Assert.AreEqual(0f, SonarWaveMath.Radius(0f, 1f, 25f), 1e-4f);
            Assert.AreEqual(12.5f, SonarWaveMath.Radius(0.5f, 1f, 25f), 1e-4f);
            Assert.AreEqual(25f, SonarWaveMath.Radius(3f, 1f, 25f), 1e-4f);
        }

        [Test]
        public void TheWaveFadesAfterItHasSpread()
        {
            Assert.AreEqual(1f, SonarWaveMath.Fade(0.5f, 1f, 0.5f, 1f), 1e-4f);
            Assert.AreEqual(0.5f, SonarWaveMath.Fade(1.75f, 1f, 0.5f, 1f), 1e-3f);
            Assert.AreEqual(0f, SonarWaveMath.Fade(9f, 1f, 0.5f, 1f), 1e-4f);
        }

        [Test]
        public void OnlyWhatTheFrontIsTouchingIsLit()
        {
            float r = 10f, w = SonarWaveMath.BandWidth(r);
            Assert.AreEqual(1f, SonarWaveMath.Ring(10f, r, w), 1e-4f, "on the front");
            Assert.Less(SonarWaveMath.Ring(3f, r, w), 0.01f, "well inside the ring: dark again");
            Assert.Less(SonarWaveMath.Ring(20f, r, w), 0.01f, "not reached yet: dark");
            Assert.Greater(SonarWaveMath.Ring(10.5f, r, w), 0.8f, "just in front");
        }

        [Test]
        public void TheBandIsThinNearTheSlimeAndWiderFarAway()
        {
            Assert.GreaterOrEqual(SonarWaveMath.BandWidth(2f), 1.2f);
            Assert.Greater(SonarWaveMath.BandWidth(30f), SonarWaveMath.BandWidth(5f));
        }
    }
}
