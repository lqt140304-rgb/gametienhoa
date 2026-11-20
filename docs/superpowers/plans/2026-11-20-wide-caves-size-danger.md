# Khoang rộng có nhịp, kích cỡ theo vai trò, độ khó theo tầng: kế hoạch thực hiện

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Khoang rộng có vùng có vai trò, quái có kích cỡ theo vai trò và tầng, độ khó tăng theo tầng với hang đầu an toàn.

**Architecture:** Luật thuần (bảng tầng, kích cỡ, bố cục vùng) nằm trong các thư mục `Pure/` và có test chạy ngoài Unity bằng `tools/cstest/run.sh`. Mã Unity mỏng: một bước `CreatureTuning.Apply` gọi từ `Region` khi sinh vật được tạo hoặc nhận vào, và bộ dựng Editor của khoang dùng `ChamberZones`.

**Tech Stack:** Unity 6.6 URP, C#, NUnit (ngoài Unity: .NET 8 + NUnit 3.14; trong Unity NUnit cũ hơn, không dùng `Is.AnyOf`), `tools/syntaxcheck/run.sh` cho kiểm tra cú pháp.

**Spec:** `docs/superpowers/specs/2026-11-20-wide-caves-size-danger-design.md`

## Global Constraints

- Văn bản người chơi và tài liệu bằng tiếng Việt; mã và chú thích mã bằng tiếng Anh.
- Mọi số là bản tạm, chỉnh được trong menu thần (F1, mục Kiểm thử).
- Hang đầu bán kính 40 m giữ một tổ mỗi loài; khoang 75 m có ba tổ mỗi loài, Chúa Hang một tổ.
- Slime lớn nhất rộng 2,0 m (bán kính 0,5 m, tỉ lệ thân tối đa 2). Chúa Hang sau hệ số rộng từ 3,0 m trở lên.
- Chúa Hang hiền ở hang đầu: đánh trả 30 giây kể từ đòn cuối của người chơi.
- Không đổi prefab hay mô hình của bên làm hang; sửa file Editor của họ phải ghi vào `docs/superpowers/handoff/2026-11-20-core-update-for-cave-team.md`.
- Commit kết thúc bằng hai dòng ghi công của phiên (Co-Authored-By và Claude-Session). Không tạo PR.
- Mỗi task: chạy `tools/cstest/run.sh` (phải xanh) và `tools/syntaxcheck/run.sh origin/claude/unity-test-maze` (0 lỗi cú pháp) trước khi commit.

## Review Focus

1. Sinh vật tạo ngoài `Region` (khoang dùng `PlaceCreatures`): không có hang thì giữ cỡ và độ khó cũ, không lỗi (Task 2).
2. Áp kích cỡ hai lần cho cùng một con (con vừa nhận vào rồi bị tạo lại, hoặc cất rồi lấy ra): kích cỡ không nhân đôi (Task 2).
3. Hang không có trong bảng tầng (tên lạ, rỗng): mặc định tầng Giữa, hệ số 1 (Task 1).
4. Kẻ săn đã nhận đòn rồi người chơi chạy ra khỏi vùng an toàn: không bị coi là hiền sau 30 giây (Task 1, 3).
5. Khoang chỉ có một cửa hầm hoặc không có góc nào đủ xa cửa hầm: bố cục vẫn trả về ít nhất cụm cảnh và một tổ, không ném lỗi (Task 5).

---

### Task 1: Bảng tầng, độ khó và kích cỡ (luật thuần)

**Files:**
- Create: `Assets/_Project/Scripts/Creatures/Pure/CaveTier.cs`
- Test: `Assets/_Project/Tests/EditMode/Creatures/CaveTierTests.cs`

