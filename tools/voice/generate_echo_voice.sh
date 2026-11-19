#!/usr/bin/env bash
# Tạo giọng Tiếng Vọng (mọi câu của cảnh ra đời) bằng ElevenLabs, làm phẳng cao độ rồi thêm hiệu ứng "hệ thống" bằng ffmpeg.
# Danh sách câu lấy từ BirthScript (một nguồn duy nhất): script gọi test ExportVoiceLines để xuất ra file.
# Cần: ffmpeg, dotnet (bộ test thuần), python3 + numpy pyworld soundfile (pip install), quyền gọi api.elevenlabs.io
# (khóa API do môi trường tự gắn; KHÔNG ghi khóa vào file này).
# Chạy: tools/voice/generate_echo_voice.sh        (ghi đè file trong Assets/_Project/Resources/Voice/Echo)
#   FLAT=0.0  phẳng hoàn toàn (một nốt như máy, mặc định);  FLAT=0.2  còn chút nhấn;  FLAT=1  giữ nguyên cao độ
#   ONLY=core.quen_dat,flaw.doi_som   chỉ tạo lại vài câu
set -euo pipefail
cd "$(dirname "$0")/../.."
OUT=Assets/_Project/Resources/Voice/Echo
VOICE=pGapy9MNHCukzJtjavF0   # Hạnh: nữ trẻ Hà Nội (thư viện chung của ElevenLabs, gọi thẳng bằng id)
MODEL=eleven_turbo_v2_5      # ép tiếng Việt bằng language_code (mô hình multilingual tự đoán ngôn ngữ nên dễ lệch)
FLAT="${FLAT:-0.0}"
ONLY="${ONLY:-}"
# Hiệu ứng "hệ thống": lọc dải, chorus mỏng, vang ngắn kiểu kim loại, nén, chuẩn hóa âm lượng.
FX="highpass=f=170,lowpass=f=6500,chorus=0.7:0.9:50:0.45:0.3:2,aecho=0.8:0.7:20|35:0.4|0.3,acompressor=threshold=-18dB:ratio=3,loudnorm=I=-16:TP=-1.5:LRA=7"

tmp=$(mktemp -d); trap 'rm -rf "$tmp"' EXIT
mkdir -p "$OUT"
EXPORT_VOICE_LINES="$tmp/lines.tsv" dotnet test tools/cstest/cstest.csproj --nologo -v q --filter ExportVoiceLines >/dev/null
[ -s "$tmp/lines.tsv" ] || { echo "không xuất được danh sách câu" >&2; exit 1; }

while IFS=$'\t' read -r -u 3 id text; do
  [ -n "$id" ] || continue
  if [ -n "$ONLY" ] && [[ ",$ONLY," != *",$id,"* ]]; then continue; fi
  body=$(python3 -c 'import json,sys; print(json.dumps({"text":sys.argv[1],"model_id":sys.argv[2],"language_code":"vi","voice_settings":{"stability":1.0,"similarity_boost":0.7,"style":0.0,"speed":0.95}}))' "$text" "$MODEL")
  code=$(curl -sS -m 90 -X POST "https://api.elevenlabs.io/v1/text-to-speech/$VOICE?output_format=mp3_44100_128" \
    -H "Content-Type: application/json" -d "$body" -o "$tmp/$id.mp3" -w "%{http_code}")
  [ "$code" = "200" ] || { echo "$id: HTTP $code" >&2; head -c 300 "$tmp/$id.mp3" >&2; exit 1; }
  ffmpeg -nostdin -loglevel error -y -i "$tmp/$id.mp3" -ar 22050 -ac 1 "$tmp/$id.wav"
  python3 tools/voice/flatten_pitch.py "$tmp/$id.wav" "$tmp/$id.flat.wav" "$FLAT" < /dev/null
  ffmpeg -nostdin -loglevel error -y -i "$tmp/$id.flat.wav" -af "$FX" -ar 44100 -ac 1 -b:a 96k "$OUT/$id.mp3"
  echo "$id ok"
done 3< "$tmp/lines.tsv"

# xóa file của những câu không còn trong danh sách (khi không dùng ONLY)
if [ -z "$ONLY" ]; then
  for f in "$OUT"/*.mp3; do
    id=$(basename "$f" .mp3)
    cut -f1 "$tmp/lines.tsv" | grep -qx "$id" || { rm -f "$f" "$f.meta"; echo "bỏ $id"; }
  done
fi
ls "$OUT" | grep -c mp3
