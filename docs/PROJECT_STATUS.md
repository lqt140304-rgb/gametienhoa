# PROJECT EVOLVE: Tình hình dự án (bàn giao)

> Cập nhật: 2026-11-17, sau pull request #27 và HUD mới của Codex (commit `bdccede`). Dành cho một phiên làm việc mới: nắm nhanh dự án đang ở đâu, cách làm việc với chủ dự án, và việc tiếp theo.
> Repo: `lqt140304-rgb/gametienhoa`, nhánh chính `main`.

## 0. Việc đầu tiên khi bắt đầu phiên mới

1. Đọc file này, rồi `docs/GAME_DESIGN.md` (nguồn thiết kế chính).
2. Chạy `git fetch` rồi xem `git log origin/main` và `git branch -r`. **Chủ dự án và các phiên khác cũng đang sửa repo.** Hiện có các nhánh `docs/bestiary` (một phiên khác đang soạn `docs/BESTIARY.md`), `claude/tender-lamport-kegpkf` và hai nhánh cũ đã merge. Đừng đụng vào nhánh của phiên khác.
   - **Về `docs/bestiary`:** nhánh có lúc lỡ kéo theo thay đổi scene cũ (commit `eabe0fa`), nhưng đã được gỡ bằng commit `e728532`. Đã kiểm tra (2026-11-17): so với `main`, nhánh chỉ thêm `docs/BESTIARY.md` và gộp được sạch, không xung đột. Vẫn nhắc phiên soạn bestiary chỉ `git add docs/BESTIARY.md`, không dùng `git add .`.
3. Làm việc trên **nhánh riêng** của phiên (không đẩy thẳng lên `main`), xong thì mở pull request. Chỉ merge khi chủ dự án bảo ("merge pull request #N").

## 1. Dự án là gì

Game **sinh tồn tiến hóa 3D low poly, co-op 4–8 người**, làm bằng **Unity 6.6 (`6000.6.9f1`) + URP**, C#, model bằng Blender. Người chơi bắt đầu là một slime nguyên sinh: không mắt, không chân, nhìn đen trắng, sống trong bóng tối. Họ tiến hóa bằng cách **săn sinh vật để lấy cơ quan** rồi gắn lên cơ thể. Không có cây tiến hóa cố định.

**Tài liệu thiết kế** (đọc trước khi làm tính năng lớn):

| File | Nội dung | Ghi chú |
|---|---|---|
| `docs/GAME_DESIGN.md` | GDD, có đánh dấu **[ĐÃ CHỐT]** và **[ĐỀ XUẤT]** | **Nguồn thiết kế chính.** Chủ dự án chốt; mọi thay đổi thiết kế phải ghi vào đây. Mục 4.3 (rơi cơ quan), 4.4 (EXP và cấp cơ quan), 6.1 (mạng, chết, cứu) rất quan trọng |
| `docs/BIOLOGICAL_INTEGRITY.md` | Hệ thống hư hỏng và tái tạo cơ quan (BIS v1.1) | Nguyên tắc đã chốt, công thức còn là đề xuất |
| `docs/GDD.md` | Bản thiết kế cũ | Cũ hơn `GAME_DESIGN.md` |
| `docs/ROADMAP.md` | Lộ trình 0.1 → 0.5 và checklist | |
| `docs/PROTOTYPE_01_SETUP.md` | Cách dựng scene, bảng phím, cách chỉnh thông số | |
| `docs/BESTIARY.md` | Bestiary: 12 vị trí, 54 cơ quan, tier quái, kiểu xã hội, kỹ năng quái, 29 loài theo vùng, ánh xạ vào code | Nằm ở nhánh `docs/bestiary` (chưa vào `main`), do chủ dự án và một phiên khác soạn. **Điểm đã chốt đã được chép vào `GAME_DESIGN.md` mục 4.5**; phần còn lại là đề xuất |

**Quyết định thiết kế đã chốt** (chi tiết trong `GAME_DESIGN.md`):
- Tạo nhân vật: rút ngẫu nhiên 1 Dị Năng chủ động; chọn 1 Thiên Phú trong 3; chọn 1 Khiếm Khuyết trong 3 (vĩnh viễn).
- Cơ quan phải tìm hoặc săn mới có. Nguồn theo bậc: **C** từ sinh vật nhỏ, **B** từ sinh vật thường, **A** từ đại thú, **S** từ boss.
- **Rơi cơ quan (mục 4.3):** bốc hai bước, chọn bậc trước rồi cơ quan trong bậc. Cơ quan đang gắn trên người vẫn rơi được nhưng trọng số ×0.8.
- **EXP tiến hóa (mục 4.4):** ăn cho ít EXP, hấp thụ cơ quan cho nhiều EXP. Chi EXP **chủ động + tự động** (đổ tay, hoặc đặt 1 cơ quan ưu tiên). Cơ quan mầm → Lv1 → Lv3.
- **Mạng, chết, cứu (mục 6.1):** 3 mạng mỗi người. Chết còn mạng thì hồi sinh nhưng **cơ quan rơi tại chỗ chết, mỗi cái 50% bị mất**. Hết mạng thì thành xác, đồng đội có 120 giây để cứu (5 giây kéo về 1 máu, rồi chuyền máu 3:1, càng về sau càng nhanh). Không cứu kịp thì đồ rơi ra, mỗi cơ quan 50% mất.
- Sát thương tính theo vị trí trúng đòn và lan sang bộ phận liên kết. **Blueprint** cơ quan giữ vĩnh viễn; cơ quan bị phá hủy tái tạo được bằng tài nguyên.
- Chỉ số: máu, đói, năng lượng, EXP tiến hóa, nhiệt độ.

## 2. Cách làm việc với chủ dự án

- Chủ dự án dùng **Windows + PowerShell**, repo ở **`D:\gametienhoa`**, viết bằng **tiếng Việt**. Họ mới làm quen với Git và Unity, nên hướng dẫn **từng bước**, mỗi lệnh một khối code `bash`.
- **Quy trình mỗi thay đổi:**
  1. Agent code trên nhánh `claude/...` → mở pull request → chủ dự án bảo "merge pull request #N" → agent merge.
  2. Chủ dự án: trong Unity **Ctrl+S và đóng scene** → `git add .` → `git commit -m "wip"` → `git pull --no-edit` → (nếu cần) **Evolve → Build Prototype Scene → Dựng lại** → Play → báo phản hồi.
  3. Chủ dự án commit scene và asset đã dựng lại → `git push`.
- Môi trường cloud của agent **không có Unity và không có trình biên dịch C#**. **Code chưa bao giờ được biên dịch trước khi gửi.** Luôn nói rõ điều này, và nhờ chủ dự án chụp tab Console nếu có lỗi.
- **API của Unity 6.6 hay bị đánh dấu lỗi thời:** `GetInstanceID()` (lỗi biên dịch, dùng id cố định), `FindObjectsByType(..., FindObjectsSortMode)` (cảnh báo, dùng `FindObjectsByType<T>()`). Kiểm tra trước khi dùng hàm tìm đối tượng.
- **Các lỗi Git đã gặp với chủ dự án** (xem cách xử lý bên dưới):
  - Vim mở ra khi gộp code → gõ Esc, `:wq`, Enter (đã khuyên đổi editor sang notepad).
  - `MERGE_HEAD exists` do lần gộp chưa hoàn tất.
  - `push` bị từ chối vì chưa pull.
  - **Xung đột ở file scene `Prototype_01.unity`**: chủ dự án lỡ `git add .` và commit khi còn dấu `<<<<<<<`. Cách sửa: `git checkout origin/main -- Assets/_Project/Scenes/Prototype_01.unity`, commit lại, rồi dựng lại scene bằng menu. **Khi Git báo CONFLICT thì dặn không chạy `git add .`.**
  - Chủ dự án có lúc **đang ở nhánh khác** (như `docs/bestiary`) nên `git pull` báo "Already up to date" dù `main` đã có bản mới. Luôn nhắc kiểm tra `git branch`.
- Đọc `git log origin/main` trước khi sửa: chủ dự án **cũng tự commit** (chỉnh thông số, scene, tài liệu).

## 3. Kiến trúc code (`Assets/_Project`)

Assembly `Evolve` (thư mục `Scripts/`) và `Evolve.Editor` (thư mục `Editor/`, chỉ chạy trong Editor). Input dùng Input System, có nhánh dự phòng cho input cũ. UI là IMGUI tạm.

| Thư mục | Lớp chính | Vai trò |
|---|---|---|
| `Survival/` | `SurvivalState` (dữ liệu thuần, không phải MonoBehaviour), `SurvivalStats`, `SurvivalConfig` (SO), `SurvivalHUD` (IMGUI, chia `partial` thành `SurvivalHUD.cs` và `SurvivalHUD.Drawing.cs`), `HudLayout` (quy tắc bố cục và chuyển menu), `KeyPressed` | Máu, đói, năng lượng, nhiệt độ, EXP tiến hóa, mạng; `SurvivalStats.All` là danh sách người chơi cho AI |
| `Player/` | `SlimeController`, `ThirdPersonCamera` | Trườn, nước rút, nảy, quay mặt theo hướng đi, hồi sinh (R), chơi lại từ đầu khi hết mạng (Y, tạm); camera quỹ đạo |
| `Abilities/` | `AbilitySO`, `AbilityCaster`, `BurstDashAbility` | Dị Năng: rút ngẫu nhiên, phím Q, hồi chiêu, tốn năng lượng |
| `Organs/` | `OrganSO`, `OrganTier`, `OrganSocket`, `OrganHost`, `OrganInteractor`, `OrganPickup`, `DropTableSO` | Cơ quan, bậc, điểm gắn, EXP và cấp (`Invest`, `Priority`), bảng hỏi lắp/thay/hấp thụ, bảng Cơ thể, rơi khi chết, bảng rơi đồ hai bước |
| `Creatures/` | `Prey`, `PreySpawner`, `Predator`, `PredatorSpawner` | Con mồi bỏ chạy và mệt; kẻ săn mồi tuần tra, đuổi, cắn |
| `World/` | `Nutrient`, `MonochromeVision`, `VibrationSense`, `TemperatureZone`, `ClimateSensor` | Thức ăn; thị giác (đen trắng, ánh sáng quanh nhân vật); quét rung; vùng nhiệt độ |
| `Net/` | `CoopManager`, `CoopMenu`, `RemoteAvatar`, `PlayerNetState`, `WorldSync` | **Co-op 0.4a** (Netcode for GameObjects, host + IP). Assembly riêng `Evolve.Net`, chỉ biên dịch khi đã cài gói `com.unity.netcode.gameobjects` (`defineConstraints` + `versionDefines`). Tự thêm khi Play, phím F2. Xem `docs/COOP_SETUP.md` |
| `Ecology/` | `Region`, `RegionPopulation`, `SpeciesPopulation`, `SpeciesSO`, `Den`, `PopulationCap`, `EmbodiedCreature`, `PredatorIncursion`, `IncursionRule`, `Carcass`, `CarcassDecay`, `CarcassStage` | **0.5a, chưa biên dịch.** Quần thể theo vùng thay spawner trong khu hồ (L0 hiện thân từ tổ, L2 chạy phép tính), kẻ săn B đi vào từ rìa, xác 3 giai đoạn. Cùng với `World/VibrationEmitter` (rung phát ra từ sinh vật). Spec: `docs/superpowers/specs/2026-11-17-underroot-survival-slice-design.md` |
| `Survival/CombatFeedback` | | Phản hồi trúng đòn: thanh máu trên kẻ săn mồi và con mồi bị thương (`CombatEvents`), số sát thương bay lên (gộp các gai cùng một loạt), dấu × ở hồng tâm; `CombatEvents.Raise`, `HealthFraction` (Predator và Prey), `HitFlash`. Người vào phòng thấy ngay khi bắn trúng (dự đoán 1 giây, sau đó theo số của Host) |
| `World/` (0.5b) | `WaterZone`, `WaterSensor`, `ShelterZone`, `ShelterSensor`, `ZoneSet` | **0.5b, chưa biên dịch.** Vùng nước (sạch/bẩn, `WaterZone.AnyInScene` bật khát) và nơi trú (trigger); sensor trên slime theo dõi vùng đang đứng trong (`ZoneSet` chống kẹt khi chồng vùng hoặc vùng bị tắt) |
| `Survival/StressEffects` | | **0.5b.** Hàm thuần: độ rung camera, hệ số bán kính quét, hệ số hồi năng lượng theo Căng thẳng (ngưỡng trong `SurvivalConfig`) |
| `Survival/HudVisibility` | | **0.5b.** Hàm thuần quyết định thanh nào hiện trên HUD tối giản (chỉ thanh có vấn đề; giữ Tab hiện tất cả) |
| `Player/BodyFeedback` | | **0.5b.** Thân slime thể hiện chỉ số: xám khô khi khát, nhạt khi đói, run khi căng thẳng |
| `Dev/` | `GodMenu` | Menu thử nghiệm (F1), tự thêm khi Play; chỉ có trong Editor và bản build debug |
| `World/` (khu lớn) | `WorldScale`, `TerrainShape`, `DomeMesh` | **0.6a, chưa biên dịch.** Hằng số quy mô (khu 480 m, lõi phẳng 130 m), hàm độ cao địa hình (lõi phẳng đúng y = 0, đồi, vách rìa), mesh mái vòm quay vào trong. `UnderrootLakeBuilder` dùng chúng để dựng Terrain, mái, rễ, cột mốc, đèn, ranh giới; asset sinh ra nằm ở `Assets/_Project/Terrain/` |
| `World/` (bố cục khu) | `GroundHeight`, `ZoneLayout` | **0.6b, chưa biên dịch.** `GroundHeight` đọc độ cao Terrain để con mồi, kẻ săn, cơ quan rơi và xác bám mặt đất (không có Terrain thì giữ độ cao cũ). `ZoneLayout` giữ dữ liệu bệ vi sinh cảnh, suối, đường mòn; `TerrainShape` dùng nó để làm phẳng bệ và suối (y = 0) |
| `World/` (thực vật) | `VegetationScatter`, `GlowPulse` | **0.6c, chưa biên dịch.** `VegetationScatter` chọn vị trí và loại cây phát quang (mật độ theo vùng, cách nhau ≥ 6 m, trần 380); `GlowPulse` làm cây Bọc Bào Tử nhấp nháy khi camera ở gần (trong 80 m). `UnderrootLakeBuilder` đặt chúng thành `Nutrient` (12 dinh dưỡng, mọc lại 180 giây) |
| `Survival/` (dinh dưỡng) | `NutrientKind`, `FoodProfile`, `NutrientEffects`; `Organs/OrganNutrient`, `OrganDecay` | **0.5c, chưa biên dịch.** Năm kho trong `SurvivalState.Nutrients` (hao theo thời gian và theo `NutrientLoad` cơ quan), hệ số suy yếu khi thiếu; mỗi nguồn thức ăn đưa kèm `FoodProfile` (`NetWorld.Feed(stats, amount, profile)`); `OrganHost` chặn trang bị khi thiếu chất, cho cơ quan yếu dần rồi tan (`Dissolved`) |
| `Survival/` (bệnh tật), `World/` | `DiseaseKind`, `DiseaseRules`; `SporeZone`, `SporeSensor`, `ContagionSensor` | **0.5d, chưa biên dịch.** `SurvivalState.DiseaseTime` (giây còn lại của từng bệnh), `TryInfect`/`Infect`/`Cure`, triệu chứng trong `Tick`; nguồn lây ở `WaterSensor` (nước bẩn), `Carcass` (xác thối), `Damage` (vết thương), `SporeSensor` (bào tử), `ContagionSensor` (người bệnh gần); `PlayerNetState.Diseases` (một byte) gửi bệnh qua mạng |
| `Editor/` | `PrototypeSceneBuilder`, `UnderrootLakeBuilder`, `HudSmokeCheck` | Menu **Evolve → Build Prototype Scene** dựng scene `Scenes/Prototype_01`; menu **Evolve → Build Underroot Lake** dựng scene `Scenes/Underroot_Lake` (tạo species asset D/C/B, prefab `Prey_C`, gắn `VibrationEmitter`, tag `ShallowWater`; dùng chung `CreateSharedAssets`/`CreatePlayer` của builder prototype); `HudSmokeCheck` là kiểm tra tự động HUD (batchmode, chưa chạy thành công) |

**Lưu ý về công cụ dựng scene:**
- Nó **tạo lại scene** từ đầu nhưng **dùng lại** material, prefab và ScriptableObject đã có. Đổi giá trị mặc định trong code **không ảnh hưởng** prefab hay asset cũ.
- Muốn áp giá trị mới cho asset đã có: sửa thẳng file YAML của asset trong repo (khi chủ dự án đã commit nó), hoặc bảo chủ dự án xóa asset rồi dựng lại.
- `EnsureOrgans` và phần nâng cấp `Prey.prefab` bổ sung dữ liệu mà không ghi đè thông số đã chỉnh.
- Khi đổi tên trường được serialize, dùng `[FormerlySerializedAs]` (đã dùng cho `GrowthExpRequired`, `blindSightRadius`).

## 4. Điều khiển (bản prototype)

| Phím | Tác dụng |
|---|---|
| WASD / Shift / Space | Trườn / nước rút (tốn thức ăn) / nảy (tốn năng lượng) |
| Q | Dị Năng (hiện chỉ có "Bộc Phá Màng") |
| Chuột / Alt / F5 | Kỹ năng bắn/lao **theo hướng con trỏ** ở chế độ chuột. **Alt**: chế độ ngắm (hồng tâm giữa màn hình, chuột xoay camera trực tiếp, camera lệch qua vai, kỹ năng theo hồng tâm). **F5**: góc nhìn thứ nhất (camera ở mắt, ẩn thân, có hồng tâm). `AimController.Aiming` / `FirstPerson` / `AimDirection()`; menu mở thì con trỏ tự hiện |
| 1–5 | 5 ô kỹ năng gắn cơ quan (từ cơ quan bậc B ở Lv2, Lv3 nâng cấp); đổi ô trong bảng Cơ thể (G) |
| C | **Quét rung**: sóng lan 25 m, ánh sáng lan ra soi địa hình, thức ăn/con mồi/kẻ săn mồi lóe lên một lần (10 năng lượng, hồi 5 giây) |
| E / F / X | Khi chạm cơ quan rơi: lắp hoặc thay / hấp thụ lấy EXP / bỏ qua |
| G | Bảng **cơ quan** bên phải (cuộn được): đổ EXP, đặt ưu tiên, tháo, gán kỹ năng vào ô 1–5 |
| K | **Kho kỹ năng**: kỹ năng đã mở từ cơ quan đang gắn, mô tả/chi phí, nút gán vào ô 1–5 (tối đa 5; Q và C không chiếm ô) |
| Esc | Đóng menu đang mở (chỉ mở được một menu) |
| R / Y | Hồi sinh khi còn mạng / chơi lại từ đầu khi hết mạng |
| V | Bật/tắt mắt (thử nghiệm) |
| H / F1 | Mở trợ giúp / Menu God |

