# Chiến đấu của quái, lát 2: Chồn Khe, Cáo Gió, rình: kế hoạch thực hiện

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Thêm Chồn Khe (rình ẩn mình bằng mắt sáng, vồ xa rồi rút lui) và Cáo Gió (Băng Rơi, Gió Hú, Vồ Cắn, Gọi Bầy) vào bộ chiêu dùng chung.

**Architecture:** Mở rộng luật thuần (`MoveDef`, `MoveScheduler.Force`, `WindRules`, `AmbushRules`, `MovesetSeed`, `TellPose`) có test ngoài Unity, rồi mở rộng `CreatureCombat` cho hình chiêu mới.

**Tech Stack:** Unity 6.6 URP, C#, NUnit; `tools/cstest/run.sh`, `tools/syntaxcheck/run.sh <ref>`.

**Spec:** `docs/superpowers/specs/2026-11-20-monster-combat-movesets-design.md` (mục "Lát 2", đã chốt).

## Global Constraints

- Văn bản người chơi, tài liệu, báo cáo bằng tiếng Việt; mã và chú thích mã bằng tiếng Anh. Số tạm, chỉnh được.
- Chồn Khe: `Vồ Xa` gồng 0,35 s, Active 0,5 s, phóng 8 m, sát thương 16, tầm 3 đến 9, rút lui 6 m trong 1,5 s kiệt sức, hồi chiêu 4 s. Rình: lộ ra khi người chơi trong 5 m, gồng nhanh 0,2 s; rình lại sau 20 s không có mục tiêu.
- Cáo Gió: `Vồ Cắn` gồng 0,5 s, phóng 4 m, sát thương 22; `Băng Rơi` (tầm 4 đến 12) gồng 1,0 s, vòng báo dưới chân mục tiêu, rơi sau 1,2 s, bán kính 1,5 m, sát thương 12, đóng băng 1,5 s, hồi chiêu 6 s; `Gió Hú` (dưới 66% máu) gồng 0,9 s, đẩy 1,2 s tốc độ 6 m/s; `Gọi Bầy` (dưới 33% máu) gồng 1,0 s, gọi Chồn Khe trong 30 m. Mọi chiêu cáo kiệt sức 1,5 s.
- Lực đẩy gió: nhân hệ số theo khối slime (1 tới khối 10; 0,5 từ khối 40; tuyến tính ở giữa); Thu Mình (đứng im, `Rooted`) và Bám Vách (`Climbing`) không bị đẩy.
- Không phá lát 1, ghì chặt, vồ, sinh thái. Commit kết thúc bằng hai dòng ghi công của phiên. Không tạo PR.
- Mỗi task: `tools/cstest/run.sh` xanh và `tools/syntaxcheck/run.sh origin/claude/unity-test-maze` 0 lỗi cú pháp trước khi commit.

## Review Focus

1. Chồn ẩn mình mà bị giết, bị choáng, bị hất, hoặc người chơi vào vùng an toàn: hình hiện lại, đôi mắt được dọn, không kẹt `Busy` (Task 2, 4).
2. Băng Rơi khi mục tiêu chết hoặc ra xa giữa lúc băng chưa rơi: không lỗi, không đóng băng người không ở dưới (Task 4).
3. Gió Hú vào slime đang bám (ghì chặt) hoặc đang `Climbing`/`Rooted`: không đẩy (Task 2, 4).
4. Rút lui đâm vách (`Move` trả `false`): dừng, không trượt xuyên vách (Task 4).
5. Vồ vào chồn đang ẩn: không chọn được (Task 4).

---

### Task 1: Chiêu mới trong dữ liệu và lịch (thuần)

**Files:**
- Modify: `Assets/_Project/Scripts/Combat/Pure/MoveScheduler.cs`
- Test: `Assets/_Project/Tests/EditMode/Combat/Pure/MoveSchedulerTests.cs`

**Interfaces:**
- Produces: `MoveShape` thêm `IceDrop`, `Gust`; `MoveDef` thêm `float RetreatDistance, Force, Freeze`; `bool MoveScheduler.Force(MoveDef move, float windupSeconds)` (chỉ khi `Idle`; vào `Windup` với độ dài đã cho, không nhân `TellScale`; trả `true` nếu thành công; hồi chiêu chiêu đó tính như thường khi vào Active).

