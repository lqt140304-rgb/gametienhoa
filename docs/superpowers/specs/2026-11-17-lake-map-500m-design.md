# Hồ Khởi Sinh — bản đồ 500 m, hình dạng tự nhiên, quần xã sinh động (thiết kế)

Thay thế bản 300 m (`2026-11-17-lake-map-300m-design.md`). Gộp góp ý của phiên kia (`map5000-supplement`); **phiên này làm hết** bước A (số liệu, địa hình, nước, bơi) và các bước trang trí.

## Ý định
Người chơi thấy 300 m còn nhỏ, khu hẹp, hình dạng dập khuôn (hồ elip, khu chữ nhật), slime "đi trên nước" chứ không bơi, thực vật phủ đều như nhau. Muốn: bản đồ lớn, tự nhiên như concept, mỗi khu có quần xã riêng hòa dần sang khu bên, địa hình **không quá phẳng**.

## Mục tiêu
1. **500 × 500 m**, đi ngang ~8 phút ở 1 m/s. Mỗi khu cỡ **~80 m** (cạnh lớn nhất 80, giữ tỉ lệ concept).
2. **Vị trí** (từ bảng phiên kia, tọa độ mét, bắc = +z): 01 Vũng Sinh Mệnh (-8,-120); 02 Chân Rễ Cổ Thụ (-150,-18); 03 Bãi Mùn Rễ (-125,118); 04 Vườn Bào Tử (137,110); 05 Khe Thủy Mạch (173,-8); 06 Rìa Săn Mồi (13,153), lối vào của B ở (17,233).
3. **Hình dạng tự nhiên, sinh từ nhiễu có hạt giống (cố định, dựng lại y hệt):**
   - Hồ quanh tâm (-8,-42), cỡ ~220 × 160 m; viền là vòng tròn méo theo nhiều lớp nhiễu (vịnh, mũi đất). Độ sâu gồ ghề: bờ thoải ~6 m, giữa hồ **1,2–1,8 m** (bơi được), có bãi cạn 0,3–0,4 m; vịnh nông 0,4 m ở phía khu 01.
   - Suối từ (213,10) qua (187,-7), (158,-17), (133,-25) tới bờ đông hồ (103,-37); uốn khúc, rộng **7–12 m** thay đổi dọc dòng, sâu 0,15 m; hai hố sâu 2 m (bán kính ~10 m, viền méo) ở (193,0) và (157,-20).
   - Vũng đọng khu 03 ở (-120,103), ~6 m, sâu 0,3 m, **nước vàng lục** (bẩn); mọi nước khác **xanh ngọc cũ**.
   - Khu không còn hình chữ nhật: mỗi khu là một **vùng ảnh hưởng** (1 ở lõi, giảm dần tới 0 trong ~60 m), biên méo theo nhiễu.
4. **Địa hình không phẳng:** trong khu gợn nhẹ ±1–1,5 m (đồi thấp, trũng); giữa các khu gò/đồi rễ cao tới ~6 m; quanh rìa vách lên ~20 m từ ±175 m tới ±250 m, khe ở lối B phía bắc. Dốc đi được (≤ ~35°) trừ vách rìa. Đường mòn được làm thoải dọc theo đường đi (không san phẳng cả vùng).
5. **Quần xã sinh động:** mỗi loài có bản đồ "đám" riêng (nhiễu ngưỡng): nấm thành cụm, rêu thành mảng, lá thành đống, có chỗ trống. Loài được chọn theo tỉ lệ ảnh hưởng của các khu tại điểm đó ⇒ hòa dần giữa khu. Bảng thực vật (đã duyệt):
   - 01: bùn ướt, rêu thấp, nấm kem nhỏ, cụm gel ở bờ vịnh
   - 02: thảm rêu dày, rễ nổi, cỏ ba lá, nền mùn ấm
   - 03: lá rụng dày, gỗ mục, nấm tai mèo
   - 04: nấm kem dày đặc đủ cỡ, bụi bào tử
   - 05: đá vôi nhạt phủ rêu, cỏ ba lá, nấm nhỏ
   - 06: bùn trơ, dấu chân, cây thưa, rễ tối
   Màu nền (texture địa hình) cũng hòa theo ảnh hưởng khu.
6. **Bơi thật sự** (sửa lỗi "đi trên nước"): nước vẽ **sau** slime (thân gel ghi độ sâu), nên phần thân dưới nước hiện mờ, nhuốm màu nước thay vì bị cắt; khi bơi tâm thân ở **0,2 m dưới mặt nước** (chỉ lưng ló lên), nhấp nhô nhẹ, vòng gợn sóng quanh thân; camera dưới mặt nước phủ màu nước mờ. Nước ≥ 0,5 m: bơi (Space nổi, Ctrl lặn, 65% tốc độ, tốn năng lượng nhẹ, không chết đuối). Nước nông: lội 65%.
7. Mái lưới rễ ~25 m phủ kín, 8–10 tia sáng; rễ nhiều lớp quanh vách rìa. Điểm nhấn từng khu 01→06 (như bản 300 m: cụm gel, hang rễ khe 0,9×0,65 m trần 12 m, gỗ mục Ø1,5 m, cụm nấm lớn, đá vôi + 2 hố, vòm rễ 4×8 m).

## Không làm / tránh
- Không hình học dập khuôn (elip, chữ nhật, đường thẳng dài) cho hồ, suối, khu, đường mòn.
- Không vật cản vô hình quanh nước. Không đổi AI/hệ sinh thái/co-op (chỉ dời vị trí theo bố cục).
- Không để FPS tụt: thực vật chia chunk 64 m + culling theo khoảng cách như hiện có.

## Cách đã cân nhắc
1. **Sinh hình dạng từ nhiễu có hạt giống (đã chọn):** tự nhiên, kiểm bằng test, dễ chỉnh bằng vài số.
2. Vẽ tay viền hồ/suối/khu theo concept: giống ảnh hơn nhưng hàng trăm điểm, khó sửa; thực vật vẫn cần cách 1.

## Ràng buộc
Unity 6.6 URP, C# 9; không có Unity trong môi trường làm việc (chưa biên dịch/chạy; người chơi gửi ảnh Console). Mọi công cụ dựng chạy lại được, không nhân đôi. Mọi vật đặt theo độ cao mặt đất thật (không giả định y = 0). Heightmap 1025 (≈0,5 m/điểm) để hố sâu không răng cưa.

## Lựa chọn của tôi (người chơi kiểm)
- Độ sâu hồ 1,2–1,8 m (bảng phiên kia ghi 0,4 m — quá nông để bơi).
- Kích thước khu: cạnh lớn ~80 m theo tỉ lệ concept (01 80×70, 02 70×60, 03 80×70, 04 70×70, 05 90×40 theo dòng suối, 06 80×55 — lấy từ bảng phiên kia).
- Vùng ảnh hưởng mỗi khu giảm trong ~60 m; đồi giữa khu tới ~6 m.
- Mỗi bước là một PR: A (số liệu, địa hình, nước, bơi) → B (mái rễ, nền, thực vật theo quần xã) → C (01, 02) → D (03, 04) → E (05, 06, tài liệu).
