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

        // ---------------------------------------------------------------- the deck the player picks from

        [Test]
        public void DealDeck_DistinctAndSized()
        {
            var deck = FateRoll.DealDeck(7, Pool(6, 4, 2), 8);
            Assert.AreEqual(8, deck.Length);
            Assert.AreEqual(8, deck.Select(c => c.Id).Distinct().Count(), "no core twice in one deck");
            Assert.AreEqual(5, FateRoll.DealDeck(7, Pool(2, 2, 1), 8).Length, "a small pool gives a small deck");
            Assert.AreEqual(0, FateRoll.DealDeck(7, null, 8).Length);
            Assert.AreEqual(0, FateRoll.DealDeck(7, new List<FateCard>(), 8).Length);
            Assert.AreEqual(0, FateRoll.DealDeck(7, Pool(3, 0, 0), 0).Length);
        }

        [Test]
        public void DealDeck_SameSeedSameDeck_DifferentSeedsDiffer()
        {
            var a = FateRoll.DealDeck(11, Pool(6, 4, 2), 8).Select(c => c.Id).ToArray();
            var b = FateRoll.DealDeck(11, Pool(6, 4, 2), 8).Select(c => c.Id).ToArray();
            CollectionAssert.AreEqual(a, b);
            Assert.IsTrue(Enumerable.Range(0, 30).Select(s => string.Join(",", FateRoll.DealDeck(s, Pool(6, 4, 2), 8).Select(c => c.Id))).Distinct().Count() > 10);
        }

        [Test]
        public void DealDeck_PickedCardKeepsTheRarityOdds()
        {
            // the player picks a position blindly, so the odds of what they get are the share of each rarity over all positions
            var pool = Pool(6, 4, 2);
            int n = 4000, common = 0, rare = 0, mythic = 0;
            for (int seed = 0; seed < n; seed++)
                foreach (var c in FateRoll.DealDeck(seed, pool, 8))
                { if (c.Rarity == TraitRarity.Common) common++; else if (c.Rarity == TraitRarity.Rare) rare++; else mythic++; }
            float total = n * 8f;
            Assert.AreEqual(0.65f, common / total, 0.05f);
            Assert.AreEqual(0.25f, rare / total, 0.05f);
            Assert.AreEqual(0.10f, mythic / total, 0.05f);
        }

        [Test]
        public void ShuffleOptions_SameThreeOptionsInAnOrder()
        {
            var options = new[] { new[] { "a" }, new[] { "b", "c" }, new[] { "d" } };
            var shuffled = FateRoll.ShuffleOptions(5, options);
            Assert.AreEqual(3, shuffled.Length);
            CollectionAssert.AreEquivalent(options.Select(o => string.Join("+", o)), shuffled.Select(o => string.Join("+", o)));
            CollectionAssert.AreEqual(shuffled.Select(o => string.Join("+", o)), FateRoll.ShuffleOptions(5, options).Select(o => string.Join("+", o)));
            Assert.IsTrue(Enumerable.Range(0, 30).Select(sd => string.Join("|", FateRoll.ShuffleOptions(sd, options).Select(o => string.Join("+", o)))).Distinct().Count() > 2);
            Assert.AreEqual(0, FateRoll.ShuffleOptions(1, null).Length);
        }
    }
}
