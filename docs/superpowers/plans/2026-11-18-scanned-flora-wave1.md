# Scanned Flora, đợt 0–1 (chọn tài sản, dây chuyền cài, vật liệu bán thực, loài mẫu) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Chủ dự án bấm một menu là tải về máy các tài sản quét CC0 đã chọn, chúng thành vật liệu URP bán thực, và một bộ loài mẫu (mỗi tầng một loài) đứng ở khu 01 để duyệt chất lượng và FPS.

**Architecture:** Danh sách tài sản (`tools/assets/flora_manifest.json`) trong git; phần thuần (đọc manifest, chọn URL tải, chỉnh màu bán thực) ở `Scripts/World` có test EditMode; menu Editor tải bằng `UnityWebRequest` từ API Poly Haven vào thư mục gitignore, dựng vật liệu, rồi một menu khác đặt loài mẫu. Đợt 2 trở đi (Habitat, luật mọc, các khu, kỳ ảo, ánh sáng) là các plan sau.

**Tech Stack:** Unity 6.6 URP 17, C#, EditMode tests (`Evolve.Tests.EditMode`), JsonUtility, UnityWebRequest; Poly Haven API (`https://api.polyhaven.com/files/{id}`).

**Spec:** `docs/superpowers/specs/2026-11-18-scanned-flora-design.md`

## Global Constraints

- Phong cách **bán thực**: bớt bão hòa, tông theo khu, độ nhám đồng đều hơn (spec mục 8).
- Tài sản CC0 Poly Haven / ambientCG; tải **trên máy chủ dự án**; tổng 1 đến 3 GB; không đẩy nhị phân lớn lên git.
- Máy mục tiêu RTX 3050 6 GB, Full HD; giữ khoảng 90 FPS trở lên ở chỗ đo (hiện 150).
- Chỉ thêm file mới; không sửa lớp nền của phiên kia. Không `git add .`.
- Map phải mở và chạy khi chưa cài tài sản (menu đặt cây báo thiếu và bỏ qua).

## Review Focus

1. Mạng lỗi hoặc tài sản đã bị đổi tên trên Poly Haven: menu cài phải báo rõ tài sản nào lỗi và cài tiếp phần còn lại (Task 3 test `PickUrl_MissingFormat_ReturnsNull`).
2. Chạy menu cài hai lần: không tải lại file đã có (Task 3 test `Plan_SkipsExistingFiles`).
3. Máy chưa cài tài sản mà chạy menu đặt loài mẫu: không lỗi, chỉ cảnh báo (Task 5 bước kiểm thủ công).
4. Texture màu quá bão hòa sau chỉnh vẫn phải giữ độ sáng (Task 4 test `Grade_KeepsLuminance`).
5. Lá cây hai mặt, alpha clip: không bị đen mặt sau (Task 4 kiểm thủ công trong Unity).

---

### Task 1: Chọn tài sản và viết manifest

**Files:**
- Create: `tools/assets/flora_manifest.json`, `tools/assets/select_flora.py`, `docs/art/flora-selection.md`

**Interfaces:**
- Produces: manifest JSON `{ "assets": [ { "id": string, "source": "polyhaven", "kind": "model"|"texture", "resolution": "1k"|"2k", "role": string, "tier": "ground"|"shrub"|"tall"|"rock", "species": string } ] }`

- [ ] **Step 1:** `select_flora.py` đọc `https://api.polyhaven.com/assets?t=models` và `?t=textures` (cần chủ dự án cho phép lệnh mạng), lọc theo danh mục (plants, rocks, nature, ground), in danh sách ứng viên kèm kích thước tải ở 1k/2k.
- [ ] **Step 2:** Chọn mỗi loài trong spec mục 5.2 một đến ba ứng viên; ghi manifest; tổng dung lượng ≤ 3 GB (mục tiêu ≤ 1,5 GB ở đợt 1).
- [ ] **Step 3:** `docs/art/flora-selection.md`: bảng loài → tài sản, link trang và ảnh xem trước (`https://cdn.polyhaven.com/asset_img/thumbs/{id}.png`), dung lượng. Chủ dự án duyệt.
- [ ] **Step 4:** Commit `docs(flora): asset selection and manifest`.

### Task 2: Đọc manifest (thuần)

**Files:**
- Create: `Assets/_Project/Scripts/World/FloraManifest.cs`
- Test: `Assets/_Project/Tests/EditMode/FloraManifestTests.cs`

**Interfaces:**
- Produces: `[Serializable] class FloraAsset { string id, source, kind, resolution, role, tier, species; }`, `[Serializable] class FloraManifest { FloraAsset[] assets; static FloraManifest Parse(string json); IEnumerable<FloraAsset> Tier(string tier); }`

- [ ] **Step 1: Test** `Parse_ReadsAllFields` (JSON 2 mục → đủ trường), `Parse_EmptyOrBroken_ReturnsEmpty` (chuỗi rỗng, JSON hỏng → `assets.Length == 0`, không ném), `Tier_FiltersByTier`.
- [ ] **Step 2:** Chạy test, FAIL.
- [ ] **Step 3:** Cài đặt bằng `JsonUtility.FromJson`, bọc try/catch.
- [ ] **Step 4:** Test PASS; commit `feat(flora): manifest model`.

### Task 3: Menu cài tài sản quét

