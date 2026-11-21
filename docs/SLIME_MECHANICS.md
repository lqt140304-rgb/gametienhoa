# Cơ chế và kỹ năng của slime (bản mới nhất)

Tài liệu cho **mọi phiên làm việc** (lõi, bên làm hang, bên làm quái) biết slime hiện có gì và nằm ở đâu trong code. Cập nhật 2026-11-21, nhánh `claude/unity-test-maze`. Chi tiết từng mục: `docs/PROJECT_STATUS.md` (số mục ghi bên dưới), danh sách thử tay: `docs/TEST_CHECKLIST_UNITY.md`.

> **Lưu ý trung thực:** phần luật thuần có test chạy ngoài Unity (`tools/cstest/run.sh`, 386 test xanh). **Phần Unity (cảnh, hiệu ứng, cảm giác tay) chủ dự án đang thử dần; mọi mục "chưa kiểm" nghĩa là chưa ai mở trong Unity.**

## 1. Phím và hành động của slime

| Phím | Làm gì | Mục |
|---|---|---|
| Chuột trái | **Vồ** quái gần trước mặt (tầm khoảng 4 m) | 54 |
| A/D hoặc kéo chuột | Giữ chấm trong vùng xanh của mini game **Ghì Chặt** | 54 |
| Space | **Siết** khi quái gồng đỏ (báo ~0,5 s); khi không có cảnh báo thì **buông** nhảy xuống | 54 |
| F (giữ F nếu xác to) | Nuốt / hòa tan xác kẻ săn mồi | 36 |
| B | Túi Không Đáy (6 chỗ) | 36 |
| G | Biến Hình (nặn kỹ năng bị động, chỉ trong hang trú) | 36, 37 |
| K | Bảng kỹ năng đã học | 37 |
| 1–5 | Kỹ năng chủ động đã gắn phím | 37, 41 |
| F1 | Menu thần (6 tab, có A−/A+ chỉnh cỡ) | 58 |
| F3 | Màu thân (Cầu Vồng khi đã mở) | — |
| F10 | Bảng thần của bên làm hang (mê cung) | — |

Chuột trái **bị chặn** khi menu thần, HUD hoặc bảng nào đang mở, khi bị đóng băng, khi đang bám (có chỉ dẫn và log `[Vồ] ...`).

## 2. Combat của slime: Vồ – Ghì Chặt – Nuốt (mục 54)

Thiết kế: `docs/superpowers/specs/2026-11-20-slime-combat-latch-design.md`. Luật thuần ở `Scripts/Combat/Pure/` (`LatchGame`, `PounceRules`, `StruggleSeed`), Unity ở `Scripts/Combat/` (`Pounce`, `LatchSession`, `LatchHud`, `SwallowCinematic`).

- **Vồ** (có từ đầu): con **nhỏ hơn** slime (≤ 0,8 lần khối) thì **nuốt sống** ngay; **to hơn** thì **bám lên và vào Ghì Chặt**; **quá to** (> 4 lần khối) thì bật ra ("Quá to để vồ"). Quái đang trong chiêu có giáp thì vồ bật ra; quái **hở sau chiêu** (kiệt sức) thì vồ trúng.
- **Ghì Chặt** (kiểu câu cá): kéo chấm giữ trong **vùng xanh**; trong vùng quái mất máu đều (~8/giây, ×2 trên **khúc vàng** điểm yếu) và slime hút lại một ít dinh dưỡng. Ngoài vùng quá 0,25 giây thì **một lỗi**; đủ lỗi (con mồi 5, kẻ săn 4, Chúa Hang 3) thì quái hất slime và **chạy đi, nhớ slime** (lần sau cảnh giác hơn). Quái khỏe hơn thì vùng hẹp hơn, giãy nhanh hơn. Máu quái < 30% thì giãy điên cuồng. Hết năng lượng thì vùng xanh hẹp dần.
- **Cú giãy mạnh** (kẻ săn, Chúa Hang): chữ đỏ báo ~0,5 giây; Space đúng lúc thì siết, vùng xanh rộng ra vài giây; trật là một lỗi.
- **Quái chết khi đang bám:** **nuốt tại chỗ** kèm cảnh chậm và camera áp sát ~2 giây (bỏ qua bằng Esc hoặc chuột trái); xác mới nhất, Giải Mã ra gene tốt nhất.
- **Chưa làm:** Vồ Xa của Chồn Khe (đã có chiêu quái tên Vồ Xa trong bộ chiêu quái), kỹ năng hang tác động lên mini game, co-op (client chưa thấy), tiếng riêng, hoạt hình quái giãy.

## 3. Nuốt, Túi, Giải Mã, Biến Hình (mục 36)