- [ ] **Step 1: Viết test**: `Force_StartsAQuickWindup` (`Force(L, 0.2f)`; sau 0,15 s vẫn `Windup`; sau 0,25 s tổng là `Active`; trả `true`), `Force_WhenBusy_IsRefused` (đang `Windup`: `Force` trả `false`), `Force_Ignores_TellScale` (`TellScale = 2f`: vẫn vào Active sau 0,25 s), `Force_StartsTheCooldown` (sau chu kỳ, `Update` tự chọn cùng chiêu bị chặn tới hết hồi chiêu).
- [ ] **Step 2: Chạy, thấy đỏ. Step 3: Cài. Step 4: Chạy lại, xanh. Commit.**

### Task 2: `WindRules` và `AmbushRules` (thuần)

**Files:**
- Create: `Assets/_Project/Scripts/Combat/Pure/WindRules.cs`, `Assets/_Project/Scripts/Combat/Pure/AmbushRules.cs`
- Test: `Assets/_Project/Tests/EditMode/Combat/Pure/AmbushRulesTests.cs`

**Interfaces:**
- Produces:
  - `static float WindRules.PushSpeed(float force, float slimeMass, bool resists)`: `force × (1 − 0,5 × clamp01((khối − 10) / 30))`, bằng 0 khi `resists`.
  - `enum AmbushState { Lurking, Revealed, Roaming }`; `sealed class AmbushRules` với `AmbushState State` (bắt đầu `Lurking`), `bool Update(float dt, float playerDistance)` (trả `true` đúng một lần khi `Lurking` chuyển sang `Revealed`; `playerDistance = float.MaxValue` khi không có mục tiêu hợp lệ), `void StrikeDone()` (`Revealed` sang `Roaming`), `void Reset()` (về `Lurking`), hằng `RevealRange = 5f`, `LurkAfter = 20f`, `RoamNearRange = 15f`.

- [ ] **Step 1: Viết test**: `PushSpeed_ByMass` (khối 5 và 10 giữ nguyên `6`; khối 25 là `4,5`; khối 40 và 80 là `3`; `resists = true` là `0`); `Ambush_StartsLurking_AndStaysHiddenBeyondFiveMetres` (6 m không lộ); `Ambush_RevealsOnceWithinFive` (4 m trả `true`, lần sau `false`, `State == Revealed`); `Ambush_AfterTheStrike_RoamsThenLurksAgainAfterTwentySeconds` (`StrikeDone()` sang `Roaming`; 19 s với `playerDistance = MaxValue` vẫn `Roaming`, 20 s sang `Lurking`); `Ambush_PlayerNearResetsTheLurkTimer` (người chơi trong 15 m mỗi vài giây thì không bao giờ `Lurking`); `Ambush_RoamingPlayerWithinFiveDoesNotRevealAgain` (đang `Roaming`, 3 m: không trả `true`).
- [ ] **Step 2: Chạy, đỏ. Step 3: Cài. Step 4: Chạy lại, xanh. Commit.**

### Task 3: Bảng chiêu Chồn Khe, Cáo Gió và tư thế Cast (thuần)

**Files:**
- Modify: `Assets/_Project/Scripts/Combat/Pure/MovesetSeed.cs`, `Assets/_Project/Scripts/Combat/Pure/TellPose.cs`
- Test: `Assets/_Project/Tests/EditMode/Combat/Pure/MovesetSeedTests.cs`

**Interfaces:**
- Consumes: Task 1, 2.
- Produces: `MovesetSeed.For` trả bộ chiêu cho `chonkhe` (1 chiêu `Vồ Xa`) và `caogio` (4 chiêu); `static bool MovesetSeed.IsAmbusher(string)` (đúng cho Chồn Khe); `static MoveDef MovesetSeed.AmbushMove(string)` (chiêu dùng khi lộ ra, `Vồ Xa`); `PoseKind.Cast` (tư thế đưa tay lên: `PitchDeg -12`, `ScaleY 1,15`, `OffsetY 0,2`, `Shake 0,4`); `PoseOf`: `IceDrop` cho `Cast`, `Gust` cho `Roar`.

