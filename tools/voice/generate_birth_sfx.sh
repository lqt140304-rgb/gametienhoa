#!/usr/bin/env bash
# Tạo hiệu ứng âm thanh cho cảnh ra đời bằng ElevenLabs Sound Effects, theo bảng BirthSfx (một nguồn duy nhất).
# Cần: ffmpeg, dotnet, quyền gọi api.elevenlabs.io (khóa có quyền Sound Effects; khóa do môi trường gắn, KHÔNG ghi trong file này).
# Chạy: tools/voice/generate_birth_sfx.sh                 (tạo tất cả, ghi đè)
#       ONLY=slam_big,alarm tools/voice/generate_birth_sfx.sh   (chỉ vài tiếng)
set -euo pipefail
cd "$(dirname "$0")/../.."
OUT=Assets/_Project/Resources/Sfx/Birth
ONLY="${ONLY:-}"
INFLUENCE="${INFLUENCE:-0.5}"   # 0..1: càng cao càng bám sát mô tả
tmp=$(mktemp -d); trap 'rm -rf "$tmp"' EXIT
mkdir -p "$OUT"
EXPORT_BIRTH_SFX="$tmp/sfx.tsv" dotnet test tools/cstest/cstest.csproj --nologo -v q --filter ExportBirthSfx >/dev/null
[ -s "$tmp/sfx.tsv" ] || { echo "không xuất được bảng tiếng" >&2; exit 1; }

while IFS=$'\t' read -r -u 3 id secs mode prompt; do
  [ -n "$id" ] || continue
  if [ -n "$ONLY" ] && [[ ",$ONLY," != *",$id,"* ]]; then continue; fi
  loop=false; [ "$mode" = loop ] && loop=true
  body=$(python3 -c 'import json,sys; d={"text":sys.argv[1],"duration_seconds":float(sys.argv[2]),"prompt_influence":float(sys.argv[3])}
if sys.argv[4]=="true": d["loop"]=True
print(json.dumps(d))' "$prompt" "$secs" "$INFLUENCE" "$loop")
  code=$(curl -sS -m 120 -X POST "https://api.elevenlabs.io/v1/sound-generation?output_format=mp3_44100_128" \
    -H "Content-Type: application/json" -d "$body" -o "$tmp/$id.mp3" -w "%{http_code}")
  if [ "$code" != "200" ] && [ "$loop" = true ]; then   # một số mô hình không nhận "loop": thử lại không có
    body=$(python3 -c 'import json,sys; print(json.dumps({"text":sys.argv[1],"duration_seconds":float(sys.argv[2]),"prompt_influence":float(sys.argv[3])}))' "$prompt" "$secs" "$INFLUENCE")
    code=$(curl -sS -m 120 -X POST "https://api.elevenlabs.io/v1/sound-generation?output_format=mp3_44100_128" \
      -H "Content-Type: application/json" -d "$body" -o "$tmp/$id.mp3" -w "%{http_code}")
  fi
  [ "$code" = "200" ] || { echo "$id: HTTP $code" >&2; head -c 300 "$tmp/$id.mp3" >&2; exit 1; }
  ffmpeg -nostdin -loglevel error -y -i "$tmp/$id.mp3" -af "loudnorm=I=-18:TP=-1.5:LRA=11" -ar 44100 -ac 1 -b:a 96k "$OUT/$id.mp3"
  echo "$id ok"
done 3< "$tmp/sfx.tsv"
ls "$OUT" | grep -c mp3
