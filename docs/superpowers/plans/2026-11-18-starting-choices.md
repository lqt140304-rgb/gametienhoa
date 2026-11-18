# Lựa chọn đầu game (Dị Năng, Lõi, Khuyết Điểm) — Kế hoạch cài đặt lát cắt đầu

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Người chơi (solo hoặc co-op tối đa 8) nhận 3 thẻ Lõi riêng ở màn hình tải, chọn 1 Lõi và 1 Khuyết Điểm theo Lõi, rồi các hiệu ứng của 3 Lõi và 4 Khuyết Điểm của lát cắt đầu chạy trong game.

**Architecture:** Toàn bộ luật chọn và tính toán là hàm thuần trong `Assets/_Project/Scripts/Traits/` (namespace `Evolve.Traits`, assembly `Evolve`) nên kiểm thử được bằng EditMode. Dữ liệu Lõi và Khuyết Điểm là ScriptableObject nạp từ `Resources/Traits`, sinh ra từ bảng `TraitSeed` bằng một menu Editor. Hiệu ứng nối vào hook có sẵn (`SlimeController`, `SurvivalState`, `OrganInteractor`, `DropTableSO`). Đồng bộ co-op theo mẫu `TreefolkSync` (named message, host là trọng tài).

**Tech Stack:** Unity 6000.6.9f1, URP, C# 9, NUnit EditMode (`Evolve.Tests.EditMode`), Netcode for GameObjects (`CustomMessagingManager`), IMGUI (`OnGUI`, như `CoopMenu`).

**Spec:** `docs/superpowers/specs/2026-11-18-starting-choices-design.md` (mục 13 là các quyết định chốt sau khi duyệt).

## Global Constraints
- Môi trường dựng plan không có Unity: **không chạy được test hay game ở đây**. Mỗi task ghi lệnh chạy thật; người triển khai chạy trong Unity (Window → General → Test Runner → EditMode → Run, hoặc `Unity -batchmode -projectPath . -runTests -testPlatform EditMode -logFile -`).
- Hiệu ứng nào cần hệ chưa có thì **không làm giả, không cài**: chỉ 3 Lõi (Nhân Con Bạc, Nhân Giữ Gốc, Nhân Quen Đất) và 4 Khuyết Điểm (Đói Sớm, Run Rẩy, Luống Cuống, Dạ Dày Kén) được cài hiệu ứng; 9 Lõi và 8 Khuyết Điểm còn lại chỉ tồn tại ở dạng dữ liệu, hiện được trong thẻ nhưng chưa có hiệu ứng (đánh dấu `Implemented = false`).
- Mọi số liệu nằm trong `TraitTuning` (hằng số có tên), không rải trong logic. Giá trị tạm theo spec: mỗi người luôn 3 thẻ; bù cơ quan 35% mỗi cơ quan đúng vị trí theo độ nguyên vẹn, tổng tối đa 70%; Con Bạc tăng tỉ lệ rơi tương đối D +20%, C +16%, B +12%, A +8%, S +4%, boss một nửa, có trần không bao giờ đạt chắc chắn; Quen Đất ô 4 m, tối đa +15% tốc độ; Run Rẩy khựng 0,7 s; Đói Sớm độ no tụt nhanh +10%; Luống Cuống khi máu dưới 25%; Dạ Dày Kén hấp thụ chậm 30% trong 20 s với thức ăn lạ lần đầu.
- Không biến mutable static không có reset (đã quy ước của dự án). Không thêm hệ vào `SkillBar`/`AbilityCaster`.
- Đã có 4 vị trí `OrganSlot { Eyes, Legs, Skin, Back }`; ánh xạ bù: Chân→Legs; Mắt, Cảm quan phụ→Eyes; Da/màng→Skin; Lưng→Back. Vị trí chưa có thì Khuyết Điểm đó chưa bù được.
- Chống trùng Lõi giữa người chơi; Dị Năng trùng giữa người chơi cho phép; xác nhận là cuối cùng; chơi một mình đi cùng màn chọn.
- Commit message tiếng Anh ngắn gọn; mỗi task một commit.

## Review Focus
- Phòng 1 người, hoặc kho chưa đủ 3 Lõi: vẫn phải ra bộ thẻ hợp lệ, không crash, không thẻ trùng trong cùng bộ.
- Hai người xác nhận cùng một Lõi trong cùng khung hình: đúng một người được, người kia được thay thẻ và không bao giờ có hai người cùng Lõi.
- Người xác nhận xong rồi rời phòng, rồi có người vào trễ: Lõi trả về kho, người vào trễ nhận bộ thẻ hợp lệ từ phần còn lại.
- Cơ quan độ nguyên vẹn 0, âm, hoặc lớn hơn 1, hoặc nhiều cơ quan cùng vị trí: tỉ lệ bù luôn nằm trong [0, 0.7].
- Tỉ lệ rơi sau khi nhân vận may: luôn nằm trong [0, trần] với mọi giá trị đầu vào, kể cả `DropChance` bằng 0 hoặc 1.
- Nhân Giữ Gốc ở lần chết cuối (hết mạng) và khi chưa có Lõi nào: không giữ cơ quan.
- Người chơi chọn Lõi nhưng không có Khuyết Điểm hợp lệ (dữ liệu hỏng): màn chọn báo lỗi rõ ràng, không treo ván.

