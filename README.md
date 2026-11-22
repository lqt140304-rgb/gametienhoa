# PROJECT EVOLVE

Game sinh tồn tiến hóa 3D Low Poly, co-op 4–8 người. Người chơi bắt đầu là một slime nguyên sinh và tiến hóa mở bằng cách thu thập cơ quan.

- Thiết kế chi tiết: [`docs/GDD.md`](docs/GDD.md)
- Dựng scene prototype: [`docs/PROTOTYPE_01_SETUP.md`](docs/PROTOTYPE_01_SETUP.md)
- Lộ trình: [`docs/ROADMAP.md`](docs/ROADMAP.md)
- **Cơ chế và kỹ năng của slime (bản mới nhất):** [`docs/SLIME_MECHANICS.md`](docs/SLIME_MECHANICS.md)
- Trùm Chúa Hang làm bằng Meshy, quy trình và trạng thái: [`docs/MESHY_BOSSES.md`](docs/MESHY_BOSSES.md)
- Trạng thái từng mục và danh sách thử tay: [`docs/PROJECT_STATUS.md`](docs/PROJECT_STATUS.md), [`docs/TEST_CHECKLIST_UNITY.md`](docs/TEST_CHECKLIST_UNITY.md)
- **Trạng thái:** đã có project Unity, đang triển khai và kiểm thử gameplay, sinh vật, VFX và co-op.

## Hang 01 · AI, kỹ năng và VFX

Đã triển khai Bọ Cánh Cứng Nhỏ (Bật Cánh Thoát Thân, Da Dày), Ốc Hồ (Thu Mình, Bám Đá Ướt) và Thằn Lằn Bờ Hồ (Lưỡi Dài, Ngụy Trang Bờ Rêu). Lưỡi phóng từ miệng rồi rút lại; ốc xoắn đầu, đuôi và co phần thân mềm vào dưới vỏ.

- Bản review và các điểm tích hợp cho nhánh khác nằm trong [PR #125](https://github.com/lqt140304-rgb/gametienhoa/pull/125).
- Danh sách file, thông số kỹ năng và cách chạy Unity nằm trong [bàn giao Hang 01](docs/HANG-01-CREATURE-SKILLS.md).
- Đã đạt 93/93 test EditMode và xem hiệu ứng trên prefab thật trong fixture Play Mode URP ở hai mức sáng. Cần kiểm tra tiếp toàn cảnh Hang Khởi Sinh và co-op hai máy; client hiện chưa hiển thị VFX riêng của ba loài.
- Sau khi cập nhật code, chạy **Evolve → Build Wave 1 Creatures & Prefabs** để nhận controller Thu Mình mới của Ốc Hồ.

## Công nghệ

Unity 6000.6.9f1 + URP, C#, Blender, Shader Graph, Netcode for GameObjects.

## Thiết lập repo

1. Cài [Git LFS](https://git-lfs.com) rồi chạy `git lfs install`. File `.blend`, `.fbx`, ảnh và âm thanh được lưu qua LFS (xem `.gitattributes`).
2. Trong Unity, vào **Project Settings → Editor**, đặt *Asset Serialization = Force Text* và *Version Control = Visible Meta Files*.
3. `.gitignore` đã loại trừ `Library/`, `Temp/`, `Logs/`, `UserSettings/`… Không commit các thư mục này.

## Cấu trúc thư mục dự kiến

```
Assets/_Project/
  Scripts/            # C#: Core, Survival, Abilities, Organs, Networking
  Art/                # Models, Materials, Shaders
  Prefabs/
  ScriptableObjects/  # Dữ liệu Dị Năng, cơ quan
  Scenes/             # Prototype_01
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
