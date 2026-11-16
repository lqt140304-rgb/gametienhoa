# PROJECT EVOLVE

Dự án game **sinh tồn tiến hóa 3D Low Poly** theo hướng **co-op 4–8 người**, xây dựng bằng **Unity 6.6 + URP** và tạo model/animation bằng **Blender**.

## Ý tưởng đã thống nhất

- Mọi người chơi xuất phát từ cùng một slime nguyên sinh: không mắt, miệng, tay, chân hoặc hệ tiêu hóa hoàn chỉnh.
- Khi chưa có mắt, hình ảnh cảm nhận là đen trắng, tầm nhìn ngắn. Sinh vật chỉ có thể hấp thụ ít dưỡng chất qua màng cơ thể.
- Mỗi người nhận **1 Dị Năng chủ động ngẫu nhiên**, sau đó chọn **1 Thiên Phú bị động từ 3 lựa chọn** và **1 Khiếm Khuyết từ 3 lựa chọn**.
- Khiếm Khuyết **tồn tại vĩnh viễn**, có thể được bù đắp bằng cơ quan tiến hóa nhưng không thể bị xóa.
- Tiến hóa **mở**, không theo loài hay nhánh cố định. Cơ quan mới phải được tìm kiếm, khám phá, thu thập khi săn mồi hoặc đánh boss.
- Thức ăn, môi trường và kẻ thù tạo điều kiện và áp lực thích nghi; các vùng đầu tiên gồm đồng bằng, hoang mạc, băng tuyết và hang động.
- Chỉ số chính: máu, đói, năng lượng, kinh nghiệm tiến hóa, nhiệt độ. Đề xuất thêm độ ẩm, sinh khối và các chỉ số sinh học phụ.
- Hướng phát triển gồm săn mồi, sinh tồn, khám phá, xây tổ và phối hợp trong bầy. PvP chưa ưu tiên; chênh lệch sức mạnh giữa người chơi được chấp nhận.

## Kho Dị Năng dự kiến (v2.0)

| Bậc | Số lượng ứng viên | Định hướng |
|---|---:|---|
| C | 15 | Đại trà, tác dụng đơn giản |
| B | 12 | Đa dụng nhưng có giới hạn |
| A | 6 | Hiếm, định hình lối chơi |
| S | 4 | Cực hiếm, cơ chế sinh học đặc biệt |

Bậc SS và SSS để phát triển sau. Danh sách chi tiết **chưa được duyệt lần cuối**.

## Công nghệ

- **Unity 6.6** (đề xuất Editor `6000.6.9f1`, xác nhận khi tạo project)
- **Universal Render Pipeline (URP)**, C#, 3D Stylized Low Poly
- **Blender** cho model, rig, animation; Shader Graph cho shader gel
- **ScriptableObject** mô tả cơ quan và Dị Năng; prefab mô-đun, bones và blend shapes cho cơ thể biến đổi
- **Netcode for GameObjects** được cân nhắc cho co-op 4–8 người

## Prototype 0.1 (phạm vi thu gọn)

Mục tiêu: chứng minh "trườn, hấp thụ, sống sót" là vui, trước khi mở rộng.

1. Unity project mẫu Universal 3D, scene `Prototype_01`, **1 vùng: đồng bằng**.
2. Slime nguyên sinh: di chuyển trườn/nảy, camera góc nhìn thứ ba.
3. Thị giác đơn sắc, tầm nhìn ngắn **+ 1 giác quan thay thế** (cảm rung / khứu giác hiển thị bằng hạt) để giai đoạn đầu không gây bực bội.
4. **3 chỉ số:** máu, đói, năng lượng. EXP tiến hóa, nhiệt độ, độ ẩm, sinh khối để bản sau.
5. Hấp thụ dưỡng chất qua bề mặt; khoảng 5 Dị Năng bậc C để thử nghiệm.
6. **Kiến trúc sẵn sàng cho mạng ngay từ đầu:** dữ liệu trạng thái (chỉ số, cơ quan) tách khỏi phần hiển thị; dựng sẵn khung Netcode for GameObjects (host + 1 client) trong 0.1.
7. Bản 0.2: gắn cơ quan mô-đun qua **socket cố định** trên khung xương, chuyển động bằng procedural animation (IK).

## 5 Dị Năng bậc C cho Prototype 0.1 (đã chốt)

