# Dựng scene Prototype_01

## Cách nhanh (khuyên dùng)

1. Mở project `gametienhoa` trong Unity (thanh tiêu đề phải ghi `gametienhoa`).
2. Trên thanh menu, bấm **Evolve → Build Prototype Scene**.
3. Đợi vài giây. Scene `Assets/_Project/Scenes/Prototype_01` được tạo và mở sẵn.
4. Bấm **Play**.

Công cụ tự tạo: mặt đất, slime (kèm mọi component), camera góc nhìn thứ ba, 20 cục thức ăn, hiệu ứng đen trắng và sương mù, giác quan rung, thanh chỉ số, Dị Năng mẫu, cùng các material, prefab và ScriptableObject trong `Assets/_Project`.

Chạy lại menu này bất cứ lúc nào để dựng lại scene từ đầu. Các asset đã có (material, prefab, cấu hình) được giữ nguyên, nên chỉnh thông số trong đó không bị mất.

Dựng xong, commit các file mới (scene, prefab, material, `.meta`) lên Git.

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
| Space / nút A | Nảy (tốn năng lượng) |
| Q / nút X | Dùng Dị Năng |
| Giữ chuột phải + kéo | Xoay camera |
| Lăn chuột | Phóng to/thu nhỏ |
| V | Bật/tắt mắt (thử nghiệm: xem màu, nhìn xa) |
| H | Ẩn/hiện bảng hướng dẫn |

## Cần kiểm thử
- Đói giảm dần; khi đói = 0 thì mất máu; khi no thì hồi máu.
- Đứng yên thì hồi năng lượng; nảy và dùng Dị Năng thì tốn năng lượng.
- Chạm vào thức ăn thì nó co lại và thanh đói tăng; sau 20 giây thức ăn xuất hiện lại.
- Màn hình đen trắng, sương mù dày, nhìn xa khoảng 12 m.
