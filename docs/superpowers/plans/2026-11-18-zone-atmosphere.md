# Không khí riêng cho từng khu (sương và hạt): Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Mỗi khu trong 6 khu có sương mù và hạt lơ lửng riêng, trộn mượt theo vị trí người chơi.

**Architecture:** Dữ liệu và phép trộn thuần (`AtmosphereProfile`, `ZoneAtmosphereMath`, có test). `ZoneAtmosphere` (MonoBehaviour trên camera, thêm lúc chạy như `UnderwaterTint`) ghi sương trong `LateUpdate` (sau `MonochromeVision`, nhường `UnderwaterTint` khi dưới nước) và điều khiển 3 hệ hạt đi theo camera với tổng tối đa 150 hạt.

**Tech Stack:** Unity 6.6 URP, C#, ParticleSystem, EditMode tests.

**Spec:** `docs/superpowers/specs/2026-11-18-zone-atmosphere-design.md`.

## Global Constraints

- Tối đa **150 hạt** hiển thị cùng lúc, 3 hệ hạt, không bóng đổ; có công tắc tắt hạt; mục tiêu FPS giảm không quá 3%.
- Sương `FogMode.Linear`; khi chưa có mắt (sight = 0) sương vẫn **đen** như `MonochromeVision`; khi có mắt màu theo khu; tầm nhìn nhân `1 + max(0, sight − 1)`.
- Nhường cho `UnderwaterTint` khi camera dưới nước (không ghi sương); tự khôi phục đúng khi nổi lên.
- Không sửa `MonochromeVision`, ánh sáng chính, ambient, hậu xử lý; không đụng `Prototype_01` (chỉ thêm vào camera khi cảnh có `ZoneLayout` và Terrain); không đụng model nhân vật, sinh vật, cơ quan.
- Dùng `ZoneLayout.Influence(int zone, float x, float z)` và `ZoneLayout.Zones` (6 khu, thứ tự 01..06).
- Không `git add .`; giữ dòng netcode trong `Packages/manifest.json`. Agent không có Unity: chủ dự án chụp ảnh gửi lại.

## Review Focus

- Chuyển khu mượt: dịch chuyển tức thì (teleport) hoặc hồi sinh không gây nhảy sương đột ngột (trộn theo thời gian).
- Chìm xuống nước rồi nổi lên: sương trở về đúng giá trị của khu, không kẹt xanh ngọc.
- Chưa có mắt: sương đen, hạt tắt (không thấy hạt ở xa).
- Ngoài mọi khu (vùng hoang): không ném lỗi, dùng hồ sơ `Wild`, hạt rất ít hoặc không có.
- Không có camera hoặc không có `MonochromeVision`: không lỗi (coi sight = 1).

---

### Task 1: Dữ liệu bầu không khí và phép trộn (thuần)

**Files:**
- Create: `Assets/_Project/Scripts/World/AtmosphereProfile.cs`, `Assets/_Project/Scripts/World/ZoneAtmosphereMath.cs`
- Test: `Assets/_Project/Tests/EditMode/ZoneAtmosphereMathTests.cs`

