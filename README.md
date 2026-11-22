# PROJECT EVOLVE

Game sinh tồn tiến hóa 3D Low Poly, co-op 4–8 người. Người chơi bắt đầu là một slime nguyên sinh và tiến hóa mở bằng cách thu thập cơ quan.

- Thiết kế chi tiết: [`docs/GDD.md`](docs/GDD.md)
- Dựng scene prototype: [`docs/PROTOTYPE_01_SETUP.md`](docs/PROTOTYPE_01_SETUP.md)
- Lộ trình: [`docs/ROADMAP.md`](docs/ROADMAP.md)
- **Cơ chế và kỹ năng của slime (bản mới nhất):** [`docs/SLIME_MECHANICS.md`](docs/SLIME_MECHANICS.md)
- Trùm Chúa Hang: quy trình Meshy và trạng thái từng trùm [`docs/MESHY_BOSSES.md`](docs/MESHY_BOSSES.md); bộ kỹ năng chuẩn [`docs/superpowers/specs/2026-11-21-boss-standard-kit.md`](docs/superpowers/specs/2026-11-21-boss-standard-kit.md); phòng trùm [`docs/BOSS_ROOMS_BRIEF.md`](docs/BOSS_ROOMS_BRIEF.md) và [`docs/BOSS_ROOM_CONTRACT.md`](docs/BOSS_ROOM_CONTRACT.md)
- Trạng thái từng mục và danh sách thử tay: [`docs/PROJECT_STATUS.md`](docs/PROJECT_STATUS.md), [`docs/TEST_CHECKLIST_UNITY.md`](docs/TEST_CHECKLIST_UNITY.md)
- **Ai đang làm gì:** [`docs/PROJECT_BOARD.md`](docs/PROJECT_BOARD.md)

## Trạng thái

Đã có project Unity (`Assets/_Project`) với các cảnh `Prototype_01`, `Underroot_Lake`, `Slime_Cavern`, `TestArena` và ba phòng trùm `BossRoom_Golem`, `BossRoom_Bird`, `BossRoom_Scorpion`. Logic thuần C# có test tự chạy ngoài Unity (hiện khoảng 590 test, xem `tools/cstest`); **mọi thứ về cảnh, prefab, menu Editor và hiển thị đều cần người thử trong Unity** (danh sách thử ở `docs/TEST_CHECKLIST_UNITY.md`).

**Trùm Chúa Hang** (mỗi trùm theo bộ chuẩn: 2 đánh thường, 1 đánh xa, 1 nội tại, 1 đặc biệt có thử thách, 1 ultimate ≤ 25% máu, tối đa 1 combo):

| Trùm | Bộ chiêu | Ghi chú |
|---|---|---|
| Golem Thạch Anh | xong (tay không và dạng rìu) | phase, đọc người chơi, phòng riêng |
| Chim Hang Mẹ | xong | phase, đọc người chơi, phòng riêng |
| Chó Xương | xong | phase, đọc người chơi |
| **Bọ Cạp Thạch Anh** | xong (Phóng Gai, Vỏ Pha Lê, Châm Choáng, Giáng Đuôi, Đào Chui, Lao Kẹp, Bùng Pha Lê) | mô hình Meshy gắn xương bằng Blender, 28 clip (đi 4 hướng, ba pha mỗi chiêu, 30 khóa), nhịp trận, phòng riêng; nhạc trận chưa có |
| Cóc Hồ Già, Rắn Dung Nham, Cáo Gió | đề xuất, chưa duyệt | |

## Công nghệ

Unity 6.6 (`6000.6.9f1`) + URP, C#, Blender (script `bpy` trong `tools/blender`), Shader Graph, Netcode for GameObjects. Âm thanh và nhạc làm bằng ElevenLabs (`tools/voice`), mô hình trùm bằng Meshy.

## Thiết lập repo

