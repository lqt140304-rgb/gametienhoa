# Ánh sáng và nước đẹp hơn (bước A của bộ vật thể Blender, mục 5.4): Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Làm cả bản đồ 500 m trông có không khí hơn bằng ánh sáng (sương, ambient, hậu xử lý) và nước rõ ràng hơn (mép mờ dần theo độ sâu, màu đậm theo độ sâu, bọt chạm đáy, phản chiếu).

**Architecture:** Hàm màu và độ sâu thuần (`WaterLook`) có test; shader URP viết tay `Evolve/LakeWater` đọc độ sâu cảnh; công cụ Editor mới `Editor/LakeLook.cs` (menu **Evolve → Dress Lake: Light and Water Look**, thêm làm bước cuối của Rebuild Lake) bật depth texture của URP, đặt sương/ambient, dựng Volume hậu xử lý và đổi shader của hai material nước `Lake_WaterClean` và `Lake_WaterDirty` sang shader mới.

**Tech Stack:** Unity 6.6 URP (Volume, Bloom, Color Adjustments, Vignette, Tonemapping), HLSL, C#, EditMode tests.

**Spec:** `docs/superpowers/specs/2026-11-18-blender-props-kit-design.md` mục 5.4; hướng nước xanh ngọc đã chốt (vũng đọng khu 03 vàng lục) trong `docs/superpowers/specs/2026-11-17-lake-map-500m-design.md`.

## Global Constraints

- Nước giữ màu **xanh ngọc**; `Lake_WaterDirty` (vũng đọng khu 03) giữ **vàng lục**; không đổi cách nước gây va chạm hay cách slime bơi (chỉ hình ảnh).
- Không dùng ánh sáng nướng nặng; không thêm ánh sáng thực lớn (giữ FPS); bóng đổ mềm.
- Không sửa logic địa hình, `WaterMap`/`ZoneLayout`, bơi lặn.
- Không `git add .`; giữ dòng netcode trong `Packages/manifest.json`.
- Agent không có Unity: shader viết tay, kiểm bằng ảnh chủ dự án gửi lại; `Prototype_01` không bị đổi.
- Menu chạy lại được (thay Volume cũ của chính nó, không nhân đôi).

## Review Focus

- Shader không biên dịch hoặc hồng/đen trong Unity: phải có phương án quay lại material cũ (menu `Restore`, hoặc nếu shader lỗi thì giữ Lit).
- Depth texture tắt: nước mất mép mờ dần; menu phải bật, và shader phải có kết quả hợp lý khi depth bằng 0 (không đen).
- Sương quá dày làm bản đồ 500 m mờ: mật độ có trần; khi camera dưới nước `UnderwaterTint` vẫn ghi đè sương và khôi phục đúng.
- Chạy menu hai lần: một Volume duy nhất.
- Màn hình tối quá hoặc cháy sáng: giá trị Color Adjustments và Bloom có khoảng an toàn (test thuần).

---

### Task 1: `WaterLook` (hàm màu và độ sâu thuần)

**Files:**
- Create: `Assets/_Project/Scripts/World/WaterLook.cs`
- Test: `Assets/_Project/Tests/EditMode/WaterLookTests.cs`

**Interfaces:**
- Produces: `static class WaterLook`:
  - `float DepthFade(float waterDepth, float fadeDistance)` → 0..1 (0 khi độ sâu 0, 1 khi ≥ `fadeDistance`; smoothstep);
  - `Color DepthColor(Color shallow, Color deep, float waterDepth, float deepDistance)` (nội suy tuyến tính theo `DepthFade`, alpha cũng nội suy);
  - `float FoamAmount(float waterDepth, float foamWidth)` → 1 khi độ sâu 0, giảm về 0 tại `foamWidth`;
  - `float FogDensityFor(float visibilityMetres)` = `2.5 / visibilityMetres` (sương mũ 2 gần đúng) kẹp trong [0.002, 0.03];
  - `static readonly` hằng: `AmbientSky`, `AmbientEquator`, `AmbientGround` (xanh lục tối), `FogColor`.

