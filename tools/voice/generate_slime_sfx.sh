#!/usr/bin/env bash
# Tạo tiếng slime (di chuyển, nhảy, nuốt, hòa tan, nhả) bằng ElevenLabs Sound Effects vào Resources/Sfx/Slime/<id>.mp3.
# Chạy: tools/voice/generate_slime_sfx.sh   hoặc   ONLY=slime_hop tools/voice/generate_slime_sfx.sh
set -euo pipefail
cd "$(dirname "$0")/../.."
OUT=Assets/_Project/Resources/Sfx/Slime
ONLY="${ONLY:-}"
tmp=$(mktemp -d); trap 'rm -rf "$tmp"' EXIT
mkdir -p "$OUT"
# id|giây|lặp|mô tả
cat > "$tmp/t.txt" <<'T'
slime_move_1|0.5|0|a tiny soft rubbery pat, a cute muted bloop, clean and gentle, dry, not wet
slime_move_2|0.5|0|a soft light bubble tap, delicate and cute, gentle, clean, not wet
slime_move_3|0.5|0|a small soft cushion pat with a faint rubber squeak, gentle and clean, not wet
slime_hop|0.7|0|a cute light bouncy boing, soft rubber ball hop, clean, cheerful and gentle
slime_land|0.5|0|a soft muffled cushion thud, gentle pat on the ground, short and clean, not wet
slime_swallow|1.2|0|a single gentle soft gulp, one clean round water bubble glup, cute and calm, not gross
slime_dissolve|3|1|very quiet low soft bubbling, gentle muffled water simmering, warm and calm, no high pitch, no hiss, loop
slime_spit|0.6|0|a light soft bubble pop, a clean cute pop, gentle
T
while IFS='|' read -r -u 3 id secs loop prompt; do
  [ -n "$id" ] || continue
  if [ -n "$ONLY" ] && [[ ",$ONLY," != *",$id,"* ]]; then continue; fi
  body=$(python3 -c 'import json,sys; d={"text":sys.argv[1],"duration_seconds":float(sys.argv[2]),"prompt_influence":0.75}
if sys.argv[3]=="1": d["loop"]=True
print(json.dumps(d))' "$prompt" "$secs" "$loop")
  code=$(curl -sS -m 120 -X POST "https://api.elevenlabs.io/v1/sound-generation?output_format=mp3_44100_128" -H "Content-Type: application/json" -d "$body" -o "$tmp/$id.mp3" -w "%{http_code}")
  if [ "$code" != "200" ] && [ "$loop" = 1 ]; then
    body=$(python3 -c 'import json,sys; print(json.dumps({"text":sys.argv[1],"duration_seconds":float(sys.argv[2]),"prompt_influence":0.75}))' "$prompt" "$secs")
    code=$(curl -sS -m 120 -X POST "https://api.elevenlabs.io/v1/sound-generation?output_format=mp3_44100_128" -H "Content-Type: application/json" -d "$body" -o "$tmp/$id.mp3" -w "%{http_code}")
  fi
  [ "$code" = "200" ] || { echo "$id: HTTP $code" >&2; head -c 300 "$tmp/$id.mp3" >&2; exit 1; }
  af="highpass=f=150,lowpass=f=9000,loudnorm=I=-21:TP=-1.5:LRA=11"
  [ "$id" = slime_dissolve ] && af="highpass=f=120,lowpass=f=3500,loudnorm=I=-30:TP=-3:LRA=7"
  ffmpeg -nostdin -loglevel error -y -i "$tmp/$id.mp3" -af "$af" -ar 44100 -ac 1 -b:a 96k "$OUT/$id.mp3"
  echo "$id ok"
done 3< "$tmp/t.txt"
