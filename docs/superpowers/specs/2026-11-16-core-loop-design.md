# Thiết kế vòng lặp chính — Project Evolve

Ngày: 2026-11-16 · Trạng thái: chờ duyệt

## Mục đích
Chốt các quyết định về vòng lặp chính (trước đây là "Câu hỏi cần chốt" trong `docs/GDD.md`) để định hướng các bản sau Prototype 0.1, mà không làm phình phạm vi 0.1.

## Mục tiêu / Không phải mục tiêu / Phản mục tiêu
- **Mục tiêu:** sinh tồn thế giới mở lâu dài; chết thật sự đau; tiến hóa dẫn đường khám phá; co-op có ý nghĩa.
- **Không phải mục tiêu (lúc này):** hệ thống xây dựng, PvP, mục tiêu cuối duy nhất.
- **Phản mục tiêu:** spam tự sát để bốc lại Dị Năng; cày cấp Dị Năng vô nghĩa; thành "Minecraft với nhân vật slime".

## Quyết định đã chốt
### Vòng lặp & cái chết
- Thế giới mở, không giới hạn thời gian, lưu lại giữa các buổi chơi (kiểu Minecraft).
- Chết = làm lại từ đầu: mất toàn bộ cơ quan, chỉ số, sinh khối; quay về slime nguyên sinh.
- Dị Năng, Thiên Phú, Khiếm Khuyết **cố định theo người chơi trong mỗi thế giới**, không mất khi chết.

### Dị Năng
- Tiến hóa theo **sử dụng**; chỉ tính khi dùng có tác dụng (trúng mục tiêu, né đòn, di chuyển được).
- Tốn Năng lượng và có thời gian hồi.
- Đổi được bằng **vật phẩm đặc biệt rất hiếm** (từ boss): bốc **ngẫu nhiên**, mất cấp tiến hóa của Dị Năng cũ.

### Boss
- Nhiều boss, ít nhất một con mỗi vùng; không bắt buộc thứ tự, môi trường tự cản đường.
- Rơi cơ quan độc nhất; **mỗi người tham gia quay tỉ lệ rơi riêng**.

### Cơ quan
- Tự mọc khi ăn/hấp thụ nguồn có cơ quan, người chơi chọn giữ hoặc bỏ (bỏ được trả sinh khối).
- Lớn dần qua 3 giai đoạn: mầm → non → trưởng thành; tiêu sinh khối khi lớn; hoạt động một phần khi chưa trưởng thành.
- Mọc vào các vị trí cố định trên cơ thể; số vị trí tăng theo kích thước.

### Chỉ số
- Máu (sống/chết), Đói (áp lực liên tục), Năng lượng (áp lực trong trận), Sinh khối (vật liệu tiến hóa, để sau 0.1).

### Tổ & xây dựng
- Bản đầu: tổ đơn giản = điểm hồi sinh + kho thức ăn dùng chung; còn lại khi chết; không cất cơ quan.
- Xây dựng **để sau**, theo hướng **sinh học** (vật liệu tiết ra từ cơ quan, đặt theo lưới, tốn sinh khối/dưỡng chất).
- Nền chuẩn bị từ đầu: lưu thế giới, tài nguyên sinh khối/dưỡng chất rõ ràng, trường "tiết vật liệu" trống trên SO cơ quan, địa hình không cần voxel.

## Cách tiếp cận
1. **Viết tài liệu, giữ nguyên phạm vi 0.1 — đã chọn.** Prototype vẫn tập trung vào "trườn, hấp thụ, sống sót".
2. Như 1, và thêm năng lượng/hồi chiêu/lên cấp Dị Năng vào 0.1 — bỏ: làm phình 0.1, khó biết cái vui đến từ đâu.
3. Viết lại toàn bộ GDD — bỏ: tốn công, dễ mất phần đang ổn.

## Đề xuất của Claude (chưa được người dùng xác nhận rõ — kiểm tra khi đọc)
- Chống xui: không rơi thì tăng tỉ lệ lần sau, reset khi rơi.
- Boss hồi sinh sau một thời gian.
- Đổi Dị Năng không bao giờ ra bậc thấp hơn hiện tại.
- Vật phẩm đổi Dị Năng: cất trong tổ thì còn, mang theo người thì mất khi chết.
- Cơ quan đang lớn làm năng lượng hồi chậm hơn.
- Bậc giai đoạn tăng vị trí: slime nguyên sinh 2 vị trí, trưởng thành 6–8.

## Việc sẽ làm
- Cập nhật `docs/GDD.md`: đưa quyết định vào "Ý tưởng đã thống nhất", đánh dấu câu hỏi đã trả lời, thêm lộ trình: 0.1 trườn & sống sót → 0.2 cơ quan mọc dần → boss → xây dựng sinh học.

## Để lại cho người làm
- Con số cụ thể (tỉ lệ rơi, thời gian hồi boss, tốc độ lớn của cơ quan) và danh sách vị trí/loại cơ quan.
- Mục tiêu dài hạn ngoài boss: chưa bàn.
