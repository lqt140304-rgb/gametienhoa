# Bộ chiêu của Chó Xương (đã chốt với chủ dự án 2026-11-21)

Chúa Hang Nghĩa Địa Cự Thú, mô hình Meshy gắn xương từ metarig của chủ dự án (`tools/blender/rig_from_metarig.py`). Loài dựa trên đời thực: thú săn đuổi bền như linh cẩu và sói (đuổi, vồ, cắn giữ, hú). Số là số tạm, chỉnh khi chơi.

| Chiêu | Dữ liệu (`MovesetSeed`, khóa `choxuong`) | Mở khi |
|---|---|---|
| **Chạy đuổi** | không phải chiêu: tốc độ đi 1,1, đuổi 3,6 (số của prefab); thân uốn theo sải chạy (`ChoXuongRigFx`) | luôn |
| **Vồ** (`Pounce`) | Lunge, báo 0,9 s, lao 5 m trong 0,6 s, hồi 1,8 s (nặng và chậm kiểu quái vật), sát thương 24, tầm 2,5–7 m, hồi chiêu 4,5 s. Lúc lao **bốn chân duỗi thẳng: chân trước vươn ra trước, chân sau thẳng ra sau** (`ChoXuongRigFx`, không chạy chân). **Vồ hụt thì chó ngất: lăn ra nằm nghiêng 2,2 s** (`CreatureCombat.Faint`, mở ra thế bị đánh gấp rưỡi). Khi cắn chặt, hai chân trước ép lên con mồi. **Đầu trúng mục tiêu (trong 60° phía trước) thì cắn chặt: mini game ấn xen kẽ A và D để thoát** (`PinGame`, `HoundPin`): cắn liên tục 6 sát thương/giây, mỗi lần ấn đúng +0,09, thanh tự tụt 0,18/giây, thoát khi đầy thì chó bị hất lùi và loạng choạng 1,2 s; sau 8 s chó tự nhả | luôn |
| **Cắn Nghiền** (`Bite`) | Lunge, báo 0,35 s, đớp 0,2 s, hồi 1,5 s, sát thương 20, tầm 0–2,5 m, hồi chiêu 3 s | luôn |
| **Cào Xé** (`Claw`, thêm theo gợi ý chủ dự án) | Lunge ngắn, báo 0,55 s (vươn đầu, giơ chân trước lên), vung 0,25 s, hồi 1,2 s, sát thương 16, tầm 0–3,5 m, hồi chiêu 5 s; chân trước trái rồi phải luân phiên | luôn |
| **Nhảy Vượt** (`Leap`) | Perch (như Khỉ Hang nhảy bục), xa 6 m, cao 0,3–1,5 m, đứng trên bục 1,5 s, hồi chiêu 15 s | luôn (cần có bục) |
| **Gầm** (`Roar`, kỹ năng Gầm) | mới `MoveShape.Roar`: báo 0,9 s, bán kính 8 m, hồi chiêu 28 s. Loài nhỏ hơn bỏ chạy 4 s (`FearSeconds`); **slime nhỏ hơn bị đẩy lùi (Force 8) và choáng 0,8 s (`Freeze`)** | máu ≤ 66% |
| **Triệu Hồi** (`Summon`, trước là Hú Gọi Bầy) | báo 1,4 s, hồi chiêu 30 s: **3 con Chó Xương nhỏ chui từ đất lên quanh nó** (cùng mô hình, nhỏ một nửa, 20% máu, cắn 40%, chỉ có Cắn và Cào, 1,3 s trồi lên, ở 25 s rồi chìm xuống; chết theo trùm; tối đa 6 con) (`HoundMinion`) | máu ≤ 66% |
| **Trúng đòn** | giật đầu 0,25 s (`HoundRules.Flinch`); đòn ≥ 12% máu tối đa thì **loạng choạng 0,6 s** (`CreatureCombat.Stagger`: dừng đòn đang ra, đứng im, lúc đó nhận ×1,5 sát thương như pha Recover) | luôn |