## File Structure
Tạo mới, thư mục `Assets/_Project/Scripts/Traits/` (namespace `Evolve.Traits`):
- `TraitTuning.cs` — hằng số số liệu.
- `CoreSO.cs`, `FlawSO.cs` — dữ liệu ScriptableObject.
- `TraitSeed.cs` — bảng 12 Lõi và 12 Khuyết Điểm (thuần dữ liệu) để sinh asset và kiểm thử luật.
- `TraitCatalog.cs` — nạp `CoreSO`/`FlawSO` từ `Resources/Traits`, tra cứu theo Id.
- `CardDealer.cs` — chia thẻ thuần.
- `TraitRoom.cs` — trạng thái phòng thuần: bộ thẻ, khóa Lõi, thay thẻ, rời phòng, vào trễ.
- `FlawCompensation.cs` — bù cơ quan thuần.
- `PlayerTraits.cs` — component giữ Lõi và Khuyết Điểm đã chọn của một người chơi.
- `LuckRules.cs`, `DeathLossRules.cs`, `FamiliarityGrid.cs`, `FlawRules.cs` — hàm thuần cho các hiệu ứng.
- `Assets/_Project/Scripts/Net/TraitsSync.cs` — đồng bộ co-op.
- `Assets/_Project/Scripts/Traits/TraitPicker.cs` — màn hình chọn IMGUI.
- `Assets/_Project/Editor/TraitAssets.cs` — menu `Evolve/Create Traits`.
Sửa: `DropTableSO.cs`, `OrganInteractor.cs`, `SlimeController.cs`, `SurvivalState.cs`, `CoopManager.cs`.
Test: `Assets/_Project/Tests/EditMode/TraitSeedTests.cs`, `CardDealerTests.cs`, `TraitRoomTests.cs`, `FlawCompensationTests.cs`, `LuckRulesTests.cs`, `DeathLossRulesTests.cs`, `FamiliarityGridTests.cs`, `FlawRulesTests.cs`.

---

### Task 1: Dữ liệu Lõi và Khuyết Điểm (`TraitSeed`, `CoreSO`, `FlawSO`, `TraitTuning`)

**Files:**
- Create: `Assets/_Project/Scripts/Traits/TraitTuning.cs`, `CoreSO.cs`, `FlawSO.cs`, `TraitSeed.cs`, `TraitCatalog.cs`
- Test: `Assets/_Project/Tests/EditMode/TraitSeedTests.cs`

**Interfaces:**
- Produces:
  - `public static class TraitTuning` với `public const int HandSize = 3; public const float PerOrganCompensation = 0.35f, MaxCompensation = 0.7f, QuenDatCell = 4f, QuenDatMaxBonus = 0.15f, RunRayFreeze = 0.7f, HungerBurnExtra = 0.10f, LuongCuongHealth = 0.25f, DaDayKenAbsorb = 0.70f, DaDayKenWindow = 20f, BossLuckFactor = 0.5f, DropChanceCap = 0.95f;` và `public static float LuckBonus(Evolve.Organs.OrganTier tier)` (D 0.20, C 0.16, B 0.12, A 0.08, S 0.04).
  - `public sealed class FlawDef { public string Id, Name, Description; public Evolve.Organs.OrganSlot[] CompensatedBy; public bool Implemented; }`
  - `public sealed class CoreDef { public string Id, Name, Summary, Detail; public string[][] Options; public bool Heavy; public bool Implemented; }` (`Options` luôn đúng 3 phần tử; mỗi phần tử 1 Id Khuyết Điểm, hoặc đúng 2 nếu `Heavy`).
  - `public static class TraitSeed { public static IReadOnlyList<CoreDef> Cores { get; } public static IReadOnlyList<FlawDef> Flaws { get; } }` chứa đúng 12 Lõi và 12 Khuyết Điểm của spec mục 8 và 9, Id không dấu dạng `core.quen_dat`, `flaw.run_ray`.
  - `public class CoreSO : ScriptableObject` (`Id, DisplayName, Summary, Detail, OrbColor, Options (FlawOption[]), Heavy, Implemented`), `public class FlawSO : ScriptableObject` (`Id, DisplayName, Description, CompensatedBy, Implemented`), `[Serializable] public struct FlawOption { public string[] FlawIds; }`.
  - `public static class TraitCatalog { public static IReadOnlyList<CoreSO> Cores { get; } public static CoreSO Core(string id); public static FlawSO Flaw(string id); public static void Reload(); }` đọc `Resources.LoadAll<CoreSO>("Traits")`; không có asset thì danh sách rỗng, không ném lỗi.

