using System;
using System.Collections.Generic;

namespace Evolve.Maze
{
    /// <summary>
    /// Đồ thị mê cung khoang: 12–16 khoang chia 3 tầng sâu (Nông, Giữa, Sâu).
    /// Bất biến theo seed trong suốt một vòng chơi.
    /// </summary>
    public sealed class MazeGraph
    {
        readonly Dictionary<int, ChamberNode> chambersById = new Dictionary<int, ChamberNode>();
        readonly Dictionary<int, List<TunnelEdge>> tunnelsByChamber = new Dictionary<int, List<TunnelEdge>>();

        public int Seed { get; }
        public int BirthChamberId { get; }
        public int SealedChamberId { get; }
        public IReadOnlyList<ChamberNode> Chambers { get; }
        public IReadOnlyList<TunnelEdge> Tunnels { get; }

        public MazeGraph(
            int seed,
            int birthChamberId,
            int sealedChamberId,
            IReadOnlyList<ChamberNode> chambers,
            IReadOnlyList<TunnelEdge> tunnels)
        {
            Seed = seed;
            BirthChamberId = birthChamberId;
            SealedChamberId = sealedChamberId;
            Chambers = chambers ?? throw new ArgumentNullException(nameof(chambers));
            Tunnels = tunnels ?? throw new ArgumentNullException(nameof(tunnels));

            foreach (var c in chambers)
            {
                chambersById[c.Id] = c;
                tunnelsByChamber[c.Id] = new List<TunnelEdge>();
            }

            foreach (var t in tunnels)
            {
                if (tunnelsByChamber.TryGetValue(t.FromId, out var fromList)) fromList.Add(t);
                if (tunnelsByChamber.TryGetValue(t.ToId, out var toList)) toList.Add(t);
            }
        }

        public ChamberNode GetChamber(int id)
        {
            if (chambersById.TryGetValue(id, out var node)) return node;
            throw new KeyNotFoundException($"Không tìm thấy khoang {id}.");
        }

        public bool TryGetChamber(int id, out ChamberNode node) => chambersById.TryGetValue(id, out node);

        public IReadOnlyList<TunnelEdge> GetTunnels(int chamberId)
        {
            if (tunnelsByChamber.TryGetValue(chamberId, out var list)) return list;
            throw new KeyNotFoundException($"Không tìm thấy khoang {chamberId}.");
        }

        public int CountTunnels(int chamberId) => GetTunnels(chamberId).Count;

        public IReadOnlyList<int> GetNeighborIds(int chamberId)
        {
            var list = GetTunnels(chamberId);
            var result = new List<int>(list.Count);
            for (int i = 0; i < list.Count; i++)
                result.Add(list[i].OtherEnd(chamberId));
            return result;
        }

        /// <summary>Kiểm tra có đường đi từ khoang nguồn tới khoang đích hay không (BFS).</summary>
        public bool HasPath(int fromId, int toId) => FindPath(fromId, toId).Count > 0;

        /// <summary>Tìm đường đi ngắn nhất theo số khoang (trả về danh sách rỗng nếu không tới được).</summary>
        public IReadOnlyList<int> FindPath(int fromId, int toId)
        {
            if (!chambersById.ContainsKey(fromId) || !chambersById.ContainsKey(toId))
                return Array.Empty<int>();
            if (fromId == toId)
                return new[] { fromId };

            var visited = new HashSet<int> { fromId };
            var parent = new Dictionary<int, int>();
            var queue = new Queue<int>();
            queue.Enqueue(fromId);

            while (queue.Count > 0)
            {
                int cur = queue.Dequeue();
                var edges = tunnelsByChamber[cur];
                for (int i = 0; i < edges.Count; i++)
                {
                    int next = edges[i].OtherEnd(cur);
                    if (!visited.Add(next)) continue;
                    parent[next] = cur;
                    if (next == toId)
                        return ReconstructPath(parent, fromId, toId);
                    queue.Enqueue(next);
                }
            }

            return Array.Empty<int>();
        }

        /// <summary>Kiểm tra mọi khoang đều tới được từ Hang Khởi Sinh.</summary>
        public bool IsFullyConnected()
        {
            if (Chambers.Count == 0) return false;
            var visited = new HashSet<int> { BirthChamberId };
            var queue = new Queue<int>();
            queue.Enqueue(BirthChamberId);

            while (queue.Count > 0)
            {
                int cur = queue.Dequeue();
                var edges = tunnelsByChamber[cur];
                for (int i = 0; i < edges.Count; i++)
                {
                    int next = edges[i].OtherEnd(cur);
                    if (visited.Add(next)) queue.Enqueue(next);
                }
            }

            return visited.Count == Chambers.Count;
        }

        static IReadOnlyList<int> ReconstructPath(Dictionary<int, int> parent, int fromId, int toId)
        {
            var path = new List<int>();
            int cur = toId;
            while (cur != fromId)
            {
                path.Add(cur);
                cur = parent[cur];
            }
            path.Add(fromId);
            path.Reverse();
            return path;
        }
    }
}
