# Trần vòm rễ và ánh sáng ba lớp hoàng hôn: Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ánh sáng hoàng hôn dịu ba lớp theo khu, và một trần vòm đa tầng nối liền với trụ rễ ở vách rìa, thay lớp rễ đan phẳng hiện nay.

**Architecture:** Giai đoạn 1 (ánh sáng): thêm ambient theo khu vào `ZoneAtmosphereMath`/`ZoneAtmosphere`, thành phần `LightByVision` cho đèn sinh học và đèn lối ra, hạ mặt trời. Giai đoạn 2 (vòm): `VaultLayout` (thuần: đường sườn rễ tỏa từ Đại sảnh, độ cao vòm nhiều khoang, khe sáng) + bộ đoạn rễ Blender + công cụ Editor `LakeVault` thay "Lake Canopy (Dressed)/Roof" và cụm rễ rìa. Giai đoạn 3: rễ rủ, túi phát quang, nhũ đá, điểm nhấn từng khu.

**Tech Stack:** Unity 6.6 URP 17, C#, Blender `bpy` 5.3, EditMode tests.

**Spec:** `docs/superpowers/specs/2026-11-18-vault-and-light-design.md` (mục 5, 8, 9; chủ dự án chọn cội ở Đại sảnh, hướng A).

## Global Constraints

- Mỗi giai đoạn là một PR riêng; giai đoạn 1 dùng được ngay không cần vòm.
- Không sửa `LakeCanopy.cs`, `LakeTreefolkCity.cs`, `LakeClassHouses.cs`, model nhân vật, sinh vật, cơ quan; công cụ mới **thay** mái bằng cách xoá theo tên rồi dựng bản mới, giữ tên `Roof` dưới `Lake Canopy (Dressed)` (để `LakeSun` bật đổ bóng) và chỉ xoá khi dựng được bản mới.
- Chưa có mắt: đen trắng, tối, chỉ sáng quanh người. Mọi đèn sinh học, đèn lối ra và ambient theo khu **nhân với thị lực** (`MonochromeVision.Sight`, mặc định 1 nếu thiếu).
- Ambient theo khu (hoàng hôn): 01 (0,14; 0,16; 0,15), 02 (0,08; 0,09; 0,08), 03 (0,10; 0,10; 0,08), 04 (0,12; 0,10; 0,14), 05 (0,15; 0,17; 0,17), 06 (0,20; 0,19; 0,16), Wild (0,07; 0,08; 0,08). Thị lực dở dang: nội suy từ đen lên ambient khu.
- Mặt trời (`DappledSunMath.Intensity`) 0,6, màu ấm hơn.
- Vòm: cao tối đa `LakeLayout.CeilingY` trừ 3 m, thấp nhất 14 m; khoang cao trên hồ và khu 04, khoang thấp trên khu 02 và 03; chênh cao giữa khoang 6–12 m; 9 lỗ sáng bán kính 8 m ở `ZoneLayout.LightShafts` và khoảng 40 khe hẹp 1–4 m; vòm không đè Đại sảnh (`ZoneLayout.InsideCave`).
- Sườn rễ: cội tại `ZoneLayout.RootFoot.Center`; 9–11 sườn chính bán kính 2,5–4 m thon dần, nhánh cấp hai 0,8–1,6 m, cấp ba 0,3–0,6 m; thêm 3 sườn phụ tỏa từ hồ (`ZoneLayout.LakeCenter`); chân chống ở vách rìa nối xuống đất giữa các tấm `Cliff_N`.
- Ngân sách: trần và rễ ≤ ~400.000 mặt hiển thị (có LOD), ≤ 40 đèn sinh học hoạt động cùng lúc (tắt xa 60 m).
- FBX cài từ `tools/blender/out/*.fbx.b64` (Git LFS bị chặn), menu `Reinstall Blender Props (overwrite)`. Không `git add .`; giữ dòng netcode trong `Packages/manifest.json`. Agent không có Unity: ảnh render Blender gửi chủ dự án duyệt trước khi ghép vào Unity.

