# Bộ chiêu của Chó Xương (đã chốt với chủ dự án 2026-11-21)

Chúa Hang Nghĩa Địa Cự Thú, mô hình Meshy gắn xương từ metarig của chủ dự án (`tools/blender/rig_from_metarig.py`). Loài dựa trên đời thực: thú săn đuổi bền như linh cẩu và sói (đuổi, vồ, cắn giữ, hú). Số là số tạm, chỉnh khi chơi.

| Chiêu | Dữ liệu (`MovesetSeed`, khóa `choxuong`) | Mở khi |
|---|---|---|
| **Chạy đuổi** | không phải chiêu: tốc độ đi 1,1, đuổi 3,6 (số của prefab); thân uốn theo sải chạy (`ChoXuongRigFx`) | luôn |
| **Vồ** (`Pounce`) | Lunge, báo 0,6 s, lao 5 m trong 0,45 s, hồi 1,5 s, sát thương 24, tầm 2,5–7 m, hồi chiêu 2,5 s. Lúc lao **thu chân ra sau** (`ChoXuongRigFx`). **Đầu trúng mục tiêu (trong 60° phía trước) thì cắn chặt: mini game ấn xen kẽ A và D để thoát** (`PinGame`, `HoundPin`): cắn liên tục 6 sát thương/giây, mỗi lần ấn đúng +0,09, thanh tự tụt 0,18/giây, thoát khi đầy thì chó bị hất lùi và loạng choạng 1,2 s; sau 8 s chó tự nhả | luôn |
| **Cắn Nghiền** (`Bite`) | Lunge, báo 0,35 s, đớp 0,2 s, hồi 1,5 s, sát thương 20, tầm 0–2,5 m, hồi chiêu 1,5 s | luôn |
| **Cào Xé** (`Claw`, thêm theo gợi ý chủ dự án) | Lunge ngắn, báo 0,55 s (vươn đầu, giơ chân trước lên), vung 0,25 s, hồi 1,2 s, sát thương 16, tầm 0–3,5 m, hồi chiêu 3 s; chân trước trái rồi phải luân phiên | luôn |
| **Nhảy Vượt** (`Leap`) | Perch (như Khỉ Hang nhảy bục), xa 6 m, cao 0,3–1,5 m, đứng trên bục 1,5 s, hồi chiêu 12 s | luôn (cần có bục) |
| **Gầm** (`Roar`, kỹ năng Gầm) | mới `MoveShape.Roar`: báo 0,9 s, bán kính 8 m, hồi chiêu 20 s. Loài nhỏ hơn bỏ chạy 4 s (`FearSeconds`); **slime nhỏ hơn bị đẩy lùi (Force 8) và choáng 0,8 s (`Freeze`)** | máu ≤ 66% |
| **Hú Gọi Bầy** (`Howl`) | Call, báo 1,2 s, gọi Bọ Ăn Xác trong 25 m, hồi chiêu 20 s (phải có Bọ Ăn Xác trong 25 m mới thấy tác dụng) | máu ≤ 66% (trước là 33%, đổi để dễ thấy khi thử) |
| **Trúng đòn** | giật đầu 0,25 s (`HoundRules.Flinch`); đòn ≥ 12% máu tối đa thì **loạng choạng 0,6 s** (`CreatureCombat.Stagger`: dừng đòn đang ra, đứng im, lúc đó nhận ×1,5 sát thương như pha Recover) | luôn |

## Cách làm
- Dữ liệu chiêu và tư thế là **mã thuần có test**: `Combat/Pure/MovesetSeed.cs`, `Combat/Pure/HoundPose.cs` (`HoundRules`: góc cổ và hàm theo từng chiêu và pha), test `MovesetSeedTests`, `HoundRulesTests`.
- Không có clip: `Combat/ChoXuongRigFx.cs` xoay xương `spine.009-.011` (cổ, nâng hoặc hạ đầu), `jaw` (há, giới hạn 24°; xương nhỏ `JawTip` cho Unity biết chiều há), `spine.007` (lưng uốn khi chạy). `CreatureLocomotion` vẫn lo chân (`Leg_*`), `Root`, `Head`, đuôi `Tail1-5`.
- Tư thế thân (cúi, vươn) vẫn do `TellPose` của `CreatureCombat` (nén thân khi vồ, vươn khi gầm).
- Gầm xử lý trong `CreatureCombat.Roar`.

## Chỉnh sau lần thử đầu (2026-11-21)
- Animation bị giật và quá nhanh: (1) cổ dùng chung một trục cho cả ba xương, nay mỗi xương có trục riêng; (2) báo trước của Chó Xương chậm đi 1,5 lần (`ChoXuongRigFx.TellSlowdown`); (3) run còn 22 Hz biên 2° (trước 60 Hz); (4) tư thế nén thân của `TellPose` còn 40% (`MovesetSeed.PoseStrength`), vì xương đã tự cử động; (5) xương làm mượt chậm hơn (9/giây).
- Cỡ mô hình nhỏ đi 30% rồi thêm 10% (prefab: mô hình và collider ×0,63).
- Trọng số chân: xương chân chỉ kéo đỉnh trong 18 cm và dưới khớp hông (`rig_from_metarig.py`), nên xoay chân trước không còn kéo xương sườn và lưng (đo: 0 đỉnh trên z=0,95 dịch chuyển, trước là 1.350).
- Gọi bầy: đã có `Hú Gọi Bầy` (gọi Bọ Ăn Xác, máu dưới 33%).

## Chưa làm / rủi ro
- Chưa kiểm trong Unity: hướng xoay cổ và hàm tự đo lúc chạy; hàm há quá có thể kéo méo mõm (mõm sọ chỉ hé nhẹ): giảm `HoundRules.MaxJawDeg` nếu méo.
- Chân chỉ xoay ở hông; cẳng chân không gập.
- Chiêu chưa có tiếng riêng, đòn Gầm chưa có hình sóng (chỉ có vòng báo tầm trên đất).
- Co-op: client chưa thấy hiệu ứng xương.
