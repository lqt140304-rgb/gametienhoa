# Hang 01 · Bàn giao AI và VFX

## Phạm vi

| Sinh vật | Triển khai |
| --- | --- |
| Bọ Cánh Cứng Nhỏ | Phát hiện trong 2 m, mở cánh 0,35 giây, lướt 2–3 m và hồi 8 giây. Quét collider trên tuyến bay, chấm điểm rêu và vật che ở điểm đáp. Giữ gene Da Dày +10 HP của slime. |
| Ốc Hồ | Thu mình 3 giây, nhận 50% sát thương, đứng yên và giữ cơ chế chặn bắt khi đóng vỏ. Hồi chiêu 12 giây. Khi mở vỏ nhận 125% sát thương trong 1 giây. Nền ẩm chống kéo và đẩy lùi. |
| Thằn Lằn Bờ Hồ | Lưỡi Dài 6 m, báo đòn 0,8 giây, hồi 8 giây, chỉ kéo mục tiêu nặng tối đa bằng nửa thân. Kiểm tra đường nhìn cả trước khi báo đòn và lúc chạm. Giữ Rình–Vọt. Đứng trên rêu đủ 3 giây giảm 40% bán kính phát hiện áp dụng cho nhận biết của sinh vật. |

Mốc hồi chiêu tính từ lúc kích hoạt. Dữ liệu nền lấy từ trọng số lớp Terrain Mud/Moss của Slime Cavern, với ngưỡng 50%. Màu texture và cường độ ánh sáng không tham gia quyết định. Host thực hiện AI, sát thương và kéo. Giao thức co-op hiện dùng prefab đại diện theo loại sinh vật và đồng bộ vị trí/HP, vì vậy client chưa thấy các VFX riêng của ba loài.

## File thay đổi

| File | Hạng mục |
| --- | --- |
| CreatureMotor.cs, PreyDefense.cs, PreyDefenseSeed.cs và Pure/PreyDefense.cs | Chọn và quét đường lướt, tải animation Fly/Retract, bụi và vệt bay, trạng thái thu mình và hậu hiệu lực. |
| HangOneGround.cs | Truy vấn alphamap và vật cản bằng bộ đệm dùng lại. |
| Prey.cs, Predator.cs, CreatureBrain.cs | Gắn hành vi cho đúng loài khi sinh ra, giữ vị trí rình trên rêu và đọc ngụy trang khi phát hiện. |
| CreatureStatus.cs, PullAbility.cs | Dùng chung điều kiện kháng kéo và đẩy, áp dụng cho cả kỹ năng sẵn có của slime. |
| CreatureCombat.cs, CreatureCombat.HangOne.cs | Lưỡi Dài, đổi sắc ngụy trang, hiệu ứng chạm và giữ đòn cơ bản. |
| HangOneSkillsTests.cs | Biên tầm và hồi chiêu, thời điểm đóng/mở vỏ, khối lượng, Terrain alphamap và vật cản. |

## Chạy thử trong Unity 6000.6.9f1

1. Chạy `git lfs pull` nếu checkout vừa được tạo. Nếu chưa có cảnh, chạy **Evolve → Build Slime Cavern (new map)**. Mở **Assets/_Project/Scenes/Slime_Cavern.unity** rồi chạy **Evolve → Build Maze World (Step 3)** nếu cảnh chưa có các đàn tại Hang Khởi Sinh. Để thử từng loài riêng bằng God Menu, có thể chỉ dựng prefab qua **Evolve → Build Wave 1 Creatures & Prefabs**.
2. Vào Play Mode, tìm ba sinh vật cạnh bờ hồ hoặc dùng God Menu để sinh mẫu. Khi thử thằn lằn với slime, đứng ngoài vùng an toàn quanh điểm xuất hiện. Có thể cho thằn lằn đói và đặt bọ làm con mồi.
3. Bọ. Tiếp cận từ ngoài 2 m và quan sát vỏ cánh, bụi nhẹ ở phần rêu và vệt bay. Đặt BoxCollider sau lưng bọ và kiểm tra nó đổi hướng hoặc ở lại khi tất cả lối đều bị chặn.
4. Ốc. Thử cùng một đòn trước, trong và sau khi đóng vỏ. Thử kéo trên lớp Mud chiếm đa số rồi lặp lại trên nền khô. Kiểm tra ốc có thể bị kéo trở lại sau khi mở vỏ trên nền khô.
5. Thằn lằn. Thử con mồi nhỏ, con mồi lớn và một vách chen giữa lúc báo đòn. Đặt thằn lằn ở nền Moss, để đứng 3 giây rồi cho di chuyển hoặc tấn công. Kiểm tra màu lưng và việc mất ngụy trang.
6. Lặp lại quan sát ở vùng sáng dưới tia nắng và vùng tối xa tia. Kiểm tra độ rõ của hướng lưỡi, cánh mở và chớp ở chân ốc. Dùng Frame Debugger hoặc Profiler nếu cần đo overdraw và cấp phát trên bản build đích.
7. Mở **Window → General → Test Runner → EditMode** và chạy **HangOneSkillsTests**, **PreyDefenseTests**, **MovesetSeedTests** và nhóm test nuốt/gene.
