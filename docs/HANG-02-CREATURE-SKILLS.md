# Hang 02 · AI, kỹ năng và VFX Vườn Quặng Cầu Vồng

## Phạm vi đã chốt

| Sinh vật | Hành vi |
| --- | --- |
| Đom Đóm Sắc Màu | Khi mối đe dọa vào tầm phòng thủ, bật Phát Sáng 10 giây, hồi 20 giây tính từ lúc bật. Ánh sáng trên thân tăng trong 0,18 giây và tắt mềm ở cuối. Giữ tác dụng gây mất dấu 1 giây cho kẻ săn trong 4 m của phòng thủ sẵn có. |
| Thỏ Lá | Lách ngang 2 m trong 0,35 giây, né sát thương và bắt nuốt trong khoảng này, hồi 6 giây. Thử hai bên, quét collider trước và trong khi lách. Khi cả hai bên bị chặn, thỏ giữ hành vi chạy và thử lại sau 0,2 giây. |
| Bọ Giáp Quặng | Vỏ Cứng giảm 15% sát thương ở mọi mức máu; hệ số này kết hợp với hệ số giáp/hồi sức hiện có của scheduler. Giữ Húc với báo đòn 0,9 giây, lao 2,3 giây, hồi sức 1,6 giây theo MovesetSeed. Húc đi thẳng và dừng/choáng khi đâm vách. |

Phạm vi gồm ba sinh vật thường đã được duyệt. Rùa Pha Lê được để cho đợt riêng.

## VFX và tích hợp

- Đom đóm dùng thuộc tính shader _Glow, đổi cường độ theo cửa sổ Phát Sáng thực tế và có nhịp thở sáng nhẹ. Kỹ năng không tạo đèn động.
- Thỏ có nhịp nhún lên, nghiêng người khi đổi hướng và vệt xanh lá ngắn. Vệt tắt khi lách xong hoặc bị vật cản chặn.
- Bọ giáp có ánh quặng nhấp nhẹ lúc nghỉ, tăng sáng khi báo Húc, đường chỉ hướng ngắn trước sừng và hiệu ứng vụn khi va vách. Đường cảnh báo tắt khi đòn chuyển pha hoặc bị gián đoạn.
- HangTwoPrey nối vào PreyDefense cho hai loài con mồi. Prey tự gắn phòng thủ theo tên loài, kiểm tra né trước khi nhận sát thương. CatchVeto chặn nuốt trong lúc né.
- Nội tại bọ giáp đi qua PassiveRules.DamageFactor. CreatureCombat.HangTwo.cs dùng pha của scheduler để điều khiển VFX.
- Renderers, MaterialPropertyBlock, TrailRenderer và LineRenderer được giữ để dùng lại. Mỗi lần dùng chiêu không tạo lại các vật liệu/vệt/đường này. Vụn va chạm dùng hệ thống SkillFx hiện có.
- Mô phỏng chạy trên host. Cần thử hai máy để xác nhận đường truyền thực tế; hệ thống client hiện dùng prefab đại diện nên VFX theo loài còn là phần tích hợp mạng tiếp theo.

## File chính

Scripts/Creatures/HangTwoPrey.cs, Scripts/Creatures/CreatureGlow.cs, Scripts/Creatures/Pure/HangTwoRules.cs, Scripts/Combat/CreatureCombat.HangTwo.cs, Tests/EditMode/Creatures/HangTwoRulesTests.cs và HangTwoUnityTests.cs, cùng các khối nối nhỏ trong PreyDefense, Prey, CreatureMotor, CreatureCombat, PreyDefenseSeed và PassiveRules.

## Chạy thử trong Unity 6000.6.9f1

1. Lấy đầy đủ file Git LFS. Chạy **Evolve → Build Wave 1 Creatures & Prefabs** nếu chưa có prefab cho ba loài.
2. Mở cảnh đang thử, vào Play Mode và dùng God Menu để tạo **Đom Đóm Sắc Màu**, **Thỏ Lá**, **Bọ Giáp Quặng**. Thử ở ngoài SafeZone.
3. Đom đóm. Tiếp cận trong tầm phòng thủ, theo dõi lúc bật, duy trì đến giây thứ 10 và tắt. Tiếp cận tiếp trước/sau giây thứ 20 để thử hồi chiêu.
4. Thỏ. Tiếp cận ở khoảng 3 m và nhìn cú lách ngang. Đặt vách ở một bên rồi cả hai bên, kiểm tra chọn bên còn trống và không xuyên vách. Thử gây sát thương trong nhịp né và sau khi hết né.
5. Bọ giáp. Gây cùng lượng sát thương ở trạng thái nghỉ, kiểm tra hệ số 0,85. Tạo khoảng cách 3–9 m để kích hoạt Húc. Đặt vách trên đường lao, kiểm tra dừng và choáng. Theo dõi đường chỉ hướng cùng pha báo đòn.
6. Lặp lại dưới vùng sáng và vùng tối. Kiểm tra khả năng nhìn thấy dấu hiệu trên thân, tránh tăng bloom đến mức mất chi tiết quặng/cánh.
7. Chạy tools/cstest/run.sh cho logic thuần. Trong Unity Test Runner chạy HangTwoRulesTests và nhóm hồi quy Hang 01, phòng thủ, bộ đòn, sinh thái, nuốt và gene.

## Kết quả kiểm thử

- 573/573 test logic thuần C# đạt qua tools/cstest/run.sh.
- 108/108 EditMode đạt trên Unity 6000.6.9f1. Bao gồm 12 ca thời gian/hồi chiêu/Vỏ Cứng, một ca shader xác nhận chỉ material slot phát quang đổi sáng và phục hồi đúng độ sáng nghỉ, cùng hồi quy Hang 01, sinh thái, nuốt và gene.
- Fixture Play Mode dùng ba prefab thật đã kiểm tra hai mức sáng. Log xác nhận đom đóm tắt sau 10 giây, thỏ né sát thương khi đang lách, hai lối bị chặn không tiêu hồi chiêu, mở một lối cho phép né ngay, và bọ giáp lao rồi dừng trước vách.
- Còn cần kiểm tra toàn cảnh Vườn Quặng Cầu Vồng và phiên co-op hai máy. Ảnh fixture được dùng để kiểm tra từng trạng thái riêng trong điều kiện ánh sáng kiểm soát.
