using System.Collections.Generic;

namespace Evolve.Maze
{
    /// <summary>
    /// Bộ sinh số giả ngẫu nhiên tất định (SplitMix32) thuần C#.
    /// Đảm bảo cùng một hạt giống luôn sinh ra đúng một mê cung trên mọi nền tảng.
    /// </summary>
    public struct MazeRng
    {
        uint state;

        public MazeRng(int seed)
        {
            state = unchecked((uint)seed);
            if (state == 0u) state = 0x9E3779B9u;
        }

        public uint NextUInt()
        {
            uint z = (state += 0x9E3779B9u);
            z = (z ^ (z >> 16)) * 0x85EBCA6Bu;
            z = (z ^ (z >> 13)) * 0xC2B2AE35u;
            return z ^ (z >> 16);
        }

        /// <summary>Trả về số nguyên trong khoảng [minInclusive, maxExclusive).</summary>
        public int Range(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive) return minInclusive;
            uint span = (uint)(maxExclusive - minInclusive);
            return minInclusive + (int)(NextUInt() % span);
        }

        /// <summary>Trả về số thực trong khoảng [0, 1).</summary>
        public float NextFloat() => (NextUInt() >> 8) / 16777216f;

        /// <summary>Xáo trộn danh sách tại chỗ theo thuật toán Fisher-Yates.</summary>
        public void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
