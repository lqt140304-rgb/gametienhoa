# Combat của slime: Vồ – Bám – Tiêu Hóa: kế hoạch thực hiện

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Slime có hành động Vồ từ đầu game; vồ con to hơn thì bám lên và chơi mini game Ghì Chặt để tiêu hóa nó, kết liễu bằng cảnh nuốt chửng.

**Architecture:** Luật thuần (`LatchGame`, `PounceRules`, `StruggleSeed`) trong `Scripts/Combat/Pure/` có test chạy ngoài Unity. Mã Unity mỏng gọi luật: `Pounce` (phím và bật tới), `LatchSession` (gắn slime lên quái, chạy `LatchGame`, áp sát thương), `LatchHud` (IMGUI), `SwallowCinematic` (cảnh nuốt).

**Tech Stack:** Unity 6.6 URP, Input System (`Keyboard.current`, `Mouse.current`), IMGUI như HUD hiện có, C#, NUnit. Ngoài Unity: `tools/cstest/run.sh`; kiểm cú pháp: `tools/syntaxcheck/run.sh <ref>`.

**Spec:** `docs/superpowers/specs/2026-11-20-slime-combat-latch-design.md` (đã duyệt, mục 5 là thứ tự làm).

## Global Constraints

- Văn bản người chơi, tài liệu và báo cáo bằng tiếng Việt; mã và chú thích mã bằng tiếng Anh.
- Mọi số là bản tạm (spec mục 4), chỉnh được khi chơi.
- Con có khối nhỏ hơn hoặc bằng `0,8 × khối slime` (`MassRules.CanSwallowWhole`) nuốt sống ngay; con có khối lớn hơn `4 × khối slime` thì vồ bật ra.
- Vùng xanh: 40% (con mồi), 28% (kẻ săn), 18% (Chúa Hang) của thanh; số lỗi tối đa 5 / 4 / 3; ân hạn 0,25 giây; sát thương tiêu hóa 8 mỗi giây, điểm yếu gấp đôi; năng lượng ghì 4 mỗi giây; hệ số kích cỡ `clamp(khối slime / khối quái, 0,6; 1,5)`; giai đoạn tuyệt vọng khi máu quái dưới 30%.
- Phím Vồ: chuột trái (không kích hoạt khi một menu cần chuột đang mở: `AimController.MouseUiOpen`). Bấm nhảy (Space) để buông; Space đúng lúc trong cú giãy mạnh để siết.
- Không đổi file của bên làm hang ngoài các chỗ đã ghi trong tài liệu bàn giao; thay đổi `Predator.cs`/`Prey.cs` chỉ thêm cờ và hàm nhỏ.
- Commit kết thúc bằng hai dòng ghi công của phiên (Co-Authored-By và Claude-Session). Không tạo PR.
- Mỗi task: `tools/cstest/run.sh` xanh và `tools/syntaxcheck/run.sh origin/claude/unity-test-maze` 0 lỗi cú pháp trước khi commit. Thêm `Scripts/Combat/Pure/*.cs` và `Tests/EditMode/Combat/Pure/*.cs` vào `tools/cstest/cstest.csproj`.

## Review Focus

1. Quái chết hoặc biến mất (bị phiên khác phá, cất lại bởi vùng sinh thái) trong lúc đang bị bám: phiên bám kết thúc sạch, slime được thả, không lỗi tham chiếu (Task 3).
2. Slime chết hoặc buông (Space) trong lúc đang bám: quái được thả, va chạm bật lại, slime không kẹt trên không (Task 3).
3. Hai slime (co-op) cùng vồ một quái, hoặc vồ lần hai khi đang bám: chỉ một phiên bám mỗi slime và mỗi quái, lần vồ thừa bị bỏ qua (Task 2, 3).
4. Vồ khi không có quái nào trong tầm hoặc quái ở sau lưng: không làm gì, không tốn năng lượng (Task 2).
5. Năng lượng về 0 giữa trận ghì: vùng xanh hẹp dần, không âm, không lỗi chia cho 0 (Task 1).

---

### Task 1: Luật thuần `LatchGame`

**Files:**
- Create: `Assets/_Project/Scripts/Combat/Pure/LatchGame.cs`
- Test: `Assets/_Project/Tests/EditMode/Combat/Pure/LatchGameTests.cs`
- Modify: `tools/cstest/cstest.csproj`

