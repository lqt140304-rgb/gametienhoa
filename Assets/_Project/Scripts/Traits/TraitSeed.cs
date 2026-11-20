using System.Collections.Generic;
using Evolve.Organs;

namespace Evolve.Traits
{
    public sealed class FlawDef
    {
        public string Id, Name, Description;
        public OrganSlot[] CompensatedBy;
        public bool Implemented;
    }

    public sealed class CoreDef
    {
        public string Id, Name, Summary, Detail;
        /// <summary>Exactly three options; each is one flaw id (two for a heavy core).</summary>
        public string[][] Options;
        public bool Heavy;
        public bool Implemented;
        public TraitRarity Rarity;
    }

    /// <summary>
    /// The 12 cores and 12 flaws of the design (spec sections 8 and 9) as plain data. The editor menu Evolve → Create Traits turns
    /// them into assets; tests check the rules on them. All numbers in the texts are provisional.
    /// </summary>
    public static class TraitSeed
    {
        static readonly OrganSlot[] None = new OrganSlot[0];

        // declared before Flaws and Cores: static fields start in textual order and C() reads this table
        static readonly System.Collections.Generic.Dictionary<string, TraitRarity> RarityOf = new System.Collections.Generic.Dictionary<string, TraitRarity>
        {
            { "core.mang_tham_thau", TraitRarity.Common }, { "core.an_mon", TraitRarity.Common }, { "core.quang_hop", TraitRarity.Common },
            { "core.cong_sinh", TraitRarity.Common }, { "core.thich_nghi", TraitRarity.Common }, { "core.bam_dinh", TraitRarity.Common },
            { "core.than_toc", TraitRarity.Rare }, { "core.nguy_sac", TraitRarity.Rare }, { "core.phan_chan", TraitRarity.Rare }, { "core.quen_dat", TraitRarity.Rare },
            { "core.con_bac", TraitRarity.Mythic }, { "core.giu_goc", TraitRarity.Mythic },
        };

        public static IReadOnlyList<FlawDef> Flaws { get; } = new List<FlawDef>
        {
            F("flaw.run_ray", "Run Rẩy", "Bị đánh trúng thì khựng 0,7 giây, không nảy được.", new[] { OrganSlot.Legs }, true),
            F("flaw.vet_mui", "Vệt Mùi", "Đi qua đâu để lại vệt mùi 20 giây; kẻ địch trong khoảng 15 m có thể theo vệt.", None, true),
            F("flaw.mo_trong_toi", "Mờ Trong Tối", "Ở nơi tối thì tầm nhìn hẹp lại 15%.", new[] { OrganSlot.Eyes }, false),
            F("flaw.doi_som", "Đói Sớm", "Độ no tụt nhanh hơn 10%, báo đói sớm hơn; không mất thêm máu.", None, true),
            F("flaw.bam_bun", "Bám Bùn", "Ra khỏi nước nông (hoặc bùn) vẫn bị chậm thêm 2 giây.", new[] { OrganSlot.Legs }, true),
            F("flaw.luong_cuong", "Luống Cuống", "Máu dưới 25% thì xoay hướng chậm hơn rõ rệt.", new[] { OrganSlot.Eyes }, true),
            F("flaw.ngai_dong", "Ngại Đông", "Có từ ba sinh vật trở lên gần nhau (trong 6 m) thì hồi năng lượng chậm đi 20%.", None, true),
            F("flaw.mau_kho", "Mau Khô", "Ở xa nước hoặc hang trú quá 90 giây thì thân khô, đi chậm hơn 8%; vào nước hoặc hang trú là hết ngay.", new[] { OrganSlot.Skin }, true),
            F("flaw.co_quan_mau_mon", "Cơ Quan Mau Mòn", "Độ nguyên vẹn cơ quan mất nhanh hơn 20% khi trúng đòn.", new[] { OrganSlot.Back, OrganSlot.Skin }, false),
            F("flaw.de_bi_chu_y", "Dễ Bị Chú Ý", "Kẻ địch trong tầm 20 m bị thu hút về phía bạn.", new[] { OrganSlot.Skin }, true),
            F("flaw.la_vung", "Lạ Vùng", "Vào vùng mới thì 15 giây đầu tầm nhìn hẹp 15%.", new[] { OrganSlot.Eyes }, false),
            F("flaw.da_day_ken", "Dạ Dày Kén", "Ăn thức ăn lạ lần đầu thì hấp thụ chậm hơn 30% trong 20 giây.", None, true),
        };

