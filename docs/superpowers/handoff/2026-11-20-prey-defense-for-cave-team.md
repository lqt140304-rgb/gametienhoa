# Cho bên làm hang: con mồi biết chống trả (cần bạn làm 2 việc trên prefab)

Con mồi nay có phòng vệ riêng (thoát thân, trả đũa, 3 cơ chế đặc biệt). Code ở `Scripts/Creatures/PreyDefense.cs`, dữ liệu 21 loài ở `Scripts/Creatures/Pure/PreyDefenseSeed.cs` (bảng đã duyệt: spec `docs/superpowers/specs/2026-11-20-caves-creatures-ecosystem-design.md` mục 4). **Bạn không cần sửa code**, chỉ làm trên prefab của từng loài con mồi:

1. **Gắn component `PreyDefense`** cạnh `Prey` (nó tự yêu cầu có `Prey`).
2. **Đặt tên prefab đúng tên loài** (ví dụ `Ốc Hồ`, `Thỏ Lá`), hoặc điền ô `Defense Id` bằng id trong bảng (ví dụ `tho_la`). Không tìm thấy thì con mồi chỉ chạy như cũ.

Lưu ý:
- **Chim Hang Mẹ** phải có tên bắt đầu bằng `Chim Hang Mẹ` (là `Predator`): Chim Non bị đánh sẽ kêu gọi nó đến.
- Prefab cần collider không phải trigger như `Prey` vẫn yêu cầu. Khi bị chạm, `Prey` hỏi `PreyDefense` trước khi bị nuốt (có loài chống lại được việc bị nuốt).
- Hiệu ứng "ẩn nấp" của Nấm Đi Bộ, Chuột Gió, Giòi Trắng, Cua Đá Nóng: nếu có hố, khe hoặc xác để chui vào thì chưa nối; hiện chỉ đứng yên.
- Thiếu `drops` vẫn để trống như cũ (do phần lõi game gán).
