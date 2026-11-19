using System;
using System.Collections.Generic;

namespace Evolve.Maze
{
    /// <summary>
    /// Sinh đồ thị mê cung 12–16 khoang chia 3 tầng sâu từ hạt giống (seed).
    /// Thuần C#, không phụ thuộc UnityEngine.
    /// </summary>
    public static class MazeGenerator
    {
        public const int MinChambers = 12;
        public const int MaxChambers = 16;
        public const int DefaultChambers = 14;

        public static MazeGraph Generate(int seed, int chamberCount = DefaultChambers)
        {
            if (chamberCount < MinChambers || chamberCount > MaxChambers)
                throw new ArgumentOutOfRangeException(nameof(chamberCount), $"Số khoang phải từ {MinChambers} đến {MaxChambers}.");

            var rng = new MazeRng(seed);

            // 1. Phân bổ số lượng khoang theo 3 tầng sâu
            int nongCount = chamberCount / 3;
            int giuaCount = chamberCount / 3;
            int sauCount = chamberCount - nongCount - giuaCount;

            var depths = new ChamberDepth[chamberCount];
            int idx = 0;
            for (int i = 0; i < nongCount; i++) depths[idx++] = ChamberDepth.Nong;
            for (int i = 0; i < giuaCount; i++) depths[idx++] = ChamberDepth.Giua;
            for (int i = 0; i < sauCount; i++) depths[idx++] = ChamberDepth.Sau;

            int birthId = 0;
            int sealedId = chamberCount - 1;

            // 2. Chọn các khoang đặc biệt: Hang Trú (rải mỗi tầng 1 hang) và Lối Giả (1-2 ngõ cụt)
            var kinds = new ChamberKind[chamberCount];
            for (int i = 0; i < chamberCount; i++) kinds[i] = ChamberKind.Normal;
            kinds[birthId] = ChamberKind.BirthCavern;
            kinds[sealedId] = ChamberKind.SealedCavern;

            PickSpecialInRange(ref rng, kinds, 1, nongCount, ChamberKind.Shelter);
            PickSpecialInRange(ref rng, kinds, nongCount, nongCount + giuaCount, ChamberKind.Shelter);
            PickSpecialInRange(ref rng, kinds, nongCount + giuaCount, sealedId, ChamberKind.Shelter);

            // Chọn 1–2 lối giả ở tầng Nông/Giữa/Sâu (từ các khoang Normal còn lại)
            int deadEndTarget = chamberCount >= 14 ? 2 : 1;
            var normalPool = new List<int>();
            for (int i = 1; i < sealedId; i++)
            {
                if (kinds[i] == ChamberKind.Normal) normalPool.Add(i);
            }
            rng.Shuffle(normalPool);
            for (int i = 0; i < deadEndTarget && i < normalPool.Count; i++)
                kinds[normalPool[i]] = ChamberKind.DeadEnd;

            // 3. Tạo danh sách ChamberNode với chủ đề đa dạng theo tầng sâu
            var nongThemes = new List<ChamberTheme>(ChamberThemeCatalog.NongThemes);
            var giuaThemes = new List<ChamberTheme>(ChamberThemeCatalog.GiuaThemes);
            var sauThemes = new List<ChamberTheme>(ChamberThemeCatalog.SauThemes);
            rng.Shuffle(nongThemes);
            rng.Shuffle(giuaThemes);
            rng.Shuffle(sauThemes);

            var chambers = new List<ChamberNode>(chamberCount);
            int nongThemeIdx = 0, giuaThemeIdx = 0, sauThemeIdx = 0;
            int shelterNum = 1, deadEndNum = 1;

            for (int i = 0; i < chamberCount; i++)
            {
                ChamberTheme theme = kinds[i] switch
                {
                    ChamberKind.BirthCavern => ChamberTheme.BirthLake,
                    ChamberKind.SealedCavern => ChamberTheme.SealedDragonTomb,
                    ChamberKind.Shelter => ChamberTheme.MossSanctuary,
                    ChamberKind.DeadEnd => ChamberTheme.MirageDeadEnd,
                    _ => depths[i] switch
                    {
                        ChamberDepth.Nong => nongThemes[nongThemeIdx++ % nongThemes.Count],
                        ChamberDepth.Giua => giuaThemes[giuaThemeIdx++ % giuaThemes.Count],
                        _ => sauThemes[sauThemeIdx++ % sauThemes.Count]
                    }
                };

                string name = kinds[i] switch
                {
                    ChamberKind.BirthCavern => "Hang Khởi Sinh",
                    ChamberKind.SealedCavern => "Khoang Phong Ấn",
                    ChamberKind.Shelter => $"Hang Trú #{shelterNum++}",
                    ChamberKind.DeadEnd => $"Lối Giả #{deadEndNum++}",
                    _ => ChamberThemeCatalog.Get(theme).DisplayName
                };
                chambers.Add(new ChamberNode(i, name, depths[i], kinds[i], hasHighLedge: true, theme: theme));
            }

            // 4. Tạo đồ thị hầm nối các khoang chính (không phải DeadEnd)
            var mainByDepth = new List<int>[3] { new List<int>(), new List<int>(), new List<int>() };
            var deadEnds = new List<int>();
            for (int i = 0; i < chamberCount; i++)
            {
                if (kinds[i] == ChamberKind.DeadEnd) deadEnds.Add(i);
                else mainByDepth[(int)depths[i]].Add(i);
            }

            // Xáo thứ tự trong từng tầng, giữ BirthCavern đầu và SealedCavern cuối
            for (int d = 0; d < 3; d++) rng.Shuffle(mainByDepth[d]);
            MoveToFront(mainByDepth[0], birthId);
            MoveToBack(mainByDepth[2], sealedId);

            var backbone = new List<int>();
            backbone.AddRange(mainByDepth[0]);
            backbone.AddRange(mainByDepth[1]);
            backbone.AddRange(mainByDepth[2]);

            var degree = new int[chamberCount];
            var pairSet = new HashSet<long>();
            var tunnels = new List<TunnelEdge>();
            int nextTunnelId = 0;

            void AddEdge(int a, int b, TunnelSize size, TunnelSlope slopeAB, TunnelSlope slopeBA, ObstacleKind obstacle)
            {
                long key = PairKey(a, b);
                if (a == b || !pairSet.Add(key)) return;
                tunnels.Add(new TunnelEdge(nextTunnelId++, a, b, size, slopeAB, slopeBA, obstacle));
                degree[a]++;
                degree[b]++;
            }

            // Nối đường xương sống đảm bảo liên thông từ BirthCavern tới SealedCavern
            for (int i = 0; i < backbone.Count - 1; i++)
            {
                int a = backbone[i];
                int b = backbone[i + 1];
                var (size, sAB, sBA, obs) = RollEdgeAttributes(ref rng, chambers[a], chambers[b], isBackbone: true);
                AddEdge(a, b, size, sAB, sBA, obs);
            }

            // 4b. Gắn toàn bộ Lối Giả (ngõ cụt dẫn lên cao) trước tiên để luôn có host còn trống bậc < 3
            for (int i = 0; i < deadEnds.Count; i++)
            {
                int deadId = deadEnds[i];
                int host = FindDeadEndHost(ref rng, backbone, degree, chambers[deadId].Depth, chambers);
                if (host >= 0)
                {
                    TunnelSize size = rng.NextFloat() < 0.4f ? TunnelSize.Small : TunnelSize.Medium;
                    AddEdge(host, deadId, size, TunnelSlope.Up, TunnelSlope.Down, ObstacleKind.None);
                }
            }

            // 4c. Đảm bảo mọi khoang chính đều có ít nhất 2 hầm (Birth và Sealed lúc mới tạo backbone chỉ có 1)
            for (int i = 0; i < backbone.Count; i++)
            {
                int a = backbone[i];
                if (degree[a] >= 2) continue;

                int candidate = FindPartner(ref rng, a, backbone, degree, pairSet, maxDegree: 2, chambers);
                if (candidate >= 0)
                {
                    var (size, sAB, sBA, obs) = RollEdgeAttributes(ref rng, chambers[a], chambers[candidate], isBackbone: false);
                    AddEdge(a, candidate, size, sAB, sBA, obs);
                }
            }

            // 4d. Thêm các nhánh rẽ phụ để đa số khoang có 2-3 hầm (giữ tối đa bậc 3)
            var shuffledMain = new List<int>(backbone);
            rng.Shuffle(shuffledMain);
            for (int i = 0; i < shuffledMain.Count; i++)
            {
                int a = shuffledMain[i];
                if (degree[a] >= 3) continue;
                if (rng.NextFloat() > 0.55f) continue;

                int partner = FindPartner(ref rng, a, backbone, degree, pairSet, maxDegree: 2, chambers);
                if (partner >= 0)
                {
                    var (size, sAB, sBA, obs) = RollEdgeAttributes(ref rng, chambers[a], chambers[partner], isBackbone: false);
                    AddEdge(a, partner, size, sAB, sBA, obs);
                }
            }

            return new MazeGraph(seed, birthId, sealedId, chambers, tunnels);
        }

