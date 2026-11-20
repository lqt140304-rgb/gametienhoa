using System.Linq;
using Evolve.Maze;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    public class CaveEncounterWave2Tests
    {
        [Test]
        public void BreathingMoss_RequiresExhaleOnMossWhileStill()
        {
            var state = new CaveEncounterWave2Rules.BreathingMossState();

            // Inhale phase (t = 1.0s < 4.5s)
            Assert.AreEqual(BreathingMossPhase.Inhale, CaveEncounterWave2Rules.BreathingMossState.EvaluatePhase(1.0f));
            Assert.IsFalse(state.TryComplete(1.0f, onMoss: true, isStill: true));

            // Exhale phase (t = 5.0s >= 4.5s) but moving or off moss
            Assert.AreEqual(BreathingMossPhase.Exhale, CaveEncounterWave2Rules.BreathingMossState.EvaluatePhase(5.0f));
            Assert.IsFalse(state.TryComplete(5.0f, onMoss: false, isStill: true));
            Assert.IsFalse(state.TryComplete(5.0f, onMoss: true, isStill: false));

            // Exhale + on moss + still -> completes once
            Assert.IsTrue(state.TryComplete(5.0f, onMoss: true, isStill: true));
            Assert.IsTrue(state.Completed);
            Assert.IsFalse(state.TryComplete(5.5f, onMoss: true, isStill: true));
        }

        [Test]
        public void LivingEgg_HatchesOnceAfterIncubationDuration()
        {
            var state = new CaveEncounterWave2Rules.LivingEggState();

            Assert.IsFalse(state.AdvanceIncubation(200f));
            Assert.IsTrue(state.TrySwallowEgg());
            Assert.IsFalse(state.TrySwallowEgg());

            Assert.IsFalse(state.AdvanceIncubation(60f));
            Assert.IsFalse(state.Hatched);

            Assert.IsTrue(state.AdvanceIncubation(65f));
            Assert.IsTrue(state.Hatched);
            Assert.IsFalse(state.AdvanceIncubation(10f));
        }

        [Test]
        public void MazeGenerator_IncludesWave2NongCaves()
        {
            var graph = MazeGenerator.Generate(2026, 14);
            var themes = graph.Chambers.Select(c => c.Theme).ToHashSet();

            Assert.IsTrue(themes.Contains(ChamberTheme.BirthLake));
            Assert.IsTrue(themes.Contains(ChamberTheme.LumaMushroomGrove));
            Assert.IsTrue(themes.Contains(ChamberTheme.BreathingMoss));
            Assert.IsTrue(themes.Contains(ChamberTheme.AbandonedNest));
            Assert.IsTrue(themes.Contains(ChamberTheme.CrystalVein));
        }
    }
}
