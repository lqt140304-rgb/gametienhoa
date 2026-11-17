using Evolve.Ecology;
using NUnit.Framework;
using UnityEngine;

namespace Evolve.Tests.EditMode
{
    public class EmbodiedCreatureTests
    {
        [SetUp]
        public void SetUp() => PopulationCap.Embodied = 0;

        [Test]
        public void EmbodiedCreatureReportsOnce()
        {
            var pop = new SpeciesPopulation { Id = "a", Count = 5, Capacity = 10 };
            pop.TryEmbody();
            PopulationCap.TryReserve();
            var go = new GameObject("c");
            var ec = go.AddComponent<EmbodiedCreature>();
            ec.Init(null, pop);

            ec.NotifyKilled();
            ec.NotifyKilled();

            Assert.AreEqual(4, pop.Count);
            Assert.AreEqual(0, pop.Embodied);
            Assert.AreEqual(0, PopulationCap.Embodied);
            Object.DestroyImmediate(go);
        }
    }
}
