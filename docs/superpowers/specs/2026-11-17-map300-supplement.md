# Bổ sung kế hoạch: bản đồ 300 m (12 mục của chủ dự án) — ý kiến của Claude

> Ngày: 2026-11-17 · Nhãn: **[ĐÃ CHỐT]** chủ dự án quyết; **[ĐỀ XUẤT]** Claude đề xuất, chờ duyệt.
> Đây là phần bổ sung, chưa phải plan triển khai. Khi chủ dự án chốt các câu hỏi ở mục 4, Claude viết plan cho từng bước (A, B, C...).

## 1. 12 mục cần làm (do chủ dự án giao)

1. Bảng số liệu bản đồ 300 m: vị trí 6 khu, hồ, suối, hai hố sâu, đường mòn.
2. Địa hình đọc từ bảng đó: suối sâu 0,15 m, nước nông 0,4 m, hố 2 m. Bỏ lớp nền phẳng và các khối chặn cũ.
3. Nước: tất cả về màu xanh ngọc cũ.
4. Bơi và lặn: nước sâu hơn 0,5 m thì slime bơi; Space nổi lên, Ctrl lặn xuống, tốc độ 65%; nước nông vẫn lội như cũ.
5. Mái lưới rễ cao khoảng 25 m, 8–10 tia sáng, rễ nhiều lớp quanh viền.
6. Nền và thực vật theo từng khu.
7–12. Điểm nhấn từng khu theo thứ tự 01 đến 06.
8'. Cập nhật tài liệu, chạy Rebuild Lake (all steps) để kiểm tra (**chủ dự án chạy trên máy mình**; Claude không chạy được Unity).

## 2. Ý kiến bổ sung

**2.1. Làm sau khi kiểm 0.5d và 0.5e.** Cả hai bản đã merge nhưng chưa biên dịch lần nào. Nên có kết quả Console và Test Runner trước khi làm lại bản đồ.

**2.2. 300 m thay 480 m là làm lại 0.6a–0.6c.**
- Phải sửa `WorldScale`, `TerrainShape`, `ZoneLayout`, mái vòm (`DomeMesh`), tường biên, vị trí quần thể (Region, Den), vị trí rải thực vật, và test `ZoneLayoutTests`, `TerrainShapeTests`, `VegetationScatterTests`, `DomeMeshTests`.
- Đi ngang khu mất khoảng 5 phút (300 m ÷ 1 m/s), không còn 8 phút như spec 6.9; spec cần sửa.
- Các khu trong concept chỉ 25–35 m. Trên 300 m mỗi khu chỉ bằng khoảng một phần mười bề ngang, giữa các khu sẽ trống. **[ĐỀ XUẤT]** nhân kích thước khu lên khoảng 1,5 lần so với bảng concept, hoặc chủ dự án tự ghi kích thước mới.

**2.3. Bảng số liệu (mục 1).** Dạng dữ liệu thuần (không phụ thuộc cảnh), một nguồn dùng chung cho địa hình, nước, đường mòn, dựng cảnh và test. Vị trí tương đối lấy từ concept (bắc là phía rìa săn mồi 06, hồ ở giữa và phía nam, khe nước 05 ở đông, nơi trú 02 ở tây, bãi mùn 03 tây bắc, vườn bào tử 04 đông bắc). Chủ dự án duyệt từng số trước khi làm bước sau.

**2.4. Địa hình khoét nước (mục 2).**
- Mực nước y = 0; địa hình tự khoét sâu xuống 0,15 m (suối), 0,4 m (nước nông), 2 m (hố).
- Bỏ các hộp trigger nước cũ thì thay bằng một hàm tra độ sâu nước theo tọa độ, lấy từ cùng bảng số liệu. Cảm biến nước, thức ăn, thú dùng chung hàm này. `Prototype_01` vẫn dùng trigger như cũ.
- Bỏ lớp nền phẳng: các thứ đang đứng trên y = 0 (thức ăn, tổ, quần thể) phải đặt theo `GroundHeight`.

