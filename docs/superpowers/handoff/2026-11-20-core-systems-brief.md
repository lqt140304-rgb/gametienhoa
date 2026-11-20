# Giao việc: phần lõi game (kỹ năng, hệ sinh thái, kỳ ngộ, Lõi/Khuyết Điểm)

Dành cho **bất kỳ phiên nào** nhận làm tiếp phần của phiên chính (Claude), phòng khi phiên chính hết lượt giữa chừng. Mỗi mục A–E làm độc lập được; nhiều phiên có thể làm song song nếu giữ đúng **thư mục được sửa** của mục mình.

## Bối cảnh bắt buộc đọc
- Game Unity 6.6 URP, co-op slime "PROJECT EVOLVE". Chủ dự án nói tiếng Việt: **mọi chữ trong game, tài liệu và tin nhắn báo cáo bằng tiếng Việt.** Code và comment trong code bằng tiếng Anh.
- Nội dung đã duyệt: `docs/superpowers/specs/2026-11-20-caves-creatures-ecosystem-design.md` (12 hang, 44 loài, 44 kỹ năng, hệ sinh thái). Thiết kế tổng: `docs/superpowers/specs/2026-11-19-reincarnation-survival-design.md`. Trạng thái từng phần: `docs/PROJECT_STATUS.md` (mục 36–39 là gần nhất).
- Nhánh gốc: `claude/youthful-mendel-yw0yza`. Mỗi phiên tạo nhánh riêng từ đó (ví dụ `git checkout -b skills-wave1 origin/claude/youthful-mendel-yw0yza`), không đẩy thẳng lên nhánh gốc.
- **Phiên đám mây không có Unity.** Logic thuần đặt trong thư mục `Pure/` và test bằng `tools/cstest/run.sh` (.NET 8 + NUnit, có `UnityStubs.cs`; thêm đường dẫn mới vào `tools/cstest/cstest.csproj` nếu cần). Code Unity (MonoBehaviour, ScriptableObject, UGUI) chỉ kiểm được bằng đọc kỹ; **không nói "xong" khi chưa được chủ dự án mở Unity kiểm.** Báo cho chủ dự án: kéo nhánh, chạy menu nào, bấm Play, gửi ảnh Console.
- Hiện có 137 test thuần xanh. Trong Unity có Test Runner EditMode (khoảng 460 test).
- Âm thanh và giọng: ElevenLabs qua proxy (khóa do môi trường gắn, không ghi vào file). Mẫu script: `tools/voice/generate_slime_sfx.sh`, `generate_birth_sfx.sh`, `generate_echo_voice.sh`. Tiếng mới nên **nhẹ, sạch, không nhớt** (chủ dự án không thích tiếng nhầy ướt, chói).
- Bên làm hang (Antigravity) có bản giao việc riêng: `2026-11-20-cave-encounters-brief.md`. **Không sửa** `Scripts/World/`, `Editor/Cavern*.cs`, `Editor/Lake*.cs`, `Scripts/Maze/` của họ.

## A. Kỹ năng (44 kỹ năng, làm theo hang)
**Thư mục được sửa:** `Scripts/Abilities/`, `Scripts/Organs/`, `Scripts/Swallow/` (kể cả `Pure/`), `Editor/` (menu tạo dữ liệu mới, tên file không bắt đầu bằng `Cavern`/`Lake`), test tương ứng.

Cách game đang chạy: giết/bắt sinh vật → nuốt → trong Túi, Giải Mã đọc **gene** → học kỹ năng. Gene là một `OrganSO` (dữ liệu, không còn là cơ quan đeo): có `GrantedAbility` (một `AbilitySO`) thì là kỹ năng chủ động (phím 1–5, `Abilities/SkillBar`); có số bị động (`GrantsSight`, `HeatResistance`, `SpeedMultiplier`, `ContactDamage`) thì là bị động (tối đa 3, đổi trong hang trú). Phân loại ở `Swallow/Pure/SkillRules.UseOf`. Sao ★1–3 nâng cấp kỹ năng (`AbilitySO.Power`, `CooldownFor`). Phần thân mọc ra khi dùng kỹ năng chủ động: `Abilities/SkillVisuals`. Bảng rơi gene của loài: `DropTableSO`, đặt ở ô `drops` của `Prey`/`Predator`.

