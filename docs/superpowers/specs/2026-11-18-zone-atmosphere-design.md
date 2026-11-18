# Thiết kế: Không khí riêng cho từng khu (sương và hạt kiểu bioma Minecraft)

> Ngày: 2026-11-18 · Chủ dự án chọn hướng **A** (script trộn theo vị trí). Đây là thiết kế ngắn cho thay đổi nhỏ–vừa.
> Nhãn: **[ĐÃ CHỐT]** chủ dự án quyết; **[ĐỀ XUẤT]** Claude đề xuất, chờ duyệt; **[BUILDER]** người làm tự quyết.

## 1. Ý định

Mỗi khu trong 6 khu của hồ phải có **cảm giác hoàn toàn khác**, như đi qua các bioma đặc biệt trong Minecraft (rừng hoa anh đào, rừng gỗ nhạt): mỗi nơi có **lớp sương mù riêng và các hạt lơ lửng riêng** để người chơi cảm nhận được "mình đang ở khu nào" và thấy thế giới chân thực hơn. **[ĐÃ CHỐT]**

## 2. Mục tiêu, không mục tiêu, điều phải tránh

**Mục tiêu**
- Sương (màu, độ dày, tầm nhìn) trộn mượt theo vị trí giữa các khu.
- Một bộ hạt khác nhau cho mỗi khu, đi theo camera, mật độ theo mức ảnh hưởng của khu.
- Tương thích với hệ thống nhìn (`MonochromeVision`: mù thì tối và đen trắng) và với sương dưới nước (`UnderwaterTint`).

**Không mục tiêu**
- Không làm sương khối thật (volumetric) hay mảng sương sát đất (hướng C, để sau nếu cần).
- Không đổi ánh sáng chính, ambient, hậu xử lý hay `MonochromeVision`.
- Không làm hiệu ứng thời tiết theo thời gian (mưa, gió).

**Điều phải tránh**
- Sương theo khu làm mất hiệu ứng "chưa có mắt thì tối và chỉ thấy gần": sương khi mù vẫn phải gần như đen.
- Hạt làm tụt FPS (ngân sách bên dưới) hoặc che mất slime và thức ăn.
- Giật hình khi đi qua ranh giới giữa hai khu hoặc khi chìm xuống nước.

## 3. Ràng buộc

- Sương Unity là một giá trị chung (`RenderSettings`), không có Fog Volume trong URP 17: phải đổi bằng script mỗi frame.
- `MonochromeVision` ghi sương khi thị lực đổi; `UnderwaterTint` ghi đè khi camera dưới nước. Script mới chạy sau chúng (`LateUpdate`) và nhường khi đang dưới nước.
- Dùng `ZoneLayout.Influence(zone, x, z)` có sẵn để lấy mức ảnh hưởng (mượt, biên uốn lượn).
- Chỉ trong cảnh hồ (không đổi `Prototype_01`); thêm vào camera lúc chạy giống `UnderwaterTint` (không cần dựng lại cảnh).
- Agent không có Unity: kiểm bằng ảnh chủ dự án gửi lại.
- Không đụng model nhân vật, sinh vật, cơ quan.

## 4. Các hướng đã cân nhắc

| Hướng | Nội dung | Được | Mất |
|---|---|---|---|
| **A (chọn)** | Script trộn sương và hạt theo ảnh hưởng của khu tại vị trí người chơi | chuyển cảnh mượt, một chỗ chỉnh số, nhẹ | sương toàn màn hình, chưa có lớp sương sát đất |
| B | Hộp kích hoạt cố định mỗi khu | dễ làm | giật ở ranh giới, tự tính khi chồng nhau |
| C | Sương và hạt là vật thể đặt trong cảnh | có chiều sâu | nặng, không nhuộm cả màn hình |

## 5. Thiết kế **[ĐỀ XUẤT]**

### 5.1. Bảng bầu không khí (dữ liệu thuần, có test)

| Khu | Sương (màu, tầm nhìn khi có mắt) | Hạt | Cảm giác |
|---|---|---|---|
| 01 Vùng Sự Khởi Sinh | xanh ngọc nhạt, 160 m | giọt nước li ti và đốm sáng ngọc lơ lửng, bay chậm | trong lành, an toàn |
| 02 Lõi Rễ Cổ Đại | nâu ấm vàng, 70 m | bụi gỗ vàng rơi chậm trong tia sáng | cổ xưa, kín đáo |
| 03 Bãi Mùn Rễ | nâu cam đục, 55 m | mảnh lá mục và bào tử nâu rơi xuống | mục nát, ẩm |
| 04 Vườn Bào Tử | tím nhạt pha vàng, 90 m | bào tử tím và vàng bay lên, lấp lánh | huyền bí, mơ màng |
| 05 Khe Thủy Mạch | trắng xanh lạnh, 120 m | hơi nước và tia nước bắn gần nước | mát, ẩm, rì rào |
| 06 Rìa Săn Mồi | xám đỏ sẫm, 40 m | tro đen và đốm đỏ rất thưa, rơi chậm | căng thẳng, nguy hiểm |

