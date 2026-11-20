# Cáo Gió: Giáo Gió (3 mũi giáo gió xoay trên đuôi rồi phóng đi): kế hoạch thực hiện

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Thay Băng Rơi và Gió Hú của Cáo Gió bằng chiêu Giáo Gió: ba mũi giáo gió xoay vòng liên tục trên các đuôi (đó là dấu hiệu báo), rồi phóng lần lượt; giáo xé gió có tiếng, sát thương cao và hất lùi.

**Architecture:** Luật thuần (`SpearRules` quỹ đạo và nhịp phóng, `WhooshSynth` tiếng xé gió tổng hợp, bộ chiêu mới) có test ngoài Unity; mã Unity: giáo bay thẳng (`MonsterSpear`), bộ giáo xoay trong `CreatureCombat`, tiếng phát bằng `AudioClip` tạo từ mẫu tổng hợp (không cần tệp âm thanh).

**Tech Stack:** Unity 6.6 URP, C#, NUnit; `tools/cstest/run.sh`, kiểm cú pháp: `tools/syntaxcheck` chạy trực tiếp (xem lỗi hiếm).

**Spec:** `docs/superpowers/specs/2026-11-20-monster-combat-movesets-design.md` (chủ dự án yêu cầu đổi chiêu Cáo Gió, 2026-11-20).

## Global Constraints

- Văn bản người chơi, tài liệu, báo cáo bằng tiếng Việt; mã và chú thích mã bằng tiếng Anh. Số tạm, chỉnh được.
- Giáo Gió: ba mũi giáo, gồng 1,3 s (giáo xoay), ra đòn 0,9 s (phóng lần lượt cách nhau 0,3 s), kiệt sức 1,6 s, hồi chiêu 6 s, tầm 4 đến 16 m, tốc độ giáo 26 m/s, sát thương 26 mỗi mũi, hất lùi 9 m/s trong 0,35 s, bán kính trúng 0,7 m. Mỗi mũi nhắm vào vị trí người chơi lúc nó được phóng. Giáo dừng khi chạm vách.
- Cáo Gió còn Vồ Cắn (như cũ) và Gọi Bầy (dưới 33% máu, như cũ); Băng Rơi và Gió Hú bị bỏ khỏi bộ chiêu (mã hình chiêu `IceDrop`, `Gust` giữ lại, không dùng).
- Tiếng xé gió tổng hợp bằng mã (nhiễu lọc quét tần số), một tiếng khi giáo xuất hiện và quay, một tiếng lớn mỗi lần phóng.
- Không phá các lát trước. Commit kết thúc bằng hai dòng ghi công của phiên. Không tạo PR.
- Mỗi task: `tools/cstest/run.sh` xanh và chạy công cụ kiểm cú pháp trực tiếp (không lọc) trên file đổi, đọc các mã hiếm (CS0019, CS0037, CS0165 nêu kiểu của dự án).

## Review Focus

1. Cáo bị choáng, chết hoặc mất mục tiêu giữa lúc giáo xoay: các giáo chưa phóng bị dọn hết, không để giáo lơ lửng (Task 3).
2. Người chơi chết hoặc đứng sát cáo (dưới 4 m) khi đang gồng: không lỗi, giáo vẫn phóng theo vị trí hiện tại hoặc bị hủy sạch (Task 3).
3. Giáo bay trúng vách: biến mất, không xuyên; giáo trượt: hết đường bay thì tự hủy (Task 3).
4. Nhiều Cáo Gió cùng lúc: mỗi con có bộ giáo riêng, không lẫn (Task 3).

---

### Task 1: Quỹ đạo giáo và tiếng xé gió (thuần)

**Files:**
- Create: `Assets/_Project/Scripts/Combat/Pure/SpearRules.cs`, `Assets/_Project/Scripts/Combat/Pure/WhooshSynth.cs`
- Test: `Assets/_Project/Tests/EditMode/Combat/Pure/SpearRulesTests.cs`