Việc làm:
1. **Khuôn dùng chung cho kỹ năng chủ động** (mỗi cái một lớp `AbilitySO` có tham số): Bắn ra (tia/đạn: Lưỡi Dài, Phun Độc, Tiết Axit, Phun Lửa), Lao/nhảy (Lách Né, Nhảy Cao, Vồ, Cuộn Lăn), Vùng quanh mình (Ợ Chấn, Nổ Bào Tử, Đập Đất, Gầm, Hút Năng Lượng, Mây Bào Tử, Hơi Nước, Phát Sáng), Đặt vật (Tiết Nhớt, Cắm Gai, Ảo Ảnh), Đổi trạng thái thân (Thu Mình, Khiên Quặng, Hóa Đá, Ẩn Thân, Thu Nhỏ, Bám Vách, Tái Sinh, Lột Xác, Châm Choáng). Đã có sẵn: `BurstDashAbility`, `ChargeAbility`, `SpikeBurstAbility`, `SprintAbility`: dùng lại khi hợp.
2. **Bị động mới** cần trường số mới trên `OrganSO` và chỗ áp: máu tối đa (Da Dày), giảm sát thương (Vỏ Cứng), hút máu (Hút Máu), làm chậm khi đánh trúng (Nọc Tê), rơi không mất máu (Rơi Nhẹ), nuốt nhanh (Nuốt Nhanh), Sóng Dò xa hơn (Sóng Dò Mạnh), kháng độc/bệnh (Kháng Độc), bỏng khi bị chạm (Thân Nóng), báo kẻ săn (Nghe Xa), ăn thối không bệnh (Ăn Tạp), thấy xác xa (Đánh Hơi Xác), không phát rung (Bước Êm), cảm rung (Cảm Rung). Cập nhật `SkillRules.UseOf` để các trường mới là Bị động.
3. **Họ kỹ năng** (Linh Hoạt, Đá, Độc, Âm Ảnh, Sinh Lực, Nhiệt): thêm trường trên `OrganSO`, chưa dùng ở chương 1 (để dung hợp sau).
4. **Menu tạo dữ liệu** (ví dụ `Evolve → Create Cave Skills`): tạo `OrganSO` + `AbilitySO` + `DropTableSO` cho từng loài từ một bảng dữ liệu thuần (giống cách `Traits/TraitSeed.cs` + `Evolve → Create Traits` đang làm). Chạy lại nhiều lần không hỏng.
5. **Thứ tự:** trước hết 12 kỹ năng của 3 hang đã có (Hang Khởi Sinh, Vườn Quặng Cầu Vồng, Vực Nứt Tím), rồi tới hang nào bên làm hang xong trước.
6. Mỗi kỹ năng một tiếng hiệu ứng ngắn (ElevenLabs, nhẹ, sạch).

Kiểm: luật thuần (sát thương, thời gian, hồi chiêu, khoảng cách) có test; chủ dự án dùng menu thần (`Dev/GodMenu`) để cho mình gene và thử từng kỹ năng.

## B. Hệ sinh thái
**Thư mục được sửa:** `Scripts/Ecology/`, `Scripts/Creatures/`, test tương ứng.

Đang có: mô hình quần thể theo vùng (`Ecology/RegionPopulation`, `SpeciesPopulation`, `SpeciesSO`, `Den`, `PopulationCap`, `EmbodiedCreature`): thức ăn vùng → loài ăn cỏ → kẻ săn, sinh từ tổ, chết đói, xác thành mùn trả thức ăn cho vùng. Hạn chế: mỗi loài chỉ có **một** loài mồi (`PreyId`); sinh vật trong cảnh chỉ săn người chơi. Mục `Ecology` trong `PROJECT_STATUS.md` ghi "0.5a, chưa biên dịch": kiểm lại trạng thái trước khi xây thêm.

Việc làm (theo spec mục 5):
1. **Nhiều mồi:** `SpeciesSO`/`SpeciesPopulation` ăn được danh sách loài (có trọng số) và/hoặc nguồn sống của hang (rêu, nấm, quặng, tảo, địa y, xác).
2. **Cân bằng:** luật thuần có test chạy mô phỏng dài (ví dụ 2 giờ chơi): không loài nào vượt sức chứa vô hạn, không loài nào về 0 vĩnh viễn (tổ luôn sinh lại chậm), Chúa Hang sinh lại sau 8–10 phút.
3. **Di cư** giữa các hang kề khi đói hoặc quá đông; các loài đi xa theo spec 5.3 (Dơi, Chim Hang Mẹ, Cáo Gió, Kền Kền). Cần biết hang nào kề hang nào: lấy từ đồ thị mê cung của bên làm hang (hỏi qua `docs/superpowers/handoff/maze-questions.md` nếu chưa có giao diện).
4. **Hành vi trong cảnh:** kẻ săn đuổi bắt cả con mồi khác loài (không chỉ người chơi); loài ăn xác tụ quanh `Carcass`; con mồi chạy khi thấy kẻ săn; hai kẻ săn gặp nhau thì đánh hoặc tránh; Chó Xương canh xác lớn.
5. **Slime leo chuỗi thức ăn:** loài nhỏ hơn slime (so khối, `SlimeBody.Mass`) thì chạy khỏi slime; loài lớn hơn thì săn; kẻ săn né slime khi slime đủ lớn.
6. **Giữ nguyên các móc đã có** trong `Creatures/Predator.cs`: `TraitSense.DetectMultiplier`, `TraitSense.SmellsTrail`, `TraitSense.Lure` (Lõi/Khuyết Điểm đợt 2). Co-op: sinh vật chạy ở máy chủ (`NetWorld`, `WorldSync`).

