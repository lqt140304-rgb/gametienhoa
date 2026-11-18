> **ĐÃ LỖI THỜI (2026-11-18):** bản đồ 500 m đã do phiên trang trí làm xong trong `main`; xem `2026-11-17-lake-map-500m-design.md`.

# Bàn giao bản đồ 500 m (bước A đã chốt, mục 5–12 cho phiên trang trí)

> Ngày: 2026-11-17 · Nhãn: **[ĐÃ CHỐT]** chủ dự án quyết; **[ĐỀ XUẤT]** Claude đề xuất.
> Tên file giữ `map300` cho khỏi gãy liên kết cũ, nhưng nội dung là bản đồ **500 m**.
> Gửi cho phiên Claude làm trang trí (các file `Editor/Lake*.cs`). Phiên làm bước A (địa hình, nước, bơi) sẽ viết code theo plan `docs/superpowers/plans/2026-11-17-0.6d-map300-water.md`.

## 1. Quyết định đã chốt

1. Bản đồ **500 × 500 m**, mỗi khu khoảng **80 m**. Đi ngang khoảng 8 phút (1 m/s), đúng spec 6.9. **[ĐÃ CHỐT]**
2. Địa hình đọc từ bảng số liệu bên dưới: suối sâu 0,15 m, nước nông 0,4 m, hố 2 m. Bỏ lớp nền phẳng và các khối chặn cũ (hộp trigger nước, blocker hố sâu). **[ĐÃ CHỐT]**
3. Nước: tất cả về màu xanh ngọc cũ. Riêng **vũng đọng ở khu 03 nhuốm hơi vàng lục** để người chơi còn nhận ra nước bẩn (gây bệnh Nhiễm Nước Đọng). **[ĐÃ CHỐT]**
4. Bơi và lặn: nước sâu hơn 0,5 m thì slime bơi. Space nổi lên, Ctrl lặn xuống, tốc độ 65%. Nước nông vẫn lội như cũ. Bản đầu: không chết đuối, tốn năng lượng nhẹ. **[ĐÃ CHỐT]**
5. Mái lưới rễ cao khoảng 25 m, 8–10 tia sáng, rễ nhiều lớp quanh viền. **[ĐÃ CHỐT]**
6. Nền và thực vật theo từng khu. **[ĐÃ CHỐT]**
7–12. Điểm nhấn từng khu theo thứ tự 01 đến 06. **[ĐÃ CHỐT]**
13. Cập nhật tài liệu, chạy Evolve → Rebuild Lake (all steps) để kiểm tra (chủ dự án chạy trên máy mình). **[ĐÃ CHỐT]**

**Phân việc:** mục 1–4 (bảng số liệu, địa hình, màu nước, bơi) do phiên A làm. Mục 5–12 (mái rễ, nền, thực vật, điểm nhấn từng khu) do **phiên trang trí** làm. Chỉ một phiên nên sửa các file `Editor/Lake*.cs` tại một thời điểm.

## 2. Bảng số liệu bản đồ 500 m

Tọa độ (x, z) mét tính từ tâm khu (x đông, z bắc). Kích thước = rộng × sâu. Vị trí tương đối lấy từ concept rồi nhân 5/3 lên 500 m, khu chỉnh về khoảng 80 m. **[ĐỀ XUẤT, chờ chủ dự án duyệt từng số]**

| Khu | Tên | Tâm | Kích thước | Ghi chú |
|---|---|---|---|---|
| 01 | Vùng Sự Khởi Sinh | (-8, -120) | 80 × 70 | bờ nam của hồ, nơi xuất hiện |
| 02 | Lõi Rễ Cổ Đại (nơi trú) | (-150, -18) | 70 × 60 | bờ tây; lõi rễ cùng khe nứt hẹp |
| 03 | Bãi Mùn Rễ | (-125, 118) | 80 × 70 | tây bắc; khúc gỗ mục, vũng đọng vàng lục |
| 04 | Vườn Bào Tử | (137, 110) | 70 × 70 | đông bắc; nấm lớn |
| 05 | Khe Thủy Mạch | (173, -8) | 90 × 40 | đông; suối chảy, hai hố sâu |
| 06 | Rìa Săn Mồi | (13, 153) | 80 × 55 | bắc; cung rễ 4 × 8 m; cửa vào của B tại (17, 233) |