**Interfaces:**
- Produces:
  - `enum ParticleKind { Droplets, Dust, Leaves, Spores, Mist, Ash }` (rơi hoặc bay: mỗi kind ứng với một trong 3 hệ: `Falling` = Dust, Leaves, Ash; `Rising` = Spores; `Drifting` = Droplets, Mist).
  - `struct AtmosphereProfile { Color FogColor; float FogEnd; ParticleKind Particles; Color ParticleColor; float Density; float Size; float Speed; }` (`Density` hạt/giây trên toàn hộp 30 m; `Size` mét; `Speed` m/s).
  - `static class ZoneAtmosphereMath`:
    - `static readonly AtmosphereProfile[] Zones` (6 phần tử, bảng thiết kế 5.1: 01 xanh ngọc nhạt FogEnd 160, 02 nâu ấm vàng 70, 03 nâu cam đục 55, 04 tím nhạt vàng 90, 05 trắng xanh lạnh 120, 06 xám đỏ sẫm 40; `Density` 3 / 3 / 4 / 5 / 3 / 2) và `static readonly AtmosphereProfile Wild` (xám xanh tối, FogEnd 100, Density 0);
    - `AtmosphereProfile Blend(float[] weights)` (độ dài 6; trọng số âm coi là 0; `Wild` có trọng số `max(0, 1 − tổng)` rồi chuẩn hóa tất cả; màu, FogEnd, Density, Size, Speed nội suy theo trọng số; `Particles` lấy của khu có trọng số lớn nhất);
    - `Color FogColorFor(AtmosphereProfile p, float sight)` = `Color.Lerp(Color.black, p.FogColor, Mathf.Clamp01(sight))`;
    - `float FogEndFor(AtmosphereProfile p, float sight)` = `Mathf.Lerp(80f, p.FogEnd, c * c) * (1 + Mathf.Max(0, sight − 1))` với `c = Clamp01(sight)`;
    - `float ParticleBudget(float sight, float density)` = `Mathf.Min(MaxParticles, density * 20f * Mathf.Clamp01(sight))` (`MaxParticles = 150`).

- [ ] **Step 1: Test (thất bại).** `Zones_HasSixDistinctProfiles` (6 phần tử, FogColor khác nhau, FogEnd khu 06 nhỏ nhất ≥ 40); `Blend_AllWeightsZeroGivesWild`; `Blend_SingleZoneWeightOneGivesThatZone` (FogColor, FogEnd bằng khu); `Blend_HalfAndHalfIsMidpoint` (hai khu 0,5 và 0,5: FogEnd là trung bình); `Blend_NormalisesWhenWeightsExceedOne`; `Blend_NegativeWeightIgnored`; `FogColor_BlindIsBlackSightedIsZoneColour` (sight 0 → đen, 1 → màu khu, 0,5 → giữa); `FogEnd_GrowsWithSightAboveOne` (sight 2 → gấp đôi sight 1); `FogEnd_BlindIsEighty`; `ParticleBudget_NeverAboveMaxAndZeroWhenBlind` (Density rất lớn → 150; sight 0 → 0).
- [ ] **Step 2:** Chạy EditMode: FAIL (chưa có kiểu).
- [ ] **Step 3:** Cài đặt như Interfaces; màu cụ thể do người làm chọn theo mô tả bảng thiết kế.
- [ ] **Step 4:** Test pass. **Step 5:** Commit `feat(atmosphere): zone profiles and blending`.

### Task 2: `ZoneAtmosphere` (sương theo khu)

**Files:**
- Create: `Assets/_Project/Scripts/World/ZoneAtmosphere.cs`
- Test: `Assets/_Project/Tests/EditMode/ZoneAtmosphereMathTests.cs` (thêm test cho hàm trọng số thuần nếu tách)

**Interfaces:**
- Consumes: `ZoneAtmosphereMath`, `ZoneLayout.Influence`, `MonochromeVision.Sight` (đọc bằng `FindFirstObjectByType<MonochromeVision>()`, cache), `UnderwaterTint` (kiểm tra camera dưới nước bằng `SwimLogic.IsUnderwater(eye.y, ZoneLayout.WaterLevel, wasUnderwater)` như `UnderwaterTint`; hoặc thêm thuộc tính `UnderwaterTint.IsUnder` nếu cần, một dòng).
- Produces: `ZoneAtmosphere : MonoBehaviour` với `public static float[] WeightsAt(float x, float z)` (6 phần tử = `ZoneLayout.Influence(i, x, z)`), `public bool ParticlesEnabled { get; set; } = true`, `public AtmosphereProfile Current { get; }`; `LateUpdate`: tính trọng số tại vị trí camera, `Blend`, làm mượt theo thời gian (`Mathf.MoveTowards` trên các thành phần, tốc độ ~ 0,5 đơn vị/giây; lần đầu nhảy thẳng), nếu không dưới nước thì đặt `RenderSettings.fog = true`, `fogMode = Linear`, `fogStartDistance = 0`, `fogEndDistance = FogEndFor(...)`, `fogColor = FogColorFor(...)`.

