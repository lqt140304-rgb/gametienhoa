using System.Collections.Generic;
using System.Linq;
using Evolve.Traits;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    public class FateRollTests
    {
        static FateCard Card(string id, TraitRarity r) =>
            new FateCard(id, r, new[] { new[] { id + ".a" }, new[] { id + ".b" }, new[] { id + ".c" } });

        static List<FateCard> Pool(int common, int rare, int mythic)
        {
            var pool = new List<FateCard>();
            for (int i = 0; i < common; i++) pool.Add(Card("c" + i, TraitRarity.Common));
            for (int i = 0; i < rare; i++) pool.Add(Card("r" + i, TraitRarity.Rare));
            for (int i = 0; i < mythic; i++) pool.Add(Card("m" + i, TraitRarity.Mythic));
            return pool;
        }

        static float Share(IReadOnlyList<FateCard> pool, TraitRarity r, int n = 20000)
        {
            int hits = 0;
            for (int seed = 0; seed < n; seed++) if (FateRoll.Roll(seed, pool).Value.Rarity == r) hits++;
            return hits / (float)n;
        }

        [Test]
        public void Roll_SameSeedSameResult()
        {
            var pool = Pool(6, 4, 2);
            var a = FateRoll.Roll(42, pool).Value; var b = FateRoll.Roll(42, pool).Value;
            Assert.AreEqual(a.CoreId, b.CoreId); Assert.AreEqual(a.OptionIndex, b.OptionIndex);
            Assert.IsTrue(Enumerable.Range(0, 50).Select(s => FateRoll.Roll(s, pool).Value.CoreId).Distinct().Count() > 3, "different seeds give different results");
        }

        [Test]
        public void Roll_TierShares()
        {
            var pool = Pool(6, 4, 2);
            Assert.AreEqual(0.65f, Share(pool, TraitRarity.Common), 0.02f);
            Assert.AreEqual(0.25f, Share(pool, TraitRarity.Rare), 0.02f);
            Assert.AreEqual(0.10f, Share(pool, TraitRarity.Mythic), 0.02f);
        }

        [Test]
        public void Roll_UniformInsideTier()
        {
            var pool = Pool(0, 0, 2);
            int m0 = 0;
            for (int seed = 0; seed < 4000; seed++) if (FateRoll.Roll(seed, pool).Value.CoreId == "m0") m0++;
            Assert.AreEqual(0.5f, m0 / 4000f, 0.06f);
        }

        [Test]
        public void Roll_EmptyTier_Renormalises()
        {
            var pool = Pool(6, 0, 2);   // no Rare cards: Common 0.65 / (0.65 + 0.10)
            Assert.AreEqual(0.65f / 0.75f, Share(pool, TraitRarity.Common), 0.02f);
        }

        [Test]
        public void Roll_OnlyOneTier_AlwaysThatTier() => Assert.AreEqual(1f, Share(Pool(5, 0, 0), TraitRarity.Common, 500), 1e-6f);

        [Test]
        public void Roll_FlawIdsAreExactlyOneOfTheOptions()
        {
            var pool = Pool(3, 3, 3);
            for (int seed = 0; seed < 300; seed++)
            {
                var r = FateRoll.Roll(seed, pool).Value;
                var card = pool.First(c => c.Id == r.CoreId);
                CollectionAssert.AreEqual(card.Options[r.OptionIndex], r.FlawIds);
                Assert.That(r.OptionIndex, Is.InRange(0, 2));
            }
        }

        [Test]
        public void Roll_EmptyOrNull_ReturnsNull()
        {
            Assert.IsNull(FateRoll.Roll(1, new List<FateCard>()));
            Assert.IsNull(FateRoll.Roll(1, null));
        }

        [Test]
        public void PlayerSeed_DiffersByPlayer_AndIsStable()
        {
            Assert.AreNotEqual(FateRoll.PlayerSeed(1, 0), FateRoll.PlayerSeed(1, 1));
            Assert.AreEqual(FateRoll.PlayerSeed(7, 3), FateRoll.PlayerSeed(7, 3));
        }

        [Test]
        public void SeedFateCards_HasTwelveWithRarity()
        {
            var cards = TraitSeed.FateCards();
            Assert.AreEqual(12, cards.Count);
            Assert.AreEqual(6, cards.Count(c => c.Rarity == TraitRarity.Common));
            Assert.AreEqual(4, cards.Count(c => c.Rarity == TraitRarity.Rare));
            Assert.AreEqual(2, cards.Count(c => c.Rarity == TraitRarity.Mythic));
            string[] mythic = { "core.con_bac", "core.giu_goc" };
            CollectionAssert.AreEquivalent(mythic, cards.Where(c => c.Rarity == TraitRarity.Mythic).Select(c => c.Id));
            string[] rare = { "core.than_toc", "core.nguy_sac", "core.phan_chan", "core.quen_dat" };
            CollectionAssert.AreEquivalent(rare, cards.Where(c => c.Rarity == TraitRarity.Rare).Select(c => c.Id));
            foreach (var c in cards) Assert.AreEqual(3, c.Options.Length, c.Id);
        }
    }
}
