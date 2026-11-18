# Lựa chọn đầu game: Dị Năng, Lõi, Khuyết Điểm (bản nháp quyết định)

Trạng thái: **đang thảo luận**, chưa phải thiết kế hoàn chỉnh. Chỉ ghi lại những gì người thiết kế đã chốt.

## Đã chốt
- **Dị Năng** = kỹ năng chủ động ở phím **Q**. Rút **ngẫu nhiên** đầu game. Đổi lại được bằng **dụng cụ đặc biệt** (chưa thiết kế). Dị Năng chính là "thiên phú" chủ động trong tài liệu cũ.
- **Lõi** = tên mới của "Thiên Phú bị động". Chọn **1 trong 3**. **Chống trùng** giữa các người chơi trong phòng. Đổi lại được, trả giá bằng **một trang bị hiếm** (chưa thiết kế).
- **Khuyết Điểm**: chọn 1 trong 3, **vĩnh viễn**, không xoá được, chỉ được cơ quan bù. Hướng thiết kế đã chấp nhận: gắn với lõi sống, đổi cách chơi thay vì chỉ trừ chỉ số.
- **Thanh kỹ năng 6 ô**: Q (Dị Năng) + 5 ô lấy từ cơ quan và class. Giữ hotbar hiện có trong code; bỏ ý "chỉ 2 ô chủ động" trong tài liệu gameplay V1.1 (mục 5.4 cần sửa).
- **Giai đoạn đầu phát triển** chỉ làm Lõi và Khuyết Điểm.
- **Lõi theo cơ chế** (đổi cách chơi), không thuần số liệu. **Khuyết Điểm** là khuyết điểm thật, **không kèm lợi ích nhỏ**. Lõi cho lợi thế rõ, Khuyết Điểm là mặt trái cùng chủ đề (hướng ví dụ: Màng Thẩm Thấu, Nhân Thần Tốc).
- **Cách chọn = C (nối tiếp):** chọn Lõi trước, sau đó chọn Khuyết Điểm trong 3 cái **cùng chủ đề với Lõi đã chọn**.

## Luật viết Khuyết Điểm (đã duyệt)
1. Khuyết Điểm đánh vào **một trục khác**, không đánh vào tình huống Lõi phát huy.
2. Lõi + Khuyết Điểm vẫn là **lợi ròng**: bỏ Khuyết Điểm đi thì Lõi vẫn rõ là tốt.
3. Khuyết Điểm **làm người chơi để ý**, không làm họ thua; không biến mất sau cấp 20, không phạt nặng mọi trận.

## Danh sách Lõi đầu tiên (bản nháp, chưa duyệt)
Kho Khuyết Điểm dùng chung (số liệu tạm): Thân Dễ Vỡ (máu tối đa thấp hơn), Hồi Chậm (hồi năng lượng chậm), Đói Nhanh, Cảm Giác Cùn (tầm cảm nhận ngắn), Mô Nặng (di chuyển chậm hơn chút), Nhảy Thấp, Sợ Nhiệt, Sợ Lạnh, Sợ Độc, Hao Nền (năng lượng hao nền nhiều hơn).

