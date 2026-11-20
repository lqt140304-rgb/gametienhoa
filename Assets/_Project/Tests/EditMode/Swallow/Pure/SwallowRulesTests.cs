using Evolve.Ecology;
using Evolve.Swallow;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    public class SwallowRulesTests
    {
        [Test]
        public void CarcassQuality_Stages()
        {
            Assert.AreEqual(1f, SwallowRules.CarcassQuality(CarcassStage.Fresh, 0.5f));
            Assert.AreEqual(0.5f, SwallowRules.CarcassQuality(CarcassStage.Fresh, 2f));
            Assert.AreEqual(0.2f, SwallowRules.CarcassQuality(CarcassStage.Rotten, 4f));
            Assert.AreEqual(0f, SwallowRules.CarcassQuality(CarcassStage.Humus, 9f));
            Assert.AreEqual(0f, SwallowRules.CarcassQuality(CarcassStage.Gone, 20f));
        }

        [Test]
        public void SwallowRules_RemoteCatcher_FallsBackToFeed()
        {
            Assert.IsFalse(SwallowRules.ShouldUseStomach(true, true));
            Assert.IsTrue(SwallowRules.ShouldUseStomach(false, true));
            Assert.IsFalse(SwallowRules.ShouldUseStomach(false, false));
        }
    }
}
