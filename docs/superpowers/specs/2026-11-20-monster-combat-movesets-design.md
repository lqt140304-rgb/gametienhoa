# Thiết kế: chiến đấu của quái bằng bộ chiêu dùng chung (2026-11-20)

Chủ dự án đã chọn hướng (cách 2, lát 1) trong hội thoại ngày 2026-11-20. Các loài và con số trong bản này là **đề xuất để chủ dự án chốt trước khi làm**.

## Ý chủ dự án và lý do
- Chiến đấu hiện nay chủ yếu là lao vào nhau: kẻ săn đuổi rồi cắn, slime húc hoặc dùng kỹ năng. Thiếu đa dạng, không cần suy nghĩ.
- Chủ dự án muốn một trận solo thật sự, kiểu Hollow Knight: quái **báo đòn**, người chơi **né đúng lúc rồi phản công** khi quái lộ sơ hở.
- Quái khác nhau về cách đánh (gần, xa), có kỹ năng và bản năng riêng như ngoài đời (muỗi bám hút máu, rùa rụt mai rồi lao hoặc xoay, chồn đánh rồi bỏ chạy, cáo dùng phép băng và gió, khỉ ném vật), có thể **rình ở góc khuất** (kiểu Dark Souls, Elden Ring), có **đồng bọn và gọi bầy** nên đánh không dứt điểm thì bị hội đồng.
- Người chơi phải biết khắc chế và điểm yếu trước khi lao vào (kiểu Monster Hunter), và chỉ đánh trực diện khi đủ mạnh.
- Hạ quái thì nhận **kỹ năng nội tại** của nó (kiểu truyện chuyển sinh thành slime), không chỉ điểm kinh nghiệm hay vàng. Phần Giải Mã đã làm một nửa.
- **Né (dash) không có sẵn**: slime học nó từ kỹ năng của quái (Lách Né, học từ Thỏ Lá ở Vườn Quặng, đã có).

## Mục tiêu / không mục tiêu
- Mục tiêu: bộ máy chiêu dùng chung, chạy trên cả `Predator` và `Prey`; 4 loài lát 1 chơi được; mỗi đòn nguy hiểm đều có **dấu hiệu báo** và **cửa sổ phản công**; trùm có pha theo lượng máu.
- Không mục tiêu (lát sau): đánh xa bằng phép (cục băng, gió đẩy), khỉ ném vật và leo trèo, rình ở góc khuất, yếu theo họ kỹ năng, đổi chiêu quái thành kỹ năng người chơi một-một, mô hình hoặc hoạt hình mới, tiếng mới.
- Chống mục tiêu: đòn trúng mà người chơi không có cách thấy trước; quái không bao giờ nghỉ; trận hỗn loạn khó đọc; làm hỏng hành vi sinh thái và tổ đã có.

## Các cách đã cân nhắc (chủ dự án chọn cách 2)
1. Mỗi loài một script riêng: nhanh cho vài loài, nhưng 44 loài sẽ lặp code và khó cân bằng.
2. **Bộ chiêu dùng chung (chọn):** dữ liệu cho từng chiêu, một bộ máy chung, một thư viện nhỏ kiểu chiêu dùng lại.
3. Quái dùng đúng hệ thống kỹ năng người chơi: nhất quán phần thưởng nhưng phải sửa nặng phần gắn với năng lượng và thanh kỹ năng. Cách 2 được thiết kế để sau này mỗi chiêu quái đổi một-một thành kỹ năng người chơi.

## Quyết định đã thống nhất
- **Nhịp một chiêu:** Chờ → **Gồng** (dấu hiệu báo, có thể né) → **Ra đòn** (gây sát thương) → **Kiệt sức** (cửa sổ phản công) → Chờ (hồi chiêu). Trong lúc gồng và ra đòn của chiêu có giáp, quái không bị vồ (vồ bật ra); trong lúc kiệt sức, mọi sát thương nhận vào ×1,5.
- **Trùm có pha:** mỗi chiêu có ngưỡng máu mở; máu giảm thì thêm chiêu mới, không bỏ chiêu cũ.
- **Một bộ máy, hai loại chủ:** quái có bộ chiêu thì bộ máy điều khiển khi đang gồng, ra đòn, kiệt sức (`Busy`), còn lúc rảnh thì hành vi cũ (đi tuần, đuổi, chạy trốn, não sinh thái) vẫn chạy. Quái có bộ chiêu bỏ cú cắn va chạm cũ, chiêu thay vào đó.
- **Dấu hiệu báo:** hoạt hình sẵn có của loài (`Attack`, `Retract`, `Evade`…) cộng nháy màu và co giãn thân, đủ rõ ngay cả khi mô hình thô.
- **Độ khó theo tầng** (đã có `CaveTier`): sát thương chiêu nhân hệ số sát thương của tầng; hang đầu có thời gian gồng dài hơn ×1,3 (dễ đọc hơn).
- **Co-op:** chiêu chạy trên máy chủ; máy khách thấy chuyển động nhưng chưa thấy dấu hiệu báo riêng (giới hạn lát 1).