- [ ] **Step 1: Viết test thất bại `TraitSeedTests`**: `TwelveCoresAndTwelveFlaws` (đếm 12 và 12, Id duy nhất, không rỗng); `EveryCoreHasThreeOptionsWithKnownFlaws` (mỗi `Options.Length == 3`, mọi Id Khuyết Điểm có trong `Flaws`); `HeavyCoresUsePairsAndOthersSingles` (`core.con_bac`, `core.giu_goc` có mỗi lựa chọn đúng 2 Id khác nhau; các Lõi khác đúng 1); `FlawRulesFromSpec` với các cặp cấm: `core.bam_dinh` không chứa `flaw.bam_bun`, `flaw.run_ray`; `core.thich_nghi` không chứa Khuyết Điểm tên bắt đầu `flaw.so_`; `core.quang_hop`, `core.cong_sinh` đúng bảng spec mục 8; `OnlySliceImplemented` (đúng `core.con_bac`, `core.giu_goc`, `core.quen_dat` và `flaw.doi_som`, `flaw.run_ray`, `flaw.luong_cuong`, `flaw.da_day_ken` có `Implemented == true`, phần còn lại `false`); `LuckBonusByTier` (D 0.20, C 0.16, B 0.12, A 0.08, S 0.04).
- [ ] **Step 2: Chạy EditMode, xác nhận FAIL (biên dịch lỗi: kiểu chưa có)**
- [ ] **Step 3: Cài `TraitTuning`, `CoreDef`/`FlawDef`, `TraitSeed` (bảng 12 Lõi, 12 Khuyết Điểm chép đúng từ spec mục 8 và 9, tên tiếng Việt có dấu trong `Name`, `Summary` là mô tả rút gọn một dòng, `Detail` là luật đầy đủ từ spec), `CoreSO`, `FlawSO`, `FlawOption`, `TraitCatalog`.**
- [ ] **Step 4: Chạy EditMode, xác nhận `TraitSeedTests` PASS**
- [ ] **Step 5: Commit** — `git add Assets/_Project/Scripts/Traits Assets/_Project/Tests/EditMode/TraitSeedTests.cs && git commit -m "feat(traits): core and flaw data, tuning and seed table"`

### Task 2: Menu Editor sinh asset

**Files:**
- Create: `Assets/_Project/Editor/TraitAssets.cs`

**Interfaces:**
- Consumes: `TraitSeed.Cores/Flaws`, `CoreSO`, `FlawSO`, `FlawOption`.
- Produces: `public static class TraitAssets { [MenuItem("Evolve/Create Traits")] public static void Create(); }` sinh 12 `CoreSO` và 12 `FlawSO` vào `Assets/_Project/Resources/Traits/` (`Core_<Id>.asset`, `Flaw_<Id>.asset`); chạy lại thì cập nhật tại chỗ, không nhân đôi; `OrbColor` là màu tạm theo chủ đề (hue theo chỉ số Lõi trong bảng).

- [ ] **Step 1: Cài `Create()` bằng `AssetDatabase.LoadAssetAtPath` để cập nhật nếu đã có, ngược lại `CreateAsset`; cuối cùng `AssetDatabase.SaveAssets()` và `Debug.Log` số lượng.**
- [ ] **Step 2: Chạy menu `Evolve → Create Traits` trong Unity, mở thư mục `Resources/Traits`, xác nhận có 24 asset; chạy lần hai vẫn 24.**
- [ ] **Step 3: Commit** — `git add Assets/_Project/Editor/TraitAssets.cs Assets/_Project/Resources && git commit -m "feat(traits): editor menu that creates core and flaw assets"`

### Task 3: Chia thẻ và trạng thái phòng (`CardDealer`, `TraitRoom`)

**Files:**
- Create: `Assets/_Project/Scripts/Traits/CardDealer.cs`, `TraitRoom.cs`
- Test: `Assets/_Project/Tests/EditMode/CardDealerTests.cs`, `TraitRoomTests.cs`

