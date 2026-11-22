# Bọ Cạp Thạch Anh: bộ chiêu chuẩn, xương, clip Blender (2026-11-22)

Chủ dự án duyệt bộ chiêu, duyệt dùng clip Blender, chọn giữ mô hình Meshy hiện có (không tạo lại, không tốn credit Meshy). **Mọi thứ phía Unity chưa chạy thử** (môi trường này không có Unity): chỉ kiểm được bằng test thuần (`tools/cstest`, 526 test xanh), kiểm cú pháp Roslyn và render trong Blender. Danh sách thử: `docs/TEST_CHECKLIST_UNITY.md` mục H13.

## Bộ chiêu (số liệu tạm, chỉnh ở `MovesetSeed.cs`)
Mỗi chiêu có đủ **ba giai đoạn**: tụ lực (`Windup`), tung chiêu (`Active`), hậu tung chiêu (`Recover`). Mỗi giai đoạn có **clip riêng** (`Tell_<Anim>`, `Strike_<Anim>`, `Recover_<Anim>`), hiệu ứng và tiếng riêng.

| Vai trò | Chiêu (`Anim`) | Tụ lực | Tung chiêu | Hậu tung chiêu | Số |
|---|---|---|---|---|---|
| Đánh xa | **Phóng Gai Tinh Thể** (`Volley`, `Spears`) | Đuôi cong, ngòi tím sáng dần, hạt sáng quay quanh, tiếng ngân tăng | 3 gai bắn lần lượt, thân giật mỗi gai, tia lóe ở ngòi | Đuôi chùng, ngòi nguội | 5–16 m, báo 1,0 s, 9 sát thương/gai (≤ 27), 20 m/s, hồi 7 s |
| Nội tại | **Vỏ Pha Lê** | | | | −25% sát thương từ trước mặt (góc ±60°), không bị choáng |
| Thường 1 | **Châm Choáng** (`Sting`, `Lunge`) | Hạ thân, đuôi ngả sau, hai càng mở, ngòi sáng | Đâm ngòi xuống nhanh, tia tím + rung nhẹ | Ngòi cắm đất rồi kéo lên (hở sườn) | 0–3,2 m, báo 0,6 s, 18 sát thương, choáng người chơi 1 s, hồi 3,5 s |
| Thường 2 | **Giáng Đuôi** (`TailSlam`, `Spin`) | Dựng thân, đuôi thẳng lên run, vòng báo | Đập đuôi, sóng bụi, gai tinh thể bắn, màn hình rung | Đuôi kẹt đất, rút chậm, thở nặng | 0–3,6 m, báo 0,9 s, 26 sát thương, đẩy 6, hồi 4 s |
| Đặc biệt (câu đố né) | **Đào Chui Trồi Lên** (`Burrow`, `Dodge`) | Cào đất, lún dần, bụi xoáy (1,2 s) | Biến mất, **gò đất** đuổi theo người chơi (1,0 s), rồi **vòng đỏ khóa chỗ** (1,0 s, không đuổi nữa), rồi trồi lên nổ đất (0,35 s) | Rũ đất; trượt thì nằm hở (choáng nhẹ ≈ x1,5 sát thương) | 5–18 m, bán kính 4 m, 30 sát thương + hất tung, hồi 20 s |
| Combo (tùy chọn) | **Lao Kẹp** (`Pinch`, `Charge`) | Hai càng mở rộng, chân sau cào đất, hạ thấp | Chạy nhanh hai càng mở, khép lại kẹp | Trượt dừng, càng xòe; còn trong tầm thì nối **Châm Choáng** | 4–12 m, 20 sát thương, hồi 14 s |
| Tuyệt chiêu (máu ≤ 25%) | **Bùng Pha Lê** (`Burst`, `Shatter`) | Vươn cao trên chân thẳng, càng giơ, đuôi dựng, vỏ bừng sáng, rung tăng dần, ngân ~1,8 s | **3 vòng gai** lan ra (cách 0,6 s, 9 m/s, tới 10 m, mỗi vòng có **khe 70°** để lọt), rồi 4 gai rơi quanh người chơi | Đập đuôi xong vỏ nứt, gục thở dài ~3 s | 3 × 13 = 39 + gai rơi 8, hồi 45 s |

