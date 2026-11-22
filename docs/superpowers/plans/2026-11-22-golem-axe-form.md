# Golem dạng rìu và phòng trùm: kế hoạch thực hiện

> **Dành cho người thực hiện:** dùng superpowers:subagent-driven-development (khuyến nghị) hoặc superpowers:executing-plans để làm từng task. Các bước dùng hộp kiểm `- [ ]`.

**Mục tiêu:** Golem có dạng rìu (rút rìu 3 nhịp, đánh thường dày hơn, ném rìu rồi đi nhặt lại, vòng hất người lại gần lúc kéo) và một khối `BossRoom` để bên dựng map nối vào.

**Kiến trúc:** logic thuần C# (`Assets/_Project/Scripts/Combat/Pure/`, chạy bằng `tools/cstest/run.sh`, theo TDD) cho trạng thái rìu, đi lấy rìu, kéo nhọc nhằn, vòng hất, sóng Bổ Đất; lớp Unity mỏng gọi chúng từ `CreatureCombat`/`GolemRigFx`. Đổi dạng dùng `MoveScheduler.Allow` lọc theo `MoveDef.Form`.

**Công nghệ:** Unity 6 URP, C#, NUnit (qua `tools/cstest`, .NET 8), Blender không cần, ElevenLabs cho âm thanh (khóa nằm trong secret môi trường, không ghi vào repo).

**Tài liệu thiết kế:** `docs/superpowers/specs/2026-11-22-golem-axe-form-design.md`

## Ràng buộc chung
- Unity không chạy được trong môi trường này: mọi thứ phía Unity là "chưa kiểm chứng", báo thẳng; chủ dự án test và gửi Console/ảnh.
- Mã và chú thích mã bằng tiếng Anh; báo cáo và tài liệu bằng tiếng Việt.
- Chỉ đẩy lên `claude/movement` và `claude/unity-test-maze` (`git pull --no-rebase origin claude/unity-test-maze` trước khi đẩy); không tạo PR; không commit khóa API.
- Giữ nguyên: hệ phase (66%/33%), `PlayerReader`, `CircleRules`, `Invulnerable`, bộ chiêu tay không hiện có; số liệu trong spec là **số tạm**: kéo rìu **3 nhịp, 3,5 s**; vòng hất **4 m, lực 10, 6 sát thương, choáng 1 s, giãn cách 1,5 s**; đổi dạng ở **máu ≤ 50%**, một chiều; nhặt lại sau ít nhất **1,5 s** tay không.
- Mỗi dạng đủ bộ chuẩn: 2 thường, 1 đặc biệt, 1 ultimate (tay không có thêm combo Húc Đầu và không có đánh xa; dạng rìu có Ném Rìu làm đánh xa).
- Chạy test: `tools/cstest/run.sh` (mong đợi `Passed!`, 0 failed); kiểm cú pháp Unity: `cd tools/syntaxcheck && dotnet run -c Release -- /home/user/gametienhoa <file chứa đường dẫn>`, bỏ qua nhiễu CS0246/CS0103/CS1061/CS1503 và các cảnh báo cũ của `Predator.cs`.

## Trọng tâm rà soát (mỗi dòng có test ở task sở hữu)
1. Rìu ném rơi chỗ không tới được (tường, hố): phải rơi xuống chân Golem, trận không kẹt (Task 3).
2. Máu 50% đến đúng lúc Golem đang gầm chuyển phase: chờ gầm xong mới đi rút (Task 3).
3. Golem hồi máu trên 50% sau khi đã cầm rìu: không quay về hẳn tay không (Task 3).
4. Hai người chơi cùng lại gần lúc kéo: hồi hất tính riêng từng người (Task 5).
5. Người chơi chết hoặc đi mất giữa lúc Golem đang đi lấy rìu: không đứng chờ mãi, hết thời gian đi thì vẫn kéo tại chỗ (Task 4).

