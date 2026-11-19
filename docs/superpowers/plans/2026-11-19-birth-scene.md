# Cảnh Ra Đời: chia bài số mệnh — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Lúc vào vòng chơi, slime xem một cảnh khoảng 22 giây: Tiếng Vọng báo sáu Ân Điển, rồi số mệnh chia bài Lõi (theo độ hiếm), rồi chia bài Khuyết Điểm trong 3 cái đi kèm. Kết quả vào `PlayerTraits`. Màn chọn cũ bị thay hẳn.

**Architecture:**
- Luật rút (`FateRoll`), thời gian cảnh (`BirthTimeline`) và lời Tiếng Vọng (`BirthScript`) là lớp thuần, có test chạy ngoài Unity bằng `tools/cstest/run.sh`.
- `BirthScene` là một MonoBehaviour vẽ bằng IMGUI (thay `TraitPicker`), đóng băng slime bằng ba cờ cũ.
- Co-op: mỗi máy tự rút, client gửi kết quả bằng tin `evolve.tr.pick` đang có; `TraitRoom`, `CardDealer`, tin chia thẻ và tin kết quả bị bỏ.

**Tech Stack:** Unity 6.6 C#, IMGUI, NUnit EditMode, Netcode (assembly `Evolve.Net`).

**Spec:** `docs/superpowers/specs/2026-11-19-birth-scene-design.md` (cùng `docs/superpowers/specs/2026-11-19-reincarnation-survival-design.md` mục 2, 3, 4)

## Global Constraints

- Độ hiếm Lõi: Thường 65%, Hiếm 25%, Cực hiếm 10%. Chọn bậc theo tỉ lệ (bậc rỗng thì chia lại cho các bậc còn lại), rồi chọn đều trong bậc. Khuyết Điểm: chọn đều trong 3 lựa chọn của Lõi.
- Bậc của 12 Lõi: Thường = Màng Thẩm Thấu, Ăn Mòn, Quang Hợp, Cộng Sinh, Thích Nghi, Bám Dính; Hiếm = Thần Tốc, Nguỵ Sắc, Phản Chấn, Quen Đất; Cực hiếm = Con Bạc, Giữ Gốc.
- Kho rút gồm cả 12 Lõi; Lõi chưa có hiệu ứng ghi nhỏ "chưa có hiệu ứng" trên thẻ.
- Con Bạc → **Dòng Máu Tham Lam**: ×1,5 cơ hội Giải Mã cho dòng gene Hiếm, Quý, Huyền thoại. Giữ Gốc → **Dạ Dày Bất Diệt**: chết khi còn mạng thì giữ món trong Túi Không Đáy (hai lần chết đầu). Giữ nguyên `Id` (`core.con_bac`, `core.giu_goc`).
- Cảnh: Drop 2 s, Echo 5 s, DealCore 7 s, DealFlaw 5 s, Summary 3 s (tổng 22 s); bỏ qua được sau 3 s (nhảy tới Summary, kết quả giữ nguyên); chạy một lần mỗi vòng chơi, không chạy lại khi hồi sinh.
- Đóng băng như màn cũ: `SurvivalStats.Frozen = true`, `State.Invulnerable = true`, `SurvivalHUD.ExternalMenuOpen = true`; trả lại giá trị cũ khi xong.
- Không có phím "chơi không Lõi". Máy chủ co-op chỉ kiểm Lõi có trong danh mục và Khuyết Điểm đúng một lựa chọn của Lõi (`TraitChoice.IsValid`).
- Mọi chữ trong game bằng tiếng Việt. Giọng đọc Tiếng Vọng là tùy chọn: có file âm thanh thì phát, không có thì im (chỉ chữ).
- Không `git add .`. Số liệu để trong hằng số, dễ chỉnh.

## Review Focus

