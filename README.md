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

## Prototype 0.1

1. Tạo Unity project mẫu Universal 3D, scene `Prototype_01`.
2. Slime nguyên sinh có lõi sống, di chuyển trườn/nảy, camera góc nhìn thứ ba.
3. Thị giác đơn sắc, tầm quan sát ngắn.
4. Thanh máu, đói, năng lượng, EXP tiến hóa và nhiệt độ.
5. Hấp thụ dưỡng chất qua bề mặt; kiểm thử hệ thống sinh tồn.
6. Tiếp theo thử nghiệm gắn cơ quan mô-đun rồi mới mở rộng multiplayer.

**Trạng thái:** tài liệu thiết kế và chuẩn bị prototype; chưa có game hoàn chỉnh.
