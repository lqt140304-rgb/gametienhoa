# Cáo Gió trông giống trùm hơn: kích cỡ thật khi thử, trang phục phụ kiện, màu Rasengan: kế hoạch thực hiện

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Giáo Gió màu xanh da trời nhạt kiểu Rasengan; Cáo Gió to đúng cỡ trùm khi thử trong menu thần; cáo mang trang phục và phụ kiện (áo choàng gió, vương miện, khăn, bùa ngực) dựng bằng mã.

**Architecture:** Hình học thuần (`RegaliaMeshes`: vòng, vương miện, lưới áo choàng, sóng gió) có test ngoài Unity; `LordRegalia` (Unity) dựng lưới và vật liệu rồi gắn vào mô hình cáo, đặt chỗ theo hộp bao của mô hình và chỉnh được trong menu thần. `CreatureTuning` gắn nó cho trùm có bộ trang trí.

**Tech Stack:** Unity 6.6 URP, C#, NUnit; `tools/cstest/run.sh`, công cụ kiểm cú pháp chạy trực tiếp (đọc mã hiếm).

**Spec:** `docs/superpowers/specs/2026-11-20-monster-combat-movesets-design.md` (chủ dự án yêu cầu, 2026-11-20).

## Global Constraints

- Văn bản người chơi, tài liệu, báo cáo bằng tiếng Việt; mã và chú thích mã bằng tiếng Anh.
- Màu giáo: xanh da trời nhạt, RGB `(0.58, 0.8, 1.0)`, viền sáng mạnh.
- Cáo Gió mang: áo choàng gió xanh nhạt viền vàng bay phấp phới, vương miện vàng 5 mũi có viên ngọc xanh, khăn vòng cổ xanh nhạt, bùa ngực là quả cầu xanh nhạt phát sáng.
- Vị trí đặt theo hộp bao của mô hình (hướng trước là +Z cục bộ): đầu ở phía trước và trên, cổ sau đầu, áo choàng từ vai rủ về sau; số tỉ lệ chỉnh được trong menu thần (tab Combat).
- Mô hình thử trong menu thần phải nhận cỡ và độ khó như quái trong hang (`CreatureTuning.Apply` với hang của loài).
- Không phá các lát trước. Commit kết thúc bằng hai dòng ghi công của phiên. Không tạo PR.
- Mỗi task: `tools/cstest/run.sh` xanh và công cụ kiểm cú pháp trực tiếp trên file đổi.

## Review Focus

1. Mô hình không có `Renderer` hoặc hộp bao rỗng: không dựng trang phục, không lỗi (Task 2).
2. Cáo bị hủy hoặc bị cất lại: trang phục (con của mô hình) biến mất cùng, lưới và vật liệu động được dọn (Task 2).
3. Gắn trang phục hai lần cho cùng một con (Apply gọi lại): chỉ một bộ (Task 2).
4. Lưới áo choàng đổi mỗi khung hình: không tạo rác mỗi khung (tái dùng mảng đỉnh) (Task 2).

---

### Task 1: Hình học trang phục (thuần)

**Files:**
- Create: `Assets/_Project/Scripts/Combat/Pure/RegaliaMeshes.cs`
- Test: `Assets/_Project/Tests/EditMode/Combat/Pure/RegaliaMeshesTests.cs`

**Interfaces:**
- Produces: `sealed class MeshData { float[] Vertices; int[] Triangles; int VertexCount }` (đỉnh xếp x, y, z liên tiếp); `static MeshData RegaliaMeshes.Torus(float major, float minor, int majorSegments, int minorSegments)` (vòng quanh trục Y); `static MeshData RegaliaMeshes.Crown(float radius, float height, int spikes)` (các mũi chóp bốn cạnh dựng trên vòng bán kính `radius` trong mặt x-z, chĩa lên +Y); `static MeshData RegaliaMeshes.CapeGrid(float width, float length, int cols, int rows)` (lưới phẳng trong mặt x-z: x từ `-width/2` đến `width/2`, z từ 0 (mép gắn vai) xuống `-length` (gấu), y = 0); `static float RegaliaMeshes.CapeWave(float u, float v, float time)` (độ lệch y theo gió, u ngang 0..1, v từ mép vai 0 tới gấu 1; bằng 0 tại v = 0 và không vượt 1 về trị tuyệt đối).