**Interfaces:**
- Produces:
  - `public static class CardDealer { public static string[][] Deal(int seed, int players, IReadOnlyList<string> coreIds, int hand = TraitTuning.HandSize); }` — trả `players` bộ thẻ, mỗi bộ `min(hand, coreIds.Count)` Id khác nhau trong cùng bộ; ưu tiên chia không trùng giữa các bộ, chỉ trùng khi hết Lõi chưa chia; cùng `seed` cho cùng kết quả; `players <= 0` hoặc kho rỗng trả mảng rỗng.
  - `public sealed class TraitRoom { public TraitRoom(int seed, IReadOnlyList<string> coreIds); public string[] Join(int playerId); public ConfirmResult Confirm(int playerId, string coreId); public void Leave(int playerId); public string[] Hand(int playerId); public bool IsLocked(string coreId); public string OwnerOf(string coreId); }`
  - `public enum ConfirmOutcome { Accepted, TakenReplaced, Invalid }`, `public readonly struct ConfirmResult { public ConfirmOutcome Outcome; public string[] NewHand; }` — `Confirm` theo thứ tự gọi (không có thời gian): người đầu được Lõi và nó bị khóa; người sau nhận `TakenReplaced` với `NewHand` đã thay thẻ bị khóa bằng một Lõi chưa khóa; `Invalid` khi `coreId` không có trong bộ thẻ của người đó hoặc người đó đã xác nhận rồi. `Leave` trả Lõi đã khóa về kho.

- [ ] **Step 1: Viết test thất bại**: `DealGivesThreeDistinctPerHand`; `DealIsDistinctAcrossPlayersWhenEnoughCores` (24 Lõi, 8 người, không Id nào xuất hiện ở hai bộ); `DealOverlapsOnlyWhenShort` (12 Lõi, 8 người: mỗi bộ 3 Id khác nhau trong bộ, tổng số Id khác nhau trên toàn phòng đúng 12); `DealDeterministicBySeed`; `DealSoloAndEmpty` (1 người nhận 3 thẻ; 0 người hoặc kho rỗng trả rỗng; kho 2 Lõi mỗi bộ 2 thẻ); `RoomFirstConfirmWins` và `RoomSecondGetsReplacementNeverDuplicate` (hai người cùng có `core.x` trong bộ, người thứ hai không bao giờ ra `Accepted` cùng Lõi, `NewHand` không chứa Lõi bị khóa và có 3 thẻ khác nhau khi kho đủ); `RoomLeaveReleasesCore`; `RoomLateJoinGetsHandFromUnlocked` (người vào trễ không nhận Lõi đã khóa); `RoomConfirmInvalid` (Id ngoài bộ, xác nhận hai lần).
- [ ] **Step 2: Chạy EditMode, xác nhận FAIL**
- [ ] **Step 3: Cài `CardDealer.Deal` (xáo bằng `System.Random(seed)`, phát vòng tròn không trùng trước, khi cạn thì lấp từ phần dư xáo lại) và `TraitRoom` (Dictionary người→bộ thẻ, Dictionary Lõi→chủ); khi thay thẻ lấy ngẫu nhiên từ Lõi chưa khóa và chưa có trong bộ của người đó, bằng `System.Random` gieo từ `seed` và `playerId`.**
- [ ] **Step 4: Chạy EditMode, xác nhận PASS**
- [ ] **Step 5: Commit** — `git add Assets/_Project/Scripts/Traits/CardDealer.cs Assets/_Project/Scripts/Traits/TraitRoom.cs Assets/_Project/Tests/EditMode && git commit -m "feat(traits): pure card dealing and room lock logic"`

### Task 4: Bù Khuyết Điểm bằng cơ quan (`FlawCompensation`)

**Files:**
- Create: `Assets/_Project/Scripts/Traits/FlawCompensation.cs`
- Test: `Assets/_Project/Tests/EditMode/FlawCompensationTests.cs`

**Interfaces:**
- Produces: `public static class FlawCompensation { public static float Fraction(IEnumerable<(Evolve.Organs.OrganSlot slot, float integrity)> worn, Evolve.Organs.OrganSlot[] compensatedBy); public static float Strength(float fraction); }` — `Fraction` = tổng `PerOrganCompensation × clamp01(integrity)` của các cơ quan có `slot` trong `compensatedBy`, kẹp [0, `MaxCompensation`]; `Strength(fraction) = 1 − fraction` (hệ số nhân còn lại của Khuyết Điểm).

- [ ] **Step 1: Viết test thất bại**: `NoOrgansGivesZero`; `OneMatchingOrganFullIntegrity` (= 0.35); `IntegrityScalesContinuously` (integrity 0.5 → 0.175); `TwoMatchingCapsAtSeventy` (2 cơ quan đúng vị trí ở 1.0 → 0.7; 3 cơ quan vẫn 0.7); `BadIntegrityIsClamped` (integrity âm → 0, lớn hơn 1 → coi như 1); `NonMatchingSlotIgnored`; `EmptyCompensatedByGivesZero` (Khuyết Điểm chưa bù được); `StrengthIsOneMinusFraction`.
- [ ] **Step 2: Chạy EditMode, xác nhận FAIL**
- [ ] **Step 3: Cài hai hàm trong `FlawCompensation.cs`.**
- [ ] **Step 4: Chạy EditMode, xác nhận PASS**
- [ ] **Step 5: Commit** — `git add Assets/_Project/Scripts/Traits/FlawCompensation.cs Assets/_Project/Tests/EditMode/FlawCompensationTests.cs && git commit -m "feat(traits): flaw compensation from worn organs"`