1. **Chưa chạy menu `Evolve → Create Traits`** (danh mục asset rỗng): cảnh vẫn chạy, rút từ dữ liệu `TraitSeed` (Task 1 test `SeedFateCards_HasTwelveWithRarity`).
2. **Một bậc hiếm rỗng** (ví dụ kho chỉ có Lõi Thường): vẫn rút được, tỉ lệ chia lại (Task 1 test `Roll_EmptyTier_Renormalises`).
3. **Bấm bỏ qua trước 3 giây hoặc giữa lúc chia bài:** trước 3 giây không làm gì; sau đó nhảy tới Summary, kết quả không đổi (Task 4 test `Skip_*`).
4. **Chết rồi hồi sinh trong cùng vòng chơi:** cảnh không chạy lại, Lõi giữ nguyên (Task 4 test `ShouldRun_*`).
5. **Client co-op gửi Lõi lạ hoặc Khuyết Điểm sai:** máy chủ bỏ qua, không lỗi (Task 6, dùng lại `TraitChoice` và `DecodePick` đã có test; thêm test `Pick_UnknownCore_Rejected`).

---

### Task 1: Luật rút số mệnh (thuần)

**Files:**
- Create: `Assets/_Project/Scripts/Traits/Pure/FateRoll.cs`
- Modify: `Assets/_Project/Scripts/Traits/TraitSeed.cs` (thêm `Rarity` vào `CoreDef`, gán độ hiếm cho 12 Lõi, thêm `FateCards()`)
- Modify: `tools/cstest/cstest.csproj` (thêm `Scripts/Traits/Pure/*.cs`, `Scripts/Traits/TraitSeed.cs`, `Tests/EditMode/Traits/Pure/*.cs`)
- Test: `Assets/_Project/Tests/EditMode/Traits/Pure/FateRollTests.cs`

**Interfaces:**
- Produces:
  - `enum TraitRarity { Common, Rare, Mythic }`
  - `readonly struct FateCard { string Id; TraitRarity Rarity; string[][] Options; }` (constructor `FateCard(string id, TraitRarity rarity, string[][] options)`)
  - `readonly struct FateResult { string CoreId; TraitRarity Rarity; int OptionIndex; string[] FlawIds; }`
  - `static class FateRoll`: `float Weight(TraitRarity r)` (0.65 / 0.25 / 0.10); `FateResult? Roll(int seed, IReadOnlyList<FateCard> pool)` (null khi `pool` rỗng hoặc null); `int PlayerSeed(int sessionSeed, int playerId)`.
  - `CoreDef.Rarity`; `static IReadOnlyList<FateCard> TraitSeed.FateCards()`.

- [ ] **Step 1: Viết test** trong `FateRollTests.cs`, với bộ kho giả (`Card(id, rarity, 3 options)`):
  - `Roll_SameSeedSameResult`: hai lần `Roll(42, pool)` bằng nhau; hạt khác nhau thì thỉnh thoảng khác.
  - `Roll_TierShares`: 20000 hạt giống 0..19999, kho 6 Thường + 4 Hiếm + 2 Cực hiếm: tỉ lệ bậc nằm trong ±0,02 của 0,65 / 0,25 / 0,10.
  - `Roll_UniformInsideTier`: kho 2 Cực hiếm, mỗi cái trúng 0,4–0,6 trong các lần ra Cực hiếm.
  - `Roll_EmptyTier_Renormalises`: kho chỉ Thường và Cực hiếm (không Hiếm): tỉ lệ Thường ≈ 0,65/0,75 ± 0,02.
  - `Roll_OnlyOneTier_AlwaysThatTier`.
  - `Roll_FlawIdsAreExactlyOneOfTheOptions`: `FlawIds` bằng `Options[OptionIndex]`.
  - `Roll_EmptyOrNull_ReturnsNull`.
  - `PlayerSeed_DiffersByPlayer`: `PlayerSeed(1, 0) != PlayerSeed(1, 1)` và ổn định.
