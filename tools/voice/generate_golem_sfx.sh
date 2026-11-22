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
golem_step|0.6|one single very heavy footstep of a colossal stone giant on a stone floor, a deep dull thud with a grinding rock crunch, tight and short, no reverb tail
golem_leap|1.0|a colossal stone golem crouching then leaping high into the air, a deep straining grunt, rock cracking under its feet and a heavy rushing whoosh upward, powerful
golem_spikes|0.9|sharp stone spikes bursting up out of the ground all around, a loud cracking and grinding eruption of rock, debris scattering, decisive and heavy
golem_roar|2.5|a colossal stone golem bellowing one enormous earth-shaking roar, a deep thundering rumbling bellow with grinding rock in the chest, rising then booming outward like a shock wave, huge and terrifying, ends with a rocky rumble
golem_punch_wind|0.9|a colossal stone golem drawing back a huge fist, a deep gravelly effort grunt and grinding rock in the shoulder, tense, short
golem_punch_hit|0.8|a giant stone fist smashing a target, one heavy dull impact with cracking rock and a body thud, punchy and decisive, tight tail
golem_sweep|1.0|a massive stone arm sweeping across in a wide arc, a heavy deep whoosh with grinding stone and a low grunt, powerful, tight tail
golem_rush_charge|1.2|a stone golem lowering its head and building up to charge, a low rumbling growl rising with heavy stomps and grinding rock, menacing
golem_rush_run|1.8|a colossal stone golem charging forward faster and faster, rapid heavy stomps accelerating, deep rumbling growl and grinding rock, relentless
golem_rush_crash|1.4|a giant stone golem crashing head first into a stone wall, an enormous crack and boom with falling rubble, then a dazed low groan, heavy
golem_point|1.0|a stone golem pointing at its enemy and growling, a low menacing rumble with rock grinding in the chest, threatening, short
golem_land|1.2|a colossal stone golem landing from a great height, one enormous deep earth-shaking impact with the ground cracking and debris, heavy and decisive
golem_strain|0.8|a huge stone golem straining with effort to pull something heavy out of the ground, a deep guttural grunt of strain with grinding rock
golem_hurl|0.8|a stone golem hurling a long stone spear like a javelin, a deep effort grunt and a fast heavy whoosh, punchy
golem_pant|3.0|a colossal exhausted stone golem catching its breath, slow deep labored heavy breathing and low wheezing exhales with faint rock grinding, tired
golem_door|2.2|a huge rusty steel door slamming shut with a deafening metallic boom, heavy iron grinding and chains rattling, echoing in a stone chamber, final and ominous
golem_awaken|3.0|a colossal stone golem waking up, stone cracking and crumbling, a deep rising rumble and a slow low groan as huge rock limbs begin to move, heavy and ancient
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
