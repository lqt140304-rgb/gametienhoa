# Bước 1: Vòng lõi Nuốt, Bụng, Giải Mã, Biến Hình — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Slime nuốt con mồi và xác vào bụng (Túi Không Đáy). Mẫu trong bụng được Giải Mã theo tỉ lệ may mắn cộng tích lũy để học cơ quan. Cơ quan đã học được nặn lên người bằng khối cơ thể, chỉ ở hang trú. Đây là thay thế cho hệ nhặt-gắn cơ quan và EXP.

**Architecture:** Toàn bộ luật nằm trong lớp thuần (namespace `Evolve.Swallow`, thư mục `Scripts/Swallow/`), có test EditMode. MonoBehaviour mỏng nối luật vào Prey, Predator, Carcass, OrganHost, SlimeController và HUD (IMGUI hiện có).

Bảng di truyền của mỗi loài lấy luôn từ `DropTableSO` đang có (danh sách organ + tier), không cần asset mới. Thông báo đi qua một hàng đợi `Echo` thuần, HUD đọc ra (cảnh Tiếng Vọng đầy đủ là bước 2).

**Tech Stack:** Unity 6.6, C#, EditMode tests (`Evolve.Tests.EditMode`, NUnit), IMGUI HUD (`SurvivalHUD` partial).

**Spec:** `docs/superpowers/specs/2026-11-19-reincarnation-survival-design.md` (mục 3, 5, 6, 16 bước 1)

## Global Constraints

- Tỉ lệ: Thường 25% / +15%; Hiếm 8% / +7%; Quý 2% / +4%; Huyền thoại 10% / +34%, chỉ từ con đầu đàn. Thanh tích lũy đầy (≥ 1) thì chắc chắn học được.
- Chất lượng mẫu: nuốt sống hoặc vừa giết ×1; xác cũ ×0.5; xác thối ×0.2. Hệ số nhân cả cơ hội lẫn tích lũy.
- Lên sao: sau khi học, 3 mẫu thì ★2, 9 mẫu thì ★3; đã ★3 thì mẫu chỉ còn là thức ăn.
- Hiển thị theo cấp Giải Mã: cấp 1 thấy thanh tích lũy và "???"; cấp 2 thấy tên; cấp 3 thấy % cơ hội.
- Phân tích chạy chậm khi ở ngoài, nhanh gấp 4 lần trong hang trú (`SurvivalState.InShelter`).
- Cơ quan: 4 ô (Mắt, Chân, Da, Lưng). Đổi tự do chỉ trong hang trú. Tốn khối, không tốn EXP.
- Khối lượng là máu: `MaxHealth = Mass × 10`; kích thước cơ thể theo khối.
- Mọi chữ trong game bằng tiếng Việt. Tên thiên phú: Túi Không Đáy, Nuốt Chửng, Giải Mã, Biến Hình.
- Không `git add .`; chỉ add file của task.
- Số liệu là đề xuất: để trong `const` của lớp luật, dễ chỉnh.

## Review Focus

1. **Bụng đầy khi nuốt:** con mồi nhỏ vẫn bị ăn (no bụng như cũ) nhưng không vào bụng, kèm thông báo "Túi Không Đáy đã đầy". Không mất con mồi trong im lặng (Task 3 test `TryAdd_Full_ReturnsFalse`, Task 6 bước nối Prey).
2. **Người chơi phía client (co-op):** bắt hoặc giết sinh vật khi `SurvivalStats.IsRemote` thì giữ hành vi cũ (chỉ cho ăn), không lỗi, không spawn pickup (Task 6 test `SwallowRules_RemoteCatcher_FallsBackToFeed`).
3. **Nặn cơ quan khi khối không đủ, hoặc ở ngoài hang trú:** nút bị khóa và nói rõ lý do; khối không bao giờ xuống dưới `MinMass` (Task 4 test `CanSculpt_*`, Task 7 test `TrySculpt_*`).
4. **Chết rồi tái tạo:** bụng bị xóa, nhưng cơ quan đã học, đang gắn và tiến độ tích lũy được giữ; không còn rơi pickup cơ quan (Task 7 bước OrganInteractor, test `AnalysisBook_SurvivesStomachClear`).
5. **Tích lũy ở mốc biên do sai số float** (Quý: 25 × 0.04 = 1.0): mẫu thứ 25 phải chắc chắn học (Task 1 test `Pity_Epic_Guaranteed_At25`).