1. Cài [Git LFS](https://git-lfs.com) rồi chạy `git lfs install`. File `.blend`, `.fbx`, ảnh và âm thanh được lưu qua LFS (xem `.gitattributes`).
2. Trong Unity, vào **Project Settings → Editor**, đặt *Asset Serialization = Force Text* và *Version Control = Visible Meta Files*.
3. `.gitignore` đã loại trừ `Library/`, `Temp/`, `Logs/`, `UserSettings/`… Không commit các thư mục này.

## Cấu trúc thư mục

```
Assets/_Project/
  Scripts/            # C# (một asmdef Evolve): Abilities, Combat, Creatures, Ecology, Maze, Net, Organs,
                      #   Player, Survival, Swallow, Traits, Visuals, World ...; logic thuần ở thư mục con Pure/
  Editor/             # menu Evolve → ... (dựng prefab, cảnh, phòng trùm)
  Models/ Prefabs/    # mô hình (Meshy, Blender), prefab (quái, phòng trùm)
  Resources/          # Sfx/<trùm>/ và Music/ (ElevenLabs)
  ScriptableObjects/  # dữ liệu Dị Năng, cơ quan, loài
  Scenes/             # Prototype_01, Underroot_Lake, Slime_Cavern, TestArena, BossRoom_*
  Tests/EditMode/     # test của logic thuần
tools/
  cstest/             # chạy test logic thuần bằng .NET 8, không cần Unity: tools/cstest/run.sh
  syntaxcheck/        # kiểm cú pháp C# bằng Roslyn khi không có Unity
  blender/            # script bpy: dựng đồ, gắn xương và bake clip (ví dụ rig_scorpion.py)
  voice/              # script ElevenLabs: tiếng chiêu và nhạc trận
docs/                 # thiết kế, bảng việc, checklist test, specs/
```

## Quy ước

- **Nhánh làm việc:** các agent và người cùng làm trên `claude/unity-test-maze` (gộp từ `claude/movement`); Codex dùng nhánh `codex/*` riêng. Không đẩy thẳng lên `main`.
- **Vào `main`:** chủ dự án mở pull request từ `claude/unity-test-maze` và gộp khi test (GitHub Actions) xanh.
- Trước khi sửa và trước khi push: `git pull --no-rebase origin claude/unity-test-maze`, rồi chạy `tools/cstest/run.sh`.
- Class C# đặt tên PascalCase, mỗi file một class; ScriptableObject có hậu tố `SO`, ví dụ `AbilitySO`.

## Làm việc với nhiều agent (Claude Code, Codex, Antigravity)
Nhiều agent và người cùng làm trên repo này. Để không giẫm chân nhau:
- **Quy tắc chung:** [`AGENTS.md`](AGENTS.md) (Codex đọc trực tiếp; `CLAUDE.md` và `GEMINI.md` trỏ về nó).
- **Bảng việc chung:** [`docs/PROJECT_BOARD.md`](docs/PROJECT_BOARD.md): ai làm gì, file nào của ai, trạng thái từng trùm, việc tồn đọng. Mỗi agent **đọc trước khi làm và sửa một dòng của mình khi xong**.
- **Bước đầu tiên của mỗi phiên:** `bash tools/session_brief.sh` (in bảng việc và commit mới của phiên khác; Claude Code tự chạy, các agent khác chạy bằng tay).
- **Trước khi push:** `git pull --no-rebase origin claude/unity-test-maze`, rồi `tools/cstest/run.sh` (cần .NET 8; phải `Passed!`). GitHub tự chạy lại ở [`.github/workflows/tests.yml`](.github/workflows/tests.yml) sau mỗi push và mỗi PR.
- **Phần Unity** (cảnh, prefab, menu Editor, hiển thị) không chạy được ngoài Unity: ghi rõ "chưa kiểm chứng" và chờ chủ dự án thử.
- Không commit khóa API; hỏi chủ dự án trước mỗi lần tạo bằng Meshy.