- **Nuốt Chửng** (`Swallow/Swallower`): đâm vào con mồi ≤ 80% khối slime thì vào bụng; thú săn chết để lại **xác có bảng di truyền** (F để nuốt).
- **Túi** (`Stomach`, B): mỗi món Tiêu hóa (8 s, cho no, +30% khối món) / Phân tích (20 s trong hang trú, chậm ×4 ngoài) / Giữ / Nhả.
- **Giải Mã** (`AnalysisRules`): mỗi mẫu một lần may (Thường 25%, Hiếm 8%, Quý 2%, Huyền thoại 10% chỉ con đầu đàn) cộng bước tích lũy; chất lượng mẫu ×1 tươi, ×0,5 cũ, ×0,2 thối; 3 mẫu lên ★2, 9 mẫu lên ★3. Sổ giữ nguyên khi chết.
- **Khối** (`SlimeBody`): máu tối đa = khối × 10, kích thước theo khối (khối 10 = cỡ gốc). Biến Hình tốn khối (2/4/7/12 theo độ hiếm), chỉ trong hang trú.

## 4. Kỹ năng (mục 37, 41, 42, 50)

- Slime **trơn**, không gắn cơ quan lên người; gene học được là **kỹ năng**.
- **Bị động:** luôn bật, **tối đa 5 ô** (`SkillRules.MaxPassives`), chỉ nặn/tháo ở hang trú, không hiện trên người. Ví dụ: Da Dày (+10 máu tối đa), Vỏ Cứng (−15% sát thương), **Sóng Dò Mạnh** (vòng quét của `VibrationSense` ×1,5), Mắt, Màng chịu nhiệt.
- **Chủ động:** gắn phím 1–5 ở đâu cũng được; học xong dùng được ở ★1, ★3 là bản nâng. Năng lượng 25/30/35 theo độ mạnh (thanh 100 đủ 3 kỹ năng). Bộ phận chỉ **mọc ra lúc dùng** rồi thu vào (`SkillVisuals`).
- **Kỹ năng gốc:** Phóng Gai, Lao Tới, Chạy Nước Rút (4/5/6 giây theo sao, hồi 10 giây).
- **12 kỹ năng hang đợt 1** (`Swallow/Pure/CaveSkillSeed.cs`, tạo bằng menu `Evolve → Create Cave Skills`): Da Dày, Vỏ Cứng, Sóng Dò Mạnh, Thu Mình, Khiên Quặng, Phát Sáng, Bám Vách, Lách Né, Lưỡi Dài, Ợ Chấn, Cắm Gai, Châm Choáng (5 khuôn: `AreaAbility`, `LeapAbility`, `PullAbility`, `TrapAbility`, `BodyStateAbility`). Châm Choáng chỉ choáng, không sát thương.
- **Né (dash)** là kỹ năng học từ quái, không có sẵn.
- **Kỹ năng có điều kiện** (Đạn Nước, Tan Chảy): đồng ý ý tưởng, **chưa chốt cách mở khóa; hỏi chủ dự án trước khi làm**.
- Co-op: kỹ năng của client chưa tác động lên sinh vật ở máy chủ.

## 5. Di chuyển của slime (mục 49)

Bò qua đá/bậc cao tới **40% bề ngang thân** (`SlimeStep`, `SlimeController.StepUp`); vách cao hơn thì không leo. Slime là vật thể động nên tự va chạm với sinh vật bằng vật lý.

## 6. Công cụ thử

- **Menu thần F1** (`Dev/GodMenu*.cs`): Bản thân (bất tử, hồi, mạng, khối, tốc độ game), Kỹ năng (học kỹ năng, phần thưởng kỳ ngộ), Sinh vật (hành vi, cỡ/độ khó, tạo quái thử bằng **mô hình thật**), Combat (Vồ, pha chiêu quái), Thế giới, Khác.
- **Khu thử sạch** (menu `Evolve → Build Test Arena`): sàn sáng, vật cản, bục, dốc để thử Vồ/né.
- Dựng lại: `Evolve → Build Maze World (Step 3)`, `Evolve → Attach Creature Brains`, `Evolve → Create Cave Skills`.

## 7. Giao diện với bên làm quái

Mỗi loài có `StruggleProfile` (độ rộng vùng, số lỗi, nhịp giãy) trong `StruggleSeed`; quái biết mình **đang bị bám** (`Prey.Latched`, `Predator.Latched`) để chọn chiêu hất. Bộ chiêu quái dùng chung (`MoveDef`, `MoveScheduler`, `MovesetSeed`, mục 55–57) chia pha Windup → Active → Recover; **lúc Recover đánh trúng đau ×1,5**. Chi tiết: `docs/superpowers/specs/2026-11-20-monster-combat-movesets-design.md`.

## 8. Quy ước cho các phiên

- Nhánh người chơi thử: `claude/unity-test-maze` (bên làm hang cũng đẩy vào đây: luôn `git pull --no-rebase` rồi gộp trước khi đẩy). Nhánh làm di chuyển/combat: `claude/movement`.
- **Không `git add .` khi đang xung đột.** Không đưa khóa API vào file (khóa ElevenLabs/Meshy nằm ở mạng của môi trường).
- Chữ hiển thị trong game và tài liệu bằng tiếng Việt; mã và chú thích mã bằng tiếng Anh. Luật thuần để trong thư mục `Pure/` có test.