---

### Task 1: Luật Giải Mã (thuần)

**Files:**
- Create: `Assets/_Project/Scripts/Swallow/GeneRarity.cs`, `Assets/_Project/Scripts/Swallow/AnalysisRules.cs`
- Test: `Assets/_Project/Tests/EditMode/AnalysisRulesTests.cs`

**Interfaces:**
- Produces:
  - `enum GeneRarity { Common, Rare, Epic, Legendary }`
  - `static class AnalysisRules`:
    - `float Chance(GeneRarity r)` → 0.25 / 0.08 / 0.02 / 0.10
    - `float PityGain(GeneRarity r)` → 0.15 / 0.07 / 0.04 / 0.34
    - `const float PityFull = 0.9999f`
    - `bool Roll(GeneRarity r, ref float pity, float quality, float chanceMultiplier, float roll01)`: hit nếu `roll01 < Chance × quality × chanceMultiplier`; trượt thì `pity += PityGain × quality` và hit nếu `pity >= PityFull`. Khi hit, pity về 0.
    - `int Stars(int samplesAfterLearn)` → ≥9: 3; ≥3: 2; còn lại 1
    - `GeneRarity FromTier(OrganTier t)`: D, C → Common; B → Rare; A → Epic; S → Legendary
    - `int DecoderLevel(int analysesDone)`: 0 → 1; ≥10 → 2; ≥30 → 3
    - `float DecoderChanceMultiplier(int level)` → `1 + 0.1 × (level − 1)`
    - `bool ShowsName(int level)` (≥2), `bool ShowsChance(int level)` (≥3)
    - `const float Fresh = 1f, Old = 0.5f, Rotten = 0.2f`

- [ ] **Step 1: Viết test**

```csharp
[Test] public void Roll_HitBelowChance() { float p=0; Assert.IsTrue(AnalysisRules.Roll(GeneRarity.Common, ref p, 1f, 1f, 0.24f)); Assert.AreEqual(0f, p); }
[Test] public void Roll_MissAddsPity() { float p=0; Assert.IsFalse(AnalysisRules.Roll(GeneRarity.Rare, ref p, 1f, 1f, 0.99f)); Assert.AreEqual(0.07f, p, 1e-5f); }
[Test] public void Pity_Common_Guaranteed_At7() => Assert.AreEqual(7, SamplesUntilLearned(GeneRarity.Common, 1f));
[Test] public void Pity_Rare_Guaranteed_At15() => Assert.AreEqual(15, SamplesUntilLearned(GeneRarity.Rare, 1f));
[Test] public void Pity_Epic_Guaranteed_At25() => Assert.AreEqual(25, SamplesUntilLearned(GeneRarity.Epic, 1f));
[Test] public void Pity_Legendary_Guaranteed_At3() => Assert.AreEqual(3, SamplesUntilLearned(GeneRarity.Legendary, 1f));
[Test] public void Quality_Halves_ChanceAndPity() { float p=0; Assert.IsFalse(AnalysisRules.Roll(GeneRarity.Common, ref p, 0.5f, 1f, 0.2f)); Assert.AreEqual(0.075f, p, 1e-5f); }
[Test] public void Stars_Thresholds() { Assert.AreEqual(1, AnalysisRules.Stars(2)); Assert.AreEqual(2, AnalysisRules.Stars(3)); Assert.AreEqual(2, AnalysisRules.Stars(8)); Assert.AreEqual(3, AnalysisRules.Stars(9)); }
[Test] public void FromTier_Maps() { Assert.AreEqual(GeneRarity.Common, AnalysisRules.FromTier(OrganTier.D)); Assert.AreEqual(GeneRarity.Legendary, AnalysisRules.FromTier(OrganTier.S)); }
[Test] public void DecoderLevel_AndReveal() { Assert.AreEqual(1, AnalysisRules.DecoderLevel(9)); Assert.AreEqual(2, AnalysisRules.DecoderLevel(10)); Assert.AreEqual(3, AnalysisRules.DecoderLevel(30)); Assert.IsFalse(AnalysisRules.ShowsName(1)); Assert.IsTrue(AnalysisRules.ShowsName(2)); Assert.IsFalse(AnalysisRules.ShowsChance(2)); Assert.IsTrue(AnalysisRules.ShowsChance(3)); }
// helper: luôn roll 0.999 (trượt), đếm số mẫu đến khi Roll trả true
static int SamplesUntilLearned(GeneRarity r, float q) { float p=0; for (int i=1;i<200;i++) if (AnalysisRules.Roll(r, ref p, q, 1f, 0.999f)) return i; return -1; }
```