## 5. Đã làm được (đã merge vào `main`, chủ dự án đã chơi thử và nói ổn)

- **Slime:** trườn 1, nước rút ×1.5 (chủ dự án chỉnh). Đói chỉ giảm khi di chuyển (0.5/giây), nước rút tốn thêm 1.5/giây. Nảy tốn 8 năng lượng. Năng lượng hồi 10/giây khi đứng yên và **3/giây khi đang di chuyển** (`SurvivalConfig.EnergyRegenMovingPerSec`, chủ dự án yêu cầu 2026-11-17; đói bằng 0 thì không hồi), kẻ săn mồi khó phát hiện.
- **Thị giác:** chưa có mắt thì đen trắng, thế giới tối, chỉ có một đèn quanh nhân vật soi bán kính 6 m (`MonochromeVision.blindSightRadius`, vùng nhìn tính quanh nhân vật, không theo camera). Mắt lớn dần thì màu, ánh sáng ngày và tầm nhìn xa trở lại. Quét rung (C) thả thêm một luồng sáng lan rộng trong chốc lát. Không còn chấm sáng tự động.
- **Con mồi:** bỏ chạy 1.8, phát hiện 6 m, chạy 5 giây thì mệt. Bắt được thì +15 thức ăn và rơi cơ quan theo `DropTable_Prey` (60% có rơi, trọng số bậc C/B/A/S 70/22/7/1).
- **Cơ quan:** 6 loại (bảng dưới). EXP tiến hóa: ăn 0.2 EXP mỗi đơn vị thức ăn, hấp thụ cơ quan C/B/A/S = 15/30/60/120. Chi phí mầm → Lv1 = `GrowthExpRequired` (C 30, B 45, A 70), Lv1 → Lv2 = 1×, Lv2 → Lv3 = 2×. Mỗi cấp trên 1 tăng hiệu ứng +25%.
- **Kẻ săn mồi:** phát hiện 7 m (×0.4 nếu đứng yên), đuổi 1.4, mệt sau 6 giây, cắn 15 máu kèm hất văng, 40 máu. Bị Gai độc phản hoặc bị lao vào (vận tốc ≥ 4) thì mất máu. Hạ được: +35 thức ăn, chắc chắn rơi cơ quan theo `DropTable_Predator` (C/B/A/S = 45/48/7/0, siết theo Bestiary mục 3.2).
- **Nhiệt độ:** thân nhiệt dần theo không khí. Ngoài 5–35°C (cộng chịu nóng từ cơ quan) thì mất 0.2 máu/giây cho mỗi độ vượt. Hoang mạc ở phía đông (x 25–50), 50°C, nhiều thức ăn, 2 kẻ săn mồi; đồng bằng có 1 kẻ săn mồi.
- **Mạng và chết:** 3 mạng; chết thì cơ quan rơi tại chỗ chết, mỗi cái 50% mất; hết mạng thành xác (cứu đồng đội chưa làm).
- **Menu God (F1):** bất tử, vô hạn thức ăn/năng lượng, bỏ hồi chiêu, nhận cơ quan, +EXP, tạo/hạ sinh vật, hòa bình, đóng băng, dịch chuyển, tốc độ game, bốc thử 1000 lần trên bảng rơi.

| Cơ quan | Bậc | Vị trí | Hiệu ứng (Lv1) | Cần để mầm → Lv1 |
|---|---|---|---|---|
| Mắt sơ khai | C | Mắt | Có màu, nhìn xa | 30 EXP |
| Chân giả | C | Chân | ×1.4 tốc độ | 30 EXP |
| Chân giả cơ bắp | B | Chân | ×1.7 tốc độ; kỹ năng Xung Phong Húc (Lv2) | 45 EXP |
| Chân chạy | A | Chân | ×2.0 tốc độ | 70 EXP |
| Màng chịu nhiệt | C | Da | Chịu nóng thêm 20°C | 30 EXP |
| Gai độc | B | Lưng | Phản 15 sát thương khi bị cắn; kỹ năng Xả Gai (Lv2) | 45 EXP |

## 6. Khác biệt giữa prototype và thiết kế

- Prototype **gắn cơ quan nhặt trực tiếp**. Thiết kế đã chốt dùng **blueprint nghiên cứu một lần + tái tạo bằng tài nguyên**, có **chất lượng mẫu (Q)** và Integrity/Trauma (xem BIS mục H). Chưa làm.
- Chưa có: độ ẩm, Thiên Phú, Khiếm Khuyết, màn hình tạo nhân vật, lưu/tải, tổ, boss, multiplayer.
- Luật chết mới làm phần 3 mạng và rơi cơ quan. **Cứu đồng đội, chuyền máu, chế độ quan sát** cần multiplayer.
- Dị Năng duy nhất là "Bộc Phá Màng", **không nằm** trong danh sách Dị Năng v2.0 của GDD.
- `README.md` vẫn ghi "chưa có project Unity", cần cập nhật (kèm dòng phiên bản `6000.6.9f1`).
- HUD là IMGUI, sau này có thể thay bằng UI Toolkit.
- **HUD mới do Codex làm** (commit `f07fc46`…`bdccede`, đã đẩy thẳng lên `main`, chi tiết ở `docs/HUD_UI_QA.md`): HUD tối giản hai góc dưới; thanh kỹ năng **Q · 1–5 · C** giữa dưới màn hình có hồi chiêu; nút **Giao diện** chọn HUD cố định hoặc đi theo nhân vật (lưu bằng PlayerPrefs); khi mở menu nhân vật vẫn di chuyển được nhưng kỹ năng, nhặt cơ quan và xoay camera bị chặn (`SurvivalHUD.BlocksInput`); con trỏ tự hiện khi mở menu. Slime không có `SkillBar` thì HUD tự thêm trong bộ nhớ. Codex báo đã biên dịch được bằng Roslyn với assembly Unity và chạy `tools/check-hud.ps1` (6 kiểm tra đạt), **chưa chạy thử trực quan trong Play Mode**. `HUD_UI_QA.md` vẫn ghi nhánh `codex/hud-ui` (nhánh đó chưa từng được đẩy lên GitHub).
- Có **nhiều công cụ cùng làm trên một thư mục** (Claude, Codex): chủ dự án từng bị đứng nhầm nhánh (`docs/bestiary`, `codex/hud-ui`). Dặn chạy `git branch --show-current` trước khi `pull`; thư mục chứa project Unity nên ở `main`.
- Chủ dự án có thể chưa commit bản scene mới nhất sau khi dựng lại; nếu thấy scene lệch với code, bảo họ dựng lại.

## 6.1. Từ Bestiary: các việc code đã được chốt hoặc gợi ý

Quyết định đã chốt nằm ở `GAME_DESIGN.md` mục 4.5. Hệ quả cho code (chưa làm):

- **Đã làm:** bỏ Chân chạy (A) khỏi `DropTable_Prey` (nguồn đúng là đầu đàn Hươu Sừng Rung); công cụ dựng scene cũng không thêm nó vào bảng con mồi nữa. Bảng con mồi giờ chỉ còn 5 cơ quan (Mắt sơ khai, Chân giả, Chân giả cơ bắp, Màng chịu nhiệt, Gai độc); trọng số bậc A (7) không còn tác dụng vì bảng không có cơ quan A. Chân chạy vẫn rơi từ bảng kẻ săn mồi (`DropTable_Predator`).
- **5 ô kỹ năng đã có bản đầu** (`SkillBar`, phím 1–5), tách khỏi Dị Năng Q (`AbilityCaster` giữ đúng 1). `OrganSO.GrantedAbility` + `AbilityUnlockLevel` (2); `AbilitySO.Activate(user, level)` và `CooldownFor(level)` cho Lv3; hồi chiêu tính theo cơ quan. Hai kỹ năng mẫu cho cơ quan bậc B: `ChargeAbility` (Xung Phong Húc) và `SpikeBurstAbility` (Xả Gai: chùm 5 gai bắn về phía hồng tâm, đạn là `SpikeProjectile` tạo lúc chạy), con số là đề xuất. **Chế độ ngắm (Alt)** do `AimController` (tự thêm khi Play, `AimController.Aiming`, `AimDirection()`); bảng Cơ thể (G) và Menu God tự trả con trỏ. Menu God có "Mọi cơ quan Lv2/Lv3" để thử nhanh. **Cần dựng lại scene** (scene cũ chưa có `SkillBar`); chế độ ngắm không cần dựng lại.
- **HP quái theo số người** (A, S, Boss): `HP = gốc × (0.6 + 0.2 × số người trong vùng)`; chỉ cần khi làm co-op.
- **Ăn xác** (~5 giây mất 1 cơ quan) cần xác người chơi chết hẳn, cũng thuộc co-op.
- **Gợi ý thứ tự của Bestiary mục 8** (chủ dự án chưa duyệt, và chưa chọn giữa co-op và BIS): trước co-op nên gộp `Prey`/`Predator` thành một `Creature` đọc `CreatureSO` (giữ đúng thông số hiện tại), rồi thêm bầy Sói Gai (Alpha, hú gọi) và vài loài hoang mạc; sau đó 5 ô kỹ năng, BIS cho mắt/chân/giáp, và boss đầu tiên (Mẹ Bầy Ngàn Chân). Lý do: Netcode chỉ phải đồng bộ một lớp quái.
- **Còn mở (cần hỏi chủ dự án):** quy tắc kích thước → số khe cơ quan; cách chọn ô khi có hơn 5 kỹ năng (đang dùng bảng Cơ thể G, tạm); nâng Nhảy vọt, Lặn cát, Pheromone lên bậc B.

## 6.2. Bản 0.5a (Hồ Khởi Sinh): trạng thái và giới hạn

**Code 0.5a chưa từng được biên dịch hay chạy** (môi trường viết code không có Unity). Việc đầu tiên: mở Unity, chờ biên dịch, sửa lỗi nếu có.

- **Chạy test:** Window → General → Test Runner → EditMode → Run All. Cần gói **Test Framework** (Package Manager). Test nằm ở `Assets/_Project/Tests/EditMode/` (assembly `Evolve.Tests.EditMode`): quần thể, kẻ săn đi vào, xác phân hủy, bậc D, sinh vật hiện thân.
- **Giới hạn co-op:** xác (`Carcass`) **chưa đồng bộ** qua mạng; bản sao D/C trên client **trông giống nhau**; scene có `Net Template (Prey)` và `Net Template (Predator)` (spawner count 0) chỉ để `WorldSync` tìm được mẫu bản sao. Mỗi người vẫn có thế giới riêng như 0.4a ở scene hồ.
- Tên loài (Mudmite, Rootcrawler, Hunter) và số liệu là **[ĐỀ XUẤT]**. `TemperatureZone.Ambient` là °C tuyệt đối (hốc rễ 18°C, bãi mùn 26°C).
- Builder hồ đặt scene mới dựng làm scene đầu trong Build Settings (cùng helper với builder prototype).

## 6.3. Bản 0.5b (Khát, nơi trú, Căng thẳng): trạng thái và giới hạn

**Code 0.5b chưa từng được biên dịch hay chạy.** Kiểm tra theo mục 9.

- Scene hồ: `WaterZone` sạch trên Shallows North/West (Vũng Sinh Mệnh) và Khe Thủy Mạch; một vũng nước đọng **bẩn** (màu tối) ở Bãi Mùn Rễ, quanh (38, 0, -8). 3 Root Shelter ở Chân Rễ có `ShelterZone` (`MaxBodyScale` 1,3), khe = đường kính slime gốc 1,0 m × 1,3 + 0,05 = **1,35 m** (trước là 1,6 m).
- `WaterSensor` và `ShelterSensor` gắn trong `CreatePlayer`, nên có ở cả hai scene; `Prototype_01` không có `WaterZone` nên khát tự tắt.
- **Kẻ săn B chưa bị khe trú chặn thật:** collider capsule của nó chỉ rộng 0,7 m và nó là Rigidbody kinematic (đi bằng `MovePosition`, xuyên qua collider tĩnh). Cần xem bằng mắt; muốn chặn thật phải sửa `Predator` (bản sau).
- Chỉ số mới (khát, căng thẳng) chưa gửi qua mạng: người khác không thấy.

## 6.4. Bản 0.6a (khu 480 m): trạng thái và giới hạn

**Code 0.6a chưa từng được biên dịch hay chạy.** Kiểm tra theo mục 10. Địa hình tạo bằng script chưa ai nhìn thấy: độ dốc, màu và mái vòm có thể cần chỉnh sau lần xem đầu.

- Vùng lõi 130 × 130 m **phẳng tuyệt đối ở y = 0**, nên nước nông, tổ, nơi trú, điểm xuất hiện vẫn đứng đúng chỗ cũ. Ngoài lõi là đồi (cao tới 14 m) rồi vách dốc ở rìa (tới 45 m); 4 hộp ranh giới vô hình chặn cứng ở mép khu.
- **Cảnh báo cho 0.6b:** `Prey`/`Predator` giữ độ cao cố định theo prefab, nên ngoài vùng lõi phẳng chúng sẽ lơ lửng hoặc lún. Hiện mọi sinh vật vẫn ở trong lõi nên chưa lộ ra; 0.6b phải cho sinh vật bám theo mặt đất trước khi đặt tổ ra ngoài lõi.
- Nội dung (nước, tổ, nơi trú, cửa vào của B) vẫn nằm trong lõi; các vi sinh cảnh chưa được dời ra xa (0.6b). Không có hang chồng tầng (Terrain là mặt đồi).
- Trên cấu hình **Mobile** (Forward, 4 đèn mỗi vật), terrain và mái chỉ nhận 4 trong 25 đèn nên phần lớn khu tối; PC dùng Forward+ nên ổn. Nếu terrain hiện màu hồng, gán vật liệu URP TerrainLit vào `materialTemplate`.
- Mái vòm bán kính 360 m, cao 85 m, không có collider; 28 rễ buông và 4 cột mốc (Bắc, Đông, Nam, Tây, cách tâm 190 m).

## 6.5. Bản 0.6b (vi sinh cảnh trên khu 480 m): trạng thái và giới hạn

**Code 0.6b chưa từng được biên dịch hay chạy.** Kiểm tra theo mục 11.

- Bốn bệ phẳng bán kính 50 m: Chân Rễ (bắc, 0 170), Bãi Mùn Rễ (đông, 170 0), Vườn Bào Tử (tây, -170 0), Rìa Săn Mồi (đông nam, 170 -170); suối Khe Thủy Mạch dài xuống tận z = -170. Nội dung hồ chỉ dời x, z, **không đổi độ cao** (vì bệ phẳng y = 0).
- Ba `Region` thay cho một: Bãi Mùn Rễ [D], Vườn Bào Tử [D, C], Rìa Săn Mồi [D, C, B]; mỗi vùng có đủ mồi cho kẻ săn và tổ D để C sinh sản (C chỉ sinh khi có D trong vùng). Rìa Săn Mồi có `deactivateRadius` 140 để kẻ săn không biến mất giữa lúc đang đuổi. Số mặc định mới khi tạo asset loài: D 18/32, C 10/16 (sinh 0,35, ăn D 0,05/con/phút), B 2/3 (ăn C 0,15/con/phút); tốc độ ăn giảm so với 0.5a để quần thể không tuyệt chủng trong vài phút khi vùng không có người. **Asset loài đã tồn tại thì giữ số cũ**; muốn dùng số mới thì xóa `ScriptableObjects/Species_*.asset` rồi dựng lại.
- Kẻ săn B hiện thân ở cửa vào của Rìa Săn Mồi và tuần tra góc đông nam (nhà tại (110, -110), bán kính 90). Các thiết lập `homeRadius` chỉnh được ở `Predator Incursion`.
- **Cột mốc:** một cột 30 m ngoài tâm mỗi bệ và một cột ở cuối suối (thay cho 4 cột la bàn của 0.6a).
- Đường mòn là lớp đất sáng vẽ lên terrain (không làm phẳng, vẫn leo dốc được). Giới hạn dốc nới tới 0,75 (khoảng 37°).
- **Vẫn chưa làm:** nơi trú chặn kẻ săn B, sinh vật D bỏ chạy khi B tới, nước ở điểm xuất hiện, thảm thực vật dọc đường (0.6c). Các khu khác chưa có.
- **Công cụ `LakeGround.cs` của phiên trang trí** vẽ đường đất tới các vị trí **cũ** của vi sinh cảnh ((40,0), (0,50), (-40,0)); sau 0.6b nó trỏ vào đất trống, cần phiên đó cập nhật.
- **Phiên trang trí hồ** ("Dress Lake", `Editor/LakeWater.cs`, `LakeGround.cs`) đọc scene theo tên và theo `WaterZone`: sau khi dựng lại scene, **chạy lại các menu "Dress Lake"** để nước bờ và đất trang trí được dựng lên chỗ mới.

## 6.6. Bản 0.6c (thực vật phát quang): trạng thái và giới hạn

**Code 0.6c chưa từng được biên dịch hay chạy.** Kiểm tra theo mục 12.

- Năm loại cây (chỉ khác màu, độ sáng, kích thước; vẫn là `Nutrient`): Rêu Lân Quang (xanh ngọc), Nụ Hổ Phách (hổ phách), Bọc Bào Tử Tím (tím, nhấp nháy), Giọt Sương Rễ (xanh nhạt), Quả Than Hồng (đỏ cam). Mỗi loại có vật liệu phát sáng riêng (`Veg_*.mat`). Mô hình riêng chưa có.
- Trần **380 cây** vì `WorldSync` chỉ đồng bộ tối đa 400 cục thức ăn mỗi scene (đã có 6 cục ở Bãi Mùn Rễ). Builder cảnh báo trong Console nếu tổng vượt 400.
- Mọi con số (mật độ, 12 dinh dưỡng, 180 giây, ngưỡng nhấp nháy) là số đặt tạm để chỉnh khi chơi thử. Cây chưa có chất dinh dưỡng riêng (đó là 0.5c).
- Hiệu ứng phát sáng trông đẹp nhất khi có Bloom trong hậu kỳ; chưa kiểm tra có sẵn hay không.
- **Công cụ "Dress Lake"** của phiên khác vẫn dựng cây cỏ trang trí không ăn được; hai bên độc lập, chạy lại cả hai sau khi dựng lại scene.

## 6.7. Bản 0.5c (dinh dưỡng): trạng thái và giới hạn

**Code 0.5c chưa từng được biên dịch hay chạy.** Kiểm tra theo mục 13.

