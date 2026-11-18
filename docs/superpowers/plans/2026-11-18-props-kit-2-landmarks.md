# Bộ vật thể Blender, bước 2: khúc gỗ mục, nấm khổng lồ, cung rễ: Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Thay ba điểm nhấn dựng bằng code trong `LakeLandmarks` (khúc gỗ mục rỗng khu 03, cụm nấm khổng lồ khu 04, cung rễ lối vào khu 06) bằng vật thể Blender chi tiết hơn, có va chạm thật, đặt đúng chỗ cũ.

**Architecture:** Script Blender `tools/blender/build_landmarks.py` dựng các FBX (mesh hiển thị + `<tên>_COL`) dùng lại `kit_common.py` và dây chuyền đã có (`PropInstaller`, `PropImportPostprocessor`, shader `RockVertexColor`). Công cụ Editor mới `Editor/LakeLandmarkProps.cs` (menu **Evolve → Dress Lake: Landmark Props**, bước 16/16 của Rebuild Lake) xoá ba object code cũ theo tên rồi đặt bản Blender ở cùng vị trí; không sửa `LakeLandmarks.cs` (file của phiên trang trí).

**Tech Stack:** Blender `bpy` (Python), FBX, Unity 6.6 URP, C#, EditMode tests.

**Spec:** `docs/superpowers/specs/2026-11-18-blender-props-kit-design.md` (mục 5, 8, 9; vật thể thay thế bản dựng bằng code đã chốt).

## Global Constraints

- Phong cách: bán thực, fantasy huyền bí, phát quang (không ảnh chụp thật); màu đỉnh trong Blender dùng cùng quy ước với đá (shader `RockVertexColor` đổi sRGB sang tuyến tính).
- Va chạm đơn giản và đúng: khúc gỗ **rỗng, hai đầu hở** (slime chui qua được; collider là mesh ống không lồi, không lấp lòng); nấm: thân rắn, mũ không va chạm nếu cao hơn 2,5 m; cung rễ: hai chân rắn, lối đi giữa rộng ≥ 2,2 m (đủ slime cỡ tối đa BodyScale 1,5) và cao 8 m.
- Kích thước theo `LandmarkLayout` và cũ: gỗ dài 9 m, bán kính 0,95 m, góc 25°; nấm cao 7 / 5,5 / 4,5 m + loại nhỏ 0,8–3,5 m; cung rễ rộng 4 m cao 8 m.
- Số mặt: gỗ ≤ 6.000, mỗi nấm lớn ≤ 4.000, cung rễ ≤ 10.000; va chạm ≤ 300 mặt mỗi vật.
- Phiên trang trí đã dựng bản code: menu mới chỉ **thay** (xoá "03 Rotting Log", "04 Giant Mushrooms", "06 Root Arch" rồi đặt bản Blender); chạy lại Rebuild Lake thì bước này chạy sau `Dress Lake: Landmarks` nên luôn thay lại.
- Không sửa model nhân vật, sinh vật, cơ quan (phiên khác làm). FBX và ảnh không vào git (Git LFS bị chặn): FBX nằm dạng `tools/blender/out/*.fbx.b64` và `PropInstaller` cài.
- Đèn nấm của `LakeLook` tìm renderer tên bắt đầu "Giant Caps": mũ nấm Blender phải đặt tên `Giant Caps_N` (hoặc `LakeLook` nhận cả tên mới).
- Không `git add .`; giữ dòng netcode trong `Packages/manifest.json`. Agent không có Unity: ảnh render Blender gửi chủ dự án duyệt, rồi chủ dự án gửi ảnh Unity.

## Review Focus

- Slime không chui qua được khúc gỗ (collider lấp lòng) hoặc kẹt ở cửa ống.
- Cung rễ chắn lối đi vào khu 06 (phải đi qua được, kể cả slime cỡ tối đa).
- Chạy menu hai lần: không nhân đôi, không xoá nhầm object khác tên gần giống.
- Thiếu FBX hoặc thiếu object cũ: báo rõ, không ném lỗi, không xoá bản code cũ khi chưa đặt được bản mới.
- Đèn nấm: vẫn tìm thấy mũ nấm mới.

---

### Task 1: Dựng vật thể trong Blender

