namespace Evolve.Maze
{
    public enum BreathingMossPhase
    {
        Inhale = 0,
        Exhale = 1
    }

    /// <summary>
    /// Luật thuần cho 2 kỳ ngộ Tầng Nông còn lại: Hang Rêu Thở & Tổ Bỏ Hoang.
    /// </summary>
    public static class CaveEncounterWave2Rules
    {
        public const float BreathCycleSeconds = 8.0f;
        public const float ExhaleStartSeconds = 4.5f;
        public const float EggHatchSeconds = 120.0f;

        /// <summary>Kỳ ngộ Hang Rêu Thở: Hơi Thở Của Rêu (nằm yên trên rêu đúng lúc rêu thở ra).</summary>
        public sealed class BreathingMossState
        {
            public bool Completed { get; private set; }

            public static BreathingMossPhase EvaluatePhase(float timeSeconds)
            {
                float t = ((timeSeconds % BreathCycleSeconds) + BreathCycleSeconds) % BreathCycleSeconds;
                return t >= ExhaleStartSeconds ? BreathingMossPhase.Exhale : BreathingMossPhase.Inhale;
            }

            public bool TryComplete(float timeSeconds, bool onMoss, bool isStill)
            {
                if (Completed || !onMoss || !isStill) return false;
                if (EvaluatePhase(timeSeconds) != BreathingMossPhase.Exhale) return false;
                Completed = true;
                return true;
            }
        }

        /// <summary>Kỳ ngộ Tổ Bỏ Hoang: Quả Trứng Còn Sống (nuốt trứng rồi chờ nở trong Túi).</summary>
        public sealed class LivingEggState
        {
            public bool EggSwallowed { get; private set; }
            public float ElapsedSeconds { get; private set; }
            public bool Hatched { get; private set; }

            public bool TrySwallowEgg()
            {
                if (EggSwallowed) return false;
                EggSwallowed = true;
                return true;
            }

            public bool AdvanceIncubation(float deltaSeconds)
            {
                if (!EggSwallowed || Hatched || deltaSeconds <= 0f) return false;
                ElapsedSeconds += deltaSeconds;
                if (ElapsedSeconds >= EggHatchSeconds)
                {
                    Hatched = true;
                    return true;
                }
                return false;
            }
        }
    }
}