Khe của vòng đầu nằm **ngay chỗ người chơi đứng lúc bắt đầu** (bài học), hai vòng sau xoay 120°: đứng yên là sai từ vòng hai.
Bộ chiêu qua `BossKitTests` (2 thường, 1 xa từ 5 m, 1 đặc biệt `Dodge`, 1 tuyệt chiêu, 1 combo, nội tại).

## Logic thuần (`Combat/Pure/ScorpionRules.cs`, test `ScorpionRulesTests`)
- `ScorpionRules`: `ClipSpeed` 1,26 m/s (phải khớp `STRIDE` trong script Blender), `WalkAnimSpeed`, `PhaseAnimSpeed` (kéo clip giãn theo giây của giai đoạn), `LocalMove` (vận tốc thế giới sang MoveX/MoveY), `ShellAngle`, `FanAngle`.
- `BurrowRules`: đồng hồ `Dig/Travel/Marked/Emerge`, quãng gò đất chạy, khi nào vòng đỏ khóa. `ShatterRules`: vòng, khe, ai bị trúng.
- `PassiveRules.ShellFactor/StunImmune`, `MovesetSeed.AuthoredClips` (clip thật) và `PoseStrength` = 0 (không vẽ dáng giả).

## Xương và clip (`tools/blender/rig_scorpion.py`)
Chạy: `python -I tools/blender/rig_scorpion.py [in.fbx] [out.fbx] [--previews thư_mục]` (cần `bpy` 5.3, xem README). Mặc định đọc và **ghi đè** `Creature_BoCapThachAnh_Meshy.fbx` tại chỗ (bỏ xương cũ trước nên chạy lại được; bản tĩnh gốc còn trong git). Mô hình: 31.607 đỉnh, 1,9 × 1,9 × 1,43 m, quay mặt -Y, chân sát Z=0. Hình học (vị trí chân, đuôi, càng) đo trực tiếp từ mesh.
- 29 xương: `Root`; `Sting1..5`, `StingTip` (ngòi, chỗ ánh sáng tụ); `Pede{L,R}{1..4}_A/_B` (8 chân; tên không bắt đầu bằng Leg, Tail để `CreatureLocomotion` không chạy dáng đi giả đè lên clip); `Claw{L,R}_A/_B/_Finger`. Trọng số: 3 xương gần nhất, tính bán kính xương.
- **Bốn hướng**: `Walk_F`, `Walk_B`, `Walk_L`, `Walk_R` (vòng 20 khung, 30 fps). Hai nhóm chân bước luân phiên (chân 1 và 3 trái cùng chân 2 và 4 phải). Chân **IK số** bám đất tuyệt đối: chân đang chạm đất không trượt khi thân hạ, ngả, lao. Trái/phải tính theo chính con bọ cạp (trái = +X Blender).
- Tổng 25 clip: `Idle`, 4 `Walk_*`, `Hit`, `Death`, và 3 clip × 6 chiêu. Mọi clip tính từng khung (không key thưa).
- Kiểm trong Blender (đã chạy): sai số chân ≤ 3,3 cm, không clip nào đẩy chân xuống dưới đất (trừ Đào Chui, lún có chủ ý), FBX nhập lại đủ 25 take, ảnh render từng tư thế bằng `--previews`.
- Unity: menu **`Evolve → Build Bọ Cạp Thạch Anh (Meshy) Prefab`** (`MeshyScorpionPrefabBuilder`): cài rig Generic, đặt tên clip, loop cho Idle/Walk, dựng Animator (cây trộn 2D `Locomotion` theo `MoveX/MoveY`, 18 trạng thái chiêu vào bằng trigger `Tell_/Strike_/Recover_<Anim>`, `Hit`, `Die`, `Reset`), dựng prefab từ prefab hang `BoCapThachAnh` (giữ số liệu, bỏ `CreatureLocomotion` vì bước chân theo tên xương sẽ đánh nhau với clip). Tắt Play trước khi chạy.
- `ScorpionMoves` (thêm vào tự động khi `CreatureCombat` gặp `bocapthachanh`): bắn trigger theo pha và kéo giãn tốc độ clip theo giây của pha, ánh sáng ở ngòi (sáng khi tụ lực, lóe khi tung, tắt khi hậu), hạt, rung màn hình, tiếng; điều khiển `Burrow` và `Shatter` bằng tay.
- Điểm móc vào file chung (chỉ vài dòng): `CreatureCombat` (gắn `ScorpionMoves`, `OnPhase`, `Tick`, `ShellFactor`, miễn choáng ở `Stagger`), `Predator.TakeDamage` (nhân `ShellFactor(by)`), `CreatureStatus.Stun` (miễn choáng).

