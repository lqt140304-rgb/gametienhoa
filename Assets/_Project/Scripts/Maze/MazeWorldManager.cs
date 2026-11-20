using System;
using System.Collections.Generic;
using Evolve.World;
using UnityEngine;

namespace Evolve.Maze
{
    /// <summary>
    /// Quản lý đồ thị mê cung trong scene: giữ chỉ khoang hiện tại và khoang kề được bật,
    /// xử lý hoán đổi ẩn ở giữa hầm và dịch chuyển khi bị lũ cuốn.
    /// </summary>
    public class MazeWorldManager : MonoBehaviour
    {
        public const int CurrentBuildVersion = 8;

        [SerializeField] int seed = 2026;
        [SerializeField] int chamberCount = MazeGenerator.DefaultChambers;
        [SerializeField] int currentChamberId;
        [SerializeField] int buildVersion;
        [SerializeField] List<ChamberInstance> chambers = new List<ChamberInstance>();

        readonly Dictionary<int, ChamberInstance> chamberMap = new Dictionary<int, ChamberInstance>();

        public int Seed => seed;
        public int CurrentChamberId => currentChamberId;
        public int BuildVersion => buildVersion;
        public MazeGraph Graph { get; private set; }
        public IMazeAdjacency Adjacency => Graph;

        public IReadOnlyList<int> GetAdjacentChambers(int chamberId) =>
            Graph != null ? Graph.GetAdjacentChamberIds(chamberId) : Array.Empty<int>();

        public ChamberInstance CurrentChamber =>
            chamberMap.TryGetValue(currentChamberId, out var inst) ? inst : null;

        public event Action<int, int> ChamberChanged;

        public void SetBuildVersion(int version) => buildVersion = version;

        public void Configure(int mazeSeed, int count, List<ChamberInstance> instances)
        {
            seed = mazeSeed;
            chamberCount = Mathf.Clamp(count, MazeGenerator.MinChambers, MazeGenerator.MaxChambers);
            chambers = instances ?? new List<ChamberInstance>();
            buildVersion = CurrentBuildVersion;
            RebuildRuntimeLookup();
        }

        void Awake()
        {
#if UNITY_EDITOR
            if (buildVersion < CurrentBuildVersion && InvokeEditorRebuild())
                return;
#endif
            RebuildRuntimeLookup();
            ApplyActiveChambers();
        }

#if UNITY_EDITOR
        /// <summary>Gọi MazeBuilder.Build() trong assembly Evolve.Editor để dựng lại Mê Cung ở chế độ Editor.</summary>
        public static bool InvokeEditorRebuild()
        {
            var type = Type.GetType("Evolve.EditorTools.MazeBuilder, Evolve.Editor")
                    ?? Type.GetType("Evolve.EditorTools.MazeBuilder, Assembly-CSharp-Editor");
            var method = type?.GetMethod("Build", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (method == null) return false;
            method.Invoke(null, null);
            return true;
        }
#endif

        void OnEnable() => GroundHeight.UseCustom(SampleActiveChamberGround);

        void OnDisable() => GroundHeight.Reset();

        float? SampleActiveChamberGround(float worldX, float worldZ)
        {
            var cur = CurrentChamber;
            return cur != null ? cur.SampleGroundHeight(worldX, worldZ) : null;
        }

        public void RebuildRuntimeLookup()
        {
            Graph = MazeGenerator.Generate(seed, chamberCount);
            chamberMap.Clear();
            for (int i = 0; i < chambers.Count; i++)
            {
                if (chambers[i] != null)
                    chamberMap[chambers[i].ChamberId] = chambers[i];
            }
        }

        public void SwapThroughTunnel(Transform player, int fromChamberId, int toChamberId)
        {
            if (player == null || Graph == null) return;
            if (!chamberMap.TryGetValue(toChamberId, out var destChamber)) return;

            int prev = currentChamberId;
            currentChamberId = toChamberId;
            ApplyActiveChambers();

            var arrivalPortal = destChamber.FindPortalTo(fromChamberId);
            Transform targetTransform = arrivalPortal != null ? arrivalPortal.ExitSpawnPoint : destChamber.ArrivalPoint;
            MovePlayer(player, targetTransform.position, targetTransform.rotation);

            ChamberChanged?.Invoke(prev, currentChamberId);
        }

        public void TeleportPlayerToChamber(Transform player, int targetChamberId)
        {
            if (player == null || Graph == null) return;
            if (!chamberMap.TryGetValue(targetChamberId, out var destChamber)) return;

            int prev = currentChamberId;
            currentChamberId = targetChamberId;
            ApplyActiveChambers();

            var anchor = destChamber.ArrivalPoint;
            MovePlayer(player, anchor.position, anchor.rotation);
            ChamberChanged?.Invoke(prev, currentChamberId);
        }

        public void ApplyActiveChambers()
        {
            if (Graph == null) return;
            var activeIds = MazeActivationRules.GetActiveChamberIds(Graph, currentChamberId);

            for (int i = 0; i < chambers.Count; i++)
            {
                var c = chambers[i];
                if (c == null) continue;
                bool shouldBeActive = c.KeepAlwaysActive || activeIds.Contains(c.ChamberId);
                if (c.gameObject.activeSelf != shouldBeActive)
                    c.gameObject.SetActive(shouldBeActive);
            }
        }

        static void MovePlayer(Transform player, Vector3 position, Quaternion rotation)
        {
            var cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            var rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.position = position;
                rb.linearVelocity = Vector3.zero;
            }
            player.SetPositionAndRotation(position, rotation);

            if (cc != null) cc.enabled = true;
        }
    }
}
