# Chiến đấu của quái: bộ chiêu dùng chung, lát 1: kế hoạch thực hiện

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Quái có bộ chiêu báo trước (gồng, ra đòn, kiệt sức); 4 loài lát 1 chơi được, trong đó Muỗi Rêu đi theo bầy hút máu người chơi.

**Architecture:** Luật thuần trong `Scripts/Combat/Pure/` (lịch chiêu, luật muỗi, tư thế dấu hiệu, bảng chiêu theo loài) có test chạy ngoài Unity. Mã Unity mỏng: `CreatureCombat` chạy trên cả `Predator` và `Prey`, tạm giữ hành vi của chủ khi đang gồng, ra đòn hoặc kiệt sức.

**Tech Stack:** Unity 6.6 URP, C#, NUnit; `tools/cstest/run.sh` ngoài Unity, `tools/syntaxcheck/run.sh <ref>` kiểm cú pháp.

**Spec:** `docs/superpowers/specs/2026-11-20-monster-combat-movesets-design.md`

## Global Constraints

- Văn bản người chơi, tài liệu, báo cáo bằng tiếng Việt; mã và chú thích mã bằng tiếng Anh.
- Số liệu là bản tạm của spec, chỉnh được: `Rình-Vọt` gồng 0,6 s phóng 3 m kiệt 1,0 s sát thương 10; `Húc` gồng 0,9 s lao 7 m với tốc độ 2,2 × tốc độ đuổi kiệt 1,6 s (choáng 2 s khi đâm vách) sát thương 14 có giáp; `Chích-Hút` gồng 0,4 s hút 1,5 máu mỗi giây kiệt 0,8 s; Rùa Pha Lê: `Cắn Cổ` gồng 0,5 s tầm 2,5 m sát thương 22, `Rụt Mai–Lao` gồng 0,8 s lao 6 m sát thương 18 có giáp, `Xoay Mai` (dưới 66% máu) gồng 0,8 s bán kính 4 m, `Gọi Bầy` (dưới 33% máu) gồng 1,0 s bán kính 25 m, kiệt sức 1,5 s.
- Trong lúc kiệt sức, quái nhận sát thương ×1,5. Chiêu có giáp: vồ bật ra khi quái đang gồng hoặc ra đòn.
- Muỗi: phát hiện người chơi trong 14 m, bám khi cách 1,2 m, báo bầy trong 15 m; thoát khi nhảy, kỹ năng tức thời, trúng đòn, hoặc chạy từ 3 m/s trở lên trong 0,5 s; sợ 30 s rồi quay lại nếu người chơi còn trong 15 m.
- Hang đầu: thời gian gồng ×1,3; sát thương chiêu nhân hệ số sát thương của tầng (`CaveTier.DangerOf`) cho kẻ săn.
- Hoạt hình sẵn có (`Attack`, `Retract`, `Evade`…) dùng khi animator có trigger đó; không có thì dùng tư thế bằng code.
- Không phá sinh thái, tổ, `CreatureBrain`, `CreatureMotor`, vồ và ghì chặt; `Predator.cs` và `Prey.cs` chỉ thêm móc nhỏ.
- Commit kết thúc bằng hai dòng ghi công của phiên. Không tạo PR.
- Mỗi task: `tools/cstest/run.sh` xanh và `tools/syntaxcheck/run.sh origin/claude/unity-test-maze` 0 lỗi cú pháp trước khi commit.

## Review Focus

1. Quái đang gồng hoặc ra đòn thì chết, bị choáng, bị hất, hoặc người chơi chết/ra khỏi tầm: bộ máy về trạng thái chờ sạch, không kẹt `Busy` làm quái đứng im mãi (Task 1, 4).
2. Quái không có mục tiêu hợp lệ (trong vùng an toàn, Chúa Hang hiền, người chơi từ xa): không gồng, không ra đòn (Task 4).
3. Hai muỗi cùng bám một slime, hoặc muỗi bám khi slime đang ghì một quái: hút không cộng dồn lỗi, không kẹt trạng thái; slime chết thì muỗi thả ngay (Task 2, 4).
4. Chạy nhanh rồi dừng, hoặc nhảy khi không có muỗi: không làm gì, không lỗi (Task 2).
5. Quái có chiêu mà prefab thiếu mô hình con hoặc animator: tư thế bằng code không ném lỗi (Task 3, 4).

