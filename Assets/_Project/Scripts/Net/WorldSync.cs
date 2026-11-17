using System;
using System.Collections.Generic;
using System.Linq;
using Evolve.Creatures;
using Evolve.Organs;
using Evolve.Survival;
using Evolve.World;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Evolve.Net
{
    /// <summary>
    /// The shared world (co-op 0.4b, part 1). The HOST runs the world as in single player: creatures, food and drops.
    /// A CLIENT hides its own copies and shows replicas of what the host reports, and tells the host what its
    /// player did.
    ///
    /// Host -> client: creature positions (10x/s), food left and drops (2x/s), bites, food eaten.
    /// Client -> host: hits on predators, drops taken, drops to create (when the client's player dies).
    ///
    /// Remote players are ghosts (RemoteAvatar with SurvivalStats.IsRemote); creatures and food react to them by
    /// distance on the host, and what happens to them is sent to their own machine through NetWorld.
    /// </summary>
    public class WorldSync : MonoBehaviour
    {
        const string CreaturesMessage = "evolve.cr";  // host -> client
        const string ItemsMessage = "evolve.it";      // host -> client
        const string BiteMessage = "evolve.bite";     // host -> client
        const string FeedMessage = "evolve.feed";     // host -> client
        const string HitMessage = "evolve.hit";       // client -> host
        const string TakeMessage = "evolve.take";     // client -> host
        const string SpawnMessage = "evolve.spawn";   // client -> host

        const byte KindPrey = 0;
        const byte KindPredator = 1;
        const int MaxCreatures = 40;
        const int MaxPickups = 30;
        const float ReplicaStale = 1.5f;

        class Replica
        {
            public GameObject Go;
            public ReplicaMotion Motion;
            public Predator Predator; // null for prey
        }

        CoopManager coop;

        // host
        float nextCreatures, nextItems, nextFeed;
        readonly Dictionary<ulong, float> pendingFeed = new Dictionary<ulong, float>();

        // client
        bool clientMode;
        Prey preyTemplate;
        Predator predatorTemplate;
        readonly Dictionary<int, Replica> creatures = new Dictionary<int, Replica>();
        readonly Dictionary<int, OrganPickup> pickups = new Dictionary<int, OrganPickup>();
        readonly Dictionary<int, float> takenRecently = new Dictionary<int, float>();
        readonly List<GameObject> hiddenLocal = new List<GameObject>();
        readonly List<Behaviour> disabledSpawners = new List<Behaviour>();
        bool nutrientMismatchLogged;

        void Awake()
        {
            coop = GetComponent<CoopManager>();
            NetWorld.IsClient = false;
            NetWorld.ReportPredatorHit = ReportPredatorHit;
            NetWorld.ReportPickupTaken = ReportPickupTaken;
            NetWorld.RequestPickupSpawn = RequestPickupSpawn;
            NetWorld.BiteRemote = BiteRemote;
            NetWorld.FeedRemote = FeedRemote;
        }

        void OnDestroy()
        {
            ExitClientMode();
            NetWorld.ReportPredatorHit = null;
            NetWorld.ReportPickupTaken = null;
            NetWorld.RequestPickupSpawn = null;
            NetWorld.BiteRemote = null;
            NetWorld.FeedRemote = null;
        }

        /// <summary>Called by CoopManager right after the network session starts.</summary>
        public void RegisterMessages(CustomMessagingManager messaging, bool server)
        {
            if (messaging == null) return;
            if (server)
            {
                messaging.RegisterNamedMessageHandler(HitMessage, OnHit);
                messaging.RegisterNamedMessageHandler(TakeMessage, OnTake);
                messaging.RegisterNamedMessageHandler(SpawnMessage, OnSpawn);
            }
            else
            {
                messaging.RegisterNamedMessageHandler(CreaturesMessage, OnCreatures);
                messaging.RegisterNamedMessageHandler(ItemsMessage, OnItems);
                messaging.RegisterNamedMessageHandler(BiteMessage, OnBite);
                messaging.RegisterNamedMessageHandler(FeedMessage, OnFeed);
            }
        }

        void Update()
        {
            var nm = coop.Nm;
            if (nm == null || !nm.IsListening)
            {
                if (clientMode) ExitClientMode();
                pendingFeed.Clear();
                return;
            }
            if (nm.IsServer) HostTick(nm);
            else ClientTick(nm);
        }

        // ================================================================= host

        void HostTick(NetworkManager nm)
        {
            bool anyClient = false;
            foreach (var id in nm.ConnectedClientsIds)
                if (id != nm.LocalClientId) { anyClient = true; break; }
            if (!anyClient) { pendingFeed.Clear(); return; }

            float now = Time.unscaledTime;
            if (now >= nextCreatures) { nextCreatures = now + 0.1f; SendCreatures(nm); }
            if (now >= nextItems) { nextItems = now + 0.5f; SendItems(nm); }
            if (now >= nextFeed) { nextFeed = now + 0.1f; FlushFeed(nm); }
        }

        void SendToClients(NetworkManager nm, string message, FastBufferWriter w, NetworkDelivery delivery)
        {
            foreach (var id in nm.ConnectedClientsIds)
                if (id != nm.LocalClientId) nm.CustomMessagingManager.SendNamedMessage(message, id, w, delivery);
        }

        void SendCreatures(NetworkManager nm)
        {
            var list = new List<(int id, byte kind, Vector3 pos, float yaw, byte hp)>();
            foreach (var p in Prey.Active)
                if (p != null && !p.IsReplica && list.Count < MaxCreatures)
                    list.Add((p.NetId, KindPrey, p.transform.position, p.transform.eulerAngles.y, (byte)255));
            foreach (var p in Predator.Active)
                if (p != null && !p.IsReplica && list.Count < MaxCreatures)
                    list.Add((p.NetId, KindPredator, p.transform.position, p.transform.eulerAngles.y,
                        (byte)Mathf.RoundToInt(Mathf.Clamp01(p.Health / Mathf.Max(1f, p.MaxHealth)) * 255f)));

            var w = new FastBufferWriter(1200, Allocator.Temp);
            try
            {
                w.WriteValueSafe((byte)list.Count);
                foreach (var e in list)
                {
                    w.WriteValueSafe(e.id);
                    w.WriteValueSafe(e.kind);
                    w.WriteValueSafe(e.pos.x);
                    w.WriteValueSafe(e.pos.y);
                    w.WriteValueSafe(e.pos.z);
                    w.WriteValueSafe(e.yaw);
                    w.WriteValueSafe(e.hp);
                }
                SendToClients(nm, CreaturesMessage, w, NetworkDelivery.UnreliableSequenced);
            }
            finally { w.Dispose(); }
        }

        static List<Nutrient> SortedNutrients() =>
            Nutrient.All.Where(n => n != null)
                .OrderBy(n => n.transform.position.x)
                .ThenBy(n => n.transform.position.z)
                .ToList();

        void SendItems(NetworkManager nm)
        {
            var nutrients = SortedNutrients();
            var drops = new List<OrganPickup>();
            foreach (var p in OrganPickup.Active)
                if (p != null && !p.IsReplica && p.Organ != null && drops.Count < MaxPickups) drops.Add(p);

            var w = new FastBufferWriter(4096, Allocator.Temp);
            try
            {
                w.WriteValueSafe((ushort)Mathf.Min(nutrients.Count, 400));
                for (int i = 0; i < nutrients.Count && i < 400; i++)
                    w.WriteValueSafe((byte)Mathf.RoundToInt(nutrients[i].Fraction * 255f));

                w.WriteValueSafe((byte)drops.Count);
                foreach (var p in drops)
                {
                    Vector3 pos = p.transform.position;
                    w.WriteValueSafe(p.NetId);
                    w.WriteValueSafe(p.Organ.name);
                    w.WriteValueSafe(pos.x);
                    w.WriteValueSafe(pos.y);
                    w.WriteValueSafe(pos.z);
                }
                SendToClients(nm, ItemsMessage, w, NetworkDelivery.ReliableFragmentedSequenced);
            }
            finally { w.Dispose(); }
        }

        void FlushFeed(NetworkManager nm)
        {
            if (pendingFeed.Count == 0) return;
            foreach (var kv in pendingFeed)
            {
                var w = new FastBufferWriter(32, Allocator.Temp);
                try
                {
                    w.WriteValueSafe(kv.Value);
                    nm.CustomMessagingManager.SendNamedMessage(FeedMessage, kv.Key, w, NetworkDelivery.Reliable);
                }
                finally { w.Dispose(); }
            }
            pendingFeed.Clear();
        }

        // What the game asks of the host when something happens to a remote player.
        void BiteRemote(SurvivalStats stats, float damage, Vector3 knockback)
        {
            var nm = coop.Nm;
            if (nm == null || !nm.IsListening || !nm.IsServer || stats == null) return;
            var w = new FastBufferWriter(64, Allocator.Temp);
            try
            {
                w.WriteValueSafe(damage);
                w.WriteValueSafe(knockback.x);
                w.WriteValueSafe(knockback.z);
                nm.CustomMessagingManager.SendNamedMessage(BiteMessage, stats.RemoteClientId, w, NetworkDelivery.Reliable);
            }
            finally { w.Dispose(); }
        }

        void FeedRemote(SurvivalStats stats, float amount)
        {
            if (stats == null) return;
            pendingFeed.TryGetValue(stats.RemoteClientId, out float have);
            pendingFeed[stats.RemoteClientId] = have + amount;
        }

        // A client reports what its player did.
        void OnHit(ulong sender, FastBufferReader reader)
        {
            try
            {
                reader.ReadValueSafe(out int id);
                reader.ReadValueSafe(out float damage);
                var avatar = coop.GetRemote(sender);
                if (avatar == null) return;
                damage = Mathf.Clamp(damage, 0f, 40f);
                foreach (var p in Predator.Active)
                {
                    if (p == null || p.IsReplica || p.NetId != id) continue;
                    p.TakeDamage(damage, avatar.Stats);
                    break;
                }
            }
            catch (Exception e) { Debug.LogException(e); }
        }

        void OnTake(ulong sender, FastBufferReader reader)
        {
            try
            {
                reader.ReadValueSafe(out int id);
                foreach (var p in OrganPickup.Active)
                {
                    if (p == null || p.IsReplica || p.NetId != id) continue;
                    Destroy(p.gameObject);
                    break;
                }
            }
            catch (Exception e) { Debug.LogException(e); }
        }

        void OnSpawn(ulong sender, FastBufferReader reader)
        {
            try
            {
                reader.ReadValueSafe(out string organName);
                reader.ReadValueSafe(out float x);
                reader.ReadValueSafe(out float y);
                reader.ReadValueSafe(out float z);
                var organ = RemoteAvatar.FindOrgan(organName);
                if (organ != null) OrganPickup.Spawn(organ, new Vector3(x, y, z));
            }
            catch (Exception e) { Debug.LogException(e); }
        }

        // ================================================================= client: entering and leaving

        void ClientTick(NetworkManager nm)
        {
            if (!nm.IsConnectedClient) return;
            if (!clientMode) EnterClientMode();

            float now = Time.unscaledTime;
            List<int> stale = null;
            foreach (var kv in creatures)
                if (kv.Value.Go == null || now - kv.Value.Motion.LastSeen > ReplicaStale)
                    (stale ??= new List<int>()).Add(kv.Key);
            if (stale != null) foreach (var id in stale) RemoveCreature(id);
        }

        // Hide this machine's own creatures, food and drops: the host's world replaces them.
        void EnterClientMode()
        {
            clientMode = true;
            nutrientMismatchLogged = false;
            NetWorld.IsClient = true;
            Nutrient.ClientMode = true;

            foreach (var s in FindObjectsByType<PreySpawner>())
            {
                if (preyTemplate == null) preyTemplate = s.Prefab;
                if (s.enabled) { s.enabled = false; disabledSpawners.Add(s); }
            }
            foreach (var s in FindObjectsByType<PredatorSpawner>())
            {
                if (predatorTemplate == null) predatorTemplate = s.Prefab;
                if (s.enabled) { s.enabled = false; disabledSpawners.Add(s); }
            }
            foreach (var p in new List<Prey>(Prey.Active)) Hide(p.gameObject);
            foreach (var p in new List<Predator>(Predator.Active)) Hide(p.gameObject);
            foreach (var p in new List<OrganPickup>(OrganPickup.Active)) Hide(p.gameObject);

            if (preyTemplate == null || predatorTemplate == null)
                Debug.LogWarning("Co-op: không tìm thấy PreySpawner/PredatorSpawner trong scene nên chưa hiện được quái của chủ phòng.");
        }

        void Hide(GameObject go)
        {
            go.SetActive(false);
            hiddenLocal.Add(go);
        }

        void ExitClientMode()
        {
            if (!clientMode) return;
            clientMode = false;
            NetWorld.IsClient = false;
            Nutrient.ClientMode = false;

            foreach (var r in creatures.Values) if (r.Go != null) Destroy(r.Go);
            creatures.Clear();
            foreach (var p in pickups.Values) if (p != null) Destroy(p.gameObject);
            pickups.Clear();
            takenRecently.Clear();

            foreach (var go in hiddenLocal) if (go != null) go.SetActive(true);
            hiddenLocal.Clear();
            foreach (var s in disabledSpawners) if (s != null) s.enabled = true;
            disabledSpawners.Clear();
            foreach (var n in Nutrient.All) if (n != null) n.RefreshLocal();
        }

        // ================================================================= client: what the host reports

        void OnCreatures(ulong sender, FastBufferReader reader)
        {
            try
            {
                reader.ReadValueSafe(out byte count);
                float now = Time.unscaledTime;
                for (int i = 0; i < count; i++)
                {
                    reader.ReadValueSafe(out int id);
                    reader.ReadValueSafe(out byte kind);
                    reader.ReadValueSafe(out float x);
                    reader.ReadValueSafe(out float y);
                    reader.ReadValueSafe(out float z);
                    reader.ReadValueSafe(out float yaw);
                    reader.ReadValueSafe(out byte hp);
                    if (clientMode) ApplyCreature(id, kind, new Vector3(x, y, z), yaw, hp, now);
                }
            }
            catch (Exception e) { Debug.LogException(e); }
        }

        void ApplyCreature(int id, byte kind, Vector3 position, float yaw, byte hp, float now)
        {
            if (!creatures.TryGetValue(id, out var replica) || replica.Go == null)
            {
                replica = CreateReplica(id, kind, position, yaw);
                if (replica == null) return;
                creatures[id] = replica;
            }
            replica.Motion.SetTarget(position, yaw, now);
            if (replica.Predator != null) replica.Predator.SetNetHealth(hp / 255f);
        }

        Replica CreateReplica(int id, byte kind, Vector3 position, float yaw)
        {
            GameObject go;
            Predator replicaPredator = null;
            if (kind == KindPrey)
            {
                if (preyTemplate == null) return null;
                var prey = Instantiate(preyTemplate, position, Quaternion.Euler(0f, yaw, 0f));
                prey.IsReplica = true;
                prey.NetId = id;
                go = prey.gameObject;
            }
            else
            {
                if (predatorTemplate == null) return null;
                var predator = Instantiate(predatorTemplate, position, Quaternion.Euler(0f, yaw, 0f));
                predator.IsReplica = true;
                predator.NetId = id;
                go = predator.gameObject;
                replicaPredator = predator;
            }
            return new Replica { Go = go, Motion = go.AddComponent<ReplicaMotion>(), Predator = replicaPredator };
        }

        void RemoveCreature(int id)
        {
            if (creatures.TryGetValue(id, out var r) && r.Go != null) Destroy(r.Go);
            creatures.Remove(id);
        }

        void OnItems(ulong sender, FastBufferReader reader)
        {
            try
            {
                reader.ReadValueSafe(out ushort nutrientCount);
                var fractions = new byte[nutrientCount];
                for (int i = 0; i < nutrientCount; i++)
                {
                    reader.ReadValueSafe(out byte b);
                    fractions[i] = b;
                }

                reader.ReadValueSafe(out byte pickupCount);
                var seen = new HashSet<int>();
                float now = Time.unscaledTime;
                for (int i = 0; i < pickupCount; i++)
                {
                    reader.ReadValueSafe(out int id);
                    reader.ReadValueSafe(out string organName);
                    reader.ReadValueSafe(out float x);
                    reader.ReadValueSafe(out float y);
                    reader.ReadValueSafe(out float z);
                    seen.Add(id);
                    if (clientMode) ApplyPickup(id, organName, new Vector3(x, y, z), now);
                }

                if (!clientMode) return;
                ApplyNutrients(fractions);
                RemovePickupsNotIn(seen);
            }
            catch (Exception e) { Debug.LogException(e); }
        }

        void ApplyPickup(int id, string organName, Vector3 position, float now)
        {
            if (takenRecently.TryGetValue(id, out float takenAt) && now - takenAt < 3f) return;
            if (pickups.TryGetValue(id, out var existing) && existing != null) return;
            var organ = RemoteAvatar.FindOrgan(organName);
            if (organ == null) return;
            pickups[id] = OrganPickup.CreateReplica(organ, position, id);
        }

        void RemovePickupsNotIn(HashSet<int> seen)
        {
            List<int> gone = null;
            foreach (var kv in pickups)
                if (kv.Value == null || !seen.Contains(kv.Key)) (gone ??= new List<int>()).Add(kv.Key);
            if (gone == null) return;
            foreach (var id in gone)
            {
                if (pickups[id] != null) Destroy(pickups[id].gameObject);
                pickups.Remove(id);
            }
        }

        void ApplyNutrients(byte[] fractions)
        {
            var mine = SortedNutrients();
            if (mine.Count != fractions.Length)
            {
                if (!nutrientMismatchLogged)
                {
                    nutrientMismatchLogged = true;
                    Debug.LogWarning($"Co-op: số cục thức ăn khác nhau (bạn {mine.Count}, chủ phòng {fractions.Length}). Hai bên cần cùng một scene; thức ăn sẽ không đồng bộ.");
                }
                return;
            }
            for (int i = 0; i < mine.Count; i++) mine[i].NetApply(fractions[i] / 255f);
        }

        void OnBite(ulong sender, FastBufferReader reader)
        {
            try
            {
                reader.ReadValueSafe(out float damage);
                reader.ReadValueSafe(out float kx);
                reader.ReadValueSafe(out float kz);
                var stats = coop.LocalStats;
                if (stats == null || stats.State.IsDead) return;
                stats.State.Damage(damage);
                var rb = stats.GetComponent<Rigidbody>();
                if (rb != null) rb.AddForce(new Vector3(kx, 0f, kz) + Vector3.up * 2f, ForceMode.VelocityChange);
            }
            catch (Exception e) { Debug.LogException(e); }
        }

        void OnFeed(ulong sender, FastBufferReader reader)
        {
            try
            {
                reader.ReadValueSafe(out float amount);
                var stats = coop.LocalStats;
                if (stats != null && !stats.State.IsDead) stats.State.Feed(amount);
            }
            catch (Exception e) { Debug.LogException(e); }
        }

        // ================================================================= client: what this player did

        bool CanReportToHost =>
            coop != null && coop.Nm != null && coop.Nm.IsListening && !coop.Nm.IsServer && coop.Nm.IsConnectedClient;

        void ReportPredatorHit(int id, float damage)
        {
            if (!CanReportToHost) return;
            var w = new FastBufferWriter(32, Allocator.Temp);
            try
            {
                w.WriteValueSafe(id);
                w.WriteValueSafe(damage);
                coop.Nm.CustomMessagingManager.SendNamedMessage(HitMessage, NetworkManager.ServerClientId, w, NetworkDelivery.Reliable);
            }
            finally { w.Dispose(); }
        }

        void ReportPickupTaken(int id)
        {
            takenRecently[id] = Time.unscaledTime;
            if (pickups.TryGetValue(id, out var p) && p == null) pickups.Remove(id);
            if (!CanReportToHost) return;
            var w = new FastBufferWriter(16, Allocator.Temp);
            try
            {
                w.WriteValueSafe(id);
                coop.Nm.CustomMessagingManager.SendNamedMessage(TakeMessage, NetworkManager.ServerClientId, w, NetworkDelivery.Reliable);
            }
            finally { w.Dispose(); }
        }

        void RequestPickupSpawn(string organName, Vector3 position)
        {
            if (!CanReportToHost) return;
            var w = new FastBufferWriter(128, Allocator.Temp);
            try
            {
                w.WriteValueSafe(organName ?? "");
                w.WriteValueSafe(position.x);
                w.WriteValueSafe(position.y);
                w.WriteValueSafe(position.z);
                coop.Nm.CustomMessagingManager.SendNamedMessage(SpawnMessage, NetworkManager.ServerClientId, w, NetworkDelivery.Reliable);
            }
            finally { w.Dispose(); }
        }
    }
}
