using System;
using System.Collections.Generic;

namespace Evolve.Traits
{
    /// <summary>
    /// Deals each player a hand of core cards. Within a hand the cards are always different; across hands they are different
    /// as long as there are enough cores (players x hand size), and only overlap when the stock runs short.
    /// </summary>
    public static class CardDealer
    {
        public static string[][] Deal(int seed, int players, IReadOnlyList<string> coreIds, int hand = TraitTuning.HandSize)
        {
            if (players <= 0 || coreIds == null || coreIds.Count == 0 || hand <= 0) return new string[0][];
            int n = coreIds.Count, size = Math.Min(hand, n);
            var deck = new List<string>(coreIds);
            var rnd = new Random(seed);
            for (int i = deck.Count - 1; i > 0; i--)   // Fisher-Yates
            {
                int j = rnd.Next(i + 1);
                (deck[i], deck[j]) = (deck[j], deck[i]);
            }
            var hands = new string[players][];
            for (int p = 0; p < players; p++)
            {
                hands[p] = new string[size];
                for (int k = 0; k < size; k++) hands[p][k] = deck[(p * size + k) % n];   // size <= n, so a hand never repeats a card
            }
            return hands;
        }
    }
}