- [ ] **Step 2:** Thêm vào `FateRollTests.cs` (để bộ chạy ngoài Unity thấy được): `SeedFateCards_HasTwelveWithRarity` (12 thẻ; đúng bảng bậc ở Global Constraints; số Thường 6, Hiếm 4, Cực hiếm 2). Thêm các file thuần vào `cstest.csproj`. Chạy `tools/cstest/run.sh`, kỳ vọng FAIL (không biên dịch).
- [ ] **Step 3: Cài đặt** `FateRoll` và độ hiếm trong `TraitSeed`. `Roll`: dùng `System.Random(seed)`; tính tổng trọng số của các bậc có thẻ; chọn bậc; chọn thẻ đều trong bậc; chọn lựa chọn đều trong 3.
- [ ] **Step 4:** `tools/cstest/run.sh`, kỳ vọng PASS toàn bộ.
- [ ] **Step 5: Commit** `feat(fate): rarity and the fate roll`.

### Task 2: Độ hiếm trong dữ liệu Unity

**Files:**
- Modify: `Assets/_Project/Scripts/Traits/CoreSO.cs`, `Assets/_Project/Scripts/Traits/TraitCatalog.cs`, `Assets/_Project/Editor/TraitAssets.cs`
- Test: `Assets/_Project/Tests/EditMode/TraitCatalogTests.cs` (cần Unity)

**Interfaces:**
- Consumes: `TraitRarity`, `FateCard`, `TraitSeed.FateCards()` (Task 1).
- Produces: `CoreSO.Rarity` (`TraitRarity`); `static IReadOnlyList<FateCard> TraitCatalog.FateCards()`: từ asset nếu có, nếu danh mục rỗng thì từ `TraitSeed.FateCards()`.

- [ ] **Step 1: Viết test** `FateCards_FallBackToSeedWhenNoAssets` (khi `TraitCatalog.Cores.Count == 0` thì `FateCards()` có 12 thẻ) và `FateCards_UseAssetRarity` (tạo `CoreSO` trong bộ nhớ không được vì `Resources` phải có asset: bỏ qua nếu danh mục rỗng, dùng `Assert.Ignore`).
- [ ] **Step 2:** Cài đặt `CoreSO.Rarity`, `TraitCatalog.FateCards()`; `TraitAssets.Create` gán `so.Rarity = def.Rarity` (cập nhật asset cũ tại chỗ).
- [ ] **Step 3:** Mở Unity (chủ dự án) chạy `Evolve → Create Traits` một lần để asset có độ hiếm. Kiểm: Console không lỗi.
- [ ] **Step 4: Commit** `feat(fate): core rarity in assets and the fate pool`.

### Task 3: Làm lại hai Lõi cực hiếm

**Files:**
- Modify: `Assets/_Project/Scripts/Traits/TraitSeed.cs` (tên, tóm tắt, mô tả của hai Lõi), `Scripts/Traits/DeathLossRules.cs`, `Scripts/Traits/TraitTuning.cs`, `Scripts/Swallow/Pure/AnalysisBook.cs`, `Scripts/Swallow/Swallower.cs`
- Test: `Tests/EditMode/DeathLossRulesTests.cs` (sửa), `Tests/EditMode/Swallow/Pure/AnalysisBookTests.cs` (thêm), `TraitSeedTests.cs` (thêm)

**Interfaces:**
- Produces:
  - `TraitTuning.ThamLamFactor = 1.5f`
  - `AnalysisBook.RareGeneBonus` (`float`, mặc định 1): nhân với cơ hội của gene không phải Thường trong `Submit`.
  - `DeathLossRules.KeepsStomach(bool hasCore, int livesAfterDeath) => hasCore && livesAfterDeath >= 1` (thay `KeepsEquippedOrgans`).

- [ ] **Step 1: Viết test:**
  - `AnalysisBookTests.RareGeneBonus_RaisesOnlyNonCommon`: `RareGeneBonus = 1.5`; gene Hiếm với roll 0,11 học được (0,08 × 1,5 = 0,12); gene Thường với roll 0,26 không học được; bonus 1 thì roll 0,11 không học.
  - `DeathLossRulesTests`: `KeepsStomach` đúng với 2 mạng và 1 mạng, sai với 0, sai khi không có Lõi, sai khi âm (đổi tên từ các test cũ).
  - `TraitSeedTests.ReworkedCores`: tên hiển thị của `core.con_bac` là "Dòng Máu Tham Lam", của `core.giu_goc` là "Dạ Dày Bất Diệt"; `Id` giữ nguyên; vẫn `Implemented`.
