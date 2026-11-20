# Giao việc: Kỳ ngộ của từng hang (đề xuất, chờ chủ dự án duyệt)

Dành cho phiên làm mê cung (Antigravity). Đọc kèm `2026-11-19-maze-step3-brief.md` (ranh giới thư mục vẫn như cũ). **Phần nội dung kỳ ngộ dưới đây là đề xuất của phiên chính; chủ dự án duyệt hoặc sửa trước khi dựng.**

## Ý chủ dự án
Mỗi hang (khoang) có **một cơ chế kỳ ngộ riêng**, không hang nào giống hang nào.

## Luật chung (đề xuất)
- **Mỗi khoang một kỳ ngộ**, gán theo hạt giống của vòng chơi (cùng seed ra cùng cách gán), giữ nguyên qua các lần tái tạo trong vòng. Hang Khởi Sinh và Khoang Phong Ấn có kỳ ngộ cố định.
- **Không đánh dấu trên bản đồ, không nhiệm vụ.** Người chơi tự nhận ra nhờ dấu hiệu trong hang (ánh sáng, âm thanh, hình dạng lạ).
- **Mỗi kỳ ngộ dùng một lần mỗi vòng**, kích hoạt bằng hành động của slime (nuốt, đứng yên, Sóng Dò, chui khe…), không bằng nút "tương tác".
- Khi kích hoạt: **Tiếng Vọng nói một câu** (`Echo.Say`) và có một tiếng hiệu ứng.
- Phần thưởng đi qua cơ chế sẵn có của slime (gene Giải Mã, khối, chỉ số sinh tồn, Lõi/Khuyết Điểm), không có đồ vật kiểu túi đồ RPG.

## 10 kỳ ngộ đề xuất (mỗi cái một kiểu khoang)

| # | Khoang | Dấu hiệu | Cách kích hoạt | Phần thưởng |
|---|---|---|---|---|
| 1 | **Hang Tinh Thể Hát** | Tinh thể ngân nga khi có Sóng Dò | Dùng Sóng Dò, theo tiếng vang tìm tinh thể lõi rồi nuốt | Một gene Hiếm trở lên chắc chắn khi Giải Mã |
| 2 | **Xác Cự Thú** | Một bộ xác khổng lồ, kẻ ăn xác lảng vảng | Giữ F hòa tan suốt ~60 giây, vừa làm vừa bị quấy | Khối lớn + một gene của loài cổ |
| 3 | **Tổ Bỏ Hoang** | Ổ trứng nằm trong khe | Nuốt quả trứng còn sống; trứng nở trong Túi sau vài phút | Gene sinh vật lạ (một dòng không gặp ở nơi khác) |
| 4 | **Vòng Nấm Huỳnh Quang** | Vòng nấm phát sáng theo nhịp | Đứng yên giữa vòng đúng lúc bào tử sáng rực | Đổi một Khuyết Điểm (rút lại ngẫu nhiên, có thể tệ hơn) |
| 5 | **Suối Nước Nóng** | Hơi nóng, nước sủi | Ngâm đủ lâu khi nhiệt lên cao mà không chết | Tăng chịu nhiệt vĩnh viễn trong vòng |
| 6 | **Khe Gió Hú** | Tiếng gió rít từ một khe rất hẹp | Chỉ slime nhỏ lọt (đọc `SlimeBody.Mass`) | Túi của một kẻ chuyển sinh trước: vài món trong bụng + một câu chuyện qua Tiếng Vọng |
| 7 | **Hồ Tảo Sáng** | Mặt hồ có tảo phát sáng | Nổi yên trên hồ vào lúc Chu Kỳ Lũ đang lặng | Một lần hồi đầy năng lượng và no + đêm sau thấy rõ hơn (tầm nhìn rộng tạm thời) |
| 8 | **Hóa Thạch Slime Cổ** | Hình slime đá hóa trên vách | Bò lên và đứng yên trên đó 5 giây | Tiếng Vọng kể chuyện cũ; Lõi được "khắc sâu": tăng nhẹ hiệu ứng Lõi trong vòng |
| 9 | **Hang Tiếng Thở** (gần Khoang Phong Ấn) | Tiếng thở của sinh vật cổ to dần | Ngủ (đứng yên lâu) ngay trong hang mà không bị phát hiện | Gợi ý đường tới Khoang Phong Ấn qua Tiếng Vọng |
| 10 | **Hang Khởi Sinh** (cố định) | Tia nắng qua lỗ trần chiếu xuống hồ | Nổi trong tia nắng lần đầu | Câu mở đầu của Tiếng Vọng + một lần no nhẹ (kỳ ngộ dạy cách chơi) |

Muốn thêm thì giữ đúng khuôn: **dấu hiệu nhìn/nghe được → hành động bằng cơ thể → phần thưởng qua hệ thống sẵn có**.

## Chia việc giữa hai phiên
- **Bên làm hang:** gán kỳ ngộ cho khoang (luật thuần có test), dựng dấu hiệu và vùng kích hoạt trong khoang, gọi phần thưởng qua giao diện bên dưới. Không sửa `Scripts/Traits`, `Swallow`, `Survival`, `Player`.
- **Phiên chính:** cung cấp giao diện phần thưởng `Evolve.Traits.EncounterRewards` (sẽ làm sau khi chủ dự án duyệt danh sách): `GrantGene(player, minRarity)`, `AddMass(player, kg)`, `RerollFlaw(player, seed)`, `AddHeatResistance(player, value)`, `RefillStats(player)`, `BoostCore(player)`, `GiveStomachItems(player, items)`, `Say(line)`.
- **Đã có sẵn cho bên làm hang dùng ngay:** `Evolve.Traits.LightShafts.Register(center, radius, topY)` để khai báo tia sáng trong khoang (Nhân Quang Hợp đọc nó). Hang trú dùng `ShelterZone` như cũ (Nhân Màng Thẩm Thấu và Mau Khô coi hang trú là "có rêu ẩm").

## Câu hỏi cho chủ dự án trước khi dựng
1. Danh sách 10 kỳ ngộ trên có giữ không, bỏ hay thêm cái nào?
2. Kỳ ngộ số 4 (đổi Khuyết Điểm) và số 8 (tăng Lõi) có quá mạnh không?