**Interfaces:**
- Produces (namespace `Evolve.Combat`):
  - `enum StruggleKind { Even, Jerky, TwoPhase }`, `enum LatchState { Holding, Escaped, MonsterDead }`
  - `readonly struct StruggleProfile(float zoneWidth, int maxFaults, StruggleKind kind, float amplitude, float heavyPerSecond)` với các trường cùng tên viết hoa; `const float DigestPerSecond = 8f`
  - `struct LatchStep { float Damage; float EnergySpent; LatchState State; bool HeavyWarning; }`
  - `sealed class LatchGame(StruggleProfile profile, float slimeMass, float monsterMass, int seed)` với `float Dot` (0 đến 1, bắt đầu 0,5), `float ZoneWidth` (hiệu lực, đã nhân hệ số), `int Faults`, `bool Telegraphing`, `float Time`, `float WeakCenter`, `const float WeakWidth = 0.06f`, và `LatchStep Step(float dt, float input, bool squeeze, float monsterHealth01, float energy)`.
  - Hằng số: `Grace = 0.25f`, `EnergyPerSecond = 4f`, `PlayerSpeed = 1.2f` (đơn vị thanh mỗi giây khi `input` là ±1), `HeavyWindow = 0.5f`, `SqueezeBoost = 1.4f` trong `SqueezeSeconds = 3f`, `DesperateBelow = 0.3f`, `DesperateFactor = 1.6f`, `NoEnergyZone = 0.6f`.

- [ ] **Step 1: Viết test** (tên và khẳng định cụ thể; hồ sơ thử `P(amp: 0)` là `new StruggleProfile(0.28f, 4, StruggleKind.Even, 0f, 0f)` trừ khi ghi khác):
  - `Starts_HoldingAtCentre`: `Dot == 0.5f`, `Faults == 0`, `Step(0.1f,0,false,1,100).State == Holding`.
  - `InZone_DealsEightPerSecond`: hồ sơ `P(amp: 0)`, chạy 10 bước 0,1 giây `input 0`, `Damage` cộng lại khoảng `8` (sai số 0,01 hơn trừ phần điểm yếu: dùng `WeakCenter` ở đầu = 0,5 nên bước đầu gấp đôi; test lấy tổng của 10 bước và khẳng định nằm trong 8 đến 16, và ở một test riêng `WeakPoint_DoublesDamage` bước đầu có `Damage == 1.6f ± 0.001f` còn khi `WeakCenter` đã trôi ra xa thì `0.8f`).
  - `SizeFactor_ScalesZone`: slime 15, quái 10: `ZoneWidth == 0.28f * 1.5f ± 1e-4`; slime 3, quái 30: `ZoneWidth == 0.28f * 0.6f ± 1e-4`.
  - `OutOfZone_GraceThenFault`: đẩy `Dot` ra ngoài vùng bằng `input +1` (bước 0,1 giây tới khi `Dot` lệch hơn nửa vùng), rồi `input 0`: sau 0,2 giây `Faults == 0`; sau thêm 0,1 giây `Faults == 1`.
  - `EnoughFaults_Escapes`: `MaxFaults = 3`: giữ ngoài vùng liên tục, sau 3 lần ân hạn `State == Escaped`.
  - `NoEnergy_ShrinksZoneWithoutNegative`: `energy = 0` thì `ZoneWidth == 0.28f * 0.6f ± 1e-4`; `energy = -5` không ném lỗi, `EnergySpent >= 0`.
  - `Desperate_StrugglesHarder`: `Even`, `amplitude 0.5`, cùng `seed`: biên độ lớn nhất của `Dot - 0.5` trong 3 giây với `monsterHealth01 = 0.2` lớn hơn với `0.9`.
  - `Heavy_WithSqueeze_WidensZoneNoFault`: `heavyPerSecond 1000`: sau bước đầu `Telegraphing == true`; bấm `squeeze` trong cửa sổ rồi hết 0,5 giây: `Faults == 0` và `ZoneWidth == 0.28f * 1.4f ± 1e-4`.
  - `Heavy_WithoutSqueeze_IsAFault`: như trên, không bấm: `Faults == 1`.
  - `MonsterDead_Wins`: `monsterHealth01 = 0` trả `State == MonsterDead`.
  - `SameSeed_SameTrace`: `Jerky`, hai ván cùng `seed` cho cùng chuỗi `Dot` sau 5 giây; `seed` khác thì khác.
