using Evolve.Ecology;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    public class RegionPopulationTests
    {
        [SetUp]
        public void SetUp() => PopulationCap.Embodied = 0;

        static RegionPopulation Region(float food, float regen = 0f, float cap = 1000f)
            => new RegionPopulation { Food = food, FoodRegenPerMinute = regen, FoodCapacity = cap };

        [Test]
        public void NoBirthsWithoutDens()
        {
            var r = Region(100f);
            var s = r.Add(new SpeciesPopulation { Id = "a", Count = 5, Capacity = 50, BirthsPerMinute = 1f, Dens = 0 });
            r.Tick(10f);
            Assert.AreEqual(5, s.Count);
        }

        [Test]
        public void BirthsCappedAtCapacity()
        {
            var r = Region(100f);
            var s = r.Add(new SpeciesPopulation { Id = "a", Count = 9, Capacity = 10, BirthsPerMinute = 5f, Dens = 2 });
            r.Tick(1f);
            Assert.AreEqual(10, s.Count);
        }

        [Test]
        public void CountNeverNegative()
        {
            var r = Region(0f);
            var s = r.Add(new SpeciesPopulation { Id = "a", Count = 3, Capacity = 10, FoodPerHeadPerMinute = 1f });
            r.Tick(1000f);
            Assert.GreaterOrEqual(s.Count, 0);
            s.Count = 0;
            s.Kill(false);
            Assert.AreEqual(0, s.Count);
        }

        [Test]
        public void StarvationKillsOnlyUnembodied()
        {
            var r = Region(0f);
            var s = r.Add(new SpeciesPopulation { Id = "a", Count = 4, Embodied = 4, Capacity = 10, FoodPerHeadPerMinute = 1f });
            r.Tick(100f);
            Assert.AreEqual(4, s.Count);
        }

        [Test]
        public void PredatorEatsPrey()
        {
            var r = Region(0f);
            var prey = r.Add(new SpeciesPopulation { Id = "prey", Count = 10, Capacity = 20 });
            r.Add(new SpeciesPopulation { Id = "pred", Count = 1, Capacity = 5, PreyId = "prey", KillsPerHeadPerMinute = 2f });
            r.Tick(1f);
            Assert.AreEqual(8, prey.Count);
        }

        [Test]
        public void StoreThenEmbodyKeepsCount()
        {
            var s = new SpeciesPopulation { Id = "a", Count = 5, Capacity = 10 };
            for (int i = 0; i < 3; i++) Assert.IsTrue(s.TryEmbody());
            for (int i = 0; i < 3; i++) s.Store();
            for (int i = 0; i < 3; i++) Assert.IsTrue(s.TryEmbody());
            Assert.AreEqual(5, s.Count);
            Assert.AreEqual(3, s.Embodied);
        }

        [Test]
        public void EmbodyRespectsGlobalCap()
        {
            PopulationCap.Embodied = PopulationCap.Max;
            Assert.IsFalse(PopulationCap.TryReserve());
            PopulationCap.Release();
            Assert.IsTrue(PopulationCap.TryReserve());
            Assert.AreEqual(PopulationCap.Max, PopulationCap.Embodied);
        }
    }
}
