namespace Evolve.Maze
{
    /// <summary>Khả năng hiện tại của slime để kiểm tra kích thước hầm và chướng ngại (mục 9.4, 9.5).</summary>
    public readonly struct SlimeCapabilities
    {
        public float BodyScale { get; }
        public bool CanBreatheWater { get; }
        public bool CanClimbWall { get; }
        public bool CanSecreteAcid { get; }
        public bool HasHeatMembrane { get; }
        public bool HasStrongSonar { get; }

        public SlimeCapabilities(
            float bodyScale,
            bool canBreatheWater = false,
            bool canClimbWall = false,
            bool canSecreteAcid = false,
            bool hasHeatMembrane = false,
            bool hasStrongSonar = false)
        {
            BodyScale = bodyScale;
            CanBreatheWater = canBreatheWater;
            CanClimbWall = canClimbWall;
            CanSecreteAcid = canSecreteAcid;
            HasHeatMembrane = hasHeatMembrane;
            HasStrongSonar = hasStrongSonar;
        }
    }

    /// <summary>
    /// Luật thuần kiểm tra khe hẹp theo kích thước cơ thể và chướng ngại tự nhiên theo kỹ năng.
    /// </summary>
    public static class PassageRules
    {
        /// <summary>Khe nhỏ chỉ slime nhỏ hơn mức chuẩn (ví dụ sau khi tách Phân Thân) mới chui lọt.</summary>
        public const float SmallTunnelMaxScale = 0.95f;
        public const float MediumTunnelMaxScale = 1.45f;

        public static bool CanFitTunnelSize(TunnelSize size, float bodyScale) => size switch
        {
            TunnelSize.Small => bodyScale <= SmallTunnelMaxScale + 1e-4f,
            TunnelSize.Medium => bodyScale <= MediumTunnelMaxScale + 1e-4f,
            _ => true
        };

        public static bool CanPassObstacle(ObstacleKind obstacle, in SlimeCapabilities caps) => obstacle switch
        {
            ObstacleKind.None => true,
            ObstacleKind.Flooded => caps.CanBreatheWater,
            ObstacleKind.HighWall => caps.CanClimbWall,
            ObstacleKind.CrumblingRock => caps.CanSecreteAcid,
            ObstacleKind.HotDry => caps.HasHeatMembrane,
            ObstacleKind.PitchDark => caps.HasStrongSonar,
            _ => false
        };

        public static bool EvaluatePassage(
            TunnelSize size,
            ObstacleKind obstacle,
            in SlimeCapabilities caps,
            out string blockReason)
        {
            if (!CanFitTunnelSize(size, caps.BodyScale))
            {
                blockReason = size == TunnelSize.Small
                    ? "Khe quá nhỏ: chỉ slime nhỏ mới chui lọt (hãy tách Phân Thân hoặc giảm khối)."
                    : "Lối hầm hơi hẹp so với kích thước hiện tại.";
                return false;
            }

            if (!CanPassObstacle(obstacle, caps))
            {
                blockReason = obstacle switch
                {
                    ObstacleKind.Flooded => "Hầm ngập hoàn toàn: cần kỹ năng thở nước.",
                    ObstacleKind.HighWall => "Vách cao dựng đứng: cần kỹ năng bám vách.",
                    ObstacleKind.CrumblingRock => "Vách đá vụn chắn lối: cần kỹ năng tiết axit.",
                    ObstacleKind.HotDry => "Hầm nóng khô khắc nghiệt: cần màng chịu nhiệt.",
                    ObstacleKind.PitchDark => "Vùng tối đặc: cần Sóng Dò mạnh.",
                    _ => "Lối đi đang bị chướng ngại tự nhiên chắn."
                };
                return false;
            }

            blockReason = null;
            return true;
        }
    }
}