**Files:**
- Create: `Assets/_Project/Scripts/World/FloraDownload.cs` (thuần), `Assets/_Project/Editor/ScannedAssetInstaller.cs`
- Modify: `.gitignore` (thêm `Assets/_Project/ThirdParty/Scanned/` và `.meta` tương ứng)
- Test: `Assets/_Project/Tests/EditMode/FloraDownloadTests.cs`

**Interfaces:**
- Consumes: `FloraManifest` (Task 2).
- Produces: `static class FloraDownload { string FolderFor(FloraAsset a) => "Assets/_Project/ThirdParty/Scanned/" + a.id; string PickUrl(string filesJson, FloraAsset a, out string fileName); List<(string url, string path)> Plan(string filesJson, FloraAsset a, Func<string,bool> exists); }`; menu `Evolve/Install Scanned Assets`.

- [ ] **Step 1: Test** `PickUrl_Model_PrefersFbxThenGltf` (JSON mẫu của `/files/{id}` có `fbx` và `gltf` ở `1k`,`2k` → lấy fbx đúng độ phân giải), `PickUrl_Texture_ReturnsDiffNorRoughAo` (qua `Plan`: 4 file ảnh), `PickUrl_MissingFormat_ReturnsNull`, `Plan_SkipsExistingFiles` (exists trả true cho 1 file → danh sách bớt 1).
- [ ] **Step 2:** FAIL.
- [ ] **Step 3:** Cài đặt `FloraDownload` (đọc JSON bằng parser nhỏ hoặc `JsonUtility` qua lớp bọc; ưu tiên `fbx`, rồi `gltf`; texture lấy `Diffuse`, `nor_gl`, `Rough`, `AO`, PNG). Menu: đọc manifest, với từng tài sản gọi `/files/{id}`, tải các URL trong `Plan`, thanh tiến trình có nút huỷ, log tổng MB, cài tiếp khi một tài sản lỗi, `AssetDatabase.Refresh` một lần ở cuối.
- [ ] **Step 4:** Test PASS; commit `feat(flora): scanned asset installer`.

### Task 4: Vật liệu bán thực

**Files:**
- Create: `Assets/_Project/Scripts/World/SemiRealGrade.cs`, `Assets/_Project/Editor/ScannedMaterials.cs`
- Test: `Assets/_Project/Tests/EditMode/SemiRealGradeTests.cs`

**Interfaces:**
- Produces: `static class SemiRealGrade { const float Desaturate = 0.25f; Color Grade(Color albedo, Color zoneTint, float tintAmount = 0.12f); float Roughness(float scanned) => Mathf.Lerp(scanned, 0.8f, 0.35f); }`; `ScannedMaterials.Build(FloraAsset a) -> Material` (URP Lit, `_BaseMap`, `_BumpMap`, độ nhám qua `_Smoothness` nghịch đảo trung bình, `_OcclusionMap`; lá: `_AlphaClip` 1, `_Cutoff` 0.45, `_Cull` 0; tông bán thực nhân vào `_BaseColor`).

- [ ] **Step 1: Test** `Grade_Desaturates` (màu đỏ thuần → độ bão hòa giảm khoảng 25%), `Grade_KeepsLuminance` (độ sáng lệch < 5%), `Grade_TintsTowardZone`, `Roughness_PullsTowardMatte` (0.2 → ~0.41).
- [ ] **Step 2:** FAIL. **Step 3:** cài đặt. **Step 4:** PASS.
- [ ] **Step 5:** Menu cài (Task 3) gọi `ScannedMaterials.Build` cho mỗi model và gán vào prefab `Assets/_Project/ThirdParty/Scanned/{id}/{id}.prefab` (kèm `LODGroup` nếu FBX có LOD, nếu không thì một LOD có cull 1%).
- [ ] **Step 6:** Commit `feat(flora): semi-real materials`.

### Task 5: Bộ loài mẫu ở khu 01 và đo FPS

**Files:**
- Create: `Assets/_Project/Editor/LakeScannedFloraSample.cs`
- Modify: `docs/PROJECT_STATUS.md` (mục 31: checklist, góc máy cố định)

**Interfaces:**
- Consumes: prefab của Task 4, `FloraManifest.Tier`.
- Produces: menu `Evolve/Dress Lake: Scanned Flora Sample` và `Evolve/Restore Lake Scanned Flora Sample (undo)`; object gốc `Lake Scanned Flora Sample (Dressed)`.

- [ ] **Step 1:** Đặt dọc bờ nam hồ (khu 01) một dải 40 m: mỗi tầng một loài (bụi, cây cao, đá, gỗ), mật độ như thật, tránh nước sâu và đường mòn (`ZoneLayout.DepthAt`, `DistanceToPath`), bám đất (`LakeGround.GroundY`); thiếu prefab thì cảnh báo và bỏ qua.
- [ ] **Step 2:** Thêm 2 góc máy cố định (empty `Camera Spot 01a/01b`) để chụp so sánh.
- [ ] **Step 3:** Kiểm thủ công trong Unity (chủ dự án): Console sạch; lá không đen mặt sau; chưa cài tài sản thì menu chỉ cảnh báo; FPS ≥ 90 tại góc máy.
- [ ] **Step 4:** Commit `feat(flora): scanned sample strip in zone 01`.

## Sau đợt 1

Khi chủ dự án duyệt chất lượng và FPS: viết plan đợt 2 (`World/Habitat`, luật mọc, tầng bụi và cây cho khu 01, 03), rồi các đợt 3 đến 5 như spec mục 6.