- [ ] **Step 1: Viết test**: `Slice2_Seed_ByAnyName` (`For("Chồn Khe")`, `For("ChonKhe(Clone)")` có `Vồ Xa`; `For("Cáo Gió")`, `For("CaoGio")` có 4 chiêu: `Vồ Cắn`, `Băng Rơi`, `Gió Hú`, `Gọi Bầy`); `Slice2_Numbers_MatchTheSpec` (Vồ Xa: Windup 0.35, Active 0.5, Recover 1.5, Distance 8, Damage 16, MinRange 3, MaxRange 9, RetreatDistance 6, Cooldown 4; Vồ Cắn: Windup 0.5, Distance 4, Damage 22; Băng Rơi: Windup 1.0, Active 1.2, Radius 1.5, Damage 12, Freeze 1.5, MinRange 4, MaxRange 12, Cooldown 6; Gió Hú: Windup 0.9, Active 1.2, Force 6, UnlockBelow 0.66; Gọi Bầy: Windup 1.0, Radius 30, UnlockBelow 0.33, `TargetSpecies` là khóa của Chồn Khe; mọi chiêu cáo Recover 1.5); `Ambusher_OnlyTheWeasel` (`IsAmbusher("Chồn Khe")` đúng; `Thằn Lằn`, `Cáo Gió` sai; `AmbushMove("Chồn Khe").Name == "Vồ Xa"`); `PoseOf_IceDropAndGust` (`Cast`, `Roar`). Test `Pose_EveryKind_IsVisible_AndFinite` có sẵn tự phủ `Cast`.
- [ ] **Step 2: Chạy, đỏ. Step 3: Cài. Step 4: Chạy lại, xanh. Commit.**

### Task 4: `CreatureCombat` cho hình chiêu mới (Unity)

**Files:**
- Create: `Assets/_Project/Scripts/Combat/WindPush.cs`
- Modify: `Assets/_Project/Scripts/Combat/CreatureCombat.cs`, `Combat/Pounce.cs`, `Abilities/BodyState.cs` (thêm `Rooted`)

**Interfaces:**
- Consumes: `WindRules`, `AmbushRules`, `MovesetSeed.IsAmbusher/AmbushMove`, `MoveScheduler.Force`, `MoveDef` mới (Task 1 đến 3).
- Produces: `CreatureCombat.IsHidden` (`bool`); `WindPush.Apply(SurvivalStats slime, Vector3 velocity, float seconds)` (thành phần tự dọn, dịch chuyển slime mỗi bước vật lý, dừng khi vướng vách); `BodyState.Rooted`.

- [ ] **Step 1: Mã Unity, không có test chạy ngoài Unity; luật nó dựa vào đã có test ở Task 1 đến 3.**
- [ ] **Step 2: Cài** theo spec: `IceDrop` (đầu Active: ghi vị trí mục tiêu, `SkillFx.Ring` bán kính chiêu màu xanh băng kéo dài `Active` giây; vào `Recover`: người chơi cục bộ trong bán kính thì `Damage` và `BodyState.Root(Freeze)` cộng `SlimeController.FreezeFor(Freeze)`), `Gust` (mỗi bước ở `Active` đẩy mục tiêu trong tầm bằng `WindPush` theo `WindRules.PushSpeed` với `resists = BodyState.Rooted || BodyState.Climbing || LatchSession.Active != null`), `Retreat` (vào `Recover` của chiêu có `RetreatDistance > 0`: chạy ngược hướng mục tiêu với tốc độ `RetreatDistance / Recover`, dừng khi `Move` trả `false`), `Rình` (`AmbushRules` cho loài `IsAmbusher`: khi `Lurking` thì `Busy` đúng, tắt mọi `Renderer`, hiện đôi mắt sáng gồm hai quả cầu nhỏ vật liệu `SkillFx.Gel` và một đèn điểm mờ; khi lộ thì bật lại hình, dọn mắt, `scheduler.Force(AmbushMove, 0.2f)`; chiêu xong thì `StrikeDone()`; `HostPaused` hoặc chết thì dọn mắt và hiện hình).
- [ ] **Step 3: `Pounce.Add` bỏ qua quái `IsHidden`.**
- [ ] **Step 4: `syntaxcheck` 0 lỗi, `cstest` xanh. Commit.**

### Task 5: Menu thần, tài liệu, giao bên làm hang, gộp

**Files:**
- Modify: `Assets/_Project/Scripts/Dev/GodMenu.Tests.cs`, `docs/TEST_CHECKLIST_UNITY.md`, `docs/PROJECT_STATUS.md`, `docs/superpowers/handoff/2026-11-20-monster-tell-poses-for-cave-team.md`

- [ ] **Step 1: Thêm nút Chồn Khe (3 con), Cáo Gió; mục kiểm thử H7; mục 56 trong PROJECT_STATUS; bản bổ sung clip cho Chồn Khe (rình nằm, bật ra, chạy trốn) và Cáo Gió (đưa tay phép, hú, gầm) trong tài liệu giao.**
- [ ] **Step 2: Gộp `claude/movement` vào `claude/unity-test-maze`, chạy `cstest` và `syntaxcheck`, đẩy cả hai nhánh. Báo cáo bằng tiếng Việt, nói rõ phần chưa kiểm trong Unity.**
