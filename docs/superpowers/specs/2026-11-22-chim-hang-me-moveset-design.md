# Chim Hang Mẹ: biết bay, vỗ cánh có thu cánh, bộ chiêu (2026-11-22)

Code viết và kiểm bằng test thuần + biên dịch; **chưa chạy thử trong Unity** (xem "Cần thử" cuối trang).

## Hình dáng (BirdRigFx)
Xương: `Root`, `Head`, `Beak`/`BeakTip`, `Pinion_L1-3`, `Pinion_R1-3`, `Leg_L`, `Leg_R`, `Tail1`. Mọi thứ do một `BirdPose` điều khiển (`Combat/Pure/BirdRules.cs`):
`Spread` (cánh khép → mở), `Beat` (sức vỗ), `Raise` (cánh nâng cao ↔ hạ), `Legs` (-1 duỗi móng ra trước, 0 buông, +1 thu gọn dưới bụng), `Pitch` (độ, + = cúi mũi xuống), `Beak`.

- **Vỗ cánh vừa vỗ vừa thu** (`WingRules`): vai vỗ xuống nhanh (xa gấp 1,7 lần) và lên chậm; trên đường lên **khuỷu và cổ tay gập lại, kéo cánh về phía thân** (khuỷu +38°/cổ tay +52° sweep, rủ xuống 24°/32°) rồi bung ra ở đỉnh; đoạn ngoài trễ pha nên đầu cánh quất. Thân/cánh khép (Spread 0) vẫn xếp như cũ.
- **Cúi người**: xoay `Root` quanh trục phải của chim; đầu bù ngược 50% (nhìn thẳng hơn), đuôi bù 40%.
- **Thu chân**: `Leg_L/R` xoay cùng chiều với "cúi mũi xuống" (chân đá ra sau): tối đa 85°; duỗi ra trước tối đa 55°.
- Hướng quay mỗi xương **đo lúc khởi động** (không giả định trục); `Debug.Log` một dòng `[BirdRigFx] ...` in dấu đo được và xương nào thiếu.
- Chim bay từ xa tới (cách người chơi > 11 m, đang chạy): cả model nâng lên 1,3-2,8 m (chỉ hình; thân vẫn đi trên đất) với cánh vỗ, chân thu; hạ xuống khi còn < 8 m.

## Bộ chiêu (MovesetSeed, `BirdMoves` điều khiển thân)
| Chiêu | Hình dạng | Cách chạy |
|---|---|---|
| Mổ | Lunge (gần, 0-3,2 m) | Mổ thường 14 sát thương, rồi **vỗ cánh lùi 6 m** (đánh nhanh rút lẹ) để người chơi lại nằm trong tầm các chiêu bay (cần 3-5 m trở lên); không thì chim đứng dính người chơi chỉ để mổ |
| Chém Gió | `WindSlash` (3-16 m, hồi 5 s) | Lơ lửng ~1,2 m, vỗ cánh 2 nhát; mỗi nhát bắn **2 lưỡi gió từ 2 đầu cánh** (15 sát thương/lưỡi, 24 m/s, đẩy lùi; dùng `MonsterSpear`) |
| Bổ Nhào | `Swoop` (5-18 m, hồi 14 s) | Vỗ cánh bay vút lên 9 m (thấp hơn nếu trần hang thấp) và áp sát tới ~6 m, **vòng đỏ hiện ở 60% windup** chỗ nó sẽ đáp (có dự đoán 0,35 s), gập cánh bổ xuống nhanh dần, đáp 28 sát thương + hất tung bán kính 3 m |
| Quắp | `Talon` (4-13 m, hồi 20 s) | Bay thấp, móng duỗi ra, lao xuyên qua người chơi. **Trúng** (8 sát thương): xách người chơi lên 6,5 m trong 1,1 s → **tung lên trời** (12 m/s) → bay xuyên qua họ **4 lượt, mỗi lượt cào** (7 sát thương, mỗi lượt hất họ lên lại để không rơi xuống) → hạ cánh. **Trượt**: hạ xuống, hở sườn (x1,5 sát thương) |

Quắp chỉ dùng được khi người chơi là local, còn sống, không bị chó cắn/bám ở chỗ khác (`HoundPin`, `LatchSession`) và có Rigidbody động. Bị choáng/giết giữa chừng: thả người chơi, rơi xuống đất (`BirdMoves.Abort`).

`CreatureCombat` chỉ thêm vài dòng: gắn `BirdMoves`, `scheduler.Allow`, `HoldRecover` khi đang xách/cào (như cơn mưa đá của golem), và gọi `bird.Tick(...)` mỗi bước vật lý. Lượt cào bay theo chuyển động con lắc (`TalonFsm.SwingOffset`) nên không có chỗ nhảy vị trí.

## Cần thử trong Unity (chưa ai làm)
1. Tạo chim từ menu thần F1 (hoặc menu `Build Chim Hang Mẹ (Meshy) Prefab` nếu chưa có prefab). Console phải in một dòng `[BirdRigFx] ...`; nếu `chân THIẾU`/`đầu thiếu` thì tên xương trong FBX khác.
2. Cánh: đứng yên (khép), đi (xoè nửa, rung nhẹ), đang chiến (vỗ + thu mỗi nhịp). Nếu cánh vỗ ngược chiều (xuống thay vì lên) hoặc thu sai hướng: sai dấu đo ở `MakeWing`.
3. Cúi/thu chân: Bổ Nhào (cúi ~60°, chân duỗi ra khi sắp chạm đất), bay tới từ xa (chân thu lên bụng). Nếu chân đá ra trước thay vì ra sau: đảo dấu `leg` trong `BirdRigFx.LateUpdate`.
4. Bổ Nhào: vòng đỏ có hiện, chim không xuyên trần/tường; Quắp: xách lên, tung, 4 lượt cào, hạ cánh; chim không kẹt trên không sau khi bị đánh choáng.
5. Mọi con số (độ cao, tốc độ tung, sát thương, thời gian) là tạm, chỉnh ở `MovesetSeed.cs` (số chiêu) và hằng số đầu `BirdMoves.cs`.

## Kiểm bằng gì
`tools/cstest` (19 test `BirdRulesTests`: cánh gập khi lên, đỉnh dốc, độ cao Bổ Nhào, máy trạng thái Quắp, tư thế) và biên dịch toàn bộ `Scripts/` bằng Roslyn của Unity.
