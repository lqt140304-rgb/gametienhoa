using System;
using System.Collections.Generic;

namespace Evolve.Traits
{
    public enum ConfirmOutcome { Accepted, TakenReplaced, Invalid }

    public readonly struct ConfirmResult
    {
        public readonly ConfirmOutcome Outcome;
        /// <summary>The player's hand after the call (for TakenReplaced: the taken card swapped for a free one).</summary>
        public readonly string[] NewHand;

        public ConfirmResult(ConfirmOutcome outcome, string[] newHand) { Outcome = outcome; NewHand = newHand; }
    }

    /// <summary>
    /// The host's record of a room's core cards: each player's hand, which cores are taken, who owns them.
    /// No timing: whoever's confirm is processed first gets the core, the later one is given a replacement card.
    /// Pure logic, so it can be tested; the network layer only feeds it joins, confirms and leaves.
    /// </summary>
    public sealed class TraitRoom
    {
        readonly int seed;
        readonly List<string> cores;
        readonly Dictionary<int, List<string>> hands = new Dictionary<int, List<string>>();
        readonly Dictionary<string, int> owners = new Dictionary<string, int>();
        readonly Dictionary<int, string> chosen = new Dictionary<int, string>();

        public TraitRoom(int seed, IReadOnlyList<string> coreIds)
        {
            this.seed = seed;
            cores = coreIds != null ? new List<string>(coreIds) : new List<string>();
        }

        public string[] Hand(int playerId) => hands.TryGetValue(playerId, out var h) ? h.ToArray() : new string[0];
        public bool IsLocked(string coreId) => coreId != null && owners.ContainsKey(coreId);
        /// <summary>The player who took this core, or null if it is free.</summary>
        public int? OwnerOf(string coreId) => coreId != null && owners.TryGetValue(coreId, out var id) ? id : (int?)null;

        /// <summary>Deals the player a hand (3 cards when the stock allows) from the cores nobody has taken, preferring cards not in another hand.</summary>
        public string[] Join(int playerId)
        {
            if (hands.TryGetValue(playerId, out var existing)) return existing.ToArray();
            var hand = new List<string>();
            hands[playerId] = hand;
            FillHand(playerId, hand);
            return hand.ToArray();
        }

        public ConfirmResult Confirm(int playerId, string coreId)
        {
            if (coreId == null || !hands.TryGetValue(playerId, out var hand) || chosen.ContainsKey(playerId) || !hand.Contains(coreId))
                return new ConfirmResult(ConfirmOutcome.Invalid, Hand(playerId));

            if (owners.ContainsKey(coreId))
            {
                hand.Remove(coreId);       // taken by someone else: swap for a free card
                FillHand(playerId, hand);
                return new ConfirmResult(ConfirmOutcome.TakenReplaced, hand.ToArray());
            }
            owners[coreId] = playerId;
            chosen[playerId] = coreId;
            return new ConfirmResult(ConfirmOutcome.Accepted, hand.ToArray());
        }

        /// <summary>The player left the room: their hand goes away and the core they took becomes free again.</summary>
        public void Leave(int playerId)
        {
            hands.Remove(playerId);
            if (chosen.TryGetValue(playerId, out var core)) { owners.Remove(core); chosen.Remove(playerId); }
        }

        void FillHand(int playerId, List<string> hand)
        {
            int target = Math.Min(TraitTuning.HandSize, cores.Count);
            var inOthers = new HashSet<string>();
            foreach (var kv in hands)
                if (kv.Key != playerId) foreach (var c in kv.Value) inOthers.Add(c);

            var fresh = new List<string>();     // free cards nobody else holds
            var shared = new List<string>();    // free cards another player also holds
            foreach (var c in cores)
            {
                if (owners.ContainsKey(c) || hand.Contains(c)) continue;
                (inOthers.Contains(c) ? shared : fresh).Add(c);
            }
            var rnd = new Random(unchecked(seed * 31 + playerId * 7919 + hand.Count));
            Shuffle(fresh, rnd);
            Shuffle(shared, rnd);
            foreach (var c in fresh) { if (hand.Count >= target) break; hand.Add(c); }
            foreach (var c in shared) { if (hand.Count >= target) break; hand.Add(c); }
        }

        static void Shuffle(List<string> list, Random rnd)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rnd.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
