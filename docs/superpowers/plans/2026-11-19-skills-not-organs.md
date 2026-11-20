# Kỹ năng thay cho cơ quan — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: superpowers:executing-plans. Steps use checkbox (`- [ ]`) syntax.

**Goal:** Gene học được là kỹ năng. Bị động (tối đa 3) nặn ở hang trú bằng khối. Chiêu chủ động gắn phím 1–5 ở đâu cũng được; bộ phận chỉ hiện lúc vận chiêu rồi tan vào thân.

**Architecture:**
- Luật thuần `Swallow/Pure/SkillRules` (có test .NET): phân loại gene, cấp chiêu theo sao, thời gian chạy nước rút, đường cong hiện hình.
- `OrganHost` chỉ giữ bị động.
- `SkillBar` lấy chiêu từ `AnalysisBook`.
- `SprintAbility` được tạo lúc chạy cho gene chân không có chiêu riêng.
- `SkillVisuals` cho bộ phận mọc ra, giữ, rồi thu vào.
- `SprintBoost` nhân tốc độ.

**Spec:** `docs/superpowers/specs/2026-11-19-reincarnation-survival-design.md` mục 6.1

## Global Constraints
- Bị động tối đa 3; chỉ đổi ở hang trú; tốn khối như `MassRules`.
- Chiêu: gắn phím ở đâu cũng được; dùng được từ ★1; cấp chiêu = min(3, sao + 1). Chiêu cũ thiết kế cho Lv2 là gốc, Lv3 là nâng cấp.
- Chạy nước rút: 4 / 5 / 6 giây theo ★1 / ★2 / ★3; hồi chiêu 10 giây; tốn 15 năng lượng.
- Hiện hình: mọc 0,15 giây, giữ suốt thời gian hiệu lực, thu vào 0,35 giây.
- Phản đòn của gai chỉ tính khi gai đang hiện.

## Review Focus
1. Gene vừa có chiêu vừa tăng tốc (Chân giả cơ bắp): chỉ thành một chiêu, là chiêu riêng của nó (Lao Tới) (`UseOf_AbilityWins`).
2. Nặn một gene chủ động ở bảng G: bị từ chối, có lý do (`SculptResult.NotPassive`).
3. Bị động thứ 4: bị từ chối (`Check` trả `NoRoom`, test `Passives_CapAt3`).
4. Bấm chiêu lần nữa khi bộ phận còn đang hiện: không nhân đôi hình (`SkillVisuals` khởi động lại hình đang có).
5. Chạy nước rút chồng lên nhau: lấy hệ số lớn nhất, gia hạn thời gian, không nhân dồn.

### Task 1: SkillRules (thuần)
- Tạo `Scripts/Swallow/Pure/SkillRules.cs`, test `Tests/EditMode/Swallow/Pure/SkillRulesTests.cs`.
- `enum GeneUse { None, Passive, Active }`
- `GeneUse UseOf(bool hasAbility, bool grantsSight, float heatResistance, float speedMultiplier, float contactDamage)`, theo thứ tự ưu tiên:
  1. Có chiêu riêng: Active.
  2. Mắt hoặc chịu nhiệt > 0: Passive.
  3. Tốc độ > 1,01: Active (chạy nước rút).
  4. Phản đòn > 0: Passive.
  5. Còn lại: None.
- `const int MaxPassives = 3`
- `int AbilityLevel(int stars)`
- `float SprintSeconds(int stars)`, `const float SprintCooldown = 10f`, `const float SprintEnergy = 15f`
- `const float GrowSeconds = 0.15f, RetractSeconds = 0.35f`
- `float VisualScale(float elapsed, float hold)`: tăng 0→1 trong lúc mọc, giữ 1 trong `hold` giây, giảm về 0 trong lúc thu; `ShowSeconds(hold)` là tổng thời gian.
- Test cả ba loại. Riêng Chân giả cơ bắp (có chiêu, tốc độ 1,7) phải ra Active.

### Task 2: Bị động trong OrganHost, chiêu trong SkillBar
- `OrganSO` thêm `GeneUse Use` (đọc theo luật) và `bool IsPassive`.
- `OrganHost`:
  - `Check` trả `NoRoom` khi đã có 3 bị động.
  - `TrySculpt` trả `SculptResult.NotPassive` cho gene chủ động.
  - Hiệu ứng (`Recalculate`) chỉ tính bị động.
  - `ContactDamage` = phản đòn bị động + `SkillVisuals.ContactDamage`.
- `SkillBar`:
  - Nguồn chiêu là `host.Book.Known` lọc Active.
  - `AbilityFor(organ)`: chiêu riêng, hoặc một `SprintAbility` tạo lúc chạy (cache theo organ).
  - Cấp = `AbilityLevel(book.StarsOf)`.
  - Mỗi lần dùng gọi `SkillVisuals.Show(organ, hold)`.
- Test Unity: sửa `SculptTests` (thêm `Sculpt_ActiveRefused`, `Passives_CapAt3`) và `SkillBarMenuTests`.

### Task 3: SprintAbility, SprintBoost, SkillVisuals
- `Abilities/SprintAbility.cs` (AbilitySO, tạo lúc chạy): `Activate` gọi `SprintBoost.Apply(user, mult, seconds)`.
- `Abilities/SprintBoost.cs`: `static float Of(GameObject)`; chồng thì lấy max và gia hạn.
- `Abilities/SkillVisuals.cs`:
  - Tìm `OrganSocket`.
  - `Show(OrganSO, float hold)` tạo hoặc khởi động lại `VisualPrefab` ở socket, cỡ theo `VisualScale`.
  - `ContactDamage` = tổng phản đòn của các bộ phận đang hiện.
- `SlimeController`: tốc độ × `SprintBoost.Of(gameObject)`.

### Task 4: HUD
- G (Cơ thể):
  - Tiêu đề "BỊ ĐỘNG · n/3".
  - Danh sách chỉ hiện bị động đã học, theo ô Mắt và Da.
  - Có dòng chỉ sang K để gắn chiêu.
- K (Kỹ năng):
  - Danh mục lấy từ chiêu đã học.
  - Cấp hiển thị theo ★.
  - Dấu cho chạy nước rút là "»".
  - Có phần chi tiết cho chạy nước rút.
- Help text cập nhật.
- Kiểm thủ công:
  - Học Gai độc thì gai không hiện trên lưng; bấm phím thì gai mọc, bắn, rồi thu vào.
  - Học Chân chạy thì bấm phím chân hiện ra, chạy nhanh khoảng 4 giây.
  - Mắt chỉ nặn ở tổ và luôn hiện.