## Âm thanh và nhạc
- `tools/voice/generate_scorpion_sfx.sh` (làm theo `generate_golem_sfx.sh`): 19 tiếng vào `Resources/Sfx/Scorpion/` (mỗi giai đoạn của mỗi chiêu một tiếng, thêm `scorpion_hurt`, `scorpion_death`). **Đã tạo xong.**
- Nhạc `scorpion_battle` đã thêm vào `generate_boss_music.sh`, gọi bằng `BossMusic.Ping("scorpion_battle")`. **Chưa tạo được**: khóa ElevenLabs thiếu quyền `music_generation` (HTTP 401). Bật quyền đó trên khóa rồi chạy `ONLY=scorpion_battle tools/voice/generate_boss_music.sh`. Chưa có file thì Console chỉ cảnh báo, trận vẫn chạy không nhạc.

## Ý tưởng phòng boss riêng (chỉ là ý tưởng, chưa làm)
Phòng chưa có, nên chưa có mã. Các ý dưới dựa trên chính bộ chiêu để phòng "chơi cùng" boss:
1. **Nền đất mềm / nền đá cứng.** Giữa phòng là cát và đất vụn, rìa là đá nguyên. Đào Chui chỉ chạy được dưới nền mềm (gò đất dừng ở ranh giới): đứng trên đá thì tránh được vòng đỏ nhưng bị Phóng Gai và Lao Kẹp dồn. Móc vào: một hàm hỏi "chỗ này đào được không" thay cho chiều dài chạy của `BurrowRules.MoundDistance`.
2. **Cột đá che chắn.** Cột chặn gai bay (Phóng Gai, gai rơi của Bùng Pha Lê) và chặn đường Lao Kẹp: nó đâm vào cột thì kẹt càng ~2 s, hở sườn (không choáng vì Vỏ Pha Lê miễn choáng) và cột nứt dần rồi vỡ. Giáng Đuôi trúng cột thì đá rơi quanh. Cột vỡ hết thì mất chỗ nấp, nên phòng khó dần theo máu.
3. **Tinh thể tím ở tường.** Sáng lên khi Bọ Cạp tụ lực (ăn khớp với ngòi sáng, báo trước cho người chơi). Gai của Bùng Pha Lê để lại **tinh thể sàn** tồn tại vài giây làm vật cản tạm và làm khe thoát chật dần.
4. **Trần thạch nhũ.** Nhũ rơi khi Giáng Đuôi hoặc Bùng Pha Lê rung phòng; người chơi lợi dụng cho nhũ rơi trúng lưng boss (đòn từ sau không bị Vỏ Pha Lê giảm).
5. **Cửa chặn.** Khóa cửa lúc bắt đầu trận (như boss thường), mở khi boss chết.
Phòng nên cung cấp cho boss ba thứ: danh sách cột (vị trí, máu), mặt nạ "nền mềm", và điểm bắt đầu để dựng vòng khe của Bùng Pha Lê. Làm khi dựng phòng.

## Còn lại và rủi ro (chưa kiểm trong Unity)
- Tên clip sau khi nhập: Blender xuất take dạng `Armature|Armature|<tên>`; builder cắt phần sau dấu `|`. Nếu Console báo `Thiếu clip ...` chụp gửi lại danh sách trong ngoặc.
- Hướng trái/phải và bước chân: nếu đi ngang bị ngược hướng, đảo vị trí `Walk_L`/`Walk_R` trong cây trộn (`MeshyScorpionPrefabBuilder`).
- Đào Chui ẩn thân bằng cách tắt Renderer (mô hình cao 1,4 m, lún 0,8 m chưa đủ giấu); gò đất là quả cầu nâu dẹt tạm. Bùng Pha Lê dùng capsule tím làm gai. Cần thay bằng hiệu ứng đẹp hơn khi có thời gian.
- Phóng Gai dùng cơ chế `Spears` có sẵn (3 gai xoay trên lưng rồi phóng, của Cáo Gió); `FanAngle` (xòe quạt) đã có luật và test nhưng **chưa nối** vào lúc phóng.
- Số liệu (thời gian, sát thương, tầm) đều tạm.