## Cấu trúc file
- Tạo (thuần): `Combat/Pure/AxeRules.cs` (`AxeFsm`), `Combat/Pure/FetchFsm.cs`, `Combat/Pure/AuraRules.cs` (`PushAura`), `Combat/Pure/ShockwaveRules.cs`
- Sửa (thuần): `Combat/Pure/MoveScheduler.cs` (`MoveForm`, `MoveDef.Form`, hình dạng chiêu mới), `Combat/Pure/MeteorRules.cs` (`PullRules` theo số nhịp), `Combat/Pure/MovesetSeed.cs`, `Combat/Pure/GolemRules.cs`
- Tạo (Unity): `Combat/GolemAxe.cs`, `Combat/AxeFlight.cs`, `World/BossRoom.cs`
- Sửa (Unity): `Combat/CreatureCombat.cs`, `Combat/GolemRigFx.cs`, `Combat/GolemFx.cs`
- Test: thêm `AxeRulesTests.cs`, `FetchFsmTests.cs`, `AuraRulesTests.cs`, `ShockwaveRulesTests.cs`; sửa `PullRulesTests` (trong `MeteorRulesTests.cs`), `BossKitTests.cs`, `GolemRulesTests.cs`, `GolemBodyTests.cs`.
- Docs: `docs/BOSS_ROOM_CONTRACT.md` (cho phiên dựng map), `docs/TEST_CHECKLIST_UNITY.md`.

---

### Task 1: Dạng của chiêu và bộ chiêu dạng rìu
**Files:** Sửa `Combat/Pure/MoveScheduler.cs`, `Combat/Pure/MovesetSeed.cs`, `Tests/EditMode/Combat/Pure/BossKitTests.cs`, `Tests/EditMode/Combat/Pure/GolemRulesTests.cs`

**Interfaces:**
- Produces: `public enum MoveForm { Both, Fist, Axe }`; `MoveDef.Form` (mặc định `Both`); `MoveShape` thêm `AxeThrow, Shockwave, Fetch`; `MovesetSeed.For("Golem Thạch Anh")` trả **cả hai dạng**; `MovesetSeed.ForForm(string species, MoveForm form) -> IReadOnlyList<MoveDef>` trả các chiêu có `Form == Both` hoặc `== form`.

- [ ] **Step 1: Viết test hỏng** trong `BossKitTests.cs`: `GolemKit_EachFormHasTheStandardKit` — với `form` thuộc `{Fist, Axe}` lấy `ForForm("Golem Thạch Anh", form)`: đúng 2 `Role.Normal`, đúng 1 `Role.Special` (Fist: Giáng Thạch `Dodge`; Axe: Bổ Đất `Dodge`), đúng 1 `Role.Ultimate` (Lõi Nổ, `UnlockBelow ≤ 0.25`), Axe có đúng 1 `Role.Ranged` tên "Ném Rìu" `MinRange ≥ 5`, Fist có đúng 1 `Role.Combo` "Húc Đầu"; `Chém Dọc.Combo == {"Chém Ngang","Chém Dọc"}`; "Giáng Thạch" và "Húc Đầu" có `Form == Fist`, "Lõi Nổ" có `Both`. Cập nhật `GolemRulesTests`: số chiêu của `For(...)` = 5 tay không + 4 rìu (Chém Dọc, Chém Ngang, Ném Rìu, Bổ Đất) = 9.
- [ ] **Step 2: Chạy** `tools/cstest/run.sh`; mong đợi: lỗi biên dịch `MoveForm`/`ForForm` chưa có.
- [ ] **Step 3: Cài đặt** `MoveForm`, `MoveDef.Form`, các hình dạng mới, `ForForm` trong `MovesetSeed`; thêm 4 chiêu rìu vào bộ golem với `Form = MoveForm.Axe` và số tạm trong spec (Chém Dọc 0,8/0,25/1,2 s sát thương 30 MaxRange 4; Chém Ngang `Spin` 0,7 s sát thương 26 Force 8 Radius 4; Ném Rìu `AxeThrow` gồng 1,4 s bay 1,0 s 6–20 m sát thương 34 hồi 14 s `Role.Ranged`; Bổ Đất `Shockwave` gồng 1,8 s hồi 20 s `Dodge` sát thương 30 `Role.Special`), gắn `Form = Fist` cho Nện Đấm, Quét Tay, Húc Đầu, Giáng Thạch.
- [ ] **Step 4: Chạy lại** `tools/cstest/run.sh`; mong đợi `Passed!`.
- [ ] **Step 5: Commit** `git add -A && git commit -m "Golem: move forms (fist and axe) and the axe move set"`.