**Files:**
- Create: `tools/blender/build_landmarks.py`, `tools/blender/test_landmarks.py`
- Modify: `tools/blender/kit_common.py` (thêm hàm dùng chung nếu cần), `tools/blender/README.md`
- Output (không vào git): `Assets/_Project/Models/Underroot/{Log_Hollow,Mushroom_Giant_0..2,Mushroom_Small_0..3,RootArch}.fbx`, ảnh xem trước; vào git dạng `tools/blender/out/<tên>.fbx.b64`.

**Interfaces:**
- Produces:
  - `build_landmarks.build_log(seed: int) -> tuple[object, object]` (ống rỗng dài 9 m, bán kính ngoài 0,95 m, thành dày 0,15 m, cong nhẹ, hai đầu hở; kèm nấm tầng, rêu, hai gốc cành gãy; collider `Log_Hollow_COL` là mesh ống ≤ 300 mặt);
  - `build_landmarks.build_mushroom(seed: int, height: float, kind: str) -> tuple[object, object]` (`kind` "giant" hoặc "small"; thân loe chân + mũ vòm dày, tên đối tượng mũ `Giant Caps_<seed>`; collider thân bằng hình trụ thấp mặt);
  - `build_landmarks.build_root_arch(seed: int) -> tuple[object, object]` (cung rễ nhiều tầng rộng 4 m cao 8 m, rễ phụ rủ; collider hai chân).
  - Lệnh: `python -I tools/blender/build_landmarks.py --out Assets/_Project/Models/Underroot --previews docs/art/previews`.

- [ ] **Step 1: Test (thất bại).** `test_landmarks.py`: `Log_Hollow` kích thước ≈ 9 × 1,9 × 1,9 m ± 10%; mesh có lòng rỗng (một tia từ đầu này sang đầu kia dọc trục không cắt mặt nào: dùng `ray_cast`); số mặt ≤ 6.000; `_COL` ≤ 300 mặt và **không lấp lòng** (tia dọc trục không cắt); mỗi `Mushroom_Giant_k` cao ≈ 7 / 5,5 / 4,5 m ± 10%, có đối tượng mũ tên `Giant Caps_*`, mũ cao hơn 2,5 m không nằm trong `_COL`; `RootArch` rộng ≈ 4 m (khoảng trống giữa hai chân ≥ 2,2 m ở độ cao 0–2 m) và cao ≈ 8 m ± 10%; mọi mesh đáy tại z = 0.
- [ ] **Step 2:** Chạy `<venv>/bin/python -I tools/blender/test_landmarks.py`: FAIL (chưa có hàm). Venv: `python3 -m venv <thư mục ngoài repo> && pip install bpy` (Python 3.13 trở xuống; bpy 5.3).
- [ ] **Step 3:** Cài đặt ba hàm theo chữ ký (modifier, nhiễu, `Skin`/`Curve` tuỳ người làm); màu đỉnh: gỗ nâu sẫm và rêu, nấm kem hoặc cam nhạt với đốm sáng ở mũ, rễ nâu sẫm.
- [ ] **Step 4:** Chạy test: PASS; xuất FBX, ảnh xem trước (trước, bên, trên); đọc ảnh, tự nhận xét hình, chỉnh nếu thô.
- [ ] **Step 5:** Tạo `.fbx.b64` vào `tools/blender/out/`; commit script, test, `.b64`: `feat(props): Blender log, giant mushrooms and root arch`.

### Task 2: Cài FBX mới và nhận tên mũ nấm cho đèn

**Files:**
- Modify: `Assets/_Project/Editor/PropInstaller.cs` (danh sách tên cần cài), `Assets/_Project/Editor/LakeLook.cs` (nhận renderer tên bắt đầu "Giant Caps")
- Test: `Assets/_Project/Tests/EditMode/PropNamingTests.cs` (thêm nếu có hàm thuần mới)

**Interfaces:**
- Consumes: tên FBX ở Task 1.
- Produces: `PropInstaller.EnsureLandmarks() -> int` (ghi các `.fbx` còn thiếu từ `.b64`); `PropInstaller.EnsureRocks` giữ nguyên.

- [ ] **Step 1:** Thêm `EnsureLandmarks` cho 8 tên (`Log_Hollow`, `Mushroom_Giant_0..2`, `Mushroom_Small_0..3`... theo số lượng thật), dùng chung hàm ghi file.
- [ ] **Step 2:** Biên dịch sạch; commit `feat(props): install landmark FBX files`.

