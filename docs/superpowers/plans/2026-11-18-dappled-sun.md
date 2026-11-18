# Ánh sáng lọt qua kẽ rễ (mặt trời đổ bóng của mái rễ): Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Cả bản đồ tối, ánh sáng chỉ lọt qua kẽ rễ của mái thành mảng sáng khớp kẽ hở thật, mảng sáng trôi chậm, có sương mỏng.

**Architecture:** Hàm thuần `DappledSunMath` (lắc góc đèn, hằng số an toàn) có test. Công cụ Editor `LakeSun` (menu Dress / Restore, sao lưu JSON) đặt đèn chính, bật đổ bóng cho mái rễ, ambient tối, nâng tầm bóng đổ của mọi URP asset. `DappledSun` (runtime trên đèn chính) lắc góc; `ShadowToggle` (phím F9) bật tắt bóng đổ của mái để so FPS.

**Tech Stack:** Unity 6.6 URP 17 (Directional light shadows, URP asset shadow settings), C#, EditMode tests.

**Spec:** `docs/superpowers/specs/2026-11-18-dappled-sun-design.md`.

## Global Constraints

- Không sửa `MonochromeVision` (nó nhân cường độ đèn chính theo thị lực và đọc `sunBase` lúc Awake), `ZoneAtmosphere`, `LakeCanopy.cs`, `LakeRootsLight.cs`; công cụ tự tìm renderer mái theo tên.
- `DappledSun` **không đụng cường độ** đèn chính, chỉ góc quay.
- Chỉ chạy ở scene `Underroot_Lake` (`Assets/_Project/Scenes/Underroot_Lake.unity`); không đổi `Prototype_01`.
- Đèn chính: cao độ khoảng **72°**, hơi chéo (quay Y ~25°), màu ấm nhạt, cường độ **1,8**, `LightShadows.Soft`, strength 1.
- Ambient: `AmbientMode.Flat`, màu khoảng **(0,06; 0,08; 0,08)**; `reflectionIntensity` giảm còn 0,3.
- URP (mọi asset trong `GraphicsSettings.defaultRenderPipeline` và `QualitySettings`): `shadowDistance` **120 m**, `shadowCascadeCount` 4, `mainLightShadowmapResolution` 2048, soft shadows bật; sao lưu và khôi phục từng asset.
- Lắc: biên độ **±2,5°**, chu kỳ khoảng **70 s**.
- Mục tiêu FPS giảm không quá **15%**; phương án khi nặng: Restore, hạ `shadowDistance` còn 80 m, hoặc đổi sang cookie.
- Không `git add .`; giữ dòng netcode trong `Packages/manifest.json`. Agent không có Unity: chủ dự án chụp ảnh và FPS gửi lại.

## Review Focus

- Chưa có mắt vẫn tối và đen trắng, chỉ sáng quanh người (cường độ đèn chính do `MonochromeVision` nhân lại, `sunBase` lấy đúng giá trị 1,8 từ Editor).
- Chạy Dress hai lần rồi Restore: URP, RenderSettings, đèn và renderer mái về đúng như trước lần đầu (sao lưu giữ trạng thái gốc, không bị ghi đè).
- Chưa có canopy (Prototype hoặc chưa dựng) hoặc không có đèn Directional: cảnh báo rõ, không ném lỗi, không đổi gì.
- Lắc không làm cường độ nhảy và không rung khi `Time.timeScale = 0` hoặc khi pause.
- F9 tắt bóng đổ rồi bật lại: renderer mái trở về đúng chế độ ban đầu (kể cả hai mức chi tiết).

---

### Task 1: `DappledSunMath` (thuần) và hằng số

**Files:**
- Create: `Assets/_Project/Scripts/World/DappledSunMath.cs`
- Test: `Assets/_Project/Tests/EditMode/DappledSunMathTests.cs`

