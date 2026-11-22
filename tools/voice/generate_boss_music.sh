#!/usr/bin/env bash
# Tạo nhạc chiến đấu từng trùm bằng ElevenLabs Music vào Resources/Music/<tên>.mp3 (BossMusic.Ping("<tên>") phát). Mỗi bản 60 giây rồi nối đuôi hòa vào đầu để lặp liền mạch (56 giây).
# Chạy: tools/voice/generate_boss_music.sh   hoặc   ONLY=golem_battle tools/voice/generate_boss_music.sh
set -euo pipefail
cd "$(dirname "$0")/../.."
OUT=Assets/_Project/Resources/Music
ONLY="${ONLY:-}"
tmp=$(mktemp -d); trap 'rm -rf "$tmp"' EXIT
mkdir -p "$OUT"
cat > "$tmp/t.txt" <<'T'
golem_battle|slow crushing epic boss battle music for a fantasy cave game against a colossal crystal stone golem: massive thunderous low drums hitting on every beat like footsteps of a giant, deep pounding bass drone, grinding low brass, slow heavy rhythm around 90 bpm, tense orchestral strings tremolo, rumbling choir, sparse glassy crystal chimes, menacing and monumental, instrumental, no vocals, loopable, no fade in, no fade out
bird_battle|dark fantasy boss battle music for a cave game against a giant magical vulture, a relentless airy chase: fast urgent war drums, shrieking high violins and soaring strings swirling like wind, fluttering arpeggios, ominous brass stabs, a wordless ghostly choir, rising tension that never lets go, driving and dangerous, loopable
T
while IFS='|' read -r -u 3 id prompt; do
  [ -n "$id" ] || continue
  if [ -n "$ONLY" ] && [[ ",$ONLY," != *",$id,"* ]]; then continue; fi
  body=$(python3 -c 'import json,sys; print(json.dumps({"prompt":sys.argv[1],"music_length_ms":60000}))' "$prompt")
  code=$(curl -sS -m 300 -X POST "https://api.elevenlabs.io/v1/music?output_format=mp3_44100_128" -H "Content-Type: application/json" -d "$body" -o "$tmp/$id.mp3" -w "%{http_code}")
  [ "$code" = "200" ] || { echo "$id: HTTP $code" >&2; head -c 300 "$tmp/$id.mp3" >&2; exit 1; }
  ffmpeg -nostdin -loglevel error -y -i "$tmp/$id.mp3" -ar 44100 -ac 2 "$tmp/$id.wav"
  ffmpeg -nostdin -loglevel error -y -i "$tmp/$id.wav" -ss 4 -t 56 "$tmp/a.wav"
  ffmpeg -nostdin -loglevel error -y -i "$tmp/$id.wav" -ss 0 -t 4 "$tmp/b.wav"
  ffmpeg -nostdin -loglevel error -y -i "$tmp/a.wav" -i "$tmp/b.wav" -filter_complex "[0:a][1:a]acrossfade=d=4:c1=tri:c2=tri,loudnorm=I=-20:TP=-1.5:LRA=9" -ar 44100 -b:a 112k "$OUT/$id.mp3"
  echo "$id ok"
done 3< "$tmp/t.txt"
