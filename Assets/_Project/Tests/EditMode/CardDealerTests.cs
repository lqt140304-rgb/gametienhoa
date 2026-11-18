using System.Collections.Generic;
using System.Linq;
using Evolve.Traits;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    public class CardDealerTests
    {
        static List<string> Cores(int n) => Enumerable.Range(0, n).Select(i => "core." + i).ToList();

        [Test]
        public void DealGivesThreeDistinctPerHand()
        {
            foreach (int players in new[] { 1, 4, 8 })
                foreach (var hand in CardDealer.Deal(7, players, Cores(12)))
                {
                    Assert.AreEqual(3, hand.Length);
                    Assert.AreEqual(3, hand.Distinct().Count());
                }
        }

        [Test]
        public void DealIsDistinctAcrossPlayersWhenEnoughCores()
        {
            var hands = CardDealer.Deal(3, 8, Cores(24));
            var all = hands.SelectMany(h => h).ToList();
            Assert.AreEqual(24, all.Count);
            Assert.AreEqual(24, all.Distinct().Count());
        }

        [Test]
        public void DealOverlapsOnlyWhenShort()
        {
            var hands = CardDealer.Deal(3, 8, Cores(12));
            Assert.AreEqual(8, hands.Length);
            foreach (var h in hands) Assert.AreEqual(3, h.Distinct().Count());
            Assert.AreEqual(12, hands.SelectMany(h => h).Distinct().Count());
        }

        [Test]
        public void DealDeterministicBySeed()
        {
            var a = CardDealer.Deal(42, 5, Cores(12));
            var b = CardDealer.Deal(42, 5, Cores(12));
            for (int i = 0; i < a.Length; i++) CollectionAssert.AreEqual(a[i], b[i]);
        }

        [Test]
        public void DealSoloAndEmpty()
        {
            Assert.AreEqual(3, CardDealer.Deal(1, 1, Cores(12))[0].Length);
            Assert.AreEqual(0, CardDealer.Deal(1, 0, Cores(12)).Length);
            Assert.AreEqual(0, CardDealer.Deal(1, 4, new List<string>()).Length);
            Assert.AreEqual(0, CardDealer.Deal(1, 4, null).Length);
            foreach (var h in CardDealer.Deal(1, 3, Cores(2))) Assert.AreEqual(2, h.Length);
        }
    }
}
