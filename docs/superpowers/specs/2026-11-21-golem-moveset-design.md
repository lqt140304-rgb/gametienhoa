# Bộ chiêu của Golem Thạch Anh (đã chốt với chủ dự án 2026-11-21)

Chúa Hang Vực Nứt Tím, mô hình Meshy rig tự động (Humanoid) với clip đi và chạy của Meshy. Số là số tạm, chỉnh khi chơi. Làm theo quy trình của Chó Xương.

| Chiêu | Dữ liệu (`MovesetSeed`, khóa `golemthachanh`) | Mở khi |
|---|---|---|
| **Đập Đất** (`Slam`) | Spin: báo 1,1 s (giơ hai tay qua đầu, ngả ra sau), đập 0,2 s (hai nắm đấm bổ xuống, thân chúi), hồi 2 s, bán kính 5 m, sát thương 26, có giáp lúc báo và lúc đập, hồi chiêu 8 s | luôn |
| **Ném Đá** (`Throw`) | Throw: tay phải kéo ra sau 1 s rồi quăng; tảng đá to 0,7 m bay vòng cung 0,9 s; sát thương 16; tầm 5–18 m; hồi chiêu 6 s; vỡ tung khi rơi | luôn |
| **Giáp Tinh Thể** (`Harden`) | mới `MoveShape.Harden`: báo 0,8 s, **giữ giáp 4 s nhận 35% sát thương** (`MoveDef.DamageTakenFactor`), rồi hồi 1 s (lúc này nhận ×1,5 như mọi pha hở); hồi chiêu 18 s; hai tay co lại che ngực, lõi sáng | máu ≤ 66% |
| **Lõi Nổ** (`CoreBlast`) | Spin: báo 1,8 s (dang hai tay rộng, lõi ngực sáng dần rồi chớp), nổ 0,3 s, hồi 2,2 s, bán kính 7 m, sát thương 35, có giáp; hồi chiêu 25 s | máu ≤ 33% |

## Cách làm
- Dữ liệu chiêu và tư thế là **mã thuần có test**: `MovesetSeed`, `GolemRules` (góc tay, thân, độ sáng lõi theo pha), `MoveScheduler` (giáp giảm sát thương), test `GolemRulesTests`.
- Đi và chạy là clip Meshy (Animator). `GolemRigFx` **cộng thêm** lên xương tay (`LeftArm`, `RightArm`, cẳng tay) và cột sống (`Spine01`) theo chiêu đang ra; hướng xoay từng xương tự đo lúc chạy. Một đèn điểm ấm trong ngực là **lõi**: đập nhẹ lúc nghỉ, sáng khi nạp Lõi Nổ và khi giáp, chớp khi trúng đòn.
- Tư thế nén thân chung của `CreatureCombat` giảm còn 35% (`MovesetSeed.PoseStrength`), vì tay đã tự cử động.
- Âm thanh thật (ElevenLabs, `tools/voice/generate_golem_sfx.sh` → `Resources/Sfx/Golem/`): gằn, đập đất, ném, đá vỡ, giáp tinh thể, nạp lõi, nổ lõi, đau. Thiếu file thì dùng tiếng tổng hợp bằng mã. Hiệu ứng (`GolemFx`): bụi và vòng sóng khi đập đất, mảnh tinh thể bắn lên, bụi và vòng khi tảng đá vỡ, tia tinh thể khi giáp, vòng sáng và bụi khi lõi nổ.
- **Nhạc riêng** `Resources/Music/golem_battle.mp3` (trống nặng như bước chân khổng lồ, bass trầm, đồng trầm, 90 nhịp, `tools/voice/generate_boss_music.sh`). `BossMusic.Ping("golem_battle")`: bật khi có đòn, nhỏ dần sau 10 giây yên (cùng luật với Chó Xương). Mỗi trùm một bản.

## Chưa làm / rủi ro
- Chưa kiểm trong Unity: hướng xoay tay, hướng lệch thân tự đo; lõi sáng bằng đèn điểm nên ở nơi sáng có thể khó thấy.
- Chưa có clip vẽ riêng cho chiêu (tay chỉ xoay bằng mã trên clip đi), nên tay không cử động theo kiểu nhân vật thật.
- Chưa có tiếng bước chân nặng khi đi.
- Co-op: client chưa thấy hiệu ứng.