---

### Task 1: Lịch chiêu thuần

**Files:**
- Create: `Assets/_Project/Scripts/Combat/Pure/MoveScheduler.cs`
- Test: `Assets/_Project/Tests/EditMode/Combat/Pure/MoveSchedulerTests.cs`

**Interfaces:**
- Produces (namespace `Evolve.Combat`):
  - `enum MoveShape { Lunge, Charge, Spin, Drain, Call }`, `enum MovePhase { Idle, Windup, Active, Recover }`
  - `sealed class MoveDef` với các trường công khai, đặt bằng khởi tạo đối tượng: `string Name; MoveShape Shape; float Windup, Active, Recover, Cooldown, MinRange, MaxRange, Damage, Distance, Radius, Weight = 1f, UnlockBelow = 1f; bool Armored; string Anim; string TargetSpecies`
  - `sealed class MoveScheduler(IReadOnlyList<MoveDef> moves, int seed)` với `MovePhase Phase`, `MoveDef Current`, `float PhaseTime`, `float Progress01` (tiến độ trong pha hiện tại, 0 đến 1), `bool Busy` (`Phase != Idle`), `bool IsArmored` (`Current.Armored` và pha Windup hoặc Active), `float DamageTakenFactor` (1,5 khi Recover, ngược lại 1), `float TellScale` (hệ số nhân thời gian gồng, mặc định 1), `MoveDef Update(float dt, bool hasTarget, float targetDistance, float health01)` trả chiêu vừa chuyển sang Active (còn lại trả `null`), `void Stun(float extraRecover)` (chuyển thẳng sang Recover kéo dài thêm), `void Reset()`, `const float RecoverDamageFactor = 1.5f`.

- [ ] **Step 1: Viết test** (tên và khẳng định; `L` là chiêu `{Name="L", Shape=Lunge, Windup=0.6, Active=0.25, Recover=1.0, Cooldown=2.5, MinRange=0, MaxRange=5}`):
  - `Idle_UntilATargetIsInRange`: không mục tiêu, hoặc mục tiêu cách 6 m: `Phase == Idle` sau nhiều bước.
  - `Cycle_WindupActiveRecoverIdle`: mục tiêu cách 3 m, bước 0,1 s: `Phase` đi `Windup` (khoảng 0,6 s) rồi `Active`, `Recover`, rồi `Idle`; `Update` trả `L` đúng một lần, lúc vào `Active`.
  - `Armored_OnlyDuringWindupAndActive`: chiêu `Armored = true`: `IsArmored` đúng ở Windup và Active, sai ở Recover và Idle.
  - `Recover_TakesMoreDamage`: `DamageTakenFactor == 1.5f` ở Recover, `1f` còn lại.
  - `Cooldown_BlocksTheSameMoveAgain`: sau chu kỳ, mục tiêu vẫn ở 3 m: trong 2,5 s kể từ khi ra đòn không có gồng mới; sau đó có.
  - `HealthGate_UnlocksPhases`: chiêu A (`UnlockBelow = 1`) và B (`UnlockBelow = 0.66`, `MinRange..MaxRange` phủ khoảng cách thử): `health01 = 0.9` không bao giờ chọn B; `0.5` thì chọn được B.
  - `Range_PicksTheMoveThatFits`: chiêu gần (0 đến 2,5) và chiêu xa (3 đến 8): khoảng cách 5 chọn chiêu xa; 1 chọn chiêu gần.
  - `TellScale_StretchesTheWindup`: `TellScale = 1.3f`: `Windup` kéo dài `0,6 × 1,3 = 0,78 s`.
  - `Stun_GoesToRecoverAndExtends`: đang Active, `Stun(2f)`: `Phase == Recover` và còn khoảng `1,0 + 2,0` giây.
  - `TargetLost_DuringWindup_ReturnsToIdle`: `hasTarget = false` giữa Windup: `Phase == Idle` (không kẹt `Busy`).
  - `SameSeed_SameChoices`: hai lịch cùng `seed` và cùng bộ chiêu cho cùng chuỗi chiêu; có hai chiêu cùng dùng được thì không lặp chiêu vừa dùng nếu chiêu kia sẵn sàng.
