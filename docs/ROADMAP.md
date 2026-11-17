# Lộ trình phát triển

Mỗi giai đoạn kết thúc bằng một bản **chơi thử được**. Chỉ sang giai đoạn sau khi trả lời được câu hỏi của giai đoạn trước.

| Giai đoạn | Nội dung | Câu hỏi cần trả lời |
|---|---|---|
| **0.1: Slime sống sót** | Trườn, nảy, ăn, đói, chết; nhìn đen trắng; giác quan rung; 1 Dị Năng | Điều khiển slime có đã tay không? |
| **0.2: Tiến hóa đầu tiên** | Con mồi nhỏ rơi cơ quan; gắn cơ quan qua điểm gắn cố định (mắt → có màu, chân → chạy nhanh) | Cảm giác "lớn lên" có đã không? |
| **0.3: Thế giới phản kháng** | Kẻ thù săn người chơi; chỉ số nhiệt độ; vùng thứ 2 (hoang mạc) | Có áp lực buộc phải tiến hóa không? |
| **0.4: Co-op** | 2–4 người chơi qua Netcode for GameObjects; đồng bộ chỉ số và cơ quan | Chơi chung có vui hơn chơi một mình không? |
| **0.5: Vòng lặp hoàn chỉnh** | Boss đầu tiên; xây tổ; lưu tiến trình; chọn Thiên Phú và Khiếm Khuyết | Một ván chơi có trọn vẹn không? |
| **Sau đó** | Thêm vùng, Dị Năng, cơ quan; thay hình khối tạm bằng model Blender | — |

## Nguyên tắc

- **Đồ họa làm sau cùng.** Đến hết 0.3 dùng hình khối tạm (cầu, hộp). Chỉ làm model Blender khi đã chốt cần cơ quan nào.
- **Dữ liệu tách khỏi hiển thị** (`SurvivalState` là ví dụ) để chuyển sang multiplayer ở 0.4 không phải viết lại.
- **Mỗi thay đổi đi qua pull request**, chơi thử rồi mới merge.

## Trạng thái

- [x] Thiết lập repo, project Unity URP
- [x] Code 0.1: di chuyển, camera, chỉ số sinh tồn, thức ăn, thị giác, giác quan rung, khung Dị Năng
- [x] Công cụ tự dựng scene (menu **Evolve → Build Prototype Scene**)
- [ ] Chơi thử 0.1 và ghi nhận phản hồi
- [ ] Chốt các câu hỏi trong `docs/GDD.md`