**2.5. Màu nước (mục 3).** Nước đọng màu bùn là dấu hiệu để tránh bệnh Nhiễm Nước Đọng. **[ĐỀ XUẤT]** tất cả nước xanh ngọc, riêng một vũng đọng ở khu 03 nhuốm hơi vàng lục (hoặc có bọt tảo) để người chơi còn phân biệt được.

**2.6. Bơi và lặn (mục 4).**
- Ngưỡng bơi: độ sâu nước tại vị trí slime > 0,5 m (tính bằng mực nước − `GroundHeight`).
- Hiện Space là nhảy (`SlimeController.ReadHop`); khi đang bơi Space đổi thành nổi lên. Cần kiểm tra Ctrl chưa dùng cho việc khác.
- **[ĐỀ XUẤT]** bản đầu: không chết đuối, tốn năng lượng nhẹ khi bơi (số đặt trong `SurvivalConfig`), tốc độ 65%, camera không đổi. Chìm, ngạt, hiệu ứng dưới nước để bản sau.
- Co-op: vị trí y đã được gửi qua mạng nên người khác thấy bạn bơi; chưa cần đổi `PlayerNetState`.
- Thú D/C trong hố sâu và rung động trong nước: để bản sau.

**2.6b. Nơi trú (liên quan 0.5e).** Luật "chỉ đổi cơ thể ở nơi trú" đã bỏ; khu 02 (lõi rễ cổ đại) vẫn là nơi trú ẩn khỏi kẻ săn B.

**2.7. Mái rễ 25 m (mục 5).** Vách đá viền hiện cao 45 m (`TerrainShape.CliffHeight`), cao hơn mái; phải hạ xuống dưới 25 m. Số tia sáng tối đa hiện khoảng 12 theo công thức cũ, 8–10 tia là hợp lý. Rễ nhiều lớp quanh viền cần giới hạn số mesh để giữ FPS.

**2.8. Phân chia việc và nhịp làm [ĐỀ XUẤT].** Các file `Editor/Lake*.cs` do phiên Claude bên kia trang trí; chỉ một phiên nên giữ chúng tại một thời điểm.

| Bước | Nội dung | Kiểm bằng |
|---|---|---|
| A | Mục 1–4 (dữ liệu, địa hình, màu nước, bơi) | Test Runner + chơi thử |
| B | Mục 5–6 (mái rễ, nền, thực vật) | Rebuild Lake + ảnh |
| C | Khu 01, 02 | Rebuild + ảnh |
| D | Khu 03, 04 | Rebuild + ảnh |
| E | Khu 05, 06, rồi cập nhật tài liệu | Rebuild + ảnh |

Mỗi bước là một PR riêng, để nếu có lỗi thì chỉ cần sửa trong bước đó.

## 3. Rủi ro

- Không biên dịch và không thấy hình trong môi trường agent; bước B–E phải qua ảnh chủ dự án gửi lại.
- Làm lại bố cục có thể làm hỏng quần thể, đường mòn, đồng bộ co-op và các test đã có.
- Hiệu năng: nhiều rễ, nấm, tia sáng; kiểm FPS sau mỗi bước.

## 4. Câu hỏi cần chủ dự án chốt

1. Kích thước khu: giữ đúng bảng concept, hay nhân 1,5 lần (đề xuất)?
2. Mục 5–12 do phiên nào làm (phiên này hay phiên bên kia)?
3. Vũng nước đọng ở khu 03: nhuốm hơi vàng lục (đề xuất), hay xanh ngọc như các nước khác?
4. Bơi bản đầu: không chết đuối, tốn năng lượng nhẹ (đề xuất), hay có ngạt?

## 5. Quyết định đã chốt (2026-11-17)

1. Kích thước khu: **nhân 1,5 lần** so với bảng concept.
2. Mục 5–12 (mái lưới rễ, nền, thực vật, điểm nhấn): **phiên Claude bên kia** làm. Phiên này làm bước A (mục 1–4), plan: `docs/superpowers/plans/2026-11-17-0.6d-map300-water.md`.
3. Vũng nước đọng khu 03: **vàng lục**.
4. Bơi bản đầu: theo đề xuất (không chết đuối, tốn năng lượng nhẹ).