### Task 5: `PlayerTraits` — Lõi và Khuyết Điểm đã chọn của một người

**Files:**
- Create: `Assets/_Project/Scripts/Traits/PlayerTraits.cs`
- Modify: `Assets/_Project/Scripts/Organs/OrganHost.cs` chỉ để lộ danh sách cơ quan đang mang cho `PlayerTraits` đọc, nếu chưa có.

**Interfaces:**
- Consumes: `FlawCompensation.Fraction/Strength`, `TraitCatalog`, `OrganHost` (cơ quan đang trang bị và độ nguyên vẹn của từng cái; người triển khai tìm đúng thành viên trong `OrganHost`, nếu độ nguyên vẹn chưa có thì coi là 1).
- Produces: `public class PlayerTraits : MonoBehaviour { public string CoreId { get; } public IReadOnlyList<string> FlawIds { get; } public bool HasCore(string coreId); public bool HasFlaw(string flawId); public float FlawStrength(string flawId); public void Set(string coreId, IReadOnlyList<string> flawIds); public event System.Action Changed; }` — `FlawStrength` trả 0 nếu không có Khuyết Điểm đó, ngược lại `FlawCompensation.Strength(Fraction(đang mang, FlawSO.CompensatedBy))`; `Set` gọi `Changed`.

- [ ] **Step 1: Viết test thất bại trong `FlawCompensationTests`** cho phần thuần có thể kiểm thử: `StrengthUsesFlawCompensatedBySlots` dùng `FlawCompensation` với `OrganSlot.Legs` và không cơ quan (trả 1).
- [ ] **Step 2: Cài `PlayerTraits` gắn vào prefab người chơi hiện có (thêm component trong đúng nơi tạo avatar người chơi, tìm trong `PrototypeSceneBuilder` hoặc `SlimeController`), mặc định không có Lõi.**
- [ ] **Step 3: Chạy EditMode xác nhận không lỗi biên dịch, rồi trong Unity thêm `PlayerTraits` vào người chơi và dùng inspector kiểm tra `Set`.**
- [ ] **Step 4: Commit** — `git add Assets/_Project/Scripts && git commit -m "feat(traits): player trait component"`

### Task 6: Nhân Con Bạc (`LuckRules` + `DropTableSO`)

**Files:**
- Create: `Assets/_Project/Scripts/Traits/LuckRules.cs`
- Modify: `Assets/_Project/Scripts/Organs/DropTableSO.cs`
- Test: `Assets/_Project/Tests/EditMode/LuckRulesTests.cs`

**Interfaces:**
- Produces:
  - `public static class LuckRules { public static float TierMultiplier(Evolve.Organs.OrganTier tier, bool isBoss); public static float DropChanceMultiplier(IEnumerable<(Evolve.Organs.OrganTier tier, float share)> distribution, bool isBoss); public static float AdjustedDropChance(float baseChance, float multiplier); }` — `TierMultiplier` = `1 + LuckBonus(tier)` (boss: `1 + LuckBonus × BossLuckFactor`); `DropChanceMultiplier` = trung bình có trọng số `Σ share × TierMultiplier` chia tổng `share` (rỗng hoặc tổng 0 trả 1); `AdjustedDropChance` = `baseChance × multiplier` kẹp [0, `DropChanceCap`], và nếu `baseChance` đã lớn hơn `DropChanceCap` thì trả đúng `baseChance` (không bao giờ làm giảm).
  - `DropTableSO.Roll(Func<OrganSO,bool> isOwned = null, float dropChanceMultiplier = 1f)` — tham số mới tùy chọn nhân vào `DropChance` qua `AdjustedDropChance`; mặc định cho kết quả giống hệt hôm nay. Thành phần bậc không đổi (chỉ tăng xác suất có rơi, theo spec "tăng tỉ lệ rơi, không tăng bậc").

