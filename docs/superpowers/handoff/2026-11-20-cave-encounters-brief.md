# Giao việc cho bên làm hang (Antigravity): 12 hang, kỳ ngộ, quái (đã duyệt 2026-11-20)

Đọc kèm `2026-11-19-maze-step3-brief.md` (bối cảnh, cách kiểm, ranh giới thư mục). **Nội dung đã duyệt nằm ở** `docs/superpowers/specs/2026-11-20-caves-creatures-ecosystem-design.md`: 12 hang và kỳ ngộ (mục 3), 44 loài (mục 4), hệ sinh thái (mục 5). File này chỉ ghi phần **bên làm hang** phải làm.

Lưu ý chung: **tạm chưa làm nội dung dưới nước** (không lặn, không hầm ngập, không quái dưới nước); nước dâng / Chu Kỳ Lũ đã bỏ. Mọi chữ hiện trong game và báo cáo bằng tiếng Việt.

## Việc cần làm
1. **Dựng 9 hang còn thiếu** theo chủ đề ở spec mục 3 (Hang Rêu Thở, Tổ Bỏ Hoang, Hang Nấm Huỳnh Quang, Suối Nước Nóng, Khe Gió Hú, Nghĩa Địa Cự Thú, Hang Hóa Thạch Slime Cổ, Hang Tiếng Thở, Khoang Phong Ấn). Ba hang đã có: Hang Khởi Sinh, Vườn Quặng Cầu Vồng (cây sắc màu, suối, quặng), Vực Nứt Tím (tinh thể tím, gai, vết nứt).
2. **Xếp 12 hang vào mê cung 3 tầng** (Nông: 1–4, Giữa: 5–8, Sâu: 9–12), xáo vị trí theo hạt giống mỗi vòng; Hang Khởi Sinh là điểm xuất phát, Khoang Phong Ấn ở tầng Sâu. Hang trú rải xen giữa. Đưa ra một giao diện để code khác hỏi **hang nào kề hang nào** (hệ sinh thái cần cho di cư).
3. **Kỳ ngộ của từng hang:** dựng dấu hiệu (ánh sáng, âm thanh, hình), vùng và điều kiện kích hoạt; phần điều kiện viết luật thuần có test (thứ tự tinh thể, đủ 5 màu quặng, nhịp thở của rêu, thời gian ngâm, đứng yên bao lâu…). Đạt điều kiện thì gọi `Evolve.Traits.EncounterRewards` (do phần lõi game làm, xem `2026-11-20-core-systems-brief.md` mục C; nếu chưa có thì để chỗ gọi sẵn và ghi vào `maze-questions.md`).
4. **Quái: hình dáng và nơi sống.** Với mỗi loài ở spec mục 4: làm prefab (mô hình, vật liệu, chuyển động cơ bản) gắn `Creatures/Prey` (con mồi) hoặc `Creatures/Predator` (kẻ săn, Chúa Hang); đặt **tổ** (`Ecology/Den`) và vùng sống trong đúng hang; số lượng gợi ý ở spec 5.4. **Để trống ô `drops`** (bảng rơi gene do phần lõi game tạo và gán). **Hành vi** (săn nhau, ăn xác, di cư, chạy trốn) do phần lõi game viết trong `Creatures/` và `Ecology/`; bên làm hang không sửa code hai thư mục đó, cần gì thì ghi câu hỏi.
5. **Nguồn sống của hang** (rêu, nấm, quặng, tảo nóng, địa y, hạt theo gió, xác): đặt trong cảnh để con mồi có cái ăn và slime nhìn thấy được chuỗi thức ăn.
6. **Chướng ngại ở lối vào hang** (vách cao, vách đá vụn, hầm nóng, vùng tối đặc, khe hẹp, gờ cao): chỉ dựng khung và điều kiện qua interface, vì kỹ năng mở chúng (Bám Vách, Tiết Axit, Chịu Nhiệt, Sóng Dò Mạnh, Thu Nhỏ, Nhảy Cao) đang được làm. Đặt chúng ở lối vào hang khác để hang này cho chìa mở hang kia.
7. **Tia sáng trong hang:** khai báo bằng `Evolve.Traits.LightShafts.Register(center, radius, topY)` (Nhân Quang Hợp đọc nó). Hang trú dùng `ShelterZone` như cũ (Nhân Màng Thẩm Thấu, Mau Khô coi hang trú là có rêu ẩm).

## Thứ tự gợi ý
Kỳ ngộ + quái của 3 hang đã có trước (để phần lõi game làm kỹ năng theo), rồi lần lượt các hang tầng Nông, Giữa, Sâu.

## Ranh giới
- **Được sửa:** `Scripts/World/`, `Scripts/Maze/` (hoặc thư mục mới của bạn), `Editor/Cavern*.cs`, `Editor/Maze*.cs`, prefab/vật liệu/mô hình mới, test mới.
- **Không sửa:** `Scripts/Traits`, `Swallow`, `Survival`, `Player`, `Abilities`, `Organs`, `Creatures`, `Ecology`. Cần gì thì ghi vào `docs/superpowers/handoff/maze-questions.md`.

## Báo cáo
Mỗi hang xong thì báo: đã dựng gì, kỳ ngộ kích hoạt thế nào, quái nào đã đặt, test nào, chưa làm gì. Không nói "xong" nếu chưa mở Unity kiểm. Đẩy lên nhánh riêng để gộp.