| Lõi | Cơ chế | 3 Khuyết Điểm (chọn 1) |
|---|---|---|
| Màng Thẩm Thấu | Đứng yên trong nước hoặc rêu thì hút dưỡng chất dần | Thân Dễ Vỡ · Cảm Giác Cùn · Sợ Nhiệt |
| Nhân Thần Tốc | Nảy đúng nhịp liên tiếp thì tăng tốc theo chuỗi | Hao Nền · Thân Dễ Vỡ · Sợ Độc |
| Nhân Ngụy Sắc | Đứng yên vài giây thì hòa vào nền | Hồi Chậm · Sợ Lạnh · Đói Nhanh |
| Nhân Phản Chấn | Bị đánh trúng thì tích lực, đòn kế tiếp mạnh hơn | Mô Nặng · Đói Nhanh · Sợ Nhiệt |
| Nhân Cộng Sinh | Ở gần đồng đội thì cả hai hồi máu nhanh hơn | Cảm Giác Cùn · Hồi Chậm · Sợ Độc |
| Nhân Ăn Mòn | Chạm vật hữu cơ thì tiết dịch ăn dần, hút thêm dinh dưỡng | Thân Dễ Vỡ · Nhảy Thấp · Cảm Giác Cùn |
| Nhân Bám Dính | Không bị hất văng khi đứng vững hoặc đi chậm; leo dốc không mất tốc | Sợ Nhiệt · Hao Nền · Đói Nhanh |
| Nhân Dự Trữ | Khi no thì tích mỡ, về sau đói chậm lại | Nhảy Thấp · Sợ Lạnh · Mô Nặng |
| Nhân Quang Hợp (thay Phát Quang) | Đứng hoặc đi trong tia sáng xuyên rễ thì hồi năng lượng dần | Thân Dễ Vỡ · Cảm Giác Cùn · Sợ Lạnh |

Đã bỏ khỏi danh sách theo phản hồi: Nhân Dò Rung và Nhân Tái Cấu (vô dụng), Nhân Tích Bào (giống kỹ năng bấm hơn là Lõi).
Nguyên tắc: **Lõi tự chạy, đổi một luật của cơ thể; Dị Năng mới là thứ bấm nút.**

Đã duyệt thêm:
- **Nhân Con Bạc**: tăng **tỉ lệ rơi** cơ quan từ quái và đồ từ rương (không tăng bậc). Chỉ áp dụng cho lượt rơi của chính người đó, host quyết định. Khuyết Điểm gợi ý: Thân Dễ Vỡ · Đói Nhanh · Hao Nền. **Áp dụng cả cho boss** (đã duyệt). Bù lại, Khuyết Điểm của Lõi này **nặng hơn các Lõi khác** (mức số liệu cao hơn); vẫn tuân luật 3 (không làm người chơi thua chắc). Cần giới hạn: tăng tỉ lệ rơi có trần để không chắc chắn lấy hết cơ quan hiếm của boss.

Đã duyệt: **Nhân Quen Đất** (đi lại một đường nhiều lần thì nhanh hơn trên đường đó).
Đã duyệt: **Nhân Thích Nghi** (chịu lặp lại một loại sát thương môi trường thì kháng loại đó tăng dần, hết bị tác động thì giảm; Khuyết Điểm không dùng nhóm Sợ-).
Đã duyệt: **Nhân Giữ Gốc** = khi chết **không mất cơ quan đang trang bị** (không áp dụng cho vật phẩm mang theo hay kho). Quyết định của Claude, chờ phản hồi: không áp dụng ở lần chết thứ 3 (hết mạng); Khuyết Điểm nặng hơn mức thường, kiểu cộng hai Khuyết Điểm nhẹ.
Đã chỉnh lại theo phản hồi: Nhân Phát Quang thành Nhân Quang Hợp; Nhân Bám Dính thành luật không bị hất văng khi đứng vững. Đã duyệt.

## Cơ chế chi tiết của Lõi (bản nháp mẫu, chưa duyệt)
Mỗi Lõi sẽ được mô tả theo cùng khuôn: kích hoạt khi nào, giới hạn, tương tác (cơ quan, đồng đội, boss), đồng bộ co-op, số liệu tạm. Hai mẫu đầu:

**Nhân Quen Đất**
- Kích hoạt: mỗi lần đi qua một đoạn đường (ô lưới khoảng 4 m) thì tăng điểm "quen" của đoạn đó.
- Hiệu ứng: điểm quen cao thì tốc độ trên đoạn đó tăng tối đa +15% (số tạm).
- Giới hạn: điểm quen giảm dần khi bỏ đoạn đường nhiều phút; không cộng dồn khi đứng yên; không tác dụng trong nước sâu.
- Tương tác: đồng đội không chia sẻ điểm quen; ghi theo từng người; lưu theo ván.
- Co-op: tính toán tại máy mỗi người, host chỉ kiểm tra tốc độ tối đa.

