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

## Giọng đang dùng (đã tạo sẵn)
Năm file đã có trong `Assets/_Project/Resources/Voice/Echo/`. Cách làm lại hoặc đổi giọng: sửa và chạy `tools/voice/generate_echo_voice.sh` (cần ffmpeg và quyền gọi ElevenLabs; khóa API do môi trường gắn, không ghi trong file).

- **Giọng:** Chi (ElevenLabs, nữ, giọng Bắc), mô hình `eleven_turbo_v2_5` với `language_code=vi` (mô hình multilingual tự đoán ngôn ngữ nên đọc lệch tiếng Việt).
- **Đọc:** `stability 1.0`, `style 0`, `speed 0.95` cho đều, ít cảm xúc.
- **Hiệu ứng "hệ thống" (ffmpeg):** lọc dải 160 Hz–7,5 kHz, chorus mỏng, méo số nhẹ, vang ngắn kiểu kim loại, nén, chuẩn hóa âm lượng -16 LUFS. Muốn máy móc hơn: tăng `acrusher mix` hoặc thêm `asetrate=44100*0.96`; muốn nhẹ hơn: bỏ `chorus` và `acrusher`.
- Mỗi câu 1,5 đến 2,2 giây.

## Hướng dẫn giọng (nếu tự đổi)
- **Nữ, bình tĩnh, trung tính, hơi máy:** như một hệ thống thông báo, không cảm xúc, không nhấn mạnh.
- Mỗi câu dưới 3,5 giây (cảnh để 3,5 giây cho mỗi dòng).
- **Dùng một giọng có sẵn của dịch vụ, không bắt chước hay nhân bản giọng của một diễn viên thật.**
