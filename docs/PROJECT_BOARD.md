# Bảng việc chung (đọc trước khi sửa, cập nhật khi xong)

> **Mọi phiên và mọi agent (Claude Code, Codex, Antigravity) hoặc người đọc file này trước khi bắt đầu và cập nhật một dòng khi xong hoặc đổi việc.** Mục đích: nhiều phiên cùng làm trên `claude/unity-test-maze` mà không giẫm chân nhau. Cập nhật lần cuối: 2026-11-22.

## Quy tắc chung (bản đầy đủ nằm ở `CLAUDE.md` ở gốc repo: mọi phiên Claude Code tự đọc nó khi bắt đầu)
1. Nhánh làm việc: **`claude/unity-test-maze`** (gộp từ `claude/movement`). Luôn `git pull --no-rebase origin claude/unity-test-maze` **trước khi sửa và trước khi push**; nhóm cave "Antigravity" cũng push vào đó. Không tạo PR trừ khi chủ dự án bảo; chỉ push hai nhánh trên.
2. **Trước khi push chạy `tools/cstest/run.sh`** (cần .NET 8, phải `Passed!`); GitHub cũng tự chạy lại sau mỗi push (`.github/workflows/tests.yml`). Phần Unity không chạy được ngoài Unity: ghi "chưa kiểm chứng" và chờ chủ dự án test.
3. **Mỗi phiên sở hữu file của mình** (bảng dưới). File dùng chung chỉ **thêm một khối nhỏ của mình**, commit nhỏ, push sớm.
4. Không bao giờ commit khóa API (ElevenLabs, Meshy: nằm trong secret của môi trường). Trước mỗi lần tạo Meshy phải hỏi chủ dự án (kèm số credit).
5. Báo cáo cho chủ dự án bằng tiếng Việt; mã và chú thích mã bằng tiếng Anh. Viết test trước (TDD) cho logic thuần C#.