- [ ] **Step 2:** Chạy test EditMode `AnalysisRulesTests` trong Unity Test Runner (hoặc `Unity -batchmode -runTests -testPlatform EditMode -testFilter AnalysisRulesTests`). Kỳ vọng FAIL: không biên dịch được.
- [ ] **Step 3:** Cài đặt hai file theo Interfaces.
- [ ] **Step 4:** Chạy lại, kỳ vọng PASS.
- [ ] **Step 5:** Commit `feat(swallow): analysis odds, pity and stars`.

### Task 2: Bảng di truyền và Sổ Giải Mã (thuần)

**Files:**
- Create: `Assets/_Project/Scripts/Swallow/Genome.cs`, `Assets/_Project/Scripts/Swallow/AnalysisBook.cs`
- Test: `Assets/_Project/Tests/EditMode/AnalysisBookTests.cs`

**Interfaces:**
- Consumes: `AnalysisRules`, `GeneRarity` (Task 1); `DropTableSO.Organs`, `OrganSO.Tier`.
- Produces:
  - `readonly struct Gene { public readonly OrganSO Organ; public readonly GeneRarity Rarity; }`
  - `static class Genome`:
    - `List<Gene> From(DropTableSO table, bool isLeader)`: bỏ organ null; bỏ Legendary khi `!isLeader`; trùng organ thì lấy một.
  - `enum GeneOutcome { Progress, Learned, StarUp, FoodOnly }`
  - `struct GeneResult { public OrganSO Organ; public GeneOutcome Outcome; public int Stars; public float Pity; }`
  - `class AnalysisBook`:
    - `List<GeneResult> Submit(IReadOnlyList<Gene> genes, float quality, Func<float> roll01)`: mỗi gene một lần roll, theo thứ tự danh sách.
      - Chưa học: gọi `AnalysisRules.Roll`, kết quả Learned hoặc Progress.
      - Đã học, < ★3: đếm thêm mẫu, kết quả StarUp nếu sao tăng, ngược lại Progress.
      - Đã ★3: FoodOnly.
      - Mỗi lần Submit tăng `AnalysesDone` thêm 1.
    - `bool Knows(OrganSO o)`, `int StarsOf(OrganSO o)` (0 nếu chưa học), `float PityOf(OrganSO o)`
    - `IEnumerable<OrganSO> Known`
    - `int AnalysesDone`, `int DecoderLevel => AnalysisRules.DecoderLevel(AnalysesDone)`
    - `void Clear()` (dùng cho chuyển sinh ở bước 6)
    - Chance multiplier khi roll là `AnalysisRules.DecoderChanceMultiplier(DecoderLevel)`.

- [ ] **Step 1: Viết test**
  - `Genome_SkipsLegendaryUnlessLeader`: bảng có organ tier C và S. Với `isLeader=false` chỉ còn C; với `true` còn cả hai.
  - `Submit_LearnsOnLuckyRoll`: roll luôn 0 thì gene Common là Learned, `StarsOf == 1`.
  - `Submit_PityAccumulatesAcrossCalls`: roll luôn 0.999 thì sau 6 lần Progress, lần thứ 7 Learned.
  - `Submit_StarUpAt3And9`: sau khi học, mẫu thứ 3 sau đó là StarUp với Stars=2; mẫu thứ 9 là Stars=3; mẫu tiếp theo là FoodOnly.
  - `Submit_QualityScalesPity`: quality 0.5, roll 0.999 thì `PityOf ≈ 0.075` sau 1 lần.
  - `AnalysisBook_SurvivesStomachClear`: tạo `Stomach` (Task 3) và book; nuốt, phân tích, `stomach.Clear()` thì book vẫn `Knows`. *(Viết test này trong Task 3, sau khi có Stomach.)*
  - Organ dùng trong test: `ScriptableObject.CreateInstance<OrganSO>()`, đặt `name`, `Tier`, `Slot`; `DestroyImmediate` trong `TearDown`.