- [ ] **Step 1:** Cài đặt; thiếu `MonochromeVision` thì sight = 1; không camera thì không làm gì.
- [ ] **Step 2:** Biên dịch sạch; commit `feat(atmosphere): fog follows the zone`.

### Task 3: Hạt theo khu

**Files:**
- Modify: `Assets/_Project/Scripts/World/ZoneAtmosphere.cs`

**Interfaces:**
- Consumes: `ZoneAtmosphereMath.ParticleBudget`, `AtmosphereProfile` hiện tại.
- Produces: ba `ParticleSystem` con (tên "Atmosphere Falling", "Atmosphere Rising", "Atmosphere Drifting") tạo bằng code trong `Awake`: `main.maxParticles` cộng lại ≤ 150, `simulationSpace = World`, hộp phát 30 × 14 × 30 m quanh camera (hình dạng `Box`), `shadows` tắt, vật liệu `Sprites/Default` hoặc `Universal Render Pipeline/Particles/Unlit` tạo bằng code với texture chấm tròn mờ sinh bằng code (`Texture2D` 32×32), `renderMode = Billboard`; mỗi `LateUpdate` đặt hộp theo camera, màu, kích thước, tốc độ và `emission.rateOverTime = ParticleBudget(...) × trọng số kiểu` (kiểu của hồ sơ trộn quyết định hệ nào phát; hệ khác phát 0). Tắt hẳn (`Stop`) khi `ParticlesEnabled` false, khi sight < 0,05 hoặc khi dưới nước.
- Công tắc: phím `F8` bật/tắt (`ParticlesEnabled`), ghi một dòng `Debug.Log` khi đổi.

- [ ] **Step 1:** Cài đặt ba hệ hạt như trên; kích thước hạt 0,06–0,25 m theo hồ sơ.
- [ ] **Step 2:** Biên dịch sạch; test cũ vẫn pass; commit `feat(atmosphere): zone particles under a 150 cap`.

### Task 4: Gắn vào cảnh, tài liệu, checklist

**Files:**
- Modify: `Assets/_Project/Scripts/Player/SlimeController.cs` (cạnh chỗ thêm `UnderwaterTint`, khối `if (layoutWater)`), `docs/PROJECT_STATUS.md` (mục 20), `docs/ROADMAP.md`

- [ ] **Step 1:** Trong khối `if (layoutWater)` thêm: nếu `cameraTransform != null` và chưa có `ZoneAtmosphere` thì `AddComponent<Evolve.World.ZoneAtmosphere>()` (một lần).
- [ ] **Step 2:** Tài liệu: bảng 6 khu, ngân sách 150 hạt, phím F8, giới hạn (chưa có sương sát đất; hạt xuyên vật thể gần camera).
- [ ] **Step 3:** Checklist: (1) Console sạch, Test Runner xanh (`ZoneAtmosphereMathTests`); (2) đi qua 6 khu: màu sương và hạt khác nhau, ranh giới mượt; (3) chưa có mắt: sương đen, không thấy hạt xa; có mắt (phím V): sương có màu khu; (4) lặn xuống hố sâu và nổi lên: sương trở về đúng khu; (5) F8 bật tắt hạt; (6) FPS bằng `PerfOverlay` không giảm quá 3%; (7) chụp ảnh từng khu gửi tôi.
- [ ] **Step 4:** Commit `feat(atmosphere): attach to the camera, docs and checklist`.

## Tự rà soát

- Phủ spec: bảng 5.1 (T1), trộn (T1–T2), tích hợp với `MonochromeVision` và `UnderwaterTint` (T2), hạt và ngân sách 150 (T3), gắn cảnh và kiểm chứng (T4).
- Chữ ký nhất quán: `ZoneAtmosphereMath.Blend/FogColorFor/FogEndFor/ParticleBudget`, `ZoneAtmosphere.WeightsAt/ParticlesEnabled/Current`.
- Tỉ lệ: plan ngắn hơn spec; Task 3 (hạt) là phần khó nhất và chỉ kiểm được trong Unity.
