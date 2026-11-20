using System;
using Evolve.Traits;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Evolve.Net
{
    /// <summary>
    /// Co-op for the birth scene (spec 2026-11-19-birth-scene-design 5.6). Every player draws their own core and flaw in the scene; a client
    /// reports the result to the host with one message, and the host, after checking that the core exists and the flaws are one of its options,
    /// gives them to that client's avatar (the host rolls drops and reacts to cores for every player). Nothing is dealt or contested any more.
    /// The scene talks to this through TraitNet.
    /// </summary>
    public class TraitsSync : MonoBehaviour
    {
        const string PickMessage = "evolve.tr.pick";      // client -> host: core and flaws

        CoopManager coop;
        NetworkManager hooked;

        void Awake() => coop = GetComponent<CoopManager>();

        void OnDestroy() => EndSession();

        public void RegisterMessages(CustomMessagingManager messaging, bool server)
        {
            if (messaging == null) return;
            var nm = coop != null ? coop.Nm : null;
            if (server)
            {
                messaging.RegisterNamedMessageHandler(PickMessage, OnPick);
                return;
            }
            EndSession();
            TraitNet.IsClient = true;
            TraitNet.ClientPick = SendPick;
            if (nm == null) return;
            hooked = nm;
            nm.OnClientConnectedCallback += OnConnected;
        }

        void Update()
        {
            var nm = coop != null ? coop.Nm : null;
            if ((nm == null || !nm.IsListening) && (hooked != null || TraitNet.IsClient)) EndSession();
        }

        void EndSession()
        {
            if (hooked != null)
            {
                hooked.OnClientConnectedCallback -= OnConnected;
                hooked = null;
            }
            TraitNet.ClearClient();
        }

        // ---------------------------------------------------------------- client

        // Connected: a player who was born before joining (solo first) reports what fate gave them.
        void OnConnected(ulong clientId)
        {
            if (hooked == null || clientId != hooked.LocalClientId) return;
            var local = TraitNet.Local;
            if (local == null || local.CoreId == null || local.FlawIds.Count == 0) return;
            var flaws = new string[local.FlawIds.Count];
            for (int i = 0; i < flaws.Length; i++) flaws[i] = local.FlawIds[i];
            SendPick(local.CoreId, flaws);
        }

        void SendPick(string coreId, string[] flawIds)
        {
            var nm = coop != null ? coop.Nm : null;
            if (nm == null || !nm.IsListening || nm.IsServer) return;
            var w = new FastBufferWriter(512, Allocator.Temp);
            try
            {
                w.WriteValueSafe(TraitNet.EncodePick(coreId, flawIds));
                nm.CustomMessagingManager.SendNamedMessage(PickMessage, NetworkManager.ServerClientId, w, NetworkDelivery.ReliableSequenced);
            }
            finally { w.Dispose(); }
        }

        // ---------------------------------------------------------------- host

        void OnPick(ulong sender, FastBufferReader reader)
        {
            try
            {
                reader.ReadValueSafe(out string text);
                if (!TraitNet.DecodePick(text, out string coreId, out string[] flaws)) return;
                // Not trusted blindly: the core must exist and the flaws must be one of its options. The rarity and the seed are not checked.
                if (!TraitChoice.IsKnownPick(TraitCatalog.FateCards(), coreId, flaws)) return;
                GiveTraitsToAvatar(sender, coreId, flaws);
            }
            catch (Exception e) { Debug.LogException(e); }
        }

        /// <summary>The host reacts to every player's cores, so a client's avatar needs its core too.</summary>
        void GiveTraitsToAvatar(ulong client, string coreId, string[] flaws)
        {
            var avatar = coop != null ? coop.GetRemote(client) : null;
            if (avatar == null) return;
            var traits = avatar.GetComponent<PlayerTraits>();
            if (traits == null) traits = avatar.gameObject.AddComponent<PlayerTraits>();
            traits.Set(coreId, flaws);
        }
    }
}
