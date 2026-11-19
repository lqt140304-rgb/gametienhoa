using System;
using System.Collections.Generic;

namespace Evolve.Maze
{
    /// <summary>
    /// Luật thuần cho việc bật/tắt khoang và bố trí khoảng cách giữa các khoang trong cùng một scene (mục 9.1 & 17).
    /// Chỉ khoang hiện tại và các khoang kề được bật; các khoang còn lại tắt.
    /// </summary>
    public static class MazeActivationRules
    {
        public const float DefaultChamberSpacing = 250f;
        public const int GridColumns = 4;

        /// <summary>Trả về tập Id khoang cần bật (khoang hiện tại + các khoang kề trực tiếp).</summary>
        public static HashSet<int> GetActiveChamberIds(MazeGraph graph, int currentChamberId)
        {
            if (graph == null) throw new ArgumentNullException(nameof(graph));
            var active = new HashSet<int> { currentChamberId };
            var neighbors = graph.GetNeighborIds(currentChamberId);
            for (int i = 0; i < neighbors.Count; i++)
                active.Add(neighbors[i]);
            return active;
        }

        /// <summary>Tính tọa độ XZ đặt khoang trong scene (khoang 0 nằm tại (0, 0) để trùng Hang Khởi Sinh).</summary>
        public static (float x, float z) ComputeChamberOrigin(int chamberId, float spacing = DefaultChamberSpacing)
        {
            if (chamberId <= 0) return (0f, 0f);
            int col = chamberId % GridColumns;
            int row = chamberId / GridColumns;
            return (col * spacing, row * spacing);
        }
    }
}