**Nhân Giữ Gốc**
- Kích hoạt: khi chết ở lần thứ 1 hoặc 2.
- Hiệu ứng: cơ quan đang trang bị không bị mất, vẫn gắn vào thân sau hồi sinh.
- Giới hạn: không áp dụng lần chết thứ 3; không bảo vệ vật phẩm mang theo hay kho; cơ quan đã hỏng vẫn hỏng.
- Tương tác: không đổi hình phạt mất mạng; lõi này cộng hai Khuyết Điểm nhẹ.
- Co-op: host quyết định lúc chết, gửi kết quả cho mọi người.

**Màng Thẩm Thấu**
- Kích hoạt: đứng yên (di chuyển dưới 0,3 m/s) trong nước nông hoặc trên rêu ẩm.
- Hiệu ứng: hút dưỡng chất dần, tương đương một phần nhỏ nguồn ăn (tạm 1,5% độ no mỗi giây); khi ngồi thấm thì ngừng hao độ no.
- Giới hạn: không hoạt động trong nước sâu khi đang bơi; tối đa lấy đầy 60% thanh no, phần còn lại phải ăn thật; bị đánh thì ngắt 3 giây.
- Tương tác: cơ quan tiêu hóa không đổi; tia sáng gắt không cộng thêm.
- Co-op: tính tại máy từng người, host kiểm tra tốc độ tăng độ no tối đa.

**Nhân Thần Tốc**
- Kích hoạt: mỗi cú nảy chạm đất đúng nhịp (bấm nảy trong khoảng 0,3–0,6 giây sau lần chạm trước).
- Hiệu ứng: mỗi nảy đúng nhịp cộng một cấp chuỗi; mỗi cấp +4% tốc độ, tối đa 5 cấp (tạm +20%).
- Giới hạn: nảy sai nhịp hoặc đứng yên quá 1 giây thì mất chuỗi; không hoạt động khi bơi hoặc bám vách.
- Tương tác: cơ quan vận động cộng bình thường; chuỗi không tăng sát thương.
- Co-op: chuỗi là trạng thái tại máy người chơi; host kiểm tra tốc độ tối đa cho phép.

**Nhân Ngụy Sắc**
- Kích hoạt: đứng yên 2 giây.
- Hiệu ứng: màu thân dần hòa với nền; kẻ địch phải lại gần hơn mới phát hiện (tạm giảm 60% tầm phát hiện).
- Giới hạn: di chuyển, tấn công hoặc bị đánh thì mất ngụy trang ngay; không hiệu quả ở nền quá sáng (tia sáng gắt) hoặc quá lạ màu.
- Tương tác: kẻ địch có khứu giác vẫn đánh hơi được; đồng đội không bị ảnh hưởng.
- Co-op: trạng thái ngụy trang do host đồng bộ để AI kẻ địch dùng chung một nguồn.

**Nhân Phản Chấn**
- Kích hoạt: nhận sát thương từ đòn trúng thân.
- Hiệu ứng: tích điểm lực theo lượng sát thương nhận; đòn tấn công kế tiếp mạnh hơn theo điểm tích (tạm +25% ở mức đầy).
- Giới hạn: điểm lực tối đa 3 lần tích; mất sau 6 giây không đánh; sát thương môi trường (độc, nhiệt) không tích.
- Tương tác: cộng với giáp nhưng giáp càng cao thì tích càng ít (vì sát thương nhận ít).
- Co-op: host tính điểm lực khi tính sát thương để tránh lệch.

**Nhân Cộng Sinh**
- Kích hoạt: có đồng đội trong bán kính 8 m.
- Hiệu ứng: cả hai hồi máu nhanh hơn (tạm +40% tốc độ hồi); người có Lõi này mạnh hơn khi nhiều người cùng đứng gần.
- Giới hạn: tối đa 3 đồng đội tính vào; đồng đội không có Lõi vẫn được hưởng hồi nhanh nhưng yếu hơn (một nửa); khi chơi một mình hoặc không có ai trong 8 m thì vẫn được hồi nhanh nhẹ (tạm +10%, bằng một phần tư mức gần đồng đội).
- Tương tác: không chồng với hồi máu của cơ quan; lấy mức cao hơn.
- Co-op: host tính khoảng cách giữa người chơi mỗi 0,5 giây.