- [ ] **Step 2:** Chạy test, kỳ vọng FAIL.
- [ ] **Step 3:** Cài đặt. Book lưu `Dictionary<OrganSO, Entry { float pity; bool learned; int samplesAfter; }>`.
- [ ] **Step 4:** Chạy test, kỳ vọng PASS.
- [ ] **Step 5:** Commit `feat(swallow): genome from drop tables and analysis book`.

### Task 3: Túi Không Đáy (thuần)

**Files:**
- Create: `Assets/_Project/Scripts/Swallow/Stomach.cs`
- Test: `Assets/_Project/Tests/EditMode/StomachTests.cs`

**Interfaces:**
- Consumes: `Gene` (Task 2).
- Produces:
  - `enum StomachMode { Digest, Analyze, Keep }`
  - `class StomachItem`, các trường public:
    - `string Label`; `float Mass`; `float Nutrition`; `FoodKind Food`
    - `List<Gene> Genes` (rỗng nếu là thức ăn thường); `float Quality`
    - `StomachMode Mode`; `float Progress` (0..1)
    - `bool IsSample => Genes.Count > 0`
  - `class Stomach`:
    - `const int DefaultCapacity = 6`
    - `const float AnalyzeSeconds = 20f` (trong hang trú), `const float OutsideSpeed = 0.25f`
    - `const float DigestSeconds = 8f`
    - `int Capacity`; `IReadOnlyList<StomachItem> Items`
    - `bool TryAdd(StomachItem item)`: false khi đầy. Mode mặc định: mẫu thì Analyze, thức ăn thì Digest.
    - `void SetMode(int index, StomachMode mode)`
    - `StomachItem Spit(int index)`: lấy ra, trả item hoặc null.
    - `void Tick(float dt, bool inShelter, List<StomachItem> analyzed, List<StomachItem> digested)`:
      - Item Analyze: `Progress += dt / AnalyzeSeconds × (inShelter ? 1 : OutsideSpeed)`. Đủ 1 thì đưa vào `analyzed`, rồi tự chuyển sang Digest với `Progress = 0`.
      - Item Digest: đủ `DigestSeconds` thì gỡ khỏi bụng và đưa vào `digested`.
      - Item Keep: đứng yên.
    - `void Clear()`

- [ ] **Step 1: Viết test**
  - `TryAdd_Full_ReturnsFalse`: capacity 2, thêm 3 thì lần thứ 3 trả false.
  - `DefaultModes`: mẫu là Analyze, thức ăn là Digest.
  - `Analyze_FourTimesFasterInShelter`: trong hang trú xong sau 20 s; ở ngoài, sau 20 s mới được 0.25.
  - `Analyzed_ThenDigests`: sau khi phân tích xong, item chuyển sang Digest; thêm 8 s thì nằm trong `digested` và rời bụng.
  - `Keep_NeverProgresses`.
  - `Spit_RemovesItem`.
  - `AnalysisBook_SurvivesStomachClear` (xem Task 2).
- [ ] **Step 2:** Chạy test, kỳ vọng FAIL.
- [ ] **Step 3:** Cài đặt.
- [ ] **Step 4:** Chạy test, kỳ vọng PASS.
- [ ] **Step 5:** Commit `feat(swallow): stomach with digest, analyze and keep`.

### Task 4: Khối lượng và luật nuốt (thuần + body)

**Files:**
- Create: `Assets/_Project/Scripts/Swallow/MassRules.cs`, `Assets/_Project/Scripts/Swallow/SlimeBody.cs` (MonoBehaviour)
- Modify: `Assets/_Project/Scripts/Player/BodySize.cs`, `Assets/_Project/Scripts/Player/SlimeController.cs:235`
- Test: `Assets/_Project/Tests/EditMode/MassRulesTests.cs`; sửa `BodySizeTests.cs`