## Các phiên và việc đang làm (sửa lại nếu sai)
| Vai trò | Việc | File sở hữu | Trạng thái | Việc tiếp theo |
|---|---|---|---|---|
| **Phiên Golem** (phiên "làm game", đám mây) | Golem: bộ chiêu, phase, đọc người chơi, dạng rìu, khối `BossRoom`; âm thanh và nhạc Golem, kế hoạch chiều sâu trùm | `Golem*`, `GolemAxe`, `AxeFlight`, `Pure/{GolemRules,MeteorRules,AxeRules,FetchFsm,AuraRules,ShockwaveRules,RoomRules,PhaseRules,ReadRules,CircleRules,VfxRules}`, `World/BossRoom.cs`, `tools/voice/generate_golem_sfx.sh` | dạng rìu và `BossRoom` đã viết, **chưa kiểm chứng trong Unity** | chờ kết quả test phòng Golem; sau đó nhạc phase, bộ phận yếu, poise |
| **Phiên Chim Hang Mẹ** | kền kền Dreadwing: model, rig, chiêu bay, âm thanh gắn chiêu | `Bird*`, `Pure/BirdRules.cs`, `Models/Wildlife/Meshy/*Dreadwing*`, `tools/voice/generate_bird_sfx.sh` | đã theo bộ chuẩn (Vả Cánh, Rít Gầm, Lướt Gió); phần Unity chưa kiểm chứng | động tác riêng cho Vả Cánh và Rít Gầm |
| **Phiên dựng map** | Phòng trùm Đợt 1 (Golem, Chim, Bọ Cạp) & Đợt 2 (Chó Xương, Cáo Gió, Cóc Hồ Già, Rắn Dung Nham): hình học, cửa thép, builders | `Editor/BossRoom*`, `Scenes/BossRoom_*.unity`, `Prefabs/BossRooms/`, `Models/Underroot/BossRoom_*`, `tools/blender/*boss_room*`, `docs/superpowers/specs/2026-11-22-boss-rooms*` | Đợt 1 (3 phòng) đã dựng & kiểm hợp đồng trong Unity 6000.6 (PR #127); đã chốt & commit spec Đợt 2 (`2026-11-22-boss-rooms-wave2-design.md`) | đặt Golem vào `BossRoom_Golem.unity` (menu **Place Golem In The Open Boss Room**), dựng 4 phòng Đợt 2 (`BossRoom_Hound/Fox/Toad/Snake`) kèm đặt sẵn trùm (Meshy) tại `Boss_Wait` |
| **Phiên Bọ Cạp** | Bọ Cạp Thạch Anh: bộ chiêu chuẩn (đã duyệt), rig và 28 clip Blender (đi 4 hướng, ba pha mỗi chiêu, 30 khóa), nhịp trận (đi vòng, hở sườn), combo, trồi kép ≤ 66%, cột tinh thể, 26 tiếng, đặt vào phòng Bọ Cạp | `ScorpionMoves`, `CrystalColumn`, `World/ScorpionRoom.cs`, `Pure/ScorpionRules.cs`, `Editor/MeshyScorpionPrefabBuilder.cs`, `Editor/ScorpionBossRoomPlacer.cs`, `tools/blender/rig_scorpion.py`, `tools/voice/generate_scorpion_sfx.sh`, `Models/Wildlife/Meshy/*BoCapThachAnh*` | **PR #126 đang mở** (nhánh `claude/friendly-cori-3fjujb`); đã được thử một phần trong Unity (đi, gai lưng, tự nhận sát thương), phần còn lại **chưa kiểm chứng** (checklist H13) | nhạc `scorpion_battle` (khóa ElevenLabs thiếu quyền `music_generation`); chủ dự án test H13b–e; phase 66% và 33% với đọc người chơi |
| **Phiên Codex Hang 01** | AI, kỹ năng và VFX Bọ Cánh Cứng Nhỏ, Ốc Hồ, Thằn Lằn Bờ Hồ | `PreyDefense*`, `HangOneGround.cs`, `CreatureCombat.HangOne.cs`, `HangOneSkillsTests.cs`; khối tích hợp nhỏ trong AI/combat dùng chung | **PR #125**, commit `8264aae4`; 93/93 EditMode đạt, đã xem fixture Unity sáng/tối; đã sửa một lưỡi và xoắn thân ốc | đồng bộ nhánh chung; kiểm tra toàn cảnh Hang 01 và co-op hai máy |
| **Nhóm cave "Antigravity"** | hang, mê cung, thế giới | `World/`, `Maze/`, `Editor/Cavern*`, `Slime_Cavern.unity` | ngoài phạm vi các phiên trùm | (họ tự quản) |
| **Phiên "Golem Thạch Anh testing"** (máy chủ dự án) | test trong Unity, Unity MCP | tùy việc | đang chờ chủ dự án trả lời | (xem phiên đó) |

## Trạng thái từng trùm (bộ chiêu chuẩn: 2 thường, 1 xa, 1 nội tại, 1 đặc biệt, 1 ultimate, tối đa 1 combo)
| Trùm | Bộ chiêu | Phase và đọc người chơi | Phòng | Ghi chú |
|---|---|---|---|---|
| Golem Thạch Anh | xong (tay không 5 chiêu, dạng rìu 4 chiêu) | có | có (chờ đặt trùm) | lore: `docs/lore/golem-thach-anh.md` |
| Chim Hang Mẹ | xong | có | có (chưa đặt trùm) | |
| Chó Xương | xong | có | chưa | |
| Bọ Cạp Thạch Anh | xong (6 chiêu, chưa kiểm chứng đủ trong Unity) | chưa (`PhaseRules` và `MoveVfx` chỉ cho Golem, Chim, Chó Xương; Bọ Cạp có hiệu ứng ba pha riêng trong `ScorpionMoves`) | có (đặt bằng menu **Place Scorpion In The Open Boss Room**) | thiếu nhạc `scorpion_battle` |
| Cóc, Rắn Dung Nham, Cáo Gió | đề xuất trong `docs/superpowers/specs/2026-11-21-boss-standard-kit.md`, chưa duyệt | chưa | chưa | Rắn: duỗi thẳng chưa xong; Cáo: model mới chưa vào repo |

## Việc tồn đọng chưa ai nhận
- Lỗi "Collection was modified" khi Chó Xương chết (cần stack trace của chủ dự án).
- Đưa `Slime_Cavern.unity` (63 MB) sang Git LFS (đổi lịch sử; cần chủ dự án đồng ý).
- Hiệu ứng trong co-op (hiện chỉ hiện trên máy dùng chiêu).
- Đi bốn hướng cho các quái khác ngoài Golem (Chó Xương, Chim, Cáo...).
- Nhạc có lớp tăng dần theo phase; bộ phận yếu, poise, môi trường (kế hoạch: `docs/superpowers/specs/2026-11-22-boss-depth-plan.md`).

## Tài liệu chính
`docs/superpowers/specs/2026-11-21-boss-standard-kit.md` (bộ chuẩn), `docs/BOSS_ROOMS_BRIEF.md` và `docs/BOSS_ROOM_CONTRACT.md` (phòng trùm), `docs/superpowers/specs/2026-11-22-golem-axe-form-design.md` (dạng rìu), `docs/TEST_CHECKLIST_UNITY.md` (cách test trong Unity).