        static void PickSpecialInRange(ref MazeRng rng, ChamberKind[] kinds, int startInclusive, int endExclusive, ChamberKind target)
        {
            var pool = new List<int>();
            for (int i = startInclusive; i < endExclusive; i++)
            {
                if (kinds[i] == ChamberKind.Normal) pool.Add(i);
            }
            if (pool.Count == 0) return;
            kinds[pool[rng.Range(0, pool.Count)]] = target;
        }

        static void MoveToFront(List<int> list, int id)
        {
            int idx = list.IndexOf(id);
            if (idx > 0) (list[0], list[idx]) = (list[idx], list[0]);
        }

        static void MoveToBack(List<int> list, int id)
        {
            int idx = list.IndexOf(id);
            int last = list.Count - 1;
            if (idx >= 0 && idx < last) (list[last], list[idx]) = (list[idx], list[last]);
        }

        static int FindPartner(
            ref MazeRng rng,
            int fromId,
            List<int> candidates,
            int[] degree,
            HashSet<long> pairSet,
            int maxDegree,
            List<ChamberNode> chambers)
        {
            var pool = new List<int>();
            int fromDepth = (int)chambers[fromId].Depth;
            for (int i = 0; i < candidates.Count; i++)
            {
                int b = candidates[i];
                if (b == fromId) continue;
                if (degree[b] > maxDegree) continue;
                if (pairSet.Contains(PairKey(fromId, b))) continue;
                // Ưu tiên nối cùng tầng hoặc tầng kề nhau
                if (Math.Abs((int)chambers[b].Depth - fromDepth) <= 1) pool.Add(b);
            }
            if (pool.Count == 0)
            {
                for (int i = 0; i < candidates.Count; i++)
                {
                    int b = candidates[i];
                    if (b == fromId || degree[b] > maxDegree || pairSet.Contains(PairKey(fromId, b))) continue;
                    pool.Add(b);
                }
            }
            if (pool.Count == 0) return -1;
            return pool[rng.Range(0, pool.Count)];
        }

