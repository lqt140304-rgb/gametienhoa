using System;
using System.Collections.Generic;
using Evolve.Organs;
using Evolve.Player;
using Evolve.Survival;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace Evolve.Net
{
    /// <summary>
    /// Co-op connection (Netcode for GameObjects, host + IP). Foundation stage: every player keeps playing their own
    /// local world and the players see each other as RemoteAvatar ghosts. No NetworkObjects or prefabs are used:
    /// each player sends their state as a small named message to the host (about 10 times a second) and the host
    /// forwards it to the other clients. The world (creatures, food, drops) is not shared yet.
    /// Created automatically when entering Play mode. F2 opens the menu (CoopMenu).
    /// </summary>
    public class CoopManager : MonoBehaviour
    {
        const string UpMessage = "evolve.up";     // client -> host: my state
        const string DownMessage = "evolve.dn";   // host -> client: someone's state (source id + state)
        const float SendInterval = 0.1f;
        const float StaleSeconds = 4f;
        const float ConnectTimeout = 10f;

        public static CoopManager Instance { get; private set; }

        public string PlayerName = "Slime";
        public string Address = "127.0.0.1";
        public ushort Port = 7777;

        NetworkManager nm;
        UnityTransport transport;
        readonly Dictionary<ulong, RemoteAvatar> remotes = new Dictionary<ulong, RemoteAvatar>();
        string statusOverride = "";
        float nextSend;
        float connectStarted;

        SurvivalStats localStats;
        OrganHost localOrgans;
        Transform localVisual;

        public bool IsActive => nm != null && nm.IsListening;
        internal NetworkManager Nm => nm;

        /// <summary>This machine's own player (not a ghost), or null if there is none in the scene.</summary>
        public SurvivalStats LocalStats
        {
            get
            {
                if (localStats == null) FindLocalPlayer();
                return localStats;
            }
        }

        internal RemoteAvatar GetRemote(ulong clientId) =>
            remotes.TryGetValue(clientId, out var avatar) && avatar != null ? avatar : null;
        public IEnumerable<RemoteAvatar> Remotes => remotes.Values;

        public string Status
        {
            get
            {
                if (nm == null || !nm.IsListening) return string.IsNullOrEmpty(statusOverride) ? "Chưa kết nối" : statusOverride;
                if (nm.IsServer) return $"Đang host · {1 + remotes.Count} người chơi";
                if (nm.IsConnectedClient) return $"Đã vào phòng · {1 + remotes.Count} người chơi";
                return $"Đang kết nối tới {Address}:{Port}...";
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            NetBridge.Available = true;
            if (FindAnyObjectByType<CoopManager>() != null) return;
            var go = new GameObject("Co-op (F2)");
            DontDestroyOnLoad(go);
            go.AddComponent<CoopManager>();
            go.AddComponent<WorldSync>();
            go.AddComponent<CoopMenu>();
        }

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (nm != null)
            {
                UnhookEvents();
                if (nm.IsListening) nm.Shutdown();
                Destroy(nm.gameObject);
            }
        }

        // ---------------------------------------------------------------- connecting

        public void Host()
        {
            if (!EnsureManager() || nm.IsListening) return;
            Application.runInBackground = true; // so a second instance on the same PC keeps running when unfocused
            transport.SetConnectionData("127.0.0.1", Port, "0.0.0.0");
            HookEvents();
            if (nm.StartHost())
            {
                RegisterMessages();
                statusOverride = "";
            }
            else statusOverride = $"Không mở được phòng (cổng {Port} đang bận?)";
        }

        public void Join()
        {
            if (!EnsureManager() || nm.IsListening) return;
            Application.runInBackground = true;
            transport.SetConnectionData(Address.Trim(), Port);
            HookEvents();
            connectStarted = Time.unscaledTime;
            if (nm.StartClient())
            {
                RegisterMessages();
                statusOverride = "";
            }
            else statusOverride = "Không bắt đầu được kết nối";
        }

        public void Disconnect()
        {
            if (nm != null && nm.IsListening) nm.Shutdown();
            ClearRemotes();
            statusOverride = "Đã ngắt kết nối";
        }

        bool EnsureManager()
        {
            if (nm != null) return true;

            // Created inactive so it can be fully configured before it wakes up.
            var go = new GameObject("Network Manager (Co-op)");
            go.SetActive(false);
            DontDestroyOnLoad(go);
            nm = go.AddComponent<NetworkManager>();
            transport = go.AddComponent<UnityTransport>();
            nm.NetworkConfig = new NetworkConfig
            {
                NetworkTransport = transport,
                EnableSceneManagement = false // every player loads their own scene; nothing to synchronise yet
            };
            go.SetActive(true);
            return true;
        }

        void HookEvents()
        {
            UnhookEvents();
            nm.OnClientDisconnectCallback += OnDisconnected;
        }

        void UnhookEvents()
        {
            if (nm != null) nm.OnClientDisconnectCallback -= OnDisconnected;
        }

        void OnDisconnected(ulong clientId)
        {
            if (nm == null) return;
            if (nm.IsServer) RemoveRemote(clientId);
            else
            {
                ClearRemotes();
                statusOverride = "Mất kết nối với chủ phòng";
            }
        }

        void RegisterMessages()
        {
            var messaging = nm.CustomMessagingManager;
            if (messaging == null) return;
            if (nm.IsServer) messaging.RegisterNamedMessageHandler(UpMessage, OnUp);
            else messaging.RegisterNamedMessageHandler(DownMessage, OnDown);
            GetComponent<WorldSync>()?.RegisterMessages(messaging, nm.IsServer);
        }

        // ---------------------------------------------------------------- per frame

        void Update()
        {
            if (nm == null || !nm.IsListening)
            {
                if (remotes.Count > 0) ClearRemotes();
                return;
            }

            PurgeStale();

            if (!nm.IsServer && !nm.IsConnectedClient)
            {
                if (Time.unscaledTime - connectStarted > ConnectTimeout)
                {
                    Disconnect();
                    statusOverride = "Không kết nối được (kiểm tra IP, cổng, tường lửa)";
                }
                return;
            }

            if (Time.unscaledTime < nextSend) return;
            nextSend = Time.unscaledTime + SendInterval;
            if (!TryBuildLocalState(out PlayerNetState state)) return;

            try
            {
                if (nm.IsServer)
                {
                    foreach (var id in nm.ConnectedClientsIds)
                        if (id != nm.LocalClientId) SendDown(id, nm.LocalClientId, state);
                }
                else SendUp(state);
            }
            catch (Exception e) { Debug.LogException(e); }
        }

        bool TryBuildLocalState(out PlayerNetState s)
        {
            s = default;
            if (localStats == null) FindLocalPlayer();
            if (localStats == null) return false;

            var st = localStats.State;
            s.Name = PlayerName;
            s.Position = localStats.transform.position;
            s.Yaw = localVisual != null ? localVisual.eulerAngles.y : 0f;
            s.Health = st.Health;
            s.MaxHealth = st.MaxHealth;
            s.Lives = (byte)Mathf.Clamp(st.Lives, 0, 255);
            s.Dead = st.IsDead;

            var list = localOrgans != null ? localOrgans.OrganList : new List<OrganSO>();
            int n = Mathf.Min(list.Count, PlayerNetState.MaxOrgans);
            s.OrganNames = new string[n];
            s.OrganLevels = new byte[n];
            for (int i = 0; i < n; i++)
            {
                s.OrganNames[i] = list[i].name;
                s.OrganLevels[i] = (byte)Mathf.Clamp(localOrgans.GetLevel(list[i]), 0, 255);
            }
            return true;
        }

        void FindLocalPlayer()
        {
            foreach (var p in SurvivalStats.All)
            {
                if (p == null || p.GetComponent<SlimeController>() == null) continue;
                localStats = p;
                localOrgans = p.GetComponent<OrganHost>();
                localVisual = p.transform.Find("Visual");
                return;
            }
        }

        // ---------------------------------------------------------------- messages

        void SendUp(PlayerNetState state)
        {
            var w = new FastBufferWriter(1024, Allocator.Temp);
            try
            {
                state.Write(ref w);
                nm.CustomMessagingManager.SendNamedMessage(UpMessage, NetworkManager.ServerClientId, w, NetworkDelivery.UnreliableSequenced);
            }
            finally { w.Dispose(); }
        }

        void SendDown(ulong target, ulong source, PlayerNetState state)
        {
            var w = new FastBufferWriter(1100, Allocator.Temp);
            try
            {
                w.WriteValueSafe(source);
                state.Write(ref w);
                nm.CustomMessagingManager.SendNamedMessage(DownMessage, target, w, NetworkDelivery.UnreliableSequenced);
            }
            finally { w.Dispose(); }
        }

        // Host: a client told me its state. Show it and pass it on to the other clients.
        void OnUp(ulong senderId, FastBufferReader reader)
        {
            try
            {
                var state = PlayerNetState.Read(ref reader);
                ApplyRemote(senderId, state);
                foreach (var id in nm.ConnectedClientsIds)
                    if (id != nm.LocalClientId && id != senderId) SendDown(id, senderId, state);
            }
            catch (Exception e) { Debug.LogException(e); }
        }

        // Client: the host told me someone's state.
        void OnDown(ulong senderId, FastBufferReader reader)
        {
            try
            {
                reader.ReadValueSafe(out ulong source);
                var state = PlayerNetState.Read(ref reader);
                if (nm != null && source == nm.LocalClientId) return;
                ApplyRemote(source, state);
            }
            catch (Exception e) { Debug.LogException(e); }
        }

        // ---------------------------------------------------------------- remote players

        void ApplyRemote(ulong id, PlayerNetState state)
        {
            if (!remotes.TryGetValue(id, out var avatar) || avatar == null)
            {
                avatar = RemoteAvatar.Create(id, state.Name);
                remotes[id] = avatar;
            }
            avatar.Apply(state, Time.unscaledTime);
        }

        void PurgeStale()
        {
            List<ulong> stale = null;
            foreach (var kv in remotes)
                if (kv.Value == null || Time.unscaledTime - kv.Value.LastSeen > StaleSeconds)
                    (stale ??= new List<ulong>()).Add(kv.Key);
            if (stale == null) return;
            foreach (var id in stale) RemoveRemote(id);
        }

        void RemoveRemote(ulong id)
        {
            if (remotes.TryGetValue(id, out var avatar) && avatar != null) Destroy(avatar.gameObject);
            remotes.Remove(id);
        }

        void ClearRemotes()
        {
            foreach (var avatar in remotes.Values)
                if (avatar != null) Destroy(avatar.gameObject);
            remotes.Clear();
        }
    }
}
