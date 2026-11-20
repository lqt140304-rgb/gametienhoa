using Evolve.Combat;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    public class PounceRulesTests
    {
        [Test] public void Decide_ByMass()
        {
            Assert.AreEqual(PounceOutcome.SwallowWhole, PounceRules.Decide(10f, 8f));
            Assert.AreEqual(PounceOutcome.Latch, PounceRules.Decide(10f, 8.1f));
            Assert.AreEqual(PounceOutcome.Latch, PounceRules.Decide(10f, 40f));
            Assert.AreEqual(PounceOutcome.Bounce, PounceRules.Decide(10f, 40.1f));
        }

        [Test] public void PickTarget_NearestInFront()
        {
            var c = new[] { new Candidate(0f, 2f, false), new Candidate(0f, 3f, false), new Candidate(0f, -1f, false) };
            Assert.AreEqual(0, PounceRules.PickTarget(c, 0f, 0f, 0f, 1f));
            Assert.AreEqual(-1, PounceRules.PickTarget(new[] { new Candidate(0f, -2f, false) }, 0f, 0f, 0f, 1f));   // behind
            Assert.AreEqual(-1, PounceRules.PickTarget(new[] { new Candidate(0f, 5f, false) }, 0f, 0f, 0f, 1f));    // too far
            Assert.AreEqual(1, PounceRules.PickTarget(new[] { new Candidate(0f, 2f, true), new Candidate(0f, 3f, false) }, 0f, 0f, 0f, 1f));   // taken
            Assert.AreEqual(-1, PounceRules.PickTarget(c, 0f, 0f, 0f, 0f));   // no facing direction: nothing chosen, no error
            Assert.AreEqual(-1, PounceRules.PickTarget(new Candidate[0], 0f, 0f, 0f, 1f));
        }

        [Test] public void PickTarget_ChoosesCloserOfTwoInTheSameDirection()
        {
            var c = new[] { new Candidate(0f, 3f, false), new Candidate(0f, 2f, false) };
            Assert.AreEqual(1, PounceRules.PickTarget(c, 0f, 0f, 0f, 1f));
        }

        [Test] public void StruggleSeed_ByRole()
        {
            var prey = StruggleSeed.For("Ốc Hồ"); var hunter = StruggleSeed.For("ThanLanBoHo"); var lord = StruggleSeed.For("Cóc Hồ Già");
            Assert.AreEqual(0.40f, prey.ZoneWidth); Assert.AreEqual(5, prey.MaxFaults);
            Assert.AreEqual(0.28f, hunter.ZoneWidth); Assert.AreEqual(4, hunter.MaxFaults);
            Assert.AreEqual(0.18f, lord.ZoneWidth); Assert.AreEqual(3, lord.MaxFaults);
            Assert.AreEqual(prey.ZoneWidth, StruggleSeed.For("xyz").ZoneWidth);
            Assert.AreEqual(0f, prey.HeavyPerSecond);
            Assert.Greater(lord.HeavyPerSecond, hunter.HeavyPerSecond);
        }
    }
}
