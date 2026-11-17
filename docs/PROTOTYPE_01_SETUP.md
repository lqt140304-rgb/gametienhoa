# Dựng scene Prototype_01

## Cách nhanh (khuyên dùng)

1. Mở project `gametienhoa` trong Unity (thanh tiêu đề phải ghi `gametienhoa`).
2. Trên thanh menu, bấm **Evolve → Build Prototype Scene**.
3. Đợi vài giây. Scene `Assets/_Project/Scenes/Prototype_01` được tạo và mở sẵn.
4. Bấm **Play**.

Công cụ tự tạo: mặt đất, slime (kèm mọi component), camera góc nhìn thứ ba, 20 cục thức ăn, hiệu ứng đen trắng và sương mù, giác quan rung, thanh chỉ số, Dị Năng mẫu, cùng các material, prefab và ScriptableObject trong `Assets/_Project`.

Chạy lại menu này bất cứ lúc nào để dựng lại scene từ đầu. Các asset đã có (material, prefab, cấu hình) được giữ nguyên, nên chỉnh thông số trong đó không bị mất.

Dựng xong, commit các file mới (scene, prefab, material, `.meta`) lên Git.

Từ bản 0.2, scene có thêm **con mồi** (màu hồng, tạo bởi `Prey Spawner`). Bắt được con mồi thì nó rơi ra một cơ quan; chạm vào cơ quan để gắn mầm lên slime. Mầm lớn bằng thức ăn: mỗi cơ quan cần `Growth Food Required` (mặc định 30), một nửa mỗi bữa ăn nuôi cơ quan.

### Bản 0.3: kẻ săn mồi, nhiệt độ, hoang mạc

- **Kẻ săn mồi** (màu tím sẫm, mắt đỏ): 2 con trong hoang mạc, 1 con ở góc tây bắc đồng bằng. Phát hiện trong 7 m (chỉ 2.8 m nếu bạn đứng yên), đuổi tốc độ 1.4, mệt sau 6 giây. Cắn 15 máu và hất văng. Có 40 máu; bị **Gai độc** đâm lại hoặc bị **lao vào** nhanh (Dị Năng Q, nảy) thì mất máu. Hạ được: +35 thức ăn và chắc chắn rơi cơ quan theo `DropTable_Predator` (bậc cao dễ ra hơn).
- **Nhiệt độ**: thân nhiệt dần theo nhiệt độ không khí. Đồng bằng 22°C, hoang mạc 50°C (vùng `Desert Zone`, phía đông, x > 25). Quá 35°C (+ khả năng chịu nóng của cơ quan) thì mất máu theo số độ vượt. Chỉnh trong `SurvivalConfigSO` (mục Temperature).
- **Cơ quan mới**: Màng chịu nhiệt (C, Da, +20°C chịu nóng), Gai độc (B, Lưng, phản 15 sát thương).

### Bậc cơ quan và tỉ lệ rơi

Cơ quan có bậc **C / B / A / S** (ô `Tier`), phát sáng theo màu bậc khi rơi ra. Con mồi rơi đồ theo `DropTable_Prey` trong `Assets/_Project/ScriptableObjects`:

- `Drop Chance`: tỉ lệ có rơi đồ (mặc định 60%).
- `Weight C/B/A/S`: trọng số từng bậc (mặc định 70 / 22 / 7 / 1).
- `Organs`: danh sách cơ quan có thể rơi. Mỗi cơ quan có thêm `Drop Weight` (hệ số hiếm riêng trong bậc).

Cơ quan bạn đã có sẽ không rơi lại. Xem phần trăm thực tế: chọn `DropTable_Prey`, bấm ⋮ góc phải Inspector → **Print drop rates**, kết quả hiện ở Console. Cơ quan là các file `Organ_*.asset` trong `Assets/_Project/ScriptableObjects`, chỉnh hiệu ứng ngay trong Inspector.

## Cách thủ công

Chỉ cần nếu muốn hiểu scene được ghép thế nào.

## 1. Mặt đất
- **GameObject → 3D Object → Plane**, scale (10, 1, 10).

## 2. Slime
1. **3D Object → Sphere**, đặt tên `Slime`, vị trí (0, 1, 0).
2. Xóa component *Mesh Renderer* và *Mesh Filter* trên `Slime` (giữ *Sphere Collider*).
3. Tạo một Sphere con, đặt tên `Visual`, xóa collider của nó. Đây là phần nhún/nảy.
4. Trên `Slime`, thêm: **Rigidbody** (Interpolate = Interpolate), **Survival Stats**, **Slime Controller**, **Ability Caster**, **Vibration Sense**.
5. Trong *Slime Controller*, kéo `Visual` vào ô **Visual**.

## 3. Camera
- Trên *Main Camera*, thêm **Third Person Camera**, kéo `Slime` vào **Target**.

## 4. Thức ăn
1. Tạo Sphere nhỏ (scale 0.5), thêm **Nutrient** (collider tự chuyển thành trigger).
2. Kéo vào thư mục `Prefabs` để tạo prefab, rồi rải 10–20 cái quanh map.

## 5. Thị giác đơn sắc
1. **GameObject → Volume → Global Volume**, bấm **New** để tạo profile.
2. Thêm **Monochrome Vision** vào GameObject đó.
3. Trên *Main Camera*, bật **Post Processing**.

## 6. Giác quan rung
- Tạo một **Particle System** dạng nổ (Looping tắt, Duration 0.5, Emission Burst 20 hạt), lưu thành prefab, kéo vào ô **Pulse Prefab** của *Vibration Sense*.

## 7. HUD và Dị Năng
1. Tạo GameObject rỗng `HUD`, thêm **Survival HUD**, kéo `Slime` vào ô **Stats**.
2. Trong cửa sổ Project, chuột phải **Create → Evolve → Abilities → Burst Dash** để tạo `BurstDashSO`, kéo vào ô **Random Pool** của *Ability Caster*.
3. (Tuỳ chọn) **Create → Evolve → Survival Config** để chỉnh tốc độ đói, hồi năng lượng…, kéo vào *Survival Stats*.

## Điều khiển
| Phím | Tác dụng |
|---|---|
| WASD / cần trái | Trườn |
| Shift | Chạy nhanh (tốn thêm thức ăn) |
| Space / nút A | Nảy (tốn năng lượng) |
| Q / nút X | Dùng Dị Năng |
| Giữ chuột phải + kéo | Xoay camera |
| Lăn chuột | Phóng to/thu nhỏ |
| V | Bật/tắt mắt (thử nghiệm: xem màu, nhìn xa) |
| H | Ẩn/hiện bảng hướng dẫn |
| E | Chọn khi chạm vào cơ quan: lắp / thay thế / hấp thụ |
| R | Hồi sinh khi chết (giữ nguyên cơ quan) |
| X | Bỏ qua cơ quan |
| G | Mở/đóng bảng Cơ thể; trong bảng bấm 1, 2… để tháo cơ quan (rơi ra đất) |

## Cần kiểm thử
- Đói chỉ giảm khi di chuyển (nhanh hơn khi chạy nước rút), đứng yên không giảm; khi đói = 0 thì mất máu; khi no thì hồi máu.
- Đứng yên thì hồi năng lượng; nảy và dùng Dị Năng thì tốn năng lượng.
- Chạm vào thức ăn thì nó co lại và thanh đói tăng; sau 20 giây thức ăn xuất hiện lại.
- Màn hình đen trắng, sương mù dày, nhìn xa khoảng 12 m.
