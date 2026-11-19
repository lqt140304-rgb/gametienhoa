# Giọng Tiếng Vọng: danh sách câu thoại

Cảnh ra đời phát giọng Tiếng Vọng nếu có file âm thanh; không có thì chỉ hiện chữ. Mỗi câu là một file đặt tại:

`Assets/_Project/Resources/Voice/Echo/<id>.mp3` (hoặc `.wav`, `.ogg`)

Tên file phải **đúng bằng `id`** (không có đuôi trùng). Unity tự nhận khi bạn bỏ file vào thư mục.

| id | Câu thoại |
|---|---|
| `echo.born` | Xác nhận… Cá thể mới hình thành. |
| `echo.gift` | Ban tặng Ân Điển Khởi Sinh. |
| `echo.core` | Phát hiện Lõi độc nhất. |
| `echo.flaw` | Phát hiện khiếm khuyết. |
| `echo.done` | Số mệnh đã định. Hãy sống sót. |

## Hướng dẫn giọng
- **Nữ, bình tĩnh, trung tính, hơi máy:** như một hệ thống thông báo, không cảm xúc, không nhấn mạnh.
- Nói tiếng Việt, tốc độ chậm vừa, ngắt đúng chỗ dấu "…".
- Mỗi câu dưới 4 giây (cảnh để 3,5 giây cho mỗi dòng).
- **Chọn một giọng có sẵn của dịch vụ, không bắt chước hay nhân bản giọng của một diễn viên thật.**

## Cách làm với ElevenLabs
1. Chọn một giọng nữ trung tính, đặt ổn định (Stability) cao và phong cách (Style) thấp để nghe đều.
2. Dán từng câu thoại ở trên, xuất mp3.
3. Đổi tên theo `id`, bỏ vào thư mục `Resources/Voice/Echo/`.

(Phiên làm việc dựng cảnh không có công cụ ElevenLabs nối sẵn nên chưa tạo giọng; bạn hoặc một phiên có kết nối ElevenLabs tạo giúp năm câu này.)