- [ ] **Step 1: Viết test**: `Torus_CountsAndRadius` (số đỉnh `majorSegments × minorSegments`, số chỉ số tam giác `majorSegments × minorSegments × 6`, mọi chỉ số trong khoảng, khoảng cách từ trục Y của mọi đỉnh nằm trong `[major - minor, major + minor]`), `Crown_Spikes` (5 mũi: mỗi mũi 5 đỉnh, 6 tam giác; đỉnh cao nhất bằng `height`; đỉnh đáy cách trục `radius`), `CapeGrid_Layout` (`(cols+1) × (rows+1)` đỉnh, `cols × rows × 6` chỉ số, hộp bao x trong `±width/2`, z trong `[-length, 0]`, mọi y bằng 0), `CapeWave_AnchoredAndBounded` (bằng 0 ở `v = 0` với mọi `u` và thời gian; `|giá trị| <= 1` trên lưới mẫu dày), `CapeWave_MovesWithTime` (cùng `(u, v)` ở hai thời điểm khác nhau cho giá trị khác nhau với `v = 1`), `CapeWave_GrowsTowardsTheHem` (biên độ lớn nhất theo thời gian ở `v = 1` lớn hơn ở `v = 0.3`).
- [ ] **Step 2: Chạy `tools/cstest/run.sh`, thấy đỏ. Step 3: Cài. Step 4: Chạy lại, xanh. Commit.**

### Task 2: Trang phục trong Unity, cỡ thật khi thử, màu giáo (Unity)

**Files:**
- Create: `Assets/_Project/Scripts/Combat/LordRegalia.cs`
- Modify: `Assets/_Project/Scripts/Creatures/CreatureTuning.cs`, `Assets/_Project/Scripts/Combat/CreatureCombat.cs` (màu giáo), `Assets/_Project/Scripts/Dev/GodMenu.Tests.cs` (cỡ thật khi tạo prefab; thanh chỉnh trang phục)

**Interfaces:**
- Consumes: `RegaliaMeshes`, `SkillFx.Gel`.
- Produces: `LordRegalia : MonoBehaviour` với `static bool Has(string speciesIdOrName)` (loài có bộ trang trí: Cáo Gió), `static LordRegalia Attach(GameObject creature)`, và các hệ số tĩnh chỉnh được `HeadHeight`, `HeadForward`, `NeckForward`, `Size`; `CreatureTuning.Apply` gọi `LordRegalia.Attach` cho trùm có bộ trang trí.

- [ ] **Step 1: Mã Unity, không có test chạy ngoài Unity; hình học đã có test ở Task 1.**
- [ ] **Step 2: `LordRegalia`:** lấy hộp bao của mọi `Renderer` của mô hình trong hệ tọa độ cục bộ của cây sinh vật; không có `Renderer` thì thôi. Dựng bốn món con của nút mô hình: **vương miện** (vàng, 5 mũi, viên ngọc xanh nhạt ở giữa trán), **khăn** (vòng xanh nhạt quanh cổ), **áo choàng** (lưới `CapeGrid`, xanh nhạt, viền vàng ở gấu bằng một dải mỏng; cập nhật đỉnh mỗi khung hình theo `CapeWave`, tái dùng mảng), **bùa ngực** (quả cầu xanh nhạt phát sáng và một vòng nhỏ xoay quanh). Vật liệu bằng `SkillFx.Gel` (xanh nhạt, vàng). Chỉ gắn một bộ mỗi con (đánh dấu). Dọn lưới và vật liệu động khi hủy.
- [ ] **Step 3: Màu giáo** trong `CreatureCombat.MakeSpears`: `SkillFx.Gel(new Color(0.58f, 0.8f, 1f), 1f, 0.9f, 3.4f)`.
- [ ] **Step 4: Menu thần:** `SpawnPrefab` gọi `CreatureTuning.Apply(go, cave)` với `cave = CaveEcologySeed.CaveOfSpecies(CaveEcologySeed.IdByName(displayName))`; tab Combat có bốn thanh chỉnh `HeadHeight`, `HeadForward`, `NeckForward`, `Size` của trang phục (áp cho cáo tạo sau khi đổi).
- [ ] **Step 5: Kiểm cú pháp trực tiếp, `cstest` xanh. Commit.**

### Task 3: Tài liệu, giao bên làm hang, gộp

**Files:**
- Modify: `docs/TEST_CHECKLIST_UNITY.md`, `docs/PROJECT_STATUS.md`, `docs/superpowers/handoff/2026-11-20-monster-tell-poses-for-cave-team.md`

- [ ] **Step 1: Mục kiểm thử (cỡ thật khi thử, màu giáo, trang phục, thanh chỉnh), mục 60 trong PROJECT_STATUS, đề nghị bên làm hang: mô hình Cáo Gió có thể dựng sẵn mũ, áo, khăn trong Blender và khi đó thêm khóa `caogio` vào danh sách tắt trang phục bằng mã.** Thêm `LordRegalia.DisabledFor` (tập khóa loài) nếu cần: không làm ở lần này (không ai cần).
- [ ] **Step 2: Gộp `claude/movement` vào `claude/unity-test-maze`, chạy `cstest` và kiểm cú pháp, đẩy cả hai nhánh. Báo cáo bằng tiếng Việt, nói rõ phần chưa kiểm trong Unity.**