## Review Focus

- Chưa có mắt: ambient, đèn sinh học, đèn lối ra đều tắt (không rò sáng); có mắt dở dang: không sáng hơn có mắt đầy đủ.
- Thay mái hai lần liên tiếp và Restore: không nhân đôi, trả về đúng bản cũ (kể cả rễ rìa).
- Sườn rễ không đè Đại sảnh người cây và không chắn lối vào phía bắc; khe sáng không nằm dưới nhà người cây.
- Vòm không phẳng: chênh cao thật (ít nhất 6 m giữa khoang cao nhất và thấp nhất) và không có bậc thẳng đứng.
- Mối nối giữa hai đoạn rễ liền mạch (bán kính và tâm miệng ống trùng khớp).

---

## Giai đoạn 1: Ánh sáng ba lớp

### Task 1: Ambient theo khu (thuần)

**Files:**
- Modify: `Assets/_Project/Scripts/World/AtmosphereProfile.cs`, `Assets/_Project/Scripts/World/ZoneAtmosphereMath.cs`
- Test: `Assets/_Project/Tests/EditMode/ZoneAtmosphereMathTests.cs`

**Interfaces:**
- Produces: `AtmosphereProfile.Ambient` (`Color`); `ZoneAtmosphereMath.Zones[i].Ambient` và `Wild.Ambient` theo Global Constraints; `Blend` nội suy `Ambient`; `static Color AmbientFor(AtmosphereProfile p, float sight)` = `Color.Lerp(Color.black, p.Ambient, Mathf.Clamp01(sight))`.

- [ ] **Step 1: Test (thất bại).** `Ambient_OrderMatchesTheBrief` (02 tối nhất, 06 sáng nhất theo độ sáng trung bình; thứ tự 02 < 03 < 04 < 01 < 05 < 06); `Ambient_AllAreDuskDim` (mọi thành phần ≤ 0,25 và ≥ 0,05); `Blend_InterpolatesAmbient` (hai khu 0,5/0,5); `AmbientFor_BlindIsBlackSightedIsZone` (sight 0 → đen, 1 → màu khu, 0,5 → giữa).
- [ ] **Step 2:** FAIL. **Step 3:** cài đặt. **Step 4:** pass. **Step 5:** commit `feat(light): ambient per zone`.

### Task 2: `ZoneAtmosphere` ghi ambient

**Files:** Modify `Assets/_Project/Scripts/World/ZoneAtmosphere.cs`

**Interfaces:** Consumes `ZoneAtmosphereMath.AmbientFor`, `MonochromeVision.Sight`.

- [ ] **Step 1:** Trong `LateUpdate`, khi không dưới nước: `RenderSettings.ambientMode = AmbientMode.Flat` và `RenderSettings.ambientLight = AmbientFor(Current, sight)` (cũng khi mù: đen). Thêm `public bool ControlsAmbient { get; set; } = true`.
- [ ] **Step 2:** Biên dịch sạch; commit `feat(light): zone ambient follows the camera and the player's sight`.

### Task 3: Đèn sinh học và đèn lối ra theo thị lực; mặt trời dịu

**Files:**
- Create: `Assets/_Project/Scripts/World/LightByVision.cs`
- Modify: `Assets/_Project/Editor/LakeLook.cs` (đèn nấm dùng `LightByVision`; thêm đèn sinh học theo khu và đèn lối ra), `Assets/_Project/Scripts/World/DappledSunMath.cs` (Intensity 0,6, SunColor ấm hơn), `Assets/_Project/Tests/EditMode/DappledSunMathTests.cs`