- Mọi số liệu (hao 0,04 mỗi giây cộng 0,03 mỗi cơ quan, ngưỡng 40 và 25, tan sau 300 giây, hồi sau 600 giây, các hệ số) là số đặt tạm trong `SurvivalConfig` và `NutrientEffects` để chỉnh khi chơi thử. Khi cả 5 kho từ 40 trở lên, mọi hành vi giữ y như trước.
- **Co-op:** kho dinh dưỡng của mỗi người do máy của chính họ tính. Thức ăn do Host điều khiển cho người vào phòng chỉ đi qua tin nhắn mang lượng calo, **không mang loại chất**, nên người vào phòng nhận bộ chất "hỗn hợp" mặc định cho phần ăn đó (làm đủ cần mở rộng đồng bộ co-op). Người khác chưa thấy kho của bạn.
- Cơ quan tan thì rơi ra đất như một vật nhặt được (mất phần đã nuôi) cho đến khi có bản mẫu thật (0.5e). Nhặt lại cần đủ chất cơ quan đó đòi.
- Huyết Thiết chưa có cơ quan nào dùng; bệnh tật (0.5d) chưa có. `OrganSO.NutrientNeed` đặt `Auto` cho mọi asset cũ (theo vị trí gắn); muốn đổi cho từng cơ quan, chỉnh trong Inspector.
- Scene cũ chưa gán loại thức ăn cho cây: dựng lại bằng **Evolve → Build Underroot Lake** để cây có loại (Rêu Lân Quang cho Tinh Chất Sống, ...); thức ăn thường của scene khác dùng hồ sơ hỗn hợp.

## 6.8. Bản 0.5d (bệnh tật): trạng thái và giới hạn

**Code 0.5d chưa từng được biên dịch hay chạy.** Kiểm tra theo mục 14.

- Mọi con số (thời lượng 180/240/150 giây, xác suất nhiễm, tốc độ hồi, hệ số triệu chứng) là hằng số đặt tạm trong `DiseaseRules`, không nằm trong `SurvivalConfig`.
- **Lây trong co-op phi tập trung:** mỗi máy tự quyết cho người chơi của mình theo vị trí và byte bệnh của bóng người khác (spec ghi chủ phòng quyết, bản này làm đơn giản hơn). `PlayerNetState` có thêm **một byte** ở cuối (`Write`/`Read` cùng thứ tự): hai bản chơi phải cùng phiên bản code. Phần gửi nhận không có test tự động, cần kiểm tay bằng hai bản.
- Chỉ **Thối Gel** lây. `Revive` (được cứu) giữ nguyên bệnh; hồi sinh (`Reset`) xóa hết bệnh. Không có thuốc ngoài các cách chữa ghi trong HUD.
- Thối Gel **chặn hồi máu tự nhiên** (nếu không, hồi 1,5 mỗi giây sẽ che mất việc mất máu 0,3 mỗi giây); máu dừng giảm ở 10% tối đa.
- Chưa có hiệu ứng hạt hoặc âm thanh cho bệnh. Đám bào tử (`SporeZone`) là hộp vô hình ở cụm nấm Vườn Bào Tử; hình ảnh bào tử do phiên trang trí lo.
- Scene cũ chưa có `SporeSensor`/`ContagionSensor`/`SporeZone`: dựng lại bằng **Build Underroot Lake** và **Build Prototype Scene**.

## 6.9. Bản đồ Hồ Khởi Sinh 500 m (PR #85, #88 và PR E): trạng thái và giới hạn

- Thiết kế: `docs/superpowers/specs/2026-11-17-lake-map-500m-design.md`; kế hoạch: `docs/superpowers/plans/2026-11-17-lake-map-500m.md`.
- **Bố cục** (`World/ZoneLayout`): 500 × 500 m, 6 khu ~80 m quanh hồ giữa, cách nhau ≥ 65 m; mỗi khu có vùng ảnh hưởng 40 m (biên méo theo nhiễu); độ cao: 01 ngang hồ, 02 hang (−1 m), 03 hố (−6 m), 04 cao nguyên (+10 m), 05 khe (miệng +8 m, suối ở đáy), 06 sống đất (+6 m). Hồ viền méo sâu 1,2–1,8 m, vịnh 0,4 m; suối 7–12 m sâu 0,15 m; 2 hố 2 m; vũng đọng (bẩn) dưới hố 03. Nhiễu tự viết (`ZoneLayout.Noise`) nên máy nào cũng ra cùng bản đồ.
- **Bơi** (`Player/SwimLogic`): nước ≥ 0,5 m bơi (Space nổi, Ctrl lặn, 65 %, tốn năng lượng nhẹ, không chết đuối), nổi 0,2 m dưới mặt; lội ở nước nông; gợn sóng quanh thân; camera dưới nước phủ màu. Thân gel ghi độ sâu, nước vẽ sau gel.
- **Trang trí** (menu Evolve → Rebuild Lake, 10 bước): quần xã thực vật theo khu (`World/PlantCommunities`) với LOD 3 mức theo ô 64 m; mái lưới rễ ~25 m + trần đất + rễ quanh vách; nền địa hình nhuộm theo khu; điểm nhấn: bờ ổ gel (01), hang rễ có cửa và khe 1,35 × 1,1 m (02), đường hầm 02 → 03, khúc gỗ mục rỗng (03), cụm nấm khổng lồ (04), cầu rễ 04 → 06, khe đá vôi + ghềnh + viền đá quanh hố (05), vòm rễ 4 × 8 m ở lối vào của B (06).
- **Giới hạn:** khe hang rộng 1,35 m (không phải 0,9 × 0,65 m như concept) vì slime hiện rộng ~1 m; LOD xa chỉ bớt số vật, chưa có bản đa giác thấp; ghềnh suối chỉ là đá và bọt (suối không dốc); sinh vật vẫn dùng AI cũ, chỉ đổi vị trí tổ theo bố cục.

## 6.10. Lựa chọn đầu game: Lõi và Khuyết Điểm (lát cắt đầu, CHƯA chạy thử trong Unity)
- Thiết kế: `docs/superpowers/specs/2026-11-18-starting-choices-design.md`; kế hoạch: `docs/superpowers/plans/2026-11-18-starting-choices.md`.
- Code mới ở `Assets/_Project/Scripts/Traits/` (dữ liệu `CoreSO`/`FlawSO`/`TraitSeed`, chia thẻ `CardDealer`/`TraitRoom`, bù cơ quan `FlawCompensation`, hiệu ứng `LuckRules`, `DeathLossRules`, `FamiliarityGrid`, `FlawRules`, màn hình chọn `TraitPicker`) và `Scripts/Net/TraitsSync.cs` (co-op).
- Đã cài hiệu ứng: 3 Lõi (**Nhân Con Bạc, Nhân Giữ Gốc, Nhân Quen Đất**) và 4 Khuyết Điểm (**Đói Sớm, Run Rẩy, Luống Cuống, Dạ Dày Kén**). 9 Lõi và 8 Khuyết Điểm còn lại chỉ là dữ liệu, hiện được trong thẻ kèm dòng "chưa có hiệu ứng".
- Để dùng: chạy menu **Evolve → Create Traits** một lần (tạo 24 asset trong `Resources/Traits`), rồi vào Play: màn hình chọn hiện ra (1–3 chọn, Tab chi tiết, Enter tiếp, Esc bỏ qua). Test: `Assets/_Project/Tests/EditMode/Trait*Tests.cs`, `CardDealerTests`, `FlawCompensationTests`, `LuckRulesTests`, `DeathLossRulesTests`, `FamiliarityGridTests`, `FlawRulesTests`.
- Giới hạn đã biết: chưa chạy test hay game; Nhân Con Bạc chưa áp cho boss hay rương (chưa có); Luống Cuống làm chậm đổi hướng di chuyển (slime không có xoay); báo đói sớm của Đói Sớm chưa làm; màn chọn là một "căn phòng riêng" phủ kín màn hình (không phải một scene riêng): trong lúc chọn người chơi bị đóng băng, bất tử, không bị quái nhắm tới (`SurvivalStats.Frozen`), không trừ chỉ số, HUD bị che và chặn; sinh tồn chỉ bắt đầu sau khi chọn xong hoặc bỏ qua.

## 6.11. Hố lộ thiên (dự án 1 của đổi môi trường khởi sinh; CHƯA chạy thử trong Unity)
- Thiết kế: `docs/superpowers/specs/2026-11-19-open-sky-pit-design.md`. Hồ Khởi Sinh thành một hố lộ thiên: bầu trời, nắng, thế giới trên miệng hố; vách rìa cao 55 m và dốc.
- Dùng: mở scene `Underroot_Lake`, chạy **Evolve → Rebuild Lake (all steps)** (bước 25 là Open Sky), hoặc lần lượt **Dress Lake: Terrain** rồi **Dress Lake: Open Sky**. Hoàn tác trần: **Evolve → Restore Lake Roof (undo)**.
- Chưa có: mảnh Blender trên mặt vách, mê cung hang (dự án 2), vòng ngày đêm, Nhân Quang Hợp theo nắng. Thị giác không mắt vẫn làm tối cảnh.

## 6.12. Hang Khởi Sinh của Slime: bản đồ mới sạch (CHƯA chạy thử trong Unity)
- Thiết kế: `docs/superpowers/specs/2026-11-19-slime-cavern-design.md`. Bản đồ mới, độc lập với bản đồ Underroot Lake cũ: một hang nhỏ, một hồ sâu 8 m nơi Slime sinh ra, lỗ trời trên hồ, thảm cây nhỏ và quặng phát sáng quanh hồ.
- Dùng: chạy **Evolve → Build Slime Cavern (new map)**; scene được tạo ở `Assets/_Project/Scenes/Slime_Cavern.unity`. Không dùng `Rebuild Lake (all steps)` cho map này.
- Có thức ăn: 12 loại ăn được (8 cây và nấm, 4 quặng), mỗi loại một công thức dinh dưỡng và một nơi mọc; thảm cỏ và đá thường không ăn được. Chưa có: sinh vật, người cây, hệ sinh thái. Bản đồ cũ và hố lộ thiên (6.11) vẫn còn trong dự án nhưng không phải hướng hiện tại.
- **Menu Evolve gọn lại:** các công cụ của bản đồ cũ (Dress Lake..., Restore..., Build Underroot Lake, Rebuild Lake (all steps), cài vật thể Blender/quét) được chuyển vào menu con **Evolve → Bản đồ cũ (Underroot)**. Menu chính chỉ còn Build Slime Cavern (new map), Create Traits, Build Prototype Scene, Rebuild Slime Visuals. Chỉ đổi đường dẫn menu, không xóa code; trong tài liệu cũ các tên menu vẫn ghi đường dẫn cũ.

## 7. Việc tiếp theo (chủ dự án chưa chọn)

1. **0.4b đợt 1 (thế giới chung) đã viết, chưa chạy thử**: Host điều khiển quái, thức ăn, cơ quan rơi; người vào phòng thấy bản sao, bị cắn, bắn trúng, ăn, nhặt (xem `docs/COOP_SETUP.md`). 0.4a (thấy nhau) **đã chạy được** trên máy chủ dự án (Netcode 2.14.0). Đợt 2 còn: cứu đồng đội, chuyền máu, quan sát. Ý ban đầu của hạng mục này: đưa quái, thức ăn, cơ quan rơi về Host điều khiển bằng `NetworkObject`/prefab, đồng bộ sát thương, rồi cứu đồng đội và quan sát. Trước đó: **bản 0.4a** (Netcode for GameObjects, host + IP, người chơi thấy nhau qua tin nhắn có tên, không dùng `NetworkObject`) cần chủ dự án cài gói, thử, và gửi lỗi biên dịch nếu có. Mục tiêu gốc của hạng mục này (`GAME_DESIGN.md` mục 8): Netcode for GameObjects, thử 2 người, rồi làm cứu đồng đội, chuyền máu, chế độ quan sát theo mục 6.1. Dữ liệu đã tách sẵn (`SurvivalState`, `SurvivalStats.All`, `OrganHost`) để đồng bộ ID cơ quan và trạng thái. Nên xin ý kiến chủ dự án về mô hình mạng (host/client hay dedicated) trước khi làm.
2. **Hệ thống hư hỏng cơ quan (BIS)** theo `BIOLOGICAL_INTEGRITY.md` mục H: mắt, chân, giáp; vùng trúng đòn; Integrity; blueprint; tái tạo tại tổ. Chơi một người cũng làm được. Công thức còn là đề xuất, cần hỏi chủ dự án trước.
3. Quay lại phản hồi gần nhất của chủ dự án để chỉnh: độ sáng quanh nhân vật (`blindLightIntensity` 8), tầm quét, độ khó kẻ săn mồi và hoang mạc.
4. Các mục còn lại: Thiên Phú, Khiếm Khuyết, màn hình tạo nhân vật, Dị Năng theo danh sách v2.0, lưu/tải, tổ, boss; cập nhật `README.md`.

## 8. Danh sách kiểm tra thủ công 0.5a (cho chủ dự án)

1. Unity mở project, chờ biên dịch xong: Console không có lỗi đỏ.
2. Menu Evolve → Build Underroot Lake → (nếu hỏi) "Dựng lại". Scene `Underroot_Lake` mở ra, có trong File → Build Profiles/Build Settings.
3. Kiểm tra asset: `ScriptableObjects/Species_D_Mudmite`, `Species_C_Rootcrawler`, `Species_B_Hunter` có số liệu đúng; `Prefabs/Prey.prefab` có VibrationEmitter strength 0.4, `Predator.prefab` strength 1; có `Prey_C.prefab` (to hơn).
4. Play: Console không lỗi đỏ.
5. Đi về phía đông (Bãi Mùn Rễ) → D xuất hiện quanh tổ; sang tây (Vườn Bào Tử) → C xuất hiện. Đi xa > 60 m khỏi mọi tổ và tâm hồ → chúng biến mất; quay lại → số lượng đúng.
6. Giết hết D quanh một tổ → chỉ quay lại chậm theo thời gian. Tắt GameObject `Den D (Mudmite) 1` và `2` (hoặc GodMenu nếu có) → D không sinh thêm.
7. Đứng ở Vũng Sinh Mệnh: không thấy B. Chờ ~4 phút → thấy vòng rung tím ở rìa trước khi thấy B.
8. Hạ B → xác đổi màu qua 3 giai đoạn trong ~12 phút.
9. Thử lăn vào hốc sâu (tây bắc và nam-đông nam): bị chặn, không rơi xuống. Tường bao và đá chặn khe bắc/nam không cho ra ngoài.
10. Vào Chân Rễ Cổ Thụ (bắc) thấy nhiệt độ lạnh, Bãi Mùn Rễ (đông) ấm (HUD).
11. Evolve → Build Prototype Scene vẫn chạy như cũ (slime, camera, HUD, con mồi, hoang mạc, kẻ săn).
12. Co-op F2: host + client vào được scene Underroot_Lake (mỗi người thế giới riêng như 0.4a), không lỗi "không tìm thấy PreySpawner/PredatorSpawner".

## 9. Danh sách kiểm tra thủ công 0.5b (cho chủ dự án)

Lưu ý: scene đã lưu từ trước **không tự có** sensor mới và `BodyFeedback`. Phải dựng lại: **Evolve → Build Underroot Lake** và **Evolve → Build Prototype Scene**.

1. Console không có lỗi đỏ; Test Runner (EditMode) xanh toàn bộ.
2. Ở hồ: Khát giảm dần; đứng trong nước thì đầy lại. HUD lúc đầu chỉ hiện số mạng và nhiệt độ; giữ **Tab** thấy đủ các thanh.
3. Đi xa nước lâu (ví dụ ở Vườn Bào Tử) → thanh Khát hiện trên HUD, thân slime xám khô; Khát về 0 thì mất máu.
4. Vào một Root Shelter (Chân Rễ, phía bắc): Căng thẳng giảm nhanh, năng lượng hồi gấp đôi. Bị kẻ săn cắn → Căng thẳng tăng.
5. Căng thẳng > 60 → camera rung và vòng quét (phím C) co lại; ở 100 không có gì tệ hơn (không mất máu, không mất điều khiển).
6. Nuôi 6 cơ quan → slime to hơn và không chui lọt khe trú; tháo bớt cơ quan thì chui lại được.
7. Nhìn bằng mắt xem kẻ săn B có đi vào được hốc trú không. Theo code hiện tại **có thể vào được** (B rộng 0,7 m < khe 1,35 m và di chuyển kinematic xuyên collider) — ghi lại để quyết định ở bản sau.
8. Vũng nước đọng ở Bãi Mùn Rễ trông tối hơn nước sạch, vẫn uống được.
9. `Prototype_01` chạy như cũ, không bị khát chết.
10. Co-op F2: vào phòng không lỗi (chỉ số mới chưa hiển thị cho người khác).
11. Đã biết, vô hại: khi đã chết và đang quan sát, Tab vừa hiện đủ thanh vừa đổi người được quan sát.

## 10. Danh sách kiểm tra thủ công 0.6a (cho chủ dự án)

1. Mở Unity, Console không lỗi đỏ; **Test Runner → EditMode → Run All** xanh toàn bộ (thêm 11 test: `TerrainShapeTests`, `DomeMeshTests`).
2. Chạy lại **Evolve → Build Underroot Lake**. Kiểm tra thư mục `Assets/_Project/Terrain/` có `Underroot_Zone1.asset`, `Underroot_Dome.asset`, `Underroot_Zone1_Layer.terrainlayer`, `Underroot_Mud.asset`.
3. **Đóng scene rồi mở lại** `Underroot_Lake`: địa hình và mái vẫn còn (không phẳng, không biến mất).
4. Vùng lõi trông như trước: nước nông, tổ, nơi trú, điểm xuất hiện không lún hay lơ lửng.
5. Đi từ tâm ra rìa bắc/đông/nam/tây: có đồi, trườn được mọi chỗ (nếu có dốc quá không leo được, chụp ảnh gửi mình); tới rìa gặp vách dốc và không thể ra ngoài. Bấm giờ: từ tâm tới vách khoảng 4 phút, ngang cả khu khoảng 8 phút.
6. Ngẩng lên thấy mái vòm và rễ buông; thấy bốn cột mốc lớn ở bốn phương khi tới gần.
7. Mở **Stats** trong cửa sổ Game và ghi lại FPS đứng giữa khu và ở rìa.
8. Co-op F2: vào phòng không lỗi.
9. `Prototype_01` dựng lại vẫn chạy như cũ.

## 11. Danh sách kiểm tra thủ công 0.6b (cho chủ dự án)

