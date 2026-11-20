using Evolve.Traits;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    public class EnvTraitRulesTests
    {
        [Test]
        public void NguySac_OnlyAfterTwoSecondsStill()
        {
            Assert.AreEqual(1f, EnvTraitRules.NguySacDetect(true, 1.9f));
            Assert.AreEqual(0.4f, EnvTraitRules.NguySacDetect(true, 2f), 1e-5f);
            Assert.AreEqual(1f, EnvTraitRules.NguySacDetect(false, 10f));
        }

        [Test]
        public void ThamThau_WorksStillOnWetGround_UpToSixtyPercent()
        {
            Assert.AreEqual(1.5f, EnvTraitRules.ThamThauGain(true, 0.1f, true, false, 30f, 100f, -1f), 1e-4f);
            Assert.AreEqual(0f, EnvTraitRules.ThamThauGain(true, 0.1f, true, false, 60f, 100f, -1f));          // full up to the cap
            Assert.AreEqual(0f, EnvTraitRules.ThamThauGain(true, 1f, true, false, 30f, 100f, -1f));            // moving
            Assert.AreEqual(0f, EnvTraitRules.ThamThauGain(true, 0.1f, false, false, 30f, 100f, -1f));         // dry ground
            Assert.AreEqual(0f, EnvTraitRules.ThamThauGain(true, 0.1f, true, true, 30f, 100f, -1f));           // swimming
            Assert.AreEqual(0f, EnvTraitRules.ThamThauGain(true, 0.1f, true, false, 30f, 100f, 1f));           // just hit
            Assert.AreEqual(1.5f, EnvTraitRules.ThamThauGain(true, 0.1f, true, false, 30f, 100f, 3f), 1e-4f);  // the pause is over
            Assert.AreEqual(0f, EnvTraitRules.ThamThauGain(false, 0.1f, true, false, 30f, 100f, -1f));
            Assert.IsTrue(EnvTraitRules.ThamThauStopsBurn(true, 0.1f, true, false, -1f));
            Assert.IsFalse(EnvTraitRules.ThamThauStopsBurn(true, 0.5f, true, false, -1f));
        }

        [Test]
        public void QuangHop_InLightNotUnderwater()
        {
            Assert.AreEqual(3f, EnvTraitRules.QuangHopEnergy(true, true, false));
            Assert.AreEqual(0f, EnvTraitRules.QuangHopEnergy(true, false, false));
            Assert.AreEqual(0f, EnvTraitRules.QuangHopEnergy(true, true, true));
            Assert.AreEqual(0f, EnvTraitRules.QuangHopEnergy(false, true, false));
        }

        [Test]
        public void AnMon_ThirtyPercent()
        {
            Assert.AreEqual(1.3f, EnvTraitRules.AnMonNutritionMultiplier(true), 1e-5f);
            Assert.AreEqual(1f, EnvTraitRules.AnMonNutritionMultiplier(false));
            Assert.AreEqual(1.3f, EnvTraitRules.AnMonDissolveMultiplier(true), 1e-5f);
        }

        [Test]
        public void Flaws_ScaleWithStrength()
        {
            Assert.AreEqual(20f, EnvTraitRules.LureDistance(1f), 1e-5f);
            Assert.AreEqual(0f, EnvTraitRules.LureDistance(0f));
            Assert.AreEqual(1f, EnvTraitRules.CrowdRegen(2, 1f));
            Assert.AreEqual(0.8f, EnvTraitRules.CrowdRegen(3, 1f), 1e-5f);
            Assert.AreEqual(1f, EnvTraitRules.CrowdRegen(5, 0f));
            Assert.AreEqual(1f, EnvTraitRules.DrySpeed(90f, 1f));
            Assert.AreEqual(0.92f, EnvTraitRules.DrySpeed(91f, 1f), 1e-5f);
            Assert.AreEqual(1f, EnvTraitRules.MudSpeed(-1f, 1f));
            Assert.AreEqual(0.7f, EnvTraitRules.MudSpeed(0.5f, 1f), 1e-5f);
            Assert.AreEqual(1f, EnvTraitRules.MudSpeed(2f, 1f));
        }

        [Test]
        public void ScentTrail_LaysSpacedPoints_AndForgets()
        {
            var t = new ScentTrail();
            t.Visit(0f, 0f, 0f);
            t.Visit(0.5f, 0f, 0.1f);           // too close to the last point
            Assert.AreEqual(1, t.Count);
            t.Visit(2f, 0f, 1f);
            Assert.AreEqual(2, t.Count);
            Assert.IsTrue(t.Near(10f, 0f, 5f, 15f));
            Assert.IsFalse(t.Near(30f, 0f, 5f, 15f));
            Assert.IsFalse(t.Near(1f, 0f, 25f, 15f));   // both points older than 20 s
            t.Visit(50f, 0f, 30f);                       // old points dropped
            Assert.AreEqual(1, t.Count);
        }
    }
}