**Interfaces:**
- Produces: `static class DappledSunMath`:
  - hằng: `Elevation = 72f`, `Yaw = 25f`, `Intensity = 1.8f`, `SwayAmplitude = 2.5f`, `SwayPeriod = 70f`, `ShadowDistance = 120f`, `ShadowCascades = 4`, `ShadowResolution = 2048`, `AmbientColor` (`Color` ~ (0,06; 0,08; 0,08)), `SunColor` (`Color` ấm nhạt, ví dụ (1; 0,92; 0,75));
  - `Vector2 Angles(float elevation, float yaw, float time, float amplitude, float period)` → (pitch, yaw) đã lắc: `pitch = elevation + amplitude * sin(2π t / period)`, `yaw = yaw + 0,6 * amplitude * sin(2π t / period * 0,73 + 1,3)`; period ≤ 0 thì không lắc;
  - `Quaternion Rotation(float elevation, float yaw, float time, float amplitude, float period)` = `Quaternion.Euler(angles.x, angles.y, 0)`.

- [ ] **Step 1: Test (thất bại).** `Angles_AtTimeZeroIsBasePlusSmallOffset` (pitch = 72 tại t = 0; yaw gần 25 ± 1,5); `Angles_StaysWithinAmplitude` (quét t từ 0 đến 140: |pitch − 72| ≤ 2,5 + 1e-4, |yaw − 25| ≤ 1,5 + 1e-4); `Angles_RepeatsEveryPeriodForPitch` (t và t + 70 cùng pitch); `Angles_ZeroPeriodDoesNotSway` (period 0: pitch 72, yaw 25); `Constants_AreSafe` (Intensity trong [1, 3]; ShadowDistance ≥ 80; ShadowCascades trong [2, 4]; mọi thành phần AmbientColor ≤ 0,12).
- [ ] **Step 2:** Chạy EditMode: FAIL (chưa có kiểu). **Step 3:** Cài đặt. **Step 4:** Test pass.
- [ ] **Step 5:** Commit `feat(sun): dappled sun maths`.

### Task 2: `DappledSun` và `ShadowToggle` (runtime)

**Files:**
- Create: `Assets/_Project/Scripts/World/DappledSun.cs`, `Assets/_Project/Scripts/World/ShadowToggle.cs`
- Modify: `Assets/_Project/Scripts/Survival/KeyPressed.cs` (thêm `F9()` cho cả hai nhánh Input System và Input cũ)

**Interfaces:**
- Consumes: `DappledSunMath.Rotation`.
- Produces:
  - `DappledSun : MonoBehaviour` (trên đèn Directional): `Awake` nhớ `Elevation`/`Yaw` gốc từ `transform.eulerAngles` (nếu muốn dùng giá trị công cụ đặt); `Update` đặt `transform.rotation = DappledSunMath.Rotation(baseElevation, baseYaw, Time.time, DappledSunMath.SwayAmplitude, DappledSunMath.SwayPeriod)` (dùng `Time.time`, không `unscaledTime`; không đổi `intensity`).
  - `ShadowToggle : MonoBehaviour` (trên đèn chính): `[SerializeField] Renderer[] roof` do công cụ gán; `Update`: `KeyPressed.F9()` đảo `on`; đặt `shadowCastingMode` của mỗi renderer mái (`On` hoặc `Off`, nhớ giá trị gốc trong mảng `ShadowCastingMode[] original`), `QualitySettings.shadowDistance` (120 hoặc 0 khi tắt), `Debug.Log("Bóng đổ của mái: bật/tắt")`; `OnDisable` khôi phục.

- [ ] **Step 1:** Cài đặt hai script (chỉ nhận tham chiếu `null` an toàn, mảng rỗng không lỗi).
- [ ] **Step 2:** Biên dịch sạch; commit `feat(sun): runtime sway and F9 shadow toggle`.

### Task 3: `LakeSun` (Editor): Dress và Restore

**Files:**
- Create: `Assets/_Project/Editor/LakeSun.cs`
- Modify: `Assets/_Project/Editor/LakeRebuildAll.cs` (thêm "17/17 Mặt trời lọt kẽ rễ (Dress Lake: Dappled Sun)" sau Light and Water Look; đổi bộ đếm thành `/17`)

