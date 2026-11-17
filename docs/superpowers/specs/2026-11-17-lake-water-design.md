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
- Mặt nước nằm ngang. **Bọt bờ** (đã làm): một vòng viền trắng mờ dần hai phía (mesh vòng `WaterMeshes.DiscRing/RectRing`, vật liệu `Lake_Foam`) che đường cắt cứng giữa nước và đất. Mặt nước tự nó vẫn không mờ dần ở mép (cần shader tùy chỉnh).
- Nước chỉ chuyển động khi Play (trong Editor là mặt tĩnh).

## Bờ nước uốn lượn (sau phản hồi 17/11)
- Chủ dự án thấy bờ quá thẳng/tròn và viền bọt nhìn như sọc. Mỗi mặt nước giờ có **đường bờ ngẫu nhiên uốn lượn** (`WaterOutline`: hình chữ nhật bo tròn hoặc tròn, uốn bởi ba sóng dài ~11 m, ~6 m, ~3,4 m; mỗi ao một hạt giống riêng).
- Quanh bờ có **dải đất ướt** tối màu (`Lake_Bank`, mờ dần ra xa) và **dải bọt** loang lổ (`Lake_Foam`, nhiễu mịn), bề rộng dải cũng dao động.

## Kiểm tra
Chạy menu → Console không lỗi; Test Runner xanh (có test `WaterMeshesTests`); Play: nước xanh ngọc gợn nhẹ ở Vũng Sinh Mệnh, khe nam và hai bên; nước đọng ở Bãi Mùn Rễ màu bùn; hai hốc sâu sẫm; slime đi qua nước như cũ; FPS không giảm đáng kể.

## Các ao hòa vào nhau (sau phản hồi 17/11, lần 2)
Viền bọt và bờ đất ướt của từng ao từng bị vẽ đè lên nước của ao bên cạnh, tạo đường chia cắt. Giờ mọi `WaterSurface` biết nhau (danh sách `WaterSurface.All`): phần mặt nước nằm trong ao có ưu tiên thấp hơn bị bỏ (không vẽ nước hai lớp), và phần dải bờ/bọt rơi vào nước của ao khác bị bỏ. Hai ao chạm nhau thành một vùng nước liền.

## Mặt đất (bước 2)
Menu **Evolve → Dress Lake: Ground** (`Editor/LakeGround.cs`, chạy sau Dress Lake: Water): các mảng rêu mềm dọc bờ và rải trên đất phẳng, đá có mũ rêu (6 dạng), cỏ (3 sắc xanh), đường mòn bùn uốn lượn từ vũng sinh mệnh ra Bãi Mùn Rễ, Chân Rễ, Vườn Bào Tử (đứt đoạn khi gặp nước). Không collider, gộp thành vài mesh dưới một object, đặt theo bề mặt thật bằng tia chiếu xuống, chạy lại được. Bước sau: nấm to, dây leo, rễ, ánh sáng.

## Nấm khổng lồ và dây leo (bước 3)
Menu **Evolve → Dress Lake: Mushrooms and Vines** (`Editor/LakeFlora.cs`, chạy sau Dress Lake: Water): nấm khổng lồ (thân loe chân, mũ vòm dày, hơi nghiêng, đốm sáng trên mũ; mũ tím nhạt, san hô, xanh ngọc) thành cụm lớn ở Vườn Bào Tử, dọc bờ nước và rải rác, vài cây cao có đèn điểm; dây leo buông từ các rễ trần hang (`Root Ceiling`) với lá dọc theo dây. Không collider, gộp thành vài mesh dưới `Lake Flora (Dressed)`, chạy lại được. Bước sau: rễ chi tiết và ánh sáng (tia sáng xuyên trần).

## Rễ và ánh sáng (bước 4)
Menu **Evolve → Dress Lake: Roots and Light** (`Editor/LakeRootsLight.cs`, chạy sau Water và Ground):
- **Rễ**: vòm rễ già nhiều mấu (cao 2–5 m, có rễ phụ xuống đất và mảng rêu trên lưng) quanh Chân Rễ Cổ Thụ, tránh cửa các hốc trú; rễ bò thấp, chôn nửa, quanh các ao; rễ lớn buông từ trần hang có nhánh.
- **Ánh sáng**: tối đa 5 **tia sáng** nghiêng từ trần xuống (hình nón mờ dần, có sọc, nhìn được từ trong lẫn ngoài), mỗi tia có một **đèn rọi** mềm không đổ bóng chiếu xuống nước hoặc đất; **bụi bào tử** phát sáng trôi chậm trong không khí (một hệ hạt, tối đa 260 hạt).
Không collider; chạy lại được.