        static int FindDeadEndHost(ref MazeRng rng, List<int> backbone, int[] degree, ChamberDepth preferredDepth, List<ChamberNode> chambers)
        {
            var sameDepth = new List<int>();
            var any = new List<int>();
            var fallback = new List<int>();
            for (int i = 0; i < backbone.Count; i++)
            {
                int id = backbone[i];
                if (degree[id] >= 3) continue;
                fallback.Add(id);
                if (chambers[id].Kind == ChamberKind.Shelter || chambers[id].Kind == ChamberKind.SealedCavern) continue;
                any.Add(id);
                if (chambers[id].Depth == preferredDepth) sameDepth.Add(id);
            }
            var pool = sameDepth.Count > 0 ? sameDepth : (any.Count > 0 ? any : fallback);
            if (pool.Count == 0) return -1;
            return pool[rng.Range(0, pool.Count)];
        }

        static (TunnelSize size, TunnelSlope sAB, TunnelSlope sBA, ObstacleKind obs) RollEdgeAttributes(
            ref MazeRng rng,
            ChamberNode a,
            ChamberNode b,
            bool isBackbone)
        {
            // Hang Trú luôn có lối vào hẹp (Small/Medium) để quái lớn không vào được
            TunnelSize size;
            if (a.Kind == ChamberKind.Shelter || b.Kind == ChamberKind.Shelter)
            {
                size = TunnelSize.Small;
            }
            else
            {
                float r = rng.NextFloat();
                size = r < 0.25f ? TunnelSize.Small : (r < 0.65f ? TunnelSize.Medium : TunnelSize.Large);
            }

            // Độ dốc hai đầu có thể gây hiểu nhầm (mục 9.1: đường lên chưa chắc lên mặt đất)
            TunnelSlope[] slopes = { TunnelSlope.Down, TunnelSlope.Flat, TunnelSlope.Up };
            TunnelSlope sAB = slopes[rng.Range(0, slopes.Length)];
            TunnelSlope sBA = slopes[rng.Range(0, slopes.Length)];

            // Chướng ngại tự nhiên: đường xương sống chính (isBackbone) luôn thông suốt để bước 3 đi xuyên suốt được;
            // các nhánh phụ và lối tắt có chướng ngại theo độ sâu.
            ObstacleKind obs = ObstacleKind.None;
            bool touchesBirth = a.Kind == ChamberKind.BirthCavern || b.Kind == ChamberKind.BirthCavern;
            bool touchesShelter = a.Kind == ChamberKind.Shelter || b.Kind == ChamberKind.Shelter;
            if (!isBackbone && !touchesBirth && !touchesShelter)
            {
                if (rng.NextFloat() < 0.55f)
                {
                    int maxDepth = Math.Max((int)a.Depth, (int)b.Depth);
                    obs = maxDepth switch
                    {
                        0 => rng.NextFloat() < 0.5f ? ObstacleKind.Flooded : ObstacleKind.HighWall,
                        1 => (ObstacleKind)rng.Range(1, 4), // Flooded, HighWall, CrumblingRock
                        _ => (ObstacleKind)rng.Range(1, 6)  // Toàn bộ 5 loại chướng ngại
                    };
                }
            }

            return (size, sAB, sBA, obs);
        }

        static long PairKey(int a, int b)
        {
            int lo = a < b ? a : b;
            int hi = a < b ? b : a;
            return ((long)lo << 32) | (uint)hi;
        }
    }
}