1. Console không lỗi đỏ; **Test Runner → EditMode → Run All** xanh toàn bộ (thêm `GroundHeightTests`, `ZoneLayoutTests`, test bệ phẳng trong `TerrainShapeTests`).
2. (Nếu muốn dùng số quần thể mới) xóa `ScriptableObjects/Species_*.asset`. Chạy lại **Evolve → Build Underroot Lake**, rồi chạy lại các menu "Dress Lake" của phiên trang trí.
3. Đi từ tâm theo đường mòn sáng màu tới từng bệ (bắc, đông, tây, đông nam) và dọc suối xuống nam. Ở mỗi bệ nước, nơi trú, tổ, thức ăn đứng đúng mặt đất, không lún hay lơ lửng; đường mòn leo được, không có chỗ dốc kẹt.
4. Tới gần mỗi bệ khoảng 70 m: sinh vật hiện ra; đi xa hơn 90 m thì biến mất; quay lại thấy đúng số lượng. Sinh vật đi lại trên đồi ở rìa bệ **không lơ lửng cũng không lún**.
5. Nhìn thấy cột mốc ở mỗi bệ và cuối suối khi tới gần.
6. Kẻ săn B không bao giờ hiện thân trong 40 m quanh bạn; B xuất hiện ở Rìa Săn Mồi rồi tuần tra góc đông nam; bấm **C** khi lại gần để thấy vết rung tím trước khi thấy B. Đường vòng nam → Rìa Săn Mồi dùng được.
7. Hạ một con thú trên đồi: cơ quan rơi và xác nằm đúng mặt đất.
8. Ghi lại FPS (mở Stats) ở bệ đông sinh vật và ở giữa khu.
9. Co-op F2: người vào phòng thấy sinh vật đứng đúng mặt đất; không lỗi.
10. `Prototype_01` dựng lại vẫn chạy như cũ (sinh vật giữ độ cao như trước).
11. Ghi nhận khi chơi: kẻ săn B chỉ vào được khi bạn đứng cách tổ B khoảng 40 đến 70 m (hai cửa vào cách tâm 25 m, luật 40 m quanh người chơi); để quần thể chạy vài phút rồi xem số C và D có còn không (cân bằng sinh/ăn còn là số đặt tạm).
12. `GroundHeightTests` bị bỏ qua (không lỗi) nếu đang mở scene có Terrain; muốn chạy nó, mở `Prototype_01` rồi chạy lại Test Runner.

## 12. Danh sách kiểm tra thủ công 0.6c (cho chủ dự án)

1. Console không lỗi đỏ; **Test Runner → EditMode → Run All** xanh (thêm `VegetationScatterTests`, `GlowPulseTests`).
2. Chạy lại **Evolve → Build Underroot Lake** (rồi các menu "Dress Lake"). Console in "Thực vật phát quang: N cây, tổng M cục thức ăn": N ≤ 380, M ≤ 400, không có cảnh báo vượt giới hạn.
3. Đi dọc đường mòn: cây Rêu Lân Quang xanh ngọc phát sáng dày dọc đường; đứng lên cây thì độ no tăng.
4. Bãi Mùn Rễ nhiều Nụ Hổ Phách; Vườn Bào Tử có Bọc Bào Tử Tím nhấp nháy khi lại gần; Chân Rễ chỉ lác đác Giọt Sương Rễ; Rìa Săn Mồi rất thưa Quả Than Hồng; vùng hoang thưa.
5. Không có cây nào trên nước, hốc sâu, nơi trú, tổ hay trong vòng điểm xuất hiện; cây không lún cũng không lơ lửng trên đồi.
6. Ăn hết một cây thì sau khoảng 3 phút mọc lại.
7. Đi xa đường mòn một lúc: có đoạn thiếu thức ăn (độ no tụt); ghi nhận để chỉnh mật độ.
8. FPS ổn khi đứng giữa vùng nhiều cây (mở Stats); ghi lại con số.
9. Co-op F2: người vào phòng thấy cùng cây, không có cảnh báo "số cục thức ăn khác nhau".
10. `Prototype_01` dựng lại vẫn chạy như cũ.

## Công cụ xem map (ghost và góc nhìn rộng)
`Dev/GhostMode.cs` (chỉ Editor và bản dev, tự thêm khi Play): **F6** bật/tắt chế độ ghost (slime bay xuyên mọi thứ theo hướng nhìn: WASD, Space lên, Ctrl xuống, Shift nhanh); **F7** mở bảng nhỏ (ghost, tốc độ bay, góc nhìn); **Page Up / Page Down** tăng/giảm góc nhìn bất cứ lúc nào, **Home** về mặc định. Slime vẫn sống (đói, sinh vật tấn công) khi là ghost; trong co-op người khác thấy nó bay.

## 13. Danh sách kiểm tra thủ công 0.5c (cho chủ dự án)

1. Console không lỗi đỏ; **Test Runner → EditMode → Run All** xanh (thêm `NutrientModelTests`, `SurvivalNutrientTests`, `OrganNutritionTests`, `FoodProfilesWiringTests` và hai test mới trong `HudVisibilityTests`).
2. Chạy lại **Evolve → Build Underroot Lake** (rồi "Dress Lake"). Vào Play: ban đầu cả 5 chất ở 80, HUD **không** hiện chất nào; giữ **Tab** hiện đủ 5 hàng (Sợi Nhầy, Tinh Vôi, Huyết Thiết, Mỡ Nhờn, Tinh Chất Sống).
3. Ăn thịt (bắt con mồi): Sợi Nhầy, Huyết Thiết, Mỡ Nhờn tăng. Ăn từng loại cây: Rêu Lân Quang tăng Tinh Chất Sống, Nụ Hổ Phách tăng Mỡ Nhờn, Giọt Sương Rễ tăng Tinh Vôi, Quả Than Hồng tăng Huyết Thiết.
4. Đứng yên lâu: các kho tụt chậm. Mang thêm cơ quan: kho chất cơ quan đó đòi tụt nhanh hơn (xem hàng trên Tab).
5. Dùng Inspector (SurvivalStats → state → Nutrients) đặt Tinh Vôi = 10: HUD hiện hàng Tinh Vôi kèm dấu "!"; thân slime đổi màu; **không lắp được** cơ quan Lưng (hộp thoại báo thiếu Tinh Vôi); hấp thụ (F) vẫn dùng được.
6. Đặt Tinh Chất Sống = 10: vòng quét rung (phím C) co lại khoảng 0,7 lần. Đặt Sợi Nhầy = 0: hồi máu chậm rõ; Huyết Thiết = 0: máu tối đa giảm còn 70%.
7. Có cơ quan đang gắn, đặt chất nó cần = 5 và đợi: bảng Cơ quan (phím G) hiện "đang yếu X%"; sau khoảng 5 phút cơ quan tan và rơi ra đất. Ăn đủ lại: cơ quan còn yếu thì hồi dần.
8. Hồi sinh: 5 kho đầy lại (80). `Prototype_01` vẫn chạy như cũ.
9. Co-op F2: vào phòng không lỗi (người vào phòng nhận hồ sơ hỗn hợp cho phần ăn do Host điều khiển).

## 14. Danh sách kiểm tra thủ công 0.5d (cho chủ dự án)

1. Console không lỗi đỏ; **Test Runner → EditMode → Run All** xanh (thêm `DiseaseRulesTests`, `SurvivalDiseaseTests`, `DiseaseSourcesTests` và hai test mới trong `HudVisibilityTests`).
2. Dựng lại **Build Underroot Lake** (và Prototype). Ban đầu không có bệnh, HUD không có dòng "Bệnh:".
3. **Nhiễm Nước Đọng:** uống ở vũng nước đọng bẩn (Bãi Mùn Rễ) vài lần: có lúc HUD hiện "Bệnh: Nhiễm Nước Đọng — nghỉ ở nơi trú và uống nước sạch"; khát nhanh hơn, hồi máu chậm. Vào nơi trú hoặc đứng trong nước sạch: bệnh hết nhanh hơn.
4. **Thối Gel:** ăn xác thối (bấm vào xác ở giai đoạn thối) hoặc bị cắn nặng: có thể bị bệnh; máu không tự hồi và tụt chậm tới khoảng 10% rồi dừng (không chết); ăn Rêu Lân Quang (Tinh Chất Sống) thì bệnh hết nhanh hơn; cơ quan đang yếu hồi chậm hơn.
5. **Mờ Bào Tử:** đứng trong cụm nấm Vườn Bào Tử một lúc: bị bệnh, vòng quét rung (phím C) co lại, căng thẳng tăng; tự khỏi sau khoảng 2,5 phút, nhanh hơn khi đủ Tinh Chất Sống.
6. Đặt Tinh Chất Sống = 0 (Inspector): các lần nhiễm xảy ra dễ hơn rõ rệt.
7. Thân slime ngả xanh vàng khi có bệnh và trở lại bình thường khi hết.
8. Hồi sinh sau khi chết: hết bệnh. `Prototype_01` chạy như cũ.
9. **Co-op F2 (hai bản):** người B thấy dòng bệnh của A khi A bệnh; đứng cạnh người bệnh Thối Gel (dưới 3 m) một lúc thì có thể bị lây; chỉ Thối Gel lây.


## 6.9. Bản 0.5e (bản mẫu và đổi cơ thể ở nơi trú): trạng thái và giới hạn

- Gắn hoặc hấp thụ cơ quan là học bản mẫu; bản mẫu giữ khi cơ quan tan, bị tháo hoặc khi chết (trong phiên chơi, chưa lưu file; không đồng bộ co-op).
- Menu G có mục "Bản mẫu đã học": mô phỏng lại tốn EXP bằng EXP hấp thụ cùng bậc (số đề xuất); ô đã có cơ quan thì cơ quan cũ bị thay (mất).
- Gắn, thay, tháo và mô phỏng lại cơ quan **ở bất cứ đâu** (chủ dự án chọn bỏ luật nơi trú; spec 6.8.3 không áp dụng).
- Chưa làm: vật liệu gel riêng cho cơ quan (hiện dùng `SlimeGelTint` sẵn có, thiếu chất chỉ co nhỏ tới 70%), vỏ riêng cho bản sao rơi, hành động bậc B, hình gel khác biệt từng loại (0.5f).

## 15. Danh sách kiểm tra thủ công 0.5e (cho chủ dự án)

Dựng lại cả hai scene trước. Chạy Test Runner (EditMode): thêm các test `OrganBlueprintTests`.

1. Hấp thụ một cơ quan rơi (F) rồi mở menu G: mục "Bản mẫu đã học" hiện nút của nó.
2. Ở bất cứ đâu: gắn (E), thay, tháo cơ quan đều chạy.
3. "Mô phỏng lại" chạy ở mọi nơi, trừ đúng EXP; thiếu EXP thì báo và không trừ.
4. Cơ quan tan vì thiếu chất: bản mẫu vẫn còn trong danh sách.
5. Chết hết mạng, chơi lại: bản mẫu còn; cơ quan đã rơi theo luật 50%.
6. Cơ quan đói chất co nhỏ dần (vật liệu gel riêng chưa làm).
7. `Prototype_01`: gắn và tháo như cũ.


## 17. Bộ vật thể Blender, bước 1 (nhóm đá): trạng thái và danh sách kiểm tra

- Dây chuyền: `tools/blender/` (`bpy` 5.3 chạy không giao diện) dựng 6 tảng đá (0,3 → 3,2 m, 1.280 mặt, va chạm 80 mặt, rêu phủ mặt hướng lên), xuất FBX vào `Assets/_Project/Models/Underroot/` và ảnh xem trước vào `docs/art/previews/`. Lệnh ở `tools/blender/README.md`.
- Unity: `Editor/PropImportPostprocessor.cs` gắn Mesh Collider từ mesh `_COL`; `Scripts/World/PropPlacement.cs` (luật đặt, có test); menu **Evolve → Dress Lake: Props** (cũng là bước 8/8 của Rebuild Lake) đặt tối đa 500 đá theo bệ sinh cảnh, tránh nước nông, đường mòn, nơi trú, tổ, điểm xuất hiện.
- **FBX và PNG không nằm trong git của phiên này:** repo dùng Git LFS và proxy của môi trường chặn tải LFS. FBX và ảnh được gửi qua chat; có thể chạy lại script trên máy bạn để tạo lại (cần `pip install bpy`).
- Chưa làm: gỡ đá dựng bằng code trong `LakeGround` (chờ bạn duyệt hình, rồi báo phiên trang trí), các nhóm vật thể khác. Menu Props đã đọc `ZoneLayout.Zones` của bản đồ 500 m (phiên trang trí làm), bỏ qua phần trong hang lớn (cave), nước (đáy dưới mực nước) và đường mòn.

Checklist: (1) thả 6 FBX vào `Assets/_Project/Models/Underroot/`, Console sạch, Test Runner xanh (`PropNamingTests`, `PropPlacementTests`); (2) chọn `Rock_3.fbx`: có Mesh Collider, chiều lớn nhất khoảng 1,6 m (cao khoảng 0,75 m); đá hiện màu xám có rêu xanh phủ mặt trên (shader `Evolve/RockVertexColor`, vật liệu `Rock_VC`; nếu thấy xám trơn thì báo tôi); (3) chạy Evolve → Dress Lake: Props hai lần, số đá như nhau; (4) đá nằm trên mặt đất, không trong nước, không chắn đường mòn; (5) slime chạm đá: dừng lại và đi vòng, không rung; (6) FPS bằng PerfOverlay; (7) chụp ảnh trong Unity gửi cho tôi.


## 18. Ánh sáng và nước đẹp hơn: trạng thái và danh sách kiểm tra

- Menu **Evolve → Dress Lake: Light and Water Look** (cũng là bước 15/15 của Rebuild Lake): bật Depth Texture của URP; dựng Volume hậu xử lý (chỉ Bloom và Vignette nhẹ; màu, sương, ambient do MonochromeVision lo) lưu ở `Settings/LakeLook.asset`; khoảng 24 đèn điểm không bóng cho nấm khổng lồ; đổi hai material nước sang shader `Evolve/LakeWater` (màu và độ mờ theo độ sâu, mép nước mờ dần, bọt nơi nước chạm đáy).
- **Evolve → Restore Lake Look (undo)** trả mọi thứ về như trước (sương, ambient, depth texture, shader nước về URP Lit, xóa Volume và đèn).
- Hạn chế: không phản chiếu thật (chỉ viền sáng và vệt nắng), shader viết tay chưa biên dịch thử, sương áp cho cả scene (dưới nước `UnderwaterTint` vẫn ghi đè), chỉ bản đồ hồ 500 m (menu đã gắn vào Rebuild Lake).

Checklist: (1) Console sạch, Test Runner xanh (có `WaterLookTests`); (2) chạy menu: nước không hồng hay đen; (3) mép nước mờ dần, vùng sâu đậm hơn, bọt ở chỗ nước chạm đá và bờ; (4) vũng khu 03 vẫn vàng lục; (5) có sương, ambient xanh lục, Bloom và Vignette không làm tối hoặc cháy; (6) nấm phát sáng chiếu lên đất; (7) bơi xuống hố: `UnderwaterTint` vẫn chạy, ra khỏi nước sương trở lại; (8) FPS bằng PerfOverlay không giảm quá 15%; (9) Restore trả về như cũ; (10) chụp ảnh trong Unity gửi cho tôi.


## 19. Điểm nhấn Blender (khúc gỗ mục, nấm khổng lồ, cung rễ): trạng thái và danh sách kiểm tra

- `tools/blender/build_landmarks.py` dựng: `Log_Hollow` (gỗ rỗng 9 m, hai đầu hở, va chạm là ống rỗng), `Mushroom_Giant_0..2` (7 / 5,5 / 4,5 m, mũ tên `Giant Caps_N`, va chạm chỉ ở thân), `Mushroom_Small_0..3`, `RootArch` (khe giữa ≥ 2,2 m, cao 8 m, va chạm hai chân). FBX cài từ `tools/blender/out/*.fbx.b64` (menu **Evolve → Install Blender Rocks** hoặc tự động).
- Menu **Evolve → Dress Lake: Landmark Props** (bước 15/16 của Rebuild Lake, trước Light and Water Look): xoá "03 Rotting Log", "04 Giant Mushrooms", "06 Root Arch" của `LakeLandmarks` và đặt bản Blender ở cùng chỗ. Thiếu FBX thì giữ bản cũ. Không sửa `LakeLandmarks.cs`.
- Hạn chế: hình dạng là bản đầu (cung rễ trơn, mũ nấm bán cầu đơn giản); ảnh xem trước ở `docs/art/previews/` (không vào git).

Checklist: (1) Console sạch, Test Runner xanh; (2) chạy menu: các object cũ biến mất, bản Blender xuất hiện đúng chỗ (03 khúc gỗ, 04 cụm nấm, 06 cung rễ); (3) chui qua khúc gỗ được từ hai đầu; (4) nấm lớn: chạm thân bị chặn, đi dưới mũ cao không bị chặn; (5) đi qua cung rễ vào khu 06 với slime cỡ tối đa; (6) chạy menu hai lần: không nhân đôi; (7) FPS bằng PerfOverlay; (8) đèn nấm còn; (9) chụp ảnh gửi tôi.


## 20. Không khí riêng từng khu (sương và hạt): trạng thái và danh sách kiểm tra

- `ZoneAtmosphere` (trên camera, thêm lúc chạy cùng `UnderwaterTint` khi cảnh là hồ) trộn mượt sương và hạt theo mức ảnh hưởng của 6 khu (`ZoneLayout.Influence`); số liệu từng khu ở `ZoneAtmosphereMath.Zones` (01 xanh ngọc nhạt 160 m, 02 nâu ấm 70 m, 03 nâu cam đục 55 m, 04 tím nhạt 90 m, 05 trắng xanh lạnh 120 m, 06 xám đỏ sẫm 40 m).
- Sương: `FogMode.Linear`; chưa có mắt thì sương đen và tầm nhìn 80 m (như `MonochromeVision`), có mắt thì màu theo khu; khi camera dưới nước thì nhường cho `UnderwaterTint`.
- Hạt: ba hệ hạt (rơi, bay lên, trôi) đi theo camera, **tối đa 150 hạt (thực tế mỗi lần một nhóm phát, khoảng 20–50 hạt tùy khu, vòng đời 4–6 s)**, theo khu trội nhất, tắt khi chưa có mắt hoặc dưới nước. **Phím F8** bật tắt hạt.
- Hạn chế: chưa có lớp sương sát đất; hạt xuyên vật thể gần camera; hình hạt là chấm mờ (chưa có hình riêng như cánh hoa, lá).

Checklist: (1) Console sạch, Test Runner xanh (`ZoneAtmosphereMathTests`); (2) đi qua 6 khu: màu sương và hạt khác nhau, ranh giới mượt; (3) chưa có mắt: sương đen, không thấy hạt xa; có mắt (phím V): sương có màu khu; (4) lặn xuống hố sâu rồi nổi lên: sương trở về đúng khu, không kẹt xanh ngọc; (5) F8 bật tắt hạt; (6) FPS bằng PerfOverlay không giảm quá 3%; (7) chụp ảnh từng khu gửi tôi.


