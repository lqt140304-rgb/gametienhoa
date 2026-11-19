using Evolve.Organs;
using Evolve.Swallow;
using NUnit.Framework;
using UnityEngine;

namespace Evolve.Tests.EditMode
{
    public class EchoTests
    {
        OrganSO organ;
        [SetUp] public void Make() { Echo.Clear(); organ = ScriptableObject.CreateInstance<OrganSO>(); organ.name = "Eye"; organ.DisplayName = "Mắt Đêm"; }
        [TearDown] public void Clean() { Echo.Clear(); Object.DestroyImmediate(organ); }

        [Test]
        public void Queue_FifoAndCap()
        {
            for (int i = 1; i <= 10; i++) Echo.Say("dòng " + i);
            int n = 0; string first = null;
            while (Echo.TryNext(out var t)) { if (n == 0) first = t; n++; }
            Assert.AreEqual(Echo.MaxQueued, n);
            Assert.AreEqual("dòng 3", first);
        }

        [Test]
        public void LearnedLine_HidesNameAtLevel1()
        {
            var r = new GeneResult { Organ = organ, Outcome = GeneOutcome.Learned, Stars = 1 };
            StringAssert.Contains("[???]", Echo.LearnedLine(r, false));
            StringAssert.Contains("[Mắt Đêm]", Echo.LearnedLine(r, true));
        }

        [Test]
        public void LearnedLine_StarUp() =>
            StringAssert.Contains("★2", Echo.LearnedLine(new GeneResult { Organ = organ, Outcome = GeneOutcome.StarUp, Stars = 2 }, true));

        [Test]
        public void LearnedLine_ProgressPercent() =>
            StringAssert.Contains("45%", Echo.LearnedLine(new GeneResult { Organ = organ, Outcome = GeneOutcome.Progress, Pity = 0.45f }, false));

        [Test]
        public void LearnedLine_FoodOnly_Null() =>
            Assert.IsNull(Echo.LearnedLine(new GeneResult { Organ = organ, Outcome = GeneOutcome.FoodOnly }, true));

        [Test]
        public void Empty_TryNextFalse() => Assert.IsFalse(Echo.TryNext(out _));
    }
}