- [ ] **Step 1: Test.** `DepthFade_ZeroAtSurfaceOneAtDeep` (0 → 0, fadeDistance → 1, nửa đường 0..1); `DepthFade_NeverOutOfRange` (âm, rất lớn); `DepthColor_EndpointsAndMid`; `Foam_OneAtEdgeZeroBeyondWidth`; `FogDensity_ClampedAndDecreasesWithVisibility` (50 → lớn hơn 200; 5 → 0,03 trần; 5000 → 0,002 sàn); `Fog_VisibilityOf200IsAboutTwoPointFiveOver200` (≈ 0,0125).
- [ ] **Step 2:** Chạy EditMode: FAIL (chưa có kiểu).
- [ ] **Step 3:** Cài đặt theo chữ ký (smoothstep cho `DepthFade`).
- [ ] **Step 4:** Test pass.
- [ ] **Step 5:** Commit `feat(look): water look maths`.

### Task 2: Shader `Evolve/LakeWater`

**Files:**
- Create: `Assets/_Project/Shaders/LakeWater.shader`, `Assets/_Project/Shaders/LakeWater.shader.meta` (GUID cố định, 32 hex mới)

**Interfaces:**
- Produces: shader `Evolve/LakeWater` (trong mờ alpha blend, không ghi depth, `Queue = Transparent`) với thuộc tính `_ShallowColor`, `_DeepColor` (alpha dùng làm độ mờ), `_DeepDistance` (m), `_FoamWidth` (m), `_FoamColor`, `_BumpMap`, `_BumpScale`, `_Drift` (Vector2), `_Smoothness`, `_Emission`; đọc độ sâu cảnh qua `SampleSceneDepth` (URP `DeclareDepthTexture.hlsl`); công thức độ sâu nước = `LinearEyeDepth(sceneDepth) - LinearEyeDepth(fragmentDepth)`; màu = `DepthColor` (cùng công thức với `WaterLook`); bọt = `FoamAmount`; phản chiếu Fresnel đơn giản + nhân ánh sáng chính; khi depth texture không có (độ sâu ≤ 0 do cấu hình) thì coi là nước nông 1 m (không đen).
- Pass: `ForwardLit` và UsePass ShadowCaster không cần (nước không đổ bóng).

- [ ] **Step 1:** Viết shader HLSL URP (`Lighting.hlsl`, `DeclareDepthTexture.hlsl`, `UnityPerMaterial` CBUFFER), gồm `#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE`.
- [ ] **Step 2:** Biên dịch bằng mắt thường: kiểm đủ khai báo, không dùng macro ngoài URP 17; ghi chú trong shader về giới hạn (không phản chiếu thật).
- [ ] **Step 3:** Commit `feat(look): LakeWater shader`. Kiểm trong Unity do chủ dự án (checklist Task 5).

### Task 3: Công cụ Editor `LakeLook`

**Files:**
- Create: `Assets/_Project/Editor/LakeLook.cs`
- Modify: `Assets/_Project/Editor/LakeRebuildAll.cs` (thêm bước "15/15 Ánh sáng và nước (Dress Lake: Light and Water Look)" sau Props)