## Sửa sau lần thử đầu trong Unity (2026-11-22)
- **Chân và càng đứng yên, boss "lết"**: `Predator.cs` tự thêm `CreatureLocomotion` cho mọi quái chưa có, và component đó chạy dáng đi giả cho mọi xương tên bắt đầu bằng `Leg`/`Tail`/`Wing`/`Limb` và đặt lại `animator.speed` mỗi khung, nên đè lên clip. Đã đổi tên xương (`Leg*`→`Pede*`, `Tail*`→`Sting*`, `TailTip`→`StingTip`), đổi trạng thái đi của Animator thành `Walk`, và `ScorpionMoves` chốt `animator.speed` ở `LateUpdate` (sau khi loco chạy).
- **Clip trùng tên** (`Recover_TailSlam` hai lần): chạy lại script trên FBX đã có xương thì clip cũ đi theo file và bị xuất lần nữa (50 clip). Script giờ nhập với `use_anim=False` và xóa hết action cũ trước khi dựng.

## Nhịp trận, chuỗi đòn, combo, môi trường (2026-11-22, chủ dự án duyệt: "làm hết, trồi kép khi máu ≤ 66%, đừng copy Golem, sáng tạo theo đặc điểm quái")
Lấy ý từ Golem nhưng đổi theo đặc điểm bọ cạp (đuôi, càng, chui đất, giáp pha lê), không sao chép:

| | Golem | Bọ Cạp (làm khác) |
|---|---|---|
| Nghỉ sau chiêu | khụy gối, thở hổn hển | **hở sườn, không thở**: xìu càng, đuôi rủ, thân thấp lắc nhẹ (clip `Exposed`, nhận x1,5). Châm Choáng 0,6 s, Giáng Đuôi 1,2 s, chuỗi Lao Kẹp 1,5 s, Đào Chui 1,5 s, Bùng Pha Lê 3 s. Phóng Gai không hở |
| Giữa hai chiêu | đi vòng 6,5 m | **đi ngang quanh người chơi** 2 s bằng clip đi 4 hướng (`CircleRules.IdleSeconds` = 2) |
| Chuỗi | Húc Đầu → 4 đòn thường | **Lao Kẹp → Châm Choáng → Giáng Đuôi** (ba đòn, còn trong tầm mới nối) rồi hở; **Giáng Đuôi** tự nối **Đập Đôi** |
| Chuỗi nhiều bước | nhổ cột ném | **Đào Chui, trồi kép** (máu ≤ 66%): trồi lần một, chìm lại 0,8 s, gò đất chạy 0,6 s, **vòng đỏ nhỏ hơn (3 m) khóa 0,7 s**, trồi lần hai (60% sát thương). **Bùng Pha Lê**: sau 3 vòng gai, **đuôi đâm ba cột tinh thể mọc trước mặt và hất từng cái vào người chơi** (clip `Flick`, 0,7 s/lần, 9 sát thương/cái) |
| Môi trường | cột đá tự nhổ, đâm tường choáng | **Phóng Gai cắm cột tinh thể** tại chỗ gai rơi (xòe quạt 24°, sau lưng người chơi, 5 s): chặn gai sau và chặn đường Lao Kẹp, người chơi núp được. **Lao Kẹp đâm cột/tường**: cột vỡ, càng **kẹt 1,2 s** (clip `Stuck`; bọ cạp miễn choáng nên chỉ hở sườn). `ScorpionArena.CanBurrowAt` cho phòng boss nói chỗ nào đào được |

