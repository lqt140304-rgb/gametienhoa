using System;

namespace Evolve.Maze
{
    /// <summary>
    /// Kỳ ngộ Hang Nấm Huỳnh Quang: "Vòng Nấm".
    /// Vòng nấm sáng theo nhịp; đứng giữa vòng đúng lúc bào tử sáng rực thì đổi một Khuyết Điểm (1 lần mỗi vòng).
    /// </summary>
    public sealed class SporeRingState
    {
        public const float DefaultCyclePeriod = 8.0f;
        public const float DefaultPeakWindow = 1.8f;

        readonly float _cyclePeriod;
        readonly float _peakWindow;
        float _elapsed;

        public bool Completed { get; private set; }
        public float Phase01 => (_elapsed % _cyclePeriod) / _cyclePeriod;
        public bool IsPeakGlow => Phase01 >= 1f - (_peakWindow / _cyclePeriod);

        public SporeRingState(float cyclePeriod = DefaultCyclePeriod, float peakWindow = DefaultPeakWindow)
        {
            _cyclePeriod = Math.Max(1f, cyclePeriod);
            _peakWindow = Math.Max(0.2f, Math.Min(_cyclePeriod * 0.5f, peakWindow));
        }

        /// <summary>Trả về true ở đúng frame chuyển sang trạng thái hoàn tất kỳ ngộ.</summary>
        public bool Tick(float dt, bool playerInsideRing)
        {
            if (Completed) return false;
            if (dt > 0f) _elapsed += dt;
            if (!playerInsideRing || !IsPeakGlow) return false;

            Completed = true;
            return true;
        }
    }

    /// <summary>
    /// Kỳ ngộ Suối Nước Nóng: "Tôi Luyện".
    /// Đợt nước nóng sôi theo chu kỳ; ngâm mình sống sót trọn một đợt nóng mà không chết thì tăng chịu nhiệt suốt vòng.
    /// </summary>
    public sealed class HotSpringTemperState
    {
        public const float DefaultHeatSurgeDuration = 4.0f;

        readonly float _surgeDuration;
        float _soakTimer;
        bool _wasInSurge;

        public bool Completed { get; private set; }
        public float Progress01 => Math.Min(1f, _soakTimer / _surgeDuration);

        public HotSpringTemperState(float surgeDuration = DefaultHeatSurgeDuration)
        {
            _surgeDuration = Math.Max(0.5f, surgeDuration);
        }

        /// <summary>Trả về true khi người chơi ngâm sống sót qua trọn một đợt nóng.</summary>
        public bool Tick(float dt, bool playerInWater, bool isHeatSurgeActive, bool playerAlive)
        {
            if (Completed) return false;

            if (!playerAlive || !playerInWater || !isHeatSurgeActive)
            {
                _soakTimer = 0f;
                _wasInSurge = isHeatSurgeActive;
                return false;
            }

            if (!_wasInSurge)
            {
                _soakTimer = 0f;
                _wasInSurge = true;
            }

            if (dt > 0f) _soakTimer += dt;
            if (_soakTimer < _surgeDuration) return false;

            Completed = true;
            return true;
        }
    }

    /// <summary>
    /// Kỳ ngộ Khe Gió Hú: "Túi Của Kẻ Đi Trước".
    /// Khe đá rất hẹp: chỉ slime kích thước nhỏ hơn ngưỡng mới lọt qua để nhận đồ trong Túi của kẻ chuyển sinh trước.
    /// </summary>
    public sealed class NarrowCrackStashState
    {
        public const float DefaultMaxBodyRadius = 0.45f;

        readonly float _maxBodyRadius;
        public bool Completed { get; private set; }

        public NarrowCrackStashState(float maxBodyRadius = DefaultMaxBodyRadius)
        {
            _maxBodyRadius = Math.Max(0.1f, maxBodyRadius);
        }

        public bool CanFit(float playerBodyRadius) => playerBodyRadius <= _maxBodyRadius;

        public bool TryClaim(bool playerInsideStash, float playerBodyRadius)
        {
            if (Completed || !playerInsideStash) return false;
            if (!CanFit(playerBodyRadius)) return false;
            Completed = true;
            return true;
        }
    }
}
