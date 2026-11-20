# Chiến đấu của quái, lát 3: Khỉ Hang ném đá và nhảy bục: kế hoạch thực hiện

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Thêm Khỉ Hang (loài mới) vào bộ chiêu dùng chung: ném đá theo cung, cắn vặt rồi chạy lùi, nhảy lên bục cao ném từ trên, hú gọi đàn.

**Architecture:** Luật thuần (`ThrowRules`, `PerchRules`, bộ lọc `MoveScheduler.Allow`, bộ chiêu `khihang`) có test ngoài Unity; mã Unity mỏng: viên đá (`MonsterProjectile`) và máy trạng thái nhảy bục trong `CreatureCombat`.

**Tech Stack:** Unity 6.6 URP, C#, NUnit; `tools/cstest/run.sh`, `tools/syntaxcheck/run.sh <ref>`.

**Spec:** `docs/superpowers/specs/2026-11-20-monster-combat-movesets-design.md` (mục "Lát 3", đã chốt).

## Global Constraints

- Văn bản người chơi, tài liệu, báo cáo bằng tiếng Việt; mã và chú thích mã bằng tiếng Anh. Số tạm, chỉnh được.
- Khỉ Hang (khóa `khihang`): `Ném Đá` gồng 0,9 s, ra đòn 0,1 s, kiệt sức 1,0 s, hồi chiêu 3 s, tầm 5 đến 14 m, bay 0,9 s theo cung tới vị trí mục tiêu lúc ném, sát thương 9; `Cắn Vặt` tầm dưới 2,5 m, gồng 0,4 s, phóng 2 m trong 0,2 s, sát thương 8, kiệt sức 0,8 s kèm chạy lùi 4 m, hồi chiêu 2 s; `Nhảy Bục` khi người chơi trong 6 m và có chỗ cao 1,5 đến 3 m trong 7 m, gồng 0,5 s, nhảy 0,8 s, ở trên 8 s ném đá tầm đến 16 m, nhảy xuống 0,6 s, hồi chiêu 15 s; `Hú Báo Động` dưới 66% máu, gồng 0,8 s, ra đòn 0,1 s, kiệt sức 1,0 s, gọi Khỉ Hang trong 30 m, hồi chiêu 20 s.
- Không phá lát 1, 2, ghì chặt, vồ, sinh thái. Commit kết thúc bằng hai dòng ghi công của phiên. Không tạo PR.
- Mỗi task: `tools/cstest/run.sh` xanh và `tools/syntaxcheck/run.sh origin/claude/unity-test-maze` 0 lỗi cú pháp trước khi commit.

## Review Focus

1. Viên đá bay khi người chơi chết, đổi hang, hoặc khỉ bị hủy: viên đá tự dọn, không lỗi tham chiếu (Task 3).
2. Khỉ trên bục mà bị choáng, bị hạ gục hoặc người chơi đi xa: không kẹt `Busy`, không treo trên cao vĩnh viễn (Task 3).
3. Không có chỗ cao trong tầm: chiêu `Nhảy Bục` không bị chọn, không tốn hồi chiêu (Task 1, 3).
4. Chỗ cao ứng viên nằm trong tường, trên cao quá hoặc thấp quá, hoặc gần người chơi hơn khỉ: không được chọn (Task 1).
5. Ném đá khi mục tiêu đứng sát (tầm dưới 5 m) hoặc ở xa quá 14 m: không ném (Task 1 của lát 1 đã phủ tầm; kiểm lại trong Task 2).

---

### Task 1: `ThrowRules`, `PerchRules` và bộ lọc chiêu (thuần)

**Files:**
- Create: `Assets/_Project/Scripts/Combat/Pure/ThrowRules.cs`, `Assets/_Project/Scripts/Combat/Pure/PerchRules.cs`
- Modify: `Assets/_Project/Scripts/Combat/Pure/MoveScheduler.cs`
- Test: `Assets/_Project/Tests/EditMode/Combat/Pure/ThrowRulesTests.cs`, `MoveSchedulerTests.cs`

