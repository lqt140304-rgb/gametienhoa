using System;
using Evolve.Civilization;
using Evolve.Survival;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Evolve.Net
{
    /// <summary>
    /// Co-op for the treefolk (spec 2026-11-17-treefolk-grudge-design): the host runs their brains and sends every client,
    /// ten times a second, where each one is and what it is doing, whether the cave is closed, and that client's own
    /// standing with each tribe. A client's hits and offerings go to the host, which applies them with that client's avatar.
    /// </summary>
    public class TreefolkSync : MonoBehaviour
    {
        const string StateMessage = "evolve.tf";      // host -> client
        const string HitMessage = "evolve.tfhit";     // client -> host
        const string OfferMessage = "evolve.tfoff";   // client -> host

        CoopManager coop;
        float nextSend;
        bool clientMode;

        void Awake()
        {
            coop = GetComponent<CoopManager>();
            NetWorld.ReportTreefolkHit = ReportHit;
            NetWorld.ReportOffering = ReportOffering;
        }

        void OnDestroy()
        {
            SetClientMode(false);
            NetWorld.ReportTreefolkHit = null;
            NetWorld.ReportOffering = null;
        }

        public void RegisterMessages(CustomMessagingManager messaging, bool server)
        {
            if (messaging == null) return;
            if (server)
            {
                messaging.RegisterNamedMessageHandler(HitMessage, OnHit);
                messaging.RegisterNamedMessageHandler(OfferMessage, OnOffer);
            }
            else messaging.RegisterNamedMessageHandler(StateMessage, OnState);
        }

        void Update()
        {
            var nm = coop != null ? coop.Nm : null;
            bool listening = nm != null && nm.IsListening;
            SetClientMode(listening && !nm.IsServer);
            if (!listening || !nm.IsServer || Time.unscaledTime < nextSend) return;
            nextSend = Time.unscaledTime + 0.1f;
            foreach (var id in nm.ConnectedClientsIds)
                if (id != nm.LocalClientId) SendState(nm, id);
        }

        /// <summary>On a client every treefolk and tribe only mirrors the host.</summary>
        void SetClientMode(bool on)
        {
            if (on == clientMode) return;
            clientMode = on;
            foreach (var t in Treefolk.Active) if (t != null) t.IsReplica = on;
            foreach (var tribe in TreefolkTribe.All) if (tribe != null) tribe.IsReplica = on;
        }

        void SendState(NetworkManager nm, ulong client)
        {
            var avatar = coop.GetRemote(client);
            var stats = avatar != null ? avatar.Stats : null;
            var w = new FastBufferWriter(1400, Allocator.Temp);
            try
            {
                w.WriteValueSafe((byte)TreefolkTribe.All.Count);
                foreach (var tribe in TreefolkTribe.All)
                {
                    w.WriteValueSafe(tribe.TribeId);
                    w.WriteValueSafe(tribe.GatesClosed);
                    w.WriteValueSafe(stats != null ? tribe.Ledger.Get(stats) : 0f);
                }
                int count = Mathf.Min(Treefolk.Active.Count, 60);
                w.WriteValueSafe((byte)count);
                for (int i = 0; i < count; i++)
                {
                    var t = Treefolk.Active[i];
                    byte flags = (byte)((t.Alarmed ? 1 : 0) | (t.IsDead ? 2 : 0) | (t.Attacking ? 4 : 0));
                    w.WriteValueSafe(t.Index);
                    w.WriteValueSafe(t.transform.position.x);
                    w.WriteValueSafe(t.transform.position.y);
                    w.WriteValueSafe(t.transform.position.z);
                    w.WriteValueSafe(t.transform.eulerAngles.y);
                    w.WriteValueSafe(flags);
                }
                nm.CustomMessagingManager.SendNamedMessage(StateMessage, client, w, NetworkDelivery.UnreliableSequenced);
            }
            finally { w.Dispose(); }
        }

        void OnState(ulong sender, FastBufferReader reader)
        {
            try
            {
                reader.ReadValueSafe(out byte tribes);
                for (int i = 0; i < tribes; i++)
                {
                    reader.ReadValueSafe(out int id);
                    reader.ReadValueSafe(out bool closed);
                    reader.ReadValueSafe(out float value);
                    foreach (var tribe in TreefolkTribe.All)
                        if (tribe != null && tribe.TribeId == id) { tribe.ClientValue = value; tribe.SetGates(closed); }
                }
                reader.ReadValueSafe(out byte count);
                for (int i = 0; i < count; i++)
                {
                    reader.ReadValueSafe(out int index);
                    reader.ReadValueSafe(out float x); reader.ReadValueSafe(out float y); reader.ReadValueSafe(out float z);
                    reader.ReadValueSafe(out float yaw);
                    reader.ReadValueSafe(out byte flags);
                    foreach (var t in Treefolk.Active)
                        if (t != null && t.Index == index) { t.SetReplicaState(new Vector3(x, y, z), yaw, (flags & 1) != 0, (flags & 2) != 0, (flags & 4) != 0); break; }
                }
            }
            catch (Exception e) { Debug.LogException(e); }
        }

        void ReportHit(int index, float damage)
        {
            var nm = coop != null ? coop.Nm : null;
            if (nm == null || !nm.IsListening || nm.IsServer) return;
            var w = new FastBufferWriter(16, Allocator.Temp);
            try
            {
                w.WriteValueSafe(index);
                w.WriteValueSafe(damage);
                nm.CustomMessagingManager.SendNamedMessage(HitMessage, NetworkManager.ServerClientId, w, NetworkDelivery.Reliable);
            }
            finally { w.Dispose(); }
        }

        void ReportOffering(int tribeId)
        {
            var nm = coop != null ? coop.Nm : null;
            if (nm == null || !nm.IsListening || nm.IsServer) return;
            var w = new FastBufferWriter(8, Allocator.Temp);
            try
            {
                w.WriteValueSafe(tribeId);
                nm.CustomMessagingManager.SendNamedMessage(OfferMessage, NetworkManager.ServerClientId, w, NetworkDelivery.Reliable);
            }
            finally { w.Dispose(); }
        }

        void OnHit(ulong sender, FastBufferReader reader)
        {
            try
            {
                reader.ReadValueSafe(out int index);
                reader.ReadValueSafe(out float damage);
                var avatar = coop.GetRemote(sender);
                if (avatar == null) return;
                damage = Mathf.Clamp(damage, 0f, 40f);
                foreach (var t in Treefolk.Active)
                    if (t != null && !t.IsReplica && t.Index == index) { t.TakeDamage(damage, avatar.Stats); return; }
            }
            catch (Exception e) { Debug.LogException(e); }
        }

        void OnOffer(ulong sender, FastBufferReader reader)
        {
            try
            {
                reader.ReadValueSafe(out int tribeId);
                var avatar = coop.GetRemote(sender);
                if (avatar == null) return;
                foreach (var tribe in TreefolkTribe.All)
                    if (tribe != null && tribe.TribeId == tribeId) { tribe.Offer(avatar.Stats); return; }
            }
            catch (Exception e) { Debug.LogException(e); }
        }
    }
}