### Task 3: Menu "Dress Lake: Landmark Props"

**Files:**
- Create: `Assets/_Project/Editor/LakeLandmarkProps.cs`
- Modify: `Assets/_Project/Editor/LakeRebuildAll.cs` (thêm "16/16 Điểm nhấn Blender (Dress Lake: Landmark Props)" sau Light and Water Look? thứ tự: **trước** Light and Water Look để đèn nấm thấy mũ mới; đổi bộ đếm thành `/16`)

**Interfaces:**
- Consumes: `LandmarkLayout.LogCenter/LogAngle/LogLength`, `LandmarkLayout.GiantMushrooms`, `ZoneLayout.HuntEdge/NorthEntry` (cùng công thức `at` như `LakeLandmarks.RootArch`), `LakeGround.GroundY(x, z)`, `PropInstaller.EnsureLandmarks`, FBX Task 1; `PropImportPostprocessor` (va chạm, vật liệu).
- Produces: `LakeLandmarkProps.Dress()` (`[MenuItem("Evolve/Dress Lake: Landmark Props")]`): thiếu một FBX nào thì cảnh báo và **không** xoá bản cũ; đủ thì xoá "03 Rotting Log", "04 Giant Mushrooms", "06 Root Arch" (nếu có) rồi đặt: gỗ tại `LogCenter` hướng `LogAngle`, chìm nhẹ vào đất; nấm lớn tại tâm cụm và hai điểm (6,5; 2,5) và (-3; 6) so với tâm, nấm nhỏ rải 14 điểm tất định (hạt giống 4040) cách đường mòn ≥ 5 m; cung rễ tại điểm `at` hướng theo lối vào; đèn dưới mũ nấm (như cũ: một đèn điểm vàng nhạt tại tâm cụm, cao 3 m); tất cả dưới object cha `Lake Landmark Props (Dressed)` (chạy lại thì thay); dùng `EnsureCollision` như `LakeProps` (collider `_COL` không vẽ, thiếu thì dùng chính mesh).

- [ ] **Step 1:** Cài đặt `Dress()`; vị trí và hướng suy từ các hằng số trên, độ cao `LakeGround.GroundY`.
- [ ] **Step 2:** Thêm bước vào `LakeRebuildAll` trước Light and Water Look.
- [ ] **Step 3:** Biên dịch sạch; test cũ vẫn pass.
- [ ] **Step 4:** Commit `feat(props): Dress Lake: Landmark Props replaces the code-built log, mushrooms and arch`.

### Task 4: Tài liệu, checklist, cổng duyệt

**Files:**
- Modify: `docs/PROJECT_STATUS.md` (mục 19), `docs/ROADMAP.md`, `tools/blender/README.md`

- [ ] **Step 1:** Ghi: ba vật thể, quy ước tên, cách cài FBX (`PropInstaller`), thứ tự menu (Landmark Props chạy sau Landmarks, trước Light and Water Look), việc phiên trang trí không cần sửa `LakeLandmarks.cs`.
- [ ] **Step 2:** Checklist: (1) Console sạch, Test Runner xanh; (2) chạy menu: các object cũ biến mất, bản Blender xuất hiện đúng chỗ (khu 03 khúc gỗ, khu 04 cụm nấm, khu 06 cung rễ); (3) chui qua khúc gỗ được từ hai đầu; (4) nấm lớn: đi vào thân bị chặn, đi dưới mũ cao (≥ 2,5 m) không bị chặn; (5) đi qua cung rễ vào khu 06 với slime cỡ tối đa (nhiều cơ quan); (6) chạy menu hai lần: không nhân đôi; (7) FPS bằng PerfOverlay; (8) đèn nấm vẫn có; (9) chụp ảnh gửi tôi.
- [ ] **Step 3:** Commit `docs(props): landmark props checklist`.

## Tự rà soát

- Phủ spec: vật thể thay thế đã chốt (T3), va chạm (T1, T3), phong cách (T1). Ngoài plan này: vách rìa, rễ buông mái, rễ bò thấp (plan sau).
- Chữ ký nhất quán: `build_log/build_mushroom/build_root_arch`, `PropInstaller.EnsureLandmarks`, `LakeLandmarkProps.Dress`.
- Tỉ lệ: Task 1 (hình dạng Blender) là task lớn nhất và là nơi cần duyệt hình.