**Interfaces:**
- Produces:
  - `static (float vx, float vy, float vz) ThrowRules.Launch(float fx, float fy, float fz, float tx, float ty, float tz, float flightSeconds, float gravity = 9.81f)` (vận tốc đầu để tới đích đúng sau `flightSeconds`); `static (float x, float y, float z) ThrowRules.PositionAt(float fx, float fy, float fz, (float vx, float vy, float vz) v, float t, float gravity = 9.81f)`; `static bool ThrowRules.Hits(float px, float py, float pz, float tx, float ty, float tz, float radius)`.
  - `static int PerchRules.PickLedge(IReadOnlyList<Ledge> ledges, float cx, float cy, float cz, float px, float pz, float minRise = 1.5f, float maxRise = 3f, float maxDistance = 7f)` trả chỉ số chỗ cao tốt nhất hoặc `-1`; `readonly struct Ledge(float x, float y, float z)`. Tốt nhất: trong tầm ngang không quá `maxDistance`, cao hơn chân khỉ từ `minRise` đến `maxRise`, **xa người chơi hơn khỉ** (khỉ chạy khỏi người chơi), và trong số đó cái xa người chơi nhất.
  - `System.Func<MoveDef, bool> MoveScheduler.Allow` (mặc định `null`): chiêu bị loại khỏi lựa chọn khi `Allow` trả `false`.

- [ ] **Step 1: Viết test**:
  - `Launch_ReachesTheTargetAfterTheFlightTime`: từ `(0,1,0)` tới `(6,0,8)` bay 0,9 s: `PositionAt` ở `t = 0.9` bằng đích (sai số 0,001); ở `t = 0` bằng điểm đầu.
  - `Launch_MakesAnArc`: giữa chuyến (`t = 0.45`) cao hơn đường thẳng giữa đầu và đích.
  - `Launch_HorizontalVelocityIsDistanceOverTime`: `vx, vz` bằng `(đích - đầu) / thời gian`.
  - `Launch_UpwardsToALedge`: tới đích cao hơn 2 m trong 0,8 s vẫn tới đích đúng.
  - `Hits_WithinRadiusOnly`: cách 0,5 trúng với bán kính 0,6; cách 0,7 trượt.
  - `PickLedge_RiseWindow_AndDistance`: ứng viên cao hơn 1 m, 2 m, 4 m, và một cái cách 9 m (cao 2): chỉ cái cao 2 m trong 7 m được chọn; không ứng viên hợp lệ thì `-1`.
  - `PickLedge_PrefersFarthestFromThePlayer_AndNotTowardsThem`: hai chỗ hợp lệ, người chơi đứng gần chỗ A hơn khỉ: chọn B; chỗ nằm gần người chơi hơn khỉ không được chọn dù hợp lệ.
  - `PickLedge_EmptyList_IsMinusOne`.
  - Trong `MoveSchedulerTests`: `Allow_FiltersOutMoves` (hai chiêu cùng dùng được, `Allow = m => m.Name != "B"`: không bao giờ bắt đầu B trong 30 s), `Allow_Null_ChangesNothing` (chu kỳ như `Cycle_WindupActiveRecoverIdle`).
- [ ] **Step 2: Chạy `tools/cstest/run.sh`, thấy đỏ.**
- [ ] **Step 3: Cài.** Cung: `vy = (dy + g·T²/2) / T`; `PositionAt` dùng `y = fy + vy·t − g·t²/2`.
- [ ] **Step 4: Chạy lại, xanh. Commit.**

### Task 2: Bộ chiêu Khỉ Hang và tư thế Throw (thuần)

**Files:**
- Modify: `Assets/_Project/Scripts/Combat/Pure/MoveScheduler.cs` (hình chiêu và trường mới), `MovesetSeed.cs`, `TellPose.cs`
- Test: `Assets/_Project/Tests/EditMode/Combat/Pure/MovesetSeedTests.cs`

**Interfaces:**
- Consumes: Task 1.
- Produces: `MoveShape` thêm `Throw`, `Perch`; `MoveDef` thêm `float FlightSeconds, PerchSeconds, MinHeight, MaxHeight`; `MovesetSeed.For("khihang")` 4 chiêu; `PoseKind.Throw` (giơ tay ra sau, ngửa người: `PitchDeg -20`, `ScaleY 1,15`, `OffsetZ -0,15`, `Shake 0,2`); `PoseOf`: `Throw` cho `Throw`, `Crouch` cho `Perch`.