- [ ] **Step 2: Chạy `tools/cstest/run.sh`, thấy lỗi biên dịch vì chưa có `MoveScheduler`.**
- [ ] **Step 3: Cài `MoveDef`, `MoveScheduler`.** Chọn chiêu khi Idle: lọc theo `UnlockBelow >= health01`, `MinRange <= targetDistance <= MaxRange`, hết hồi chiêu; chọn theo trọng số `Weight` bằng `System.Random(seed)`, loại chiêu vừa dùng nếu còn chiêu khác. Hồi chiêu đếm từ lúc ra đòn.
- [ ] **Step 4: Chạy lại, thấy xanh. Commit.**

### Task 2: Luật Muỗi hút máu (thuần)

**Files:**
- Create: `Assets/_Project/Scripts/Combat/Pure/LeechRules.cs`
- Test: `Assets/_Project/Tests/EditMode/Combat/Pure/LeechRulesTests.cs`

**Interfaces:**
- Produces:
  - `enum LeechState { Idle, Hunt, Drain, Scared }`
  - `struct LeechInput { float PlayerDistance; float PlayerSpeed; bool Hopped, SkillUsed, Hit; bool PlayerAlive; }`
  - `sealed class LeechRules` với `LeechState State`, `float StateTime`, `float Update(float dt, LeechInput input)` trả lượng máu hút trong bước này (`DrainPerSecond × dt` khi `Drain`), và hằng số `DetectRange = 14f`, `AttachRange = 1.2f`, `DetachRange = 2.5f`, `ReturnRange = 15f`, `ScaredSeconds = 30f`, `ShakeSpeed = 3f`, `ShakeSeconds = 0.5f`, `DrainPerSecond = 1.5f`, `AttachWindup = 0.4f`; `void Alert()` (báo bầy: `Idle` sang `Hunt`).

- [ ] **Step 1: Viết test**:
  - `Idle_NoticesThePlayerInRange`: `PlayerDistance 20` giữ `Idle`; `10` sang `Hunt`.
  - `Hunt_AttachesAfterTheWindup`: khoảng cách 1 m: bước 0,3 s vẫn `Hunt`, sau 0,5 s tổng là `Drain`.
  - `Drain_DrainsOnePointFivePerSecond`: ở `Drain`, 10 bước 0,1 s trả tổng `1.5f ± 0.01f`.
  - `Drain_ShakenOffByHop_Skill_Hit`: mỗi cái một lần: `Hopped`, `SkillUsed`, `Hit` sang `Scared`.
  - `Drain_ShakenOffByRunningFast`: `PlayerSpeed 3.5` trong 0,4 s vẫn `Drain`, tới 0,5 s sang `Scared`; tốc độ rớt xuống 1 m/s rồi lại tăng thì bộ đếm tính lại từ đầu.
  - `Scared_FliesAway_ThenReturnsOnlyIfThePlayerIsStillNear`: sau 30 s với `PlayerDistance 10`: sang `Hunt`; với `PlayerDistance 30`: sang `Idle`; ở 29 s vẫn `Scared`.
  - `Alert_WakesAnIdleMosquito`: `Alert()` khi `Idle` sang `Hunt`, khi `Scared` không đổi.
  - `PlayerDead_OrFarAway_ReleasesTheDrain`: `PlayerAlive = false`, hoặc `PlayerDistance 4` trong lúc `Drain`: về `Hunt` (không `Scared`).
  - `HopWithoutLeech_DoesNothing`: `Hopped = true` khi `Idle` hoặc `Hunt`: không đổi trạng thái.
