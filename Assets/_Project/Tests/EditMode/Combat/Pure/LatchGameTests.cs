using System.Collections.Generic;
using Evolve.Combat;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    public class LatchGameTests
    {
        static StruggleProfile P(float amp = 0f, int faults = 4, StruggleKind kind = StruggleKind.Even, float heavy = 0f) => new StruggleProfile(0.28f, faults, kind, amp, heavy);
        static LatchGame G(StruggleProfile p, float slime = 10f, float monster = 10f, int seed = 1) => new LatchGame(p, slime, monster, seed);

        [Test] public void Starts_HoldingAtCentre()
        {
            var g = G(P());
            Assert.AreEqual(0.5f, g.Dot); Assert.AreEqual(0, g.Faults);
            Assert.AreEqual(LatchState.Holding, g.Step(0.1f, 0f, false, 1f, 100f).State);
        }

        [Test] public void InZone_DealsDamage_BetweenEightAndSixteenPerSecond()
        {
            var g = G(P()); float total = 0f;
            for (int i = 0; i < 10; i++) total += g.Step(0.1f, 0f, false, 1f, 100f).Damage;
            Assert.GreaterOrEqual(total, 8f); Assert.LessOrEqual(total, 16f);
        }

        [Test] public void WeakPoint_DoublesDamage()
        {
            var g = G(P());
            Assert.AreEqual(1.6f, g.Step(0.1f, 0f, false, 1f, 100f).Damage, 0.001f);   // the weak point starts on the centre
            for (int i = 0; i < 9; i++) g.Step(0.1f, 0f, false, 1f, 100f);
            Assert.AreEqual(0.8f, g.Step(0.1f, 0f, false, 1f, 100f).Damage, 0.001f);   // later it has drifted away from the dot
        }

        [Test] public void SizeFactor_ScalesZone()
        {
            Assert.AreEqual(0.28f * 1.5f, G(P(), 15f, 10f).ZoneWidth, 1e-4f);
            Assert.AreEqual(0.28f * 0.6f, G(P(), 3f, 30f).ZoneWidth, 1e-4f);
        }

        [Test] public void OutOfZone_GraceThenFault()
        {
            var g = G(P());
            g.Step(0.1f, 1f, false, 1f, 100f); g.Step(0.1f, 1f, false, 1f, 100f);   // pushed outside
            g.Step(0.1f, 0f, false, 1f, 100f);
            Assert.AreEqual(0, g.Faults);
            g.Step(0.1f, 0f, false, 1f, 100f);
            Assert.AreEqual(1, g.Faults);
        }

        [Test] public void EnoughFaults_Escapes()
        {
            var g = G(P(faults: 3)); var state = LatchState.Holding;
            g.Step(0.1f, 1f, false, 1f, 100f); g.Step(0.1f, 1f, false, 1f, 100f);
            for (int i = 0; i < 40 && state == LatchState.Holding; i++) state = g.Step(0.1f, 0f, false, 1f, 100f).State;
            Assert.AreEqual(LatchState.Escaped, state); Assert.AreEqual(3, g.Faults);
        }

        [Test] public void NoEnergy_ShrinksZoneWithoutNegative()
        {
            var g = G(P());
            var s = g.Step(0.1f, 0f, false, 1f, 0f);
            Assert.AreEqual(0.28f * 0.6f, g.ZoneWidth, 1e-4f);
            Assert.AreEqual(0f, s.EnergySpent);
            Assert.GreaterOrEqual(G(P()).Step(0.1f, 0f, false, 1f, -5f).EnergySpent, 0f);
            Assert.AreEqual(0.4f, G(P()).Step(0.1f, 0f, false, 1f, 100f).EnergySpent, 1e-4f);
        }

        static float MaxExcursion(float health)
        {
            var g = G(P(0.2f)); float max = 0f;
            for (int i = 0; i < 30; i++) { g.Step(0.1f, 0f, false, health, 100f); max = System.Math.Max(max, System.Math.Abs(g.Dot - 0.5f)); }
            return max;
        }

        [Test] public void Desperate_StrugglesHarder() => Assert.Greater(MaxExcursion(0.2f), MaxExcursion(0.9f) * 1.5f);

        static LatchGame Heavy(out int steps, bool squeeze)
        {
            var g = G(P(0f, 4, StruggleKind.Even, 1000f)); steps = 0;
            g.Step(0.1f, 0f, false, 1f, 100f);
            Assert.IsTrue(g.Telegraphing);
            while (g.Telegraphing && steps < 20) { g.Step(0.1f, 0f, squeeze, 1f, 100f); steps++; }
            return g;
        }

        [Test] public void Heavy_WithSqueeze_WidensZoneNoFault()
        {
            var g = Heavy(out _, true);
            Assert.AreEqual(0, g.Faults); Assert.AreEqual(0.28f * 1.4f, g.ZoneWidth, 1e-4f);
        }

        [Test] public void Heavy_WithoutSqueeze_IsAFault() => Assert.AreEqual(1, Heavy(out _, false).Faults);

        [Test] public void MonsterDead_Wins() => Assert.AreEqual(LatchState.MonsterDead, G(P()).Step(0.1f, 0f, false, 0f, 100f).State);

        static List<float> Trace(int seed)
        {
            var g = G(P(0.5f, 4, StruggleKind.Jerky), 10f, 10f, seed); var dots = new List<float>();
            for (int i = 0; i < 50; i++) { g.Step(0.1f, 0f, false, 1f, 100f); dots.Add(g.Dot); }
            return dots;
        }

        [Test] public void SameSeed_SameTrace_DifferentSeed_Differs()
        {
            CollectionAssert.AreEqual(Trace(3), Trace(3));
            CollectionAssert.AreNotEqual(Trace(3), Trace(4));
        }
    }
}
