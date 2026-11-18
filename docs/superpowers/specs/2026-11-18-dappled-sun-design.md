# Thiết kế: Ánh sáng lọt qua kẽ rễ (mặt trời đổ bóng của mái rễ)

> Ngày: 2026-11-18 · Chủ dự án chọn hướng **A** (bóng đổ thật của mái rễ). Thiết kế ngắn cho thay đổi nhỏ–vừa.
> Nhãn: **[ĐÃ CHỐT]** chủ dự án quyết; **[ĐỀ XUẤT]** Claude đề xuất, chờ duyệt.

## 1. Ý định

Làm cho cả bản đồ có cảm giác đứng **dưới tán rễ khổng lồ**: tối, ánh sáng chỉ lọt qua các kẽ rễ thành những mảng sáng trên đất (khớp đúng kẽ hở của mái và 9 lỗ sáng sẵn có), và có **lớp sương mỏng**. **[ĐÃ CHỐT]**

## 2. Mục tiêu, không mục tiêu, điều phải tránh

**Mục tiêu**
- Đèn chính (Directional) đổ bóng thật; mái rễ chặn sáng, chỉ chỗ có kẽ mới lọt sáng.
- Phần còn lại tối (ambient thấp, xanh lục tối).
- Mảng sáng trôi chậm (đèn lắc nhẹ) cho cảm giác sống.
- Sương mỏng: dùng sương theo khu đã có (`ZoneAtmosphere`), không thêm hệ mới.

**Không mục tiêu**
- Không đổi `MonochromeVision` (chưa có mắt vẫn đen trắng, tối, chỉ sáng quanh người), `ZoneAtmosphere`, hậu xử lý.
- Không làm sương khối thật hay tia sáng khối thật (đã có nón tia sáng của phiên trang trí).
- Không đổi ánh sáng của `Prototype_01`.

**Điều phải tránh**
- Tụt FPS đáng kể (đây là phần rủi ro nhất).
- Vùng xa ngoài tầm bóng đổ sáng trưng, không có mảng sáng.
- Bóng răng cưa, rung khi đèn lắc.
- Làm hỏng cảnh khi chưa có mái rễ (Prototype hoặc chưa dựng canopy).

## 3. Ràng buộc

- Mái rễ (`LakeCanopy`, các mảnh 64 m, hai mức chi tiết, cao khoảng 25 m) hiện **không đổ bóng** (`ShadowCastingMode.Off`); bóng đổ thật của đèn chính hiện chỉ trong 50 m (URP `PC_RPAsset`: 4 tầng, bản đồ 2048).
- `MonochromeVision` đọc cường độ đèn chính lúc chạy (`sunBase`) và nhân theo thị lực; công cụ chỉ đặt giá trị trong Editor, không viết đè lúc chạy.
- Các file `Editor/LakeCanopy.cs`, `LakeRootsLight.cs` thuộc phiên trang trí: không sửa; công cụ mới tự tìm renderer của mái theo tên và bật đổ bóng.
- Agent không có Unity: kiểm bằng ảnh và FPS chủ dự án gửi.

## 4. Các hướng đã cân nhắc

| Hướng | Nội dung | Được | Mất |
|---|---|---|---|
| **A (chọn)** | Mặt trời đổ bóng thật qua mái rễ | mảng sáng khớp kẽ rễ thật | nặng nhất (bản đồ bóng cho mái rộng) |
| B | Cookie (hoa văn kẽ rễ giả) | nhẹ | không khớp kẽ thật |
| C | Chỉ nâng cấp 9 tia sáng | nhẹ nhất | sáng chỉ ở 9 chỗ |

## 5. Thiết kế **[ĐỀ XUẤT]**

- Công cụ Editor `LakeSun.cs`, menu **Evolve → Dress Lake: Dappled Sun** (bước 17/17 của Rebuild Lake) và **Evolve → Restore Lake Sun (undo)** (sao lưu ở `Settings/LakeSunBackup.json`, chạy lại được). Chỉ chạy ở scene `Underroot_Lake`.
- Đèn chính: hướng gần thẳng đứng (cao độ khoảng 72°, hơi chéo), màu ấm nhạt, cường độ cao (khoảng 1,8) để mảng sáng nổi trên nền tối, đổ bóng mềm, độ lệch bóng chỉnh cho mái.
- Mái rễ: bật `ShadowCastingMode.On` cho mọi renderer dưới "Lake Canopy (Dressed)/Roof" (cả hai mức chi tiết).
- Ambient: chế độ Flat, màu xanh lục rất tối (khoảng 0,06, 0,08, 0,08); phản xạ giảm.
- URP: `shadowDistance` 120 m (từ 50) để mảng sáng còn ở xa, 4 tầng, soft shadows bật, bản đồ 2048; sao lưu và khôi phục mọi URP asset (như `LakeLook`).
- `DappledSun` (runtime, trên đèn chính): lắc góc quay ±2,5° quanh góc gốc, chu kỳ khoảng 70 s; **không đụng cường độ** (thuộc `MonochromeVision`).
- Sương mỏng: sương Linear theo khu đã có, không đổi.
- Phương án khi nặng: menu Restore; công tắc phím F9 bật tắt bóng đổ của mái lúc chạy (đặt `ShadowCastingMode` các mảnh mái và `QualitySettings.shadowDistance`).

## 6. Kiểm chứng

- Test EditMode cho phần thuần: hàm lắc `DappledSunMath.Angle(base, time, amplitude, period)` (biên độ và chu kỳ đúng, bằng góc gốc tại t = 0) và hằng số an toàn (cường độ, khoảng sáng).
- Thủ công: mảng sáng xuất hiện dưới các kẽ mái và 9 lỗ sáng, vùng khác tối; chưa có mắt vẫn tối và chỉ sáng quanh người; có mắt (V) thấy mảng sáng; đo FPS bằng `PerfOverlay` (mục tiêu giảm không quá 15%); F9 tắt bóng thì FPS hồi lại.

## 7. Rủi ro

- Nặng: nếu FPS tụt quá 15%, hạ `shadowDistance` còn 80 m hoặc giảm số tầng xuống 3, hoặc đổi sang hướng B (cookie).
- Vùng xa hơn 120 m không có bóng nên sáng đều (che bằng sương theo khu, tầm nhìn 40–160 m).
- Mảnh mái có hai mức chi tiết: bóng của mức xa thô hơn (chấp nhận).