**Interfaces:**
- Produces: `static (float x, float y, float z) SpearRules.OrbitOffset(int index, int count, float time, float radius, float degPerSecond, float height)`; `static float SpearRules.OrbitHeadingDeg(int index, int count, float time, float degPerSecond)` (hướng mũi giáo tiếp tuyến quỹ đạo); `static float SpearRules.LaunchTime(int index, float interval)`; `static float[] WhooshSynth.Make(int seed, int sampleRate, float seconds, float startHz, float endHz)` (mẫu đơn kênh, đỉnh 0,85).

- [ ] **Step 1: Viết test**: `Orbit_ThreeSpears_Are120DegreesApart_OnOneCircle` (bán kính cố định 0,9 ± 1e-3 theo mặt phẳng x-z, góc giữa các mũi 120 ± 0,1), `Orbit_Turns_AtTheGivenSpeed` (sau 0,5 s với 360 độ mỗi giây góc tăng 180 ± 0,1), `Orbit_Height_StaysNearTheGivenHeight` (độ lệch y so với `height` không quá 0,15 ở mọi thời điểm), `Heading_IsTangent` (hướng bằng góc vị trí cộng 90), `LaunchTime_ByIndex` (0, 0,3, 0,6), `Whoosh_Length_Peak_NoNaN` (độ dài `rate × giây`, đỉnh tuyệt đối trong 0,8 đến 0,86, không NaN), `Whoosh_Deterministic_AndSeedChangesIt`, `Whoosh_FadesInAndOut` (RMS 5% đầu và 5% cuối nhỏ hơn một phần ba RMS của 20% giữa).
- [ ] **Step 2: Chạy, thấy đỏ. Step 3: Cài.** `WhooshSynth`: nhiễu LCG theo `seed`, bộ lọc dải thông biến trạng thái (Chamberlin) với tần số trung tâm quét tuyến tính từ `startHz` tới `endHz`, bao hình `sin²(π·t)`, chuẩn hóa đỉnh 0,85.
- [ ] **Step 4: Chạy lại, xanh. Commit.**

### Task 2: Bộ chiêu Cáo Gió mới (thuần)

**Files:**
- Modify: `Assets/_Project/Scripts/Combat/Pure/MoveScheduler.cs` (hình chiêu `Spears`, trường `SpearCount`, `SpearSpeed`, `LaunchInterval`), `MovesetSeed.cs`
- Test: `Assets/_Project/Tests/EditMode/Combat/Pure/MovesetSeedTests.cs`

**Interfaces:**
- Consumes: Task 1.
- Produces: `MoveShape.Spears`; `MovesetSeed.For("caogio")` gồm `Vồ Cắn`, `Giáo Gió`, `Gọi Bầy`; `PoseOf`: `Spears` cho `Cast`.

- [ ] **Step 1: Sửa và thêm test**: thay hai test cũ của Cáo Gió (`Slice2_Seed_ByAnyName`, `Slice2_Numbers_MatchTheSpec`, phần cáo) cho khớp bộ chiêu mới; `Spears_Numbers_MatchTheSpec` (mọi số trong Global Constraints, gồm `SpearCount 3`, `SpearSpeed 26`, `LaunchInterval 0.3`, `Force 9`, `Damage 26`, `Windup 1.3`, `Active 0.9`, `Recover 1.6`, `Cooldown 6`, `MinRange 4`, `MaxRange 16`); `Spears_AllLaunchesFitInTheActiveTime` (`(SpearCount - 1) × LaunchInterval <= Active`); `PoseOf_Spears_IsCast`; `Fox_HasNoIceDropOrGust`.
- [ ] **Step 2: Chạy, đỏ. Step 3: Cài. Step 4: Chạy lại, xanh. Commit.**

### Task 3: Giáo xoay và giáo bay, tiếng (Unity)