## C. Phần thưởng kỳ ngộ
**Thư mục được sửa:** `Scripts/Traits/` (file mới `EncounterRewards.cs`, luật thuần trong `Traits/Pure/`).

Bên làm hang gọi các hàm này khi kỳ ngộ đạt điều kiện (spec mục 3):
`GrantGene(player, minRarity)`, `GrantUniqueGene(player, id)`, `AddMass(player, kg)`, `RerollFlaw(player, seed)`, `AddHeatResistance(player, value)`, `RefillStats(player)`, `CureDiseases(player)`, `BoostCore(player)`, `UnlockColor(player, color)`, `GiveStomachItems(player, items)`, `Say(line)`.
Chỗ có sẵn để dựa vào: `Swallow/Swallower` (Túi, `TrySwallowAlive`), `Swallow/Pure/AnalysisBook` (Giải Mã), `Swallow/SlimeBody.AddMass`, `Traits/PlayerTraits.Set`, `Traits/Pure/FateRoll`, `Visuals/SlimeColor`, `Swallow/Pure/Echo.Say`, `Survival/SurvivalState` (máu, no, năng lượng, bệnh, `HeatResistance`). Co-op: phần thưởng áp trên máy của người nhận (người chơi local); đổi Khuyết Điểm phải báo máy chủ như `TraitNet.ClientPick`. Mỗi hàm có test thuần cho phần quyết định (ví dụ đổi Khuyết Điểm không ra lại đúng cái cũ).

## D. Lõi và Khuyết Điểm còn lại
**Thư mục được sửa:** `Scripts/Traits/`, `Scripts/Survival/` (chỗ móc), test.

Đã có hiệu ứng: 7/12 Lõi, 9/12 Khuyết Điểm (`docs/superpowers/plans/2026-11-20-traits-wave2.md`). Còn lại (mục B của kế hoạch đó):
- **Làm được khi có hang/kỹ năng:** Nhân Thích Nghi (khi có độc ở Hang Nấm, nhiệt ở Suối Nóng), Mờ Trong Tối và Lạ Vùng (khi mê cung có vùng tối/khoang mới), Nhân Bám Dính (đẩy lùi đã có từ cú cắn và sẽ có từ Ợ Chấn, Gầm), Nhân Thần Tốc (nhịp nảy), Nhân Cộng Sinh (co-op).
- **Cần chủ dự án duyệt nội dung mới trước:** Nhân Phản Chấn (đề xuất: bị đánh thì lần dùng kỹ năng tấn công kế tiếp mạnh hơn), Cơ Quan Mau Mòn (đề xuất: kỹ năng hồi chiêu lâu hơn 20%), cách "bù khuyết điểm" thay cho cơ quan (`FlawDef.CompensatedBy`).
- Mỗi cái xong: bật `Implemented = true` trong `Traits/TraitSeed.cs`, sửa `Tests/EditMode/TraitSeedTests.ImplementedSet_MatchesWave2`, luật thuần trong `Traits/Pure/EnvTraitRules.cs` hoặc file mới, ghi vào `PROJECT_STATUS.md`.

## E. Chờ chủ dự án kiểm trong Unity (đã đẩy, chưa biên dịch lần nào)
- Cảnh Ra Đời bản 3 (cột chữ hai bên, ít nhiễu, bìa tạp chí slime, chuyển cảnh mờ): `PROJECT_STATUS.md` mục 38. Lần kiểm cuối chỉ còn lỗi cú pháp đã sửa.
- Tiếng slime (bò, nhảy, chạm đất, nuốt, hòa tan, nhả): `Player/SlimeSfx.cs`.
- Lõi/Khuyết Điểm đợt 2: `PROJECT_STATUS.md` mục 39.
Nếu chủ dự án gửi ảnh lỗi Console, sửa theo dòng lỗi; trong Unity 6.6 `Object.GetInstanceID()` đã cũ (dùng chỉ số hoặc `GetEntityId`).

## Báo cáo
Mỗi phần xong thì báo ngắn bằng tiếng Việt: đã làm gì, test nào chạy, cần chủ dự án làm gì trong Unity, điều gì chưa chắc. Ghi trạng thái mới vào `docs/PROJECT_STATUS.md` (mục mới, có checklist).