**Interfaces:**
- Produces:
  - `static class MassRules`:
    - Hằng số: `StartMass = 10f`, `MinMass = 5f`, `HealthPerMass = 10f`, `SwallowWholeRatio = 0.8f`, `DissolveSecondsPerRatio = 3f`, `DigestMassShare = 0.3f`
    - `bool CanSwallowWhole(float slimeMass, float targetMass)` → `target <= slime × 0.8`
    - `float DissolveSeconds(float slimeMass, float targetMass)` → `3 × target / (slime × 0.8)`
    - `float SculptCost(GeneRarity r)` → 2 / 4 / 7 / 12
    - `float Refund(GeneRarity r)` → 50% của SculptCost
    - `bool CanSculpt(bool inShelter, bool known, float mass, GeneRarity r)` → `inShelter && known && mass − cost >= MinMass`
    - `float MaxHealth(float mass)` → `mass × 10`
  - `BodySize.ScaleForMass(float mass)` → `Clamp(0.6 + 0.04 × mass, 0.6, 2.0)` (khối 10 ra 1.0). Xóa `ScaleFor(int)`.
  - `class SlimeBody : MonoBehaviour`:
    - `float Mass` (khởi tạo `StartMass`)
    - `void AddMass(float m)`, `bool TrySpend(float m)` (không cho xuống dưới MinMass)
    - `event Action Changed`
    - Mỗi khi đổi: `stats.State.MaxHealth = MassRules.MaxHealth(Mass)` và kẹp `Health` không vượt MaxHealth.
- [ ] **Step 1: Viết test**
  - `CanSwallowWhole_At80Percent` (10 với 8: true; 10 với 8.1: false)
  - `DissolveSeconds_Scales` (10 với 16 ra 6 s)
  - `CanSculpt_OutsideShelter_False`
  - `CanSculpt_BelowMinMass_False` (mass 8, Rare cost 4, còn 4 < 5 nên false)
  - `CanSculpt_Unknown_False`
  - `MaxHealth_TenPerMass`
  - Sửa `BodySizeTests`: `ScaleForMass(10)=1`, `ScaleForMass(0)=0.6`, `ScaleForMass(1000)=2`.
- [ ] **Step 2:** Chạy test, kỳ vọng FAIL.
- [ ] **Step 3:** Cài đặt. `SlimeController` dùng `BodySize.ScaleForMass(body.Mass)`, lấy `SlimeBody` qua `GetComponent`; không có SlimeBody thì scale 1.
- [ ] **Step 4:** Chạy test, kỳ vọng PASS.
- [ ] **Step 5:** Commit `feat(swallow): body mass drives health and size`.

### Task 5: Tiếng Vọng (hàng đợi thông báo)

**Files:**
- Create: `Assets/_Project/Scripts/Swallow/Echo.cs`
- Test: `Assets/_Project/Tests/EditMode/EchoTests.cs`

**Interfaces:**
- Produces:
  - `static class Echo`:
    - `void Say(string text)`; `bool TryNext(out string text)`; `void Clear()`
    - `const int MaxQueued = 8`: tràn thì bỏ dòng cũ nhất.
    - `string LearnedLine(GeneResult r, bool showName)`:
      - Learned: `«Đã phân tích xong. Học được cấu trúc: [Tên]»`, hoặc `[???]` khi chưa thấy tên.
      - StarUp: `«[Tên] thành thạo hơn: ★N»`.
      - Progress: `«Phân tích hoàn tất N%…»` theo pity.
      - FoodOnly: null.
- [ ] **Step 1: Viết test**
  - `Queue_FifoAndCap`: Say 10 dòng thì đọc ra 8 dòng, dòng đầu là dòng thứ 3.
  - `LearnedLine_HidesNameAtLevel1`
  - `LearnedLine_ProgressPercent`: pity 0.45 cho ra chuỗi chứa "45%".
  - `LearnedLine_FoodOnly_Null`
- [ ] **Step 2:** Chạy test, kỳ vọng FAIL.
- [ ] **Step 3:** Cài đặt.
- [ ] **Step 4:** Chạy test, kỳ vọng PASS.
- [ ] **Step 5:** Commit `feat(swallow): echo voice notifications`.

### Task 6: Nuốt Chửng: nối Prey, Predator, Carcass vào bụng

**Files:**
- Create: `Assets/_Project/Scripts/Swallow/Swallower.cs` (MonoBehaviour trên player), `Assets/_Project/Scripts/Swallow/SwallowRules.cs`, `Assets/_Project/Scripts/Swallow/CarcassSample.cs`
- Modify:
  - `Assets/_Project/Scripts/Creatures/Prey.cs` (`Catch`, xóa `PickDrop` và field `dropTable`)
  - `Assets/_Project/Scripts/Creatures/Predator.cs:276-288`
  - `Assets/_Project/Scripts/Ecology/Carcass.cs` (`Spawn` nhận thêm sample; thêm `float AgeMinutes`)
