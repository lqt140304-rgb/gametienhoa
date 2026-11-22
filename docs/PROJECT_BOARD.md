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
| **Phiên dựng map** | Phòng trùm Đợt 1 (Golem, Chim, Bọ Cạp) & Đợt 2 (Chó Xương, Cáo Gió, Cóc Hồ Già, Rắn Dung Nham): hình học, cửa thép, builders | `Editor/BossRoom*`, `Scenes/BossRoom_*.unity`, `Prefabs/BossRooms/`, `Models/Underroot/BossRoom_*`, `tools/blender/*boss_room*`, `docs/superpowers/specs/2026-11-22-boss-rooms*` | Đợt 1 (3 phòng) đã xong (PR #127); Đợt 2 (4 phòng: Hound, Fox, Toad, Snake) đã xong Blender FBX + 16 ảnh Cycles + Editor Builders (`check_compile` đạt; **phần Scene/Prefab Unity chưa kiểm chứng** do Editor đang mở) | chủ dự án chạy menu **Evolve → Boss Rooms → Build, Validate & Capture Wave 2** trong Unity (dừng Play trước) rồi thử mục `H15` |
| **Phiên Bọ Cạp** | chưa bắt đầu (bộ chiêu chưa được duyệt) | `Scorpion*` (chưa có) | chưa bắt đầu | đề xuất bộ chiêu cho chủ dự án duyệt trước |
| **Phiên Codex Hang 01** | AI, kỹ năng và VFX Bọ Cánh Cứng Nhỏ, Ốc Hồ, Thằn Lằn Bờ Hồ | `PreyDefense*`, `HangOneGround.cs`, `CreatureCombat.HangOne.cs`, `HangOneSkillsTests.cs`; khối tích hợp nhỏ trong AI/combat dùng chung | **PR #125**, commit `8264aae4`; 93/93 EditMode đạt, đã xem fixture Unity sáng/tối; đã sửa một lưỡi và xoắn thân ốc | đồng bộ nhánh chung; kiểm tra toàn cảnh Hang 01 và co-op hai máy |
| **Nhóm cave "Antigravity"** | hang, mê cung, thế giới | `World/`, `Maze/`, `Editor/Cavern*`, `Slime_Cavern.unity` | ngoài phạm vi các phiên trùm | (họ tự quản) |
| **Phiên "Golem Thạch Anh testing"** (máy chủ dự án) | test trong Unity, Unity MCP | tùy việc | đang chờ chủ dự án trả lời | (xem phiên đó) |

## Trạng thái từng trùm (bộ chiêu chuẩn: 2 thường, 1 xa, 1 nội tại, 1 đặc biệt, 1 ultimate, tối đa 1 combo)
| Trùm | Bộ chiêu | Phase và đọc người chơi | Phòng | Ghi chú |
|---|---|---|---|---|
| Golem Thạch Anh | xong (tay không 5 chiêu, dạng rìu 4 chiêu) | có | có (chờ đặt trùm) | lore: `docs/lore/golem-thach-anh.md` |
| Chim Hang Mẹ | xong | có | có (chưa đặt trùm) | |
| Chó Xương | xong | có | có FBX + Builder (chờ chạy Unity menu) | |
| Bọ Cạp Thạch Anh | **chưa duyệt** | chưa | có (tạm) | |
| Cóc, Rắn Dung Nham, Cáo Gió | đề xuất trong `docs/superpowers/specs/2026-11-21-boss-standard-kit.md`, chưa duyệt | chưa | có FBX + Builder (chờ chạy Unity menu) | Rắn: duỗi thẳng chưa xong; Cáo: model mới chưa vào repo |

## Việc tồn đọng chưa ai nhận
- Lỗi "Collection was modified" khi Chó Xương chết (cần stack trace của chủ dự án).
- Đưa `Slime_Cavern.unity` (63 MB) sang Git LFS (đổi lịch sử; cần chủ dự án đồng ý).
- Hiệu ứng trong co-op (hiện chỉ hiện trên máy dùng chiêu).
- Đi bốn hướng cho các quái khác ngoài Golem (Chó Xương, Chim, Cáo...).
- Nhạc có lớp tăng dần theo phase; bộ phận yếu, poise, môi trường (kế hoạch: `docs/superpowers/specs/2026-11-22-boss-depth-plan.md`).

## Tài liệu chính
`docs/superpowers/specs/2026-11-21-boss-standard-kit.md` (bộ chuẩn), `docs/BOSS_ROOMS_BRIEF.md` và `docs/BOSS_ROOM_CONTRACT.md` (phòng trùm), `docs/superpowers/specs/2026-11-22-golem-axe-form-design.md` (dạng rìu), `docs/TEST_CHECKLIST_UNITY.md` (cách test trong Unity).
