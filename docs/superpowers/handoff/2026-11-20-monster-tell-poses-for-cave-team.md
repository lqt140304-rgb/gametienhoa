# Giao bên làm hang: dấu hiệu báo đòn bằng hình dáng cơ thể (lát 1, 4 loài)

Thiết kế: `docs/superpowers/specs/2026-11-20-monster-combat-movesets-design.md`. Chủ dự án muốn người chơi **nhìn hình dáng cơ thể là biết quái sắp đánh** (kiểu Hollow Knight), nên phần hoạt hình và biến dạng mô hình do bên làm hang làm. Phần lõi đã làm xong bộ máy chiêu và một **tư thế tạm bằng code** (co giãn, nghiêng, rung thân) để thử trước.

## Cái mã đã làm, và hợp đồng với các bạn
- Mỗi chiêu có ba nhịp: **gồng** (dấu hiệu báo, người chơi né), **ra đòn**, **kiệt sức** (người chơi phản công). Thời gian theo bảng dưới; ở Hang Khởi Sinh thời gian gồng dài hơn ×1,3.
- Khi vào mỗi nhịp, mã bật trigger của animator (nếu animator có, không có thì bỏ qua, không lỗi): `Tell_<Anim>` lúc gồng, `Strike_<Anim>` lúc ra đòn, `Recover_<Anim>` lúc kiệt sức (`<Anim>` là tên ở cột "Tên chiêu" của bảng). Cú choáng (Bọ Giáp đâm vách) bật `Stun`; Muỗi bật `Feed` khi đậu hút máu và `Flee` khi bị đuổi. Trigger cũ theo tên chiêu (`Attack`, `Retract`) vẫn được bật lúc gồng.
- **Thời lượng gồng thay đổi** (×1,3 ở hang đầu, đổi khi chỉnh số), nên clip `Tell_*` phải là **tư thế giữ được**: đi vào tư thế rồi giữ ở cuối (dùng clip lặp ở khung cuối), đừng gắn cứng với một số giây.
- Khi các bạn giao đủ clip cho một loài, thêm khóa loài (`SpeciesNames.Key`, ví dụ `thanlanboho`) vào `MovesetSeed.AuthoredClips` để mã **tắt tư thế tạm** cho loài đó (tránh biến dạng hai lần). Báo cho phiên lõi hoặc tự thêm một dòng.
- Tắt root motion. Thân quái được mã đẩy tới (lao, phóng), clip chỉ cần cử động tại chỗ.
- Mô hình con tên `Model` là nơi tư thế tạm được áp lên.

## Bảng chiêu và hình dáng cần nhìn thấy
Mọi thời gian tính bằng giây ở mức tầng giữa (hang đầu gồng ×1,3).

| Loài, chiêu (tên Anim) | Gồng (`Tell_`) | Ra đòn (`Strike_`) | Kiệt sức (`Recover_`) |
|---|---|---|---|
| **Thằn Lằn Bờ Hồ**, Rình-Vọt (`Attack`) | 0,6 giây: bụng sát đất, chân sau gập, đầu ngẩng nhìn chằm chằm, đuôi cuộn lại | 0,25 giây: thân duỗi dài, chân trước vươn tới, há miệng | 1,0 giây: ngồi thở dốc, đầu cúi thấp, miệng mở |
| **Bọ Giáp Quặng**, Húc (`Attack`) | 0,9 giây: chân trước cào đất hai lần, đầu và sừng cúi thấp, mai he hé rung nhẹ | 2,3 giây: chạy nhanh, sừng chĩa thẳng về trước | 1,6 giây: thở dốc, chân xoạc, người nghiêng. Kèm clip `Stun` lặp 2 giây khi đâm vách: chao đảo, chân quờ quạng |
| **Muỗi Rêu**, Chích-Hút (`Attack`) | 0,4 giây: vòi giương thẳng về trước, cánh xòe và rung nhanh | (không có đòn riêng) clip `Feed` lặp: đậu trên slime, vòi cắm xuống, bụng phình dần và đỏ lên; clip `Flee`: vỗ cánh rất nhanh, bay lên | 0,8 giây: rũ cánh, bay chao đảo |
| **Rùa Pha Lê** (Chúa Hang), Cắn Cổ (`Attack`) | 0,5 giây: cổ rụt về hình chữ S, mắt dán vào người chơi | 0,2 giây: cổ vươn dài cắn | 1,5 giây: cổ thè ra mệt, mở miệng |
| Rùa, Rụt Mai–Lao (`Retract`) | 0,8 giây: đầu và chân rụt hẳn vào mai, mai khép kín, hơi nảy lên | 1,5 giây: mai trượt thẳng, xoay chậm theo hướng lao | 1,5 giây: chân đầu từ từ thò ra, loạng choạng |
| Rùa, Xoay Mai (`Spin`) | 0,8 giây: rụt vào mai, mai rung và phát sáng nhẹ | 1,2 giây: mai xoay tròn quanh trục dọc, chân đầu giấu hẳn | 1,5 giây: chậm lại, thò đầu ra chóng mặt |
| Rùa, Gọi Bầy (`Roar`) | 1,0 giây: chồm lên (hai chân trước nhấc), ngửa đầu, tinh thể trên mai sáng lên theo nhịp | 0,1 giây: gầm, tinh thể sáng bừng | 1,5 giây: thở, tinh thể mờ dần |

