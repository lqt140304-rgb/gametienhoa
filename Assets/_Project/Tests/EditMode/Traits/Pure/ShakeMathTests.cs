using Evolve.Traits;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    public class ShakeMathTests
    {
        [Test] public void Zero_NoShake() { var o = ShakeMath.Offset(1.234f, 0f); Assert.AreEqual(0f, o.x); Assert.AreEqual(0f, o.y); }

        [Test]
        public void Shake_BoundedByAmplitude()
        {
            for (float t = 0f; t < 20f; t += 0.037f)
            {
                var o = ShakeMath.Offset(t, 12f);
                Assert.LessOrEqual(System.Math.Abs(o.x), 12.0001f);
                Assert.LessOrEqual(System.Math.Abs(o.y), 12.0001f);
            }
        }

        [Test]
        public void Shake_DeterministicAndMoves()
        {
            Assert.AreEqual(ShakeMath.Offset(0.5f, 5f).x, ShakeMath.Offset(0.5f, 5f).x);
            Assert.AreNotEqual(ShakeMath.Offset(0.5f, 5f).x, ShakeMath.Offset(0.62f, 5f).x);
        }

        [Test]
        public void Decay_EndsAtZero()
        {
            Assert.AreEqual(1f, ShakeMath.Decay(0f, 1f), 1e-5f);
            Assert.AreEqual(0f, ShakeMath.Decay(1f, 1f), 1e-5f);
            Assert.AreEqual(0f, ShakeMath.Decay(2f, 1f), 1e-5f);
            Assert.AreEqual(1f, ShakeMath.Decay(-1f, 1f), 1e-5f);
            Assert.Greater(ShakeMath.Decay(0.2f, 1f), ShakeMath.Decay(0.6f, 1f));
        }
    }
}