- [ ] **Step 2: Chạy `tools/cstest/run.sh`, thấy lỗi biên dịch vì chưa có `LatchGame`.**
- [ ] **Step 3: Cài `LatchGame`.** Thanh 0..1, vùng xanh đặt giữa 0,5. Trôi của quái theo `Kind` (Even: một hình sin; Jerky: xung ngẫu nhiên đổi mỗi 0,6 giây; TwoPhase: 2 giây chậm ×0,5 rồi 1 giây nhanh ×1,8), nhân `DesperateFactor` khi `monsterHealth01 < DesperateBelow`; người chơi kéo `input × PlayerSpeed`. Điểm yếu là đoạn `WeakWidth` trôi hình sin trong vùng (`WeakCenter` bắt đầu 0,5 tại `Time == 0`). Vùng hiệu lực = rộng gốc × hệ số kích cỡ × (`NoEnergyZone` khi `energy <= 0`) × (`SqueezeBoost` khi còn `SqueezeSeconds`). Cú giãy mạnh bắt đầu khi `Random < heavyPerSecond × dt` (dùng `System.Random(seed)`), kéo dài `HeavyWindow`; `squeeze` bấm trong lúc báo thì thành công. Kiểm tra `monsterHealth01 <= 0` trước tiên.
- [ ] **Step 4: Chạy lại, thấy xanh. Commit** (`git add` ba file, thông điệp "LatchGame: the grip mini game rules").

### Task 2: `PounceRules` và `StruggleSeed` (luật thuần)

**Files:**
- Create: `Assets/_Project/Scripts/Combat/Pure/PounceRules.cs`, `Assets/_Project/Scripts/Combat/Pure/StruggleSeed.cs`
- Test: `Assets/_Project/Tests/EditMode/Combat/Pure/PounceRulesTests.cs`

**Interfaces:**
- Consumes: `MassRules.CanSwallowWhole`, `CaveTier.RoleOf`, `StruggleProfile` (Task 1).
- Produces:
  - `enum PounceOutcome { SwallowWhole, Latch, Bounce }`
  - `static PounceOutcome PounceRules.Decide(float slimeMass, float monsterMass)` với `const float BounceRatio = 4f`
  - `static int PounceRules.PickTarget(IReadOnlyList<Candidate> candidates, float px, float pz, float fx, float fz, float maxDistance = 4f, float minDot = 0.5f)` trả chỉ số ứng viên gần nhất ở phía trước (`dot(hướng tới quái, hướng nhìn) >= minDot`, trong `maxDistance`, bỏ qua `Taken`), `-1` nếu không có; `readonly struct Candidate(float x, float z, bool taken)`
  - `static StruggleProfile StruggleSeed.For(string speciesIdOrName)`: Prey `(0.40, 5, Even, 0.35, 0)`, Hunter `(0.28, 4, Jerky, 0.55, 0.08)`, Lord `(0.18, 3, TwoPhase, 0.8, 0.25)`.

- [ ] **Step 1: Viết test**:
  - `Decide_ByMass`: `Decide(10, 8) == SwallowWhole`; `Decide(10, 8.1f) == Latch`; `Decide(10, 40) == Latch`; `Decide(10, 40.1f) == Bounce`.
  - `PickTarget_NearestInFront`: ứng viên ở `(0,2)`, `(0,3)`, `(0,-1)`, `px=pz=0`, nhìn `(0,1)`: trả `0`; ứng viên duy nhất ở `(0,-2)` hoặc `(0,5)` trả `-1`; ứng viên đầu `Taken` thì trả `1`; hai ứng viên cùng hướng thì trả cái gần hơn; nhìn `(0,0)` (vector không) không ném lỗi và trả `-1`.
  - `StruggleSeed_ByRole`: `For("Ốc Hồ").ZoneWidth == 0.40f`, `MaxFaults == 5`; `For("ThanLanBoHo")` `0.28f`, `4`; `For("Cóc Hồ Già")` `0.18f`, `3`; tên lạ như Prey; `HeavyPerSecond` Prey 0, Lord lớn hơn Hunter.
- [ ] **Step 2: Chạy, thấy đỏ.**
- [ ] **Step 3: Cài hai tệp** (chuẩn hóa vector nhìn, bỏ qua độ dài 0).
- [ ] **Step 4: Chạy lại, xanh. Commit.**

### Task 3: Vồ và phiên bám trong cảnh (Unity)