### Task 2: Kéo nhọc nhằn theo số nhịp
**Files:** Sửa `Combat/Pure/MeteorRules.cs`, `Tests/EditMode/Combat/Pure/MeteorRulesTests.cs`

**Interfaces:**
- Produces: `PullRules.Lift(float t01, int heaves = 4)`, `Strain(float t01, int heaves = 4)`, `Heave(float t01, int heaves = 4)`; hằng `PullRules.AxeHeaves = 3`, `PullRules.AxeSeconds = 3.5f`. Giá trị mặc định giữ nguyên hành vi cũ (4 nhịp).

- [ ] **Step 1: Viết test hỏng** `Pull_ThreeHeaves_FreesTheAxeOnlyOnTheLastOne`: `Lift(0.5f, 3) < 0.45`, `Lift(0.66f, 3) < 0.6` (chưa rút ở nhịp 2), `Lift(1f, 3) == 1`, tăng đơn điệu, `Strain(0.05f,3) > 0.9`, `Heave(0.2f,3)` lớn nhưng `Heave(0.05f,3) == 0`; test cũ 4 nhịp vẫn xanh.
- [ ] **Step 2: Chạy** `tools/cstest/run.sh`; mong đợi: lỗi biên dịch (thiếu tham số).
- [ ] **Step 3: Cài đặt** tham số `heaves` (bảng tiến triển `Gained` cho 3 nhịp: `{0, 0.12, 0.35, 1}`; 4 nhịp giữ `{0, 0.08, 0.2, 0.4, 1}`).
- [ ] **Step 4: Chạy lại**; mong đợi `Passed!`.
- [ ] **Step 5: Commit** `"PullRules: heaves parameter (3 for the axe)"`.

### Task 3: Trạng thái cây rìu (`AxeFsm`)
**Files:** Tạo `Combat/Pure/AxeRules.cs`, `Tests/EditMode/Combat/Pure/AxeRulesTests.cs`

**Interfaces:**
- Produces: `enum AxePlace { Anchor, Ground, Held, Flying }`; `enum AxeLanding { AtImpact, AtGolem }`; `sealed class AxeFsm` với `AxePlace Place` (khởi đầu `Anchor`), `bool EverArmed`, `MoveForm Form` (`Axe` khi `Place == Held`, `Fist` còn lại), `void Tick(float dt)` (tích thời gian tay không), `bool ShouldFetch(float health01, bool golemIdle, bool phaseRoarPending)`, `void Pulled()` (→ `Held`, `EverArmed = true`), `void Thrown()` (`Held`→`Flying`), `AxeLanding Landed(bool reachable)` (`Flying`→`Ground`; trả `AtGolem` nếu `!reachable`).

- [ ] **Step 1: Viết test hỏng** (`AxeRulesTests`): `Fetches_AtHalfHealthWhenIdle` (`ShouldFetch(0.5f,true,false)` đúng, `0.51f` sai, không idle sai); `WaitsForThePhaseRoar` (`phaseRoarPending = true` ở 0.4f → sai, hết cờ → đúng); `AfterAThrow_WaitsOneAndAHalfSecondsBareHanded` (sau `Thrown()`, `Landed(true)`, `ShouldFetch` sai đến khi `Tick` tổng ≥ 1,5 rồi đúng); `UnreachableLanding_DropsAtTheGolem` (`Landed(false) == AtGolem`); `StaysArmedFamily_WhenItMendsAbove50` (sau `Pulled()` rồi `Thrown()`+`Landed`, `ShouldFetch(0.9f,true,false)` vẫn đúng khi hết 1,5 s, `EverArmed` đúng); `Form` bằng `Axe` chỉ khi `Held`.
- [ ] **Step 2: Chạy** `tools/cstest/run.sh`; mong đợi: lỗi biên dịch.
- [ ] **Step 3: Cài đặt** `AxeFsm` theo Interfaces; điều kiện đi lấy: (`Place == Anchor` và `health01 ≤ 0.5` và idle và không chờ gầm) hoặc (`Place == Ground` và thời gian tay không ≥ 1,5 và idle và không chờ gầm).
- [ ] **Step 4: Chạy lại**; mong đợi `Passed!`.
- [ ] **Step 5: Commit** `"AxeFsm: where the axe is and when the golem fetches it"`.

