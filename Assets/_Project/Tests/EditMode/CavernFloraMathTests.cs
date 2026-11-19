using Evolve.World;
using NUnit.Framework;
using UnityEngine;

namespace Evolve.Tests.EditMode
{
    public class CavernFloraMathTests
    {
        const CavernFloraMath.Kind Fern = CavernFloraMath.Kind.Fern, Reed = CavernFloraMath.Kind.Reed, Moss = CavernFloraMath.Kind.Moss, Bush = CavernFloraMath.Kind.Bush,
            Mushrooms = CavernFloraMath.Kind.Mushrooms, RootArc = CavernFloraMath.Kind.RootArc;

        static float Best(CavernFloraMath.Kind kind, System.Func<float, float, bool> where)
        {
            float best = 0f;
            for (float x = -46f; x <= 46f; x += 1f)
                for (float z = -46f; z <= 46f; z += 1f)
                    if (where(x, z)) best = Mathf.Max(best, CavernFloraMath.Density(kind, x, z));
            return best;
        }

        [Test]
        public void NothingGrowsInDeepWater()
        {
            foreach (CavernFloraMath.Kind k in System.Enum.GetValues(typeof(CavernFloraMath.Kind)))
            {
                if (k == CavernFloraMath.Kind.Vine) continue;
                Assert.AreEqual(0f, CavernFloraMath.Density(k, 0f, 0f), 1e-5f, k.ToString());
            }
        }

        [Test]
        public void EachKindHasItsPlaceAndKeepsToIt()
        {
            Assert.Greater(Best(Reed, (x, z) => CavernLayout.ShoreOffset(x, z) < 3.5f), 0.4f, "reeds in the shallows");
            Assert.AreEqual(0f, Best(Reed, (x, z) => CavernLayout.ShoreOffset(x, z) > 4f), 1e-5f, "no reeds away from the water");
            Assert.Greater(Best(Fern, (x, z) => CavernLayout.ShoreOffset(x, z) > 2f), 0.2f, "ferns on the damp ground");
            Assert.AreEqual(0f, Best(Fern, (x, z) => CavernLayout.ShoreOffset(x, z) < 1f), 1e-5f, "no ferns at the water");
            Assert.AreEqual(0f, Best(Bush, (x, z) => CavernLayout.ShoreOffset(x, z) < 5f), 1e-5f, "no bushes at the water");
            Assert.Greater(Best(Bush, (x, z) => CavernLayout.ShoreOffset(x, z) > 7f), 0.2f, "bushes on the drier ground");
            Assert.Greater(Best(Moss, (x, z) => true), 0.3f);
            Assert.Greater(Best(Mushrooms, (x, z) => true), 0.2f);
            Assert.Greater(Best(RootArc, (x, z) => true), 0.2f);
            Assert.AreEqual(0f, Best(RootArc, (x, z) => CavernLayout.RadiusFromCenter(x, z) < CavernLayout.FloorRadius - 8f), 1e-5f, "roots only at the wall's foot");
        }

        [Test]
        public void TheDetailKindsHaveTheirPlacesToo()
        {
            Assert.Greater(Best(CavernFloraMath.Kind.Flower, (x, z) => true), 0.2f, "flower drifts");
            Assert.Greater(Best(CavernFloraMath.Kind.Clover, (x, z) => true), 0.3f, "clover carpet");
            Assert.Greater(Best(CavernFloraMath.Kind.Leaves, (x, z) => true), 0.3f, "fallen leaves");
            Assert.Greater(Best(CavernFloraMath.Kind.Twigs, (x, z) => true), 0.2f, "twigs");
            Assert.Greater(Best(CavernFloraMath.Kind.Pebbles, (x, z) => true), 0.2f, "pebbles");
            Assert.Greater(Best(CavernFloraMath.Kind.Lichen, (x, z) => true), 0.2f, "lichen");
            Assert.AreEqual(0f, Best(CavernFloraMath.Kind.Flower, (x, z) => CavernLayout.ShoreOffset(x, z) < 2.5f), 1e-5f, "no flowers at the water");
            Assert.AreEqual(0f, Best(CavernFloraMath.Kind.Leaves, (x, z) => CavernLayout.ShoreOffset(x, z) < 4f), 1e-5f, "no fallen leaves at the water");
        }

        [Test]
        public void VinesHangRoundTheHoleNotAcrossIt()
        {
            var hole = CavernLayout.HoleCenter();
            Assert.AreEqual(0f, CavernFloraMath.VineDensity(hole.x, hole.y), 1e-5f, "not across the hole");
            float best = 0f;
            for (float a = 0f; a < Mathf.PI * 2f; a += 0.1f)
                best = Mathf.Max(best, CavernFloraMath.VineDensity(hole.x + Mathf.Cos(a) * (CavernLayout.HoleRadius + 2.5f), hole.y + Mathf.Sin(a) * (CavernLayout.HoleRadius + 2.5f)));
            Assert.Greater(best, 0.3f, "a ring of vines at the rim");
            Assert.AreEqual(0f, CavernFloraMath.VineDensity(hole.x + 40f, hole.y), 1e-5f, "none far from the light");
        }

        [Test]
        public void DensitiesStayBetweenZeroAndOne()
        {
            foreach (CavernFloraMath.Kind k in System.Enum.GetValues(typeof(CavernFloraMath.Kind)))
                for (float x = -40f; x <= 40f; x += 4f)
                    for (float z = -40f; z <= 40f; z += 4f)
                    {
                        float v = k == CavernFloraMath.Kind.Vine ? CavernFloraMath.VineDensity(x, z) : CavernFloraMath.Density(k, x, z);
                        Assert.GreaterOrEqual(v, 0f); Assert.LessOrEqual(v, 1f);
                    }
        }
    }
}
