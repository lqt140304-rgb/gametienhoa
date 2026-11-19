# Cảnh Ra Đời bản 3: đường truyền sụp đổ — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Thay cảnh ra đời bản 2 bằng cảnh "đường truyền sụp đổ": sân khấu riêng với hậu kỳ URP thật, khung vỡ, số mệnh tự lật, sáu cảnh Ân Điển riêng, đóng dấu tên nghiêng kèm rung, âm thanh thật từ ElevenLabs.

**Architecture:**
- Luật thuần (có test chạy bằng `tools/cstest/run.sh`): `Instability`, `ShakeMath`, `SlamPlan`, `BirthSfx`, `BirthFlow` bản 3, `BirthScript` bản 3.
- Unity: `BirthStage` (camera riêng ở xa + Volume riêng + Canvas World Space), `BirthGlitch`, `BirthFrames`, `BirthCards`, `BirthSlam`, `BirthGifts`, `BirthAudio`; `BirthScene` thành bộ điều phối.
- Số mệnh vẫn rút bằng `FateRoll.Roll` trước khi cảnh chạy; cảnh chỉ trình diễn. Bỏ bộ bài 8 lá và việc chọn bài.

**Tech Stack:** Unity 6.6, URP 17 (Volume: ChromaticAberration, LensDistortion, FilmGrain, Vignette, Bloom, ColorAdjustments), UGUI 2.6 (`Text` với `LegacyRuntime.ttf`), ParticleSystem, ElevenLabs API (tiếng và hiệu ứng, khóa do môi trường gắn).

**Spec:** `docs/superpowers/specs/2026-11-20-birth-scene-system-collapse-design.md`

## Global Constraints

- Sân khấu ở rất xa thế giới (y = 20000), camera `depth` cao, `far` ≈ 300, nền màu đặc; Volume hậu kỳ **ở layer riêng** (31), camera cảnh chỉ nhận layer đó; hủy hết khi xong.
- **Không bao giờ kẹt người chơi:** dựng sân khấu lỗi thì ghi cảnh báo, giao số mệnh ngay, trả cờ đóng băng và con trỏ.
- Đóng băng như cũ: `SurvivalStats.Frozen`, `State.Invulnerable`, `SurvivalHUD.ExternalMenuOpen`.
- Độ ổn định: bắt đầu ≥ 0,85, về 0 ở cuối; mọi giá trị trong [0, 1]; hiệu ứng kẹp trong khoảng an toàn để chữ vẫn đọc được.
- Đóng dấu: Lõi nghiêng −7°, Khuyết Điểm +6°, Ân Điển xen kẽ ±4° đến ±9°; Lõi Cực hiếm rung gấp đôi; thứ tự Lõi, Khuyết Điểm (lần lượt nếu có hai), sáu Ân Điển.
- Nhịp: bước ở lại tối thiểu `MinSeconds` và chờ giọng đọc xong + 0,35 giây; Space/Enter sang bước kế sau 1,5 giây; Tab nhảy tới chia bài; **không có chọn bài**.
- Âm thanh: file `Resources/Sfx/Birth/<tên>.mp3` tạo từ ElevenLabs; thiếu file thì im lặng. Giọng đọc: `Resources/Voice/Echo/<id>.mp3`.
- Mọi chữ trong game bằng tiếng Việt. Không `git add .`.

## Review Focus

1. **Dựng sân khấu thất bại** (thiếu URP, lỗi tạo Volume): cảnh bỏ qua, số mệnh vẫn được giao, không đen màn hình (Task 5 kiểm thủ công và đường lỗi trong mã).
2. **Lõi có hai Khuyết Điểm:** cả hai đều lật, đều đập, đều được đọc tên (Task 1 `Plan_TwoFlaws_BothSlam`).
3. **Thiếu file âm thanh hay giọng:** cảnh chạy tiếp bằng thời gian tối thiểu, không lỗi (Task 1 `EveryTimedStepHasTimeToRead`, Task 3 dùng `Resources.Load` kiểm null).
4. **Rung và nhiễu quá mạnh làm không đọc được:** `ShakeMath` và hệ số hậu kỳ có trần (Task 1 `Shake_BoundedByAmplitude`, `Instability_AlwaysInRange`).
5. **Chết hoặc ngắt kết nối giữa cảnh:** trả lại cờ đóng băng, con trỏ, hủy sân khấu (Task 5, `OnDisable`).

---