Rùa Pha Lê là Chúa Hang, rất to (hệ số cỡ ×2,6 ở tầng Nông), nên hình dáng cần đọc được từ xa; ưu tiên hình rõ hơn là chi tiết nhỏ.

## Yêu cầu cho mô hình
- Mô hình giữ nguyên bộ xương hiện có nếu được; cần khớp xương cho cổ Rùa (nhiều đốt để vươn dài), vòi Muỗi (xương riêng để giương), sừng Bọ Giáp (hoặc cả đầu), mai Rùa (khép kín được).
- Bụng Muỗi phình dần và đỏ lên: dùng một tham số của vật liệu hoặc một xương co giãn, mã có thể điều khiển nếu các bạn cho tên tham số.
- Màu và chất liệu theo cách dựng hiện có của các bạn (`CreatureVertexColor`).

## Cách thử
Menu thần (F1), mục Kiểm thử, dòng "Combat quái": nút tạo Thằn Lằn, Bọ Giáp, Rùa Pha Lê, Bầy Muỗi; dòng bên dưới hiện pha hiện tại của từng quái gần (Idle/Windup/Active/Recover). Cho slime học Lách Né ("Học kỹ năng chủ động ★1") để thử né.

## Việc chưa làm, bàn sau
- Lát 2: Cáo Gió (cục băng, gió đẩy), Chồn Khe (đánh rồi bỏ chạy), rình ở góc khuất. Lát 3: Khỉ (cần loài mới). Sẽ có bản giao tương tự cho từng lát.

## Bổ sung lát 2: Chồn Khe và Cáo Gió (Khe Gió Hú)
Mã đã chạy được bằng tư thế tạm. Trigger đặt theo `<Anim>` của chiêu: `Tell_Attack`/`Strike_Attack`/`Recover_Attack` (Vồ Xa, Vồ Cắn), `Tell_Cast`/`Strike_Cast`/`Recover_Cast` (Băng Rơi), `Tell_Howl`/`Strike_Howl`/`Recover_Howl` (Gió Hú), `Tell_Roar`/`Strike_Roar`/`Recover_Roar` (Gọi Bầy). Khi giao đủ clip, thêm `chonkhe` và `caogio` vào `MovesetSeed.AuthoredClips`.

| Loài, chiêu | Gồng | Ra đòn | Kiệt sức |
|---|---|---|---|
| **Chồn Khe**, rình | nằm sát đất cuộn mình trong bóng tối, chỉ thấy đôi mắt (mã đang ẩn mô hình; nếu có clip nằm thì bỏ việc ẩn và dùng clip, báo lại để đổi) | bật dậy rất nhanh (0,2 giây, đoạn gồng của lúc bật ra) | |
| Chồn Khe, Vồ Xa (`Attack`) | 0,35 giây: hạ thấp người, vai nhô cao, đuôi dựng | 0,5 giây: thân duỗi dài vọt tới 8 m, hàm há | 1,5 giây: quay ngoắt rồi **chạy ngược lại** (cần clip chạy nhanh, thân thấp) |
| **Cáo Gió**, Vồ Cắn (`Attack`) | 0,5 giây: hạ thân, tai cụp, nhe răng | 0,25 giây: nhảy lao cắn | 1,5 giây: đứng thở, đuôi cụp |
| Cáo Gió, Băng Rơi (`Cast`) | 1,0 giây: ngồi chồm, một chân trước đưa lên cao, ánh sáng xanh băng tụ quanh bàn chân | 1,2 giây: bàn chân hạ xuống chỉ vào người chơi (băng rơi từ trên) | 1,5 giây: ánh sáng tắt, thở |
| Cáo Gió, Gió Hú (`Howl`) | 0,9 giây: ngửa đầu hít sâu, lông dựng, ngực nở | 1,2 giây: hú, lông và đuôi tung bay như bị gió thổi | 1,5 giây: gục đầu thở |
| Cáo Gió, Gọi Bầy (`Roar`) | 1,0 giây: chồm lên, ngửa mặt | 0,1 giây: gầm | 1,5 giây: thở |

## Bổ sung lát 3: Khỉ Hang
Loài mới, xem `2026-11-20-monkey-species-for-cave-team.md` cho phần loài. Trigger: `Tell_Throw`/`Strike_Throw`/`Recover_Throw` (Ném Đá), `Tell_Attack`/`Strike_Attack`/`Recover_Attack` (Cắn Vặt), `Tell_Perch`/`Strike_Perch` (Nhảy Bục), `Tell_Roar`/`Strike_Roar`/`Recover_Roar` (Hú Báo Động). Khóa loài để tắt tư thế tạm: `khihang`.

| Chiêu | Gồng | Ra đòn | Kiệt sức |
|---|---|---|---|
| Ném Đá (`Throw`) | 0,9 giây: ngả người ra sau, một tay giơ cao cầm đá, mắt dán vào người chơi | 0,1 giây: vung tay ném | 1,0 giây: tay thả xuống, thở |
| Cắn Vặt (`Attack`) | 0,4 giây: cúi thấp, nhe răng | 0,2 giây: vồ tới | 0,8 giây: quay ngoắt **chạy lùi** |
| Nhảy Bục (`Perch`) | 0,5 giây: cúi xuống, hai chân đạp mạnh | 0,8 giây: bay theo cung lên chỗ cao, tay chân duỗi | không (đứng trên cao ném đá) |
| Hú Báo Động (`Roar`) | 0,8 giây: ngửa đầu há miệng, hai tay vỗ ngực | 0,1 giây: hú | 1,0 giây: thở |