## 21. Vật thể Blender đợt 3 (nấm và cung rễ chi tiết hơn, vách rìa, rễ buông mái) và khu 03

- Nấm: thân có rãnh sợi, vòng váy, chân chống; mũ dày có mép lượn sóng, đốm trắng và nếp dưới mũ. Cung rễ: 14 rễ chồng lớp (vài rễ xoắn), rễ bò chân, rễ rủ chia nhánh. Khúc gỗ sáng hơn để tách khỏi nền nâu.
- Mới: `Cliff_0..3` (tấm đá vôi phân lớp 28 × 16 m, va chạm hộp 12 mặt) đặt quanh rìa hang (trừ lối vào của kẻ săn mồi) và `HangRoot_0..3` (rễ rủ từ mái, không va chạm, khoảng 90 cây). Menu **Evolve → Dress Lake: Landmark Props** đặt cả hai. Rễ rủ tránh hang lớn và tia sáng; dài tối đa 14 m, cách đất ≥ 5 m.
- Khu 03: sương xám ấm hơn (đỡ nâu), tầm nhìn 75 m, hạt lá mờ và nhỏ hơn (số hạt 3).
- Cập nhật FBX: menu **Evolve → Reinstall Blender Props (overwrite)** ghi đè từ `tools/blender/out/*.fbx.b64` (menu Install chỉ ghi file còn thiếu).
- Checklist: (1) Console sạch; (2) Evolve → Reinstall Blender Props (overwrite), rồi Dress Lake: Landmark Props; (3) nấm khu 04 có mũ đốm và váy; cung rễ khu 06 nhiều rễ; (4) vách quanh rìa liền khối, lối vào phía bắc còn mở; (5) rễ rủ trên mái, không che tia sáng; (6) khu 03: gỗ dễ nhìn, hạt bớt chói; (7) FPS bằng PerfOverlay; (8) chụp ảnh gửi tôi.


## 22. Ánh sáng lọt qua kẽ rễ (mặt trời đổ bóng của mái): trạng thái và danh sách kiểm tra

- Menu **Evolve → Dress Lake: Dappled Sun** (bước 17/17 của Rebuild Lake, chỉ ở scene `Underroot_Lake`): đèn chính gần thẳng đứng (72°), ấm nhạt, cường độ 0,85 (đã hạ từ 1,8 vì quá sáng so với một mái toàn rễ), bóng mềm; mái rễ (Lake Canopy → Roof) đổ bóng nên ánh sáng chỉ lọt qua kẽ rễ và 9 lỗ sáng; ambient xanh lục rất tối (0,06; 0,08; 0,08); mọi URP asset: tầm bóng 120 m, 4 tầng, bản đồ 2048, bóng mềm. `DappledSun` lắc đèn ±2,5° (chu kỳ 70 s) cho mảng sáng trôi; `ShadowToggle`: **phím F9** bật tắt bóng của mái (so FPS).
- **Evolve → Restore Lake Sun (undo)** trả lại đèn, ambient, URP và mái như trước (sao lưu ở `Settings/LakeSunBackup.json`, chỉ ghi lần Dress đầu).
- Không đổi `MonochromeVision` (chưa có mắt vẫn đen trắng, tối, chỉ sáng quanh người; cường độ đèn do nó nhân lại), `ZoneAtmosphere` (sương mỏng theo khu).
- Hạn chế: nặng máy (mục tiêu FPS giảm không quá 15%); vùng xa hơn 120 m không có bóng nên sáng đều (sương theo khu che); bóng của mức chi tiết xa của mái thô hơn.

Checklist: (1) Console sạch, Test Runner xanh (`DappledSunMathTests`); (2) chạy menu trong scene `Underroot_Lake`, rồi Ctrl+S; (3) mảng sáng dưới các kẽ mái và 9 lỗ sáng, vùng khác tối, mảng sáng trôi chậm; (4) chưa có mắt: đen trắng, tối, chỉ sáng quanh người; có mắt (V): thấy mảng sáng; (5) đo FPS bằng PerfOverlay, F9 tắt bóng thì FPS hồi lại (ghi số trước và sau); (6) Restore Lake Sun trả về như cũ; (7) chụp ảnh gửi tôi.


## 23. Ánh sáng ba lớp hoàng hôn (giai đoạn 1 của trần vòm): trạng thái và danh sách kiểm tra

- **Ba lớp:** (1) đèn sinh học nhẹ có màu theo khu cạnh cây phát quang (mỗi khu tối đa 6 đèn, chỉ bật trong 60 m quanh camera) cùng đèn nấm; (2) mặt trời dịu (cường độ 0,6, ấm hơn) lọt qua kẽ mái; (3) đèn ấm ở lối vào phía bắc (`ZoneLayout.NorthEntry`), nhìn thấy từ xa.
- **Ambient hoàng hôn theo khu** (`ZoneAtmosphere` ghi mỗi frame, nhân với thị lực): 02 (0,08) tối nhất, 03, 04, 01, 05, 06 (0,20) sáng nhất. Chưa có mắt: ambient đen; thị lực dở dang: nội suy từ đen lên ambient khu.
- Mọi đèn sống (`LightByVision`) nhân cường độ với thị lực nên chưa có mắt vẫn đen trắng, tối, chỉ sáng quanh người. Đèn sinh học và đèn lối ra do menu **Evolve → Dress Lake: Light and Water Look** đặt (chạy lại menu này để có đèn mới); mặt trời do **Dress Lake: Dappled Sun** (chạy lại để lấy cường độ 0,6).
- Giai đoạn 2 (vòm và trụ rễ rìa) và 3 (vật thể treo) sẽ làm sau khi duyệt giai đoạn này; plan `docs/superpowers/plans/2026-11-18-vault-and-light.md`.

Checklist: (1) Console sạch, Test Runner xanh (`ZoneAtmosphereMathTests`, `DappledSunMathTests`); (2) chạy lại **Dress Lake: Light and Water Look** rồi **Dress Lake: Dappled Sun** trong scene `Underroot_Lake`, bấm Ctrl+S; (3) chưa có mắt: đen trắng, tối, chỉ sáng quanh người; (4) có mắt (phím V): hoàng hôn dịu, khu 06 sáng nhất, khu 02 tối nhất; (5) cạnh cây phát quang có vầng sáng màu theo khu; (6) mảng sáng mặt trời dịu hơn trước; (7) phía bắc, gần lối vào có ánh sáng ấm; (8) FPS bằng `PerfOverlay`; (9) chụp ảnh từng khu gửi tôi.


## 24. Vòm rễ và sườn rễ (giai đoạn 2 của trần vòm): trạng thái và danh sách kiểm tra

- Menu **Evolve → Dress Lake: Root Vault** (bước 10/19 của Rebuild Lake, sau Root Canopy; chạy lại được): **vòm nhiều khoang** (cao trên hồ và khu 04, thấp trên khu 02 và 03, hốc lõm và gờ nhô xuống, nhiễu nhẹ), có lỗ sáng ở 9 chỗ `LightShafts` và khoảng 40 khe hẹp, đổ bóng thật; **sườn rễ** từ mép Đại sảnh (cội) tỏa ra, chia nhánh và nhánh con, thêm 3 sườn từ hồ; mỗi sườn ghép từ các đoạn rễ cáp xoắn Blender (`Rib_*`, hai mức chi tiết, LOD tự dựng), đầu sườn là `Rib_Tip`, sườn chính chạm rìa hạ thành **chân chống** `Rib_Foot` có va chạm.
- **Kết hợp bản cũ:** rễ lớn leo vách của `LakeCanopy` ("Rim Roots") **được giữ**, chân chống mới đứng giữa chúng; mái lưới rễ cũ và tấm đất phẳng ("Ceiling Earth") bị **ẩn** (không xóa); **Evolve → Restore Lake Canopy (undo)** trả lại.
- Cần chạy **Reinstall Blender Props (overwrite)** trước để có FBX `Rib_*`; thiếu thì menu cảnh báo và giữ nguyên mái cũ. Chạy lại **Dress Lake: Dappled Sun** sau khi dựng vòm để mái mới đổ bóng (Rebuild đã đặt đúng thứ tự).
- Hạn chế: số đoạn rễ lớn (khoảng 900 đoạn, tự đổi sang bản đơn giản khi xa); hình dạng bản đầu; đoạn rẽ nhánh `Rib_Fork` và đoạn cong `Rib_Bend` chưa dùng (nhánh bắt đầu chen vào thân sườn mẹ); khi nối, mối giữa hai đoạn có thể lộ ra nhẹ.

Checklist: (1) Console sạch, Test Runner xanh (`VaultLayoutTests`); (2) **Reinstall Blender Props (overwrite)**; (3) Dress Lake: Root Vault, rồi Dress Lake: Dappled Sun, Ctrl+S; (4) nhìn lên: trần không phẳng, có khoang cao thấp và hốc; sườn rễ chạy từ phía Đại sảnh ra, chia nhánh; (5) mảng sáng qua lỗ và khe; (6) chân chống ở rìa chạm đất giữa các tấm vách; (7) khu 06 gần lối vào còn thoáng; (8) FPS bằng PerfOverlay; (9) Restore Lake Canopy trả mái cũ; (10) chụp ảnh nhìn lên từ vài khu gửi tôi.


## 25. Vật treo trên vòm và thay cột trụ cũ (giai đoạn 3 của trần vòm): trạng thái và danh sách kiểm tra

- **Thay đồ cũ:** `Dress Lake: Root Vault` ẩn 28 "Hanging Roots" tròn nhẵn (chỉ tắt renderer, vì `LakeLayout` và dây leo tìm theo tên) và treo rễ Blender cùng chỗ, cùng độ dài; ba cột "Landmark" (Humus, Spore, HuntEdge) thành thân rễ gồm 4 rễ Blender kéo giãn (2 rủ từ mái, 2 mọc từ nền), cột tròn cũ còn làm vật cản vô hình hẹp 4 m. `Restore Lake Canopy` trả lại cả hai.
- **Vật treo mới (Blender, `tools/blender/build_hanging.py`, test `test_hanging.py`):** `VaultRoot_0..3` (rễ rủ dài 4, 8, 14, 20 m), `VaultCurtain_0..1` (màn rễ mảnh rộng 6 m), `VaultPod_0..2` (túi sáng, mũi phát sáng là đối tượng riêng `Pod Glow_N`), `VaultStalactite_0..2` (nhũ đá 3, 4,5, 6 m). Không va chạm.
- **Đặt theo khu** (cùng menu Root Vault, nhóm "Vault Hanging Props"): 01 túi xanh ngọc; 02 rễ già dày, dài nhất; 03 màn rễ và rễ ngắn; 04 túi tím bào tử; 05 nhũ đá; 06 thưa. Tránh Đại sảnh (9 m), lỗ sáng (11 m), khe sáng (9 m), sườn rễ (6 m). Tối đa 14 đèn túi (`LightByVision`, tắt khi chưa có mắt). Chưa làm "nấm tầng bám sườn" của khu 03.
- Cần **Reinstall Blender Props (overwrite)** để có FBX mới; thiếu FBX thì rễ buông cũ được giữ.

Checklist: (1) Console sạch; (2) Reinstall Blender Props, rồi Dress Lake: Root Vault, Ctrl+S; (3) không còn cột đen tròn nhẵn, không còn gai khổng lồ trên sườn rễ; (4) mỗi khu có vật treo đúng tính cách, độ dài rễ tạo tầng; (5) túi sáng chỉ thấy khi có mắt; (6) FPS không tụt rõ.

- **Xương rồng treo (fantasy, thêm vào giai đoạn 3):** `DragonSkull_0..1` (sọ rồng dài 7 đến 9 m, sừng cong, răng, mắt phát sáng ngọc là đối tượng riêng `Eye Glow_N`) và `DragonRibcage_0` (lồng ngực rồng 8 m), treo bằng cáp rễ (`tools/blender/build_bones.py`, test `test_bones.py`). `Dress Lake: Root Vault` đặt tối đa 8 cái cách nhau ≥ 45 m, cao hơn đất ≥ 8 m, tránh Đại sảnh, lỗ và khe sáng, sườn rễ; tối đa 6 đèn mắt xanh ngọc (tắt khi chưa có mắt). Cần Reinstall Blender Props (overwrite) trước.
- **Sự sống trên vòm:** đèn túi sáng và đèn mắt rồng "thở" chậm (`LightByVision.Pulse`, lệch pha nhau); nhũ đá nhỏ giọt nước (`VaultLifeFx` kiểu Drip, 60% nhũ đá) và túi sáng nhả bào tử bay lên (kiểu Spores), chỉ chạy khi camera cách dưới 45 m (tối đa 12 hạt mỗi chỗ). Chạy lại Dress Lake: Root Vault để có.
- Mô hình sọ và lồng ngực rồng **chưa đẹp**: chờ chủ dự án gửi ảnh tham khảo con rồng rồi dựng lại.
- **Trần không phẳng như dưới gốc cây:** `VaultLayout.Relief` cộng vào độ cao vòm các gờ rễ to buông xuống (tới khoảng 5 m) với chỗ trũng và bướu giữa chúng (cao thêm tới 2 m), theo noise 52 m, 34 m và 24 m; sườn rễ, vật treo, khe sáng đều theo mặt trần mới. Chạy lại Dress Lake: Root Vault. Test `Relief_IsLumpyButGentle`; giới hạn bước dốc của `VaultHeight` nới từ 1,6 lên 2 m trên 2 m ngang.
- ~~Bầu trời qua các lỗ sáng~~ **Đã bỏ theo yêu cầu chủ dự án (2026-11-18):** lỗ sáng lại chỉ thấy mặt trong tối của mái hang. Chạy lại Root Vault sẽ không tạo đĩa trời nữa; đĩa cũ (`Vault Sky Windows`) bị xóa khi dựng lại.
- **Rễ rìa:** `Dress Lake: Root Vault` ẩn (chỉ tắt renderer) "Rim Roots" thẳng nhẵn của `LakeCanopy` và treo rễ Blender (`HangRoot_*`, dày ở mái, thon dần, có rễ phụ) từ vòm xuống đất theo ba hàng quanh vách (khoảng 12 đến 17 m một cây, bề rộng đổi ngẫu nhiên); cửa vào phía bắc vẫn để trống. `Restore Lake Canopy` bật lại rễ cũ.
- **Mép lỗ sáng tròn mịn:** vỏ vòm cắt lỗ theo từng ô 4 m nên mép bị bậc thang, góc vuông. `Dress Lake: Root Vault` thêm một vành đất dày hơi gồ ghề (ống uốn thành vòng, bán kính ống khoảng 1,9 m) quanh mỗi trong 9 lỗ, bám theo độ cao trần, che mép bậc thang để miệng lỗ tròn mềm. Khe sáng hẹp chưa có vành.

## 26. Ánh sáng thực tế hơn, bớt cảm giác nhựa: trạng thái và danh sách kiểm tra

- Menu **Evolve → Dress Lake: Realism** (bước 20/20 của Rebuild Lake; hoàn tác: **Restore Lake Realism**): (1) hậu xử lý điện ảnh trên volume `LakeLook`: tonemapping ACES, tương phản +12, phơi sáng +0,2, hạt phim mỏng 0,22 (độ bão hòa vẫn do `MonochromeVision`); (2) **SSAO** (tối ở khe và chỗ tiếp xúc) thêm vào mọi renderer URP; (3) mọi vật liệu URP Lit trong `Materials` (trừ nước, tinh thể, đèn, vật phát sáng) bớt bóng: độ bóng ≤ 0,1, tắt điểm sáng phản chiếu và phản xạ môi trường (sao lưu ở `Settings/LakeRealismBackup.json`).
- Shader `Evolve/RockVertexColor` (đá, rễ, khối Blender) có thêm: mảng màu và hạt nhiễu 3D theo vị trí thế giới (không hai chỗ giống nhau), pháp tuyến nhám theo nhiễu (ánh sáng bắt các gờ nhỏ), ambient tối hơn ở mặt nghiêng hoặc hướng xuống.
- Hạn chế: đất địa hình và các vật liệu Lit khác chưa có vân chi tiết (đây là việc của lớp nền); SSAO tốn FPS (kiểm bằng PerfOverlay); `Rebuild Lake` bước 18 dựng lại volume nên chạy lại bước 20 sau đó.

Checklist: (1) Console sạch; (2) Dress Lake: Realism, rồi Ctrl+S; (3) cảnh bớt bóng nhựa, đá và rễ có vân, khe tối hơn; (4) FPS không tụt quá 15%; (5) Restore Lake Realism trả lại như cũ.

## 27. Sự sống và nguồn sáng tầng mặt đất: trạng thái và danh sách kiểm tra

- Chủ dự án nhận xét: dưới mặt đất còn trống và thiếu nguồn sáng. Menu **Evolve → Dress Lake: Ground Life** (bước 20/21 của Rebuild Lake; hoàn tác: Restore Lake Ground Life): rải khoảng 430 cụm theo từng khu: nấm sáng (`LumaShroom_0..2`), cụm tinh thể phát sáng (`CrystalCluster_0..2`), hoa đèn lồng (`BulbPlant_0..1`) và rễ trồi lên đất (`RootStub_0..1`, tối, không sáng), đều làm bằng Blender (`tools/blender/build_groundlife.py`, test `test_groundlife.py`). Màu sáng theo khu: 01 ngọc, 02 hổ phách, 03 vàng lục, 04 tím, 05 xanh băng, 06 đỏ dịu. Khoảng một phần ba cụm sáng có đèn nhỏ (tối đa 90, mờ theo thị lực, tắt khi xa 60 m); thêm đèn dẫn đường mỗi 16 m dọc các đường mòn, luân phiên hai bên.
- Phần sáng của vật thể là đối tượng riêng có chữ "Glow" trong tên: `PropImportPostprocessor` gán vật liệu tự phát sáng `Rock_Glow` (shader `Evolve/RockVertexColor` có thêm `_Glow`, tắt khi mù theo `_EvolveBlind`). Túi sáng và mắt rồng treo trên vòm cũng nhận vật liệu này sau khi nhập lại FBX.
- Cần **Reinstall Blender Props (overwrite)** trước. Tránh nước sâu, đường mòn (trừ đèn dẫn đường), nơi trú, hang, cột Landmark, thức ăn.
- Hạn chế: chưa có va chạm cho các cụm này (đi xuyên qua); thú vẫn đi xuyên.

Checklist: (1) Console sạch; (2) Reinstall Blender Props, rồi Dress Lake: Ground Life, Ctrl+S; (3) mỗi khu có màu sáng riêng, đường mòn có đèn dẫn; (4) không có đèn trong nước sâu hay giữa đường; (5) FPS không tụt quá 10% (đèn nhỏ có giới hạn).