### Task 4: Đi lấy đồ (`FetchFsm`)
**Files:** Tạo `Combat/Pure/FetchFsm.cs`, `Tests/EditMode/Combat/Pure/FetchFsmTests.cs`

**Interfaces:**
- Produces: `enum FetchStage { Walk, Pull, Done }`; `enum FetchEvent { None, PullStart, Done }`; `sealed class FetchFsm(float pullSeconds)` với `Stage`, `StageTime`, `const float WalkTimeout = 4f`, `FetchEvent Update(float dt, bool arrived)`, `void Abort()` (→ `Done`).

- [ ] **Step 1: Viết test hỏng**: `WalksThenPullsThenIsDone` (`arrived` sau 0,3 s → `PullStart`, sau `pullSeconds` → `Done`); `GivesUpWalkingAfterTheTimeout` (không bao giờ `arrived` → `PullStart` ở ≈ `WalkTimeout`, tức người chơi mất hay rìu kẹt vẫn kéo tại chỗ); `AbortEndsAtOnce`; không sự kiện sau `Done`.
- [ ] **Step 2: Chạy**; mong đợi: lỗi biên dịch.
- [ ] **Step 3: Cài đặt** `FetchFsm` (không dùng lại `VolleyFsm`).
- [ ] **Step 4: Chạy lại**; mong đợi `Passed!`.
- [ ] **Step 5: Commit** `"FetchFsm: walk to the thing, then pull it out"`.

### Task 5: Vòng hất và sóng Bổ Đất
**Files:** Tạo `Combat/Pure/AuraRules.cs`, `Combat/Pure/ShockwaveRules.cs`, `Tests/EditMode/Combat/Pure/AuraRulesTests.cs`, `Tests/EditMode/Combat/Pure/ShockwaveRulesTests.cs`

**Interfaces:**
- Produces: `sealed class PushAura` với hằng `Radius = 4f, Cooldown = 1.5f, Damage = 6f, Force = 10f, Freeze = 1f` và `bool TryPush(int playerId, float distance, float now)` (đúng nếu `distance ≤ Radius` và qua `Cooldown` kể từ lần hất người đó trước); `static class ShockwaveRules` với `float Front(float speed, float t, float length)` (mép sóng, kẹp ở `length`), `bool Hits(float ox, float oz, float dx, float dz, float length, float width, float front, float depth, float px, float pz)` (người nằm trong dải `width` quanh tia từ gốc, trong khoảng `front ± depth/2`, không vượt `length`).

- [ ] **Step 1: Viết test hỏng**: `TwoPlayers_HaveTheirOwnCooldown` (hất người 1 rồi ngay lập tức người 2 vẫn đúng, người 1 lại sai trước 1,5 s và đúng sau); `OutsideRadius_NeverPushed`; `Shockwave_HitsOnlyInsideTheMovingBand` (người ở bên ngoài `width/2` không trúng, người đứng sau mép sóng chưa trúng, người ngay mép sóng trúng, vượt `length` không trúng); `Front_ClampsAtLength`.
- [ ] **Step 2: Chạy**; mong đợi: lỗi biên dịch.
- [ ] **Step 3: Cài đặt** hai lớp (toán phẳng, không dùng Unity).
- [ ] **Step 4: Chạy lại**; mong đợi `Passed!`.
- [ ] **Step 5: Commit** `"PushAura and ShockwaveRules"`.

### Task 6: Tư thế dạng rìu và cú vung tay
**Files:** Sửa `Combat/Pure/GolemRules.cs`, `Tests/EditMode/Combat/Pure/GolemBodyTests.cs`