- Code: luật thuần ở `ScorpionRules.cs` (`BurrowRules.Second*`, `ShatterRules.Flick*`, `ScorpionArena`, `ColumnDelay`), test `ScorpionRulesTests` (531 test xanh); `MovesetSeed` (Rest, Combo, Burst Active = `FlickEnd`); `CrystalColumn.cs` (cột, dựa `RockSpike`); `ScorpionMoves` (trồi kép giữ recover qua `Holding`, cột, quăng tinh thể, kẹt càng, hở sườn); `CreatureCombat` (+`Resting`, giữ recover, gọi `WallStuck` khi Lao Kẹp đụng tường).
- Blender: thêm 3 clip `Flick`, `Exposed`, `Stuck` (tổng 28 clip). Animator: ba trạng thái mới vào bằng trigger cùng tên; `Flick` vào lại được chính nó.
- Âm thanh: thêm `flick_stab`, `flick_throw`, `crystal_plant`, `crystal_shatter`, `claws_stuck`, `burrow_redive`, `exposed_hiss` (tổng 26 tiếng).
- **Chưa kiểm trong Unity.** Rủi ro: cột `CrystalColumn` có collider hộp, có thể kẹt người chơi; trồi kép dưới đất vẫn nhận sát thương (x1,5 vì đang trong recover) nếu bị đánh trúng; nếu `Flick` không thấy cột thì chỉnh khoảng 2,6 m phía trước trong `TickBurst`.

## Sửa: gai trên lưng bị kéo theo chân khi đi (2026-11-22)
Nguyên nhân: tôi đặt **khuỷu chân quá cao** (z ≈ 0,9) nên xương đùi xuyên qua vùng gai lưng, và trọng số lấy "3 xương gần nhất" nên gai trên lưng bị gán vào xương chân (đo: 2.556 trên 7.647 đỉnh lưng có trên 30% trọng số ngoài thân). Sửa trong `rig_scorpion.py`: khuỷu về z ≈ 0,58 (đo lại trên mesh), và mỗi xương chỉ ảnh hưởng đỉnh trong tầm gần nó (`REACH`: chân 0,17 m, đuôi 0,32 m, càng 0,55 m), phần còn lại thuộc `Root`. Kết quả đo: dịch chuyển của gai lưng khi đi giảm từ 0,27 m xuống ≤ 0,026 m (chỉ là cả thân nhấp nhô), chân vẫn bước.

## Clip 30 khóa và phòng boss (2026-11-22, theo yêu cầu chủ dự án)
- **Mọi clip đi và clip chiêu có đúng 30 khóa** (30 khung hình, 29/30 s ở tốc độ 1): bốn clip đi, `Tell_/Strike_/Recover_` của sáu chiêu (gồm hai đòn thường Châm Choáng và Giáng Đuôi) và `Flick`. Idle, Hit, Death, Exposed, Stuck giữ độ dài riêng. Hằng số `ScorpionRules.ClipKeys/ClipSeconds` (có test); `ScorpionMoves` kéo giãn clip theo giây của pha.
- Vòng đi dài 30 khung, bước 0,5 m: `ScorpionRules.ClipSpeed` = **1,0 m/s** (trước 1,26).
- **Chân cùng diễn:** `legwork` thêm động tác chân vào các clip chiêu: chân sau đẩy và chân trước nhấc khi tụ lực Châm Choáng, chân trước lao theo cú đâm, chân trước chống rộng khi dựng đuôi Giáng Đuôi, chân sau cào đất khi Lao Kẹp tụ lực, chân dang khi Phóng Gai và Bùng Pha Lê. Chân vẫn bám đất (sai số 1 mm).
- **Đặt vào phòng (PR #127, `BossRoom_Scorpion`):** menu `Evolve → Boss Rooms → Place Scorpion In The Open Boss Room` (`ScorpionBossRoomPlacer`, làm như bản Golem): mở cảnh `BossRoom_Scorpion`, dừng Play, chạy menu: đặt `BoCapThachAnh (Meshy)` tại `Boss_Wait`, gắn vào ô `Boss` của `BossRoom`, lưu cảnh. Khi nó thức dậy, `ScorpionRoom.Install` đọc các tảng đá `Prop_Pillar_*` của phòng và đặt `ScorpionArena.CanBurrowAt`: **gò đất của Đào Chui không đào xuyên qua tảng đá** (`ScorpionRoomRules`, chừa 1,2 m quanh tảng), nên người chơi núp sau đá thì tránh được Đào Chui; Lao Kẹp đâm tảng thì kẹt càng như đâm tường.