## Lát 1: bộ máy và 4 loài (dữ liệu đời thực, chờ chủ dự án chốt)
Thư viện kiểu chiêu: **Phóng** (Lunge), **Húc** (Charge), **Xoay** (Spin), **Bám hút** (Drain), **Gọi bầy** (Call). Số liệu tạm, chỉnh được trong menu thần.

| Loài | Cơ sở ngoài đời | Chiêu | Gồng | Kiệt sức | Ghi chú |
|---|---|---|---|---|---|
| Thằn Lằn Bờ Hồ (kẻ săn, hang đầu) | thằn lằn săn bằng cách rình bất động rồi bật nhanh một đoạn ngắn | **Rình-Vọt**: cúi người nhìn chằm chằm rồi phóng 3 m | 0,6 s (×1,3 ở hang đầu) | 1,0 s | sát thương 10 |
| Bọ Giáp Quặng (kẻ săn, Vườn Quặng) | bọ cánh cứng sừng húc thẳng, mai cứng | **Húc**: cào đất rồi lao 7 m với tốc độ gấp 2,2; đâm vách thì choáng 2 s | 0,9 s | 1,6 s (3,6 s nếu choáng) | có giáp khi gồng và húc |
| Muỗi Rêu (con mồi bay, Hang Rêu Thở) | muỗi đậu lên da và hút máu đến khi bị đuổi | **Chích-Hút**: bay tới, đậu lên slime, hút 1,5 máu mỗi giây, tự hồi máu bằng đó | 0,4 s | 0,8 s sau khi bị đuổi | thoát: chạy nhanh, hoặc giết nó (máu 8) |
| Rùa Pha Lê (Chúa Hang, Vườn Quặng) | rùa rụt vào mai, rùa cắn rụt cổ dài | **Cắn Cổ** (pha 1, tầm 2,5 m); **Rụt Mai–Lao** (pha 1, có giáp, lao 6 m); **Xoay Mai** (dưới 66% máu, đẩy lùi trong 4 m); **Gọi Bầy** (dưới 33% máu, báo Bọ Giáp trong 25 m) | 0,5 s / 0,8 s / 0,8 s / 1,0 s | 1,5 s | pha theo máu |

- Muỗi Rêu là con mồi trong dữ liệu sinh thái: giữ nguyên vai đó (chạy trốn kẻ săn), chỉ thêm việc chích slime khi slime đứng gần.
- Né của người chơi: Lách Né (đã có). Hang đầu an toàn nên không cần né.
- Quái nhận kỹ năng nội tại: lát 1 không thêm kỹ năng mới; Giải Mã đã cho kỹ năng của 12 loài đầu. Ánh xạ chiêu quái thành kỹ năng người chơi làm ở lát sau.

## Ràng buộc
- Luật thuần (bộ chọn chiêu, thời gian các giai đoạn, ngưỡng pha) trong `Combat/Pure/` có test chạy ngoài Unity; mã Unity mỏng. Không chạy được Unity trong môi trường làm việc, nên phần chạy thật phải do chủ dự án thử.
- Không phá phần sinh thái, tổ, `CreatureBrain`, `CreatureMotor`, vồ và ghì chặt của slime. `Predator.cs` và `Prey.cs` chỉ thêm móc nhỏ.
- Văn bản người chơi bằng tiếng Việt, mã bằng tiếng Anh.

## Việc để người dựng quyết
- Cách vẽ dấu hiệu báo cụ thể (nháy màu, co giãn, vòng cảnh báo trên đất) và độ lớn vùng đòn; mình đề xuất nháy đỏ nhạt cộng co giãn thân, vùng đòn là hình nón ngắn phía trước.
- Cách chọn chiêu khi có nhiều chiêu dùng được (đề xuất: trọng số, không lặp lại chiêu vừa dùng nếu có chiêu khác).

## Còn lại cho người làm sau
- Lát 2: đánh xa và rình (Cáo Gió: cục băng trên đầu và gió đẩy, Chồn Khe: đánh rồi bỏ chạy, rình ở góc khuất, gọi bầy rộng hơn).
- Lát 3: Khỉ (ném vật, leo trèo; cần thêm loài mới).
- Yếu theo họ kỹ năng; ánh xạ chiêu quái thành kỹ năng người chơi; co-op thấy dấu hiệu báo; tiếng cho từng chiêu.