**Interfaces:**
- Produces: các `anim` mới trong `GolemRules.Evaluate`: `"AxeChop"` (giơ cao hai tay rồi bổ xuống, `Hunch` dương ở đòn), `"AxeSweep"` (vặn thân, quét ngang), `"AxeThrow"` (một tay ra sau rồi ném, `Back` lớn rồi âm), `"AxeSlam"` (giơ qua đầu lâu hơn rồi đập, `Crouch` dương ở đòn), `"AxePull"` (hai tay kéo, dùng lại tư thế `VolleyStage.Pull`); `static ArmPose Swat(float seconds)` (cú vung tay hất, 0,35 s, hồi về 0).

- [ ] **Step 1: Viết test hỏng**: `AxeChop_RaisesBothArms_ThenDrivesThemDown` (`Raise > 120` cuối gồng, `Hunch > 25` ở đòn); `AxeThrow_CocksTheRightArmThenThrows` (`Back > 90`, rồi `Back < 0`); `Swat_FlickAndSettles` (`Swat(0.1f)` lớn, `Swat(0.4f)` ≈ 0); thêm các anim mới vào vòng kiểm giới hạn của `GolemBodyTests` (`Crouch/Tuck/Kneel` ∈ [0,1], `Twist` ∈ [-60,60], `Back` ∈ [-80,130]).
- [ ] **Step 2: Chạy**; mong đợi: thất bại vì anim chưa có.
- [ ] **Step 3: Cài đặt** các khóa tư thế theo mô tả trong Interfaces, theo kiểu `Keys` hiện có.
- [ ] **Step 4: Chạy lại**; mong đợi `Passed!`.
- [ ] **Step 5: Commit** `"GolemRules: axe poses and the swat"`.

### Task 7: Lớp Unity: rìu, đổi dạng, đi lấy, ném, bổ đất, vòng hất (chưa kiểm chứng)
**Files:** Tạo `Combat/GolemAxe.cs`, `Combat/AxeFlight.cs`; sửa `Combat/CreatureCombat.cs`, `Combat/GolemRigFx.cs`, `Combat/GolemFx.cs`

**Interfaces:**
- Consumes: `AxeFsm`, `FetchFsm`, `PullRules`, `PushAura`, `ShockwaveRules`, `MoveForm`, `GolemRules.Swat`, `MonsterProjectile`-kiểu bay từ `RockSpike`.
- Produces: `GolemAxe` (MonoBehaviour trên Golem): `Transform Visual` (hình khối cán và lưỡi), `void PlaceAt(Vector3 pos, Quaternion rot)` (cắm tại chỗ), `void Hold(Transform hand)`, `AxeFlight.Launch(GolemAxe, Vector3 to, float flightSeconds, float damage)`; `CreatureCombat.Form` (`MoveForm` hiện tại).

- [ ] **Step 1: `CreatureCombat`**: nối `AxeFsm`; `scheduler.Allow` gộp lọc theo `Form` (chiêu `Both` hoặc đúng dạng) với bộ lọc cũ; mỗi tick `axe.Tick`; khi `ShouldFetch(...)` đúng và scheduler rảnh thì `Force` một `MoveDef` nội bộ `Shape = Fetch` (`Armored`, không `Rest`) giữ pha hồi bằng `HoldRecover` đến khi `FetchFsm` xong; `Walk` đi tới `Axe_Anchor` hoặc điểm rơi (`motor.Move(..., detour:false)`, nhìn người chơi); `Pull` dùng `PullRules` 3 nhịp, rìu rung và nhấc theo `PullRules.Lift`; xong `axe.Pulled()`, gắn rìu vào tay phải.
- [ ] **Step 2: Chiêu mới**: `AxeThrow` thả rìu bay (xoay, lưỡi dẫn) đến `AimAt(...)`, rìu hạ cánh `Landed(reachable)` (raycast xuống đất, không tới được thì `AtGolem`) rồi `PlaceAt`; `Shockwave` tạo dải chạy theo `ShockwaveRules` (vết nứt, bụi), mỗi tick kiểm tra người chơi, `Hurt` một lần mỗi người; đổi dạng áp trọng số đòn thường ×1,5 qua `WeightScale` khi `Form == Axe`.
- [ ] **Step 3: Vòng hất**: khi `FetchFsm.Stage == Pull` kiểm tra từng người chơi bằng `PushAura.TryPush`; hất bằng `Impulse.Kick`, `p.State.Damage(PushAura.Damage * DamageScale)`, `FreezeFor(PushAura.Freeze)`, chồng `GolemRules.Swat` lên tư thế trong `GolemRigFx`.
- [ ] **Step 4: Rig và FX**: `GolemRigFx` dùng các anim mới; `GolemFx` thêm tiếng và hiệu ứng cho chém, ném, bổ đất, kéo (bụi, gằn mỗi nhịp, rìu rung); đăng ký ba giai đoạn hiệu ứng (`VfxRules` tự lo vì chiêu có `Role`).
- [ ] **Step 5: Kiểm tra cú pháp** (lệnh trong Ràng buộc chung) và `tools/cstest/run.sh` (`Passed!`); commit `"Golem: axe form wired into the combat (unverified in Unity)"`. Ghi rõ trong báo cáo: phần này chưa chạy trong Unity.