- [ ] **Step 1: Viết test thất bại**: `TierMultiplierMatchesSpec` (D 1.20, C 1.16, B 1.12, A 1.08, S 1.04); `BossGetsHalf` (D boss 1.10); `DropChanceMultiplierIsWeightedAverage` (share D 0.5 và S 0.5 → 1.12); `AdjustedChanceNeverReachesCertainty` (đầu vào 0.9 nhân 1.2 vẫn ≤ `DropChanceCap`; đầu vào 1.0 trả 1.0, không giảm); `ZeroChanceStaysZero`; `NoLuckIsIdentity` (multiplier 1 trả đúng đầu vào).
- [ ] **Step 2: Chạy EditMode, xác nhận FAIL**
- [ ] **Step 3: Cài `LuckRules`, sửa `DropTableSO.Roll`, rồi nối lời gọi `Roll` đang dùng ở chỗ quái chết: nếu người săn có `core.con_bac` (host tính) thì truyền `DropChanceMultiplier` tính từ `Distribution` của bảng rơi; boss truyền với `isBoss = true`.**
- [ ] **Step 4: Chạy EditMode, xác nhận PASS; test hiện có của `DropTableSO` vẫn PASS**
- [ ] **Step 5: Commit** — `git add Assets/_Project/Scripts Assets/_Project/Tests/EditMode/LuckRulesTests.cs && git commit -m "feat(traits): Nhan Con Bac raises drop odds by organ tier"`

### Task 7: Nhân Giữ Gốc (`DeathLossRules` + `OrganInteractor`)

**Files:**
- Create: `Assets/_Project/Scripts/Traits/DeathLossRules.cs`
- Modify: `Assets/_Project/Scripts/Organs/OrganInteractor.cs` (vòng lặp mất cơ quan quanh dòng 52–60)
- Test: `Assets/_Project/Tests/EditMode/DeathLossRulesTests.cs`

**Interfaces:**
- Produces: `public static class DeathLossRules { public static bool KeepsEquippedOrgans(bool hasGiuGoc, int livesAfterDeath); }` — đúng khi có Lõi và đây là lần chết thứ 1 hoặc 2 (còn mạng sau khi chết: `livesAfterDeath >= 1`), sai ở lần chết cuối (`livesAfterDeath <= 0`). Người triển khai đối chiếu ngữ nghĩa `SurvivalState.Lives` ("kể cả mạng đang dùng") để truyền đúng giá trị.

- [ ] **Step 1: Viết test thất bại**: `KeepsOnFirstDeath` (livesAfterDeath 2); `KeepsOnSecondDeath` (1); `LosesOnThirdDeath` (0); `NoCoreNeverKeeps`; `NegativeLivesLoses`.
- [ ] **Step 2: Chạy EditMode, xác nhận FAIL**
- [ ] **Step 3: Cài `KeepsEquippedOrgans`; trong `OrganInteractor` khi thân rơi cơ quan lúc chết, nếu `KeepsEquippedOrgans(...)` thì không rơi và không mất cơ quan nào (vật phẩm mang theo và kho không đổi), ngược lại giữ nguyên luật `deathLossChance` cũ.**
- [ ] **Step 4: Chạy EditMode, xác nhận PASS**
- [ ] **Step 5: Commit** — `git add Assets/_Project/Scripts Assets/_Project/Tests/EditMode/DeathLossRulesTests.cs && git commit -m "feat(traits): Nhan Giu Goc keeps worn organs on first two deaths"`

### Task 8: Nhân Quen Đất (`FamiliarityGrid` + `SlimeController`)

**Files:**
- Create: `Assets/_Project/Scripts/Traits/FamiliarityGrid.cs`
- Modify: `Assets/_Project/Scripts/Player/SlimeController.cs` (nhân thêm hệ số vào tính tốc độ quanh dòng 144–146)
- Test: `Assets/_Project/Tests/EditMode/FamiliarityGridTests.cs`

**Interfaces:**
- Produces: `public sealed class FamiliarityGrid { public void Visit(Vector2 worldXZ, float time); public float Familiarity(Vector2 worldXZ, float time); public float SpeedMultiplier(Vector2 worldXZ, float time); public void Clear(); }` — ô vuông `TraitTuning.QuenDatCell`; mỗi lần bước vào một ô mới (không phải đứng yên trong ô cũ) cộng một lượng quen; điểm giảm dần theo thời gian bỏ đường; `SpeedMultiplier = 1 + QuenDatMaxBonus × Familiarity` với `Familiarity` trong [0,1]. Hệ số tốc độ không bao giờ vượt `1 + QuenDatMaxBonus` và không dưới 1.
- `SlimeController` thêm `[System.NonSerialized] public float TraitSpeed = 1f;` nhân vào `speed`; `PlayerTraits` hoặc một component nhỏ cập nhật `TraitSpeed` mỗi khung khi có `core.quen_dat` và không đang bơi sâu.