### Task 1: Luật thuần (độ ổn định, rung, đóng dấu, luồng, lời, bảng tiếng)
**Files:**
- Create: `Scripts/Traits/Pure/Instability.cs`, `ShakeMath.cs`, `SlamPlan.cs`, `BirthSfx.cs`
- Modify: `Scripts/Traits/Pure/BirthFlow.cs`, `BirthScript.cs`, `FateRoll.cs` (gỡ `DealDeck`, `ShuffleOptions` và test của chúng)
- Test: `Tests/EditMode/Traits/Pure/InstabilityTests.cs`, `ShakeMathTests.cs`, `SlamPlanTests.cs`, `BirthSfxTests.cs`; sửa `BirthTests.cs`, `FateRollTests.cs`

**Interfaces:**
- Produces:
  - `enum BirthStepKind { Disconnect, Pull, Form, GiftIntro, Gift, Fate, DealCore, RevealCore, DealFlaw, RevealFlaw, Slam, Stabilize }`
  - `BirthStep` (như bản 2, bỏ `Interactive`); `BirthFlow` (như bản 2, bỏ `Pick`; `SkipIntro` nhảy tới `DealCore`).
  - `BirthScript.BuildSteps()` theo thứ tự: Disconnect, Pull, Form(`echo.born`), Form(`echo.body`), GiftIntro(`echo.gift`), Gift×6, Fate(`echo.fate`), DealCore(`echo.core`), RevealCore, DealFlaw(`echo.flaw`), RevealFlaw, Slam, Stabilize(`echo.stable`); `AllVoiceLines(cores, flaws)` (15 câu bước + 12 tên Lõi + 12 tên Khuyết Điểm = 39).
  - `Instability`: `float BaseFor(BirthStepKind kind, int giftIndex, float t01)`, `float Pulse(float age, float strength)`, `float Value(float baseValue, IReadOnlyList<(float at, float strength)> pulses, float now)` (kẹp 0..1).
  - `ShakeMath`: `Vector2 Offset(float time, float amplitude)` (|x|,|y| ≤ amplitude), `float Decay(float age, float duration)`.
  - `SlamPlan`: `struct Slam { string Text; SlamKind Kind; float Angle, Scale, Shake, At; }`, `enum SlamKind { Core, Flaw, Gift }`, `static List<Slam> Build(string coreName, TraitRarity rarity, IReadOnlyList<string> flawNames, IReadOnlyList<string> giftNames)`, `static float TotalSeconds(IReadOnlyList<Slam> slams)`.
  - `BirthSfx`: `readonly struct Sfx { string Id, Prompt; float Seconds; bool Loop; }`, `IReadOnlyList<Sfx> All`, `Sfx? Find(string id)`.
- [ ] **Step 1: Viết test:**
  - `InstabilityTests`: `Base_StartsHigh` (Disconnect t=0 ≥ 0,85); `Base_EndsAtZero` (Stabilize t=1 == 0); `Base_Stabilize_Decreases`; `Base_Gifts_Decrease` (gift 0 > gift 5); `Instability_AlwaysInRange` (mọi kind, giftIndex 0..5, t 0..1 và nhiều xung chồng: trong [0,1]); `Pulse_DecaysAndIsZeroBefore`; `Value_AddsPulseAndClamps`.
  - `ShakeMathTests`: `Zero_NoShake`; `Shake_BoundedByAmplitude`; `Shake_DeterministicAndMoves`; `Decay_EndsAtZero` (Decay(0,d)=1, Decay(d,d)=0, Decay(2d,d)=0).
  - `SlamPlanTests`: `Order_CoreFlawGifts`; `Angles_AsDesigned` (Lõi −7, Khuyết Điểm đầu +6, hai Khuyết Điểm đảo dấu, Ân Điển xen kẽ dấu, mỗi |góc| trong [4,9]); `Mythic_ShakesDouble`; `Shake_GiftsSmallerThanCore`; `Plan_TwoFlaws_BothSlam`; `Times_Increase` (At tăng dần); `Total_CoversLastSlam`.
  - `BirthSfxTests`: ids duy nhất; `Seconds` trong [0,5; 22]; có đủ các tên dùng bởi luồng (`type_tick`, `glitch_burst`, `alarm`, `hum_loop`, `whoosh`, `card_deal`, `card_flip`, `core_common`, `core_rare`, `core_mythic`, `flaw_reveal`, `slam_big`, `slam_small`, `stable_chime`, `collapse`, `pull`, và sáu `gift_*`); `hum_loop` là `Loop`; test xuất `ExportBirthSfx` (biến môi trường `EXPORT_BIRTH_SFX`) ghi `id<TAB>giây<TAB>prompt`.
  - `BirthTests` (sửa): thứ tự các bước đúng như trên; sáu Gift bước theo `Gifts`; mọi bước giờ có `MinSeconds ≥ 3`; luồng không còn bước chọn; `AllVoiceLines` có 39 mục, id duy nhất.
