using Evolve.World;
using NUnit.Framework;
using UnityEngine;

namespace Evolve.Tests.EditMode
{
    public class ZoneAtmosphereMathTests
    {
        static float[] Only(int zone, float w)
        {
            var a = new float[6];
            a[zone] = w;
            return a;
        }

        [Test]
        public void Zones_HasSixDistinctProfiles()
        {
            Assert.AreEqual(6, ZoneAtmosphereMath.Zones.Length);
            for (int i = 0; i < 6; i++)
                for (int j = i + 1; j < 6; j++)
                    Assert.AreNotEqual(ZoneAtmosphereMath.Zones[i].FogColor, ZoneAtmosphereMath.Zones[j].FogColor, $"{i}/{j}");
            float min = float.MaxValue;
            foreach (var z in ZoneAtmosphereMath.Zones) min = Mathf.Min(min, z.FogEnd);
            Assert.AreEqual(ZoneAtmosphereMath.Zones[5].FogEnd, min, "the hunting edge has the shortest sight");
            Assert.GreaterOrEqual(min, 40f);
        }

        [Test]
        public void Blend_AllWeightsZeroGivesWild()
        {
            var p = ZoneAtmosphereMath.Blend(new float[6]);
            Assert.AreEqual(ZoneAtmosphereMath.Wild.FogEnd, p.FogEnd, 1e-4f);
            Assert.AreEqual(ZoneAtmosphereMath.Wild.FogColor, p.FogColor);
            Assert.AreEqual(0f, p.Density, 1e-4f);
        }

        [Test]
        public void Blend_SingleZoneWeightOneGivesThatZone()
        {
            var z = ZoneAtmosphereMath.Zones[3];
            var p = ZoneAtmosphereMath.Blend(Only(3, 1f));
            Assert.AreEqual(z.FogEnd, p.FogEnd, 1e-3f);
            Assert.AreEqual(z.FogColor.r, p.FogColor.r, 1e-4f);
            Assert.AreEqual(z.Particles, p.Particles);
        }

        [Test]
        public void Blend_HalfAndHalfIsMidpoint()
        {
            var w = new float[6];
            w[0] = 0.5f; w[5] = 0.5f;
            var p = ZoneAtmosphereMath.Blend(w);
            Assert.AreEqual((ZoneAtmosphereMath.Zones[0].FogEnd + ZoneAtmosphereMath.Zones[5].FogEnd) * 0.5f, p.FogEnd, 1e-3f);
        }

        [Test]
        public void Blend_NormalisesWhenWeightsExceedOne()
        {
            var w = new float[6];
            w[1] = 1f; w[2] = 1f;
            var p = ZoneAtmosphereMath.Blend(w);
            Assert.AreEqual((ZoneAtmosphereMath.Zones[1].FogEnd + ZoneAtmosphereMath.Zones[2].FogEnd) * 0.5f, p.FogEnd, 1e-3f);
        }

        [Test]
        public void Blend_NegativeWeightIgnored()
        {
            var w = new float[6];
            w[2] = -5f;
            Assert.AreEqual(ZoneAtmosphereMath.Wild.FogEnd, ZoneAtmosphereMath.Blend(w).FogEnd, 1e-3f);
        }

        [Test]
        public void Blend_NullOrShortArrayIsSafe()
        {
            Assert.AreEqual(ZoneAtmosphereMath.Wild.FogEnd, ZoneAtmosphereMath.Blend(null).FogEnd, 1e-3f);
            Assert.AreEqual(ZoneAtmosphereMath.Zones[0].FogEnd, ZoneAtmosphereMath.Blend(new[] { 1f }).FogEnd, 1e-3f);
        }

        [Test]
        public void FogColor_BlindIsBlackSightedIsZoneColour()
        {
            var p = ZoneAtmosphereMath.Zones[3];
            Assert.AreEqual(Color.black.r, ZoneAtmosphereMath.FogColorFor(p, 0f).r, 1e-5f);
            var tint = ZoneAtmosphereMath.Desaturate(p.FogColor);
            Assert.AreEqual(tint.b, ZoneAtmosphereMath.FogColorFor(p, 1f).b, 1e-5f);
            Assert.AreEqual(tint.b * 0.5f, ZoneAtmosphereMath.FogColorFor(p, 0.5f).b, 1e-5f);
        }

        [Test]
        public void Desaturate_KeepsBrightnessAndLowersSpread()
        {
            var c = new Color(0.25f, 0.12f, 0.30f);
            var d = ZoneAtmosphereMath.Desaturate(c);
            float lumC = c.r * 0.30f + c.g * 0.59f + c.b * 0.11f, lumD = d.r * 0.30f + d.g * 0.59f + d.b * 0.11f;
            Assert.AreEqual(lumC, lumD, 1e-5f);
            Assert.Less(Mathf.Max(d.r, d.g, d.b) - Mathf.Min(d.r, d.g, d.b), Mathf.Max(c.r, c.g, c.b) - Mathf.Min(c.r, c.g, c.b));
        }

        [Test]
        public void FogEnd_BlindIsEightyAndGrowsWithSightAboveOne()
        {
            var p = ZoneAtmosphereMath.Zones[0];
            Assert.AreEqual(80f, ZoneAtmosphereMath.FogEndFor(p, 0f), 1e-3f);
            Assert.AreEqual(p.FogEnd, ZoneAtmosphereMath.FogEndFor(p, 1f), 1e-3f);
            Assert.AreEqual(p.FogEnd * 2f, ZoneAtmosphereMath.FogEndFor(p, 2f), 1e-3f);
        }

        [Test]
        public void ParticleBudget_NeverAboveMaxAndZeroWhenBlind()
        {
            Assert.AreEqual(ZoneAtmosphereMath.MaxParticles, ZoneAtmosphereMath.ParticleBudget(1f, 1000f), 1e-4f);
            Assert.AreEqual(0f, ZoneAtmosphereMath.ParticleBudget(0f, 5f), 1e-4f);
            Assert.AreEqual(50f, ZoneAtmosphereMath.ParticleBudget(1f, 5f), 1e-3f);
            foreach (var z in ZoneAtmosphereMath.Zones)
                Assert.LessOrEqual(ZoneAtmosphereMath.ParticleBudget(1f, z.Density), 50f, "one group emits at a time, 50 particles at most each");
        }
    }
}