**Interfaces:**
- Produces:
  - `enum CreatureRole { Prey, Hunter, Lord }`
  - `static class CaveTier` với `int Of(string caveId)` (0 hang đầu, 1 Nông, 2 Giữa, 3 Sâu; chưa biết hoặc rỗng thì 2)
  - `struct Danger { float Damage, Detect, HunterCount, LordSpeed; bool LordGentle; }` và `static Danger CaveTier.DangerOf(int tier)`
  - `static float CaveTier.SizeFactor(CreatureRole role, int tier)`
  - `static CreatureRole CaveTier.RoleOf(string speciesIdOrName)` (dùng `CaveEcologySeed` để biết con mồi, kẻ săn hay Chúa Hang; không rõ thì Prey)
  - `static bool CaveTier.IsCalm(bool gentle, float provokedUntil, float now)`: hiền và chưa bị chọc trong 30 giây
  - `const float CaveTier.ProvokeSeconds = 30f`, `const float CaveTier.SlimeMaxWidth = 2f`, `const float CaveTier.LordMinWidth = 3f`

- [ ] **Step 1: Viết test (tên và khẳng định cụ thể)**
  - `Tiers_CoverEveryCave_AndUnknownIsMiddle`: `Of("khoi_sinh")==0`; `Of("vuon_quang")==1`; `Of("reu_tho")==1`; `Of("to_bo_hoang")==1`; `Of("vuc_nut_tim")==2`; `Of("nam_huynh_quang")==2`; `Of("suoi_nuoc_nong")==2`; `Of("khe_gio_hu")==2`; `Of("nghia_dia")==3`; `Of("hoa_thach")==3`; `Of("tieng_tho")==3`; `Of("la_hoi")==2`; `Of("")==2`; `Of(null)==2`.
  - `Danger_Table_MatchesSpec`: tầng 0 `Damage==0.6`, `Detect==0.6`, `HunterCount==0.5`, `LordGentle`; tầng 1 `0.8, 0.8, 1`, `LordSpeed==0.85`; tầng 2 `1, 1, 1`, `LordSpeed==1`; tầng 3 `1.4, 1.4, 1.5`, `LordSpeed==1.15`; chỉ tầng 0 có `LordGentle`.
  - `Danger_RisesWithDepth`: `Damage`, `Detect` không giảm khi tầng tăng.
  - `SizeFactor_MatchesSpec`: `Prey` mọi tầng `0.85`; `Hunter` tầng 0..3 `1.2, 1.3, 1.4, 1.5`; `Lord` tầng 0..3 `2.4, 2.6, 2.9, 3.2`.
  - `EveryLord_AtLeast3m_AfterFactor`: duyệt các dòng `CaveCreaturesWave1Builder.Specs` là không thể từ test thuần, nên giữ bảng chiều rộng Chúa Hang đợt 1 trong test: Cóc Hồ Già 1,56, Rùa Pha Lê 1,84, Bọ Cạp Thạch Anh 1,76; khẳng định `width * SizeFactor(Lord, t) >= LordMinWidth` với mọi tầng.
  - `RoleOf_ByNameOrId`: `RoleOf("Cóc Hồ Già")==Lord`, `RoleOf("ThanLanBoHo")==Hunter`, `RoleOf("Ốc Hồ")==Prey`, `RoleOf("xyz")==Prey`.
  - `IsCalm_OnlyUntilProvoked`: `IsCalm(true, 0, 10)` đúng; `IsCalm(true, 40, 10)` sai (đang bị chọc); `IsCalm(true, 40, 41)` đúng; `IsCalm(false, 0, 10)` sai.
- [ ] **Step 2: Chạy `tools/cstest/run.sh`, thấy test mới đỏ vì chưa có `CaveTier`**
- [ ] **Step 3: Cài `CaveTier` trong `Creatures/Pure/CaveTier.cs`** theo bảng trong spec; `RoleOf` dùng `CaveEcologySeed.IdByName` rồi tìm loài trong `CaveEcologySeed.Build()` (kẻ săn có `Floor == 1 && Capacity == 3`, Chúa Hang `Capacity == 1`); tên không nhận ra thì Prey.
- [ ] **Step 4: Chạy lại, thấy xanh**
- [ ] **Step 5: Commit** (`git add` hai file trên)

### Task 2: Áp kích cỡ và độ khó khi sinh vật vào hang

