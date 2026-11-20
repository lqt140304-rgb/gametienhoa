#!/usr/bin/env bash
# Tạo tiếng kỹ năng hang (một tiếng ngắn mỗi kỹ năng chủ động) bằng ElevenLabs Sound Effects vào Resources/Sfx/Slime/skill_<id>.mp3.
# SkillBar phát "skill_" + tên gene bỏ "Gene_" (Gene_o_chan → skill_o_chan). Bị động không có tiếng.
# Chạy: tools/voice/generate_skill_sfx.sh   hoặc   ONLY=skill_o_chan tools/voice/generate_skill_sfx.sh
set -euo pipefail
cd "$(dirname "$0")/../.."
OUT=Assets/_Project/Resources/Sfx/Slime
ONLY="${ONLY:-}"
tmp=$(mktemp -d); trap 'rm -rf "$tmp"' EXIT
mkdir -p "$OUT"
# id|giây|mô tả
cat > "$tmp/t.txt" <<'T'
skill_thu_minh|0.6|a soft quick shell tuck, a light hollow clack like a small snail shell closing, clean and gentle
skill_luoi_dai|0.7|a quick light elastic whip flick then a soft snap back, clean and cute, gentle, not wet
skill_o_chan|1.0|a short deep soft croak burp with a gentle air whoomp pushing outward, round and clean, not gross
skill_phat_sang|1.2|a soft warm magical shimmer swelling, gentle glassy chime glow, calm and clean
skill_lach_ne|0.5|a quick soft whoosh sidestep, light air swish, clean and gentle
skill_khien_quang|0.9|a bright crystal shield forming, gentle glassy ring with a soft mineral clink, clean
skill_cam_gai|0.7|small crystal spikes sprouting from stone, a few light glassy tinks and a soft crunch, clean
skill_bam_vach|0.6|light quick scrabbling grip on stone, soft tiny taps, clean and gentle
skill_cham_choang|0.6|a quick sharp crystal sting tick followed by a soft dizzy glassy shimmer, clean and gentle
T
while IFS='|' read -r -u 3 id secs prompt; do
  [ -n "$id" ] || continue
  if [ -n "$ONLY" ] && [[ ",$ONLY," != *",$id,"* ]]; then continue; fi
  body=$(python3 -c 'import json,sys; print(json.dumps({"text":sys.argv[1],"duration_seconds":float(sys.argv[2]),"prompt_influence":0.75}))' "$prompt" "$secs")
  code=$(curl -sS -m 120 -X POST "https://api.elevenlabs.io/v1/sound-generation?output_format=mp3_44100_128" -H "Content-Type: application/json" -d "$body" -o "$tmp/$id.mp3" -w "%{http_code}")
  [ "$code" = "200" ] || { echo "$id: HTTP $code" >&2; head -c 300 "$tmp/$id.mp3" >&2; exit 1; }
  ffmpeg -nostdin -loglevel error -y -i "$tmp/$id.mp3" -af "highpass=f=150,lowpass=f=9000,loudnorm=I=-22:TP=-1.5:LRA=11" -ar 44100 -ac 1 -b:a 96k "$OUT/$id.mp3"
  echo "$id ok"
done 3< "$tmp/t.txt"