- [ ] **Step 1: Viết test thất bại**: `NewCellIsZero`; `RevisitingRaisesFamiliarity` (đi qua cùng ô nhiều lần, tăng đơn điệu, tối đa 1); `StandingStillDoesNotGrow`; `DecaysAfterLeaving` (sau thời gian dài về 0); `MultiplierBounds` (luôn trong [1, 1.15]); `CellsAreFourMetres` (hai điểm cùng ô 4 m dùng chung điểm, hai điểm khác ô thì không); `ClearResets`.
- [ ] **Step 2: Chạy EditMode, xác nhận FAIL**
- [ ] **Step 3: Cài `FamiliarityGrid` (Dictionary ô→(điểm, lần cuối ghé)), nối vào `SlimeController` qua `TraitSpeed` và một bộ cập nhật trên `PlayerTraits`; không áp dụng khi `SwimLogic` báo đang bơi sâu.**
- [ ] **Step 4: Chạy EditMode, xác nhận PASS; trong Unity: đi vòng một đường nhiều lần với `core.quen_dat` gán qua inspector và xác nhận chạy nhanh dần.**
- [ ] **Step 5: Commit** — `git add Assets/_Project/Scripts Assets/_Project/Tests/EditMode/FamiliarityGridTests.cs && git commit -m "feat(traits): Nhan Quen Dat grid and speed bonus"`

### Task 9: Bốn Khuyết Điểm của lát cắt (`FlawRules`)

**Files:**
- Create: `Assets/_Project/Scripts/Traits/FlawRules.cs`
- Modify: `Assets/_Project/Scripts/Survival/SurvivalState.cs` (hao độ no quanh dòng 172, `FoodFilter` dòng 109, và sự kiện nhận sát thương trong `Drain` dòng 98), `Assets/_Project/Scripts/Player/SlimeController.cs` (khựng, quay)
- Test: `Assets/_Project/Tests/EditMode/FlawRulesTests.cs`

**Interfaces:**
- Consumes: `PlayerTraits.FlawStrength(flawId)` (hệ số nhân sau khi cơ quan bù).
- Produces (mọi hàm thuần, `strength` ∈ [0,1], 1 = chưa bù, 0 = bù hết nhưng quy ước tối đa 70% nên không bao giờ 0 khi có Khuyết Điểm):
  - `public static float HungerBurnMultiplier(float strength)` = `1 + HungerBurnExtra × strength`.
  - `public static float HitFreezeSeconds(float strength)` = `RunRayFreeze × strength`.
  - `public static float TurnRateMultiplier(float healthFraction, float strength)` = 1 khi `healthFraction >= LuongCuongHealth`, ngược lại nhỏ hơn 1 và nhỏ dần theo mức thấp (giá trị nhỏ nhất 0.5 khi `strength = 1`), nới dần về 1 khi `strength` giảm.
  - `public static float AbsorbMultiplier(bool firstTimeFood, float secondsSinceFirstBite, float strength)` = `1 − (1 − DaDayKenAbsorb) × strength` trong `DaDayKenWindow` giây đầu của thức ăn lạ lần đầu, ngược lại 1.

- [ ] **Step 1: Viết test thất bại**: `HungerBurnDefaultsToOneWithoutFlaw` (strength 0 → 1.0), `HungerBurnFullStrength` (1.10); `HitFreezeScalesWithStrength` (0.7 s ở strength 1, 0 ở strength 0); `TurnRateNormalAboveThreshold` và `TurnRateSlowerBelowThreshold`, `TurnRateNeverBelowHalf`; `AbsorbOnlyFirstTimeWithinWindow` (bằng 1 khi không phải lần đầu hoặc quá 20 s), `AbsorbSeventyPercentAtFullStrength` (= 0.70).
- [ ] **Step 2: Chạy EditMode, xác nhận FAIL**
- [ ] **Step 3: Cài `FlawRules`; trong `SurvivalState` nhân `burn` bằng `HungerBurnMultiplier` khi có `flaw.doi_som`, thêm `event Action<float> Damaged` bắn trong `Drain` để `SlimeController` khựng `HitFreezeSeconds` giây (chặn nảy) khi có `flaw.run_ray`, bọc `FoodFilter` bằng `AbsorbMultiplier` khi có `flaw.da_day_ken` (theo dõi thức ăn đã ăn lần đầu trong ván), và nhân tốc độ quay bằng `TurnRateMultiplier` khi có `flaw.luong_cuong`.**
- [ ] **Step 4: Chạy EditMode, xác nhận PASS; test của `SurvivalState` hiện có vẫn PASS**
- [ ] **Step 5: Commit** — `git add Assets/_Project/Scripts Assets/_Project/Tests/EditMode/FlawRulesTests.cs && git commit -m "feat(traits): four slice flaws wired into survival and movement"`

### Task 10: Đồng bộ co-op (`TraitsSync`)

**Files:**
- Create: `Assets/_Project/Scripts/Net/TraitsSync.cs`
- Modify: `Assets/_Project/Scripts/Net/CoopManager.cs` (thêm component quanh dòng 83, đăng ký handler quanh dòng 187)
- Test: `Assets/_Project/Tests/EditMode/TraitRoomTests.cs` (bổ sung phần mã hóa)

