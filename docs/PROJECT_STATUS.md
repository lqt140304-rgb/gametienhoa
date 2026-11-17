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
| `Dev/` | `GodMenu` | Menu thử nghiệm (F1), tự thêm khi Play; chỉ có trong Editor và bản build debug |
| `Editor/` | `PrototypeSceneBuilder`, `HudSmokeCheck` | Menu **Evolve → Build Prototype Scene** dựng scene `Scenes/Prototype_01`; `HudSmokeCheck` là kiểm tra tự động HUD (batchmode, chưa chạy thành công) |

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

## 7. Việc tiếp theo (chủ dự án chưa chọn)

1. **0.4b đợt 1 (thế giới chung) đã viết, chưa chạy thử**: Host điều khiển quái, thức ăn, cơ quan rơi; người vào phòng thấy bản sao, bị cắn, bắn trúng, ăn, nhặt (xem `docs/COOP_SETUP.md`). 0.4a (thấy nhau) **đã chạy được** trên máy chủ dự án (Netcode 2.14.0). Đợt 2 còn: cứu đồng đội, chuyền máu, quan sát. Ý ban đầu của hạng mục này: đưa quái, thức ăn, cơ quan rơi về Host điều khiển bằng `NetworkObject`/prefab, đồng bộ sát thương, rồi cứu đồng đội và quan sát. Trước đó: **bản 0.4a** (Netcode for GameObjects, host + IP, người chơi thấy nhau qua tin nhắn có tên, không dùng `NetworkObject`) cần chủ dự án cài gói, thử, và gửi lỗi biên dịch nếu có. Mục tiêu gốc của hạng mục này (`GAME_DESIGN.md` mục 8): Netcode for GameObjects, thử 2 người, rồi làm cứu đồng đội, chuyền máu, chế độ quan sát theo mục 6.1. Dữ liệu đã tách sẵn (`SurvivalState`, `SurvivalStats.All`, `OrganHost`) để đồng bộ ID cơ quan và trạng thái. Nên xin ý kiến chủ dự án về mô hình mạng (host/client hay dedicated) trước khi làm.
2. **Hệ thống hư hỏng cơ quan (BIS)** theo `BIOLOGICAL_INTEGRITY.md` mục H: mắt, chân, giáp; vùng trúng đòn; Integrity; blueprint; tái tạo tại tổ. Chơi một người cũng làm được. Công thức còn là đề xuất, cần hỏi chủ dự án trước.
3. Quay lại phản hồi gần nhất của chủ dự án để chỉnh: độ sáng quanh nhân vật (`blindLightIntensity` 8), tầm quét, độ khó kẻ săn mồi và hoang mạc.
4. Các mục còn lại: Thiên Phú, Khiếm Khuyết, màn hình tạo nhân vật, Dị Năng theo danh sách v2.0, lưu/tải, tổ, boss; cập nhật `README.md`.
