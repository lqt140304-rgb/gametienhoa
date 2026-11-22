# Chim Hang Mẹ: biết bay, vỗ cánh có thu cánh, bộ chiêu (2026-11-22)

Code viết và kiểm bằng test thuần + biên dịch; **chưa chạy thử trong Unity** (xem "Cần thử" cuối trang).

## Model Dreadwing Carrion (thay model Meshy đầu tiên, đã lỗi)
Nguồn: glb rig tự động của Meshy (74 xương `Bone_nnn`, 17.578 đỉnh, cao 1,7 m, sải cánh 2,1 m, đứng thẳng, quay mặt -Y). `tools/blender/rig_bird_dreadwing.py` đổi tên đúng 15 xương rồi xuất `Models/Wildlife/Meshy/Creature_ChimHangMe_Dreadwing.fbx` (+ `_albedo.png`, `_mask.png`; glb không có bản đồ kim loại/nhám nên mask là nhám cố định):
`Root`=xương chậu, `Head`=cổ, `Tail1`, `Pinion_L1/L2/L3` = xương đòn / vai / khuỷu (L là +X, R là -X), `WingTip_L/R` = đầu bàn tay (chỗ lưỡi gió bắn ra), `Thigh_L/R` + `Shin_L/R` = hông + gối. Chân **không** đặt tên `Leg_*` để `CreatureLocomotion` không đi chân bằng trục sai; `BirdRigFx` tự đi (xoay luân phiên, gối nhấc chân) và tự thu (hông 60° + gối 55°). Không có xương hàm nên mỏ không há được.
Dựng prefab: menu `Evolve → Build Chim Hang Mẹ (Dreadwing) Prefab` (ghi đè `ChimHangMe (Meshy).prefab`, git giữ bản cũ). Vật liệu tự dựng khi Unity nhập FBX. Cỡ: prefab hang đã có collider 2,1×2,4 m nên vừa; `CreatureTuning` giới hạn hệ số cỡ của chim ở 1,3.
Đã kiểm trong Blender (nền, không Unity): trọng số hợp lệ, mọi xương đều làm mesh chuyển động, và các tư thế mô phỏng đúng công thức `WingRules`/`BirdRigFx` (nhịp lên gập khuỷu, nhịp xuống, xếp cánh, đi bộ, thu chân, lao). Biên độ vỗ chỉnh theo model này (vai ±22 ngoài 6 nền; xuống xa gấp 1,4; xếp cánh 35/70/55°) vì lớn hơn thì hai cánh bắt chéo trước người.

## Hình dáng (BirdRigFx)
Xương: `Root`, `Head`, `Beak`/`BeakTip`, `Pinion_L1-3`, `Pinion_R1-3`, `Leg_L`, `Leg_R`, `Tail1`. Mọi thứ do một `BirdPose` điều khiển (`Combat/Pure/BirdRules.cs`):
`Spread` (cánh khép → mở), `Beat` (sức vỗ), `Raise` (cánh nâng cao ↔ hạ), `Legs` (-1 duỗi móng ra trước, 0 buông, +1 thu gọn dưới bụng), `Pitch` (độ, + = cúi mũi xuống), `Beak`.

- **Vỗ cánh vừa vỗ vừa thu** (`WingRules`): vai vỗ xuống nhanh (xa gấp 1,4 lần) và lên chậm; trên đường lên **khớp giữa và khớp ngoài gập lại, kéo cánh về phía thân** (+38°/+52° sweep, rủ xuống 24°/32°) rồi bung ra ở đỉnh; đoạn ngoài trễ pha nên đầu cánh quất. Cánh khép (Spread 0) xếp 35/70/55°.
- **Cúi người**: xoay `Root` quanh trục phải của chim; đầu bù ngược 50% (nhìn thẳng hơn), đuôi bù 40%.
- **Thu chân**: xoay cùng chiều với "cúi mũi xuống" (chân đá ra sau). Rig cũ (`Leg_L/R`, một xương): tối đa 85°, duỗi ra trước 55°. Dreadwing (hông + gối): hông 60° + gối 55°, duỗi hông 40°.
- Hướng quay mỗi xương **đo lúc khởi động** (không giả định trục); `Debug.Log` một dòng `[BirdRigFx] ...` in dấu đo được và xương nào thiếu.
- Chim bay từ xa tới (cách người chơi > 11 m, đang chạy): cả model nâng lên 1,3-2,8 m (chỉ hình; thân vẫn đi trên đất) với cánh vỗ, chân thu; hạ xuống khi còn < 8 m.

## Bộ chiêu (MovesetSeed, `BirdMoves` điều khiển thân)
| Chiêu | Hình dạng | Cách chạy |
|---|---|---|
| Mổ | Lunge (gần, 0-3,2 m) | Mổ thường 14 sát thương, rồi **vỗ cánh lùi 6 m** (đánh nhanh rút lẹ) để người chơi lại nằm trong tầm các chiêu bay (cần 3-5 m trở lên); không thì chim đứng dính người chơi chỉ để mổ |
| Chém Gió | `WindSlash` (3-16 m, hồi 7 s, windup 1,1 s) | Lơ lửng, vỗ cánh 2 nhát; mỗi nhát bắn **2 lưỡi gió từ 2 đầu cánh** (10 sát thương/lưỡi, tối đa 40, 24 m/s, đẩy lùi; dùng `MonsterSpear`) |
| Bổ Nhào | `Swoop` (5-18 m, hồi 18 s, windup 2 s, lao 0,8 s, nghỉ thở 1,5 s) | Vỗ cánh bay vút lên 9 m (thấp hơn nếu trần hang thấp) và áp sát tới ~6 m, **vòng đỏ hiện ở 60% windup** chỗ nó sẽ đáp (có dự đoán 0,35 s), gập cánh bổ xuống nhanh dần, đáp 28 sát thương + hất tung bán kính 3 m |
| Quắp | `Talon` (4-11 m, hồi 26 s, windup 1,4 s, lao 0,8 s, nghỉ thở 1,5 s) | Bay thấp, móng duỗi ra, lao xuyên qua người chơi. **Trúng** (8 sát thương): xách người chơi lên 6,5 m trong 1,5 s → **tung lên trời** (12 m/s) → bay xuyên qua họ **4 lượt, mỗi lượt cào** (7 sát thương, mỗi lượt 0,65 s, hất họ lên lại để không rơi xuống) → hạ cánh. **Trượt**: hạ xuống, hở sườn (x1,5 sát thương) |

