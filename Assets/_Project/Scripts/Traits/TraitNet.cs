using System;
using System.Collections.Generic;

namespace Evolve.Traits
{
    /// <summary>
    /// The bridge between the birth scene (assembly Evolve) and the co-op code (assembly Evolve.Net, which Evolve cannot reference).
    /// TraitsSync fills ClientPick when this machine is a client; the scene calls it with what fate gave. Also holds the text codecs
    /// for the pick message, plain functions so they can be tested.
    /// </summary>
    public static class TraitNet
    {
        /// <summary>Client: send the core and flaws fate gave this player to the host. Set by TraitsSync, cleared when the session ends.</summary>
        public static Action<string, string[]> ClientPick;
        /// <summary>True while connected to a host as a client.</summary>
        public static bool IsClient;
        /// <summary>The local player's traits (the one that owns a SlimeController), set by PlayerTraits.</summary>
        public static PlayerTraits Local;

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset()
        {
            ClientPick = null; IsClient = false; Local = null;
        }

        /// <summary>Forget what came from a finished co-op session.</summary>
        public static void ClearClient() { IsClient = false; ClientPick = null; }

        // ---------------------------------------------------------------- codecs

        const char Sep = '\n';

        public static string EncodeHand(string[] hand) => hand == null ? "" : string.Join(Sep.ToString(), hand);

        /// <summary>Never throws: null, empty or garbage text gives an empty list.</summary>
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
}
