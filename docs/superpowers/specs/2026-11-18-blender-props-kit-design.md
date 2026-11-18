# Thiết kế: Bộ vật thể rắn dựng bằng Blender cho thế giới ngầm (hướng A, kết hợp)

> Ngày: 2026-11-18 · Chủ dự án chọn hướng **A (kết hợp)**.
> Nhãn: **[ĐÃ CHỐT]** chủ dự án quyết; **[ĐỀ XUẤT]** Claude đề xuất; **[BUILDER]** người làm tự quyết.

## 1. Ý định và lý do

Chủ dự án muốn map **trông thật hơn**: khối hình chi tiết hơn, ánh sáng đẹp hơn, nước rõ ràng hơn, nguyên liệu (đá, rễ, nấm, gỗ) có **khối thật và va chạm thật** khi slime chạm vào, không còn là một lớp mỏng không vật lý. **[ĐÃ CHỐT]**

Hiện nay map dựng bằng code: Terrain sinh từ nhiễu, nấm, rễ, đá là các mesh nhỏ do script Editor sinh, không có va chạm.

## 2. Mục tiêu, không mục tiêu, điều phải tránh

**Mục tiêu**
- Một **bộ vật thể rắn** dựng bằng Blender (chạy không giao diện bằng `bpy`): đá, cung rễ, khúc gỗ mục, nấm lớn, vách rìa, rễ buông mái. Mỗi vật có mesh hiển thị và **khối va chạm đơn giản riêng**.
- Một script Editor đặt bộ vật thể theo **từng khu** trong bảng số liệu, tái dựng được, tất định theo hạt giống.
- Bộ ảnh xem trước do Blender render, để chủ dự án duyệt hình trước khi vào Unity.
- Phần Unity: chỉnh ánh sáng (sương, tia sáng, hậu xử lý) và nước (độ sâu, phản chiếu, bọt) để map đẹp hơn.

**Không mục tiêu**
- Không thay Terrain: địa hình, bơi lặn, `GroundHeight`, `WaterMap` và bảng số liệu giữ nguyên.
- Không dùng tài sản có sẵn từ bên ngoài hay vật thể sinh bằng AI (hướng C bị loại, chủ dự án không chọn).
- Không làm lại nhân vật, sinh vật, cơ quan.
- Không thay đổi cách đồng bộ co-op (vật thể là cảnh tĩnh, không cần đồng bộ).

**Điều phải tránh (thất bại dù chạy được)**
- Slime bị vướng, kẹt hoặc rung vào khối va chạm; đá chắn lối đường mòn hoặc chặn cửa nơi trú, khe chui vào nơi trú hẹp hơn 1,35 m.
- Mất FPS đáng kể khi mật độ vật thể tăng.
- Vật thể lơ lửng hoặc chìm khi địa hình đổi.
- Hình nhìn lộn xộn, thiếu phong cách (lẫn nhiều kiểu).

## 3. Ràng buộc

- Unity 6.6 URP; Blender chạy dạng thư viện Python (`bpy`) trong môi trường agent; **agent không có Unity** nên không thấy được kết quả trong game: kiểm bằng ảnh render Blender, còn hình trong Unity do chủ dự án chụp.
- Nhập vào Unity bằng **FBX** (Unity đọc FBX sẵn, không cần gói thêm). **[ĐỀ XUẤT]**
- Phụ thuộc bước 0.6d (bản đồ 500 m, bảng `ZoneLayout`) đã chốt trong `docs/superpowers/plans/2026-11-17-0.6d-map300-water.md`; việc đặt vật thể theo khu chỉ làm sau khi bước A đó vào `main`.
- Các file `Editor/Lake*.cs` đang thuộc phiên Claude trang trí (mục 5–12 của bản đồ); script đặt vật thể là **file mới** (`Editor/LakeProps.cs`) và phối hợp bằng một dòng thêm vào `LakeRebuildAll`.
- Giữ nguyên `PlayerNetState`, `NetWorld`, dòng netcode trong `Packages/manifest.json`; không `git add .`.

