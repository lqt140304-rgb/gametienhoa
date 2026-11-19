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
slime_move_1|0.6|0|a single soft wet squelch, a gelatinous blob dragging one step across the ground, subtle, no music
slime_move_2|0.6|0|a gentle wet slurp and slide, jelly creeping forward, subtle, no music
slime_move_3|0.6|0|a small sticky squish, slime peeling off a surface, subtle, no music
slime_hop|0.9|0|a jelly blob jumping up, wet elastic boing with a squish, cartoon slime hop
slime_land|0.7|0|a heavy wet splat as a slime blob lands on stone ground, soft thud with squelch
slime_swallow|1.8|0|a slime blob swallowing a big chunk whole, wet squishy gulp with a deep glug, gelatinous
slime_dissolve|3|1|bubbling acid slime slowly dissolving and absorbing something, wet fizzing bubbling, continuous loop
slime_spit|1.0|0|a slime blob spitting something out, wet pop and a splat
T
while IFS='|' read -r -u 3 id secs loop prompt; do
  [ -n "$id" ] || continue
  if [ -n "$ONLY" ] && [[ ",$ONLY," != *",$id,"* ]]; then continue; fi
  body=$(python3 -c 'import json,sys; d={"text":sys.argv[1],"duration_seconds":float(sys.argv[2]),"prompt_influence":0.6}
if sys.argv[3]=="1": d["loop"]=True
print(json.dumps(d))' "$prompt" "$secs" "$loop")
  code=$(curl -sS -m 120 -X POST "https://api.elevenlabs.io/v1/sound-generation?output_format=mp3_44100_128" -H "Content-Type: application/json" -d "$body" -o "$tmp/$id.mp3" -w "%{http_code}")
  if [ "$code" != "200" ] && [ "$loop" = 1 ]; then
    body=$(python3 -c 'import json,sys; print(json.dumps({"text":sys.argv[1],"duration_seconds":float(sys.argv[2]),"prompt_influence":0.6}))' "$prompt" "$secs")
    code=$(curl -sS -m 120 -X POST "https://api.elevenlabs.io/v1/sound-generation?output_format=mp3_44100_128" -H "Content-Type: application/json" -d "$body" -o "$tmp/$id.mp3" -w "%{http_code}")
  fi
  [ "$code" = "200" ] || { echo "$id: HTTP $code" >&2; head -c 300 "$tmp/$id.mp3" >&2; exit 1; }
  ffmpeg -nostdin -loglevel error -y -i "$tmp/$id.mp3" -af "loudnorm=I=-20:TP=-1.5:LRA=11" -ar 44100 -ac 1 -b:a 96k "$OUT/$id.mp3"
  echo "$id ok"
done 3< "$tmp/t.txt"