**Files:**
- Create: `Assets/_Project/Scripts/Combat/Pounce.cs`, `Assets/_Project/Scripts/Combat/LatchSession.cs`
- Modify: `Assets/_Project/Scripts/Swallow/Swallower.cs` (thêm `SwallowCarcassNow`), `Assets/_Project/Scripts/Creatures/Prey.cs` và `Predator.cs` (thêm cờ), `Assets/_Project/Scripts/Player/SlimeController.cs` chỉ nếu cần để thả

**Interfaces:**
- Consumes: `PounceRules`, `StruggleSeed`, `LatchGame` (Tasks 1, 2); `MassRules`, `IDamageable.TakeDamage(float, SurvivalStats)`, `Prey.Active` / `Predator.Active`, `SlimeBody.Mass`, `Swallower.TrySwallowAlive`, `Carcass.Active`.
- Produces:
  - `Pounce : MonoBehaviour` (thêm vào slime lúc chạy từ `SkillBar.Awake`): đọc chuột trái (không khi `AimController.MouseUiOpen`), chọn mục tiêu bằng `PounceRules.PickTarget`, bật slime tới mục tiêu (`AddForce` VelocityChange), rồi theo `Decide`: `SwallowWhole` gọi luồng bắt hiện có của quái (`Prey` tự bắt khi chạm; với `Predator` gọi `Swallower.TrySwallowAlive`), `Latch` tạo `LatchSession`, `Bounce` hất slime ra và `FreezeFor(0.4f)`. Tốn một ít năng lượng (`stats.Config.HopEnergyCost`), không tốn khi không có mục tiêu.
  - `LatchSession : MonoBehaviour` tạo bằng `LatchSession.Begin(SurvivalStats slime, Component monster, StruggleProfile profile)`; `public static LatchSession Active` (một phiên mỗi slime cục bộ), `public LatchGame Game`, `public event Action<LatchState> Ended`.
  - `Prey.Latched` và `Predator.Latched` (`bool`, đặt bởi phiên) để quái chọn chiêu riêng sau này; `Predator.RememberSlimeUntil` / `Prey.RememberSlimeUntil` (`float`): thời điểm đến đó quái cảnh giác hơn.
  - `Swallower.SwallowCarcassNow(Carcass c)` (công khai, bọc `SwallowCarcass`).

- [ ] **Step 1: Phần này là mã Unity, không có test chạy ngoài Unity; luật nó dựa vào đã có test ở Task 1 và 2. Kiểm bằng `syntaxcheck` và bằng danh sách thử ở Task 7.**
- [ ] **Step 2: Cài `Pounce`** như trên. Chỉ cho phép khi slime sống, không đang bám, không `stats.Frozen`, và `LatchSession.Active == null`. Mỗi quái chỉ một slime bám (kiểm cờ `Latched`).
- [ ] **Step 3: Cài `LatchSession`.** Mỗi `FixedUpdate`: giữ slime trên đỉnh quái (`rb.MovePosition` theo `collider.bounds`, không trọng lực, `Physics.IgnoreCollision` giữa hai collider, `SlimeController.FreezeFor(0.2f)`); đọc `input` từ A/D hoặc độ dời chuột; Space buông (`Ended(Escaped)`) khi chưa bấm siết đúng cửa sổ; chạy `Game.Step`; áp `Damage` bằng `TakeDamage(damage, slimeStats)`; trừ `EnergySpent` (hết năng lượng thì vẫn tiếp tục, vùng hẹp dần). Kết thúc sạch khi: `Escaped` (hất slime xuống, quái bỏ chạy 3 giây bằng `ForceFleeUntil`/`LoseTarget` và đặt `RememberSlimeUntil = now + 120`), quái bị hủy hoặc `null`, slime chết, hoặc `MonsterDead` (tìm `Carcass` gần nhất trong 2 m và gọi `Swallower.SwallowCarcassNow`, rồi phát cảnh nuốt nếu Task 5 có). Mọi đường thoát bật lại va chạm, trọng lực và bỏ cờ `Latched`.
- [ ] **Step 4: `Prey.Latched` / `Predator.Latched` và `RememberSlimeUntil`:** chỉ là thuộc tính; `Predator` trong lúc `Latched` vẫn chạy hành vi của nó. Khi `RememberSlimeUntil` còn hiệu lực, `Prey` chạy từ xa hơn (+50% `fleeRadius`) và `Predator` phát hiện slime xa hơn (+30% `detectRadius`).
- [ ] **Step 5: `syntaxcheck` 0 lỗi, `cstest` xanh. Commit.**

### Task 4: Giao diện mini game (IMGUI)

