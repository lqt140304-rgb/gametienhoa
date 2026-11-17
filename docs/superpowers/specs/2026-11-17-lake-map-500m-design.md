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

---

# Bổ sung (sau khi xem bản dựng đầu): độ cao khác nhau, các khu tách xa hơn, hồ méo hơn

## Vì sao
Người chơi thấy 6 khu nằm thành một vòng quanh hồ, đi đâu cũng men bờ hồ ⇒ đơn điệu; các khu sát nhau; hồ vẫn giống hình bầu dục. Muốn: khu ở trong hang, khu trũng xuống, khu trên cao; nhiều lối đi khác nhau.

## Đã chốt (người chơi duyệt)
| Khu | Độ cao | Mô tả |
|---|---|---|
| 01 Vũng Sinh Mệnh | ngang mặt hồ (0 m) | nơi xuất hiện, điểm xuất phát |
| 02 Chân Rễ Cổ Thụ | **trong hang** | bên trong khối rễ khổng lồ, trần ~12 m; một cửa rộng + một khe hẹp 0,9 × 0,65 m chỉ slime lọt |
| 03 Bãi Mùn Rễ | **hố trũng −6 m** | ẩm, gỗ mục, vũng đọng; xuống bằng dốc mùn |
| 04 Vườn Bào Tử | **cao nguyên +10 m** | lên bằng đường dốc uốn quanh rễ |
| 05 Khe Thủy Mạch | **khe sâu −8 m** | suối dưới đáy giữa hai vách đá vôi, hai hố sâu 2 m |
| 06 Rìa Săn Mồi | **sống đất +6 m** ở rìa bắc | nhìn xuống hồ; cửa hầm cho sinh vật B |

Lối đi: bơi tắt qua hồ; đường hầm từ hang 02 sang hố 03; cầu rễ nối 04 ↔ 06; lối leo từ khe 05 lên 04; đường mòn quanh hồ vẫn còn nhưng mỗi đoạn lên/xuống dốc khác nhau.

## Quyết định của tôi (người chơi kiểm)
- **Các khu tách xa hơn:** khoảng trống giữa hai khu kề nhau ≥ 65 m (trước 49–77 m). Tâm mới: 01 (-8,-135), 02 (-165,-15), 03 (-140,135), 04 (150,125), 05 (173,-12), 06 (10,168); vách rìa bắt đầu từ ±200 m (trước 175). Vùng ảnh hưởng mỗi khu giảm còn **~40 m**, để giữa các khu có vùng hoang riêng.
- **Hồ méo hơn:** bán kính bờ dao động khoảng **−15% … +33%** quanh hình bầu dục (3 tầng nhiễu: vịnh lớn, vũng nhỏ, khía bờ). Đường mòn được nắn lại để không lội qua hồ.
- **Cách làm độ cao:** mỗi khu có một "độ cao nền" (0 / hang / −6 / +10 / −8 / +6) và địa hình hòa dần giữa các nền bằng dốc đi được (≤ ~35°), trừ vách khe 05 và mép cao nguyên 04 (dốc đứng, có đúng một lối dốc lên và một lối leo). **Hang 02** = nền hơi trũng + **mái mesh** (rễ + đất) dựng bằng code phủ trên, có cửa và khe; địa hình Unity không có trần nên không thể làm hang bằng địa hình.
- **Đường hầm 02 → 03:** một ống rễ/đất (mesh) nối miệng hang với dốc hố 03, rộng ~3 m. **Cầu rễ 04 ↔ 06:** một rễ to nằm ngang, mặt trên phẳng rộng ~2 m.
- Mực nước hồ không đổi (0); suối chảy từ trên cao (nguồn ở rìa đông) xuống đáy khe 05 rồi ra hồ — suối đi theo đáy khe, phần dốc là ghềnh nhỏ.

## Thứ tự
Phần này gộp vào **PR A** (bảng số liệu + địa hình), trước khi làm thực vật (PR B). Hang 02, đường hầm và cầu rễ là mesh nên thuộc **PR C** (02) và **PR D/E** (cầu 04–06).

## LOD: nhìn được toàn cảnh mà không giật (người chơi yêu cầu)
Hiện mọi đồ trang trí ở xa hơn 150 m bị **ẩn hẳn** (DetailCulling) ⇒ nhìn xa thấy đất trống. Thay bằng nhiều mức chi tiết:
- **Đồ trang trí theo ô 64 m** (rêu, cỏ, lá, nấm, đá, rễ): mỗi ô có một `LODGroup` với 3 mức — **LOD0** (≤ ~60 m) đầy đủ; **LOD1** (≤ ~180 m) chỉ ~1/3 số vật, lưới ít đa giác hơn (nấm/đá/rễ ít cạnh, bỏ cỏ và lá nhỏ); **LOD2** (≤ ~450 m) chỉ còn vật lớn (nấm to, rễ to, đá to) dạng thô nhất; xa hơn thì tắt. Mức chuyển có mờ dần (cross-fade) để không "nhảy hình".
- **Mái lưới rễ, rễ viền, vòm rễ, hang**: 2 mức (đầy đủ / ít cạnh), không bao giờ tắt hẳn vì là khung cảnh chính.
- **Địa hình**: dùng LOD sẵn có của Unity Terrain (pixel error ~8 gần, basemap xa 200 m); **nước**: mặt nước xa > 200 m dừng gợn sóng (đứng yên), không ẩn.
- **Ánh sáng/bóng**: bóng đổ chỉ trong ~80 m; đèn điểm của rễ/nấm tắt ngoài ~60 m (đã có), tia sáng giữ nguyên.
- Cảm giác "mù" (slime chưa có mắt) vẫn giữ: tầm nhìn bị giới hạn bởi Sight như cũ, LOD chỉ quyết định vẽ gì khi nhìn thấy.
- Camera far clip 700 m; mục tiêu: chế độ ghost (F6) nhìn từ trên cao thấy cả bản đồ, FPS không thấp hơn khi đứng dưới đất quá ~20%. Người chơi kiểm bằng F4 (bảng hiệu năng).
- Thuộc **PR B** (khi dựng lại đồ trang trí theo quần xã); PR A thêm phần nước/địa hình.
