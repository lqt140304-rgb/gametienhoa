using Evolve.Ecology;
using NUnit.Framework;
using UnityEngine;

namespace Evolve.Tests.EditMode
{
    public class IncursionRuleTests
    {
        static readonly Vector3[] Entries = { new Vector3(0, 0, 0), new Vector3(100, 0, 0) };

        [Test]
        public void IncursionNeverSpawnsNearPlayer()
            => Assert.AreEqual(1, IncursionRule.PickEntry(Entries, new[] { new Vector3(5, 0, 0) }, 40f));

        [Test]
        public void NoEntryWhenAllEntriesNearPlayers()
            => Assert.AreEqual(-1, IncursionRule.PickEntry(Entries, new[] { new Vector3(5, 0, 0), new Vector3(95, 0, 0) }, 40f));

        [Test]
        public void PicksFarthestValidEntry()
        {
            var entries = new[] { new Vector3(0, 0, 0), new Vector3(60, 0, 0), new Vector3(200, 0, 0) };
            Assert.AreEqual(2, IncursionRule.PickEntry(entries, new[] { new Vector3(-50, 0, 0) }, 40f));
        }

        [Test]
        public void EntersWhenPreyScarce()
            => Assert.IsTrue(IncursionRule.ShouldEnter(new SpeciesPopulation { Count = 3 }, 2, 4, 0f, 4f));

        [Test]
        public void EntersOnPatrolTimer()
            => Assert.IsTrue(IncursionRule.ShouldEnter(new SpeciesPopulation { Count = 3 }, 20, 4, 5f, 4f));

        [Test]
        public void NoEntryWithoutAvailablePredator()
            => Assert.IsFalse(IncursionRule.ShouldEnter(new SpeciesPopulation { Count = 3, Embodied = 3 }, 0, 4, 99f, 4f));
    }
}