- [ ] **Step 2: Chạy, thấy đỏ.**
- [ ] **Step 3: Cài `LeechRules`.**
- [ ] **Step 4: Chạy lại, xanh. Commit.**

### Task 3: Tư thế dấu hiệu và bảng chiêu theo loài (thuần)

**Files:**
- Create: `Assets/_Project/Scripts/Combat/Pure/TellPose.cs`, `Assets/_Project/Scripts/Combat/Pure/MovesetSeed.cs`
- Test: `Assets/_Project/Tests/EditMode/Combat/Pure/MovesetSeedTests.cs`

**Interfaces:**
- Consumes: `MoveDef`, `MoveShape`, `MovePhase` (Task 1), `SpeciesNames.Key`.
- Produces:
  - `enum PoseKind { Crouch, HornDown, Withdraw, Hover, Spin, Roar }`
  - `struct Pose { float ScaleX, ScaleY, ScaleZ, PitchDeg, OffsetY, OffsetZ, Shake; static Pose Identity }`
  - `static Pose TellPose.Evaluate(PoseKind kind, MovePhase phase, float progress01)`: dấu hiệu rõ ở Windup (tư thế tăng dần tới đỉnh), giữ đỉnh hay bung ở Active (tùy loại), hạ dần ở Recover, `Identity` ở Idle.
  - `static IReadOnlyList<MoveDef> MovesetSeed.For(string speciesIdOrName)` (rỗng khi loài không có bộ chiêu), `static bool Has(string)`, `static PoseKind PoseOf(MoveDef)`.

- [ ] **Step 1: Viết test**:
  - `Seed_HasTheFourSpecies_ByAnyName`: `For("Thằn Lằn Bờ Hồ")`, `For("ThanLanBoHo(Clone)")` có 1 chiêu tên `Rình-Vọt`; `BoGiapQuang` 1 chiêu `Húc` có `Armored`; `MuoiReu` 1 chiêu `Chích-Hút` hình `Drain`; `RuaPhaLe` 4 chiêu; loài khác (`Ốc Hồ`, `xyz`, rỗng) rỗng và `Has == false`.
  - `Numbers_MatchTheSpec`: `Rình-Vọt`: Windup 0.6, Active 0.25, Recover 1.0, Distance 3, Damage 10; `Húc`: Windup 0.9, Recover 1.6, Distance 7, Damage 14; `Chích-Hút`: Windup 0.4, Recover 0.8; Rùa: `Cắn Cổ` Windup 0.5, Damage 22, `UnlockBelow 1`; `Rụt Mai–Lao` Windup 0.8, Damage 18, `Armored`; `Xoay Mai` `UnlockBelow 0.66`, Radius 4; `Gọi Bầy` `UnlockBelow 0.33`, Radius 25, `TargetSpecies` khóa của `BoGiapQuang`, Windup 1.0; mọi chiêu Rùa `Recover 1.5`.
  - `Pose_IsIdentityWhenIdle_AndGrowsDuringWindup`: `Evaluate(Crouch, Idle, 0)` bằng `Identity`; `Evaluate(Crouch, Windup, 1)` có `ScaleY < 0.8f` (cúi rõ) và lớn hơn lệch so với `Windup, 0.5`; `Recover` về dần `Identity` (ở tiến độ 1 gần `Identity` trong 0,02).
  - `Pose_EveryKind_IsVisible`: với mỗi `PoseKind`, ở đỉnh Windup ít nhất một trong `|ScaleX-1|, |ScaleY-1|, |ScaleZ-1|, |PitchDeg|/90, |OffsetY|, |OffsetZ|` vượt `0.12f`, và không số nào là NaN.
  - `PoseOf_ByShape`: `Lunge` cho `Crouch`, `Charge` cho `HornDown` (trừ chiêu `Rụt Mai–Lao` cho `Withdraw`), `Drain` cho `Hover`, `Spin` cho `Spin`, `Call` cho `Roar`.
