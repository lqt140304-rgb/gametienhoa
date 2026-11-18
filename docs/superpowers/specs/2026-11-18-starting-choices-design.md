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
Đã chỉnh lại theo phản hồi: Nhân Phát Quang thành Nhân Quang Hợp; Nhân Bám Dính thành luật không bị hất văng khi đứng vững. Chờ duyệt.

## Đề xuất của Claude, chưa được duyệt
- Chống trùng dùng chung kho Lõi cả phòng; cần khoảng 12–16 Lõi lúc đầu (phòng 8 người cần ≥ 24 nếu không cho nhìn lại sau khi người khác chọn).

## Còn mở
- Danh sách Lõi đầu tiên (12–16) với cơ chế và 3 Khuyết Điểm mỗi Lõi.
- Trang bị hiếm dùng để đổi Lõi; dụng cụ đặc biệt để đổi Dị Năng.
- Có chống trùng Dị Năng không (chưa yêu cầu).