- [ ] **Step 2:** Chạy, kỳ vọng FAIL.
- [ ] **Step 3: Cài đặt.** `Swallower` đặt `Book.RareGeneBonus = ThamLamFactor` mỗi frame nếu `PlayerTraits.HasCore("core.con_bac")`, ngược lại 1. `Swallower.OnDied`: nếu `DeathLossRules.KeepsStomach(HasCore("core.giu_goc"), stats.State.Lives)` thì **không** gọi `stomach.Clear()` (vẫn bỏ `touching`). Cập nhật chữ ở `TraitSeed` cho hợp tác dụng mới; `TraitTuning` bỏ các hằng số chỉ dùng cho tỉ lệ rơi cơ quan **không** xóa (còn test dùng).
- [ ] **Step 4:** `tools/cstest/run.sh` PASS. Compile Unity do chủ dự án kiểm.
- [ ] **Step 5: Commit** `feat(fate): Dòng Máu Tham Lam and Dạ Dày Bất Diệt`.

### Task 4: Lời Tiếng Vọng và thời gian cảnh (thuần)

**Files:**
- Create: `Assets/_Project/Scripts/Traits/Pure/BirthScript.cs`, `Assets/_Project/Scripts/Traits/Pure/BirthTimeline.cs`
- Test: `Assets/_Project/Tests/EditMode/Traits/Pure/BirthTests.cs`

**Interfaces:**
- Produces:
  - `enum BirthPhase { Drop, Echo, DealCore, DealFlaw, Summary, Done }`
  - `static class BirthTimeline`: `const float Drop = 2, Echo = 5, DealCore = 7, DealFlaw = 5, Summary = 3, Total = 22, SkipAfter = 3`; `BirthPhase PhaseAt(float t)`; `float PhaseStart(BirthPhase p)`; `float PhaseTime(float t)` (giây trong bậc hiện tại); `float Skip(float t)` (trả `t` nếu `t < SkipAfter` hoặc đã ở Summary trở đi, ngược lại `PhaseStart(Summary)`); `float DealProgress(float phaseTime, int index, int count)` (0..1, thẻ chia tuần tự trong 40% đầu bậc); `float FlipProgress(float phaseTime)` (0 cho đến 65% bậc, 1 ở 90%); `bool ShouldRun(bool hasCore, bool ranThisRun)` (`!hasCore && !ranThisRun`).
  - `static class BirthScript`: `readonly struct Gift { string Id; string Name; string Line; }`; `IReadOnlyList<Gift> Gifts` (6: `gift.tui`, `gift.nuot`, `gift.giaima`, `gift.bienhinh`, `gift.songdo`, `gift.phanthan`, tên và một dòng tác dụng theo spec mục 3); `IReadOnlyList<(string id, string text)> EchoLines` (có `echo.born` «Xác nhận… Cá thể mới hình thành.», `echo.gift` «Ban tặng Ân Điển Khởi Sinh.», `echo.core` «Phát hiện Lõi độc nhất.», `echo.flaw` «Phát hiện khiếm khuyết.», `echo.done`); `string SummaryLine(string coreName, IReadOnlyList<string> flawNames)`.

- [ ] **Step 1: Viết test** trong `BirthTests.cs`:
  - `PhaseAt_Boundaries`: 0 → Drop; 1,99 → Drop; 2 → Echo; 7 → DealCore; 14 → DealFlaw; 19 → Summary; 22 → Done; âm → Drop.
  - `Total_IsSumOfPhases` (22).
  - `Skip_Before3_DoesNothing` (`Skip(1.5f) == 1.5f`); `Skip_During_DealCore_JumpsToSummary` (`Skip(9f) == 19f`); `Skip_InSummary_DoesNothing` (`Skip(20f) == 20f`).
  - `DealProgress_StaggeredAndClamped`, `FlipProgress_Zero_ThenOne`.
  - `ShouldRun`: bảng sự thật 4 trường hợp.
  - `Gifts_SixUniqueWithText`; `EchoLines_IdsUnique`; `SummaryLine_ContainsCoreAndFlaws` (có tên Lõi và các Khuyết Điểm nối bằng " + ").