**Files:**
- Create: `Assets/_Project/Scripts/Creatures/CreatureTuning.cs`
- Modify: `Assets/_Project/Scripts/Creatures/CreatureMotor.cs` (thêm `Resize`), `Assets/_Project/Scripts/Creatures/Predator.cs` (thêm `Gentle`, `ScaleDanger`, tính "hiền" vào `UpdateTarget`, `Contact`, `BiteRemotePlayers`, `TakeDamage`), `Assets/_Project/Scripts/Ecology/Region.cs` (gọi `Apply` ở `Spawn`, `Adopt`, `TryEmbodyAt`)

**Interfaces:**
- Consumes: `CaveTier` (Task 1), `Region.CaveId`.
- Produces:
  - `static void CreatureTuning.Apply(GameObject creature, string caveId)`: không làm gì khi `caveId == null`; chạy một lần cho mỗi sinh vật (đánh dấu bằng một component rỗng `TunedMarker` hoặc cờ trên `CreatureMotor`); nhân `transform.localScale` theo `SizeFactor(role, tier)`; gọi `CreatureMotor.Resize(factor)`, `CreatureLocomotion.Configure` lại với bán kính và tốc độ nhân hệ số; với kẻ săn gọi `Predator.ScaleDanger(damage, detect, speedFactor)`; với Chúa Hang ở tầng 0 đặt `Predator.Gentle = true`.
  - `CreatureMotor.Resize(float k)`: nhân `radius` và hai giới hạn bậc/vực của `Profile` (`MaxStepUp`, `MaxDrop`) với `k`.
  - `Predator.Gentle` (bool), `Predator.ScaleDanger(float damage, float detect, float speed)` (nhân `biteDamage`, `detectRadius`, `chaseSpeed`, `patrolSpeed`).

- [ ] **Step 1: Phần này là mã Unity, không có test chạy ngoài Unity; luật nó dựa vào (`SizeFactor`, `DangerOf`, `IsCalm`) đã có test ở Task 1. Kiểm bằng `syntaxcheck` ở Step 4 và bằng danh sách thử trong Unity ở Task 7.**
- [ ] **Step 2: Cài `CreatureTuning`, `Resize`, `Gentle`, `ScaleDanger`** như trên. "Hiền" nghĩa là `Gentle && CaveTier.IsCalm(...)`: `UpdateTarget` đặt `target = null`, `Contact` và `BiteRemotePlayers` bỏ qua cắn; `TakeDamage` với `by != null` đặt `provokedUntil = Time.time + CaveTier.ProvokeSeconds`.
- [ ] **Step 3: Gọi `CreatureTuning.Apply(go, CaveId)` trong `Region.Spawn` (sau `ec.Init`), `Region.Adopt` (sau `ec.Init`), `Region.TryEmbodyAt`.**
- [ ] **Step 4: Chạy `tools/syntaxcheck/run.sh origin/claude/unity-test-maze`: 0 lỗi; `tools/cstest/run.sh` xanh.**
- [ ] **Step 5: Commit**

### Task 3: Vùng an toàn quanh điểm xuất hiện, số kẻ săn theo tầng

**Files:**
- Create: `Assets/_Project/Scripts/Creatures/SafeZone.cs`
- Modify: `Assets/_Project/Scripts/Creatures/CreatureBrain.cs` (`SlimeFilter`), `Assets/_Project/Scripts/Ecology/Pure/CaveEcologySeed.cs` (tham số `hunterScaleOf`), `Assets/_Project/Scripts/Ecology/EcologyRuntime.cs`, `Assets/_Project/Editor/CaveWave1ScenePopulator.cs`
- Test: `Assets/_Project/Tests/EditMode/Ecology/CaveEcologyBalanceTests.cs`

**Interfaces:**
- Consumes: `CaveTier.DangerOf(...).HunterCount` (Task 1).
- Produces:
  - `SafeZone : MonoBehaviour` với `public float Radius`, `static bool SafeZone.Contains(Vector3 point)` (đo ngang, bỏ qua độ cao), tự đăng ký trong `OnEnable`, bỏ trong `OnDisable`.
  - `CaveEcologySeed.Build(Func<string,float> scaleOf = null, Func<string,float> hunterScaleOf = null)`: `hunterScaleOf` chỉ nhân số kẻ săn (loài có `Capacity` 3 trước khi nhân) và sinh sản của chúng, không đụng con mồi và Chúa Hang.