## 28. Thảm cỏ rẽ sang hai bên: trạng thái và danh sách kiểm tra

- Menu **Evolve → Dress Lake: Grass** (bước 21/22 của Rebuild Lake, sau Ground Clutter; chạy lại được): thảm cỏ 3D thật quanh vùng nước. `World/GrassMath.Density`: đầy trong khoảng 30 m quanh nước, thưa dần tới hết ở 80 m, loang lổ có bãi trống, không mọc trong nước, trong hang và giữa đường mòn. Bốn mẫu bụi cỏ (cao 0,55 đến 1,15 m, mỗi bụi 5 đến 8 lá thon, cong) là chi tiết địa hình (Terrain detail) dùng GPU instancing nên Unity tự cắt và mờ xa; khoảng 6 bụi mỗi mét vuông ở chỗ dày nhất. Cỏ thẻ cũ của `LakeClutter` giảm 85% ở chỗ đã có thảm cỏ 3D.
- Shader `Evolve/GrassBlade`: gốc tối, ngọn sáng, lung lay nhẹ theo gió; khi có ai đi qua thì lá cỏ **nghiêng ra xa và cúi xuống**. `World/GrassPushers` (tự tạo khi chạy game) báo cho shader 8 người hoặc thú gần camera nhất (slime cả thật lẫn từ xa, Prey, Predator) qua `_EvolvePushers`.
- Hạn chế: cỏ không đổ bóng; đo FPS (số lượng lá lớn); chưa nhìn được trong Unity.

Checklist: (1) Console sạch, Test Runner xanh (`GrassMathTests`); (2) Dress Lake: Grass, Ctrl+S; (3) Play, đi vào cỏ: lá rẽ sang hai bên và đứng dậy sau khi đi qua; (4) cỏ dày quanh hồ, thưa dần ra xa, đường mòn trống; (5) FPS không tụt quá 15%.

## 29. Thực vật và quặng hấp thụ được: trạng thái và danh sách kiểm tra

- Menu **Evolve → Dress Lake: Forage** (bước 21/23 của Rebuild Lake; hoàn tác: Restore Lake Forage). Thêm 10 loại hấp thụ được, mỗi loại có `FoodKind` và công thức dưỡng chất riêng (`Survival/FoodProfile`): **thực vật** (rải theo khu): súng nước (Tinh Chất Sống), bồn bồn (Mỡ Nhờn), dương xỉ đắng (Huyết Thiết và Tinh Chất Sống), nấm mỡ (Mỡ Nhờn), củ rễ ngọt (Sợi Nhầy), hoa băng (Tinh Vôi và Tinh Chất Sống); **quặng**: tinh thạch vôi (Tinh Vôi 1,3), quặng sắt (Huyết Thiết 1,3), muối khoáng (Tinh Vôi), thạch anh phát sáng (Huyết Thiết và Tinh Chất Sống, có phần phát sáng tách riêng). Mô hình Blender: `tools/blender/build_forage.py`, test `test_forage.py`.
- Là các `Nutrient` thường (hình cầu trigger + mô hình), nên ăn, mọc lại và đồng bộ co-op dùng đúng cơ chế cũ. Thực vật 14 đơn vị, hấp thụ 5/giây, mọc lại sau 150 giây; **quặng** 24 đơn vị, hấp thụ chậm 2,5/giây, hình thành lại sau 600 giây. Mỗi khu 34 cái: 01 súng nước (mặt nước nông) và bồn bồn ở bờ, muối; 02 củ rễ ngọt, quặng sắt; 03 nấm mỡ, dương xỉ; 04 dương xỉ, thạch anh; 05 hoa băng, tinh thạch vôi, quặng sắt; 06 quặng sắt, tinh thạch vôi, củ rễ.
- **Giới hạn đồng bộ co-op nâng từ 400 lên 1000 cục thức ăn** (`World/FoodSyncLimit`, gói tin 4096 byte); cảnh báo ở Console nếu vượt. Cả hai máy phải cùng scene (đã chạy Forage).
- Hạn chế: chưa có tên và biểu tượng riêng trong giao diện; quặng chưa có va chạm cứng (đi xuyên qua, hấp thụ khi chạm); thú chưa ăn các loại mới.

Checklist: (1) Console sạch, Test Runner xanh (`FoodProfilesWiringTests`); (2) Reinstall Blender Props, Dress Lake: Forage, Ctrl+S; (3) Play: đứng trên một cái thì thanh dưỡng chất tương ứng tăng đúng loại; (4) quặng ăn chậm và mọc lại rất lâu; (5) hai máy co-op thấy cùng thức ăn (không có cảnh báo lệch).

## 30. Nước thật hơn: trạng thái và danh sách kiểm tra

- Shader `Evolve/LakeWater` viết lại phần tô màu (giữ nguyên tên thuộc tính cũ): **khúc xạ** (thấy đáy hồ qua mặt nước, bị gợn sóng bẻ cong, đọc từ URP "Opaque Texture"); **hấp thụ ánh sáng theo độ sâu** kiểu Beer-Lambert (đỏ mất trước nên nước sâu xanh lục, nước đục nâu lục); **vân sáng tụ quang (caustics)** chuyển động trên đáy nước nông; **phản chiếu Fresnel** (yếu khi nhìn thẳng xuống, mạnh ở góc xiên gần bờ) từ môi trường, ambient và một tông tối của hang; **ánh lấp lánh** từ mặt trời và mọi đèn gần (nấm, tinh thể sáng in vệt lên nước); ba lớp gợn sóng (một lớp sóng chậm rộng, hai lớp mịn); bọt ở bờ lởm chởm, động; mép mờ, ẩn khi mù như cũ.
- Menu **Evolve → Dress Lake: Water Realism** (bước 24/24 của Rebuild Lake; chạy sau Light and Water Look): bật **Opaque Texture** (và Depth Texture) trên mọi URP asset, đặt tham số mới cho các vật liệu nước (`_Absorb`, `_Refract`, `_ReflectTint`, `_CausticStrength`, `_GlintStrength`): nước sạch trong và xanh ngọc, vũng đọng đục.
- Hạn chế: nước vẫn phẳng (chỉ pháp tuyến gợn, chưa có sóng nhô thật); không phản chiếu chính xác vật thể (không có phản xạ phẳng theo màn hình); nếu Opaque Texture tắt thì nước ra đen; đèn phụ chỉ tính trong giới hạn của URP; shader mới chưa được biên dịch hay thấy trong Unity.

Checklist: (1) Console sạch (không lỗi shader); (2) Dress Lake: Water Realism, Ctrl+S; (3) thấy đáy hồ và vân sáng chuyển động ở chỗ nông, nước sâu xanh lục sẫm; (4) nhìn xiên gần bờ thấy phản chiếu, đứng gần nấm sáng thấy vệt sáng trên nước; (5) chưa có mắt vẫn không thấy nước; (6) FPS không tụt quá 10%.

## 31. Thảm thực vật quét thật (đợt 0–1: tài sản, vật liệu bán thực, dải mẫu): trạng thái và danh sách kiểm tra

- Spec `docs/superpowers/specs/2026-11-18-scanned-flora-design.md`, plan `docs/superpowers/plans/2026-11-18-scanned-flora-wave1.md`. Danh sách 22 tài sản Poly Haven (CC0, khoảng 350 MB): `tools/assets/flora_manifest.json`, trang duyệt có ảnh `docs/art/flora-selection.md`.
- **Evolve → Install Scanned Assets**: tải về máy vào `Assets/_Project/ThirdParty/Scanned/` (không vào git), không tải lại file đã có, tài sản lỗi thì báo và làm tiếp; rồi dựng prefab với vật liệu URP **bán thực** (`World/SemiRealGrade`: bớt bão hòa 25%, ngả tông xanh lạnh của hang, giữ độ sáng; độ bóng thấp; lá cắt alpha, hai mặt, không đổ bóng).
- **Evolve → Dress Lake: Scanned Flora Sample** (hoàn tác: Restore Lake Scanned Flora Sample): dải mẫu 40 m ở khu 01 (dương xỉ, tầm ma ở mép ẩm; rêu, cỏ hoa, dây bò; đá rêu, gỗ chết, gốc cây, cụm rễ, cây non) và hai góc máy cố định `Camera Spot 01a/01b`.
- Test: `FloraManifestTests`, `FloraDownloadTests`, `SemiRealGradeTests` (chưa chạy trong Unity).

Checklist: (1) Console sạch, Test Runner xanh; (2) Install Scanned Assets (cần mạng, vài phút); (3) Dress Lake: Scanned Flora Sample, Ctrl+S; (4) nhìn từ hai góc máy: chất lượng có tới mức bạn muốn không, lá không đen mặt sau, màu có hợp với phần còn lại không; (5) FPS tại góc máy ≥ 90.

## 32. Ánh sáng huyền ảo và nước lung linh cho Hang Khởi Sinh: trạng thái và danh sách kiểm tra

- Chủ dự án chuyển sang **bản đồ mới Hang Khởi Sinh** (cách tài sản quét tốn tài nguyên nên bỏ, sẽ làm cách đơn giản hơn sau); việc đầu: ánh sáng và nước huyền ảo hơn vì hiện chỉ có một chùm sáng mảnh chiếu vào mặt nước.
- **Menu Evolve → Dress Cavern: Magic Light** (Build Slime Cavern cũng chạy nó; chạy lại được):
  - **Chùm sáng** `Evolve/LightBeam`: thể tích ánh sáng trong không khí bụi, viền mềm (không còn ống tròn cứng), vệt đậm nhạt trôi chậm, hạt bụi lấp lánh, mờ dần ở gần nước; thêm 4 tia mảnh lọt qua mép lỗ.
  - **Nước** `Evolve/LakeWater` (thuộc tính mới, mặc định tắt nên các hồ khác không đổi): vũng sáng dưới chùm có vòng sóng lan chậm và vân sáng tụ quang, lấp lánh nhấp nháy trên mặt, bờ phát quang xanh ngọc, sắc nước trôi từ xanh ngọc sang tím nhẹ.
  - **Hạt sáng** `Evolve/GlowParticle` (cộng sáng, quầng mềm, nhấp nháy riêng từng hạt): bụi trong chùm sáng (300), đom đóm bay lang thang quanh bờ (110), đốm sáng mờ to bay lên màu ngọc, tím, hổ phách (90), ánh lấp lánh trên mặt hồ (170). Tổng khoảng 670 hạt, rẻ.
  - **Không khí:** ambient và sương lạnh và xanh hơn một chút, Bloom mạnh hơn để các điểm sáng nở quầng.
- Số liệu thuần (hướng chùm, vũng sáng, vòng đom đóm) ở `World/CavernMagicMath`, test `CavernMagicMathTests`.
- Chưa biên dịch hay thấy trong Unity.

Checklist: (1) Console sạch (không lỗi shader); (2) Evolve → Dress Cavern: Magic Light, Ctrl+S; (3) Play: chùm sáng mềm có bụi, đom đóm nhấp nháy quanh bờ, hồ lấp lánh và có vũng sáng dưới chùm; (4) FPS không tụt rõ; (5) nếu quá lòe loẹt hoặc quá tối, báo để chỉnh cường độ.

- **Khi slime chưa có mắt (hang):** chùm sáng, hạt sáng (bụi, đom đóm, đốm sáng, lấp lánh nước), cỏ và các hiệu ứng nước mới chỉ hiện **loang mềm quanh slime**, mờ dần mịn ra xa (`Shaders/EvolveSight.hlsl`: `EvolveSightVisible`, đọc `_EvolveBlind` và `_EvolveSightCenter` của `MonochromeVision`). Đèn rọi của chùm và đèn quặng gắn `LightByVision` nên cũng mờ theo thị lực.

- **Độ sáng do người chơi chỉnh:** `Visuals/BrightnessSetting` (tự tạo khi chạy game): **F10** mở bảng "Độ sáng" có thanh kéo (0 đến 100%, mặc định 35%), phím **[** và **]** chỉnh nhanh từng nấc 5%, nhớ giữa các lần chơi (PlayerPrefs). Thêm một Volume hậu xử lý ưu tiên cao chỉ chỉnh độ phơi sáng (-0,5 đến +3 stop) và nâng vùng tối (LiftGammaGain), không đụng độ bão hòa của hệ thị lực. Số liệu ở `Visuals/BrightnessMath`, test `BrightnessMathTests`.
- **Chùm sáng hang:** phần của chùm gần camera mờ đi (3 đến 14 m), nên đứng trong chùm không còn thấy tường sọc dọc phủ kín màn hình.

## 33. Thảm thực vật không ăn được của Hang Khởi Sinh: trạng thái và danh sách kiểm tra

- Cách quét tài sản bên ngoài bị bỏ vì tốn tài nguyên; thay bằng mô hình Blender nhẹ (`tools/blender/build_cavern_flora.py`, test `test_cavern_flora.py`, màu đỉnh sáng vừa để không đen trong hang tối): dương xỉ hang (2), sậy (2), đệm rêu (3), bụi lá (2), cụm nấm với mũ phát sáng riêng (2), rễ trồi (2), dây leo buông từ trần (2). Mỗi cái dưới 2.200 mặt.
- **Evolve → Dress Cavern: Flora** (Build Slime Cavern cũng chạy nó; hoàn tác: Restore Cavern Flora; cần Reinstall Blender Props trước): đặt khoảng 650 cây theo **luật môi trường** `World/CavernFloraMath` (có test): sậy ở nước nông và mép ướt, rêu ở chỗ ẩm và bóng, dương xỉ ở đất ẩm cách nước 2 đến 22 m, bụi lá ở đất khô hơn cách nước 7 đến 26 m, cụm nấm ở bóng chân vách và mép hồ, rễ trồi ở chân vách, dây leo buông thành vòng quanh lỗ sáng ở trần. Không đặt trong nước sâu, trên chỗ slime sinh ra, hay đè lên thức ăn.
- Cây nhận ánh sáng như các đồ Blender khác (shader `RockVertexColor`): sáng ở gần nguồn, tối dần khi xa; khi slime mù chỉ hiện quanh slime.
- Chưa biên dịch hay thấy trong Unity. Chưa có va chạm cho các cây này.

Checklist: (1) Console sạch, Test Runner xanh (`CavernFloraMathTests`); (2) Reinstall Blender Props, rồi Dress Cavern: Flora, Ctrl+S; (3) quanh hồ thấy các dải sậy, rêu, dương xỉ; bụi lá xa hơn; dây leo quanh lỗ sáng; (4) FPS không tụt quá 10%.

- **Chi tiết hơn (đợt 2 của mục 33):** mô hình dựng kỹ hơn (lá thật dạng bầu dục thon cho dương xỉ, bụi, rễ, dây leo; sậy có lá dài cong và bông; rêu có cuống bào tử; nấm có vòng thân; dây leo có tua cuốn) và 6 loại **chi tiết mặt đất** mới: hoa chuông phát sáng 3 màu (ngọc, tím, hổ phách), thảm lá tam diệp, đống lá rụng, cành khô, cụm cuội, địa y. Mỗi loại 2 đến 3 kiểu; khoảng 1.300 vật; vẫn đặt theo luật môi trường `CavernFloraMath` (có test).

- **LOD tắt (tạm):** `Visuals/LodSetting`: khi tắt LOD (mặc định lúc này), mọi mô hình giữ chi tiết đầy đủ ở mọi khoảng cách (`lodBias` 20, `maximumLODLevel` 0), texture ở độ phân giải đầy đủ, lọc dị hướng bật; texture nền hang giữ nét khắp hang (`basemapDistance` 1000 m). Bật lại LOD (nhẹ máy hơn) trong bảng **F10** (ô "Tắt LOD"); được nhớ giữa các lần chơi. Test `LodSettingTests`.

## 34. Quét rung động là sóng âm, không phải quét ánh sáng: trạng thái và danh sách kiểm tra

- Trước đây phím **C** ném một đèn lớn (cường độ 150) và mở rộng vòng nhìn thấy, nên mọi thứ trong bán kính sáng bừng, và đèn nhỏ quanh slime (cường độ 8) làm các vật thể Blender chói bất thường khi chưa có mắt.
- Nay: **đèn quét tắt hẳn**; đèn quanh slime khi mù không bao giờ vượt 1,8 (`SonarWaveMath.MaxBlindLight`); lượng ánh sáng từ mỗi đèn phụ trong shader đá và cỏ bị chặn ở mức 3 để vật ở gần đèn không cháy sáng.
- **Sóng âm** (`World/SonarWaveMath`, shader `Evolve/SonarShell`, `MonochromeVision.UpdateWave`): một **vỏ cầu mỏng** lan ra từ chỗ slime đứng lúc bấm C, không phát sáng gì, chỉ hiện thành **đường sáng xanh nhạt nơi vỏ cắt qua mặt đất và vật thể** (đọc độ sâu cảnh) cùng viền mờ. Chỉ những gì vòng sóng đang đi qua mới hiện (chùm sáng, hạt sáng, cỏ, đá Blender, nước; `EvolveWaveVisible` trong `EvolveSight.hlsl`), rồi tối lại. Sóng mờ dần sau khi tới bán kính tối đa. Hiệu ứng chạm vật (`VibrationSense` nháy màu theo loại) giữ nguyên.
- Cần Depth Texture bật trong URP (hang đã bật). Chưa thấy trong Unity.

Checklist: (1) Console sạch, Test Runner xanh (`SonarWaveMathTests`); (2) chưa có mắt: vật thể không còn sáng bất thường; (3) bấm C: một vòng sóng lan ra, các đường sáng quét qua mặt đất và vật thể, không có đèn lớn; (4) có mắt: không đổi.

- **Sóng âm, chỉnh theo ý chủ dự án:** bỏ các hạt nháy tại vật thể khi sóng đi qua; bán kính quét giảm một nửa (`VibrationSense.ScanRadiusFactor` = 0,5); vỏ sóng (`Evolve/SonarShell`) dày hơn và có đường viền: mọi bề mặt trong dải khoảng 1,5 m quanh vỏ đều sáng, sáng hơn dọc các mép và nếp gấp của vật (nơi độ sâu nhảy), nên sóng vẽ ra **hình dạng** của mặt đất và vật thể khi lướt qua. Hạt sáng (`GlowParticle`) không còn hiện theo sóng. Đèn nhỏ quanh slime khi mù tối đa 1,0 (trước 1,8) và Bloom của hang nhẹ lại (cường độ 0,8, ngưỡng 1,0) để slime không bị chói.

