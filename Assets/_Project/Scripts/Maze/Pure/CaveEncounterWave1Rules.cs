using System.Collections.Generic;

namespace Evolve.Maze
{
    public enum ChordStepResult
    {
        Advanced = 0,
        Completed = 1,
        ResetWrongOrder = 2,
        AlreadySolved = 3
    }

    /// <summary>
    /// Luật thuần cho 3 kỳ ngộ của 3 hang đầu tiên (Hang Khởi Sinh, Vườn Quặng Cầu Vồng, Vực Nứt Tím).
    /// Không phụ thuộc UnityEngine để kiểm thử bằng cả cstest và EditMode.
    /// </summary>
    public static class CaveEncounterWave1Rules
    {
        public const int RequiredRainbowColors = 5;
        public const int RequiredCrystalCount = 3;

        /// <summary>Kỳ ngộ Hang Khởi Sinh: Tia Nắng Đầu Tiên (kích hoạt một lần khi nổi vào tia nắng lần đầu).</summary>
        public sealed class FirstSunbeamState
        {
            public bool Triggered { get; private set; }

            public bool TryTrigger(bool inSunbeam)
            {
                if (Triggered || !inSunbeam) return false;
                Triggered = true;
                return true;
            }
        }

        /// <summary>Kỳ ngộ Vườn Quặng Cầu Vồng: Cầu Vồng Trong Thân (nuốt đủ 5 màu quặng khác nhau rồi ngâm suối).</summary>
        public sealed class RainbowBodyState
        {
            readonly HashSet<int> absorbedColors = new HashSet<int>();

            public int AbsorbedCount => absorbedColors.Count;
            public bool HasAllColors => absorbedColors.Count >= RequiredRainbowColors;
            public bool Completed { get; private set; }

            public bool RecordOreSwallowed(int colorIndex)
            {
                if (Completed || colorIndex < 0 || colorIndex >= RequiredRainbowColors)
                    return false;
                return absorbedColors.Add(colorIndex);
            }

            public bool TryCompleteInStream(bool inStream)
            {
                if (Completed || !inStream || !HasAllColors)
                    return false;
                Completed = true;
                return true;
            }
        }

        /// <summary>
        /// Kỳ ngộ Vực Nứt Tím: Hợp Âm Tinh Thể.
        /// 3 tinh thể mang cao độ 0 (Trầm), 1 (Trung), 2 (Cao). Sóng Dò trúng đúng thứ tự 0 -> 1 -> 2 sẽ nứt toác khe vực,
        /// lộ tinh thể lõi để slime nuốt.
        /// </summary>
        public sealed class CrystalChordState
        {
            public int NextExpectedPitch { get; private set; }
            public bool FissureOpened { get; private set; }
            public bool CoreSwallowed { get; private set; }

            public ChordStepResult HitBySonar(int pitchIndex)
            {
                if (FissureOpened) return ChordStepResult.AlreadySolved;
                if (pitchIndex < 0 || pitchIndex >= RequiredCrystalCount)
                {
                    NextExpectedPitch = 0;
                    return ChordStepResult.ResetWrongOrder;
                }

                if (pitchIndex == NextExpectedPitch)
                {
                    NextExpectedPitch++;
                    if (NextExpectedPitch >= RequiredCrystalCount)
                    {
                        FissureOpened = true;
                        return ChordStepResult.Completed;
                    }
                    return ChordStepResult.Advanced;
                }

                NextExpectedPitch = pitchIndex == 0 ? 1 : 0;
                return ChordStepResult.ResetWrongOrder;
            }

            public bool TrySwallowCore()
            {
                if (!FissureOpened || CoreSwallowed) return false;
                CoreSwallowed = true;
                return true;
            }
        }
    }
}
