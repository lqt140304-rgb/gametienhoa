# Lộ trình phát triển

Mỗi giai đoạn kết thúc bằng một bản **chơi thử được**. Chỉ sang giai đoạn sau khi trả lời được câu hỏi của giai đoạn trước.

| Giai đoạn | Nội dung | Câu hỏi cần trả lời |
|---|---|---|
| **0.1: Slime sống sót** | Trườn, nảy, ăn, đói, chết; nhìn đen trắng; giác quan rung; 1 Dị Năng | Điều khiển slime có đã tay không? |
| **0.2: Tiến hóa đầu tiên** | Con mồi nhỏ rơi cơ quan; gắn cơ quan qua điểm gắn cố định (mắt → có màu, chân → chạy nhanh) | Cảm giác "lớn lên" có đã không? |
| **0.3: Thế giới phản kháng** | Kẻ thù săn người chơi; chỉ số nhiệt độ; vùng thứ 2 (hoang mạc) | Có áp lực buộc phải tiến hóa không? |
| **0.4: Co-op** | 2–4 người chơi qua Netcode for GameObjects; đồng bộ chỉ số và cơ quan | Chơi chung có vui hơn chơi một mình không? |
| **0.5: Vòng lặp hoàn chỉnh** | Boss đầu tiên; xây tổ; lưu tiến trình; chọn Thiên Phú và Khiếm Khuyết | Một ván chơi có trọn vẹn không? |
| **0.5a: Hồ Khởi Sinh (Underroot Lake)** | Địa hình hồ ngầm, bậc cơ quan D, quần thể theo vùng thay spawner, kẻ săn B đi vào từ rìa, xác phân hủy 3 giai đoạn | Khu hồ có "sống" như một hệ sinh thái không? |
| **0.5b: Khát, nước, nơi trú** | Thanh Khát, nước sạch/bẩn, nơi trú ẩn, chỉ số Căng thẳng | Có phải đi lại giữa các vi sinh cảnh để sống không? |
| **0.5c: Dinh dưỡng** | 5 chất kiểu slime (Sợi Nhầy, Tinh Vôi, Huyết Thiết, Mỡ Nhờn, Tinh Chất Sống); thiếu thì cơ quan yếu dần | Thức ăn có còn là lựa chọn có ý nghĩa không? |
| **0.5d: Bệnh tật** | 2–3 bệnh (nước đọng, thối gel, mờ bào tử), lây trong co-op | Hậu quả của lựa chọn sai có đủ rõ không? |
| **0.5e: Gel mô phỏng cơ quan** | Cơ quan là gel mô phỏng hình dạng; chết thì gel tan, rơi bản sao bản mẫu | Cảm giác cơ thể slime có đúng không? |
| **Sau đó** | Thêm vùng, Dị Năng, cơ quan; thay hình khối tạm bằng model Blender | — |

## Nguyên tắc

- **Đồ họa làm sau cùng.** Đến hết 0.3 dùng hình khối tạm (cầu, hộp). Chỉ làm model Blender khi đã chốt cần cơ quan nào.
- **Dữ liệu tách khỏi hiển thị** (`SurvivalState` là ví dụ) để chuyển sang multiplayer ở 0.4 không phải viết lại.
- **Mỗi thay đổi đi qua pull request**, chơi thử rồi mới merge.

## Trạng thái