**Files:**
- Create: `Assets/_Project/Scripts/Combat/LatchHud.cs`

**Interfaces:**
- Consumes: `LatchSession.Active`, `LatchGame` (Task 1, 3).
- Produces: `LatchHud : MonoBehaviour` (thêm vào slime lúc chạy cùng `Pounce`): chỉ vẽ khi `LatchSession.Active != null`.

- [ ] **Step 1: Mã Unity, không có test ngoài Unity.**
- [ ] **Step 2: Vẽ bằng `OnGUI`:** thanh ngang dưới giữa màn hình, vùng xanh (`ZoneWidth` quanh 0,5), khúc vàng (`WeakCenter`), chấm (`Dot`), chữ đỏ "GIÃY MẠNH! BẤM SPACE" khi `Telegraphing`, số lỗi còn lại (`MaxFaults - Faults`), gợi ý phím (A/D hoặc kéo chuột, Space để buông). Chữ tiếng Việt.
- [ ] **Step 3: `syntaxcheck` 0 lỗi. Commit.**

### Task 5: Cảnh nuốt chửng

**Files:**
- Create: `Assets/_Project/Scripts/Combat/SwallowCinematic.cs`
- Modify: `Assets/_Project/Scripts/Combat/LatchSession.cs`

**Interfaces:**
- Consumes: `LatchSession.Ended` (Task 3), `SlimeGelAnimator`, `SlimeSfx`.
- Produces: `static void SwallowCinematic.Play(SurvivalStats slime)` (chạy coroutine tự dọn; bỏ qua được bằng Escape hoặc phím Vồ; chỉ phát cho máy cục bộ).

- [ ] **Step 1: Mã Unity, không có test ngoài Unity.**
- [ ] **Step 2: Cài** thời gian chậm (`Time.timeScale = 0.3` trong 1,5 đến 2,5 giây, phục hồi đảm bảo bằng `try/finally`), camera áp sát (đổi `Camera.fieldOfView` rồi trả lại), `SlimeGelAnimator.Play(..., GelMotion.Gulp)`, `SlimeSfx.Play("slime_swallow")`. Không phát cho con nuốt sống nhỏ.
- [ ] **Step 3: `LatchSession` gọi `Play` khi `MonsterDead`. `syntaxcheck` 0 lỗi. Commit.**

### Task 6: Lối tắt trong menu thần

**Files:**
- Modify: `Assets/_Project/Scripts/Dev/GodMenu.Tests.cs`

**Interfaces:**
- Consumes: `StruggleSeed.For`, `LatchSession.Active` (Tasks 2, 3).

- [ ] **Step 1: Thêm mục "Combat: Vồ và Ghì Chặt"** với nút tạo một quái thử cỡ lớn hơn slime (dùng lại `SpawnTest`) ở ba mức (con mồi lớn, kẻ săn, Chúa Hang), một dòng hiện trạng thái phiên bám (lỗi, vùng, năng lượng), và nút "Hồi đầy năng lượng". Mã Unity.
- [ ] **Step 2: `syntaxcheck` 0 lỗi, `cstest` xanh. Commit.**

### Task 7: Tài liệu, bàn giao, gộp

**Files:**
- Modify: `docs/TEST_CHECKLIST_UNITY.md`, `docs/PROJECT_STATUS.md`, `docs/superpowers/specs/2026-11-20-slime-combat-latch-design.md`

- [ ] **Step 1: Mục kiểm thử mới** (Vồ con nhỏ nuốt sống, Vồ con to vào Ghì Chặt, buông bằng Space, siết đúng lúc, hết lỗi bị hất, quái chết thì cảnh nuốt, quái nhớ slime, Vồ ngoài tầm không tốn năng lượng, menu thần tạo quái thử). Cập nhật mục 54 của PROJECT_STATUS từ "chưa làm" thành trạng thái thật, ghi rõ phần chưa kiểm trong Unity. Trong spec ghi phần "Làm sau" (mục 2.5: kỹ năng hang tác động mini game, co-op) và việc đổi tên "Vồ Xa" **chưa làm vì Chồn Khe (đợt 3) chưa có dữ liệu kỹ năng**.
- [ ] **Step 2: Gộp `claude/movement` vào `claude/unity-test-maze`, chạy `cstest` và `syntaxcheck`, đẩy cả hai nhánh. Báo cáo bằng tiếng Việt, nói rõ phần chưa kiểm trong Unity.**