- [ ] **Step 1: Viết test**: `Monkey_Seed_ByAnyName` (`For("Khỉ Hang")`, `For("KhiHang(Clone)")` có 4 chiêu theo thứ tự `Ném Đá`, `Cắn Vặt`, `Nhảy Bục`, `Hú Báo Động`); `Monkey_Numbers_MatchTheSpec` (mọi số trong Global Constraints, gồm `FlightSeconds 0.9`, `MinHeight 1.5`, `MaxHeight 3`, `PerchSeconds 8`, `Distance 7` cho Nhảy Bục, `RetreatDistance 4` cho Cắn Vặt, `TargetSpecies` là khóa của Khỉ Hang); `PoseOf_ThrowAndPerch`; `Throw_RangeGate_FiveToFourteen` (lịch với riêng chiêu Ném Đá: khoảng cách 3 không bắt đầu, 8 bắt đầu, 15 không bắt đầu).
- [ ] **Step 2: Chạy, đỏ. Step 3: Cài. Step 4: Chạy lại, xanh. Commit.**

### Task 3: Viên đá và nhảy bục trong `CreatureCombat` (Unity)

**Files:**
- Create: `Assets/_Project/Scripts/Combat/MonsterProjectile.cs`
- Modify: `Assets/_Project/Scripts/Combat/CreatureCombat.cs`

**Interfaces:**
- Consumes: `ThrowRules`, `PerchRules`, `MoveScheduler.Allow`, `MoveDef` mới (Task 1, 2).
- Produces: `MonsterProjectile.Launch(Vector3 from, Vector3 to, float flightSeconds, float damage, float radius = 0.6f)`.

- [ ] **Step 1: Mã Unity, không có test chạy ngoài Unity; luật nó dựa vào đã có test ở Task 1 và 2.**
- [ ] **Step 2: `MonsterProjectile`:** quả cầu nhỏ (vật liệu `SkillFx.Gel` nâu), mỗi `FixedUpdate` đặt vị trí theo `ThrowRules.PositionAt`, gây sát thương một lần nếu người chơi cục bộ còn sống trong bán kính (`ThrowRules.Hits`) rồi tự hủy, tự hủy khi hết thời gian bay hoặc khi không còn người chơi; sát thương nhân `DamageScale` đã tính lúc ném.
- [ ] **Step 3: `CreatureCombat`:** hình `Throw` tại `BeginStrike`: bắn viên đá từ ngang ngực khỉ tới vị trí mục tiêu lúc đó. `Perch`: quét ứng viên chỗ cao (16 hướng × bán kính 3, 5, 7 ≤ `Distance`) bằng tia từ trên xuống, `PerchRules.PickLedge`, đặt `scheduler.Allow` để chiêu `Perch` chỉ dùng được khi có chỗ cao (làm mới 0,5 s một lần); khi vào `Active` nhảy theo cung `ThrowRules` trong `Active` giây bằng `rb.MovePosition`; xong thì ở trên `PerchSeconds` giây (host tạm dừng), lúc này truyền khoảng cách `dist × 0,875` cho lịch để tầm ném hiệu lực lên 16 m; hết giờ thì nhảy xuống cung 0,6 s về chỗ cũ rồi trả quyền cho host. Mọi đường thoát (chết, mất mục tiêu quá 3 s) đưa khỉ về trạng thái thường sạch.
- [ ] **Step 4: `syntaxcheck` 0 lỗi, `cstest` xanh. Commit.**

### Task 4: Menu thần, tài liệu, giao bên làm hang, gộp

**Files:**
- Modify: `Assets/_Project/Scripts/Dev/GodMenu.Tests.cs`, `docs/TEST_CHECKLIST_UNITY.md`, `docs/PROJECT_STATUS.md`, `docs/superpowers/handoff/2026-11-20-monster-tell-poses-for-cave-team.md`
- Create: `docs/superpowers/handoff/2026-11-20-monkey-species-for-cave-team.md`

- [ ] **Step 1: Nút "Khỉ Hang (3)" trong menu thần; mục kiểm thử H8; mục 57 trong PROJECT_STATUS; bản giao bên làm hang cho loài mới (mô hình, bộ xương, clip, `SpeciesSO` và prefab, chỗ đặt ở Vườn Đá Hổ Phách gồm dữ liệu sinh thái và ánh xạ chủ đề khoang `AmberPebbleGarden`, bảng rơi gene, ánh xạ tầng), clip `Tell_Throw` và `Tell_Perch` cho khỉ trong tài liệu clip.**
- [ ] **Step 2: Gộp `claude/movement` vào `claude/unity-test-maze`, chạy `cstest` và `syntaxcheck`, đẩy cả hai nhánh. Báo cáo bằng tiếng Việt, nói rõ phần chưa kiểm trong Unity.**