- **Quét khi có mắt, chậm hơn, thức ăn có hiệu ứng riêng:** quét (phím C) và sóng âm vẫn chạy khi đã có mắt. Sóng đi **chậm hơn 2,5 lần** (`VibrationSense.ExpandFactor`) và dư âm dài hơn (ít nhất 2,2 giây). **Thức ăn** (`Nutrient`) có hiệu ứng bề mặt riêng khi sóng chạm tới: phủ màu theo loại (`World/EdibleScan`: thực vật xanh bạc hà, nấm và quả hổ phách, quặng vàng, thịt đỏ), có dải sọc chuyển động và viền sáng, giữ lại sau khi sóng đã qua cho tới khi sóng tan (shader `RockVertexColor`: `_ScanTint`, `EvolveEdibleEcho`), nên sau một lần quét thấy rõ những gì ăn được trong tầm. Test `EdibleScanTests`.

## 35. Vật lý cho đá và cây cối: trạng thái và danh sách kiểm tra

- **Đá** (Hang Khởi Sinh): mọi tảng đá Blender, kể cả đá dưới cụm quặng, được đảm bảo có va chạm (`LakeProps.EnsureCollision`: mesh `_COL` đơn giản hoặc chính mesh đá): slime đi vòng, không xuyên.
- **Cây cối** (`Editor/CavernFlora.AddPhysics`): **rễ trồi** rắn (chính mesh rễ làm va chạm, không tính lá nhỏ); **dương xỉ, sậy, bụi** mềm: một vùng chạm (trigger) `World/SoftPlant` làm **chậm slime khi đi xuyên qua** (hệ số `PlantDragMath`: nhẹ nhất 1, bụi dày nhất 0,55; slime vẫn đi được), không chặn đường; các loại còn lại (rêu, hoa, lá, cuội, cành, địa y) quá nhỏ nên không có va chạm. `SlimeController.NotePlantDrag` nhận yêu cầu chậm và lấy hệ số nhỏ nhất trong 0,15 giây sau lần chạm cuối.
- Test `PlantDragMathTests`. Cần dựng lại hang (Build Slime Cavern) hoặc chạy Dress Cavern: Flora để có vật lý. Chưa thấy trong Unity.

Checklist: (1) Console sạch; (2) đi vào đá: bị chặn, đi vòng được, không kẹt, không rung; (3) đi vào bụi, dương xỉ, sậy: chậm lại rõ ràng, ra khỏi thì chạy bình thường; (4) rễ trồi chặn đường, đi vòng được; (5) FPS không tụt rõ.

- **Miệng lỗ trời rộng hơn:** bán kính lỗ trên mái hang tăng từ 6,5 m lên **10 m** (`CavernLayout.HoleRadius`, diện tích gấp khoảng 2,4 lần), nên chùm sáng và vũng sáng trên hồ lớn hơn; đèn mặt trời tăng từ 1,3 lên 1,7; chùm sáng, tia phụ, bụi trong chùm và vòng dây leo quanh miệng đều theo bán kính mới. Cần dựng lại hang (Build Slime Cavern).

## 36. Bước 1 "Chuyển Sinh Trong Hang": Nuốt, Bụng, Giải Mã, Biến Hình: trạng thái và danh sách kiểm tra

Spec `docs/superpowers/specs/2026-11-19-reincarnation-survival-design.md`, plan `docs/superpowers/plans/2026-11-19-swallow-learn-core.md`.

- **Nuốt Chửng** (`Swallow/Swallower`): đâm vào con mồi thì nuốt sống vào bụng (con mồi phải ≤ 80% khối của slime, bụng còn chỗ; nếu không thì ăn tại chỗ như cũ). Thú săn mồi chết để lại xác có bảng di truyền (`CarcassSample`): **F** để nuốt; xác to hơn thì **giữ F** để hòa tan. Không còn vật phẩm cơ quan rơi ra.
- **Túi Không Đáy** (`Stomach`, phím **B**): 6 chỗ; mỗi món Tiêu hóa / Phân tích / Giữ, hoặc Nhả ra. Phân tích 20 giây trong hang trú, chậm gấp 4 ở ngoài; tiêu hóa 8 giây, cho no và thêm 30% khối của món.
- **Giải Mã** (`AnalysisRules`, `AnalysisBook`): bảng di truyền lấy từ drop table của loài. Mỗi mẫu một lần may (Thường 25%, Hiếm 8%, Quý 2%, Huyền thoại 10% chỉ ở con đầu đàn) và một bước tích lũy (chắc chắn sau 7 / 15 / 25 / 3 mẫu). Chất lượng mẫu ×1 tươi, ×0,5 xác cũ, ×0,2 xác thối. Đã học thì 3 mẫu lên ★2, 9 mẫu lên ★3. Cấp Giải Mã (10 / 30 mẫu) mở tên rồi % cơ hội. Sổ giữ nguyên khi chết.
- **Biến Hình** (phím **G**): nặn cơ quan đã học bằng **khối** (2 / 4 / 7 / 12 theo độ hiếm), **chỉ trong hang trú**; tháo ra hoàn nửa khối. Cấp cơ quan = số sao. Bỏ hẳn EXP nuôi cơ quan và bản mẫu.
- **Khối lượng** (`SlimeBody`): máu tối đa = khối × 10; kích thước theo khối (khối 10 = cỡ cũ). Chưa mất khối khi bị đánh (để sau).
- **Tiếng Vọng** (`Echo`): dòng chữ giữa trên màn hình khi phân tích, học, lên sao.
- Chết: bụng rỗng, cơ quan và sổ Giải Mã còn nguyên. Hết mạng và mất xác: cơ quan đang gắn mất.
- Cảnh, prefab cũ: HUD tự thêm `SlimeBody` và `Swallower` cho slime khi chạy; dựng lại scene cũng có.
- Test: phần luật chạy được ngoài Unity bằng `tools/cstest/run.sh` (.NET 8, 46 test xanh). Test cần Unity: `SculptTests`. **Chưa mở trong Unity: chưa biết có lỗi biên dịch không.**
- Lưu ý: `ShelterZone.MaxBodyScale` 1,3 nghĩa là slime khối trên 17,5 không vào hang trú cũ được (sẽ làm lại hang trú ở bước 3).

Checklist: (a) Console sạch; (b) đâm vào con mồi: nó vào bụng, B thấy món đang Phân tích; (c) đứng trong tổ: thanh chạy nhanh hơn rõ; (d) học được: có dòng Tiếng Vọng; (e) G ngoài tổ: nút khóa; trong tổ: Nặn trừ khối, cơ quan hiện lên người; (f) giết thú săn mồi, F vào xác (giữ F nếu to): vào bụng; (g) chết: bụng rỗng, cơ quan còn; (h) bụng đầy: có thông báo. Menu God (F1) có "+10 khối" và "Học hết cơ quan" để thử nhanh.

- **Sinh vật và tổ tạm trong Hang Khởi Sinh** (`Editor/CavernLife`, menu `Evolve → Dress Cavern: Creatures + Shelter`, cũng chạy trong Build Slime Cavern): 2 ổ con mồi (7 con) trên bờ khô, 1 kẻ săn mồi phía đông bắc, một **tổ tạm** (vòng rêu sáng xanh có `ShelterZone`) phía tây nam gần chỗ xuất hiện. Spawner giờ đặt sinh vật **trên mặt đất** (`GroundHeight`), không còn ở độ cao cố định. Test `ShorePointIsDryAndTheRightDistanceFromTheWater`.

## 37. Kỹ năng thay cho cơ quan: trạng thái và danh sách kiểm tra

Spec mục 6.1, plan `docs/superpowers/plans/2026-11-19-skills-not-organs.md`. Sửa theo ý kiến sau khi chơi thử bước 1: slime **trơn**, không gắn cơ quan; gene học được là kỹ năng.

- **Bị động** (`SkillRules.UseOf` = Passive: mắt, màng chịu nhiệt): luôn bật, **tối đa 3**, chỉ nặn hoặc tháo ở hang trú (tốn khối như cũ). **Không** bị động nào hiện trên người, kể cả mắt (`OrganHost.ShowsOnBody = false`).
- **Chiêu chủ động** (gene có chiêu riêng, hoặc gene chân có tốc độ > 1): gắn phím **1–5 ở đâu cũng được**, không tốn khối. Học xong là dùng được ở ★1; ★3 là bản nâng cấp. Bảng **K** liệt kê chiêu đã học.
  - **Phóng Gai** (gene Gai độc), **Lao Tới** (Chân giả cơ bắp, chiêu riêng thắng chiêu chạy), **Chạy Nước Rút** (Chân giả, Chân chạy): tốc độ ×(hệ số của chân) trong **4 / 5 / 6 giây** theo ★1 / ★2 / ★3, hồi chiêu 10 giây (×0,75 ở ★3), tốn 15 năng lượng. Hai lần chạy chồng nhau: lấy hệ số lớn hơn, kéo dài đến lúc muộn hơn, không nhân dồn (`SprintTimer`).
  - **Bộ phận chỉ hiện lúc dùng** (`SkillVisuals`): mọc ra trong 0,15 giây, giữ suốt hiệu lực (gai 1 giây, chạy nước rút 4–6 giây), rồi thu vào trong 0,35 giây. Phản đòn của gai chỉ tính lúc gai đang hiện.
- Hệ cơ quan: `OrganHost` chỉ giữ bị động; tốc độ, nhìn, chịu nhiệt tính từ bị động; cơ quan "chiêu" gắn tay (menu God) không có tác dụng thường trực.
- Test: `SkillRulesTests` (chạy bằng `tools/cstest/run.sh`, 68 test xanh), `SculptTests` và `SkillBarMenuTests` (cần Unity). **Chưa mở trong Unity.**

Checklist: (a) Console sạch; (b) bấm **K**: có chiêu đã học (sau khi Giải Mã xong gene Gai độc hoặc Chân); (c) gắn phím 1–5, bấm: gai trồi trên lưng, bắn, rồi tan vào thân, hồi chiêu hiện ở ô phím; (d) chân: bấm thì chân mọc ra, chạy nhanh khoảng 4 giây rồi thu vào; (e) **G**: chỉ thấy ô Mắt và Da, đủ 3 bị động thì báo; ngoài tổ nút Nặn khóa; (f) F1 có "Học chiêu" để thử nhanh; (g) chết rồi hồi sinh: chiêu đã học vẫn còn.

## 38. Cảnh Ra Đời bản 3 "đường truyền sụp đổ" (bước 2): trạng thái và danh sách kiểm tra

Spec `docs/superpowers/specs/2026-11-20-birth-scene-system-collapse-design.md`, plan `docs/superpowers/plans/2026-11-20-birth-scene-system-collapse.md`.

- **Sân khấu riêng** (`BirthStage`): camera + Canvas UGUI đặt xa (y=20000), Volume hậu kỳ riêng (Bloom, tách màu, méo kính, nhiễu hạt, tối viền), vẽ vào RenderTexture rồi IMGUI phủ toàn màn (che cả HUD). Không thấy slime, hang hay HUD. Tạo sân khấu lỗi thì bỏ qua cảnh và trao kết quả ngay.
- **"Độ bất ổn" 0..1** (`Instability`) điều khiển nhiễu, rung, khung vỡ và chữ vỡ. Bắt đầu cao (mất kết nối), giảm dần, về 0 khi "Kết nối ổn định".
- **Khung vỡ** (`BirthWindow`): viền chia đoạn giật, rách, nhấp nháy, góc ngoặc, nghiêng; chữ gõ từng ký tự, vỡ thành ký hiệu khi bất ổn.
- **Rút tự động:** số mệnh (`FateRoll`, 65/25/10) quyết định trước, cảnh chỉ trình diễn: 7 lá Lõi xáo, lá số mệnh bay lên rồi tự lật (nổ hạt, loé, rung theo độ hiếm), 3 lá Khuyết Điểm lật 1–2 lá.
- **Sáu Ân Điển, mỗi cái một cảnh** (`BirthGiftScene`): Túi, Nuốt, Giải Mã, Biến Hình, Sóng Dò, Phân Thân (đánh dấu "sắp có").
- **Đập tên** (`SlamPlan`, `BirthSlam`): Lõi nghiêng trái, Khuyết Điểm nghiêng phải, các Ân Điển xen kẽ; rơi vào với rung màn hình (Cực hiếm rung gấp đôi).
- **Âm thanh:** 22 hiệu ứng ElevenLabs thật (`Resources/Sfx/Birth/`, bảng ở `BirthSfx`, tạo lại bằng `tools/voice/generate_birth_sfx.sh`): gõ chữ, nhiễu, báo động, nhạc nền ù, lật bài, lộ Lõi (thường/hiếm/cực hiếm), lộ Khuyết Điểm, đập tên, chuông ổn định, mỗi Ân Điển một tiếng. Thiếu file thì im. Giọng Chi tự nhiên, 39 câu (`docs/art/echo-voice-lines.md`).
- Space/Enter sang bước kế (sau 1,5 giây), Tab nhảy tới lúc rút bài. Chạy một lần mỗi vòng; chết rồi hồi sinh không chạy lại. Co-op: mỗi người tự rút, client báo `evolve.tr.pick`, máy chủ kiểm hợp lệ.
- Test ngoài Unity: 131 test xanh (`tools/cstest/run.sh`). **Toàn bộ phần Unity của bản 3 (BirthStage, Glitch, Frames, Fx, Cards, Slam, Gifts, Audio, Scene) chưa từng biên dịch hay xem trong Unity.** Chưa làm: ô video Veo, Dị Tượng, hiệu ứng thật cho Phân Thân và các Lõi ghi "chưa có hiệu ứng".

Checklist: (a) `Evolve → Create Traits` một lần; (b) Console sạch; (c) Play: nền tối, không thấy slime/hang/HUD, nhiễu và rung lúc đầu; (d) chữ gõ có tiếng, nhạc nền ù; (e) mỗi Ân Điển một cảnh và một tiếng; (f) bài tự xáo và lật, Lõi có hiệu ứng theo độ hiếm; (g) tên đập vào nghiêng trái/phải kèm rung; (h) "Kết nối ổn định" rồi điều khiển được slime; (i) Space/Tab bỏ qua được; (j) chết rồi R: không chạy lại.

## 39. Lõi và Khuyết Điểm đợt 2: trạng thái và danh sách kiểm tra

Kế hoạch `docs/superpowers/plans/2026-11-20-traits-wave2.md`. 12 hang, kỳ ngộ, quái và kỹ năng (đã duyệt, giao cho bên làm hang): `docs/superpowers/handoff/2026-11-20-cave-encounters-brief.md`.

- **Đã có hiệu ứng (mới):** Lõi Ngụy Sắc, Màng Thẩm Thấu, Quang Hợp, Ăn Mòn; Khuyết Điểm Dễ Bị Chú Ý, Vệt Mùi, Ngại Đông, Mau Khô, Bám Bùn. Tổng cộng 7/12 Lõi, 9/12 Khuyết Điểm.
- Luật thuần `Traits/Pure/EnvTraitRules.cs` (+ `ScentTrail`), 137 test xanh ngoài Unity. Phần Unity (`PlayerTraits`, `TraitSense`, `LightShafts`, `Predator`, `Swallower`, `SurvivalState`) **chưa biên dịch trong Unity**.
- "Rêu ẩm" tạm hiểu là **hang trú**; "bùn" chưa có nên Bám Bùn chỉ tính nước nông. Tia sáng: tia nắng qua lỗ trần Hang Khởi Sinh (rơi xuống hồ: nổi trên mặt nước trong tia sáng là được); hang khác khai báo bằng `LightShafts.Register`.
- Chờ nền: Thích Nghi, Phản Chấn, Cộng Sinh, Bám Dính, Thần Tốc, Mờ Trong Tối, Lạ Vùng, Cơ Quan Mau Mòn (xem kế hoạch, mục B).

Checklist: (a) chạy `Evolve → Create Traits` lại (để lá bài bỏ chữ "chưa có hiệu ứng"); (b) Console sạch; (c) dùng menu thần (GodMenu) hoặc chơi nhiều lần để thử từng thứ: Ngụy Sắc đứng yên 2 giây thì kẻ săn mồi đi sát mới thấy; Vệt Mùi bị lần theo dù đã chạy xa; Dễ Bị Chú Ý kẻ săn mồi lảng vảng lại gần; Màng Thẩm Thấu đứng yên ở nước nông thấy no tăng tới 60%; Quang Hợp nổi trong tia nắng thấy năng lượng tăng nhanh; Ăn Mòn giữ F hòa tan nhanh hơn; Mau Khô ở cạn lâu thì chậm, chạm nước hết; Bám Bùn lên bờ còn chậm 2 giây.

## 40. Hang, loài, kỹ năng, hệ sinh thái chương 1: đã duyệt, đã giao việc

- Thiết kế (nguồn duy nhất): `docs/superpowers/specs/2026-11-20-caves-creatures-ecosystem-design.md`: 12 hang 3 tầng, mỗi hang một kỳ ngộ; 44 loài (mỗi hang 2 con mồi, 1 kẻ săn, 1 Chúa Hang), mỗi loài một kỹ năng đơn giản có họ (cho dung hợp sau); chuỗi thức ăn từng hang và liên kết giữa các hang. Tạm chưa làm nội dung dưới nước.
- Giao việc bên làm hang (Antigravity): `docs/superpowers/handoff/2026-11-20-cave-encounters-brief.md`.
- Giao việc phần lõi game cho bất kỳ phiên nào làm tiếp: `docs/superpowers/handoff/2026-11-20-core-systems-brief.md` (A kỹ năng, B hệ sinh thái, C phần thưởng kỳ ngộ, D Lõi/Khuyết Điểm còn lại, E chờ kiểm trong Unity).
- Chưa có code cho phần này.

## 41. Kỹ năng hang đợt 1 (12 kỹ năng của 3 hang đã có): trạng thái và danh sách kiểm tra

Giao việc: mục A của `docs/superpowers/handoff/2026-11-20-core-systems-brief.md`. Nhánh `claude/quirky-carson-xnvto6` (gộp từ `claude/youthful-mendel-yw0yza`).

