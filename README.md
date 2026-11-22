# PROJECT EVOLVE

Game sinh tồn tiến hóa 3D Low Poly, co-op 4–8 người (Netcode for GameObjects). Người chơi bắt đầu là một sinh vật slime nguyên sinh trong lòng đất (hệ thống hang động Underroot), sinh tồn, săn mồi, hấp thụ sinh vật và tiến hóa mở bằng cách thu thập, ghép nối các cơ quan dị biến.

---

## Hang 01 · AI, kỹ năng và VFX

Đã triển khai Bọ Cánh Cứng Nhỏ (Bật Cánh Thoát Thân, Da Dày), Ốc Hồ (Thu Mình, Bám Đá Ướt) và Thằn Lằn Bờ Hồ (Lưỡi Dài, Ngụy Trang Bờ Rêu). Lưỡi phóng từ miệng rồi rút lại; ốc xoắn đầu, đuôi và co phần thân mềm vào dưới vỏ.

- Bản review và các điểm tích hợp cho nhánh khác nằm trong [PR #125](https://github.com/lqt140304-rgb/gametienhoa/pull/125).
- Danh sách file, thông số kỹ năng và cách chạy Unity nằm trong [bàn giao Hang 01](docs/HANG-01-CREATURE-SKILLS.md).
- Đã đạt 93/93 test EditMode và xem hiệu ứng trên prefab thật trong fixture Play Mode URP ở hai mức sáng. Cần kiểm tra tiếp toàn cảnh Hang Khởi Sinh và co-op hai máy; client hiện chưa hiển thị VFX riêng của ba loài.
- Sau khi cập nhật code, chạy **Evolve → Build Wave 1 Creatures & Prefabs** để nhận controller Thu Mình mới của Ốc Hồ.

## Mục lục tài liệu

- **Thiết kế tổng thể (GDD):** [`docs/GDD.md`](docs/GDD.md)
- **Cơ chế & kỹ năng Slime (bản mới nhất):** [`docs/SLIME_MECHANICS.md`](docs/SLIME_MECHANICS.md)
- **Bảng việc chung & Phân công thời gian thực:** [`docs/PROJECT_BOARD.md`](docs/PROJECT_BOARD.md) *(Đọc trước khi làm, cập nhật khi xong)*
- **Quy tắc mọi agent (Claude Code, Codex, Antigravity):** [`AGENTS.md`](AGENTS.md)
- **Danh sách kiểm thử Unity (Manual Checklist):** [`docs/TEST_CHECKLIST_UNITY.md`](docs/TEST_CHECKLIST_UNITY.md)
- **Trạng thái từng hạng mục:** [`docs/PROJECT_STATUS.md`](docs/PROJECT_STATUS.md)
- **Lộ trình phát triển:** [`docs/ROADMAP.md`](docs/ROADMAP.md)

### Hệ thống trùm & Phòng trùm (Boss & Boss Rooms)
- **Chuẩn hợp đồng phòng trùm (GameObject names, Colliders, Bounds):** [`docs/BOSS_ROOM_CONTRACT.md`](docs/BOSS_ROOM_CONTRACT.md)
- **Bản giao việc phòng trùm Đợt 1 (Golem, Chim, Bọ Cạp):** [`docs/BOSS_ROOMS_BRIEF.md`](docs/BOSS_ROOMS_BRIEF.md)
- **Thiết kế chi tiết phòng trùm Đợt 2 (Chó Xương, Cáo Gió, Cóc Hồ Già, Rắn Dung Nham):** [`docs/superpowers/specs/2026-11-22-boss-rooms-wave2-design.md`](docs/superpowers/specs/2026-11-22-boss-rooms-wave2-design.md)
- **Quy chuẩn bộ chiêu trùm (Boss Standard Kit):** [`docs/superpowers/specs/2026-11-21-boss-standard-kit.md`](docs/superpowers/specs/2026-11-21-boss-standard-kit.md)
- **Quy trình trùm Meshy 3D:** [`docs/MESHY_BOSSES.md`](docs/MESHY_BOSSES.md)
- **Chi tiết kể chuyện & Lore:** [`docs/lore/golem-thach-anh.md`](docs/lore/golem-thach-anh.md)

---

## Trạng thái hiện tại

Dự án Unity đang trong giai đoạn phát triển tích cực trên nhánh **`claude/unity-test-maze`**:

1. **Địa hình & Mê cung hang động (Underroot & Hang 01):**
   - Sinh địa hình hang động theo thuật toán procedural (`CavernBuilder`, `UnderrootLakeBuilder`, `MazeTerrainMath`).
   - Hệ sinh thái Hang 01 hoàn thiện: Bọ Cánh Cứng Nhỏ, Ốc Hồ, Thằn Lằn Bờ Hồ kèm AI và kỹ năng tự vệ.
2. **Phòng trùm Đợt 1 (Đã hoàn thiện hình học & kiểm chứng hợp đồng Unity - PR #127):**
   - **Golem Thạch Anh ("Lò Tinh Thể"):** `BossRoom_Golem.unity` / `.prefab`. Vòm đá gắn tinh thể sáng, ánh vàng cam, cột đá to cản đường đập vỡ được, khối đá lõi cắm rìu thần (`GolemAxe_Planted`), cửa thép trượt có xích.
   - **Chim Hang Mẹ ("Tổ Kền Kền"):** `BossRoom_Bird.unity` / `.prefab`. Vòm xương hóa thạch, trần vòm cao thoáng (>30m), ụ đá đậu cao 2–6m, ánh sáng xanh băng giá lạnh, 6 clip hoạt ảnh tấn công từ Blender tích hợp.
   - **Bọ Cạp Thạch Anh ("Bãi Cát Tinh Thể"):** `BossRoom_Scorpion.unity` / `.prefab`. Vòm đá cát xói mòn, hố cát lún nguy hiểm ~8m, tảng đá thấp nấp né gai độc.
3. **Phòng trùm Đợt 2 (Đã duyệt thiết kế kiến trúc & cơ chế môi trường):**
   - Thiết kế sẵn sàng cho 4 phòng trùm: Chó Xương (*Hầm Mộ Xương*), Cáo Gió (*Vực Khe Gió*), Cóc Hồ Già (*Hồ Đầm Lầy Độc*), Rắn Dung Nham (*Miệng Vực Dung Nham*).
4. **Hệ thống chiến đấu & AI Trùm:**
   - Kiến trúc logic thuần tách biệt (`Pure/`): FSM, luật theo phase, hitbox, đọc người chơi viết theo TDD và kiểm tra tự động.
   - Khối quản trị trận đánh `BossRoom.cs`: đếm người chơi trong phòng, tự động sập cửa thép, kích hoạt trùm và điều khiển âm nhạc/hiệu ứng.

---

## Công nghệ & Yêu cầu môi trường

- **Engine:** Unity 6000.6.9f1 + Universal Render Pipeline (URP).
- **Ngôn ngữ:** C# (.NET 8 / Roslyn), Shader Graph.
- **Mô hình & Hoạt họa:** Blender 5.x (hỗ trợ chế độ chạy nền `blender -b -P`), Meshy 3D AI generator.
- **Mạng (Multiplayer):** Netcode for GameObjects.
- **Quản lý phiên bản:** Git + Git LFS (bắt buộc cho `.blend`, `.fbx`, âm thanh `.mp3`/`.wav`, và texture).

---

## Kiểm tra nhanh không cần mở Unity

Để đảm bảo code không làm gãy dự án trước khi commit/push:

1. **Biên dịch toàn bộ C# bằng trình biên dịch Unity Roslyn (Windows):**
   ```powershell
   powershell -ExecutionPolicy Bypass -File tools/cstest/check_compile.ps1
   ```
   *(Yêu cầu đã từng mở Unity một lần để sinh thư viện `Library/ScriptAssemblies`; trả về `exit: 0` khi biên dịch thành công).*

2. **Chạy bộ kiểm thử Pure Logic độc lập (Cross-platform):**
   ```bash
   bash tools/cstest/run.sh
   ```
   *(Cần .NET 8 SDK; chạy nhanh toàn bộ unit test logic độc lập trong `Assets/_Project/Scripts/**/Pure/` và phải hiển thị `Passed!`).*

3. **Kiểm tra tự động Hợp đồng phòng trùm (Unity Batchmode):**
   ```powershell
   powershell -ExecutionPolicy Bypass -File tools/unity/run_batchmode.ps1 -Method Evolve.Editor.BossRoomContractVerifier.ValidateAllRoomsBatch
   ```

---

## Cấu trúc thư mục dự án

```
Assets/_Project/
  Scripts/
    Abilities/          # Hệ thống dị năng và kỹ năng kích hoạt
    AI/                 # Hành vi sinh vật và quái hoang dã
    Combat/             # Hệ thống chiến đấu, VFX, SFX, Boss Movesets
      Pure/             # Logic thuần toán học/FSM (kiểm thử độc lập không phụ thuộc Unity)
    Core/               # Vòng đời trò chơi, cấu hình chung
    Organs/             # Hệ thống cơ quan tiến hóa và biến đổi cơ thể Slime
    Networking/         # Đồng bộ hóa vị trí, trạng thái qua Netcode
    Wildlife/           # Sinh thái hang động và sinh vật săn mồi
    World/              # Khối BossRoom, cửa hang, cơ chế mê cung
  Scenes/               # Cảnh game: BossRoom_Golem, BossRoom_Bird, BossRoom_Scorpion, Prototype_01...
  Prefabs/              # Prefab phòng trùm, trùm, Slime, cơ quan, đạo cụ (Props)
  Models/               # Mô hình 3D (Meshy FBX, Underroot OBJ/FBX)
  Resources/            # SFX âm thanh, VFX hiệu ứng hạt, Material/Shader
  Settings/             # Cấu hình URP, ánh sáng, Render Pipeline
  Tests/                # EditMode & PlayMode test cases
  Editor/               # Tool sinh map procedural, menu kiểm tra hợp đồng phòng trùm

docs/                   # Toàn bộ tài liệu GDD, spec thiết kế, lore và hướng dẫn
tools/
  blender/              # Script Python dựng mesh phòng trùm và rig hoạt ảnh tự động
  cstest/               # Công cụ test logic thuần và kiểm tra biên dịch Roslyn
  session_brief.sh      # Script tóm tắt trạng thái phiên làm việc đa agent
```

---

## Quy ước phát triển & Hợp tác đa Agent

Dự án áp dụng mô hình phát triển phối hợp giữa con người và nhiều AI agent (Claude Code, Codex, Antigravity). Mọi thành viên cần tuân thủ nghiêm ngặt:

1. **Quy tắc gốc duy nhất:** Đọc kỹ [`AGENTS.md`](AGENTS.md) trước khi bắt đầu.
2. **Nhánh làm việc tập trung:** Tất cả cùng làm việc trên nhánh **`claude/unity-test-maze`** (gộp từ `claude/movement`). Codex sử dụng nhánh riêng `codex/*`. Không đẩy code trực tiếp lên `main`.
3. **Đồng bộ liên tục:** Luôn chạy:
   ```bash
   git pull --no-rebase origin claude/unity-test-maze
   ```
   **ngay trước khi sửa** và **ngay trước khi push**.
4. **Bảng phân công công việc:** Xem [`docs/PROJECT_BOARD.md`](docs/PROJECT_BOARD.md) để biết ai sở hữu file nào và trạng thái hiện tại. Sau khi hoàn thành hoặc đổi việc, cập nhật ngắn gọn dòng của phiên mình trong bảng.
5. **Đầu phiên làm việc:** Chạy `bash tools/session_brief.sh` để nắm bắt các thay đổi mới nhất từ các phiên khác.
6. **Bảo toàn phân quyền (Ownership):** Chỉ sửa các file thuộc phạm vi phiên mình đảm nhận. Không tự ý ghi đè hoặc can thiệp vào file của phiên khác.
7. **Bảo mật & Tài nguyên:** Tuyệt đối không commit API Key (Meshy, ElevenLabs) lên repo. Hỏi ý kiến chủ dự án trước mỗi lần gọi API tạo asset 3D.
8. **Ngôn ngữ:** Giao tiếp, tài liệu và báo cáo bằng **tiếng Việt**; mã nguồn và chú thích bằng **tiếng Anh**.
