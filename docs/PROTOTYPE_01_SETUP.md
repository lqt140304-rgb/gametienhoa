# Dựng scene Prototype_01

Code nằm trong `Assets/_Project/Scripts`. Khi tạo project Unity (bước 4 trong README), copy `Assets/`, `Packages/`, `ProjectSettings/` vào repo. Thư mục `Assets/_Project` đã có sẵn sẽ được giữ nguyên.

> Code chưa được biên dịch trong Unity. Nếu Console báo lỗi, gửi nội dung lỗi để sửa.

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
2. Trong cửa sổ Project, chuột phải **Create → Evolve → Abilities** và tạo đủ 5 Dị Năng bậc C: **Burst Dash** (Nảy Bật), **Sticky Grip** (Bám Dính), **Mucus Spit** (Phun Dịch Nhầy), **Play Dead** (Giả Xác), **Membrane Pulse** (Rung Màng). Kéo cả 5 vào ô **Random Pool** của *Ability Caster*. Rung Màng cần slime có *Vibration Sense* với Pulse Prefab đã gán.
3. (Tuỳ chọn) **Create → Evolve → Survival Config** để chỉnh tốc độ đói, hồi năng lượng…, kéo vào *Survival Stats*.

## Mồi sống và kẻ thù
1. **Mồi:** tạo một Sphere nhỏ (scale 0.4), thêm **Rigidbody** và **Prey** (tự thêm *Creature Motor*). Collider **không** đánh dấu Is Trigger. Mặc định Run Speed 4, nhanh hơn slime (2.5), nên phải dùng Dị Năng mới bắt được. Đặt khoảng 5 con.
2. **Kẻ thù:** tạo một Capsule (màu đỏ), thêm **Rigidbody** và **Predator**. Đặt Run Speed của *Creature Motor* khoảng 3, chậm hơn slime khi Nảy Bật nhưng nhanh hơn khi trườn. Đặt 1–2 con cách điểm xuất phát một quãng.

## Điều khiển
| Phím | Tác dụng |
|---|---|
| WASD / cần trái | Trườn |
| Space / nút A | Nảy (tốn năng lượng) |
| Q / nút X | Dùng Dị Năng |
| Giữ chuột phải + kéo | Xoay camera |
| Lăn chuột | Phóng to/thu nhỏ |

## Cần kiểm thử
- Đói giảm dần; khi đói = 0 thì mất máu; khi no thì hồi máu.
- Đứng yên thì hồi năng lượng; nảy và dùng Dị Năng thì tốn năng lượng.
- Chạm vào thức ăn thì nó co lại và thanh đói tăng; sau 20 giây thức ăn xuất hiện lại.
- Màn hình đen trắng, sương mù dày, nhìn xa khoảng 12 m.
- Mồi bỏ chạy khi slime tới gần; chạm vào thì mồi teo lại, thanh đói tăng; hút cạn thì mồi biến mất và xuất hiện lại sau 30 giây ở chỗ cũ.
- Bám Dính: chạm mồi thì dính chặt và hút nhanh hơn. Phun Dịch Nhầy: trúng mồi/kẻ thù thì nó chậm lại 3 giây.
- Kẻ thù đuổi theo trong khoảng 10 m, cắn mất 15 máu mỗi 1,5 giây, bỏ cuộc khi slime cách xa hơn 16 m.
- Giả Xác: kẻ thù bỏ đi, không cắn; mồi không bỏ chạy.
- Rung Màng: làm lộ cả mồi và kẻ thù.