- Test: `Assets/_Project/Tests/EditMode/SwallowRulesTests.cs`

**Interfaces:**
- Consumes: `Genome.From`, `AnalysisBook`, `Stomach`, `StomachItem`, `MassRules`, `SlimeBody`, `Echo` (Task 5).
- Produces:
  - `static class SwallowRules`:
    - `float CarcassQuality(CarcassStage stage, float ageMinutes)`: Fresh và tuổi < 1.5 phút ra `Fresh` (1); Fresh còn lại ra `Old` (0.5); Rotten ra `Rotten` (0.2); Humus hoặc Gone ra 0.
    - `bool ShouldUseStomach(bool catcherIsRemote, bool hasSwallower)` → `!remote && hasSwallower`
  - `class CarcassSample : MonoBehaviour { public List<Gene> Genes; public float Mass; public string Label; }`, gắn lên carcass khi Predator chết.
  - `class Swallower : MonoBehaviour`:
    - `Stomach Stomach`; `AnalysisBook Book`
    - `bool TrySwallowAlive(string label, float mass, float nutrition, FoodKind food, List<Gene> genes)`: quality Fresh; đầy thì trả false.
    - Mỗi frame:
      - Gọi `Stomach.Tick(dt, stats.State.InShelter, …)`.
      - Item phân tích xong: `Book.Submit(item.Genes, item.Quality, UnityEngine.Random.value)`, rồi gọi `Echo.Say(Echo.LearnedLine(r, AnalysisRules.ShowsName(Book.DecoderLevel)))` cho mỗi kết quả khác null (Task 5).
      - Item tiêu hóa xong: `NetWorld.Feed(stats, item.Nutrition, FoodProfiles.For(item.Food))` và `body.AddMass(item.Mass × MassRules.DigestMassShare)`.
    - Phím **F** khi chạm carcass có `CarcassSample`:
      - Nhỏ (`CanSwallowWhole`): vào bụng ngay với quality theo `CarcassQuality`; xác thối thì `TryInfect(RottenGel, DiseaseRules.RottenBiteChance)`.
      - To: giữ F trong `DissolveSeconds` (có thanh tiến độ ở HUD), xong thì vào bụng. Nhả F thì mất tiến độ.
    - Chết (`stats.State.Died`): `Stomach.Clear()`. Book giữ nguyên.
  - Hằng số mới: `Prey.bodyMass = 3f`, `Predator.bodyMass = 15f`, `Predator.isLeader = false` (serialized).
- [ ] **Step 1: Viết test**
  - `CarcassQuality_Stages` (Fresh 0.5 phút ra 1; Fresh 2 phút ra 0.5; Rotten ra 0.2; Humus ra 0)
  - `SwallowRules_RemoteCatcher_FallsBackToFeed` (remote ra false; local có swallower ra true; local không swallower ra false)
- [ ] **Step 2:** Chạy test, kỳ vọng FAIL.
- [ ] **Step 3:** Cài đặt luật và nối:
  - `Prey.Catch`:
    - `ShouldUseStomach` đúng và `TrySwallowAlive(...)` thành công: không gọi `Feed` ngay, chơi `GelMotion.Gulp`.
    - Ngược lại `NetWorld.Feed` như cũ. Bụng đầy thì thêm `Echo.Say("Túi Không Đáy đã đầy.")`.
    - Không spawn `OrganPickup` nữa.
  - `Predator` chết: không `Feed` ngay, không spawn pickup. `Carcass.Spawn` gắn `CarcassSample { Genes = Genome.From(drops, isLeader), Mass = bodyMass }`. Bỏ chuyện cắn ăn tự động của carcass (`OnTriggerStay`) với người chơi có `Swallower`; sinh vật và người không có Swallower vẫn cắn như cũ.
- [ ] **Step 4:** Chạy test, kỳ vọng PASS. Biên dịch không lỗi trong Unity.
- [ ] **Step 5:** Commit `feat(swallow): swallow prey and carcasses into the stomach`.

### Task 7: Biến Hình: OrganHost học từ sổ, nặn bằng khối, chỉ ở hang trú

