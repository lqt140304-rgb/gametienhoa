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
