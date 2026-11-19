using System;

namespace Evolve.Maze
{
    /// <summary>Tầng sâu của khoang trong mê cung (mục 9.1).</summary>
    public enum ChamberDepth
    {
        /// <summary>Tầng Nông: sáng hơn, gần Hang Khởi Sinh, ít nguy hiểm.</summary>
        Nong = 0,
        /// <summary>Tầng Giữa: tối hơn, độ ẩm thay đổi, quái vừa.</summary>
        Giua = 1,
        /// <summary>Tầng Sâu: tối đặc, khô, ít thức ăn, quái mạnh và Khoang Phong Ấn.</summary>
        Sau = 2
    }

    /// <summary>Loại khoang trong mê cung (mục 9.1, 9.2, 9.6).</summary>
    public enum ChamberKind
    {
        /// <summary>Hang Khởi Sinh: khoang xuất phát có hồ và lỗ trần thấy trời.</summary>
        BirthCavern = 0,
        /// <summary>Khoang thường: có tầng thấp ngập lũ và gờ cao trú lũ.</summary>
        Normal = 1,
        /// <summary>Hang trú: khoang nhỏ cao hơn mực lũ, lối vào hẹp, có rêu ẩm.</summary>
        Shelter = 2,
        /// <summary>Khoang Phong Ấn: ở tầng Sâu, nơi của sinh vật cổ đại và bộ xương rồng.</summary>
        SealedCavern = 3,
        /// <summary>Lối giả: đường dẫn lên cao nhưng là ngõ cụt.</summary>
        DeadEnd = 4
    }

    /// <summary>Kích thước đường hầm (mục 9.4).</summary>
    public enum TunnelSize
    {
        /// <summary>Khe nhỏ: chỉ slime nhỏ lọt, an toàn trước quái to.</summary>
        Small = 0,
        /// <summary>Hầm vừa: slime trung bình đi qua.</summary>
        Medium = 1,
        /// <summary>Hầm lớn: quái to đi lại được.</summary>
        Large = 2
    }

    /// <summary>Độ dốc tương đối của đầu hầm (đường lên chưa chắc lên mặt đất).</summary>
    public enum TunnelSlope
    {
        Down = -1,
        Flat = 0,
        Up = 1
    }

    /// <summary>Chướng ngại tự nhiên chặn hầm (mục 9.5 - cơ thể là chìa khóa).</summary>
    public enum ObstacleKind
    {
        None = 0,
        /// <summary>Hầm ngập: cần thở nước.</summary>
        Flooded = 1,
        /// <summary>Vách cao: cần bám vách.</summary>
        HighWall = 2,
        /// <summary>Vách đá vụn: cần tiết axit.</summary>
        CrumblingRock = 3,
        /// <summary>Hầm nóng khô: cần màng chịu nhiệt.</summary>
        HotDry = 4,
        /// <summary>Vùng tối đặc: cần Sóng Dò mạnh.</summary>
        PitchDark = 5
    }

    /// <summary>Chủ đề sinh thái & môi trường của từng khoang (vách, ánh sáng, thực vật, thức ăn, loài quái).</summary>
    public enum ChamberTheme
    {
        // Khoang đặc biệt
        BirthLake = 0,
        MossSanctuary = 1,
        MirageDeadEnd = 2,
        SealedDragonTomb = 3,

        // Tầng Nông
        LumaMushroomGrove = 10,
        RootCanopy = 11,
        DampReedHollow = 12,
        AmberPebbleGarden = 13,

        // Tầng Giữa
        LimestoneStalactite = 20,
        CrystalVein = 21,
        SporeCurtain = 22,
        DeepRootTangle = 23,

        // Tầng Sâu
        EmberBasalt = 30,
        SaltAbyss = 31,
        FossilBoneyard = 32
    }

    /// <summary>Kỳ ngộ hoặc bẫy/cơ chế môi trường riêng của từng khoang (lấy cảm hứng từ kỳ ngộ TFT).</summary>
    public enum ChamberEncounterKind
    {
        None = 0,
        /// <summary>Suối Nguồn Cổ Đại: hồi đầy máu và tặng thêm khối lượng (nạp lại sau mỗi đỉnh lũ).</summary>
        AncientFont = 1,
        /// <summary>Tinh Thể Cộng Hưởng: hiến 25% máu hiện tại để nhận khối lượng lớn và đẩy lùi quái quanh đó.</summary>
        ResonanceCrystal = 2,
        /// <summary>Hổ Phách Tiền Sử: cần đủ khối lượng (>= 11) để phá vỡ và nhận thưởng lớn.</summary>
        AmberShrine = 3,
        /// <summary>Mạch Phun Nước Ngầm: phun theo nhịp giúp phóng slime lên gờ cao tránh lũ hoặc vượt địa hình.</summary>
        UndergroundGeyser = 4,
        /// <summary>Túi Bào Tử Nổ: bẫy sinh học phát nổ khi slime lại gần, gây sát thương và hất văng.</summary>
        SporeGeyserTrap = 5,
        /// <summary>Khe Hơi Nóng: bẫy nhiệt tầng sâu phun theo chu kỳ ở vùng trũng.</summary>
        ThermalVentTrap = 6
    }

    /// <summary>Dữ liệu thuần một khoang trong đồ thị mê cung.</summary>
    public sealed class ChamberNode
    {
        public int Id { get; }
        public string Name { get; }
        public ChamberDepth Depth { get; }
        public ChamberKind Kind { get; }
        public ChamberTheme Theme { get; }
        public ChamberEncounterKind Encounter { get; }
        public bool HasHighLedge { get; }
        public int DangerTier => (int)Depth + 1;

        public ChamberNode(
            int id,
            string name,
            ChamberDepth depth,
            ChamberKind kind,
            bool hasHighLedge = true,
            ChamberTheme theme = ChamberTheme.LumaMushroomGrove,
            ChamberEncounterKind encounter = ChamberEncounterKind.None)
        {
            Id = id;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Depth = depth;
            Kind = kind;
            HasHighLedge = hasHighLedge;
            Theme = theme;
            Encounter = encounter;
        }
    }

    /// <summary>Đường hầm hai chiều nối hai khoang trong mê cung.</summary>
    public sealed class TunnelEdge
    {
        public int Id { get; }
        public int FromId { get; }
        public int ToId { get; }
        public TunnelSize Size { get; }
        public TunnelSlope SlopeFromTo { get; }
        public TunnelSlope SlopeToFrom { get; }
        public ObstacleKind Obstacle { get; }

        public TunnelEdge(
            int id,
            int fromId,
            int toId,
            TunnelSize size,
            TunnelSlope slopeFromTo,
            TunnelSlope slopeToFrom,
            ObstacleKind obstacle = ObstacleKind.None)
        {
            Id = id;
            FromId = fromId;
            ToId = toId;
            Size = size;
            SlopeFromTo = slopeFromTo;
            SlopeToFrom = slopeToFrom;
            Obstacle = obstacle;
        }

        public int OtherEnd(int chamberId)
        {
            if (chamberId == FromId) return ToId;
            if (chamberId == ToId) return FromId;
            throw new ArgumentOutOfRangeException(nameof(chamberId), $"Khoang {chamberId} không thuộc hầm {Id}.");
        }

        public TunnelSlope SlopeFrom(int chamberId)
        {
            if (chamberId == FromId) return SlopeFromTo;
            if (chamberId == ToId) return SlopeToFrom;
            throw new ArgumentOutOfRangeException(nameof(chamberId), $"Khoang {chamberId} không thuộc hầm {Id}.");
        }
    }
}
