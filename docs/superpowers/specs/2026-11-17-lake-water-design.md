# Thiết kế: Nước của Hồ Khởi Sinh (bước đầu của "trang trí map")

> Ngày: 2026-11-17 · Chủ dự án chọn: **làm trang trí đặt lên trên địa hình của phiên 0.6a, nước trước.**
> Nhãn: **[ĐÃ CHỐT]** chủ dự án quyết; **[ĐỀ XUẤT]** Claude đề xuất; **[BUILDER]** người làm tự quyết.

## Mục đích
Thay các hộp xám xanh phẳng đang đóng vai nước bằng **mặt nước xanh ngọc trong trẻo, gợn sóng chậm** như concept (hồ có rêu, rễ, nấm). Phần trang trí **không đụng vào code địa hình, va chạm hay vị trí** của phiên 0.6a (khu 480 m).

## Phạm vi
- Mặt nước cho: mọi `WaterZone` (nước nông sạch và nước đọng bẩn), Vũng Sinh Mệnh (đĩa lớn nơi xuất hiện), hốc nước sâu.
- **Không làm:** bờ rêu, nấm, dây leo, tia sáng (các bước sau); nước chảy có dòng thật; bơi/chìm; tiếng nước.
- **Thất bại dù chạy được:** nước che mất nơi trú hay vật khác; nước lơ lửng/lún khi địa hình đổi; nước đổi cách slime di chuyển (nó chỉ là hình ảnh).

## Cách làm [ĐỀ XUẤT]
- **Công cụ Editor riêng** `Editor/LakeWater.cs`, menu **Evolve → Dress Lake: Water**, đọc scene hiện có (WaterZone, "Pool Water", "Deep Pocket*"), ẩn các hộp phẳng cũ và thêm `WaterSurface` (không collider). Chạy lại bao nhiêu lần cũng được (thay mặt nước cũ của chính nó). Không sửa `UnderrootLakeBuilder.cs`.
- **`WaterSurface`** (runtime, `[ExecuteAlways]`): mesh lưới do code sinh (`WaterMeshes.Rect/Disc`, ô ~1,25 m), đỉnh nhấp nhô chậm (3 sóng sin, 20 Hz, biên độ 3 cm), họa tiết gợn (normal map) trôi chậm qua `MaterialPropertyBlock` (không tạo bản sao material).
- **Vật liệu** URP Lit trong mờ, không shader tùy chỉnh: nước sạch xanh ngọc (alpha 0,58), nước đọng xanh bùn (0,78), nước sâu xanh thẫm (0,88). Mỗi loại có ánh phát nhẹ để không đen kịt trong hang. Normal map gợn sóng sinh từ code (tileable), lưu PNG.
- Mực nước y = 0,04–0,06 m, đúng vùng lõi phẳng của 0.6a (y = 0).

## Hạn chế đã biết
- Mặt nước nằm ngang; chưa có bọt bờ và chưa mờ dần ở mép (cần shader tùy chỉnh hoặc mesh viền, để bước sau).
- Nước chỉ chuyển động khi Play (trong Editor là mặt tĩnh).

## Kiểm tra
Chạy menu → Console không lỗi; Test Runner xanh (có test `WaterMeshesTests`); Play: nước xanh ngọc gợn nhẹ ở Vũng Sinh Mệnh, khe nam và hai bên; nước đọng ở Bãi Mùn Rễ màu bùn; hai hốc sâu sẫm; slime đi qua nước như cũ; FPS không giảm đáng kể.
