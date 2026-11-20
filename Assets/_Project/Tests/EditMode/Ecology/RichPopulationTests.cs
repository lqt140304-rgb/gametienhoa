using Evolve.Ecology;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    public class RichPopulationTests
    {
        static SpeciesPopulation Grazer(string pool, int count = 5, int cap = 10, float meals = 0.2f) =>
            new SpeciesPopulation { Id = "g", Count = count, Capacity = cap, MealsPerHeadPerMinute = meals, Diet = { new DietEntry { Id = pool, IsPool = true } } };

        [Test] public void Pool_RegrowsAndIsEaten()
        {
            var r = new RegionPopulation();
            var pool = r.AddPool("moss", 10f, 1f, 50f);
            r.Add(Grazer("moss", 5, 10, 0.2f));
            r.Tick(1f);   // +1 regrow, 5 heads x 0.2 = 1 eaten
            Assert.AreEqual(10f, pool.Amount, 1e-3f);
        }

        [Test] public void EatingOnePool_DoesNotFeedTheOtherPoolsEaters()
        {
            var r = new RegionPopulation();
            r.AddPool("a", 0f, 0f, 10f); r.AddPool("b", 100f, 0f, 100f);
            var onA = r.Add(new SpeciesPopulation { Id = "onA", Count = 10, Capacity = 10, MealsPerHeadPerMinute = 0.5f, Diet = { new DietEntry { Id = "a", IsPool = true } } });
            var onB = r.Add(new SpeciesPopulation { Id = "onB", Count = 10, Capacity = 10, MealsPerHeadPerMinute = 0.5f, Diet = { new DietEntry { Id = "b", IsPool = true } } });
            for (int i = 0; i < 20; i++) r.Tick(1f);
            Assert.Less(onA.Count, 10);       // moss is gone: it starves
            Assert.AreEqual(10, onB.Count);   // the other food is plentiful
        }

        [Test] public void TwoPreyWithWeights_EatenInProportion()
        {
            var r = new RegionPopulation();
            var a = r.Add(new SpeciesPopulation { Id = "a", Count = 100, Capacity = 100 });
            var b = r.Add(new SpeciesPopulation { Id = "b", Count = 100, Capacity = 100 });
            r.Add(new SpeciesPopulation { Id = "toad", Count = 10, Capacity = 10, MealsPerHeadPerMinute = 1f, HalfSaturation = 0f,
                Diet = { new DietEntry { Id = "a", Weight = 3f }, new DietEntry { Id = "b", Weight = 1f } } });
            r.Tick(1f);   // 10 meals: 7.5 from a, 2.5 from b -> 7 and 2 whole kills, the rest banked
            Assert.AreEqual(93, a.Count);
            Assert.AreEqual(98, b.Count);
        }

        [Test] public void WhenOnePreyRunsOut_TheHunterTurnsToTheOther()
        {
            var r = new RegionPopulation();
            r.Add(new SpeciesPopulation { Id = "a", Count = 0, Capacity = 10 });
            var b = r.Add(new SpeciesPopulation { Id = "b", Count = 50, Capacity = 50 });
            var toad = r.Add(new SpeciesPopulation { Id = "toad", Count = 5, Capacity = 5, MealsPerHeadPerMinute = 1f, HalfSaturation = 0f,
                Diet = { new DietEntry { Id = "a" }, new DietEntry { Id = "b" } } });
            r.Tick(1f);
            Assert.AreEqual(45, b.Count);
            Assert.AreEqual(1f, toad.Satiety, 1e-3f);
        }

        [Test] public void HunterWithoutPrey_StarvesDownToItsFloor_ThenStays()
        {
            var r = new RegionPopulation();
            r.Add(new SpeciesPopulation { Id = "prey", Count = 0, Capacity = 10 });
            var h = r.Add(new SpeciesPopulation { Id = "hunter", Count = 3, Capacity = 3, MealsPerHeadPerMinute = 0.3f, Floor = 1, Diet = { new DietEntry { Id = "prey" } } });
            for (int i = 0; i < 100; i++) r.Tick(1f);
            Assert.AreEqual(1, h.Count);
        }

        [Test] public void Hunting_Saturates_RarePreyIsCaughtLessThanCommonPrey()
        {
            int Caught(int preyCount)
            {
                var r = new RegionPopulation();
                var prey = r.Add(new SpeciesPopulation { Id = "p", Count = preyCount, Capacity = 10 });
                r.Add(new SpeciesPopulation { Id = "h", Count = 3, Capacity = 3, MealsPerHeadPerMinute = 1f, Diet = { new DietEntry { Id = "p" } } });
                r.Tick(1f);
                return preyCount - prey.Count;
            }
            Assert.Greater(Caught(10), Caught(3));   // 3 hunters want 3 per minute: full prey gives 3, a rare one less
            Assert.LessOrEqual(Caught(3), 2);
        }

        [Test] public void PreyEscape_ReducesTheHunt()
        {
            int Left(float escape)
            {
                var r = new RegionPopulation();
                var prey = r.Add(new SpeciesPopulation { Id = "p", Count = 100, Capacity = 100, EscapeChance = escape });
                r.Add(new SpeciesPopulation { Id = "h", Count = 10, Capacity = 10, MealsPerHeadPerMinute = 1f, HalfSaturation = 0f, Diet = { new DietEntry { Id = "p" } } });
                r.Tick(1f);
                return prey.Count;
            }
            Assert.AreEqual(90, Left(0f));
            Assert.AreEqual(95, Left(0.5f));
        }

        [Test] public void EmptyNest_BringsOneBack_AfterRecoverMinutes_AndLordAfterNine()
        {
            var r = new RegionPopulation();
            r.AddPool("moss", 50f, 0f, 50f);
            var lord = r.Add(new SpeciesPopulation { Id = "lord", Count = 0, Capacity = 1, Dens = 1, RecoverMinutes = 9f, MealsPerHeadPerMinute = 0.1f });
            for (int i = 0; i < 17; i++) { r.Tick(0.5f); Assert.AreEqual(0, lord.Count, "t=" + (i + 1) * 0.5f); }   // 8.5 minutes
            r.Tick(0.5f);   // 9.0
            Assert.AreEqual(1, lord.Count);
        }

        [Test] public void NoDens_NoRecovery()
        {
            var r = new RegionPopulation();
            r.AddPool("moss", 50f, 0f, 50f);
            var s = r.Add(new SpeciesPopulation { Id = "s", Count = 0, Capacity = 5, Dens = 0, RecoverMinutes = 6f });
            for (int i = 0; i < 40; i++) r.Tick(1f);
            Assert.AreEqual(0, s.Count);
        }

        [Test] public void Births_NeedFood_AndNeverExceedCapacity()
        {
            var r = new RegionPopulation();
            r.AddPool("moss", 100f, 5f, 100f);
            var s = r.Add(Grazer("moss", 2, 6, 0.05f)); s.Dens = 2; s.BirthsPerMinute = 1f;
            for (int i = 0; i < 20; i++) r.Tick(1f);
            Assert.AreEqual(6, s.Count);
            var hungry = new RegionPopulation();
            hungry.AddPool("moss", 0f, 0f, 100f);
            var h = hungry.Add(Grazer("moss", 2, 6, 0.05f)); h.Dens = 2; h.BirthsPerMinute = 1f; h.Floor = 2;
            for (int i = 0; i < 20; i++) hungry.Tick(1f);
            Assert.AreEqual(2, h.Count);   // no food, no births
        }

        [Test] public void Deaths_FeedTheReturnPool()
        {
            var r = new RegionPopulation { ReturnPool = "carcass" };
            var pool = r.AddPool("carcass", 0f, 0f, 100f);
            var prey = r.Add(new SpeciesPopulation { Id = "p", Count = 50, Capacity = 50, BodyFood = 2f });
            r.Add(new SpeciesPopulation { Id = "h", Count = 5, Capacity = 5, MealsPerHeadPerMinute = 1f, HalfSaturation = 0f, Diet = { new DietEntry { Id = "p" } } });
            r.Tick(1f);
            Assert.AreEqual(5, 50 - prey.Count);
            Assert.AreEqual(10f, pool.Amount, 1e-3f);
        }

        [Test] public void LongRangeHunter_ReachesOnlyPartOfTheFarPrey()
        {
            var w = new EcologyWorld();
            var home = w.Add("home", new RegionPopulation());
            var far = w.Add("far", new RegionPopulation());
            var prey = far.Add(new SpeciesPopulation { Id = "moth", Count = 10, Capacity = 10 });
            home.Add(new SpeciesPopulation { Id = "bat", Count = 10, Capacity = 10, MealsPerHeadPerMinute = 1f, HalfSaturation = 0f,
                Diet = { new DietEntry { Id = "moth", RegionId = "far", Efficiency = 0.5f } } });
            home.AddPool("x", 0f, 0f, 1f);   // makes it a rich region
            w.Tick(1f);
            Assert.AreEqual(5, prey.Count);   // 10 meals wanted, but only half of the 10 moths are in range
        }

        [Test] public void Migration_CrowdedSpeciesSendsAGuestColony_NoOneLostOrInvented()
        {
            var w = new EcologyWorld();
            var a = w.Add("a", new RegionPopulation()); var b = w.Add("b", new RegionPopulation());
            w.Link("a", "b");
            var s = a.Add(new SpeciesPopulation { Id = "bug", Count = 10, Capacity = 10, Migrates = true });
            int moved = w.Migrate(0.5f);
            Assert.AreEqual(2, moved);   // ceil(20% of 10)
            Assert.AreEqual(8, s.Count);
            var guests = b.Get("bug");
            Assert.AreEqual(2, guests.Count); Assert.IsTrue(guests.Guest); Assert.AreEqual(0, guests.Dens);
            Assert.AreEqual(0, w.Migrate(0.5f));   // cooldown
        }

        [Test] public void Migration_NotWhenFewOrNotCrowdedOrNoNeighbour()
        {
            var w = new EcologyWorld();
            var a = w.Add("a", new RegionPopulation()); w.Add("b", new RegionPopulation());
            var calm = a.Add(new SpeciesPopulation { Id = "calm", Count = 5, Capacity = 10, Migrates = true });
            Assert.AreEqual(0, w.Migrate(1f));
            calm.Count = 10;
            Assert.AreEqual(0, w.Migrate(1f));   // no tunnel
            w.Link("a", "b");
            var lord = a.Add(new SpeciesPopulation { Id = "lord", Count = 1, Capacity = 1, Migrates = true });
            Assert.AreEqual(2, w.Migrate(1f));   // only the crowded one moves; a lord (capacity 1) never does
            Assert.AreEqual(1, lord.Count);
        }

        [Test] public void Migration_StarvingSpeciesMovesToo_AndGuestsNeverBreed()
        {
            var w = new EcologyWorld();
            var a = w.Add("a", new RegionPopulation()); var b = w.Add("b", new RegionPopulation());
            w.Link("a", "b");
            var s = a.Add(new SpeciesPopulation { Id = "bug", Count = 6, Capacity = 10, Migrates = true, HungryMinutes = 3.5f, BirthsPerMinute = 5f, Dens = 3 });
            w.Migrate(0.5f);
            var g = b.Get("bug");
            Assert.AreEqual(2, g.Count);   // ceil(20% of 6)
            s.Migrates = false;            // no more migrants: only what the colony does on its own is left
            for (int i = 0; i < 50; i++) w.Tick(1f);
            Assert.LessOrEqual(g.Count, 2);   // guests only dwindle
        }
    }
}
