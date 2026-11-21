#!/usr/bin/env bash
# Tạo tiếng Golem Thạch Anh (gằn, đập đất, ném, giáp tinh thể, nạp và nổ lõi, đá vỡ, đau) bằng ElevenLabs Sound Effects vào Resources/Sfx/Golem/<tên>.mp3.
# ProceduralSfx.Clip tải "Sfx/Golem/<tên>" trước, không có file thì dùng tiếng tổng hợp bằng mã (HoundSynth).
# Chạy: tools/voice/generate_golem_sfx.sh   hoặc   ONLY=golem_slam,golem_hurt tools/voice/generate_golem_sfx.sh
set -euo pipefail
cd "$(dirname "$0")/../.."
OUT=Assets/_Project/Resources/Sfx/Golem
ONLY="${ONLY:-}"
tmp=$(mktemp -d); trap 'rm -rf "$tmp"' EXIT
mkdir -p "$OUT"
# tên|giây|mô tả (ngắn gọn, dứt khoát, khô, không vang)
cat > "$tmp/t.txt" <<'T'
golem_grunt|0.8|a deep gravelly grunt of a colossal stone golem gathering its strength, low rumbling effort, rock grinding in the chest, short and heavy, no reverb
golem_slam|1.0|two giant stone fists slamming into a stone floor, one enormous deep boom with cracking rock and falling debris, heavy and decisive, tight tail
golem_throw|0.7|a colossal stone golem hurling a huge boulder, a deep effort grunt and a heavy fast whoosh, punchy
golem_boulder|0.8|a huge boulder crashing and shattering on stone ground, heavy crack and rock debris scattering, tight and powerful
golem_harden|1.2|crystal armour rapidly growing over a stone body, a rising chorus of glassy crystal cracking and chiming plus grinding stone, solid and decisive
golem_core_charge|1.8|a glowing energy core charging inside a stone giant, a deep rising hum building with crackling crystal and trembling rock, tense, ends at a peak
golem_core_blast|1.2|a huge energy explosion from a stone golem core, a massive deep boom with a bright crystal shatter and rocks blasting outward, decisive and powerful
golem_hurt|0.6|a short pained rumble of a stone golem being hit, cracking rock and a low grunt, abrupt and heavy
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