**Nhân Ăn Mòn**
- Kích hoạt: chạm hoặc bao phủ vật hữu cơ (xác, thực vật, nấm).
- Hiệu ứng: tiết dịch làm vật hữu cơ phân hủy nhanh hơn, hút thêm dưỡng chất (tạm +30%).
- Giới hạn: chỉ vật hữu cơ; không tác dụng với vật cứng hoặc sinh vật còn sống; chỉ tính một lần cho mỗi vật.
- Tương tác: cộng với tuyến axit nhưng không chồng cùng loại dịch.
- Co-op: host quyết định lượng hút khi nhiều người cùng ăn một xác, chia theo thời gian.

**Nhân Con Bạc**
- Kích hoạt: mỗi lượt rơi chiến lợi phẩm của chính người này (cơ quan từ quái, đồ từ rương, kể cả boss).
- Hiệu ứng: tăng tỉ lệ rơi **theo bậc cơ quan** (tăng tương đối, số tạm): D +20% · C +16% · B +12% · A +8% · S +4% (bậc S là đề xuất của Claude, chờ duyệt). Bậc càng hiếm thì tăng càng ít. Với boss áp dụng một nửa mức theo bậc.
- Giới hạn: có trần, không bao giờ đạt chắc chắn; không tăng bậc cơ quan; chỉ áp cho lượt của người đó.
- Tương tác: Khuyết Điểm của Lõi này nặng hơn mức thường (cộng hai Khuyết Điểm nhẹ).
- Co-op: host lăn xúc xắc rơi cho từng người; hiệu ứng nhẹ báo khi đồ hiếm rơi nhờ Lõi này.

**Nhân Thích Nghi**
- Kích hoạt: chịu liên tục một loại sát thương môi trường (độc, nhiệt, lạnh, axit).
- Hiệu ứng: kháng loại đó tăng dần theo thời gian chịu (tạm +4% mỗi 5 giây).
- Giới hạn: kháng tối đa 40%; giữ kháng của một loại tại một thời điểm; hết bị tác động 60 giây thì giảm dần về 0.
- Tương tác: Khuyết Điểm của Lõi này không dùng nhóm Sợ-; cộng với kháng từ cơ quan nhưng tổng không vượt 70%.
- Co-op: tính tại host khi tính sát thương môi trường.

**Nhân Quang Hợp**
- Kích hoạt: đứng hoặc đi trong tia sáng xuyên rễ (vùng LightShaft của bản đồ).
- Hiệu ứng: hồi năng lượng dần (tạm 3 điểm mỗi giây).
- Giới hạn: không hoạt động khi bị che (dưới nước sâu); thân có ngụy trang không cản; không cộng khi có cơ quan hồi năng lượng cùng lúc (lấy mức cao).
- Tương tác: Khuyết Điểm không dùng Hao Nền để tránh cùng trục.
- Co-op: mỗi người tính riêng; tia sáng là một phần bản đồ nên không cần đồng bộ thêm.

**Nhân Bám Dính**
- Kích hoạt: đứng vững hoặc đi chậm (dưới 40% tốc độ tối đa).
- Hiệu ứng: miễn nhiễm hất văng và đẩy lùi; leo dốc không mất tốc.
- Giới hạn: khi chạy hết tốc độ thì mất miễn nhiễm; không áp dụng với hiệu ứng làm choáng hoặc trói; đòn hất cực mạnh của boss vẫn đẩy được một đoạn ngắn.
- Tương tác: Khuyết Điểm không dùng Mô Nặng hoặc Nhảy Thấp.
- Co-op: host quyết định hất văng, kiểm tra điều kiện đứng vững.

