using Evolve.Traits;
using NUnit.Framework;
using UnityEngine;

namespace Evolve.Tests.EditMode
{
    public class FamiliarityGridTests
    {
        static readonly Vector2 A = new Vector2(1f, 1f), B = new Vector2(10f, 1f);

        [Test]
        public void NewCellIsZero() =>
            Assert.AreEqual(0f, new FamiliarityGrid().Familiarity(A, 0f), 1e-6f);

        [Test]
        public void RevisitingRaisesFamiliarity()
        {
            var g = new FamiliarityGrid();
            float previous = 0f;
            for (int i = 0; i < 8; i++)
            {
                g.Visit(A, i * 2f); g.Visit(B, i * 2f + 1f);
                float f = g.Familiarity(A, i * 2f + 1f);
                Assert.GreaterOrEqual(f, previous);
                Assert.LessOrEqual(f, 1f);
                previous = f;
            }
            Assert.Greater(previous, 0.98f);
        }

        [Test]
        public void StandingStillDoesNotGrow()
        {
            var g = new FamiliarityGrid();
            for (int i = 0; i < 20; i++) g.Visit(A, i);
            Assert.AreEqual(TraitTuning.QuenDatGain, g.Familiarity(A, 0f), 1e-6f);
        }

        [Test]
        public void DecaysAfterLeaving()
        {
            var g = new FamiliarityGrid();
            for (int i = 0; i < 6; i++) { g.Visit(A, i); g.Visit(B, i + 0.5f); }
            Assert.Greater(g.Familiarity(A, 6f), 0.9f);
            Assert.AreEqual(0f, g.Familiarity(A, 6f + 1000f), 1e-6f);
        }

        [Test]
        public void MultiplierBounds()
        {
            var g = new FamiliarityGrid();
            Assert.AreEqual(1f, g.SpeedMultiplier(A, 0f), 1e-6f);
            for (int i = 0; i < 12; i++) { g.Visit(A, i); g.Visit(B, i + 0.5f); }
            float m = g.SpeedMultiplier(A, 12f);
            Assert.GreaterOrEqual(m, 1f);
            Assert.LessOrEqual(m, 1f + TraitTuning.QuenDatMaxBonus + 1e-6f);
        }

        [Test]
        public void CellsAreFourMetres()
        {
            var g = new FamiliarityGrid();
            g.Visit(new Vector2(0.5f, 0.5f), 0f);
            Assert.Greater(g.Familiarity(new Vector2(3.5f, 3.5f), 0f), 0f);   // same 4 m cell
            Assert.AreEqual(0f, g.Familiarity(new Vector2(4.5f, 0.5f), 0f), 1e-6f);   // next cell
            Assert.AreEqual(0f, g.Familiarity(new Vector2(-0.5f, 0.5f), 0f), 1e-6f);  // negative side is another cell
        }

        [Test]
        public void ClearResets()
        {
            var g = new FamiliarityGrid();
            g.Visit(A, 0f);
            g.Clear();
            Assert.AreEqual(0f, g.Familiarity(A, 0f), 1e-6f);
            g.Visit(A, 1f);   // the first visit after Clear counts again
            Assert.Greater(g.Familiarity(A, 1f), 0f);
        }
    }
}
