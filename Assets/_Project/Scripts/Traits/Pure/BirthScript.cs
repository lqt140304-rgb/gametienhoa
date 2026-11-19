using System.Collections.Generic;

namespace Evolve.Traits
{
    /// <summary>
    /// The words and order of the birth scene (spec 2026-11-19-birth-scene-design 8). All Vietnamese. Every spoken line has an id that is also the name
    /// of its voice clip in Resources/Voice/Echo; the names of the cores and flaws use their own ids (core.quen_dat, flaw.doi_som).
    /// </summary>
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

        /// <summary>The steps of the scene, in order (a fresh list each time: the scene fills in the reveal steps' voices).</summary>
        public static List<BirthStep> BuildSteps()
        {
            var steps = new List<BirthStep>
            {
                new BirthStep { Kind = BirthStepKind.Boot, Text = "Hệ thống đang khởi động…", MinSeconds = 4.5f },
                Line("echo.born", "Xác nhận. Phát hiện một linh hồn vừa chuyển sinh vào thế giới này.", 4.5f),
                Line("echo.body", "Đang dựng cá thể. Chủng tộc: Slime. Hình thể ban đầu là một khối nhầy nhỏ, chưa có mắt, chưa có chân.", 6f),
                Line("echo.gift", "Ban tặng Ân Điển Khởi Sinh. Đây là sáu năng lực nền tảng mà mọi cá thể đều mang theo.", 5f),
            };
            for (int i = 0; i < Gifts.Count; i++)
            {
                var g = Gifts[i];
                steps.Add(new BirthStep
                {
                    Kind = BirthStepKind.Gift, GiftIndex = i, VoiceId = g.Id, MinSeconds = 3.5f,
                    Text = g.Name + ": " + g.Line, VoiceText = g.Name + ". " + g.Line
                });
            }
            steps.Add(Line("echo.fate", "Tiếp theo, số mệnh sẽ ban cho bạn hai thứ: một Lõi độc nhất, và một khiếm khuyết.", 5.5f));
            steps.Add(new BirthStep { Kind = BirthStepKind.DealCore, VoiceId = "echo.core", MinSeconds = 4f,
                Text = "Lõi là quy luật riêng của cơ thể bạn, luôn chạy ngầm. Hãy chọn một lá bài." });
            steps.Add(new BirthStep { Kind = BirthStepKind.PickCore, Text = "Chọn một lá bài." });
            steps.Add(new BirthStep { Kind = BirthStepKind.RevealCore, MinSeconds = 6f });
            steps.Add(new BirthStep { Kind = BirthStepKind.DealFlaw, VoiceId = "echo.flaw", MinSeconds = 4f,
                Text = "Mỗi Lõi đều có cái giá của nó. Hãy chọn một lá bài khiếm khuyết." });
            steps.Add(new BirthStep { Kind = BirthStepKind.PickFlaw, Text = "Chọn một lá bài." });
            steps.Add(new BirthStep { Kind = BirthStepKind.RevealFlaw, MinSeconds = 6f });
            steps.Add(new BirthStep { Kind = BirthStepKind.Summary, VoiceId = "echo.done", MinSeconds = 6f, Text = "Số mệnh đã định. Hãy sống sót." });
            return steps;
        }

        static BirthStep Line(string voiceId, string text, float seconds) =>
            new BirthStep { Kind = BirthStepKind.Line, VoiceId = voiceId, Text = text, MinSeconds = seconds };

        /// <summary>Every line that is read aloud: the steps' lines, then one line per core name and per flaw name. The voice list is made from this.</summary>
        public static List<(string id, string text)> AllVoiceLines(IReadOnlyList<CoreDef> cores, IReadOnlyList<FlawDef> flaws)
        {
            var lines = new List<(string, string)>();
            foreach (var step in BuildSteps())
                if (!string.IsNullOrEmpty(step.VoiceId))
                    lines.Add((step.VoiceId, string.IsNullOrEmpty(step.VoiceText) ? step.Text : step.VoiceText));
            if (cores != null) foreach (var c in cores) lines.Add((c.Id, $"Phát hiện Lõi độc nhất: {c.Name}."));
            if (flaws != null) foreach (var f in flaws) lines.Add((f.Id, $"Phát hiện khiếm khuyết: {f.Name}."));
            return lines;
        }

        public static string SummaryLine(string coreName, IReadOnlyList<string> flawNames)
        {
            string flaws = flawNames != null && flawNames.Count > 0 ? string.Join(" + ", flawNames) : "không có";
            return $"Lõi độc nhất: {coreName}. Khiếm khuyết: {flaws}.";
        }
    }
}