**Interfaces:**
- Consumes: `WaterLook`, `UnderwaterTint` (tôn trọng), hai material `Lake_WaterClean` và `Lake_WaterDirty` do `LakeWater` tạo.
- Produces: `LakeLook.Dress()` (`[MenuItem("Evolve/Dress Lake: Light and Water Look")]`) và `LakeLook.Restore()` (`[MenuItem("Evolve/Restore Lake Look (undo)")]`):
  - bật `supportsCameraDepthTexture` trên asset URP đang dùng (`GraphicsSettings.currentRenderPipeline`), ghi nhớ giá trị cũ;
  - đặt `RenderSettings`: ambient ba tầng (`WaterLook.Ambient*`), sương mũ 2 màu `WaterLook.FogColor`, mật độ `WaterLook.FogDensityFor(220f)`;
  - tạo object `Lake Post Process (Dressed)` có `Volume` toàn cục, profile lưu ở `Assets/_Project/Settings/LakeLook.asset` gồm Bloom (ngưỡng 0,9, cường độ 0,5), ColorAdjustments (độ tương phản +8, bão hòa +10), Vignette (0,25), Tonemapping (ACES nhẹ); bật `renderPostProcessing` trên camera chính;
  - đổi shader của `Lake_WaterClean` và `Lake_WaterDirty` sang `Evolve/LakeWater`, mang theo màu cũ làm `_ShallowColor`, và đặt `_DeepColor` đậm hơn cùng tông (clean: xanh ngọc thẫm; dirty: vàng lục thẫm); nếu `Shader.Find` trả null thì cảnh báo và giữ nguyên Lit;
  - chạy lại thay Volume cũ; `Restore` trả URP, RenderSettings và shader nước về như trước (lưu bản sao trạng thái vào một asset nhỏ `LakeLookBackup`).

- [ ] **Step 1:** Cài đặt `Dress` và `Restore` như trên; thiếu URP asset hoặc camera chính thì cảnh báo và bỏ qua phần đó.
- [ ] **Step 2:** Thêm bước vào `LakeRebuildAll` (đổi bộ đếm thành `/15`).
- [ ] **Step 3:** Biên dịch sạch, chạy test cũ.
- [ ] **Step 4:** Commit `feat(look): Dress Lake: Light and Water Look`.

### Task 4: Đèn nhấn cho nấm và rêu phát sáng (nhẹ)

**Files:**
- Modify: `Assets/_Project/Editor/LakeLook.cs`

**Interfaces:**
- Produces: bên trong `LakeLook.Dress`, khoảng 24 đèn điểm (không bóng) màu theo mũ nấm ở các cụm nấm lớn (khu 04) và vài điểm rêu; `Range 6–9 m`, `Intensity 1,2–2`, `shadows = None`; object cha `Lake Look Lights (Dressed)` (thay bản cũ); vị trí lấy từ các object tên `Giant Caps *` của `LakeLandmarks`, thiếu thì bỏ qua.

- [ ] **Step 1:** Cài đặt; **Step 2:** commit `feat(look): glow lights for mushrooms`.

### Task 5: Tài liệu và checklist

**Files:**
- Modify: `docs/PROJECT_STATUS.md` (mục 18), `docs/ROADMAP.md`

- [ ] **Step 1:** Ghi: công cụ, giá trị, hạn chế (nước không phản chiếu thật, sương toàn cục, chỉ đèn chính + ambient trên shader nước).
- [ ] **Step 2:** Checklist: (1) Console sạch, Test Runner xanh (`WaterLookTests`); (2) chạy menu: nước không hồng/đen; (3) mép nước mờ dần, vùng sâu đậm hơn, bọt ở chỗ nước chạm đá và bờ; (4) vũng khu 03 vẫn vàng lục; (5) cảnh có sương, ambient xanh lục, hậu xử lý (Bloom, Vignette) không làm tối hoặc cháy; (6) nấm phát sáng chiếu lên đất; (7) bơi xuống hố: `UnderwaterTint` vẫn hoạt động, ra khỏi nước sương trở lại bình thường; (8) FPS bằng `PerfOverlay` không giảm quá 15%; (9) `Evolve → Restore Lake Look (undo)` trả về như cũ; (10) chụp ảnh trong Unity gửi cho tôi.
- [ ] **Step 3:** Commit `docs(look): light and water checklist`.

## Tự rà soát

- Phủ spec 5.4: ánh sáng (T3, T4), nước (T1–T3). Rủi ro lớn nhất là shader viết tay không biên dịch thử; có `Restore` và phương án giữ Lit.
- Chữ ký nhất quán: `WaterLook.DepthFade/DepthColor/FoamAmount/FogDensityFor`; `LakeLook.Dress/Restore`.
- Tỉ lệ: Task 2 (shader) và Task 3 là lớn nhất.