- [ ] **Step 2: Chạy, thấy đỏ.**
- [ ] **Step 3: Cài hai tệp.** Tư thế dùng đường cong mượt (`smoothstep`) theo tiến độ; số lớn đủ rõ (cúi: `ScaleY 0.62`, `ScaleZ 1.22`, `PitchDeg 14`; sừng cúi: `PitchDeg 22`, `OffsetZ -0.25` rung; rụt mai: `ScaleY 0.7`, `ScaleX/Z 1.15`; vờn: `OffsetY 0.5`, `ScaleY 1.15`; xoay: `ScaleY 0.85`, rung; gầm: `ScaleY 1.2`, `PitchDeg -15`).
- [ ] **Step 4: Chạy lại, xanh. Commit.**

### Task 4: `CreatureCombat` và các móc (Unity)

**Files:**
- Create: `Assets/_Project/Scripts/Combat/CreatureCombat.cs`
- Modify: `Assets/_Project/Scripts/Creatures/Predator.cs`, `Prey.cs`, `CreatureTuning.cs`, `Abilities/SkillBar.cs` (sự kiện `SkillUsed`), `Combat/Pounce.cs`

**Interfaces:**
- Consumes: `MoveScheduler`, `MoveDef`, `MovesetSeed`, `TellPose`, `LeechRules` (Task 1 đến 3), `CreatureMotor.Move`, `CreatureStatus.Stun`, `SafeZone.Contains`, `SkillFx.Ring`, `CreatureLocomotion.PlayAction`.
- Produces:
  - `CreatureCombat : MonoBehaviour` (thêm bởi `Predator.Awake` và `Prey.Awake` khi `MovesetSeed.Has(name)`): `bool Busy`, `bool IsArmored`, `float DamageTakenFactor`, `float TellScale`, `float DamageScale`, `static IReadOnlyList<CreatureCombat> All`, `static void Alert(Vector3 center, float radius, string speciesKey)`.
  - `static event Action<SurvivalStats> SkillBar.SkillUsed` (bắn sau khi kỹ năng chủ động kích hoạt thành công).
  - `Predator.IsCalm` (công khai, đọc `Calm`), `Predator.ChaseSpeed` (đọc `chaseSpeed`), `Predator.WantsToAttack(SurvivalStats p)`: `!Passive && !Calm && !SafeZone.Contains(vị trí p) && (TargetFilter == null || TargetFilter(p))`.

- [ ] **Step 1: Mã Unity, không có test chạy ngoài Unity; luật nó dựa vào đã có test ở Task 1 đến 3. Kiểm bằng `syntaxcheck` và danh sách thử Task 6.**
- [ ] **Step 2: `CreatureCombat`:**
  - Mỗi `FixedUpdate` chủ động chọn mục tiêu (người chơi cục bộ gần nhất còn sống; kẻ săn chỉ khi `WantsToAttack`; muỗi dùng `LeechRules`), chạy `MoveScheduler.Update`. Khi vào `Active` thực thi theo `Shape`: `Lunge` và `Charge` khóa hướng lúc hết gồng rồi di chuyển bằng `CreatureMotor.Move` (Lunge: `Distance` trong `Active`; Charge: `Distance` với tốc độ `2,2 × ChaseSpeed`); trúng người chơi (khoảng cách tâm không quá `bán kính thân + 0,9 m`) một lần mỗi chiêu thì `stats.State.Damage(Damage × DamageScale)` và đẩy lùi; `Charge` mà `Move` trả `false` (vách) thì `CreatureStatus.Stun(this, 2f)` và `scheduler.Stun(2f)`; `Spin` đẩy lùi và gây sát thương một lần trong bán kính; `Call` gọi `Predator.Provoke` cho loài `TargetSpecies` trong bán kính; `Drain` theo `LeechRules` (bám lên slime, `stats.State.Damage(drained)`, cộng máu cho muỗi, bay ra xa khi `Scared`, báo bầy bằng `Alert`).
  - `Busy` chặn hành vi chủ: `Predator.FixedUpdate` và `Prey.FixedUpdate` thoát sớm khi `combat != null && combat.Busy`; muỗi `Prey` cũng thoát sớm khi `LeechRules.State != Idle`.
  - Dấu hiệu: `LateUpdate` (`[DefaultExecutionOrder(60)]`, chạy sau `CreatureLocomotion`) áp `TellPose.Evaluate` lên nút mô hình con (`Model`, hoặc nút con đầu tiên có `Renderer`) bằng nhân `localScale` gốc, xoay thêm `PitchDeg`, cộng `OffsetY/Z`, rung theo `Shake`; khi gồng bật trigger hoạt hình `MoveDef.Anim` nếu animator có (qua `CreatureLocomotion.PlayAction`); chiêu vùng vẽ `SkillFx.Ring` bán kính chiêu trong lúc gồng. Mọi chỗ tìm nút con đều xử lý khi `null`.
  - Sạch sẽ: `OnDisable`, quái chết, người chơi chết hoặc mất mục tiêu thì `scheduler.Reset()` và trả nút mô hình về tư thế gốc.