| Dị Năng | Vai trò | Tác dụng | Lên cấp khi (sau 0.1) | Code |
|---|---|---|---|---|
| Nảy Bật | Di chuyển | Bật mạnh theo hướng đang đi | Thoát kẻ thù / bật tới mồi | `BurstDashAbility` |
| Bám Dính | Săn mồi | Vài giây: dính chặt vật thể chạm vào, hấp thụ nhanh gấp bội | Giữ mồi tới khi hấp thụ xong | `StickyGripAbility` |
| Phun Dịch Nhầy | Tầm xa | Phun cục nhầy làm chậm mục tiêu | Trúng mục tiêu | `MucusSpitAbility` |
| Giả Xác | Phòng thủ | Nằm im, kẻ thù mất dấu; không hồi năng lượng | Kẻ thù bỏ đi | `PlayDeadAbility` |
| Rung Màng | Giác quan | Xung rung tầm rộng làm lộ mọi thứ xung quanh | Phát hiện mồi/kẻ thù chưa thấy | `MembranePulseAbility` |

0.1 chưa có mồi sống và kẻ thù: Bám Dính, Phun Dịch Nhầy và Giả Xác có sẵn khung (`Slowed`, `PlayingDead.IsHidden`) cho AI sau này dùng.

## Vòng lặp chính (đã chốt 2026-11-16)

Chi tiết và lý do: `docs/superpowers/specs/2026-11-16-core-loop-design.md`.

- **Sinh tồn thế giới mở**, không giới hạn thời gian, thế giới được lưu giữa các buổi chơi (kiểu Minecraft). Không có trùm cuối duy nhất.
- **Chết = làm lại từ đầu:** mất toàn bộ cơ quan, chỉ số, sinh khối. **Dị Năng, Thiên Phú, Khiếm Khuyết cố định theo người chơi trong mỗi thế giới** (chống spam tự sát để bốc lại).
- **Dị Năng:** tốn Năng lượng, có hồi chiêu; **mạnh lên khi dùng**, chỉ tính khi dùng có tác dụng. Đổi được bằng **vật phẩm rất hiếm** từ boss: bốc ngẫu nhiên, không thấp hơn bậc hiện tại, mất cấp tiến hóa cũ.
- **Boss:** nhiều boss, ít nhất một con mỗi vùng; không bắt buộc thứ tự. Rơi cơ quan độc nhất, **mỗi người tham gia quay tỉ lệ riêng**, có cơ chế chống xui; boss hồi sinh sau một thời gian.
- **Cơ quan:** tự mọc khi ăn/hấp thụ, người chơi chọn giữ hoặc bỏ; lớn dần **mầm → non → trưởng thành**, tiêu sinh khối, hoạt động một phần khi chưa trưởng thành; đang mọc thì năng lượng hồi chậm. Mọc vào vị trí cố định trên cơ thể, số vị trí tăng theo kích thước (nguyên sinh 2, trưởng thành 6–8).
- **Chỉ số:** Máu (sống/chết), Đói (áp lực liên tục), Năng lượng (áp lực trong trận), Sinh khối (vật liệu tiến hóa).
- **Tổ:** bản đầu đơn giản, là điểm hồi sinh + kho thức ăn dùng chung, còn lại khi chết, không cất cơ quan.
- **Xây dựng: để sau**, theo hướng **sinh học** (vật liệu tiết ra từ cơ quan, đặt theo lưới). Nền chuẩn bị từ đầu: lưu thế giới, tài nguyên sinh khối/dưỡng chất rõ ràng, trường "tiết vật liệu" trên SO cơ quan, địa hình không cần voxel.

## Lộ trình

1. **0.1** — trườn, hấp thụ, sống sót (phạm vi giữ nguyên như trên).
2. **0.2** — cơ quan mọc dần qua socket cố định, procedural animation.
3. **Sau đó** — boss và Dị Năng tiến hóa, lưu thế giới, tổ đơn giản.
4. **Sau nữa** — xây dựng sinh học.

## Câu hỏi còn mở

- ~~Vòng lặp chính, cái chết, lưu tiến trình~~ → đã chốt ở trên.
- ~~Cân bằng ngẫu nhiên~~ → Dị Năng tiến hóa được + đổi bằng vật phẩm hiếm.
- ~~Vai trò chỉ số~~, ~~giới hạn tổ hợp cơ quan~~ → đã chốt ở trên.
- Con số cụ thể: tỉ lệ rơi, thời gian hồi boss, tốc độ lớn của cơ quan.
- Danh sách vị trí và loại cơ quan cho từng vị trí.
- Mục tiêu dài hạn ngoài boss.

**Trạng thái:** tài liệu thiết kế; chưa có project Unity.