## Kho Khuyết Điểm chi tiết (bản nháp)
Quy ước chung (số liệu tạm):
- Khuyết Điểm vĩnh viễn, không xoá được. Cơ quan đúng vị trí giải phẫu có thể **bù tối đa 70%** tác động; còn 30% luôn ở lại. Cơ quan hỏng hoặc mất thì phần bù mất theo.
- Mỗi Khuyết Điểm có một biểu tượng cố định trên HUD và mô tả khi rê chuột. Tác động phải **nhìn thấy được**, không ẩn.
- Lõi "nặng hơn" (Nhân Con Bạc, Nhân Giữ Gốc): mỗi lựa chọn là **một cặp hai Khuyết Điểm** thay vì một.
- Trục: Sống còn (máu, đói, năng lượng) · Cảm giác · Vận động · Môi trường.

| Khuyết Điểm | Trục | Tác động (tạm) | Cơ quan bù được (vị trí) | Tín hiệu cho người chơi |
|---|---|---|---|---|
| Thân Dễ Vỡ | Sống còn | Máu tối đa −12% | Lưng (giáp), Da/màng | Thanh máu ngắn hơn, viền thanh đổi màu |
| Hồi Chậm | Sống còn | Hồi năng lượng chậm −25% | Tuyến, Tiêu hóa | Thanh năng lượng hồi chậm, đồng hồ hiện |
| Đói Nhanh | Sống còn | Độ no tụt nhanh hơn +20% | Tiêu hóa | Thanh no tụt nhanh, báo đói sớm hơn |
| Hao Nền | Sống còn | Năng lượng nền hao thêm +15% | Tiêu hóa, Hô hấp | Năng lượng không tăng khi đứng yên |
| Cảm Giác Cùn | Cảm giác | Tầm cảm nhận (vòng nhìn) ngắn lại −25% | Mắt, Cảm quan phụ | Vòng nhìn nhỏ hơn, nền mờ sớm hơn |
| Mô Nặng | Vận động | Tốc độ di chuyển −8% | Chân, Vây/cánh, Đuôi | Slime ngồi thấp hơn, trườn chậm |
| Nhảy Thấp | Vận động | Cú nảy thấp và ngắn hơn −20% | Chân, Đuôi | Cú nảy thấp, hiệu ứng nảy yếu |
| Sợ Nhiệt | Môi trường | Nhận thêm sát thương nhiệt +30% | Da/màng | Biểu tượng lửa gạch, viền đỏ khi gần nguồn nóng |
| Sợ Lạnh | Môi trường | Nhận thêm sát thương lạnh +30% | Da/màng | Biểu tượng tuyết gạch, thân run khi lạnh |
| Sợ Độc | Môi trường | Nhận thêm sát thương độc +30% | Tuyến, Hô hấp | Biểu tượng độc gạch, thân tái khi trúng độc |

Quy tắc chọn 3 Khuyết Điểm cho mỗi Lõi: không cùng trục với tác động chính của Lõi; Nhân Thích Nghi không dùng nhóm Sợ-; Nhân Quang Hợp không dùng Hao Nền và Hồi Chậm (cùng trục năng lượng); Nhân Bám Dính không dùng Mô Nặng và Nhảy Thấp.

Cần kiểm tra khi cài đặt: tổng bù từ nhiều cơ quan không vượt 70%; hai Khuyết Điểm cùng trục (trong cặp) không được chọn trùng.

## Đề xuất của Claude, chưa được duyệt
- Chống trùng dùng chung kho Lõi cả phòng; cần khoảng 12–16 Lõi lúc đầu (phòng 8 người cần ≥ 24 nếu không cho nhìn lại sau khi người khác chọn).

## Còn mở
- Danh sách Lõi đầu tiên (12–16) với cơ chế và 3 Khuyết Điểm mỗi Lõi.
- Trang bị hiếm dùng để đổi Lõi; dụng cụ đặc biệt để đổi Dị Năng.
- Có chống trùng Dị Năng không (chưa yêu cầu).
