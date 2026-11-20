# Lõi và Khuyết Điểm đợt 2: kế hoạch

Chủ dự án duyệt (2026-11-20): **làm ngay những cái game đã đủ hệ thống**; những cái còn thiếu nền thì chờ, ghi rõ chờ gì. Thông số lấy từ `Scripts/Traits/TraitSeed.cs` (mọi số đều tạm, chỉnh khi chơi).

## A. Làm ngay (phiên chính)

| Thứ | Loại | Hiệu ứng trong game | Móc vào đâu |
|---|---|---|---|
| Nhân Ngụy Sắc | Lõi (Hiếm) | Đứng yên 2 giây thì kẻ săn mồi chỉ thấy ở 40% tầm thường (cộng thêm với mức giảm sẵn có khi đứng yên). Di chuyển hoặc bị đánh thì mất ngay. | `Predator.UpdateTarget` qua `TraitSense` |
| Nhân Màng Thẩm Thấu | Lõi | Đứng yên (dưới 0,3 m/s) trong nước nông, hoặc trong hang trú (có rêu ẩm), thì no thêm 1,5%/giây, ngừng hao no; chỉ lấp tới 60%. Bị đánh thì ngắt 3 giây. Bơi thì không. | `PlayerTraits.Update` → `SurvivalState` |
| Nhân Quang Hợp | Lõi | Ở trong tia sáng thì hồi thêm 3 năng lượng/giây. Lặn sâu thì không (nổi trên mặt nước vẫn được). | `LightShafts` (đăng ký vùng sáng; Hang Khởi Sinh đăng ký tia nắng qua lỗ trần) |
| Nhân Ăn Mòn | Lõi | Hòa tan xác nhanh hơn 30%, mọi thứ nuốt vào (xác, con mồi sống) cho thêm 30% dinh dưỡng. | `Swallower` |
| Dễ Bị Chú Ý | Khuyết Điểm | Kẻ săn mồi đang đi tuần trong 20 m bị kéo dần về phía bạn (rồi tự phát hiện theo tầm thường). | `Predator` (nhánh đi tuần) |
| Vệt Mùi | Khuyết Điểm | Đi đâu để lại vệt mùi 20 giây; kẻ săn mồi ở trong 15 m của vệt thì lần theo bạn. | `ScentTrail` + `Predator.UpdateTarget` |
| Ngại Đông | Khuyết Điểm | Có từ 3 sinh vật trở lên trong 6 m thì hồi năng lượng chậm 20%. | `SurvivalState.EnergyRegenMultiplier` |
| Mau Khô | Khuyết Điểm | Xa nước và hang trú quá 90 giây thì đi chậm 8%; chạm nước hoặc vào hang trú là hết. | `SlimeController.TraitSpeed` |
| Bám Bùn | Khuyết Điểm | Ra khỏi nước nông vẫn chậm thêm 2 giây (chậm 30%). Chưa có bùn: chỉ tính nước nông. | `SlimeController.TraitSpeed` |

Cách làm: luật thuần trong `Scripts/Traits/Pure/EnvTraitRules.cs` (+ `ScentTrail`), test ở `Tests/EditMode/Traits/Pure/EnvTraitRulesTests.cs`, chạy bằng `tools/cstest/run.sh`. Phần Unity chỉ đọc trạng thái và gọi luật. Mỗi thứ xong thì bật `Implemented = true` trong `TraitSeed` (bỏ chữ "chưa có hiệu ứng" trên lá bài). Co-op: phát hiện của kẻ săn mồi chạy ở máy chủ, đọc `PlayerTraits` của avatar (đã được đồng bộ qua `TraitsSync`).

## B. Chờ nền (chưa làm)

| Thứ | Chờ gì | Hướng xử lý đề xuất |
|---|---|---|
| Nhân Thích Nghi | Nhiều loại sát thương môi trường (mới có nhiệt) | Khi hang có độc, lạnh, axit thì làm. Tạm thời có thể chỉ áp cho nhiệt. |
| Nhân Phản Chấn | "Đòn đánh kế tiếp" (slime chưa có đòn đánh thường) | Đổi thành: bị đánh thì lần dùng kỹ năng tấn công kế tiếp (Phóng Gai...) mạnh hơn. Cần chủ dự án duyệt. |
| Nhân Cộng Sinh | Chỉ có nghĩa khi co-op | Làm được, nhưng nên làm cùng đợt kiểm co-op. |
| Nhân Bám Dính | Hệ thống hất văng / đẩy lùi chung | Hiện chỉ có cú cắn của kẻ săn mồi đẩy lùi; có thể làm phần đó trước. |
| Nhân Thần Tốc | Nhịp nảy | Làm được nhưng cần chơi thử cảm giác nhịp; để đợt sau. |
| Mờ Trong Tối, Lạ Vùng | Vùng tối và vùng mới của mê cung | Chờ mê cung (bên làm hang) có khái niệm khoang/vùng. |
| Cơ Quan Mau Mòn | Cơ quan đã đổi thành kỹ năng | Đổi thành: kỹ năng hồi chiêu lâu hơn 20%. Cần chủ dự án duyệt. |

Việc "cơ quan bù khuyết điểm" (`FlawDef.CompensatedBy`) cũng dựa trên cơ quan; giữ nguyên cho tới khi chủ dự án chọn cách bù mới (ví dụ bằng kỹ năng đã học).

## C. Kỳ ngộ mỗi hang

Đã duyệt: 12 hang, mỗi hang một kỳ ngộ, quái đặc trưng và kỹ năng hấp thụ được. Chi tiết và chia việc: `docs/superpowers/handoff/2026-11-20-cave-encounters-brief.md`.