**Files:**
- Create: `Assets/_Project/Scripts/Combat/MonsterSpear.cs`, `Assets/_Project/Scripts/Combat/ProceduralSfx.cs`
- Modify: `Assets/_Project/Scripts/Combat/CreatureCombat.cs`

**Interfaces:**
- Consumes: `SpearRules`, `WhooshSynth`, `MoveDef` mới (Task 1, 2), `WindPush`, `Impulse`.
- Produces: `MonsterSpear.Launch(Transform visual, Vector3 aimAt, float speed, float damage, float force)`; `ProceduralSfx.Play(ProceduralSfx.Kind kind, Vector3 position, float volume = 1f)` với `enum Kind { SpearLaunch, SpearOrbit }`.

- [ ] **Step 1: Mã Unity, không có test chạy ngoài Unity; luật nó dựa vào đã có test ở Task 1 và 2.**
- [ ] **Step 2: `ProceduralSfx`:** tạo `AudioClip` một lần từ `WhooshSynth.Make` (phóng: 0,55 s quét 3600 xuống 600 Hz; xoay: 0,4 s quét 900 lên 1500 Hz, nhỏ hơn) và phát bằng `AudioSource.PlayClipAtPoint`.
- [ ] **Step 3: `MonsterSpear`:** bay thẳng theo hướng cố định lúc phóng với tốc độ cho trước, mỗi bước kiểm vách bằng tia và người chơi cục bộ trong 0,7 m (cộng 0,3 m độ cao tâm); trúng thì `Damage`, `WindPush.Apply(slime, hướng × force, 0,35 s)` và dọn; chạm vách hoặc bay quá 24 m hoặc 1,5 s thì dọn. Hình giáo: viên nang mảnh sáng (`SkillFx.Gel` xanh trắng) có `TrailRenderer`.
- [ ] **Step 4: `CreatureCombat`:** hình `Spears`: vào gồng thì tạo gốc xoay phía sau và trên lưng (nơi đuôi), 3 giáo xoay theo `SpearRules.OrbitOffset` (bán kính theo cỡ cáo, 540 độ mỗi giây) và quay mũi theo `OrbitHeadingDeg`, phát tiếng xoay lúc đầu và mỗi 0,45 s; ở `Active` phóng giáo thứ `k` khi thời gian pha đạt `LaunchTime(k, LaunchInterval)`, nhắm vị trí người chơi lúc đó, phát tiếng phóng; dọn mọi giáo chưa phóng khi vào `Recover`, khi bị choáng hoặc chết (`HostPaused`), và `OnDisable`.
- [ ] **Step 5: Kiểm cú pháp trực tiếp, `cstest` xanh. Commit.**

### Task 4: Tài liệu, giao bên làm hang, gộp

**Files:**
- Modify: `docs/TEST_CHECKLIST_UNITY.md`, `docs/PROJECT_STATUS.md`, `docs/superpowers/handoff/2026-11-20-monster-tell-poses-for-cave-team.md`, `docs/superpowers/specs/2026-11-20-monster-combat-movesets-design.md`

- [ ] **Step 1: Cập nhật mục H7 của checklist (bỏ Băng Rơi và Gió Hú, thêm Giáo Gió), mục 59 trong PROJECT_STATUS (cũng ghi hai sửa lỗi: kẻ săn săn mồi khi hơi đói và cảnh báo vận tốc kinematic), spec (chiêu cáo thay đổi), bản giao bên làm hang (Cáo Gió cần ba đuôi để giáo xoay trên đó; clip `Tell_Cast` là xoay ba đuôi và giáo; trigger `Strike_Cast` lúc phóng).**
- [ ] **Step 2: Gộp `claude/movement` vào `claude/unity-test-maze`, chạy `cstest` và công cụ kiểm cú pháp, đẩy cả hai nhánh. Báo cáo bằng tiếng Việt, nói rõ phần chưa kiểm trong Unity.**