Mỗi hàng là `AtmosphereProfile` (màu sương, tầm nhìn khi có mắt, loại hạt, màu hạt, mật độ, kích thước, tốc độ, hướng trôi). Màu và số là đề xuất ban đầu, chỉnh trong dữ liệu.

### 5.2. Trộn và áp dụng

- `ZoneAtmosphereMath` (thuần): trộn các hồ sơ theo trọng số = `Influence` của từng khu (chuẩn hóa; ngoài mọi khu thì dùng hồ sơ mặc định `Wild`): `Blend(float[] weights) -> AtmosphereProfile`; sương `FogEnd` nội suy; `ApplySight(float sight, ...)`: sương khi mù = đen (giữ như `MonochromeVision`), khi có mắt = màu khu; tầm nhìn nhân với `1 + max(0, sight − 1)` như hiện có.
- `ZoneAtmosphere : MonoBehaviour` (trên camera, thêm lúc chạy): `LateUpdate` đọc vị trí camera, lấy trọng số, trộn, đặt `RenderSettings.fogColor`, `fogMode = Linear`, `fogStartDistance`, `fogEndDistance`; **bỏ qua** khi camera dưới nước (`UnderwaterTint` đang ghi đè); đọc thị lực từ `MonochromeVision.Sight` (null thì coi là 1); trộn mượt theo thời gian (không nhảy khi dịch chuyển tức thì).
- Hạt: ba hệ hạt (rơi, bay lên, trôi ngang) đi theo camera trong hộp 30 m quanh camera; mỗi hệ có mật độ phát = mật độ hồ sơ trộn của kiểu đó × trọng số; **tối đa 150 hạt hiển thị cùng lúc** (chủ dự án: đừng nhiều hạt, gây lag); có công tắc tắt hạt (phím và biến) và chỉ phát hạt khi camera ở trong khu có hạt; vật liệu hạt URP Particles Unlit, trong mờ cộng hoặc alpha, tạo bằng code.
- Khi mù (chưa có mắt) hạt tắt dần (không thấy gì xa) trừ hạt rất gần.

### 5.3. Tích hợp

- `SlimeController` thêm `ZoneAtmosphere` vào camera như `UnderwaterTint` (chỉ khi cảnh có `ZoneLayout` và Terrain).
- Một dòng nhật ký (Debug.Log, một lần) khi bật.

## 6. Kiểm chứng

- Test EditMode cho `ZoneAtmosphereMath`: trọng số 0 thì dùng `Wild`; trộn đúng nội suy; tổng trọng số chuẩn hóa; sương khi mù là đen và khi có mắt là màu khu; tầm nhìn tăng khi `sight > 1`; mật độ hạt ngoài mọi khu bằng 0 hoặc thấp.
- Thủ công: đi qua từng khu và ranh giới, chụp ảnh; thử chưa có mắt và có mắt; thử lặn xuống hố sâu và nổi lên; đo FPS bằng `PerfOverlay`.

## 7. Ngân sách và rủi ro

- **≤ 150 hạt cùng lúc**, 3 hệ hạt, không bóng đổ, hạt lớn và chậm (ít mà rõ, không rải dày); mục tiêu FPS không giảm quá 3%.
- Rủi ro: hạt xuyên vật thể gần camera (chấp nhận); sương quá dày ở khu 06 làm khó chơi (giữ tầm nhìn ≥ 40 m).

## 8. Để lại cho người làm

- Chi tiết hình dạng hạt (texture sinh bằng code), tốc độ và hướng trôi cuối cùng, tên và vị trí file.
- Nếu cần, tách bảng bầu không khí thành ScriptableObject để chỉnh trong Inspector (hiện dữ liệu trong code).

## 9. Ghi chú sau phản hồi của chủ dự án (2026-11-18)

- Sương theo bảng đề xuất đã được chấp nhận (**[ĐÃ CHỐT]**); hạt phải ít để không lag: tối đa 150 hạt, có công tắc tắt.