- [ ] **Step 1: Viết test trong `CaveEcologyBalanceTests`**
  - `HunterScale_OnlyHuntersMove`: `Build(null, id => id == "vuon_quang" ? 0.5f : 1f)`; kẻ săn `Species[2].Count` của `vuon_quang` bằng 1; con mồi `Species[0]` còn 8; Chúa Hang `Species[3]` còn 1.
  - `DepthScaled_TwoHours_NoExtinctionAndInBounds`: `scaleOf` như bài khoang rộng, `hunterScaleOf = id => CaveTier.DangerOf(CaveTier.Of(id)).HunterCount`; chạy 2 giờ như `WideCaves_TwoHours_...`: không loài nào ở mức 0 lâu hơn 12 phút (Chúa Hang 10 phút).
- [ ] **Step 2: Chạy, thấy đỏ**
- [ ] **Step 3: Cài `hunterScaleOf` trong `Build`; `EcologyRuntime.World` truyền `hunterScaleOf = id => CaveTier.DangerOf(CaveTier.Of(id)).HunterCount`.**
- [ ] **Step 4: `SafeZone`; `CreatureBrain.SlimeFilter` trả `false` khi `SafeZone.Contains(p.transform.position)`; `PopulateBirthCavern` đặt một `SafeZone` bán kính 25 tại `CavernLayout.SpawnPoint()`; `PopulateEcosystem` nhân số kẻ săn (không phải Chúa Hang) với `HunterCount` của tầng hang, làm tròn lên, tối thiểu 1.**
- [ ] **Step 5: Chạy `cstest` (xanh) và `syntaxcheck` (0 lỗi). Commit**

### Task 4: Ô chỉnh trong menu thần

**Files:**
- Modify: `Assets/_Project/Scripts/Creatures/CaveTier.cs` (thêm hệ số chỉnh tĩnh), `Assets/_Project/Scripts/Dev/GodMenu.Tests.cs`

**Interfaces:**
- Produces: `static float CaveTier.SizeScale = 1f` (nhân thêm vào mọi `SizeFactor`), `static int CaveTier.TierOverride = -1` (từ 0 đến 3 thì `Of` luôn trả số đó).

- [ ] **Step 1: Test** `Overrides_AffectOutputsAndReset`: đặt `TierOverride = 3` thì `Of("khoi_sinh")==3`; đặt `SizeScale = 2` thì `SizeFactor(Prey,0)==1.7`; đặt lại `-1` và `1` rồi kiểm lại giá trị mặc định.
- [ ] **Step 2: Cài; thêm trong mục Kiểm thử một thanh kéo "Cỡ quái ×" (0,5 đến 2) và các nút "Mức độ khó: theo hang / 0 / 1 / 2 / 3".** Lưu ý: chỉ sinh vật tạo sau khi đổi mới nhận hệ số (ghi vào dòng chú thích của ô).
- [ ] **Step 3: `cstest` xanh, `syntaxcheck` 0 lỗi. Commit**

### Task 5: Bố cục vùng trong khoang rộng (luật thuần)

**Files:**
- Create: `Assets/_Project/Scripts/Maze/Pure/ChamberZones.cs`
- Test: `Assets/_Project/Tests/EditMode/Maze/Pure/ChamberZonesTests.cs`

**Interfaces:**
- Consumes: `ChamberDensity.NestPositions` (đã có).
- Produces: `struct ChamberPlan { (float X, float Z) Beacon; List<(float X, float Z)> Nests; (float X, float Z) Nook; float NookOpeningDeg; }` và `static ChamberPlan ChamberZones.Plan(float radius, IReadOnlyList<float> tunnelAnglesDeg, int seed, int nestCount)`.

