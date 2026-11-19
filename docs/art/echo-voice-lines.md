# Giọng Tiếng Vọng

Cảnh ra đời đọc mọi lời bằng giọng Tiếng Vọng. Mỗi câu là một file `Assets/_Project/Resources/Voice/Echo/<id>.mp3`; không có file thì câu đó chỉ hiện chữ. Cảnh **chờ giọng đọc xong** mới sang dòng sau.

## Danh sách câu (37 câu, lấy từ `BirthScript`)
- Lời dẫn: `echo.born`, `echo.body`, `echo.gift`, `echo.fate`, `echo.core`, `echo.flaw`, `echo.done`.
- Sáu Ân Điển: `gift.tui`, `gift.nuot`, `gift.giaima`, `gift.bienhinh`, `gift.songdo`, `gift.phanthan`.
- Tên 12 Lõi: `core.<id>` («Phát hiện Lõi độc nhất: …»), tên 12 Khuyết Điểm: `flaw.<id>` («Phát hiện khiếm khuyết: …»), dùng chính `Id` của Lõi và Khuyết Điểm.

Chữ của từng câu nằm trong `Assets/_Project/Scripts/Traits/Pure/BirthScript.cs` (một nguồn duy nhất). Đổi lời hay thêm Lõi thì chạy lại script bên dưới.

## Giọng đang dùng
- **Giọng:** Chi (ElevenLabs, nữ, giọng Bắc, `5yEPTrA0uAhZsBEhHjfi`, có sẵn trong tài khoản), mô hình `eleven_turbo_v2_5` với `language_code=vi`. Đọc **tự nhiên**: chỉ cân âm lượng, không hiệu ứng. Muốn giọng máy: `MODE=system`. Giọng khác: `VOICE=deC6NEXcbavaVWbzjgzb` (Phương, nữ trẻ giọng Trung) hoặc `VOICE=pGapy9MNHCukzJtjavF0` (Hạnh, nữ Hà Nội).
- **Đọc:** `stability 1.0`, `style 0`, `speed 0.95`.
- **Làm phẳng cao độ** (`tools/voice/flatten_pitch.py`, WORLD vocoder): đọc cùng một nốt như máy, một màu, không cảm xúc. `FLAT=0.0` phẳng hoàn toàn (mặc định), `FLAT=0.2` còn chút nhấn, `FLAT=1` giữ nguyên.
- **Hiệu ứng "hệ thống" (ffmpeg):** lọc dải 170 Hz–6,5 kHz, chorus mỏng, vang ngắn kiểu kim loại, nén, chuẩn hóa -16 LUFS. Máy móc hơn: thêm `acrusher` (méo số) hoặc `asetrate=44100*0.97` (trầm hơn); nhẹ hơn: bỏ `chorus`.

## Làm lại hoặc đổi giọng
```bash
tools/voice/generate_echo_voice.sh                 # tạo lại tất cả
ONLY=echo.born,core.quen_dat tools/voice/generate_echo_voice.sh   # chỉ vài câu
FLAT=0.2 tools/voice/generate_echo_voice.sh        # còn chút nhấn
```
Cần: `ffmpeg`, `dotnet`, `python3` với `numpy pyworld soundfile`, và quyền gọi `api.elevenlabs.io` (khóa do môi trường gắn, không ghi trong file). Đổi giọng: sửa `VOICE=` trong script. **Dùng giọng có sẵn của dịch vụ, không nhân bản giọng một diễn viên thật.**