- [ ] **Step 2:** Chạy `tools/cstest/run.sh`, kỳ vọng FAIL.
- [ ] **Step 3:** Cài đặt.
  - `ShakeMath.Offset`: tổng vài sin tần số khác nhau, chuẩn hóa về [−1, 1] rồi nhân biên độ.
  - `SlamPlan.Build`: gán thời điểm `At` tích lũy: Lõi 0,5 s; mỗi Khuyết Điểm cách 2,2 s; mỗi Ân Điển cách 0,6 s (đoạn đầu cách 1,8 s).
  - Gỡ `DealDeck`, `ShuffleOptions` khỏi `FateRoll` cùng test.
- [ ] **Step 4:** `tools/cstest/run.sh` PASS.
- [ ] **Step 5:** Commit `feat(birth3): instability, shake, slam plan, flow and script`.

### Task 2: Âm thanh và lời mới (tạo file)
**Files:**
- Create: `tools/voice/generate_birth_sfx.sh`; các file `Assets/_Project/Resources/Sfx/Birth/*.mp3`
- Modify: `tools/voice/generate_echo_voice.sh` (không đổi nếu đã đọc từ `BirthScript`); các file giọng `Resources/Voice/Echo/`
- [ ] **Step 1:** `generate_birth_sfx.sh`: xuất bảng bằng `EXPORT_BIRTH_SFX=… dotnet test --filter ExportBirthSfx`, gọi `POST /v1/sound-generation` (`text`, `duration_seconds`, `prompt_influence 0.5`, `loop` cho tiếng lặp), chuẩn hóa âm lượng bằng ffmpeg (`loudnorm`), ghi `Resources/Sfx/Birth/<id>.mp3`. Đọc dòng bằng `read -u 3` và `ffmpeg -nostdin` (tránh lỗi ăn mất đầu vào).
- [ ] **Step 2:** Chạy tạo toàn bộ tiếng; kiểm số file bằng số mục bảng; kiểm độ dài bằng `ffprobe`.
- [ ] **Step 3:** Chạy `tools/voice/generate_echo_voice.sh` để tạo lời mới (`echo.warn`, `echo.pull`, `echo.stable`, `echo.core`, `echo.flaw` đã viết lại); bỏ file `echo.done`.
- [ ] **Step 4:** Commit `feat(birth3): sound effects and new voice lines` (đẩy lên cần máy chủ LFS đã mở).

### Task 3: Sân khấu, hậu kỳ, âm thanh (Unity)
**Files:**
- Create: `Scripts/Traits/BirthStage.cs`, `BirthGlitch.cs`, `BirthAudio.cs`; sửa `BirthVoice.cs` (dùng chung bộ phát)
**Interfaces:**
- Consumes: `Instability`, `ShakeMath` (Task 1).
- Produces: `BirthStage` (`static BirthStage Create()` trả null nếu lỗi; `Camera Cam`; `RectTransform Canvas`; `Transform Root`; `void Destroy()`), `BirthGlitch.Apply(float instability, float shake, float time)`, `BirthAudio.Play(string sfxId)`, `BirthAudio.SetAmbience(bool)`.
- [ ] **Step 1:** `BirthStage.Create`: gốc ở y=20000; camera (xóa màu đặc, `depth` 100, `far` 300, `cullingMask` mọi layer, `UniversalAdditionalCameraData.renderPostProcessing = true`, `volumeLayerMask = 1<<31`); `Volume` toàn cục ở layer 31 với Bloom, ChromaticAberration, LensDistortion, FilmGrain, Vignette; Canvas World Space cỡ 1920×1080 trước camera; bọc `try/catch`, lỗi thì `Destroy()` và trả null.
- [ ] **Step 2:** `BirthGlitch.Apply`: hệ số (có trần) từ độ ổn định cho từng override; rung bằng cách dịch camera theo `ShakeMath.Offset`.
- [ ] **Step 3:** `BirthAudio`: tải `Resources/Sfx/Birth/<id>` (null thì bỏ qua), phát 2D; âm nền lặp; hạ âm nền khi `BirthVoice.Busy`.
- [ ] **Step 4:** Commit `feat(birth3): the separate stage, post-processing and audio`.

