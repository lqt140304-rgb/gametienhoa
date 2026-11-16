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

## Câu hỏi cần chốt trước khi mở rộng

- **Vòng lặp chính:** một ván dài bao lâu? Khi nào thắng hay thua? Chết thì mất gì? Tiến trình có được lưu lại không?
- **Cân bằng ngẫu nhiên trong co-op:** Dị Năng yếu thì được bù bằng Thiên Phú mạnh hơn? Cho đổi lại 1 lần? Khiếm Khuyết có kèm một lợi ích nhỏ không?
- **Vai trò từng chỉ số:** đói, năng lượng và sinh khối khác nhau thế nào, mỗi chỉ số buộc người chơi đưa ra quyết định gì?
- **Giới hạn tổ hợp cơ quan:** số socket, loại cơ quan cho mỗi socket.

**Trạng thái:** tài liệu thiết kế; chưa có project Unity.
