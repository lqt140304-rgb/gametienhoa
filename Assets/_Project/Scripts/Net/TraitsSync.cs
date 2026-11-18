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
                Send(ResultMessage, sender, result.NewHand, result.Outcome);
            }
            catch (Exception e) { Debug.LogException(e); }
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
            }
            catch (Exception e) { Debug.LogException(e); }
        }
    }
}
