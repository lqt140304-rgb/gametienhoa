# Bộ kỹ năng chuẩn của mọi trùm (chủ dự án chốt 2026-11-21)

**Mỗi trùm có đúng:** 1 kỹ năng **đánh xa**, 1 **nội tại** (luôn bật, không có nút), 2 kỹ năng **đánh thường**, 1 kỹ năng **đặc biệt tầm trung có mini game** (giữ người chơi, phải thoát bằng thao tác), và 1 **tuyệt chiêu khi gần hết máu** (máu ≤ 25%).

- Trong mã: `MoveDef.Role` = `Normal`, `Ranged`, `Special`, `Ultimate` (`MoveRole`); nội tại ở `PassiveRules` (nhân sát thương nhận vào, nhân tốc độ, hồi máu chậm, theo máu). Test `BossKitTests` kiểm: đủ 2 thường, 1 xa (tầm tối thiểu ≥ 5 m), 1 đặc biệt (có `Pins`), 1 tuyệt chiêu (mở khi máu ≤ 0,25, bốn chiêu kia luôn dùng được), tổng 5 chiêu và có nội tại. **Thêm tên loài vào danh sách `Kitted` của test khi dựng xong bộ chiêu của trùm đó.**
- Mini game đặc biệt dùng chung `PinGame` / `HoundPin`: **cắn** (ấn xen kẽ A và D, 6 sát thương/giây) hoặc **chộp** (ấn liên tục Space/A/D, bóp 9 sát thương/giây, thanh tụt nhanh; không thoát thì bị đập xuống 22 sát thương).
- Học theo trùm của các game khác: kỹ năng đánh xa nên **báo trước rõ và né được**; tuyệt chiêu **báo rất lâu và mạnh**; đòn đặc biệt là **đòn giữ** (kiểu bị chộp trong Monster Hunter, Elden Ring), thoát bằng nhịp bấm.

## Đã làm
| Trùm | Đánh xa | Nội tại | Thường 1 | Thường 2 | Đặc biệt (mini game) | Tuyệt chiêu |
|---|---|---|---|---|---|---|
| **Golem Thạch Anh** | (không có Ném Đá thường: đá ném nằm trong Giáng Thạch) | **Thân Tinh Thể**: −20% sát thương, tinh thể mọc ra khi máu ≤ 66% (−35%), hồi 1%/giây sau 8 giây yên | Nện Đấm (gần, 28) | Đập Đất (vùng 5,5 m, hất văng, 32) | **Giáng Thạch** (nhảy lên trời, vòng báo dưới đất, lao xuống; gai đá mọc quanh; nhổ gai phi liên tục. Câu đố né, không có mini game giữ) | **Lõi Nổ** (vùng 8 m, 40) |
| **Chó Xương** | Phun Xương (mảnh xương xoay, 14) | **Khát Máu**: chạy nhanh hơn 25% khi máu ≤ 50% | Cắn Nghiền (20) | Cào Xé (16) | **Vồ** (lao rồi cắn chặt, ấn xen kẽ A/D) | **Triệu Hồi** (3 con chó nhỏ chui từ đất) |

Gầm và Nhảy Vượt của Chó Xương không còn trong bộ chiêu (mã vẫn giữ); Giáp Tinh Thể của Golem thành phần nội tại.

## Đề xuất cho các trùm còn lại (chờ chủ dự án duyệt trước khi làm; số tạm)
| Trùm | Đánh xa | Nội tại | Thường 1 | Thường 2 | Đặc biệt (mini game) | Tuyệt chiêu |
|---|---|---|---|---|---|---|
| **Cóc Hồ Già** | Lưỡi Bắn (quất lưỡi 6–12 m, kéo con mồi) | Da Nhầy: kẻ đánh trúng bị chậm 30% | Nhảy Đè (bụng đè, vùng nhỏ) | Ợ Chấn (đẩy lùi 4 m, theo thiết kế sẵn) | Nuốt Chửng (nuốt người chơi vào bụng, ấn liên tục để thoát, bị axit) | Phình Nổ (phồng họng rồi nổ sóng và mây độc) |
| **Rắn Dung Nham** | Phun Nọc Dung Nham | Thân Nóng: kẻ đánh trúng bị bỏng 3/giây | Cắn Nọc (cắn kèm bỏng) | Quật Đuôi (quét vòng) | Quấn Siết (cuộn quanh người chơi, ấn xen kẽ A/D) | Phun Lửa Hình Nón (tầm 6 m, đất cháy, xòe mang) |
| **Bọ Cạp Thạch Anh** | Phóng Gai Tinh Thể (loạt gai từ đuôi) | Vỏ Pha Lê: −25% sát thương từ phía trước, không bị choáng | Châm Choáng (theo thiết kế sẵn) | Giáng Đuôi (đập từ trên xuống) | Kẹp Càng (kẹp giữ, ấn xen kẽ A/D) | Đào Chui Trồi Lên (lặn xuống rồi trồi lên đánh vùng lớn) |
| **Chim Hang Mẹ** | Bắn Lông (loạt lông bay) | Lướt Gió: nhanh hơn 20% và không bị hất văng | Mổ (gần) | Vỗ Cánh Gió (đẩy lùi) | Quắp Bay (quắp người chơi lên rồi thả, ấn liên tục để thoát) | Bổ Nhào Gầm Rít (rít làm choáng rồi bổ nhào liên tiếp) |
| **Cáo Gió** | Giáo Gió (đã có) | Gió Bảo Hộ: né đòn đầu tiên mỗi 10 giây | Vồ Cắn (đã có) | Quét Đuôi Gió | Vòng Xoáy (giữ trong lốc xoáy, ấn xen kẽ A/D) | Bão Giáo (nhiều giáo bay vòng quanh rồi phóng) |
