using System.Collections.Generic;

namespace Evolve.Traits
{
    /// <summary>The words of the birth scene: Tiếng Vọng's lines, the six Ân Điển Khởi Sinh and the summary. All Vietnamese (spec 2026-11-19-birth-scene-design).</summary>
    public static class BirthScript
    {
        public readonly struct Gift
        {
            public readonly string Id, Name, Line;
            /// <summary>False while the ability is not built yet (the scene says so on its card).</summary>
            public readonly bool Implemented;
            public Gift(string id, string name, string line, bool implemented) { Id = id; Name = name; Line = line; Implemented = implemented; }
        }

        /// <summary>The six gifts every slime is born with (spec 3), in the order they are granted.</summary>
        public static IReadOnlyList<Gift> Gifts { get; } = new List<Gift>
        {
            new Gift("gift.tui", "Túi Không Đáy", "Chứa những gì bạn nuốt.", true),
            new Gift("gift.nuot", "Nuốt Chửng", "Nuốt thứ nhỏ hơn mình.", true),
            new Gift("gift.giaima", "Giải Mã", "Học từ những gì đã nuốt.", true),
            new Gift("gift.bienhinh", "Biến Hình", "Nặn kỹ năng từ chính thân mình.", true),
            new Gift("gift.songdo", "Sóng Dò", "Nghe thế giới bằng sóng âm.", true),
            new Gift("gift.phanthan", "Phân Thân", "Tách một mảnh nhỏ, nuốt lại được.", false),
        };

        /// <summary>Tiếng Vọng's lines (id, text); the id is also the name of an optional voice clip in Resources/Voice/Echo.</summary>
        public static IReadOnlyList<(string id, string text)> EchoLines { get; } = new List<(string, string)>
        {
            ("echo.born", "Xác nhận… Cá thể mới hình thành."),
            ("echo.gift", "Ban tặng Ân Điển Khởi Sinh."),
            ("echo.core", "Phát hiện Lõi độc nhất."),
            ("echo.flaw", "Phát hiện khiếm khuyết."),
            ("echo.done", "Số mệnh đã định. Hãy sống sót."),
        };

        public static string SummaryLine(string coreName, IReadOnlyList<string> flawNames)
        {
            string flaws = flawNames != null && flawNames.Count > 0 ? string.Join(" + ", flawNames) : "không có";
            return $"Lõi độc nhất: {coreName}. Khiếm khuyết: {flaws}.";
        }
    }
}