**Files:**
- Modify:
  - `Assets/_Project/Scripts/Organs/OrganHost.cs`: xóa `Invest`, `Priority`, `TryResimulate`, `TakeFood`/`foodExpRate`, `GetProgress`, `GrowthExp`. Level = số sao.
  - `Assets/_Project/Scripts/Organs/OrganInteractor.cs`: xóa luồng pickup E/F/X và `DropAllOnDeath`; chết không rơi cơ quan.
  - `Assets/_Project/Scripts/Organs/OrganRules.cs`: xóa `ResimulateCost`.
  - `Assets/_Project/Scripts/Dev/GodMenu.cs`: bỏ nút EXP; thêm nút "+10 khối" và "Học hết cơ quan trong bảng".
  - Sửa `Tests/EditMode/OrganBlueprintTests.cs`, `SkillBarMenuTests.cs` cho API mới.
- Test: `Assets/_Project/Tests/EditMode/SculptTests.cs`

**Interfaces:**
- Consumes: `AnalysisBook` (Task 2), `SlimeBody`, `MassRules` (Task 4), `Swallower.Book` (Task 6).
- Produces:
  - `enum SculptResult { Ok, NotInShelter, Unknown, NoMass, Blocked }`
  - Trên `OrganHost`:
    - `SculptResult TrySculpt(OrganSO organ, AnalysisBook book, SlimeBody body, bool inShelter)`:
      - Gọi `MassRules.CanSculpt` với `AnalysisRules.FromTier(organ.Tier)`.
      - Ô đã có cơ quan thì tháo ra (hoàn khối) trước khi gắn.
      - Gắn với `Level = book.StarsOf(organ)`.
    - `bool Remove(OrganSlot slot, SlimeBody body, bool inShelter)`: chỉ trong hang trú; hoàn `MassRules.Refund`.
    - `void SyncStars(AnalysisBook book)`: cơ quan đang gắn lên sao theo book, bắn `LeveledUp`.
  - `Learn` và `Knows` lấy từ `AnalysisBook`. `BlueprintBook` bị xóa cùng các test của nó.
  - Thiếu chất dinh dưỡng: cơ quan tan thì chỉ `Detach`, không spawn pickup; vẫn còn trong sổ.
  - `MirrorOrgan` (bản sao mạng) giữ nguyên chữ ký.
- [ ] **Step 1: Viết test** (OrganHost trên GameObject có `SurvivalStats` + `SlimeBody`, như `OrganBlueprintTests` đang làm)
  - `TrySculpt_OutsideShelter_NotInShelter`
  - `TrySculpt_Unknown_Unknown`
  - `TrySculpt_Ok_SpendsMass_LevelEqualsStars`: Common cost 2, khối 10 thành 8, level 1.
  - `TrySculpt_NoMass_KeepsMass`: khối 6, Rare, kết quả NoMass, khối vẫn 6.
  - `TrySculpt_ReplacesSlot_RefundsOld`
  - `Remove_RefundsHalf`
  - `SyncStars_RaisesLevel`
- [ ] **Step 2:** Chạy test, kỳ vọng FAIL.
- [ ] **Step 3:** Cài đặt; sửa các test cũ bị ảnh hưởng (xóa test Resimulate và EXP).
- [ ] **Step 4:** Chạy **toàn bộ** EditMode, kỳ vọng PASS. Không còn tham chiếu tới `EvolutionExp` trong Organs hay HUD. Field `EvolutionExp` trong `SurvivalState` vẫn để nguyên cho bước 5.
- [ ] **Step 5:** Commit `feat(swallow): organs learned by analysis and sculpted from mass in shelters`.

### Task 8: HUD: Bụng (B), Cơ thể (G), dòng Tiếng Vọng

**Files:**
- Create: `Assets/_Project/Scripts/Survival/SurvivalHUD.Stomach.cs` (partial)
- Modify:
  - `Assets/_Project/Scripts/Survival/HudLayout.cs:24`: thêm `HudMenu.Stomach`.
  - `Assets/_Project/Scripts/Survival/KeyPressed.cs`: thêm `B()` và `FHeld()` cho cả hai nhánh Input System và Input cũ.
  - `Assets/_Project/Scripts/Survival/SurvivalHUD.cs:59-63`: B mở/đóng Bụng.
  - `Assets/_Project/Scripts/Survival/SurvivalHUD.Drawing.cs:467-540`: viết lại `DrawOrgansPanel`; xóa `DrawBlueprints`; dòng 355 hiện khối thay cho EXP.
- Test: `Assets/_Project/Tests/EditMode/HudCatalogTests.cs` (thêm `Toggle_Stomach`)