        public static IReadOnlyList<CoreDef> Cores { get; } = new List<CoreDef>
        {
            C("core.mang_tham_thau", "Màng Thẩm Thấu", "Đứng yên trong nước nông hoặc hang trú thì hút dưỡng chất.",
              "Đứng yên (dưới 0,3 m/s) trong nước nông hoặc trong hang trú có rêu ẩm thì hút dưỡng chất dần (tạm 1,5% độ no mỗi giây), ngừng hao độ no. Tối đa lấp đầy 60% thanh no; bị đánh thì ngắt 3 giây; không hoạt động khi bơi nước sâu.",
              S("flaw.run_ray", "flaw.mo_trong_toi", "flaw.co_quan_mau_mon"), false, true),
            C("core.than_toc", "Nhân Thần Tốc", "Nảy đúng nhịp liên tiếp thì chạy nhanh dần.",
              "Mỗi cú nảy chạm đất đúng nhịp (0,3–0,6 giây sau lần chạm trước) cộng một cấp chuỗi; mỗi cấp +4% tốc độ, tối đa 5 cấp (+20%). Sai nhịp hoặc đứng yên quá 1 giây thì mất chuỗi; không hoạt động khi bơi hoặc bám vách.",
              S("flaw.doi_som", "flaw.co_quan_mau_mon", "flaw.da_day_ken"), false, false),
            C("core.nguy_sac", "Nhân Ngụy Sắc", "Đứng yên vài giây thì hòa vào nền.",
              "Đứng yên 2 giây thì thân hòa với nền; kẻ địch phải lại gần hơn mới phát hiện (tạm giảm 60% tầm phát hiện). Di chuyển, tấn công hoặc bị đánh thì mất ngay; kẻ địch đánh hơi vẫn thấy.",
              S("flaw.ngai_dong", "flaw.da_day_ken", "flaw.luong_cuong"), false, true),
            C("core.phan_chan", "Nhân Phản Chấn", "Bị đánh trúng thì đòn kế tiếp mạnh hơn.",
              "Nhận sát thương từ đòn trúng thân thì tích lực (tối đa 3 lần); đòn tấn công kế tiếp mạnh hơn theo điểm tích (tạm +25% ở mức đầy). Mất sau 6 giây không đánh; sát thương môi trường không tích.",
              S("flaw.doi_som", "flaw.la_vung", "flaw.bam_bun"), false, false),
            C("core.cong_sinh", "Nhân Cộng Sinh", "Gần đồng đội thì cùng hồi máu nhanh hơn.",
              "Có đồng đội trong 8 m thì cả hai hồi máu nhanh hơn (tạm +40%), tối đa 3 đồng đội; đồng đội không có Lõi hưởng một nửa. Một mình vẫn hồi nhẹ (+10%).",
              S("flaw.vet_mui", "flaw.mo_trong_toi", "flaw.mau_kho"), false, false),
            C("core.an_mon", "Nhân Ăn Mòn", "Chạm vật hữu cơ thì hút thêm dinh dưỡng.",
              "Chạm hoặc bao phủ vật hữu cơ (xác, thực vật, nấm) thì tiết dịch phân hủy nhanh hơn, hút thêm dưỡng chất (tạm +30%). Chỉ vật hữu cơ; mỗi vật tính một lần.",
              S("flaw.run_ray", "flaw.la_vung", "flaw.de_bi_chu_y"), false, true),
            C("core.con_bac", "Dòng Máu Tham Lam", "Giải Mã dễ ra kỹ năng hiếm hơn.",
              "Cơ hội học dòng gene Hiếm, Quý và Huyền thoại trong Giải Mã tăng gấp rưỡi (×1,5). Gene Thường không đổi; thanh tích lũy vẫn chạy như cũ.",
              new[] { new[] { "flaw.doi_som", "flaw.mo_trong_toi" }, new[] { "flaw.run_ray", "flaw.la_vung" }, new[] { "flaw.co_quan_mau_mon", "flaw.ngai_dong" } }, true, true),
            C("core.thich_nghi", "Nhân Thích Nghi", "Chịu mãi một loại sát thương thì kháng dần.",
              "Chịu liên tục một loại sát thương môi trường (độc, nhiệt, lạnh, axit) thì kháng loại đó tăng dần (tạm +4% mỗi 5 giây), tối đa 40%; hết bị tác động 60 giây thì giảm về 0.",
              S("flaw.run_ray", "flaw.ngai_dong", "flaw.de_bi_chu_y"), false, false),
            C("core.quang_hop", "Nhân Quang Hợp", "Đứng trong tia sáng xuyên rễ thì hồi năng lượng.",
              "Đứng hoặc đi trong tia sáng xuyên rễ thì hồi năng lượng dần (tạm 3 điểm mỗi giây). Không hoạt động dưới nước sâu; không cộng với cơ quan hồi năng lượng (lấy mức cao).",
              S("flaw.bam_bun", "flaw.vet_mui", "flaw.co_quan_mau_mon"), false, true),
            C("core.bam_dinh", "Nhân Bám Dính", "Đứng vững thì không bị hất văng; leo dốc không chậm.",
              "Đứng vững hoặc đi chậm (dưới 40% tốc độ tối đa) thì miễn nhiễm hất văng và đẩy lùi; leo dốc không mất tốc. Chạy hết tốc độ thì mất; không chặn choáng hay trói; đòn hất cực mạnh của boss vẫn đẩy một đoạn ngắn.",
              S("flaw.mo_trong_toi", "flaw.mau_kho", "flaw.da_day_ken"), false, false),
            C("core.quen_dat", "Nhân Quen Đất", "Đi lại một đường nhiều lần thì nhanh hơn trên đó.",
              "Mỗi lần đi qua một đoạn đường (ô khoảng 4 m) thì tăng điểm quen của đoạn đó; điểm cao thì tốc độ trên đoạn đó tăng tối đa +15%. Điểm giảm khi bỏ đường nhiều phút; không cộng khi đứng yên; không tác dụng khi bơi.",
              S("flaw.run_ray", "flaw.doi_som", "flaw.luong_cuong"), false, true),
            C("core.giu_goc", "Dạ Dày Bất Diệt", "Chết lần 1 hoặc 2 vẫn giữ món trong Túi Không Đáy.",
              "Chết ở lần thứ 1 hoặc 2 (còn mạng) thì các món trong Túi Không Đáy không bị mất, vẫn nằm trong bụng sau hồi sinh. Không áp dụng lần chết cuối khi hết mạng; cơ quan bị động và những gì đã học vẫn được giữ như mọi slime.",
              new[] { new[] { "flaw.doi_som", "flaw.la_vung" }, new[] { "flaw.mo_trong_toi", "flaw.ngai_dong" }, new[] { "flaw.da_day_ken", "flaw.luong_cuong" } }, true, true),
        };

        static FlawDef F(string id, string name, string desc, OrganSlot[] slots, bool implemented) =>
            new FlawDef { Id = id, Name = name, Description = desc, CompensatedBy = slots, Implemented = implemented };

        static string[][] S(string a, string b, string c) => new[] { new[] { a }, new[] { b }, new[] { c } };

        static CoreDef C(string id, string name, string summary, string detail, string[][] options, bool heavy, bool implemented) =>
            new CoreDef { Id = id, Name = name, Summary = summary, Detail = detail, Options = options, Heavy = heavy, Implemented = implemented,
                          Rarity = RarityOf.TryGetValue(id, out var r) ? r : TraitRarity.Common };

        /// <summary>The pool fate draws from when no core assets exist: every core with its rarity and flaw options.</summary>
        public static IReadOnlyList<FateCard> FateCards()
        {
            var cards = new List<FateCard>();
            foreach (var c in Cores) cards.Add(new FateCard(c.Id, c.Rarity, c.Options));
            return cards;
        }
    }
}