**Interfaces:**
- Produces: `LightByVision : MonoBehaviour` (`[RequireComponent(typeof(Light))]`): lưu `baseIntensity` ở `Awake`, `Update` đặt `light.intensity = baseIntensity * Mathf.Clamp01(sight)`, tắt hẳn khi cách camera > 60 m (`light.enabled`), `sight` lấy từ `MonochromeVision.Sight` (1 nếu không có).
- `LakeLook.SetupGlowLights` cũng đặt, theo khu: 01 túi xanh ngọc (0,3; 0,9; 0,85), 03 hổ phách (1; 0,65; 0,25), 04 tím (0,7; 0,4; 1), 05 trắng lạnh (0,8; 0,95; 1), 06 đỏ ấm yếu (1; 0,45; 0,3); vị trí quanh các điểm có `Nutrient` kiểu phát quang của khu (tối đa 6 đèn mỗi khu, range 6–9 m, intensity 0,8–1,4, không bóng); và một đèn ấm nhỏ (Spot hoặc Point, range 40 m, intensity 2) tại `ZoneLayout.NorthEntry` hướng vào trong.

- [ ] **Step 1:** Sửa test `Constants_AreSafe` cho Intensity ∈ [0,4; 1]; chạy: FAIL.
- [ ] **Step 2:** Cài đặt (`LightByVision` thêm vào mọi đèn do `LakeLook` tạo).
- [ ] **Step 3:** Biên dịch sạch; test pass; commit `feat(light): living lights fade with sight, dusk sun, exit light`.

### Task 4: Tài liệu giai đoạn 1

- [ ] **Step 1:** Ghi `docs/PROJECT_STATUS.md` mục 23 (ba lớp, bảng ambient, thứ tự sáng, checklist: Console sạch; chưa có mắt tối và đen trắng; có mắt: hoàng hôn dịu, khu 06 sáng nhất, khu 02 tối nhất; đèn sinh học có màu theo khu; mảng sáng mặt trời dịu hơn; chụp ảnh). Commit `docs(light): three-layer dusk lighting status`.

---

## Giai đoạn 2: Vòm và trụ rễ rìa

### Task 5: Bộ đoạn sườn rễ (Blender)

**Files:** Create `tools/blender/build_vault.py`, `tools/blender/test_vault.py`; Output FBX `Rib_Straight_0..2`, `Rib_Bend_0..2`, `Rib_Fork_0..1`, `Rib_Tip_0..1`, `Rib_Foot_0..1` (+ `.b64`); Modify `Assets/_Project/Editor/PropInstaller.cs` (danh sách tên).

**Interfaces:**
- Produces: mỗi đoạn dài 12 m dọc trục +X, miệng vào tại gốc toạ độ (bán kính `R0 = 1.0` đơn vị chuẩn) và miệng ra tại x = 12 (`R1 = 0.8`): lúc đặt trong Unity nhân tỉ lệ đều; `Rib_Bend` cong 25° quanh trục Z; `Rib_Fork` có một miệng vào và hai miệng ra (cấp hai thon 0,6 lần); `Rib_Tip` thon cụt về 0,2; `Rib_Foot` có chân loe xuống đất (z = 0) để áp vào vách; vỏ có rãnh dọc, bướu, mảng rêu (màu đỉnh, shader `RockVertexColor`), va chạm `<tên>_COL` thấp mặt (≤ 80); `build_vault.build_segment(kind: str, seed: int) -> tuple[object, object]`; hằng `SEGMENT_LENGTH = 12.0`.

- [ ] **Step 1: Test (thất bại).** Với mọi đoạn: bán kính miệng vào ≈ 1,0 ± 5% và miệng ra ≈ 0,8 ± 5% (đo vòng đỉnh gần x = 0 và x = 12); tâm hai miệng nằm trên trục (|y|, |z − tâm| ≤ 0,05); số mặt ≤ 3.500 (Fork ≤ 5.000); không có đỉnh z < −0,05 (trừ `Rib_Foot`); `_COL` ≤ 80 mặt.
- [ ] **Step 2:** FAIL. **Step 3:** cài đặt (ống nhiều vòng nhiễu bó rãnh, như khúc gỗ). **Step 4:** pass; xuất FBX và ảnh render; gửi chủ dự án **duyệt hình** trước Task 7.
- [ ] **Step 5:** Commit `feat(vault): Blender rib segments`.