- [ ] **Step 1: Viết test**
  - `Beacon_FacesTheMainEntrance`: `tunnels = {0f, 120f}`; cụm cảnh nằm ở góc từ 150° đến 210° (ngược cửa chính, `tunnels[0]`), bán kính từ 0,4 đến 0,6 của `0,74 × radius`.
  - `Nests_AvoidTunnelMouths_AndEachOther`: `nestCount = 6`; mỗi tổ cách mọi hướng hầm hơn 22°, cách cụm cảnh và nhau ít nhất 20 m, nằm trong 0,4 đến 0,85 của `0,74 × radius`.
  - `Nook_FarFromMouths_OpeningAwayFromCentre`: tại bán kính từ 0,8 đến 0,95 của `0,74 × radius`, cách cửa hầm hơn 40°; `NookOpeningDeg` lệch hướng về tâm ít nhất 90°.
  - `SameSeed_SamePlan` và `DifferentSeed_DifferentNests`.
  - `OneTunnelOrNone_StillReturnsBeaconAndANest`: `tunnels` rỗng và `tunnels = {0f}` không ném lỗi; `Nests.Count >= 1`.
- [ ] **Step 2: Chạy, thấy đỏ**
- [ ] **Step 3: Cài `Plan`** (cửa chính là `tunnelAnglesDeg[0]`, rỗng thì 0°); dùng `System.Random(seed)`, thử tối đa 400 lần như `NestPositions`; góc ẩn chọn lần lượt trong số góc cách cửa hầm hơn 40°, không có thì lấy góc xa cửa hầm nhất.
- [ ] **Step 4: `cstest` xanh. Commit**

### Task 6: Dựng khoang theo vùng (Editor)

**Files:**
- Modify: `Assets/_Project/Editor/MazeChamberDecorator.cs`, `Assets/_Project/Editor/CaveWave1ScenePopulator.cs`

**Interfaces:**
- Consumes: `ChamberZones.Plan` (Task 5), `ChamberDensity` (đã có).
- Produces: `CaveWave1ScenePopulator.PopulateEcosystem(..., IReadOnlyList<float> tunnelAnglesDeg = null)` và các hàm `Populate*Chamber` nhận thêm `IReadOnlyList<float> tunnelAnglesDeg = null`, truyền xuống; khi `null` thì giữ cách chọn tổ hiện tại.

- [ ] **Step 1: Truyền `tunnelAnglesDeg` từ `MazeChamberDecorator` xuống mọi lời gọi `CaveWave1ScenePopulator.Populate*Chamber`; trong `PopulateEcosystem`, với khoang rộng và `tunnelAnglesDeg != null` lấy vị trí tổ phụ từ `ChamberZones.Plan(...).Nests` thay `ChamberDensity.NestPositions`.**
- [ ] **Step 2: Cụm cảnh ở cửa:** trong `MazeChamberDecorator`, sau `BuildGrandThemeLandmarks`, nhân bản cụm cảnh cao nhất của chủ đề tại `plan.Beacon`, thêm một đèn điểm màu `accentLightColor` cường độ thấp.
- [ ] **Step 3: Đường mòn và góc ẩn:** đặt đèn điểm mờ (cường độ khoảng 0,6, tầm 8 m) cách nhau 12 m dọc đoạn thẳng từ `plan.Beacon` tới từng tổ; tại `plan.Nook` đặt vòng 5–6 đá tảng (dùng `MazeTerrainMesh.PlaceFramingBoulder`) có lối vào quay theo `NookOpeningDeg`, bên trong 4 `Nutrient` khoáng (dùng `CreateNutrientObject`).
- [ ] **Step 4: `syntaxcheck` 0 lỗi, `cstest` xanh. Commit.** Không kiểm được trong Unity: ghi rõ trong báo cáo.

### Task 7: Tài liệu, bàn giao, gộp

**Files:**
- Modify: `docs/TEST_CHECKLIST_UNITY.md`, `docs/PROJECT_STATUS.md`, `docs/superpowers/handoff/2026-11-20-core-update-for-cave-team.md`

- [ ] **Step 1: Mục kiểm thử mới (kích cỡ, độ khó theo tầng, Chúa Hang hiền, vùng an toàn, cụm cảnh nhìn từ cửa, góc ẩn, FPS) và mục 53 trong PROJECT_STATUS; ghi vào tài liệu bàn giao các file Editor và `Predator.cs` đã sửa.**
- [ ] **Step 2: Gộp `claude/movement` vào `claude/unity-test-maze`, chạy `cstest` và `syntaxcheck`, đẩy cả hai nhánh. Báo cáo bằng tiếng Việt, nói rõ phần chưa kiểm trong Unity.**