### Task 8: `BossRoom`, âm thanh và tài liệu
**Files:** Tạo `World/BossRoom.cs`, `docs/BOSS_ROOM_CONTRACT.md`, `tools/voice/generate_golem_sfx.sh` (thêm dòng), sửa `docs/TEST_CHECKLIST_UNITY.md`

**Interfaces:**
- Produces: `BossRoom` (MonoBehaviour) tìm theo tên các con `Boss_Wait`, `Axe_Anchor`, `Activate_Zone` (collider trigger), `Prop_Pillar_*`, `Prop_Crystal_*`; ẩn Golem tại `Boss_Wait` trước khi kích hoạt; `event Action Activated`; `Transform AxeAnchor { get; }`.

- [ ] **Step 1: Cài đặt `BossRoom`** như trên (thiếu điểm bắt buộc thì cảnh báo một lần trong Console, không ném lỗi; không có `Activate_Zone` thì kích hoạt ngay).
- [ ] **Step 2: Âm thanh**: thêm vào `generate_golem_sfx.sh` các tiếng `golem_axe_swing`, `golem_axe_hit`, `golem_axe_throw`, `golem_axe_stick`, `golem_axe_slam`, `golem_awaken` rồi chạy `ONLY=... tools/voice/generate_golem_sfx.sh`; nối vào `GolemFx`/`BossRoom`.
- [ ] **Step 3: Tài liệu**: `docs/BOSS_ROOM_CONTRACT.md` (danh sách điểm cần đặt, tên chính xác, kiểu collider, ví dụ) và mục kiểm thử mới trong `docs/TEST_CHECKLIST_UNITY.md` (kích hoạt phòng, rút rìu 3 nhịp, vòng hất 1 s choáng, dạng rìu, ném và nhặt, Bổ Đất né được, rìu rơi chỗ không tới được).
- [ ] **Step 4: Chạy** `tools/cstest/run.sh` (`Passed!`) và kiểm cú pháp; commit và đẩy cả hai nhánh.

---

## Tự rà soát
- **Phủ spec:** luồng (T3, T4, T7), kéo 3 nhịp (T2, T7), vòng hất (T5, T7), bộ chiêu theo dạng (T1), Ném Rìu và Bổ Đất (T1, T5, T7), hợp đồng map (T8), đọc người chơi dùng lại cho ngắm rìu (T7 dùng `AimAt`), chưa làm đúng như spec đã ghi (hành vi cột/tinh thể, rìu Meshy, gộp `VolleyFsm`).
- **Nhất quán kiểu:** `AxeFsm`, `FetchFsm`, `PushAura`, `ShockwaveRules`, `MoveForm`, `PullRules(heaves)` dùng cùng tên trong mọi task.
- **Tỉ lệ:** phần thuần (T1–T6) nhỏ, có test; T7–T8 là lớp Unity mỏng, không viết lại thân hàm trong kế hoạch.
