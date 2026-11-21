#!/usr/bin/env bash
# Tạo tiếng Chó Xương (gầm, hú, đập đất, hàm đớp, lao, vung vuốt, phun xương, xương vỡ, đau) bằng ElevenLabs Sound Effects vào Resources/Sfx/Hound/<tên>.mp3.
# ProceduralSfx.Clip tải "Sfx/Hound/<tên>" trước, không có file thì dùng tiếng tổng hợp bằng mã (HoundSynth).
# Chạy: tools/voice/generate_hound_sfx.sh   hoặc   ONLY=hound_roar,hound_thud tools/voice/generate_hound_sfx.sh
set -euo pipefail
cd "$(dirname "$0")/../.."
OUT=Assets/_Project/Resources/Sfx/Hound
ONLY="${ONLY:-}"
tmp=$(mktemp -d); trap 'rm -rf "$tmp"' EXIT
mkdir -p "$OUT"
# tên|giây|mô tả (ngắn gọn, dứt khoát, khô, không vang)
cat > "$tmp/t.txt" <<'T'
hound_growl_short|0.9|a low guttural dry rasping growl of a huge skeletal undead hound, short, menacing and throaty, hollow with a faint bone rattle, close up, no reverb
hound_roar|1.6|a huge powerful hollow roar of a giant undead skeletal beast, deep throaty bellow with a dry rattle of bones, loud, decisive and heavy, no echo
hound_howl|2.2|a long eerie hollow howl of a giant undead skeleton hound, starting low and rising, ghostly but powerful and rough, ends sharply
hound_thud|0.7|a very heavy body slamming onto a stone floor, one deep punchy impact with a dry bone rattle, tight and powerful, no reverb tail
hound_snap|0.5|the jaws of a skeletal beast snapping shut, one hard sharp bone clack, dry and punchy, decisive
hound_lunge|0.8|a heavy fast whoosh of a large beast leaping through the air with a short throaty snarl burst and a dry bone rattle, punchy
hound_swipe|0.5|a fast heavy claw swipe, a sharp air slash with scraping bone claws, decisive and short
hound_spit|0.6|a bone shard launched at speed, a hard dry crack and a fast sharp whistling whoosh, punchy
hound_shatter|0.5|a bone shard shattering against stone, dry sharp cracking and splintering, tight
hound_hurt|0.6|a short angry dry snarl-yelp of a skeletal hound being hit, hollow and rasping, abrupt
T
while IFS='|' read -r -u 3 id secs prompt; do
  [ -n "$id" ] || continue
  if [ -n "$ONLY" ] && [[ ",$ONLY," != *",$id,"* ]]; then continue; fi
  body=$(python3 -c 'import json,sys; print(json.dumps({"text":sys.argv[1],"duration_seconds":float(sys.argv[2]),"prompt_influence":0.85}))' "$prompt" "$secs")
  code=$(curl -sS -m 120 -X POST "https://api.elevenlabs.io/v1/sound-generation?output_format=mp3_44100_128" -H "Content-Type: application/json" -d "$body" -o "$tmp/$id.mp3" -w "%{http_code}")
  [ "$code" = "200" ] || { echo "$id: HTTP $code" >&2; head -c 300 "$tmp/$id.mp3" >&2; exit 1; }
  # keep the low end (it carries the weight), cut the tail hard, loud and punchy
  ffmpeg -nostdin -loglevel error -y -i "$tmp/$id.mp3" -af "highpass=f=45,lowpass=f=11000,acompressor=threshold=-18dB:ratio=3:attack=5:release=80,loudnorm=I=-15:TP=-1.0:LRA=7,afade=t=out:st=$(python3 -c "print(max(0.05,$secs-0.12))"):d=0.1" -ar 44100 -ac 1 -b:a 112k "$OUT/$id.mp3"
  echo "$id ok"
done 3< "$tmp/t.txt"