- **Bảng dữ liệu thuần** `Swallow/Pure/CaveSkillSeed.cs`: 12 dòng (loài, kỹ năng, vai, họ, số tạm theo spec mục 4), `SkillFamily` (6 họ), `CaveSkillMath` (sát thương còn lại sau khiên/giảm sát thương, độ đẩy theo khoảng cách, cấp gene theo vai: mồi C, kẻ săn B, Chúa Hang A).
- **5 khuôn kỹ năng chủ động** (`Scripts/Abilities/`): `AreaAbility` (Ợ Chấn, Châm Choáng), `LeapAbility` (Lách Né), `PullAbility` (Lưỡi Dài), `TrapAbility` + `SpikeTrap` (Cắm Gai), `BodyStateAbility` + `BodyState` (Thu Mình, Khiên Quặng, Phát Sáng, Bám Vách). Sao ★2–3 mạnh hơn 50% (đẩy, sát thương, choáng, thời gian, tầm).
- **Bị động mới** trên `OrganSO`: `MaxHealthBonus` (Da Dày), `DamageReduction` (Vỏ Cứng), `SonarRangeMultiplier` (Sóng Dò Mạnh), trường `Family`. `SkillRules.UseOf` xếp chúng là Bị động. `OrganHost` cộng dồn; `SlimeBody` cộng máu tối đa.
- **Móc nhỏ ngoài thư mục A** (mỗi chỗ 1–2 dòng): `SurvivalState.IncomingDamage` (khiên, Thu Mình, Vỏ Cứng lọc đòn), `Prey`/`Predator` đứng yên và không cắn khi bị choáng/đẩy (`CreatureStatus`), `GodMenu` liệt kê gene trong `Resources/CaveSkills`.
- **Menu** `Evolve → Create Cave Skills` (`Editor/CaveSkillAssets.cs`): tạo/cập nhật `Gene_*`, `Skill_*`, `DropTable_*` trong `Resources/CaveSkills`. Chạy lại không nhân đôi.
- 147 test thuần xanh (thêm 10 ở `Tests/EditMode/Swallow/Pure/CaveSkillTests.cs`). **Phần Unity chưa biên dịch lần nào.**
- **Chỉnh theo chủ dự án (đợt 2):** Châm Choáng chỉ choáng, không sát thương; Phát Sáng làm kẻ săn thấy từ xa hơn ×1,5; sao chỉ mạnh hơn chút (★2 +10%, ★3 +20%, hồi chiêu −5%/−10%) và mỗi kỹ năng có một cơ chế nhỏ ở ★3 (`CaveAbility`, trường `Star3*`); bị động **không chiếm ô cơ thể** nữa mà chiếm **5 ô bị động** (`SkillRules.MaxPassives = 5`, `OrganHost` giữ theo gene, bảng Biến Hình hiện 5 ô); năng lượng theo độ mạnh: nhẹ 25, vừa 30, mạnh 35 (đầy thanh 100 đủ 3 kỹ năng, không đủ 4); gene Chúa Hang là A.
- **Chưa làm / còn chờ:**
  - Sóng Dò Mạnh: số đã có (`OrganHost.SonarRangeMultiplier`) nhưng `World/VibrationSense` (thư mục bên làm hang) cần nhân bán kính quét với số này. Một dòng, cần bên giữ `World/` đồng ý.
  - (Đã xong) Tiếng kỹ năng: 9 file `Resources/Sfx/Slime/skill_*.mp3` từ `tools/voice/generate_skill_sfx.sh`; bị động không có tiếng.
  - Gắn `DropTable_*` vào ô `drops` của từng loài trong hang (loài do bên làm hang dựng).
  - Co-op: kỹ năng của client chưa tác động lên sinh vật ở máy chủ (choáng/đẩy chỉ chạy ở máy mô phỏng sinh vật).
  - Hình kỹ năng (visual): mới có bụi gai tạm của Cắm Gai và đèn của Phát Sáng.

Checklist: (a) `Evolve → Create Cave Skills` một lần; (b) Console sạch; (c) Play, mở menu thần, bấm "Học chiêu"/"Nhận bị động" từng kỹ năng; (d) Da Dày: máu tối đa +10; Vỏ Cứng: bị cắn mất ít hơn; (e) Thu Mình: đứng im 3 giây, bị cắn mất nửa; (f) Khiên Quặng: cú cắn đầu tiên trong 5 giây không mất máu; (g) Phát Sáng: có đèn quanh slime 10 giây; (h) Lách Né: lách ngang ~2 m; (i) Lưỡi Dài: ngắm con mồi trong 6 m, nó bị kéo về trước mặt và đứng yên chốc lát; (j) Ợ Chấn: sinh vật quanh 4 m bị đẩy văng; (k) Cắm Gai: bụi gai tím ở chân, sinh vật dẫm vào mất máu, 20 giây thì mất; (l) Bám Vách: lao vào vách rồi dùng, slime bò lên; (m) Châm Choáng: con gần nhất trong 2,5 m đứng im 1 giây, không cắn.

## 42. Hình ảnh kỹ năng hang đợt 1 (tạm): trạng thái và danh sách kiểm tra

Thiết kế: `docs/superpowers/specs/2026-11-20-skill-visuals-design.md`. Tất cả dựng bằng code, không cần file hình.

- **Shader mới** `Shaders/SkillGel.shader`: viền sáng trong như thạch + tan dần bằng nhiễu có đường biên sáng (cộng sáng, tôn trọng tầm nhìn tối như `GlowParticle`).
- **Bộ dụng cụ** `Abilities/SkillFx.cs`, `SkillFxMeshes.cs`: vòng sóng, vỏ bong bóng, mảnh tinh thể xoay, lưỡi, ngòi, sao choáng, vệt mờ, hạt (một hệ hạt dùng chung). Số học (bảng màu theo họ, nhịp mở/giữ/tan, hàm nảy) ở `Swallow/Pure/SkillFxMath.cs`, 156 test thuần xanh.
- **Hình từng kỹ năng:** Ợ Chấn (2 vòng sóng + bụi), Châm Choáng (ngòi tinh thể đâm + lóe), Lách Né (vệt mờ + gió), Lưỡi Dài (lưỡi hồng dính con mồi khi bị kéo), Thu Mình (vỏ + mảnh vụn), Khiên Quặng (6–8 mảnh xoay, vỡ khi hết đòn), Phát Sáng (đom đóm + quầng ấm), Cắm Gai (7 tinh thể mọc lần lượt, tan dần cuối giờ), Bám Vách (quầng nhẹ + đá vụn rơi). Chung: sao xoay trên đầu sinh vật bị choáng (chiều cao đọc từ collider); Vỏ Cứng nháy nhẹ khi giảm đòn; ★3 Thu Mình có tia hồng, Phát Sáng có vòng lóa. Không có hình: Da Dày, Sóng Dò Mạnh.
- Màu theo họ kỹ năng (`SkillFxMath.FamilyColor`); lưỡi hồng, Phát Sáng vàng ấm.
- **Chưa làm / còn chờ:** hình thân mọc ra (`SkillVisuals`, cần prefab); mesh Blender thay khối dựng bằng code; chỉnh số theo ảnh Play; chỉ máy người dùng chiêu thấy hiệu ứng (co-op chưa); chạy lại `Evolve → Create Cave Skills` để gắn họ cho chiêu và tạo hai material trong `Resources/CaveSkills` (để shader vào bản build). **Phần Unity chưa biên dịch lần nào.**

Checklist: (a) `Evolve → Create Cave Skills` lại; (b) Console sạch, không "Sprites/Default" lạ (nếu thấy hiệu ứng trắng đục là shader chưa nạp); (c) từng kỹ năng bấm thử qua menu thần, xem có hình, không chói, không giật máy (F4 xem hiệu năng); (d) bị đánh trong lúc Khiên Quặng: mảnh vỡ ở đòn cuối; (e) choáng một con: có sao quay trên đầu và biến mất đúng lúc hết choáng; (f) Cắm Gai: tinh thể mọc dần, 20 giây sau tan; (g) đứng trong hang tối bấm Phát Sáng: quầng ấm hiện.

**Cập nhật mục 42 (bộ phận cơ thể bằng thạch slime):** lưỡi, ngòi, chân bám và vỏ Thu Mình nay là thạch của slime: `Abilities/Pseudopod.cs` dựng ống mềm (gốc phình ra, núm ở đầu, thu lại thì mỏng dần), vẽ bằng bản sao vật liệu thân slime (`SkillFx.GelMaterial`) nên cùng màu, độ bóng, độ trong. Bám Vách: chân thạch ấn lần lượt vào vách. Khiên Quặng vẫn là mảnh tinh thể. 159 test thuần xanh (thêm: độ dày chi, nhịp thò/thu). **Chưa biên dịch trong Unity.** Rủi ro cần xem: vật liệu thân có thể trong suốt và ghi độ sâu, nên chỗ chi chồng lên thân có thể lộ vệt; chỗ chi thò ra khỏi thân có thể thấy "ống" chứ chưa liền khối như thạch nối thân; nếu vậy nói mình để làm thân phình về phía chi (qua `SlimeGelAnimator`).
Checklist thêm: (h) Lưỡi Dài: lưỡi cùng màu thân, có núm, dính con mồi rồi thu về; (i) Châm Choáng: gai nhọn cùng màu thân đâm ra rồi thu; (j) Bám Vách: chân thạch ấn vào vách lần lượt; (k) Thu Mình: vỏ cùng màu thân dày lên; (l) đổi màu slime (F3) thì các bộ phận đổi theo.

**Cập nhật mục 42 (bộ phận liền thân, không vết nối):** theo bản vẽ của chủ dự án, lưỡi, gai Châm Choáng và chân Bám Vách nay được **kéo ra từ chính lưới thân slime** (`SlimeGelAnimator.Limb`): các điểm của thân hướng về mục tiêu dãn dài ra, cổ thu nhỏ và cong mượt vào thân, nên là một khối liền, cùng màu và chất liệu thân. Thu Mình: thân co nhỏ lại (`SlimeGelAnimator.Tuck`). Bỏ ống thạch riêng (`Pseudopod`) và lớp vỏ riêng. Hình dạng đã thử bằng mô phỏng 2D đúng công thức. 158 test thuần xanh. **Chưa biên dịch trong Unity.** Rủi ro: lưới thân có khoảng 640 đỉnh, lưỡi dài 6 m có thể thấy cạnh gãy khúc; các bộ phận gắn trên thân (cơ quan cũ) gần hướng kéo cũng bị kéo theo.

**Cập nhật mục 42 (không gì nằm trong thân):** chủ dự án không chấp nhận bất kỳ vết/đường nào bên trong thân slime (thân trong suốt nên thấy xuyên). Đã bỏ: vòng sáng bao thân (Khiên Quặng, Phát Sáng, nháy Vỏ Cứng, nháy khiên), vệt mờ Lách Né từ giữa thân, hạt sinh từ tâm thân. Nay: hạt bắn ra từ **ngoài** viền thân (`SkillFx.Shed`), vòng sóng Ợ Chấn và vòng lóa bắt đầu từ mép thân, gió Lách Né từ sau lưng, bẫy Cắm Gai đặt **trước mặt** slime (không dưới thân), gai Phóng Gai bắn ra từ ngoài viền, Vỏ Cứng giảm đòn thì thân rung thạch. **Kỹ năng cũ** Chạy Nước Rút (4 chân bước chéo) và Phóng Gai (6 gai trên lưng) cũng kéo ra từ chính thân (`SkillVisuals` không còn gắn model). Còn trong thân: lõi hổ phách và bọt nhỏ (thiết kế cũ của slime), chưa đụng tới.

## 43. Con mồi biết chống trả (phòng vệ): trạng thái và danh sách kiểm tra

Nhánh `claude/prey-defense` (từ `claude/quirky-carson-xnvto6`). Thiết kế: chủ dự án chọn "chống cả kẻ săn lẫn slime", bảng 21 loài đã duyệt (giữ cả ba cơ chế đặc biệt).

- **Luật thuần có test** (`Creatures/Pure/PreyDefense.cs`): chọn nước đi (thoát thân khi mối đe dọa lại gần; trả đũa khi bị tóm, bị đánh, bị dồn hoặc kiệt sức, thay cho "kiệt sức rồi đứng im"); mối đe dọa to hơn thì bị bỏ chạy sớm hơn (tầm +50%), ít dám trả đũa (1,5 lần khối vẫn đánh, 4 lần thì không), khó thoát hơn (−15% mỗi lần khối). Bảng 21 loài `PreyDefenseSeed` (độ khó 1/2/3 quyết định xác suất thoát 25/40/50% và sát thương trả đũa tối đa 3/8/15; chỉ Bọ Gai Tím và Cua Đá Nóng ở mức 3).
- **Ba cơ chế đặc biệt:** báo động lan truyền (Dế Gió: cả đàn quanh 12 m cùng chạy), kêu cứu (Chim Non bị đánh gọi Chim Hang Mẹ trong 25 m), mồi nhử (Tắc Kè Đá lột da để lại hình giả và thoát khỏi cú nuốt).
- **Nối hệ sinh thái:** `SpeciesPopulation.EscapeChance` làm số mồi bị bắt giảm tương ứng (trần 95%, không bao giờ bất khả xâm phạm); `SpeciesSO.EscapeChance` (chưa tự điền từ bảng).
- **Trong cảnh (chưa biên dịch):** `Creatures/PreyDefense.cs` gắn vào prefab con mồi. Móc nhỏ: `Prey` (chạy tăng tốc/ngoằn ngoèo/im lặng, nhận ít sát thương khi thủ thế, hỏi `PreyDefense` trước khi bị nuốt, **bỏ chạy cả khỏi kẻ săn**), `Predator` (mất mục tiêu, bị khiêu khích, bị chậm), `CreatureStatus` (làm chậm), `BodyState.Slow` (làm chậm slime), `SpikeTrap` (bẫy của con mồi cũng làm đau slime). Tài liệu cho bên làm hang: `docs/superpowers/handoff/2026-11-20-prey-defense-for-cave-team.md`.
- **Chưa làm / hạn chế:** kẻ săn trong cảnh vẫn chỉ đuổi người chơi (chưa đuổi con mồi, đó là mục B2), nên phòng vệ trước kẻ săn mới hiện trong mô hình số, trong cảnh chỉ khi kẻ săn lại gần; mồi nhử chỉ là hình giả; "chui khe/hố/xác" mới là đứng yên; thét gây choáng với người chơi chỉ đẩy; làm chậm slime dùng giảm tốc mỗi bước vật lý (cần chỉnh số khi thử); số cân bằng đều là tạm.
- 182 test thuần xanh (thêm 2 mục: luật phòng vệ, bảng dữ liệu, ghép với quần thể; 8 test quần thể cũ nay cũng chạy ngoài Unity).

Checklist (khi vào được Unity): (a) Console sạch; (b) gắn `PreyDefense` vào một prefab con mồi thử (ví dụ tên `Thỏ Lá`); (c) lại gần: con mồi chạy ngoằn ngoèo, bấm kỹ năng đuổi theo; (d) Ốc Hồ: chạm vào thì rụt vỏ, chưa nuốt được trong 3 giây; (e) Bọ Gai Tím: rải gai, bị cắn thì mất máu nhiều; (f) Dế Gió: cả đàn cùng chạy; (g) Chim Non bị đánh: Chim Hang Mẹ lao tới; (h) Tắc Kè Đá bị tóm: thoát và để lại hình giả; (i) slime nhỏ thì con mồi dám đánh trả, slime lớn thì chạy sớm.

## 44. Hệ sinh thái B1 (mô hình số liệu cân bằng): trạng thái và danh sách kiểm tra

Nhánh `claude/ecology-b1` (từ `claude/prey-defense`). Làm theo các giá trị mình đề xuất vì chủ dự án chưa đổi: tổ sinh lại 6 phút, Chúa Hang 9 phút, kẻ săn chết đói còn 1 con, di cư khi đầy 90% hoặc đói 3 phút (chuyển khoảng 20%).

- **Mô hình giàu hơn** (`Ecology/Pure/RegionPopulation.Rich.cs`, mở rộng `RegionPopulation`, mô hình cũ giữ nguyên khi vùng không có kho nguồn sống và loài không có khẩu phần): nhiều món ăn có trọng số cho mỗi loài (loài mồi hoặc kho nguồn sống); nhiều kho (rêu, nấm, quặng, xác…) mỗi kho tự mọc lại; chia phần công bằng khi nhiều loài tranh một thứ; săn **bão hòa** (mồi hiếm bị bắt ít, nên không bao giờ bị diệt hẳn); **đói thật** cho mọi loài (không đủ ăn thì chết dần, kẻ săn dừng ở sàn 1 con); sinh sản theo độ no; **tổ không bao giờ chết hẳn** (loài mất hết mà tổ còn thì sinh lại 1 con sau 6 phút, Chúa Hang 9 phút); xác chết trả lại nguồn sống; xác suất thoát của con mồi (mục 43) giảm số bị bắt.
- **Loài đi xa** (Dơi, Chim Hang Mẹ, Cáo Gió): khẩu phần có `RegionId`; chỉ với tới được một phần (hiệu suất) mồi ở hang kề, phần với tới được thì no bình thường.
- **Di cư** (`Ecology/Pure/EcologyWorld.cs`): loài quá đông hoặc đói gửi khoảng 20% sang hang kề thành "khách" (không có tổ, nên chỉ vơi dần, thành van xả khi hang quá tải). Chúa Hang không di cư.
- **Dữ liệu 11 hang** (`Ecology/Pure/CaveEcologySeed.cs`): theo chuỗi thức ăn mục 5.2 và số lượng mục 5.4 (mồi 8/10, kẻ săn 2/3, Chúa Hang 1), nối hang tạm bằng bảng tay (13 đường hầm) cho đến khi có đồ thị mê cung thật.
- **Bộ kiểm cân bằng 2 giờ chơi** (`Tests/EditMode/Ecology/`): không loài nào ra ngoài [0, sức chứa], không loài nào ở mức 0 quá 12 phút (Chúa Hang 10), mọi loài trung bình có mặt; giết hết kẻ săn → trở lại sau 30 phút; giết hết con mồi → con mồi và kẻ săn trở lại, mất mặt không quá 8 và 14 phút; giết Chúa Hang → trở lại sau 8–10 phút; kịch bản con mồi hung dữ nhất (Bọ Gai Tím thoát 50%) vẫn sống sau 2 giờ. Bảng thống kê quần thể từng loài in bằng test thủ công `Report` (Explicit).
- 204 test thuần xanh ngoài Unity. Số liệu mô phỏng: mồi trung bình 7–9, kẻ săn 1–2, Chúa Hang 1.

**Chưa làm:**
- **Nối vào Unity:** `Region.cs` vẫn dùng mô hình cũ; chưa có Diet, kho nguồn sống trong `SpeciesSO`, chưa gắn `EcologyWorld` toàn mê cung. Phần này không kiểm được khi chưa mở Unity, nên để sau.
- **Đồ thị hang thật** (đang tạm 13 đường hầm), hỏi bên làm hang qua `maze-questions.md`.
- **Hành vi trong cảnh** (kẻ săn đuổi con mồi, loài ăn xác tụ quanh xác, hai kẻ săn gặp nhau, slime leo chuỗi thức ăn): mục B2 trong bản giao việc.
- Rêu Mẹ chết thì rêu héo, Slime nuốt quặng thì Rùa thiếu ăn (hậu quả dây chuyền từ hành động của slime): cần nối với sự kiện trong cảnh.
- Mọi con số đều tạm: cân theo cảm giác khi chơi.
