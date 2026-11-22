#!/usr/bin/env bash
# Tạo tiếng Bọ Cạp Thạch Anh (mỗi chiêu có tiếng riêng cho từng giai đoạn: tụ lực, tung chiêu, hậu tung chiêu; thêm đau và chết) bằng ElevenLabs Sound Effects vào Resources/Sfx/Scorpion/<tên>.mp3.
# ProceduralSfx.PlayFile tải "Sfx/Scorpion/<tên>" (ScorpionMoves); không có file thì dùng tiếng tổng hợp bằng mã (HoundSynth). Khóa ElevenLabs do proxy của môi trường chèn, không nằm trong repo.
# Chạy: tools/voice/generate_scorpion_sfx.sh   hoặc   ONLY=sting_hit,scorpion_hurt tools/voice/generate_scorpion_sfx.sh
set -euo pipefail
cd "$(dirname "$0")/../.."
OUT=Assets/_Project/Resources/Sfx/Scorpion
ONLY="${ONLY:-}"
tmp=$(mktemp -d); trap 'rm -rf "$tmp"' EXIT
mkdir -p "$OUT"
# tên|giây|mô tả (ngắn gọn, dứt khoát, khô, không vang)
cat > "$tmp/t.txt" <<'T'
sting_charge|0.9|a giant crystal scorpion cocking its stinger back, dry chitin clicking and creaking, a rising glassy crystal hum gathering at the tip, tense, short
sting_hit|0.7|a giant scorpion stabbing its stinger forward, a fast sharp whip crack and a hard pierce, glassy crystal chime on impact, punchy and decisive, tight tail
tailslam_raise|0.9|a giant scorpion rearing up and raising its huge heavy tail, creaking chitin and stone, a low trembling rumble building, menacing
tailslam_hit|1.0|a giant crystal scorpion slamming its tail onto a stone floor, one heavy deep boom with cracking rock and crystal shards scattering, decisive, tight tail
volley_charge|1.1|a scorpion tail arching and glowing, a rising bright crystalline whine with dry clicking chitin and faint electric crackle, tense, builds to a peak
volley_fire|0.8|three sharp crystal spikes launched one after another from a scorpion tail, three quick glassy whooshes with sharp snapping clicks, fast and punchy
burrow_dig|1.2|a giant scorpion scraping and digging into dirt and gravel, rapid claw scratching, rocks and soil shifting, heavy and gritty
burrow_travel|1.5|something huge moving fast under the ground, a rumbling gritty tunnel of soil shifting and stones cracking, low and ominous, steady
burrow_mark|0.8|a low ominous rumble swelling right under the floor, ground cracking and trembling, a warning, tense, short
burrow_burst|1.0|a giant scorpion erupting out of the ground, an explosive burst of dirt and rock with a heavy boom and a loud hissing screech, decisive and powerful
burrow_shake|0.9|a giant scorpion shaking dirt off its body, clattering chitin and falling soil and pebbles, a short breathy hiss, tired
pinch_charge|0.9|a giant scorpion opening its huge pincers wide and scraping its feet back, dry chitin creaking and clicking, a low hiss, menacing
pinch_run|1.2|a giant scorpion charging forward on eight legs, rapid heavy skittering clatter of many legs on stone, a rushing hiss, relentless
pinch_snap|0.7|two giant scorpion pincers snapping shut, a hard loud crack of chitin and a crunch, punchy and decisive
burst_charge|1.9|a giant crystal scorpion powering up, its whole armour glowing, a long rising crystalline hum with trembling stone and chitin cracking, a low menacing rumble building to a terrifying peak
burst_ring|1.2|rings of sharp crystal spikes erupting out of the floor one after another in waves, loud crystalline cracking and shattering, glassy chimes and rock grinding, decisive and powerful
burst_gasp|2.0|a giant exhausted scorpion with a cracked shell, slow raspy hissing breaths, dry chitin ticking and faint crystal fragments falling, tired
scorpion_hurt|0.6|a short sharp hiss and screech of a giant scorpion being hit, a dry chitin crack, abrupt
scorpion_death|2.0|a giant crystal scorpion dying, a long weakening rattling hiss, chitin collapsing and crystal fragments falling and chiming, ending in silence
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
