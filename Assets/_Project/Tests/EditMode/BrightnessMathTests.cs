using Evolve.Visuals;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    public class BrightnessMathTests
    {
        [Test]
        public void ExposureAndLiftGrowWithBrightness()
        {
            Assert.Less(BrightnessMath.PostExposure(0.2f), BrightnessMath.PostExposure(0.8f));
            Assert.Less(BrightnessMath.GammaLift(0.2f), BrightnessMath.GammaLift(0.8f));
        }

        [Test]
        public void ValuesStayInRangeAndTheInputIsClamped()
        {
            Assert.AreEqual(BrightnessMath.MinExposure, BrightnessMath.PostExposure(-5f), 1e-4f);
            Assert.AreEqual(BrightnessMath.MaxExposure, BrightnessMath.PostExposure(9f), 1e-4f);
            Assert.AreEqual(0f, BrightnessMath.GammaLift(0f), 1e-4f);
            Assert.AreEqual(BrightnessMath.MaxGammaLift, BrightnessMath.GammaLift(1f), 1e-4f);
        }

        [Test]
        public void TheDefaultIsAGentleLift()
        {
            Assert.Greater(BrightnessMath.PostExposure(BrightnessMath.Default), 0f);
            Assert.Less(BrightnessMath.PostExposure(BrightnessMath.Default), 1.5f);
        }
    }
}