### Task 6: `VaultLayout` (thuần)

**Files:** Create `Assets/_Project/Scripts/World/VaultLayout.cs`; Test `Assets/_Project/Tests/EditMode/VaultLayoutTests.cs`

**Interfaces:**
- Consumes: `ZoneLayout.RootFoot.Center` (cội), `ZoneLayout.LakeCenter`, `ZoneLayout.LightShafts`, `ZoneLayout.InsideCave(x, z, margin)`, `WorldScale.ZoneSize`.
- Produces: `struct RibPath { Vector3[] Points; float RadiusStart; float RadiusEnd; int Level; }`; `static RibPath[] Ribs(int seed, Func<float,float,float> ceilingY)` (sườn chính từ cội, nhánh cấp 2 và 3, 3 sườn phụ từ hồ; đường bám vòm: y = `VaultHeight - bán kính`, tránh Đại sảnh); `static float VaultHeight(float x, float z, float ceilingY)` (đa khoang, trong [14, ceilingY − 3]); `static Vector2[] Slots(int seed)` (≈ 40 vị trí khe) và `static float SlotHalfWidth(int i)` (0,5–2); `static Vector2[] FootPoints(RibPath[] ribs)` (điểm chạm vách, trong 8 m quanh |x| hoặc |z| = 243).

- [ ] **Step 1: Test (thất bại).** `Ribs_StartAtTheHub` (mọi sườn chính bắt đầu cách cội ≤ 4 m); `Ribs_CountAndRadii` (9–11 sườn chính, bán kính gốc 2,5–4, giảm theo cấp); `Ribs_NeverCrossTheHall` (không điểm nào có `InsideCave(x, z, 2)` trừ điểm cội); `Ribs_DoNotIntersectBeyondForks` (khoảng cách giữa hai sườn khác nhau ≥ 4 m trừ trong 10 m quanh chỗ chia nhánh); `VaultHeight_IsNotFlat` (quét lưới 20 m: max − min ≥ 6, và chênh giữa hai ô kề nhau ≤ 3 m: không bậc thẳng đứng); `VaultHeight_RespectsBounds`; `Slots_AreAwayFromHallAndRibs` (không trong `InsideCave(x, z, 4)`); `FootPoints_AreOnTheRim`; `Ribs_SameSeedSameResult`.
- [ ] **Step 2:** FAIL. **Step 3:** cài đặt. **Step 4:** pass. **Step 5:** commit `feat(vault): vault layout maths`.

### Task 7: `LakeVault` (Editor)

**Files:** Create `Assets/_Project/Editor/LakeVault.cs`; Modify `Assets/_Project/Editor/LakeRebuildAll.cs` (thêm bước sau Root Canopy; đổi bộ đếm).

**Interfaces:**
- Consumes: `VaultLayout`, FBX `Rib_*` (Task 5), `LakeLayout.CeilingY`, `LakeProps.EnsureCollision`, `PropInstaller.EnsureLandmarks` (mở rộng danh sách tên).
- Produces: `LakeVault.Dress()` (`[MenuItem("Evolve/Dress Lake: Root Vault")]`) và `LakeVault.Restore()` (`[MenuItem("Evolve/Restore Lake Canopy (undo)")]`): dựng mặt vòm theo từng ô 64 m (lưới ~4 m, độ cao `VaultHeight`, lỗ ở `LightShafts` và khe `Slots`, hai mức chi tiết trong LODGroup), tên `Roof x,z` dưới đối tượng cha `Lake Canopy (Dressed)`; ghép đoạn `Rib_*` theo từng `RibPath` (nối đầu-đuôi, tỉ lệ bán kính, nhánh bằng `Rib_Fork`, đuôi bằng `Rib_Tip`, chân bằng `Rib_Foot` tại `FootPoints`); thay "Roof" và cụm rễ rìa của canopy cũ: **sao lưu bằng cách tắt (`SetActive(false)`) chứ không xoá**, bản cũ hiện lại khi Restore; chỉ tắt bản cũ khi bản mới dựng xong (thiếu FBX thì cảnh báo và giữ nguyên); chạy lại thay bản vòm của chính nó; trụ rễ ở vách không đè các tấm `Cliff_N` và lối vào phía bắc.