- [ ] **Step 3: Các móc nhỏ:** `Predator.Contact`: quái có `CreatureCombat` thì bỏ cú cắn va chạm (vẫn nhận sát thương húc); `BiteRemotePlayers` thoát sớm khi có `CreatureCombat`; `TakeDamage` ở `Predator` và `Prey` nhân `combat.DamageTakenFactor`; `Pounce.TryPounce` coi mục tiêu `IsArmored` như `Bounce`; `CreatureTuning.Apply` đặt `combat.DamageScale` (kẻ săn: `CaveTier.DangerOf(tier).Damage`) và `combat.TellScale` (`1,3` khi tầng 0, ngược lại 1).
- [ ] **Step 4: `syntaxcheck` 0 lỗi, `cstest` xanh. Commit.**

### Task 5: Lối tắt trong menu thần

**Files:**
- Modify: `Assets/_Project/Scripts/Dev/GodMenu.Tests.cs`

**Interfaces:**
- Consumes: `CreatureCombat` (Task 4), `SpawnTest` có sẵn.

- [ ] **Step 1: Thêm dòng "Combat quái" ngay dưới mục Combat của slime:** nút tạo `Thằn Lằn Bờ Hồ` (khối 10, máu 34), `Bọ Giáp Quặng` (khối 18, máu 52), `Rùa Pha Lê` (khối 42, máu 115), và `Bầy Muỗi Rêu` (4 con); dòng trạng thái liệt kê quái có bộ chiêu quanh slime kèm pha hiện tại (`Idle/Windup/Active/Recover`) và trạng thái của muỗi gần nhất. Mã Unity.
- [ ] **Step 2: `syntaxcheck` 0 lỗi. Commit.**

### Task 6: Tài liệu, kế hoạch giao bên làm hang, gộp

**Files:**
- Create: `docs/superpowers/handoff/2026-11-20-monster-tell-poses-for-cave-team.md`
- Modify: `docs/TEST_CHECKLIST_UNITY.md`, `docs/PROJECT_STATUS.md`

- [ ] **Step 1: Viết kế hoạch giao bên làm hang** (tiếng Việt): với mỗi loài trong lát 1, danh sách clip hoạt hình cần có (`Tell`, `Strike`, `Recover` cho từng chiêu, tên trigger khớp `MoveDef.Anim`), mô tả tư thế cần nhìn thấy rõ ở dạng cơ thể (Thằn Lằn cuộn cúi, Bọ Giáp cào đất cúi sừng, Muỗi giương vòi và xòe cánh, Rùa rụt đầu chân vào mai rồi mở), yêu cầu dữ liệu (`AnimatorController` trigger, thời lượng khớp bảng chiêu, root motion tắt), cách thử trong menu thần, và việc mã làm khi có clip hoặc không.
- [ ] **Step 2: Mục kiểm thử mới trong checklist và mục mới trong PROJECT_STATUS** (đã làm, chưa kiểm trong Unity, những gì chưa làm).
- [ ] **Step 3: Gộp `claude/movement` vào `claude/unity-test-maze`, chạy `cstest` và `syntaxcheck`, đẩy cả hai nhánh. Báo cáo bằng tiếng Việt, nói rõ phần chưa kiểm trong Unity.**
