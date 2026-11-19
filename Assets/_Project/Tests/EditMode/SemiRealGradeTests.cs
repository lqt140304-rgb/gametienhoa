using Evolve.World;
using NUnit.Framework;
using UnityEngine;

namespace Evolve.Tests.EditMode
{
    public class SemiRealGradeTests
    {
        static float Sat(Color c) { Color.RGBToHSV(c, out _, out float s, out _); return s; }
        static float Lum(Color c) => 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;

        [Test]
        public void Grade_Desaturates()
        {
            var red = new Color(0.8f, 0.1f, 0.1f);
            var g = SemiRealGrade.Grade(red, Color.white, 0f);
            Assert.Less(Sat(g), Sat(red) * 0.95f);
            Assert.Greater(Sat(g), Sat(red) * 0.55f);
        }

        [Test]
        public void Grade_KeepsLuminance()
        {
            foreach (var c in new[] { new Color(0.8f, 0.1f, 0.1f), new Color(0.2f, 0.5f, 0.1f), new Color(0.6f, 0.55f, 0.4f) })
            {
                var g = SemiRealGrade.Grade(c, SemiRealGrade.CaveTint);
                Assert.AreEqual(Lum(c), Lum(g), Lum(c) * 0.05f, c.ToString());
            }
        }

        [Test]
        public void Grade_TintsTowardZone()
        {
            var grey = new Color(0.5f, 0.5f, 0.5f);
            var g = SemiRealGrade.Grade(grey, new Color(0.6f, 0.8f, 1f), 0.5f);
            Assert.Greater(g.b, g.r, "toward the blue tint");
        }

        [Test]
        public void Roughness_PullsTowardMatte()
        {
            Assert.AreEqual(0.41f, SemiRealGrade.Roughness(0.2f), 0.01f);
            Assert.AreEqual(0.8f, SemiRealGrade.Roughness(0.8f), 1e-4f);
        }
    }
}
