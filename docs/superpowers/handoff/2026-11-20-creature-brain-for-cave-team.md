# Cho bên làm hang: `CreatureBrain` (sinh vật săn nhau, ăn xác, theo kích cỡ slime)

Thêm trên **mọi prefab con mồi (`Prey`) và kẻ săn / Chúa Hang (`Predator`)**: component `CreatureBrain` (`Scripts/Creatures/CreatureBrain.cs`). Không cần sửa code.

- **Tên prefab = tên hiển thị của loài** (`Thằn Lằn Bờ Hồ`, `Cóc Hồ Già`, `Giòi Trắng`…, đúng chính tả trong spec mục 4), hoặc điền ô `Species Id` (ví dụ `coc_ho_gia`). Bảng tên và id nằm ở `Scripts/Ecology/Pure/CaveEcologySeed.cs`. Không khớp thì não tự tắt và sinh vật chạy như cũ.
- Kẻ săn tự tìm mồi theo khẩu phần của loài (cùng hang): Cóc Hồ Già đuổi Thằn Lằn và Bọ Cánh Cứng, v.v. Nên **con mồi và kẻ săn cùng hang phải cùng có mặt** và đặt tên đúng.
- **Xác:** con mồi hoặc kẻ săn chết do sinh vật khác sẽ để lại một `Carcass` (có gene như xác thường); Giòi Trắng, Kền Kền Hang, Bọ Ăn Xác tìm đến ăn; **Chó Xương** (`Chó Xương`) canh xác lớn và đuổi mọi sinh vật lại gần, kể cả slime. Muốn có cảnh đẹp ở Nghĩa Địa Cự Thú thì đặt sẵn vài xác lớn.
- **Kích cỡ slime:** dựa vào `bodyMass` trên `Prey` / `Predator` (đã có ô này). Kẻ săn nhẹ hơn 1,2 lần khối slime thì săn slime, nặng hơn 1,8 lần thì tránh slime; con mồi chạy khỏi slime chỉ khi slime nuốt trọn được nó (khối con mồi ≤ 80% khối slime). **Hãy đặt `bodyMass` hợp lý cho từng loài** (con mồi nhỏ vài đơn vị, kẻ săn khoảng 15, Chúa Hang lớn hơn).
- **Bảng rơi (`drops`):** menu `Evolve → Attach Creature Brains` cũng tự gán bảng rơi gene (kỹ năng của loài) vào ô `drops` của 12 loài ba hang đầu (cần chạy `Evolve → Create Cave Skills` trước). Các loài khác để trống cho tới khi có kỹ năng của hang đó. Bạn không cần gán tay; chạy lại menu mỗi khi dựng lại prefab.
- **Công tắc:** menu thần (`Dev/GodMenu`) có ô bật tắt toàn bộ hành vi này để so sánh với cách cũ.