### Task 4: Khung vỡ, chữ gõ, bài, hạt
**Files:**
- Create: `Scripts/Traits/BirthFrames.cs`, `BirthCards.cs`, `BirthFx.cs`
- [ ] **Step 1:** `BirthFrames.Window(...)`: tạo đối tượng UGUI cho một cửa sổ gồm các đoạn viền rời (cạnh, góc vát, bracket), thân kính mờ, đường quét; mỗi frame cập nhật độ lệch, nhấp nháy, nghiêng theo độ ổn định.
- [ ] **Step 2:** Chữ gõ từng ký tự, kèm `type_tick`; khi độ ổn định cao thay ngẫu nhiên vài ký tự bằng ký tự lạ rồi sửa.
- [ ] **Step 3:** `BirthCards`: tạo bài (lưng, mặt) bằng UGUI; chia bài bay hỗn loạn, xáo, tự lật; mặt bài (Lõi theo độ hiếm, Khuyết Điểm đỏ nứt).
- [ ] **Step 4:** `BirthFx`: ParticleSystem tạo bằng mã (nổ sáng, mảnh vỡ, hạt trôi), texture đĩa mờ tạo bằng mã, vật liệu `Universal Render Pipeline/Particles/Unlit` cộng.
- [ ] **Step 5:** Commit `feat(birth3): broken frames, cards and particles`.

### Task 5: Điều phối cảnh, đóng dấu, ổn định (bỏ IMGUI cũ)
**Files:**
- Modify: `Scripts/Traits/BirthScene.cs` (viết lại thành bộ điều phối); Create: `Scripts/Traits/BirthSlam.cs`, `BirthGifts.cs` (chặng 1: tấm giữ chỗ)
- Delete: `Scripts/Traits/BirthUi.cs`
- [ ] **Step 1:** `BirthScene`: giữ quy tắc chạy một lần, đóng băng, trả cờ; rút số mệnh bằng `FateRoll.Roll` trước; dựng sân khấu (lỗi thì giao số mệnh ngay); điều khiển từng bước theo `BirthFlow` và `Instability`; cú đập xung độ ổn định.
- [ ] **Step 2:** `BirthSlam`: theo `SlamPlan`, chữ bay từ 3× vào, đập, nảy; rung camera, chớp trắng, mảnh vỡ, `slam_big/slam_small`, đọc tên bằng clip `core.<id>`, `flaw.<id>`, `gift.*`.
- [ ] **Step 3:** Đoạn Ổn định: nhiễu tan, `stable_chime`, lời `echo.stable`.
- [ ] **Step 4:** Grep toàn dự án: không còn tham chiếu `BirthUi`, `BirthStepKind.Pick*`, `DealDeck`, `ShuffleOptions`. `tools/cstest/run.sh` PASS.
- [ ] **Step 5: Kiểm thủ công trong Unity (chủ dự án):** Console sạch; thấy cảnh mất kết nối, không thấy hang; khung vỡ và liền lại dần; bài tự lật; tên đập kèm rung; kết thúc ổn định rồi điều khiển được slime; chết rồi hồi sinh không chạy lại; cảnh không kẹt khi bấm Space nhiều lần.
- [ ] **Step 6: Commit** `feat(birth3): the collapsing-connection scene`.

### Task 6 (chặng 2): Sáu cảnh Ân Điển
**Files:** Modify `Scripts/Traits/BirthGifts.cs`
- [ ] **Step 1:** Mỗi Ân Điển một cảnh dựng từ hình cơ bản UGUI và hạt (mô tả ở spec 5.5), mỗi cái dài khoảng 7 giây, có `gift_*` riêng; Phân Thân ghi "(sắp có)".
- [ ] **Step 2:** Kiểm thủ công từng cảnh; commit `feat(birth3): six gift scenes`.

### Task 7: Tài liệu
- [ ] Cập nhật `docs/PROJECT_STATUS.md` (mục 38), `docs/art/echo-voice-lines.md` (39 câu, tiếng hiệu ứng, script tạo tiếng), checklist kiểm; commit `docs: status for the collapsing-connection scene`.

## Sau kế hoạch này
Chỗ cắm video Veo (spec 5.9), Dị Tượng, hiệu ứng thật cho Phân Thân và các Lõi còn "chưa có hiệu ứng".
