using System;
using System.Collections.Generic;

namespace Evolve.Traits
{
    /// <summary>
    /// The bridge between the picking screen (assembly Evolve) and the co-op code (assembly Evolve.Net, which Evolve cannot
    /// reference). TraitsSync fills the hooks when a room is running; with none set the picker plays solo. Also holds the
    /// text codecs for the three messages, which are plain functions so they can be tested.
    /// </summary>
    public static class TraitNet
    {
        // Hooks, set by TraitsSync (host or client) and cleared when the room ends.
        public static Func<string[]> HostHand;                      // host: the host's own hand (joins the local player)
        public static Func<string, ConfirmResult> HostConfirm;      // host: the host's own pick
        public static Action<string, string[]> ClientPick;          // client: send the pick to the host
        public static bool IsClient;                                // true while connected to a host as a client
        /// <summary>The local player's traits (the one that owns a SlimeController), set by PlayerTraits.</summary>
        public static PlayerTraits Local;

        /// <summary>Client side: the host dealt (or re-dealt) this hand.</summary>
        public static event Action<string[]> HandReceived;
        /// <summary>Client side: the host's answer to the last pick.</summary>
        public static event Action<ConfirmResult> ResultReceived;
        /// <summary>The last hand a client received, kept in case the picker opens after it arrived.</summary>
        public static string[] LastHand { get; private set; }

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset()
        {
            HostHand = null; HostConfirm = null; ClientPick = null; IsClient = false; LastHand = null; Local = null;
            HandReceived = null; ResultReceived = null;
        }

        /// <summary>Forget what came from a finished co-op session.</summary>
        public static void ClearClient() { LastHand = null; IsClient = false; ClientPick = null; }

        public static void RaiseHand(string[] hand) { LastHand = hand; HandReceived?.Invoke(hand); }
        public static void RaiseResult(ConfirmResult result) { if (result.NewHand != null) LastHand = result.NewHand; ResultReceived?.Invoke(result); }

        // ---------------------------------------------------------------- codecs

        const char Sep = '\n';

        public static string EncodeHand(string[] hand) => hand == null ? "" : string.Join(Sep.ToString(), hand);

        /// <summary>Never throws: null, empty or garbage text gives an empty hand.</summary>
        public static string[] DecodeHand(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return new string[0];
            var parts = text.Split(Sep);
            var list = new List<string>(parts.Length);
            foreach (var p in parts) if (!string.IsNullOrWhiteSpace(p)) list.Add(p.Trim());
            return list.ToArray();
        }

        public static string EncodePick(string coreId, string[] flawIds) => EncodeHand(Prepend(coreId, flawIds));

        public static bool DecodePick(string text, out string coreId, out string[] flawIds)
        {
            var parts = DecodeHand(text);
            if (parts.Length < 2) { coreId = null; flawIds = new string[0]; return false; }
            coreId = parts[0];
            flawIds = new string[parts.Length - 1];
            Array.Copy(parts, 1, flawIds, 0, flawIds.Length);
            return true;
        }

        static string[] Prepend(string first, string[] rest)
        {
            var all = new List<string> { first ?? "" };
            if (rest != null) all.AddRange(rest);
            return all.ToArray();
        }
    }

    /// <summary>Checks a pick against what the core really offers.</summary>
    public static class TraitChoice
    {
        /// <summary>True when <paramref name="flawIds"/> is exactly one of the core's flaw options (order does not matter).</summary>
        public static bool IsValid(IReadOnlyList<string[]> options, string[] flawIds)
        {
            if (options == null || flawIds == null || flawIds.Length == 0) return false;
            foreach (var option in options)
            {
                if (option == null || option.Length != flawIds.Length) continue;
                var remaining = new List<string>(option);
                bool all = true;
                foreach (var id in flawIds)
                    if (!remaining.Remove(id)) { all = false; break; }
                if (all) return true;
            }
            return false;
        }
    }
}
