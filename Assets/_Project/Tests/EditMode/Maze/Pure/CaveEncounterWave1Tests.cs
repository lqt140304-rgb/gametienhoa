using Evolve.Maze;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    public class CaveEncounterWave1Tests
    {
        [Test]
        public void FirstSunbeam_TriggersOnlyOnceOnEntry()
        {
            var state = new CaveEncounterWave1Rules.FirstSunbeamState();

            Assert.IsFalse(state.TryTrigger(false));
            Assert.IsFalse(state.Triggered);

            Assert.IsTrue(state.TryTrigger(true));
            Assert.IsTrue(state.Triggered);

            Assert.IsFalse(state.TryTrigger(true));
        }

        [Test]
        public void RainbowBody_RequiresFiveDistinctColorsThenStreamDip()
        {
            var state = new CaveEncounterWave1Rules.RainbowBodyState();

            Assert.IsTrue(state.RecordOreSwallowed(0));
            Assert.IsFalse(state.RecordOreSwallowed(0)); // duplicate color
            Assert.IsTrue(state.RecordOreSwallowed(1));
            Assert.IsTrue(state.RecordOreSwallowed(2));
            Assert.IsTrue(state.RecordOreSwallowed(3));
            Assert.IsFalse(state.HasAllColors);
            Assert.IsFalse(state.TryCompleteInStream(true));

            Assert.IsTrue(state.RecordOreSwallowed(4));
            Assert.IsTrue(state.HasAllColors);
            Assert.IsFalse(state.TryCompleteInStream(false)); // not in stream yet
            Assert.IsTrue(state.TryCompleteInStream(true));
            Assert.IsTrue(state.Completed);
            Assert.IsFalse(state.TryCompleteInStream(true)); // once per run
        }

        [Test]
        public void CrystalChord_RequiresLowToHighOrder_AndResetsOnMistake()
        {
            var state = new CaveEncounterWave1Rules.CrystalChordState();

            Assert.AreEqual(ChordStepResult.ResetWrongOrder, state.HitBySonar(1));
            Assert.AreEqual(0, state.NextExpectedPitch);
            Assert.IsFalse(state.TrySwallowCore());

            Assert.AreEqual(ChordStepResult.Advanced, state.HitBySonar(0));
            Assert.AreEqual(1, state.NextExpectedPitch);

            Assert.AreEqual(ChordStepResult.ResetWrongOrder, state.HitBySonar(2));
            Assert.AreEqual(0, state.NextExpectedPitch);

            Assert.AreEqual(ChordStepResult.Advanced, state.HitBySonar(0));
            Assert.AreEqual(ChordStepResult.Advanced, state.HitBySonar(1));
            Assert.AreEqual(ChordStepResult.Completed, state.HitBySonar(2));
            Assert.IsTrue(state.FissureOpened);

            Assert.AreEqual(ChordStepResult.AlreadySolved, state.HitBySonar(0));
            Assert.IsTrue(state.TrySwallowCore());
            Assert.IsFalse(state.TrySwallowCore());
        }

        [Test]
        public void MazeGraph_ExposesSymmetricAdjacency_AndNoFloodedObstacles()
        {
            IMazeAdjacency graph = MazeGenerator.Generate(2026, 14);
            var fullGraph = (MazeGraph)graph;

            foreach (var tunnel in fullGraph.Tunnels)
            {
                Assert.AreNotEqual(ObstacleKind.Flooded, tunnel.Obstacle);
                Assert.IsTrue(graph.AreAdjacent(tunnel.FromId, tunnel.ToId));
                Assert.IsTrue(graph.AreAdjacent(tunnel.ToId, tunnel.FromId));
            }

            var birthNeighbors = graph.GetAdjacentChamberIds(fullGraph.BirthChamberId);
            Assert.GreaterOrEqual(birthNeighbors.Count, 2);
            foreach (int neighborId in birthNeighbors)
                Assert.IsTrue(graph.AreAdjacent(neighborId, fullGraph.BirthChamberId));
        }
    }
}
