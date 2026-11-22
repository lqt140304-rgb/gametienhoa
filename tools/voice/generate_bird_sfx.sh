#!/usr/bin/env bash
# Tạo tiếng Chim Hang Mẹ, con kền kền gió (mổ, vả cánh, bắn lông, quắp, rít gầm, bổ nhào, vỗ cánh, đau) bằng ElevenLabs Sound Effects vào Resources/Sfx/Bird/<tên>.mp3.
# ProceduralSfx.Clip tải "Sfx/Bird/<tên>" trước, không có file thì dùng tiếng tổng hợp bằng mã.
# Chạy: tools/voice/generate_bird_sfx.sh   hoặc   ONLY=bird_screech,bird_hurt tools/voice/generate_bird_sfx.sh
set -euo pipefail
cd "$(dirname "$0")/../.."
OUT=Assets/_Project/Resources/Sfx/Bird
ONLY="${ONLY:-}"
tmp=$(mktemp -d); trap 'rm -rf "$tmp"' EXIT
mkdir -p "$OUT"
# tên|giây|mô tả (ngắn gọn, dứt khoát, khô, không vang)
cat > "$tmp/t.txt" <<'T'
bird_screech|2.0|a giant vulture-like cave bird letting out one piercing harsh shriek, a terrifying raspy screech rising in pitch with a metallic edge and windy air, loud and menacing
bird_peck|0.6|a giant bird's beak snapping and pecking hard, a sharp fast clack with a short feathery whoosh, punchy, dry
bird_wing_slap|0.9|a huge feathered wing sweeping and slapping across, a heavy fast whoosh of air and feathers with a dull thump, powerful
bird_feather_volley|1.4|a giant bird flapping its wings and firing a fan of sharp feathers, a burst of whooshing air and rapid whistling feathers flying away, magical wind, fast
bird_swoop|1.2|a giant bird diving down fast to grab prey, a rising rush of wind and a harsh excited screech, then talons snapping shut, tense
bird_grab|0.9|sharp bird talons clamping onto a body, a tight scrape and squeeze with a harsh victorious cry, short and cruel
bird_flap|0.8|two huge wings beating heavily in the air, a deep powerful whoomp whoomp of feathers and wind, close and strong
bird_dive|1.5|a giant bird diving at terrifying speed, a long screaming rush of wind with a falling shriek, then a heavy impact whoosh
bird_stun_screech|2.5|a deafening ultimate screech of a giant bird, an enormous piercing shriek with a shockwave pulse, ringing metallic overtones and a swirl of wind, overwhelming
bird_wind|1.5|a swirl of magical teal wind circling around spread wings, a soft mystical whoosh with light chimes in the air, ghostly
bird_hurt|0.6|a short pained squawk of a giant bird being hit, a harsh startled cry with a puff of feathers, abrupt
T
while IFS='|' read -r -u 3 id secs prompt; do
  [ -n "$id" ] || continue
  if [ -n "$ONLY" ] && [[ ",$ONLY," != *",$id,"* ]]; then continue; fi
  body=$(python3 -c 'import json,sys; print(json.dumps({"text":sys.argv[1],"duration_seconds":float(sys.argv[2]),"prompt_influence":0.85}))' "$prompt" "$secs")
  code=$(curl -sS -m 120 -X POST "https://api.elevenlabs.io/v1/sound-generation?output_format=mp3_44100_128" -H "Content-Type: application/json" -d "$body" -o "$tmp/$id.mp3" -w "%{http_code}")
  [ "$code" = "200" ] || { echo "$id: HTTP $code" >&2; head -c 300 "$tmp/$id.mp3" >&2; exit 1; }
  # clean and bright (a bird is light), tail cut, loud
  ffmpeg -nostdin -loglevel error -y -i "$tmp/$id.mp3" -af "highpass=f=120,lowpass=f=14000,acompressor=threshold=-18dB:ratio=3:attack=5:release=80,loudnorm=I=-15:TP=-1.0:LRA=7,afade=t=out:st=$(python3 -c "print(max(0.05,$secs-0.12))"):d=0.1" -ar 44100 -ac 1 -b:a 112k "$OUT/$id.mp3"
  echo "$id ok"
done 3< "$tmp/t.txt"