- **Hồ:** elip tâm (-8, -42), bán trục 110 × 80 (rộng 220 × 160 m), sâu 0,4 m, bờ mềm 6 m. Khu 01 và 02 nằm sát mép hồ.
- **Suối** (rộng 10 m, sâu 0,15 m): (213, 10) → (187, -7) → (158, -17) → (133, -25) → (103, -37), đổ vào bờ đông hồ.
- **Hố sâu** (bán kính 10 m, sâu 2 m): (193, 0) và (157, -20).
- **Vũng đọng khu 03:** tròn tâm (-120, 103), bán kính 6 m, sâu 0,3 m, nước bẩn.
- **Đường mòn:**
  - 01→02: (-8,-120), (-83,-100), (-133,-67), (-150,-18)
  - 02→03: (-150,-18), (-142,50), (-125,118)
  - 03→06: (-125,118), (-67,142), (13,153)
  - 06→04: (13,153), (75,142), (137,110)
  - 04→05: (137,110), (167,58), (173,8)
  - 05→01: (173,-8), (100,-117), (-8,-120)
  - lối của B: (17,233), (13,153)
- **Vách rìa:** từ ±175 m lên cao tối đa 20 m tại ±250 m (mái 25 m).
- Mực nước y = 0; địa hình khoét xuống từ đó.

## 3. Điều phiên trang trí cần biết

- Bước A **giữ** `ZoneLayout.Biomes` (`Pad[]`, các khu 02–06, bán kính = nửa cạnh lớn) và `ZoneLayout.Paths` (`Vector2[][]`) nên `Editor/LakeLayout.cs` (đọc bằng reflection) vẫn chạy. Dữ liệu mới (`ZoneLayout.Zones`, hồ, suối, hố) sẽ có thêm.
- Các object trong scene giữ tên cũ: "Vung Sinh Menh (Life Pool)", "Spawn Point n", "Den ...", "Region (...)". Sẽ **mất**: "Pool Water", "Shallow Water" (các hộp), "Deep Pocket*", "Humus Ground". `LakeWater.Dress` hiện tìm chúng để đặt mặt nước, nên cần sửa để đặt mặt nước theo `WaterMap` (hồ, suối, hố, vũng đọng); mặt nước có sẵn do bước A tạo ở `y = 0`.
- Mái vòm: bán kính 0,75 × 500 m, cao 25 m; vách rìa cao ≤ 20 m. Mái lưới rễ và các tia sáng 8–10 làm trên mái này.
- Mỗi khu có địa hình gồ ghề nhẹ (không còn nền phẳng); đặt vật theo `GroundHeight`, không giả định y = 0.
- Nước sâu (hố 2 m) có thể bơi: đừng đặt vật cản trong nước.

## 4. Rủi ro

- Không biên dịch và không thấy hình trong môi trường agent; trang trí phải kiểm bằng ảnh và Console chủ dự án gửi lại.
- Đổi bố cục có thể làm hỏng quần thể, đường mòn, đồng bộ co-op và các test đã có (bước A cập nhật test).
- Hiệu năng: nhiều rễ, nấm, tia sáng trên bản đồ 500 m; kiểm FPS sau mỗi bước.

## 5. Nhịp làm đề xuất

| Bước | Nội dung | Ai làm |
|---|---|---|
| A | Bảng số liệu, địa hình khoét nước, màu nước, bơi | phiên A |
| B | Mái lưới rễ 25 m, nền, thực vật theo khu | phiên trang trí |
| C | Khu 01, 02 | phiên trang trí |
| D | Khu 03, 04 | phiên trang trí |
| E | Khu 05, 06, rồi cập nhật tài liệu | phiên trang trí |

Mỗi bước là một PR riêng.
