using Evolve.Combat;
using Evolve.Creatures;
using NUnit.Framework;
using UnityEngine;

namespace Evolve.Tests.EditMode
{
    public class HangOneSkillsTests
    {
        [Test]
        public void BeetleRangeDoesNotStretchWithThreatMass()
        {
            var beetle = PreyDefenseSeed.Get("bo_canh_cung");
            var input = new DefenseInput { Now = 8f, EscapeReadyAt = 8f, ThreatSeen = true, ThreatRatio = 10f, ThreatDistance = 2.01f };
            Assert.That(PreyDefenseRules.Decide(beetle, input), Is.EqualTo(DefenseAction.None));
            input.ThreatDistance = 2f;
            Assert.That(PreyDefenseRules.Decide(beetle, input), Is.EqualTo(DefenseAction.Escape));
            input.Now = 7.99f;
            Assert.That(PreyDefenseRules.Decide(beetle, input), Is.EqualTo(DefenseAction.None));
            Assert.That(beetle.EscapeCooldown, Is.EqualTo(8f));
        }

        [TestCase(2.99f, 0.5f)]
        [TestCase(3f, 1.25f)]
        [TestCase(3.99f, 1.25f)]
        [TestCase(4f, 1f)]
        public void ShellDamageWindows(float elapsed, float expected)
        {
            var snail = PreyDefenseSeed.Get("oc_ho");
            Assert.That(snail.EscapeCooldown, Is.EqualTo(12f));
            Assert.That(PreyDefenseRules.GuardFactor(snail, elapsed, 3f), Is.EqualTo(expected));
        }

        [Test]
        public void TerrainWeightsDriveHabitat()
        {
            Assert.That(HangOneGround.IsWet(0.6f), Is.True);
            Assert.That(HangOneGround.IsWet(0.4f), Is.False);
            Assert.That(HangOneGround.IsMoss(0.6f), Is.True);
            Assert.That(HangOneGround.IsMoss(0.4f), Is.False);
        }

        [Test]
        public void GroundReadsThePaintedTerrainLayers()
        {
            var data = new TerrainData { heightmapResolution = 33, alphamapResolution = 16, size = new Vector3(16f, 2f, 16f) };
            var mud = new TerrainLayer { name = "Cavern_Mud" };
            var moss = new TerrainLayer { name = "Cavern_Moss" };
            data.terrainLayers = new[] { mud, moss };
            var alpha = new float[16, 16, 2];
            for (int z = 0; z < 16; z++)
                for (int x = 0; x < 16; x++) alpha[z, x, x < 8 ? 0 : 1] = 1f;
            data.SetAlphamaps(0, 0, alpha);
            var terrain = Terrain.CreateTerrainGameObject(data);
            try
            {
                terrain.transform.position = new Vector3(0f, 100f, 0f);
                Vector3 wet = terrain.transform.position + new Vector3(2f, 0f, 2f);
                Vector3 green = wet + Vector3.right * 10f;
                Assert.That(HangOneGround.Wet(wet), Is.True);
                Assert.That(HangOneGround.Moss(wet), Is.False);
                Assert.That(HangOneGround.Wet(green), Is.False);
                Assert.That(HangOneGround.Moss(green), Is.True);
                Assert.That(HangOneGround.Wet(wet + Vector3.up), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(terrain);
                Object.DestroyImmediate(data);
                Object.DestroyImmediate(mud);
                Object.DestroyImmediate(moss);
            }
        }

        [Test]
        public void TongueRangeCooldownAndMassBoundary()
        {
            Assert.That(CreatureCombat.TongueTell, Is.EqualTo(0.8f));
            Assert.That(CreatureCombat.TongueCooldown, Is.EqualTo(8f));
            Assert.That(CreatureCombat.TongueReady(8f, 8f, 6f), Is.True);
            Assert.That(CreatureCombat.TongueReady(7.99f, 8f, 6f), Is.False);
            Assert.That(CreatureCombat.TongueReady(8f, 8f, 6.01f), Is.False);
            Assert.That(CreatureCombat.CanPull(10f, 5f), Is.True);
            Assert.That(CreatureCombat.CanPull(10f, 5.01f), Is.False);
        }

        [Test]
        public void TerrainObstacleStopsTheSweep()
        {
            var wall = new GameObject("Wall");
            try
            {
                wall.transform.position = new Vector3(0f, 100f, 2f);
                wall.AddComponent<BoxCollider>();
                Physics.SyncTransforms();
                Vector3 start = new Vector3(0f, 100f, 0f);
                Assert.That(HangOneGround.Clear(start, start + Vector3.forward * 3f, 0.2f), Is.False);
                Assert.That(HangOneGround.Clear(start, start + Vector3.right * 3f, 0.2f), Is.True);
                Assert.That(HangOneGround.Clear(start, start + Vector3.forward * 6f), Is.False);
            }
            finally { Object.DestroyImmediate(wall); }
        }
    }
}