- [x] Thiết lập repo, project Unity URP
- [x] Code 0.1: di chuyển, camera, chỉ số sinh tồn, thức ăn, thị giác, giác quan rung, khung Dị Năng
- [x] Công cụ tự dựng scene (menu **Evolve → Build Prototype Scene**)
- [x] Chơi thử 0.1, chỉnh tốc độ (trườn 1, nước rút ×1.5) và cơ chế đói (chỉ giảm khi di chuyển, nước rút tốn thức ăn)
- [x] Code 0.2: con mồi bỏ chạy và biết mệt, rơi cơ quan; Mắt sơ khai (có màu, nhìn xa), Chân giả (×1.4 tốc độ); điểm gắn trên thân; slime quay mặt theo hướng đi
- [x] Chơi thử 0.2: con mồi quá dễ bắt, cơ quan mọc ngay lập tức
- [x] Con mồi nhanh hơn (1.6); cơ quan lớn bằng thức ăn: mỗi cơ quan cần 30 thức ăn, một nửa mỗi bữa ăn nuôi cơ quan (cả bữa nếu đã no)
- [x] Chơi thử lại 0.2: con mồi tăng lên 1.8
- [x] Bảng chọn khi chạm cơ quan: lắp, thay thế, hấp thụ, bỏ qua; cơ quan bị thay rơi ra đất; bảng Cơ thể (G) để tự tháo
- [x] Chơi thử bảng chọn cơ quan
- [x] Bậc cơ quan C/B/A/S và bảng rơi đồ (DropTable) có tỉ lệ theo bậc; thêm Chân giả cơ bắp (B), Chân chạy (A)
- [x] Chơi thử tỉ lệ rơi
- [x] Code 0.3: kẻ săn mồi (tuần tra, đuổi, cắn, mệt; khó phát hiện khi đứng yên), nhiệt độ cơ thể, vùng hoang mạc 50°C, Màng chịu nhiệt (C), Gai độc (B), bảng rơi riêng của kẻ săn mồi, hồi sinh bằng R
- [x] Chơi thử 0.3
- [x] Mạng sống (3 mạng); chết thì cơ quan rơi tại chỗ, mỗi cái 50% mất
- [x] Menu God (F1) để thử nghiệm
- [x] EXP tiến hóa (ăn ít, hấp thụ cơ quan nhiều), cấp cơ quan đến Lv3, chi EXP chủ động và ưu tiên tự động
- [x] Nút quét rung (C) thay cho chấm sáng tự động; vùng nhìn quanh nhân vật khi chưa có mắt
- [x] Bestiary v0.1 (nhánh `docs/bestiary`): chốt cơ chế kỹ năng cơ quan, 12 vị trí, bậc quái, HP theo số người (xem `GAME_DESIGN.md` mục 4.5)
- [x] Sửa bảng rơi con mồi: bỏ Chân chạy (A) (đã chốt trong Bestiary)
- [ ] Gộp `Prey`/`Predator` vào `Creature` + `CreatureSO` (Bestiary mục 8), trước khi làm co-op
- [x] Siết bảng rơi kẻ săn mồi theo Bestiary mục 3.2 (45/48/7/0)
- [x] 5 ô kỹ năng gắn cơ quan (phím 1–5), mở ở Lv2, Lv3 nâng cấp; 2 kỹ năng mẫu (Xung Phong Húc, Xả Gai)
- [x] Chơi thử 5 ô kỹ năng: Xả Gai tỏa quanh người nên không biết bắn đi đâu
- [x] Chế độ ngắm (Alt): hồng tâm, chuột xoay camera; Xả Gai thành chùm gai bắn về phía hồng tâm
- [x] Chơi thử chế độ ngắm: Alt hoạt động
- [x] HUD/UI mới của Codex trên `main` (kho kỹ năng K, bảng cơ quan G, thanh Q · 1–5 · C, HUD cố định hoặc đi theo nhân vật)
- [x] Chơi thử chế độ ngắm: hồng tâm quá gần nhân vật; đạn ở chế độ chuột không theo con trỏ
- [x] Camera lệch qua vai khi ngắm; đạn và lao theo con trỏ ở chế độ chuột; góc nhìn thứ nhất (F5)
- [x] Chơi thử camera vai, góc nhìn thứ nhất, HUD mới và năng lượng hồi khi di chuyển
- [x] Co-op 0.4a (nền móng): Host + nhập IP, thấy nhau (vị trí, cơ quan, máu, mạng); mã trong assembly `Evolve.Net`, cần cài gói Netcode (`docs/COOP_SETUP.md`)
- [x] Cài gói Netcode 2.14.0, co-op 0.4a chạy được (thấy nhau)
- [x] Co-op 0.4b đợt 1: thế giới chung (quái, thức ăn, cơ quan rơi do Host điều khiển), cắn, bắn, ăn, nhặt; chưa chạy thử
- [ ] Chơi thử 0.4b đợt 1
- [x] Co-op 0.4b đợt 2: cứu đồng đội, chuyền máu, chế độ quan sát (cần chơi thử)
- [ ] Chơi thử HUD mới ở nhiều độ phân giải và chỉnh số kỹ năng Xả Gai / Xung Phong Húc
- [x] Spec lát cắt Hồ Khởi Sinh + sinh tồn sâu 0.5a–0.5e (`docs/superpowers/specs/2026-11-17-underroot-survival-slice-design.md`)
- [x] Code 0.5a: **đã code, chưa biên dịch/chạy thử** (chưa có Unity để biên dịch): bậc D, `Ecology/` (quần thể, `Region`, `Den`, xác, kẻ săn B đi vào), `VibrationEmitter`, menu **Evolve → Build Underroot Lake**, test EditMode
- [ ] Biên dịch và chạy thử 0.5a theo danh sách kiểm tra ở `PROJECT_STATUS.md` mục 8
- [ ] Chốt các câu hỏi trong `docs/GDD.md`