## Cách làm
- Dữ liệu chiêu và tư thế là **mã thuần có test**: `Combat/Pure/MovesetSeed.cs`, `Combat/Pure/HoundPose.cs` (`HoundRules`: góc cổ và hàm theo từng chiêu và pha), test `MovesetSeedTests`, `HoundRulesTests`.
- Không có clip: `Combat/ChoXuongRigFx.cs` xoay xương `spine.009-.011` (cổ, nâng hoặc hạ đầu), `jaw` (há, giới hạn 24°; xương nhỏ `JawTip` cho Unity biết chiều há), `spine.007` (lưng uốn khi chạy). `CreatureLocomotion` vẫn lo chân (`Leg_*`), `Root`, `Head`, đuôi `Tail1-5`.
- Tư thế thân (cúi, vươn) vẫn do `TellPose` của `CreatureCombat` (nén thân khi vồ, vươn khi gầm).
- Gầm xử lý trong `CreatureCombat.Roar`.

## Chỉnh sau lần thử đầu (2026-11-21)
- Animation bị giật và quá nhanh: (1) cổ dùng chung một trục cho cả ba xương, nay mỗi xương có trục riêng; (2) báo trước của Chó Xương chậm đi 1,5 lần (`ChoXuongRigFx.TellSlowdown`); (3) run còn 22 Hz biên 2° (trước 60 Hz); (4) tư thế nén thân của `TellPose` còn 40% (`MovesetSeed.PoseStrength`), vì xương đã tự cử động; (5) xương làm mượt chậm hơn (9/giây).
- Cỡ mô hình nhỏ đi 30%, thêm 10%, thêm 20% (prefab: mô hình và collider ×0,5).
- Thời gian hồi chiêu dài hơn (trước quá nhanh): Vồ 4,5 s, Cắn 3 s, Cào 5 s, Nhảy 15 s, Gầm 28 s, Triệu Hồi 30 s.
- Chân bước tự nhiên hơn: đầu gối gập theo nhịp vung hông (`ChoXuongRigFx`, từ vận tốc góc của xương hông).
- Trọng số chân: xương chân chỉ kéo đỉnh trong 18 cm và dưới khớp hông (`rig_from_metarig.py`), nên xoay chân trước không còn kéo xương sườn và lưng (đo: 0 đỉnh trên z=0,95 dịch chuyển, trước là 1.350).
- Không có Mixamo: Mixamo cần tài khoản Adobe và chỉ có clip cho **người hai chân**, không dùng được cho rig bốn chân này.

## Clip vẽ tay (2026-11-21)
`tools/blender/author_cho_clips.py` dựng clip bằng mã trên rig (xoay theo trục thế giới, đổi sang khung từng xương, nên không phụ thuộc xương xoay thế nào): **Idle** (thở, 48 khung), **Walk** (24 khung lặp, chân chéo luân phiên, gối gập, lưng và đuôi uốn), **Pounce_Tell** (ngả ra sau rồi chìm sâu vào thế nén, 32 khung), **Pounce_Strike** (lao chậm rồi bay, 14 khung, bốn chân duỗi), **Pounce_Recover** (tiếp đất nặng, đứng im chịu sức nặng, rồi từ từ đứng dậy, 43 khung). Xuất FBX bằng `rig_from_metarig.py ... --clips` (thêm `--blend-only` để lưu `.blend` xem thử). Menu `Build Meshy Static Prefabs` đặt tên clip, lặp Idle và Walk, dựng Animator Controller: `Speed` đổi Idle ↔ Walk, `WalkRate` (do `ChoXuongRigFx` đặt) chỉnh tốc độ Walk theo tốc độ thật, trigger `Tell_Pounce`, `Strike_Pounce`, `Recover_Pounce` của `CreatureCombat` chạy ba clip vồ. Có clip thì `ChoXuongRigFx` không đè chân, cổ, hàm của clip mà **cộng thêm** lên trên (hàm, cổ cho Cắn, Gầm, Cào, giật khi trúng, nằm ngất, ép chân khi cắn chặt).

## Chưa làm / rủi ro
- Chưa kiểm trong Unity: hướng xoay cổ và hàm tự đo lúc chạy; hàm há quá có thể kéo méo mõm (mõm sọ chỉ hé nhẹ): giảm `HoundRules.MaxJawDeg` nếu méo.
- Chân chỉ xoay ở hông; cẳng chân không gập.
- Chiêu chưa có tiếng riêng, đòn Gầm chưa có hình sóng (chỉ có vòng báo tầm trên đất).
- Co-op: client chưa thấy hiệu ứng xương.
