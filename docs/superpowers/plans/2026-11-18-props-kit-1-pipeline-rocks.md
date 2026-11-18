# Bộ vật thể Blender, bước 1: dây chuyền và nhóm đá: Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Dựng dây chuyền Blender (`bpy` không giao diện) → FBX có khối va chạm → Unity, và làm nhóm đầu tiên (6 loại đá) để chủ dự án duyệt hình và va chạm.

**Architecture:** Script Python `tools/blender/` sinh mesh hiển thị và mesh va chạm `<tên>_COL` cho từng đá, xuất FBX vào `Assets/_Project/Models/Underroot/` cùng ảnh render xem trước. Trong Unity, `PropImportPostprocessor` gắn `MeshCollider` từ mesh `_COL`. `PropPlacement` (C# thuần, có test) quyết định chỗ đặt; `Editor/LakeProps.cs` đặt đá theo từng khu.

**Tech Stack:** Blender `bpy` (Python, pip), FBX, Unity 6.6 URP, C#, EditMode tests.

**Spec:** `docs/superpowers/specs/2026-11-18-blender-props-kit-design.md` (mục 5, 8, 9).

## Global Constraints

- Nhập bằng **FBX**; mỗi vật có mesh hiển thị `<tên>` và mesh va chạm `<tên>_COL` (vài chục tới vài trăm mặt); đá 400–2.000 mặt.
- Bán thực kiểu fantasy huyền bí, không ảnh chụp thật; đá có mảng rêu.
- Slime đi vòng, không leo vật lớn: tảng cao > 0,3 m có vách dốc; đá ≤ 0,3 m là bậc bước qua được. Không đặt đá chắn đường mòn (cách ≥ 3 m), nơi trú, tổ, điểm xuất hiện (≥ 6 m); không đặt trong nước sâu hơn 0,5 m.
- Tổng cảnh ≤ 2.000 vật thể (toàn bộ các nhóm); bước này ≤ 500 đá.
- Agent không có Unity: kiểm bằng ảnh render Blender; hình trong Unity do chủ dự án gửi. Duyệt ảnh và va chạm của nhóm đá trước khi làm nhóm khác.
- Thay thế (đã chốt): đá do `LakeGround` dựng bằng code sẽ bị gỡ sau khi chủ dự án duyệt đá Blender; việc gỡ nằm trong `Editor/LakeGround.cs` (file của phiên trang trí, báo phiên đó).
- Không `git add .`; giữ dòng netcode trong `Packages/manifest.json`.
- Phụ thuộc: bước 0.6d (A) đã vào `main` (`ZoneLayout.Zones`, `WaterMap.BedDepth`); Task 3 dùng hàm truyền vào nên chưa phụ thuộc, Task 4 phụ thuộc.

## Review Focus

- Mesh va chạm thiếu hoặc không kín (slime xuyên đá): script Blender phải từ chối xuất khi `_COL` rỗng.
- Đá đặt chồng lên nhau hoặc lên đường mòn: test đặt vật phải bắt.
- Chạy lại menu đặt đá hai lần: không nhân đôi.
- FBX nhập với tỉ lệ sai (Blender mét/Unity mét): đá 1 m phải ra 1 m.
- Cảnh chưa có Terrain hoặc `ZoneLayout`: menu báo rõ, không lỗi ném ra.

---

### Task 1: Dây chuyền Blender và nhóm đá

**Files:**
- Create: `tools/blender/kit_common.py`, `tools/blender/build_rocks.py`, `tools/blender/README.md`, `tools/blender/test_rocks.py`
- Output: `Assets/_Project/Models/Underroot/Rock_0..5.fbx`, `docs/art/previews/rocks_*.png`

**Interfaces:**
- Produces:
  - `kit_common.new_scene() -> None` (xoá cảnh, đơn vị mét);
  - `kit_common.export_fbx(objects: list, path: str) -> None` (tỉ lệ 1 = 1 m, trục Y lên khi vào Unity, `apply_unit_scale`);
  - `kit_common.render_preview(objects: list, path: str, angle: str) -> None` (ba góc "front", "side", "top"; Cycles hoặc Eevee thấp, 800×600);
  - `kit_common.tri_count(obj) -> int`; `kit_common.make_collider(obj, target_tris: int) -> object` (Decimate rồi đổi tên `<tên>_COL`).
  - `build_rocks.build_rock(seed: int, size: float) -> tuple[object, object]` (mesh hiển thị và `_COL`).
  - Lệnh: `python -I tools/blender/build_rocks.py --out Assets/_Project/Models/Underroot --previews docs/art/previews`.

- [ ] **Step 1: Cài đặt.** `python3 -m venv .venv-blender && .venv-blender/bin/pip install bpy` (thư mục `.venv-blender` thêm vào `.gitignore`); chạy `.venv-blender/bin/python -c "import bpy; print(bpy.app.version_string)"`. Expected: in ra số phiên bản. Nếu không chạy được: dừng, báo chủ dự án (không thử cách khác).
- [ ] **Step 2: Test thất bại** `tools/blender/test_rocks.py` (pytest hoặc script tự chạy): với `seed` 0..5: `tri_count(mesh)` trong [400, 2000]; `tri_count(_COL)` trong [12, 300]; `_COL.name == mesh.name + "_COL"`; kích thước hộp bao của mesh theo mét khớp tham số `size` ± 10%; hai seed khác nhau cho mesh khác nhau (đỉnh khác); mặt đá phía trên không dốc hơn 60° tại đỉnh (đá không có đỉnh nhọn kẹt slime) — lấy pháp tuyến trung bình phía trên.
- [ ] **Step 3:** Chạy: FAIL (chưa có hàm).
- [ ] **Step 4: Cài đặt** `kit_common` và `build_rocks`: khối cầu chia nhỏ, biến dạng bằng nhiễu nhiều tầng, vát cạnh, dẹt đáy; đá "có rêu": thêm màu đỉnh (vertex color) xanh rêu trên mặt hướng lên; xuất 6 FBX (kích thước 0,3 m / 0,6 / 1 / 1,6 / 2,4 / 3,2 theo seed) và 3 ảnh xem trước cho mỗi đá, gộp một ảnh tổng `rocks_all.png`; script thoát lỗi nếu `_COL` rỗng.
- [ ] **Step 5:** Chạy test: PASS; mở ảnh xem trước (Read) và nhận xét hình.
- [ ] **Step 6: Commit** `tools/blender`, FBX, previews: `feat(props): Blender pipeline and six rocks`.

### Task 2: Nhập FBX vào Unity (`PropImportPostprocessor`)

**Files:**
- Create: `Assets/_Project/Editor/PropImportPostprocessor.cs`, `Assets/_Project/Scripts/World/PropNaming.cs`
- Test: `Assets/_Project/Tests/EditMode/PropNamingTests.cs`

**Interfaces:**
- Produces: `static class PropNaming` với `bool IsCollider(string name)` (kết thúc `_COL`), `bool IsLod(string name, out int level)` (`<tên>_LOD1`, `_LOD2`), `string BaseName(string name)`; `PropImportPostprocessor : AssetPostprocessor` (`OnPostprocessModel(GameObject root)` chỉ áp cho đường dẫn `Assets/_Project/Models/Underroot/`): với mỗi con `_COL` gắn `MeshCollider` (không lồi) lên gốc, tắt renderer của con đó; con `_LOD1`, `_LOD2` dựng `LODGroup`; đặt cờ tĩnh; `ModelImporter.globalScale = 1`, `useFileScale = false`.

- [ ] **Step 1: Test.** `IsCollider("Rock_0_COL")` true, `"Rock_0"` false; `IsLod("Rock_0_LOD1", out l)` true với `l == 1`; `BaseName("Rock_0_COL") == "Rock_0"` và `BaseName("Rock_0_LOD2") == "Rock_0"`; `BaseName("Rock_0") == "Rock_0"`; tên rỗng/null không ném lỗi.
- [ ] **Step 2:** FAIL. **Step 3:** cài `PropNaming` và postprocessor (postprocessor không có test tự động; checklist thủ công). **Step 4:** pass.
- [ ] **Step 5:** Commit `feat(props): FBX import postprocessor and naming rules`.

### Task 3: `PropPlacement` (luật đặt vật, thuần)

**Files:**
- Create: `Assets/_Project/Scripts/World/PropPlacement.cs`
- Test: `Assets/_Project/Tests/EditMode/PropPlacementTests.cs`

**Interfaces:**
- Produces:
  - `readonly struct PropSpot { Vector2 Position; float Size; int Variant; float YawDegrees; }`
  - `static class PropPlacement` với `List<PropSpot> Rocks(int seed, Rect area, int count, Func<float,float,bool> blocked, int variantCount, Vector2 sizeRange)`: tất định; bỏ điểm `blocked == true`; khoảng cách giữa hai tâm ≥ `0.5 * (Size_a + Size_b) + 1` m; số điểm ≤ `count`.
  - `static bool Allowed(float x, float z, Func<float,float> waterDepth, Func<float,float,float> distToPath, IList<Vector2> keepClear, float keepClearRadius)`: false nếu `waterDepth > 0.5`, `distToPath < 3`, hoặc trong `keepClearRadius` quanh một điểm cần giữ trống.

- [ ] **Step 1: Test.** `Rocks_SameSeedSameResult`; `Rocks_DifferentSeedDiffers`; `Rocks_NeverBlocked` (khối bị chặn là một hình tròn); `Rocks_SpacedApart`; `Rocks_CountNotExceeded`; `Rocks_SizeWithinRange`; `Allowed_RejectsDeepWater` (độ sâu 0,6 → false, 0,4 → true); `Allowed_RejectsNearPath` (2,9 m → false, 3,1 → true); `Allowed_RejectsKeepClear` (tại 5,9 m khỏi điểm giữ trống 6 m → false).
- [ ] **Step 2:** FAIL. **Step 3:** cài đặt. **Step 4:** pass.
- [ ] **Step 5:** Commit `feat(props): placement rules`.

### Task 4: Menu "Dress Lake: Props" đặt đá theo khu

**Files:**
- Create: `Assets/_Project/Editor/LakeProps.cs`
- Modify: `Assets/_Project/Editor/LakeRebuildAll.cs` (thêm một bước "8/8 Vật thể Blender (Dress Lake: Props)" sau bước Roots and Light)

**Interfaces:**
- Consumes: `PropPlacement` (Task 3), FBX `Rock_0..5` (Task 1) tải từ `Assets/_Project/Models/Underroot/`, `ZoneLayout.Zones` / `Paths` và `WaterMap.BedDepth` của 0.6d (phải có trong `main`), Terrain trong cảnh.
- Produces: `LakeProps.Dress()` (`[MenuItem("Evolve/Dress Lake: Props")]`): object cha `Lake Props (Dressed)` (thay bản cũ nếu có), đá đặt theo bảng khu (khu 01 đá bóng nhỏ ven bờ, 02 đá lớn quanh khe, 03 vừa, 04 ít, 05 đá trắng nhiều hơn ven suối, 06 rải rác), cao độ lấy từ Terrain, đặt theo mặt đất; đánh dấu tĩnh; ≤ 500 đá; không chặn đường mòn, nơi trú, tổ, điểm xuất hiện, nước sâu; ghi `Debug.Log` tổng số.

- [ ] **Step 1:** Cài `Dress()`: thiếu Terrain, `ZoneLayout.Zones` hoặc FBX thì `Debug.LogWarning` rõ ràng rồi thoát (không ném lỗi).
- [ ] **Step 2:** Chạy hai lần liên tiếp phải cho cùng số đá (xoá cha cũ trước khi dựng); test này làm tay (checklist).
- [ ] **Step 3:** Biên dịch sạch, test Task 2–3 vẫn pass.
- [ ] **Step 4:** Commit `feat(props): Dress Lake: Props places rocks by zone`.

### Task 5: Tài liệu, checklist và cổng duyệt

**Files:**
- Modify: `docs/ROADMAP.md`, `docs/PROJECT_STATUS.md` (mục 6.x + checklist), `tools/blender/README.md`

- [ ] **Step 1:** Ghi: dây chuyền, cách chạy Blender (lệnh trong Task 1), ảnh xem trước ở `docs/art/previews/`, giới hạn (thú xuyên đá, chưa có nhóm khác).
- [ ] **Step 2:** Checklist thủ công: (1) mở Unity, Console sạch, Test Runner xanh; (2) FBX trong `Models/Underroot` có Mesh Collider và hiển thị đúng tỉ lệ (đá 1 m = 1 m); (3) chạy `Evolve → Dress Lake: Props`, đá nằm trên mặt đất, không trong nước sâu, không chắn đường mòn; (4) slime chạm đá: dừng lại, đi vòng, không rung; (5) FPS bằng `PerfOverlay`; (6) gửi ảnh chụp trong Unity để duyệt; (7) sau khi duyệt, báo phiên trang trí gỡ đá code trong `LakeGround`.
- [ ] **Step 3:** Commit `docs(props): pipeline and rock group checklist`.

## Tự rà soát

- Phủ spec: dây chuyền (T1–T2), va chạm (T1–T2), đặt theo khu (T3–T4), kiểm chứng và cổng duyệt (T5). Các nhóm rễ, gỗ, nấm, vách, mái, ánh sáng và nước **ngoài plan này** (plan riêng, sau khi duyệt đá).
- Chữ ký nhất quán: `PropNaming.IsCollider/IsLod/BaseName`; `PropPlacement.Rocks/Allowed`; `PropSpot`.
- Tỉ lệ: Task 1 dài nhất; thân hàm Blender để người làm quyết.