- [ ] **Step 1:** Cài đặt vòm, ghép sườn, chân chống, Restore. **Step 2:** biên dịch sạch; test cũ pass; thử trong Unity do chủ dự án. **Step 3:** commit `feat(vault): Dress Lake: Root Vault replaces the flat root mat`.

### Task 8: Tài liệu giai đoạn 2

- [ ] **Step 1:** `docs/PROJECT_STATUS.md` mục 24 (vòm, sườn rễ, trụ rễ, Restore, checklist: ảnh trần từ dưới nhìn lên có nhiều tầng; sườn rễ chạy về phía Đại sảnh; trụ rễ chạm đất giữa vách; mảng sáng qua khe; FPS bằng `PerfOverlay`; Restore Lake Canopy trả về mái cũ). Commit `docs(vault): vault and rim roots status`.

---

## Giai đoạn 3: Rễ rủ, túi phát quang, nhũ đá, điểm nhấn

### Task 9: Vật thể treo trên trần (Blender) và đặt theo khu

**Files:** Modify `tools/blender/build_vault.py`, `test_vault.py`, `Assets/_Project/Editor/LakeVault.cs`, `PropInstaller.cs`

**Interfaces:** Produces FBX `VaultRoot_0..3` (rễ rủ dài 4 / 8 / 14 / 20 m), `VaultCurtain_0..1` (màn rễ mảnh), `VaultPod_0..2` (túi phát quang, tên mesh phát sáng `Giant Caps`-không: `Pod Glow_N`), `VaultStalactite_0..2` (nhũ đá vôi); `LakeVault.Dress` đặt theo khu: 01 túi xanh ngọc rủ xuống hồ; 02 rễ già dày; 03 nấm tầng bám sườn; 04 bọc bào tử tím; 05 nhũ đá; 06 thưa và khô; ba độ dài rễ rủ tạo tầng; tránh Đại sảnh, lỗ và khe sáng (≥ 11 m cho rễ rủ); không va chạm.

- [ ] **Step 1: Test (thất bại, Python).** Chiều dài rễ rủ theo tên (± 10%), số mặt ≤ 2.500, đỉnh trên ở z = 0, túi có đối tượng phát sáng tách riêng.
- [ ] **Step 2:** FAIL; **Step 3:** cài đặt, xuất FBX và ảnh render (chủ dự án duyệt); đặt trong `LakeVault`. **Step 4:** pass; commit `feat(vault): hanging roots, glow pods, stalactites`.

### Task 10: Tài liệu cuối và dọn

- [ ] **Step 1:** Cập nhật `PROJECT_STATUS` mục 25 (vật thể treo, checklist), `ROADMAP`; báo phiên trang trí rằng `Dress Lake: Root Vault` thay mái cũ; commit `docs(vault): hanging props and handoff`.

## Tự rà soát

- Phủ spec: ba lớp ánh sáng (T1–T3), thứ tự sáng theo khu (T1), theo thị lực (T2–T3), vòm không phẳng và khe sáng (T6–T7), liên kết rễ rìa (T6 `FootPoints`, T7), điểm nhấn theo khu (T9). Cội ở Đại sảnh và sườn phụ từ hồ (T6).
- Chữ ký nhất quán: `AtmosphereProfile.Ambient`, `AmbientFor`, `LightByVision`, `VaultLayout.Ribs/VaultHeight/Slots/FootPoints`, `LakeVault.Dress/Restore`, `build_vault.build_segment`.
- Tỉ lệ: Task 5, 6, 7 là phần lớn việc; Task 7 chỉ kiểm được trong Unity.