## 4. Các hướng đã cân nhắc

| Hướng | Nội dung | Được | Mất |
|---|---|---|---|
| **A (chọn)** | Giữ Terrain; Blender làm bộ vật thể rắn có va chạm; ánh sáng và nước chỉnh trong Unity | va chạm thật, sửa từng khu, ít rủi ro với `GroundHeight`, bơi, nước | địa hình vẫn do Terrain tạo |
| B | Cả nền đất là một mesh Blender | hình khối đẹp nhất | phải nướng lại độ cao cho `GroundHeight`, đổi bảng thì dựng lại, vòng sửa chậm khi không thấy Unity |
| C | A cộng vật thể lớn do AI sinh | nhanh và chi tiết | khó đồng bộ phong cách, nặng mặt, tốn lượt sinh |

Chủ dự án chọn **A**.

## 5. Thiết kế

### 5.1. Dây chuyền Blender → Unity

- `tools/blender/` chứa các script Python `bpy` (một script cho mỗi nhóm vật thể + một script chung `build_kit.py`). Chạy: `python -I tools/blender/build_kit.py --out Assets/_Project/Models/Underroot --previews docs/art/previews`.
- Mỗi vật thể xuất ra một FBX gồm: mesh hiển thị (`<tên>`), mesh va chạm đơn giản (`<tên>_COL`, vài chục tới vài trăm mặt), có thể thêm 2–3 mức LOD (`<tên>_LOD1`).
- Script `AssetPostprocessor` trong Unity (`Editor/PropImportPostprocessor.cs`) khi nhập FBX trong thư mục Models/Underroot: gắn `MeshCollider` (không lồi) từ mesh `_COL`, tắt renderer của nó, dựng `LODGroup` nếu có `_LOD1`, đặt cờ tĩnh.
- Vật liệu: bộ vật liệu URP Lit dùng chung theo nhóm (đá, rễ, gỗ, nấm), màu và độ nhám tinh chỉnh; ảnh bề mặt sinh bằng code hoặc dải màu đỉnh (vertex color) từ Blender để mỗi vật không cần ảnh riêng. **[BUILDER]**

### 5.2. Bộ vật thể ban đầu **[ĐỀ XUẤT]**

| Nhóm | Vật thể (số biến thể) | Khối va chạm |
|---|---|---|
| Đá | tảng nhỏ, vừa, lớn, đá có rêu (6) | mesh đơn giản gần lồi |
| Rễ | vòm rễ (cung rễ 4 m × 8 m), rễ bò thấp, gốc rễ, rễ buông mái (6) | vòm mở lối đi; rễ thấp rắn |
| Gỗ | khúc gỗ mục (đường kính 1,5 m, 3 biến thể) | hộp thân gỗ |
| Nấm | nấm lớn cao tới vài mét (4), cụm nấm nhỏ (3) | thân rắn, mũ không va chạm nếu cao hơn 2,5 m |
| Vách | mảng vách rìa ghép được (4) | mesh đơn giản |
| Mái | rễ lớn buông từ mái, tạo lưới rễ (3) | không va chạm (ở trên cao) |

Số mặt tham khảo: đá 400–2.000, rễ 1.500–6.000, nấm lớn 2.000–8.000, vách 3.000–10.000 (mỗi biến thể). Tổng cảnh ≤ 2.000 vật (dùng GPU instancing và LOD). **[BUILDER chốt số cuối khi đo FPS]**

### 5.3. Đặt theo khu

- `Editor/LakeProps.cs`, menu **Evolve → Dress Lake: Props**: đọc `ZoneLayout` (6 khu, hồ, suối, đường mòn), tìm độ cao bằng Terrain, đặt vật thể có hạt giống cố định, nằm trong một object cha `Lake Props (Dressed)`; chạy lại thay bản cũ. Không đặt trong nước sâu hơn 0,5 m, cách đường mòn ≥ 3 m (trừ gờ cố ý), cách nơi trú, tổ và điểm xuất hiện ≥ 6 m, giữ khe chui nơi trú 1,35 m.
- Mỗi khu có tính cách riêng theo concept: 01 bờ nông, đá bóng; 02 lõi rễ và khe hẹp; 03 gỗ mục, mùn; 04 nấm lớn; 05 đá trắng, nước chảy; 06 cung rễ và dấu vết thú săn.