**Interfaces:**
- Consumes: `DappledSunMath` hằng số, `DappledSun`, `ShadowToggle`, `GraphicsSettings.defaultRenderPipeline` và `QualitySettings.GetRenderPipelineAssetAt(i)` (`UniversalRenderPipelineAsset`: `shadowDistance`, `shadowCascadeCount`, `mainLightShadowmapResolution`, `supportsSoftShadows`), object "Lake Canopy (Dressed)/Roof" (renderer con, cả hai mức chi tiết).
- Produces: `LakeSun.Dress()` (`[MenuItem("Evolve/Dress Lake: Dappled Sun")]`) và `LakeSun.Restore()` (`[MenuItem("Evolve/Restore Lake Sun (undo)")]`):
  - Chỉ chạy khi scene đang mở là `Underroot_Lake.unity` (không thì cảnh báo và thoát); thiếu đèn Directional hoặc thiếu "Lake Canopy (Dressed)" thì cảnh báo rõ, không đổi gì.
  - `Backup` JSON `Assets/_Project/Settings/LakeSunBackup.json`: lưu **một lần** (lần Dress đầu): đèn (quay, màu, cường độ, `shadows`, `shadowStrength`), `RenderSettings` (ambientMode, ambientLight, reflectionIntensity), từng URP asset (đường dẫn, shadowDistance, cascade, resolution, soft), `ShadowCastingMode` ban đầu của từng renderer mái (theo đường dẫn Hierarchy).
  - Dress: đặt đèn theo hằng số Task 1; mái `ShadowCastingMode.On`; ambient Flat tối; URP theo Global Constraints; thêm `DappledSun` và `ShadowToggle` (gán mảng mái) vào đèn nếu chưa có; `MarkSceneDirty`; log tóm tắt.
  - Restore: khôi phục từ backup, xóa `DappledSun`/`ShadowToggle`, xóa backup, `MarkSceneDirty`.

- [ ] **Step 1:** Cài đặt `Dress`, `Restore` và backup/khôi phục như trên (dùng `TryGetComponent`, không dùng `??` trên đối tượng Unity).
- [ ] **Step 2:** Thêm bước vào `LakeRebuildAll`.
- [ ] **Step 3:** Biên dịch sạch; test cũ pass; commit `feat(sun): Dress Lake: Dappled Sun with backup and restore`.

### Task 4: Tài liệu, checklist

**Files:**
- Modify: `docs/PROJECT_STATUS.md` (mục 22), `docs/ROADMAP.md`

- [ ] **Step 1:** Ghi: cách hoạt động, giá trị, F9, Restore, giới hạn (vùng xa hơn 120 m sáng đều và được sương che; bóng mức xa của mái thô hơn; nặng máy).
- [ ] **Step 2:** Checklist: (1) Console sạch, Test Runner xanh (`DappledSunMathTests`); (2) chạy **Evolve → Dress Lake: Dappled Sun** trong scene `Underroot_Lake`; (3) mảng sáng dưới các kẽ mái và 9 lỗ sáng, vùng khác tối; mảng sáng trôi chậm; (4) chưa có mắt: vẫn đen trắng, tối, chỉ sáng quanh người; có mắt (V): thấy mảng sáng; (5) đo FPS bằng `PerfOverlay`; F9 tắt bóng thì FPS hồi lại (ghi số trước/sau); (6) **Restore Lake Sun (undo)** trả về như cũ; (7) chụp ảnh gửi tôi.
- [ ] **Step 3:** Commit `docs(sun): dappled sun status and checklist`.

## Tự rà soát

- Phủ spec: đèn đổ bóng (T3), mái đổ bóng (T3), ambient (T3), URP 120 m (T3), lắc (T1–T2), F9 (T2), Restore và backup (T3), kiểm chứng (T1, T4). Sương mỏng dùng hệ có sẵn, không thêm task.
- Chữ ký nhất quán: `DappledSunMath.Angles/Rotation`, `DappledSun`, `ShadowToggle`, `LakeSun.Dress/Restore`.
- Tỉ lệ: plan ngắn hơn spec; Task 3 là phần lớn nhất và chỉ kiểm được trong Unity.