**Interfaces:**
- Consumes: `Swallower.Stomach`, `Swallower.Book`, `OrganHost.TrySculpt`, `OrganHost.Remove`, `SlimeBody.Mass`, `Echo.TryNext`.
- Produces (chỉ là UI, không có API mới):
  - **Bảng Bụng (B):**
    - Tiêu đề `TÚI KHÔNG ĐÁY · n/6`.
    - Mỗi item: tên, thanh tiến độ, 3 nút chế độ `Tiêu hóa / Phân tích / Giữ` và nút `Nhả`. Nhả thì tạo lại `Carcass` có `CarcassSample` dưới chân slime.
    - Chọn một mẫu: hiện bảng di truyền theo cấp Giải Mã. Mỗi gene có thanh tích lũy; tên hoặc "???"; % cơ hội ở cấp 3. Màu theo độ hiếm: trắng, xanh lá, xanh dương, vàng.
    - Dòng cuối: `Giải Mã cấp N · đã phân tích M mẫu · trong tổ phân tích nhanh ×4`.
  - **Bảng Cơ thể (G):**
    - Trái: 4 ô và cơ quan đang gắn, kèm sao.
    - Phải: danh sách cơ quan đã học, mỗi cái có `★`, chi phí khối, và nút `Nặn` hoặc `Tháo (+hoàn khối)`.
    - Ngoài hang trú: nút bị khóa, dòng nhắc "Chỉ nặn lại cơ thể được trong hang trú."
    - Thiếu khối: "Không đủ khối (cần X, giữ tối thiểu 5)."
  - **Dòng Tiếng Vọng:** chữ giữa trên màn hình, mỗi dòng 3 giây, mờ dần, lấy từ `Echo.TryNext`.
  - **Thanh hòa tan:** khi đang giữ F vào xác to.
- [ ] **Step 1:** Thêm test `Toggle_Stomach` (`HudLayout.Toggle(HudMenu.None, HudMenu.Stomach) == HudMenu.Stomach`, toggle lại thì ra None). Chạy, kỳ vọng FAIL, rồi thêm enum, PASS.
- [ ] **Step 2:** Cài đặt UI theo mô tả trên.
- [ ] **Step 3: Kiểm thủ công trong Unity** (chủ dự án):
  - (a) Console sạch.
  - (b) Đâm vào con mồi: nó vào bụng; B thấy item đang Phân tích.
  - (c) Đứng trong tổ thì thanh chạy nhanh hơn rõ.
  - (d) Học được thì có dòng Tiếng Vọng.
  - (e) G ngoài tổ: nút khóa. Trong tổ: Nặn trừ khối và cơ quan hiện lên người.
  - (f) Giết thú săn mồi, F vào xác: hòa tan rồi vào bụng.
  - (g) Chết: bụng rỗng, cơ quan còn nguyên.
  - (h) Bụng đầy: có thông báo.
- [ ] **Step 4:** Commit `feat(swallow): stomach and body panels, echo line`.

### Task 9: Gắn vào player, tài liệu

**Files:**
- Modify:
  - Prefab và scene builder của player: tìm chỗ thêm `OrganHost` (`grep -rn "AddComponent<OrganHost>" Assets/_Project`), thêm `SlimeBody` và `Swallower` cùng chỗ.
  - `docs/PROJECT_STATUS.md`: thêm mục 36 (bước 1: cái gì đã đổi, phím B/G/F, checklist ở Task 8 bước 3).
- [ ] **Step 1:** Thêm component. Chạy toàn bộ EditMode, kỳ vọng PASS.
- [ ] **Step 2:** Commit `feat(swallow): wire body, stomach and swallow onto the player` và `docs: status for swallow-learn core`.

## Sau bước 1

Khi chủ dự án chơi thử và duyệt, viết plan **bước 2**: cảnh Tiếng Vọng ra đời, gồm Ân Điển, Lõi và Khuyết Điểm rút ngẫu nhiên. Hệ `Traits` (CoreSO, FlawSO, CardDealer) sẽ được xem lại ở đó.

Những thứ chưa có ở bước 1:
- Mất khối khi bị đánh.
- Giọt lõi khi chết (bước 6).
- Cây đặc biệt làm mẫu sống (bước 4).
- Xác bị loài khác ăn mất dòng gene (bước 4).
- Client co-op học từ con mồi (sau bước 4).