Quắp chỉ dùng được khi người chơi là local, còn sống, không bị chó cắn/bám ở chỗ khác (`HoundPin`, `LatchSession`) và có Rigidbody động. Bị choáng/giết giữa chừng: thả người chơi, rơi xuống đất (`BirdMoves.Abort`).

`CreatureCombat` chỉ thêm vài dòng: gắn `BirdMoves`, `scheduler.Allow`, `HoldRecover` khi đang xách/cào (như cơn mưa đá của golem), và gọi `bird.Tick(...)` mỗi bước vật lý. Lượt cào bay theo chuyển động con lắc (`TalonFsm.SwingOffset`) nên không có chỗ nhảy vị trí.

## Cần thử trong Unity (chưa ai làm)
1. Để Unity nhập `Creature_ChimHangMe_Dreadwing.fbx` (vật liệu tự dựng), chạy menu `Evolve → Build Chim Hang Mẹ (Dreadwing) Prefab`, rồi tạo chim từ menu thần F1 trong TestArena. Console phải in một dòng `[BirdRigFx] ...`; nếu `chân THIẾU`/`đầu thiếu` thì tên xương trong FBX khác.
2. Cánh: đứng yên (khép), đi (xoè nửa, rung nhẹ), đang chiến (vỗ + thu mỗi nhịp). Nếu cánh vỗ ngược chiều (xuống thay vì lên) hoặc thu sai hướng: sai dấu đo ở `MakeWing`.
3. Cúi/thu chân: Bổ Nhào (cúi ~60°, chân duỗi ra khi sắp chạm đất), bay tới từ xa (chân thu lên bụng). Nếu chân đá ra trước thay vì ra sau: đảo dấu `leg` trong `BirdRigFx.LateUpdate`.
4. Bổ Nhào: vòng đỏ có hiện, chim không xuyên trần/tường; Quắp: xách lên, tung, 4 lượt cào, hạ cánh; chim không kẹt trên không sau khi bị đánh choáng.
5. Mọi con số (độ cao, tốc độ tung, sát thương, thời gian) là tạm, chỉnh ở `MovesetSeed.cs` (số chiêu) và hằng số đầu `BirdMoves.cs`.

## Kiểm bằng gì
`tools/cstest` (19 test `BirdRulesTests`: cánh gập khi lên, đỉnh dốc, độ cao Bổ Nhào, máy trạng thái Quắp, tư thế) và biên dịch toàn bộ `Scripts/` bằng Roslyn của Unity.

## Sửa sau lần thử đầu trong Unity (2026-11-22, kiểm bằng Unity MCP)
- **Chim tự mất máu và slime "treo" trên không không bị tung**: collider capsule 2,1×2,4 m của chim là kinematic, bị di chuyển bằng `MovePosition`, nên chạm slime thì `Predator.Contact` tính là "bị húc ở tốc độ cao" (`ramDamage`) và chim chết sau vài lần; slime bị tung lên thì đập vào bụng collider đó, vận tốc bị triệt tiêu nên lơ lửng. `BirdMoves.IgnorePlayers` tắt va chạm chim-người chơi khi chim đang lao (giai đoạn Active, cả chuỗi Quắp, 0,5 s đầu sau Bổ Nhào/Quắp trượt) rồi bật lại. Đo lại: máu chim giữ nguyên 110 suốt chuỗi, slime bay lên ~11 m (v.y 11 → 6 → 0) rồi rơi theo trọng lực trong lúc bị cào.
- **Nhịp quá nhanh**: các chiêu nối nhau không nghỉ. Giờ windup/hồi dài hơn (xem bảng), cooldown dài hơn (Chém 7 s, Bổ 18 s, Quắp 26 s, Mổ 3,5 s), Bổ Nhào và Quắp có thêm 1,5 s thở, `CircleRules.IdleSeconds` cho chim 2 s: sau mỗi chiêu nó đi vòng quanh người chơi ~6,5 m rồi mới ra chiêu tiếp (đo: Quắp xong nghỉ 3,3 s rồi đi vòng). Khâu xách 1,5 s, tung 0,6 s, mỗi lượt cào 0,65 s, hạ cánh 1,2 s.
- Sát thương: lưỡi gió 15 → 10 (4 lưỡi ≤ 40); Quắp = 8 + 4×7 = 36.
- Đo dáng đứng khi đứng/đi vòng: chân chạm đất (điểm thấp nhất của mesh −2 cm). Khi người chơi cách > 11 m, chim "bay" thấp bằng cách nâng riêng model 2,8 m (collider vẫn dưới đất) và hạ xuống khi còn < 8 m.
