# Underroot Lake — bản đồ 300 m theo concept zoning (thiết kế)

## Ý định
Người chơi đã duyệt bản đồ phân khu (Hồ Khơi Sinh, 6 khu) và 6 ảnh concept. Muốn bản đồ **rộng hơn bản gốc 120 m → 300×300 m** (mọi kích thước khu ×2,5, kích thước sinh vật giữ nguyên nên thế giới "to" hơn so với slime 0,6 m), màu ấm đất theo concept, slime lội/chìm được trong nước, mái là lưới rễ phủ kín (tia sáng vừa phải), viền quanh có rễ cây chứ không chỉ tường trơn.

## Mục tiêu
- Địa hình 300 m (lưới ~0,6 m/điểm) thay vùng 480 m; một bảng số liệu duy nhất (`ZoneLayout`) mô tả hồ, nước và 6 khu; địa hình, nước, mòn, điểm nhấn đều đọc từ bảng.
- Bố cục (bắc ở trên, mét từ tâm, đã ×2,5): 01 Vũng Sinh Mệnh (8,-80) 75×62; 02 Chân Rễ Cổ Thụ (-85,-15) 62×50; 03 Bãi Mùn Rễ (-70,68) 75×62; 06 Rìa Săn Mồi (20,90) 75×50, lối vào sinh vật B ở bắc; 04 Vườn Bào Tử (98,65) 62×62; 05 Khe Thủy Mạch (122,-10) 88×38, suối nông 0,15 m, hai hố sâu 2 m. Hồ lớn nằm giữa, vùng nước nông ở nam.
- Slime chìm nửa người ở nước nông (~0,4 m), lội chậm 65%; hố sâu 2 m (slime không vào được trừ khi muốn — quyết định riêng).
- Mái: lưới rễ dày phủ kín độ cao ~25 m, vài khe tia sáng (khoảng 8–10, vừa phải). Rìa: cụm rễ chân tường từ nền lên mái, dày, nhiều lớp.
- Điểm nhấn: 01 cụm gel; 02 hang rễ (khe vào 0,9×0,65 m, trần 12 m); 03 khúc gỗ mục Ø1,5 m + lá rụng; 04 cụm nấm lớn kem (cao tới 1,2 m cho cụm nhỏ, lớn hơn cho landmark); 05 đá vôi + suối trong + 2 hố sâu; 06 vòm rễ 4×8 m.

## Không làm / tránh
- Không đổi gameplay, hệ sinh thái, co-op. Không để bản đồ có vật thể vô hình chặn đường không rõ lý do.
- Nước **màu xanh ngọc cũ** cho mọi nguồn (người chơi chê màu ô-liu bẩn); chỉ giữ màu ấm cho bùn, rêu, nấm, ánh sáng.

## Các cách đã cân nhắc
1. Giữ 480 m, dời khu lại gần: ít việc, nhưng không khoét sâu được (lưới 0,9 m), bản đồ trống.
2. **Dựng lại địa hình 300 m đúng bản đồ (đã chọn).** Đụng `WorldScale`/`TerrainShape`/`ZoneLayout` + test.
3. Cảnh riêng 300 m, giữ 480 m cũ: hai bản đồ song song.

## Ràng buộc
Unity 6.6 URP, C# 9, không có Unity trong môi trường làm việc (chưa biên dịch/chạy; người chơi kiểm tra). Giữ các công cụ Dress Lake chạy lại được (idempotent). Hiệu năng: chia chunk 64 m + culling theo khoảng cách như đang có.

## Giao cho người làm
Công thức chính xác của đa giác từng khu và lưới rễ mái; số liệu cao độ; thứ tự bước. Đề xuất chia bước: (1) WorldScale/ZoneLayout/địa hình/nước/dome+rễ viền+mái, (2) nền & thực vật theo khu, (3)–(8) điểm nhấn 01…06.

## Những lựa chọn của tôi (người chơi xem lại)
Kích thước ×2,5 chính xác; tia sáng 8–10; hố sâu vẫn chặn slime (chưa hỏi lại); các tọa độ khu đo từ ảnh bản đồ nên sai số vài mét.
