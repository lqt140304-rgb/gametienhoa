using System;
using Evolve.Traits;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Evolve.Net
{
    /// <summary>
    /// Co-op for the starting choices (spec 2026-11-18-starting-choices-design): the host owns one TraitRoom, deals each
    /// player a hand of core cards and decides, in the order picks arrive, who gets a core. A client sends its pick and gets
    /// either "accepted" or a hand with the taken card swapped. The picking screen talks to this through TraitNet.
    /// </summary>
    public class TraitsSync : MonoBehaviour
    {
        const string HandMessage = "evolve.tr.hand";      // host -> client: its hand
        const string PickMessage = "evolve.tr.pick";      // client -> host: core and flaws
        const string ResultMessage = "evolve.tr.result";  // host -> client: outcome and hand

        CoopManager coop;
        TraitRoom room;
        NetworkManager hooked;
        bool autoPicked;   // a client that chose before joining has already offered that core to the host

        void Awake() => coop = GetComponent<CoopManager>();

        void OnDestroy() => EndRoom();

        public void RegisterMessages(CustomMessagingManager messaging, bool server)
        {
            if (messaging == null) return;
            var nm = coop != null ? coop.Nm : null;
            if (server)
            {
                messaging.RegisterNamedMessageHandler(PickMessage, OnPick);
                StartRoom(nm);
            }
            else
            {
                messaging.RegisterNamedMessageHandler(HandMessage, OnHand);
                messaging.RegisterNamedMessageHandler(ResultMessage, OnResult);
                TraitNet.IsClient = true;
                TraitNet.ClientPick = SendPick;
            }
        }

        void Update()
        {
            var nm = coop != null ? coop.Nm : null;
            if ((nm == null || !nm.IsListening) && (hooked != null || TraitNet.IsClient)) EndRoom();
        }

        // ---------------------------------------------------------------- host

        void StartRoom(NetworkManager nm)
        {
            EndRoom();
            var ids = new System.Collections.Generic.List<string>();
            foreach (var c in TraitCatalog.Cores) ids.Add(c.Id);
            room = new TraitRoom(Environment.TickCount, ids);
            hooked = nm;
            if (nm != null)
            {
                nm.OnClientConnectedCallback += OnClientConnected;
                nm.OnClientDisconnectCallback += OnClientDisconnected;
            }
            // A host who chose a core before the room existed (solo first) keeps it, and nobody else can take it.
            var local = TraitNet.Local;
            if (local != null && local.CoreId != null) room.Claim(HostId(), local.CoreId);
            TraitNet.HostHand = () => room != null ? room.Join(HostId()) : new string[0];
            TraitNet.HostConfirm = coreId => room != null ? room.Confirm(HostId(), coreId) : new ConfirmResult(ConfirmOutcome.Invalid, new string[0]);
        }

        int HostId() => hooked != null ? (int)hooked.LocalClientId : 0;

        void EndRoom()
        {
            if (hooked != null)
            {
                hooked.OnClientConnectedCallback -= OnClientConnected;
                hooked.OnClientDisconnectCallback -= OnClientDisconnected;
                hooked = null;
            }
            room = null;
            autoPicked = false;
            TraitNet.ClearClient();
            TraitNet.HostHand = null; TraitNet.HostConfirm = null; TraitNet.ClientPick = null; TraitNet.IsClient = false;
        }

        void OnClientConnected(ulong clientId)
        {
            if (room == null || hooked == null || clientId == hooked.LocalClientId) return;
            Send(HandMessage, clientId, room.Join((int)clientId), null);
        }

        void OnClientDisconnected(ulong clientId) => room?.Leave((int)clientId);

        void OnPick(ulong sender, FastBufferReader reader)
        {
            try
            {
                reader.ReadValueSafe(out string text);
                if (room == null || !TraitNet.DecodePick(text, out string coreId, out string[] flaws)) return;
                var core = TraitCatalog.Core(coreId);
                var options = new System.Collections.Generic.List<string[]>();
                if (core != null) foreach (var o in core.Options) options.Add(o.FlawIds);
                var result = TraitChoice.IsValid(options, flaws)
                    ? room.Confirm((int)sender, coreId)
                    : new ConfirmResult(ConfirmOutcome.Invalid, room.Hand((int)sender));
                // A client who chose before joining offers a core that is not in the hand it was dealt: lock it if it is still free.
                if (result.Outcome == ConfirmOutcome.Invalid && TraitChoice.IsValid(options, flaws) && Array.IndexOf(room.Hand((int)sender), coreId) < 0)
                    result = room.Claim((int)sender, coreId)
                        ? new ConfirmResult(ConfirmOutcome.Accepted, room.Hand((int)sender))
                        : new ConfirmResult(ConfirmOutcome.TakenReplaced, room.Hand((int)sender));
                if (result.Outcome == ConfirmOutcome.Accepted) GiveTraitsToAvatar(sender, coreId, flaws);
                Send(ResultMessage, sender, result.NewHand, result.Outcome);
            }
            catch (Exception e) { Debug.LogException(e); }
        }

        /// <summary>The host rolls drops for every player, so a client's avatar needs its core too (Nhân Con Bạc).</summary>
        void GiveTraitsToAvatar(ulong client, string coreId, string[] flaws)
        {
            var avatar = coop != null ? coop.GetRemote(client) : null;
            if (avatar == null) return;
            var traits = avatar.GetComponent<PlayerTraits>();
            if (traits == null) traits = avatar.gameObject.AddComponent<PlayerTraits>();
            traits.Set(coreId, flaws);
        }

        void Send(string name, ulong client, string[] hand, ConfirmOutcome? outcome)
        {
            var nm = coop != null ? coop.Nm : null;
            if (nm == null || !nm.IsListening) return;
            var w = new FastBufferWriter(512, Allocator.Temp);
            try
            {
                if (outcome.HasValue) w.WriteValueSafe((byte)outcome.Value);
                w.WriteValueSafe(TraitNet.EncodeHand(hand));
                nm.CustomMessagingManager.SendNamedMessage(name, client, w, NetworkDelivery.ReliableSequenced);
            }
            finally { w.Dispose(); }
        }

        // ---------------------------------------------------------------- client

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

        void OnHand(ulong sender, FastBufferReader reader)
        {
            try
            {
                reader.ReadValueSafe(out string text);
                TraitNet.RaiseHand(TraitNet.DecodeHand(text));
                // Chose a core before joining: offer it to the host once, so the room locks it.
                var local = TraitNet.Local;
                if (!autoPicked && local != null && local.CoreId != null && local.FlawIds.Count > 0)
                {
                    autoPicked = true;
                    var flaws = new string[local.FlawIds.Count];
                    for (int i = 0; i < flaws.Length; i++) flaws[i] = local.FlawIds[i];
                    SendPick(local.CoreId, flaws);
                }
            }
            catch (Exception e) { Debug.LogException(e); }
        }

        void OnResult(ulong sender, FastBufferReader reader)
        {
            try
            {
                reader.ReadValueSafe(out byte outcome);
                reader.ReadValueSafe(out string text);
                if (outcome > (byte)ConfirmOutcome.Invalid) return;
                TraitNet.RaiseResult(new ConfirmResult((ConfirmOutcome)outcome, TraitNet.DecodeHand(text)));
                // The core chosen before joining was taken by someone else: give it up so the picker opens again with the new hand.
                var local = TraitNet.Local;
                if (outcome != (byte)ConfirmOutcome.Accepted && local != null && local.CoreId != null) local.Set(null, null);
            }
            catch (Exception e) { Debug.LogException(e); }
        }
    }
}