**Interfaces:**
- Consumes: `TraitRoom`, `CardDealer`, mẫu `TreefolkSync.RegisterMessages(CustomMessagingManager messaging, bool server)`.
- Produces: `public class TraitsSync : MonoBehaviour { public void RegisterMessages(CustomMessagingManager messaging, bool server); public static byte[] EncodeHand(string[] ids); public static string[] DecodeHand(byte[] data); public event System.Action<string[]> HandReceived; public event System.Action<ConfirmResult> ConfirmReceived; public void RequestConfirm(string coreId, string[] flawIds); }` — tên thông điệp: `evolve.tr.hand` (host→khách: bộ thẻ), `evolve.tr.pick` (khách→host: Lõi và Khuyết Điểm đã chọn), `evolve.tr.result` (host→khách: `Accepted` hoặc `TakenReplaced` kèm bộ thẻ mới, `Invalid`). Host giữ một `TraitRoom` duy nhất, xử lý `pick` theo thứ tự nhận; chơi một mình không dùng mạng, `TraitRoom` cục bộ.

- [ ] **Step 1: Viết test thất bại**: `HandEncodingRoundTrip` (rỗng, 1 thẻ, 3 thẻ, Id có dấu), `DecodeGarbageReturnsEmpty` (dữ liệu cắt cụt hoặc rỗng không ném lỗi).
- [ ] **Step 2: Chạy EditMode, xác nhận FAIL**
- [ ] **Step 3: Cài `TraitsSync` theo mẫu `TreefolkSync`; host: khi người chơi vào phòng gọi `Join` rồi gửi `evolve.tr.hand`, khi nhận `pick` thì `Confirm` và gửi `evolve.tr.result`; khi người rời phòng gọi `Leave`; khách xác nhận Khuyết Điểm hợp lệ với `CoreDef.Options` trước khi gửi và host kiểm tra lại.**
- [ ] **Step 4: Chạy EditMode PASS; thử trong Unity với host và một khách cục bộ (hai bản chạy): hai người cùng chọn một Lõi, người sau thấy thẻ bị thay.**
- [ ] **Step 5: Commit** — `git add Assets/_Project/Scripts/Net Assets/_Project/Tests/EditMode && git commit -m "feat(traits): co-op sync for card hands and picks"`

### Task 11: Màn hình chọn (`TraitPicker`) và nối vào vào-game

**Files:**
- Create: `Assets/_Project/Scripts/Traits/TraitPicker.cs`
- Modify: nơi vào ván hiện tại (người triển khai tìm trong `CoopManager`/`PrototypeSceneBuilder`: nơi người chơi xuất hiện) để hiện picker trước khi vào thế giới; `docs/GAME_DESIGN.md` mục 3; `docs/PROJECT_STATUS.md`.

**Interfaces:**
- Consumes: `TraitCatalog`, `TraitsSync` (co-op) hoặc `TraitRoom` cục bộ (một mình), `PlayerTraits.Set`.
- Produces: `public class TraitPicker : MonoBehaviour { public void Begin(string[] hand, System.Action<string, string[]> onConfirmed); }` vẽ bằng `OnGUI`: Dị Năng xem; ba thẻ Lõi dọc kiểu gel gọn (tên ở trên, mô tả rút gọn `Summary` ở dưới, quả cầu tạm = hình tròn `OrbColor`); phím **Tab** bật/tắt khung chi tiết (`Detail` và 3 lựa chọn Khuyết Điểm đi kèm); sau khi chọn Lõi hiện ba viên thuốc Khuyết Điểm nhỏ (Lõi nặng: ba cặp); nút Xác nhận; thẻ bị khóa mờ kèm dòng giải thích; thiếu dữ liệu (Lõi không có lựa chọn Khuyết Điểm hợp lệ) thì hiện lỗi rõ ràng và nút "Bỏ qua, chơi không Lõi".

- [ ] **Step 1: Cài `TraitPicker` theo bố cục trong spec mục 6; chọn xong gọi `onConfirmed`, rồi `PlayerTraits.Set`.**
- [ ] **Step 2: Trong Unity: chơi một mình, vào ván, chọn Lõi và Khuyết Điểm, xác nhận, kiểm `PlayerTraits` trong inspector; co-op: hai bản chạy, kiểm thẻ không trùng ở kho đủ lớn và bị thay khi trùng.**
- [ ] **Step 3: Cập nhật `docs/GAME_DESIGN.md` mục 3 và `docs/PROJECT_STATUS.md` ghi rõ phần đã cài và chưa chạy thử.**
- [ ] **Step 4: Commit** — `git add Assets/_Project/Scripts docs && git commit -m "feat(traits): trait picker screen and docs"`
