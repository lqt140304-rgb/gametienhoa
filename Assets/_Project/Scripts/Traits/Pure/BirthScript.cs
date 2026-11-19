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
            new Gift("gift.tui", "Túi Không Đáy", "Cho phép chứa mọi thứ bạn nuốt.", true),
            new Gift("gift.nuot", "Nuốt Chửng", "Nuốt trọn những gì nhỏ hơn bạn.", true),
            new Gift("gift.giaima", "Giải Mã", "Học hỏi từ những gì bạn đã nuốt.", true),
            new Gift("gift.bienhinh", "Biến Hình", "Nặn kỹ năng từ chính cơ thể bạn.", true),
            new Gift("gift.songdo", "Sóng Dò", "Cảm nhận thế giới bằng sóng âm.", true),
            new Gift("gift.phanthan", "Phân Thân", "Tách một mảnh khỏi cơ thể, và nuốt lại khi cần.", false),
        };

        /// <summary>The steps of the scene, in order (a fresh list each time: the scene fills in the reveal steps' voices and the slam step's length).</summary>
        public static List<BirthStep> BuildSteps()
        {
            var steps = new List<BirthStep>
            {
                new BirthStep { Kind = BirthStepKind.Disconnect, VoiceId = "echo.warn", MinSeconds = 6f,
                    Text = "CẢNH BÁO: ĐƯỜNG TRUYỀN KHÔNG ỔN ĐỊNH", VoiceText = "Cảnh báo. Đường truyền không ổn định." },
                new BirthStep { Kind = BirthStepKind.Pull, VoiceId = "echo.pull", MinSeconds = 5f, Text = "Đang kéo linh hồn vào thế giới mới…", VoiceText = "Đang kéo linh hồn vào thế giới mới." },
                Of(BirthStepKind.Form, "echo.born", "Xác nhận. Một linh hồn vừa chuyển sinh vào thế giới này.", 4.5f),
                Of(BirthStepKind.Form, "echo.body", "Đang dựng cá thể. Chủng tộc: Slime. Hình thể: một khối nhầy nhỏ. Chưa có mắt. Chưa có chân.", 6f),
                Of(BirthStepKind.GiftIntro, "echo.gift", "Ban tặng Ân Điển Khởi Sinh. Sáu năng lực nền tảng, mà mọi cá thể đều mang theo.", 5f),
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
            steps.Add(Of(BirthStepKind.Fate, "echo.fate", "Tiếp theo, số mệnh sẽ định đoạt hai điều. Lõi của bạn. Và cái giá phải trả.", 5.5f));
            steps.Add(Of(BirthStepKind.DealCore, "echo.core", "Lõi là quy luật riêng của cơ thể bạn, luôn âm thầm vận hành. Số mệnh đang chọn cho bạn.", 6f));
            steps.Add(new BirthStep { Kind = BirthStepKind.RevealCore, MinSeconds = 6f });
            steps.Add(Of(BirthStepKind.DealFlaw, "echo.flaw", "Mỗi sức mạnh đều có cái giá. Số mệnh đang chọn cái giá của bạn.", 5f));
            steps.Add(new BirthStep { Kind = BirthStepKind.RevealFlaw, MinSeconds = 6f });
            steps.Add(new BirthStep { Kind = BirthStepKind.Slam, MinSeconds = 3f });
            steps.Add(Of(BirthStepKind.Stabilize, "echo.stable", "Kết nối ổn định. Hãy sống sót.", 5f));
            return steps;
        }

        static BirthStep Of(BirthStepKind kind, string voiceId, string text, float seconds) =>
            new BirthStep { Kind = kind, VoiceId = voiceId, Text = text, MinSeconds = seconds };

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