### 5.4. Ánh sáng và nước (phần Unity) **[ĐỀ XUẤT]**

- Ánh sáng: sương mù thể tích giả (hạt, tia sáng có sẵn), hậu xử lý URP (Bloom nhẹ, Color Adjustments, Vignette), đèn phát sáng cho nấm, bóng đổ mềm; không dùng ánh sáng nướng nặng ở bước đầu.
- Nước: shader Graph đơn giản dùng độ sâu cảnh (mép mờ dần, màu đậm theo độ sâu), gợn sóng, phản chiếu bằng cubemap/Reflection Probe; giữ màu xanh ngọc đã chốt, vũng khu 03 vàng lục.

### 5.5. Kiểm chứng

- Mỗi nhóm vật thể: ảnh render Blender (3 góc nhìn) lưu ở `docs/art/previews/`, chủ dự án duyệt.
- Unity: Console không lỗi; slime đi qua đá, rễ, gỗ không kẹt; vào được nơi trú; FPS đo bằng `PerfOverlay` (đã có) sau mỗi nhóm khu.
- Test EditMode cho phần thuần (luật đặt vật: tránh nước, đường mòn, khoảng cách).

## 6. Nhịp làm đề xuất

| Bước | Nội dung |
|---|---|
| 0 | Chờ bước A của 0.6d (bảng số liệu 500 m, địa hình khoét nước) vào `main` |
| 1 | Dây chuyền Blender → FBX → Unity và một nhóm mẫu (đá) để chủ dự án duyệt hình và va chạm |
| 2 | Các nhóm còn lại (rễ, gỗ, nấm, vách, mái) |
| 3 | Script đặt theo khu, khu 01–02 |
| 4 | Khu 03–06 |
| 5 | Ánh sáng và nước trong Unity |

Mỗi bước là một PR riêng.

## 7. Để lại cho người làm

- Chi tiết dựng mesh trong Blender (modifier, nhiễu, bevel), số mặt cuối, số biến thể.
- Cách tạo màu và độ nhám bề mặt (vertex color, ảnh sinh code).
- Ngưỡng khoảng cách đặt vật và mật độ từng khu sau khi đo FPS.

## 8. Các quyết định Claude tự đặt sau khi người làm đọc thử (chủ dự án kiểm)

1. **Phong cách [ĐỀ XUẤT]:** bán thực, vẽ tay kiểu fantasy huyền bí và phát quang như các ảnh concept chủ dự án đã gửi (khu 03–06 và bản đồ), không phải ảnh chụp thật. Khối dày, có vết rêu và mốc, các mảng sáng ở nấm và rêu.
2. **Thú không va chạm với vật thể [ĐỀ XUẤT]:** `Prey` và `Predator` di chuyển kinematic theo `GroundHeight.Follow` nên sẽ xuyên đá, gỗ. Ở bản đầu chấp nhận; né vật cản cho thú là việc riêng sau này. Slime và va chạm với vật thể là có thật.
3. **Slime đi vòng, không leo lên vật lớn [ĐỀ XUẤT]:** vật lớn dựng vách dốc, không thành bậc; đá nhỏ dưới 0,3 m là bậc bước qua được. Khe và vòm trên lối đi rộng ít nhất 2,2 m (đủ cho slime cỡ tối đa, `BodyScale` 1,5, đường kính khoảng 1,5 m); khe nơi trú vẫn 1,35 m.
3b. Kiểm "không kẹt": slime to nhất đi qua mọi vòm và khe trên đường mòn; đá không chắn lối đường mòn.
4. **Nhịp duyệt [ĐỀ XUẤT]:** duyệt ảnh render và va chạm sau bước 1 (nhóm đá). Sau đó các nhóm còn lại làm theo lô, chủ dự án duyệt theo lô, sửa sau.
