#!/usr/bin/env bash
# Tạo giọng Tiếng Vọng (5 câu) bằng ElevenLabs rồi thêm hiệu ứng "hệ thống" bằng ffmpeg.
# Cần: ffmpeg, và quyền gọi api.elevenlabs.io (khóa API do môi trường tự gắn; KHÔNG ghi khóa vào file này).
# Chạy: tools/voice/generate_echo_voice.sh   (ghi đè các file trong Assets/_Project/Resources/Voice/Echo)
set -euo pipefail
cd "$(dirname "$0")/../.."
OUT=Assets/_Project/Resources/Voice/Echo
VOICE=5yEPTrA0uAhZsBEhHjfi   # Chi: nữ, giọng Bắc
MODEL=eleven_turbo_v2_5      # ép tiếng Việt bằng language_code (mô hình multilingual tự đoán ngôn ngữ nên dễ lệch)
mkdir -p "$OUT"
declare -A LINES=(
  [echo.born]="Xác nhận. Cá thể mới hình thành."
  [echo.gift]="Ban tặng Ân Điển Khởi Sinh."
  [echo.core]="Phát hiện Lõi độc nhất."
  [echo.flaw]="Phát hiện khiếm khuyết."
  [echo.done]="Số mệnh đã định. Hãy sống sót."
)
# Hiệu ứng: lọc dải, sóng chorus mỏng, méo số nhẹ, vang ngắn kiểu kim loại, nén, chuẩn hóa âm lượng.
FX="highpass=f=160,lowpass=f=7500,chorus=0.7:0.9:55:0.4:0.25:2,acrusher=bits=13:mix=0.25:mode=lin:aa=1,aecho=0.8:0.7:25|40:0.3|0.2,acompressor=threshold=-18dB:ratio=3,loudnorm=I=-16:TP=-1.5:LRA=7"
tmp=$(mktemp -d); trap 'rm -rf "$tmp"' EXIT
for id in "${!LINES[@]}"; do
  body=$(python3 -c 'import json,sys; print(json.dumps({"text":sys.argv[1],"model_id":sys.argv[2],"language_code":"vi","voice_settings":{"stability":1.0,"similarity_boost":0.7,"style":0.0,"speed":0.95}}))' "${LINES[$id]}" "$MODEL")
  curl -sS -m 90 -X POST "https://api.elevenlabs.io/v1/text-to-speech/$VOICE?output_format=mp3_44100_128" \
    -H "Content-Type: application/json" -d "$body" -o "$tmp/$id.mp3" -w "$id %{http_code}\n"
  ffmpeg -loglevel error -y -i "$tmp/$id.mp3" -af "$FX" -ar 44100 -ac 1 -b:a 96k "$OUT/$id.mp3"
done
ls -la "$OUT"