- [ ] **Step 2:** Chạy, kỳ vọng FAIL. **Step 3:** Cài đặt. **Step 4:** PASS.
- [ ] **Step 5: Commit** `feat(birth): echo script and timeline`.

### Task 5: Cảnh ra đời (IMGUI)

**Files:**
- Create: `Assets/_Project/Scripts/Traits/BirthScene.cs`, `Assets/_Project/Scripts/Traits/BirthVoice.cs`

**Interfaces:**
- Consumes: `FateRoll`, `TraitCatalog.FateCards()` (Tasks 1–2), `BirthTimeline`, `BirthScript` (Task 4), `PlayerTraits.Set`, `TraitNet.Local`, `TraitNet.ClientPick`, `TraitNet.EncodePick`.
- Produces: `BirthScene : MonoBehaviour` tự cài bằng `[RuntimeInitializeOnLoadMethod(AfterSceneLoad)]` (như `TraitPicker` cũ); `BirthVoice.Play(string lineId)` phát clip `Resources/Voice/Echo/<lineId>` nếu có.

- [ ] **Step 1: Cài đặt `BirthScene`:**
  - Mỗi 0,5 giây tìm `TraitNet.Local`; khi có và `BirthTimeline.ShouldRun(local.CoreId != null, ranThisRun)` thì bắt đầu.
  - Hạt giống: `FateRoll.PlayerSeed(Environment.TickCount, 0)` (người chơi cục bộ).
  - Rút kết quả **trước** khi vẽ; `FateRoll.Roll` null (kho rỗng) thì ghi cảnh báo và không chạy cảnh.
  - Đóng băng bằng ba cờ như `TraitPicker.SyncSpace` cũ; thời gian dùng `Time.unscaledDeltaTime`.
  - Vẽ theo `BirthTimeline`: nền tối mờ dần, giọt trượt xuống, dòng Tiếng Vọng hiện từng dòng, sáu Ân Điển hiện dần, thẻ úp xếp hình quạt rồi xáo, thẻ số mệnh trượt ra giữa và lật (`DealProgress`, `FlipProgress`), viền lưng thẻ theo độ hiếm (trắng, xanh dương, vàng), thẻ Cực hiếm sáng thêm; Khuyết Điểm: ba thẻ tối.
  - Thẻ Lõi chưa có hiệu ứng (`CoreSO.Implemented == false`) có dòng nhỏ "chưa có hiệu ứng".
  - Space hoặc Enter: `t = BirthTimeline.Skip(t)`.
  - Khi `t >= Total`: `PlayerTraits.Set(result.CoreId, result.FlawIds)`; nếu `TraitNet.IsClient` thì `TraitNet.ClientPick?.Invoke(coreId, flawIds)`; trả lại ba cờ; đánh dấu `ranThisRun`.
  - Dùng lại kiểu vẽ thẻ gel của `TraitPicker` (màu, khung, đĩa sáng).
- [ ] **Step 2: Cài đặt `BirthVoice`:** `Resources.Load<AudioClip>("Voice/Echo/" + id)`; không có thì không làm gì; phát bằng một `AudioSource` 2D trên đối tượng của `BirthScene`. Gọi khi mỗi dòng Tiếng Vọng bắt đầu.
- [ ] **Step 3: Kiểm thủ công trong Unity** (chủ dự án): Console sạch; vào Play thấy cảnh 22 giây; Space sau 3 giây nhảy tới tổng kết; xong thì slime điều khiển được và HUD hiện; bấm chết rồi hồi sinh (F1 → Tự sát, R) **không** chạy lại cảnh; chạy 5 lần vẫn ra các Lõi khác nhau.
- [ ] **Step 4: Commit** `feat(birth): the birth scene with fate card dealing`.

### Task 6: Cắt bỏ màn chọn cũ, đơn giản hóa co-op

**Files:**
- Delete: `Scripts/Traits/TraitPicker.cs`, `Scripts/Traits/TraitRoom.cs`, `Scripts/Traits/CardDealer.cs`, `Tests/EditMode/TraitRoomTests.cs`, `Tests/EditMode/CardDealerTests.cs` (kèm `.meta` nếu có)
- Modify: `Scripts/Traits/TraitNet.cs`, `Scripts/Net/TraitsSync.cs`, `Tests/EditMode/TraitNetTests.cs`

**Interfaces:**
- Consumes: `BirthScene` (Task 5) đã thay chỗ `TraitPicker`.
- Produces: `TraitNet` chỉ còn `Local`, `ClientPick`, `IsClient`, `EncodePick`, `DecodePick`, `EncodeHand`, `DecodeHand`, `TraitChoice`. `TraitsSync` chỉ đăng ký `evolve.tr.pick` ở máy chủ: kiểm bằng `TraitCatalog.Core(coreId) != null` (hoặc có trong `TraitSeed` khi danh mục rỗng) và `TraitChoice.IsValid`, rồi `GiveTraitsToAvatar`.

- [ ] **Step 1: Viết test** `TraitNetTests.Pick_UnknownCore_Rejected` (hàm thuần `TraitChoice.IsKnownPick(IReadOnlyList<FateCard> pool, string coreId, string[] flawIds)`: đúng khi Lõi có trong kho và Khuyết Điểm là một lựa chọn của nó; sai khi Lõi lạ, Khuyết Điểm sai, hoặc rỗng) và giữ các test hiện có còn đúng. Chạy, kỳ vọng FAIL.
- [ ] **Step 2:** Cài đặt `TraitChoice.IsKnownPick`; dùng nó trong `TraitsSync.OnPick`. Xóa các tệp, tin `evolve.tr.hand`/`evolve.tr.result`, móc `HostHand`/`HostConfirm`/`HandReceived`/`ResultReceived`/`LastHand`, `autoPicked`. Chuyển `TraitsSync.RegisterMessages` còn đăng ký `PickMessage` (server) và gán `TraitNet.IsClient`/`ClientPick` (client); `Update` vẫn dọn khi mất kết nối.
- [ ] **Step 3:** Grep toàn `Assets/_Project` tìm `TraitRoom`, `CardDealer`, `ConfirmResult`, `ConfirmOutcome`, `TraitPicker`, `HostHand`, `HostConfirm`, `HandReceived`, `ResultReceived`: không còn tham chiếu nào. Chạy `tools/cstest/run.sh`, kỳ vọng PASS.
- [ ] **Step 4:** Kiểm thủ công (chủ dự án): solo chạy cảnh; Console sạch; (nếu thử được co-op) client rút xong thì avatar của nó trên máy chủ có Lõi.
- [ ] **Step 5: Commit** `refactor(traits): drop the choosing screen, card room and hand messages`.

### Task 7: Tài liệu và giọng đọc

**Files:**
- Create: `docs/art/echo-voice-lines.md`
- Modify: `docs/PROJECT_STATUS.md` (mục 38)

- [ ] **Step 1:** `echo-voice-lines.md`: bảng `id`, chữ Tiếng Vọng tiếng Việt, và hướng dẫn giọng: bình tĩnh, trung tính, hơi máy, nữ, **không bắt chước một diễn viên thật**; lưu mỗi dòng thành `Assets/_Project/Resources/Voice/Echo/<id>.mp3` (hoặc `.wav`). Không có file thì cảnh chạy bằng chữ.
- [ ] **Step 2:** `PROJECT_STATUS.md` mục 38: cảnh ra đời làm gì, độ hiếm, hai Lõi làm lại, cách bỏ qua, co-op đã đổi thế nào, checklist kiểm (Task 5 bước 3 và Task 6 bước 4), nhắc chạy `Evolve → Create Traits`.
- [ ] **Step 3: Commit** `docs: status for the birth scene and the voice line list`.

## Sau kế hoạch này

Dị Tượng vào cảnh khi có Chu Kỳ Lũ (bước 3) và hệ sinh thái (bước 4). Hiệu ứng cho 9 Lõi và 8 Khuyết Điểm còn "chưa có hiệu ứng" làm dần, mỗi cái xong là có tác dụng thật.
