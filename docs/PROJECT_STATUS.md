# PROJECT EVOLVE: Tình hình dự án (bàn giao)

> Cập nhật: 2026-11-17, sau pull request #14. Dành cho một phiên làm việc mới, để nắm nhanh dự án đang ở đâu, cách làm việc và việc tiếp theo.
> Repo: `lqt140304-rgb/gametienhoa`, nhánh chính `main`.

## 1. Dự án là gì

Game **sinh tồn tiến hóa 3D low poly, co-op 4–8 người**, làm bằng **Unity 6.6 (`6000.6.9f1`) + URP**, C#, model bằng Blender. Người chơi bắt đầu là một slime nguyên sinh: không mắt, không chân, nhìn đen trắng. Họ tiến hóa bằng cách **săn sinh vật để lấy cơ quan** rồi gắn lên cơ thể. Không có cây tiến hóa cố định.

**Tài liệu thiết kế** (đọc trước khi làm tính năng lớn):

| File | Nội dung | Ghi chú |
|---|---|---|
| `docs/GAME_DESIGN.md` | GDD v0.1, có đánh dấu **[ĐÃ CHỐT]** và **[ĐỀ XUẤT]** | **Nguồn thiết kế chính.** Do chủ dự án viết. |
| `docs/BIOLOGICAL_INTEGRITY.md` | Hệ thống hư hỏng và tái tạo cơ quan (BIS v1.1) | Nguyên tắc đã chốt, công thức còn là đề xuất |
| `docs/GDD.md` | Bản thiết kế cũ, kèm "Câu hỏi cần chốt" | Cũ hơn `GAME_DESIGN.md` |
| `docs/ROADMAP.md` | Lộ trình 0.1 → 0.5 và checklist trạng thái | |
| `docs/PROTOTYPE_01_SETUP.md` | Cách dựng scene, bảng phím, cách chỉnh thông số | |

**Quyết định thiết kế đã chốt** (trong `GAME_DESIGN.md` và `BIOLOGICAL_INTEGRITY.md`):
- Tạo nhân vật: rút ngẫu nhiên 1 Dị Năng chủ động; chọn 1 Thiên Phú trong 3; chọn 1 Khiếm Khuyết trong 3 (vĩnh viễn).
- Cơ quan phải tìm hoặc săn mới có, không mở khóa bằng EXP.
- Nguồn cơ quan theo bậc: **C** từ sinh vật nhỏ, **B** từ sinh vật thường, **A** từ đại thú, **S** từ boss. Sinh vật mạnh vẫn có thể mang cơ quan bậc thấp.
- Sát thương tính theo vị trí trúng đòn và lan sang bộ phận liên kết. Bản thiết kế (blueprint) cơ quan **giữ vĩnh viễn**; cơ quan bị phá hủy có thể tái tạo bằng tài nguyên.
- Chỉ số: máu, đói, năng lượng, EXP tiến hóa, nhiệt độ.
- **Rơi cơ quan (mục 4.3):** bốc hai bước, bậc trước rồi cơ quan trong bậc. Cơ quan đang gắn trên người vẫn rơi được nhưng trọng số chỉ còn ×0.8.
- **Mạng, chết, cứu (mục 6.1):** 3 mạng mỗi người; hết mạng thì thành xác, đồng đội có 120 giây để cứu. Cứu gồm 5 giây kéo về 1 máu, rồi chuyền máu theo tỉ lệ 3:1, càng về sau càng nhanh (từ 5 giây xuống 0.5 giây mỗi máu). Không cứu kịp thì đồ rơi ra, mỗi cơ quan có 50% bị mất. Người hết mạng chuyển sang xem từ góc nhìn đồng đội.

## 2. Cách làm việc với chủ dự án

- Chủ dự án dùng **Windows + PowerShell**, repo ở **`D:\gametienhoa`**, viết bằng tiếng Việt. Họ mới làm quen với Git và Unity, nên cần hướng dẫn **từng bước**, mỗi lệnh một khối code.
- **Quy trình mỗi thay đổi:**
  1. Agent code trên nhánh `claude/...` → mở pull request → chủ dự án bảo "merge pull request #N" → agent merge.
  2. Chủ dự án: trong Unity bấm Ctrl+S → `git add .` → `git commit` → `git pull --no-edit` → (nếu cần) **Evolve → Build Prototype Scene → Dựng lại** → Play → báo phản hồi.
  3. Chủ dự án commit scene và asset đã dựng lại → `git push`.
- Môi trường cloud của agent **không có Unity và không có trình biên dịch C#**. Code **chưa bao giờ được biên dịch trước khi gửi**. Luôn nói rõ điều này, và nhờ chủ dự án gửi ảnh chụp tab Console nếu có lỗi.
- **Các lỗi Git hay gặp với chủ dự án:**
  - Vim mở ra khi gộp code (gõ Esc, `:wq`, Enter).
  - Lần gộp chưa hoàn tất (`MERGE_HEAD exists`).
  - `push` bị từ chối vì chưa pull.

  Đã khuyên chạy `git config --global core.editor notepad` và luôn dùng `git pull --no-edit`.
- Đọc `git log origin/main` trước khi sửa: chủ dự án **cũng tự commit** (chỉnh thông số, scene, tài liệu thiết kế).

## 3. Kiến trúc code (`Assets/_Project`)

Assembly `Evolve` (thư mục `Scripts/`) và `Evolve.Editor` (thư mục `Editor/`, chỉ chạy trong Editor). Input dùng Input System, có nhánh dự phòng cho input cũ.

| Thư mục | Lớp chính | Vai trò |
|---|---|---|
| `Survival/` | `SurvivalState` (dữ liệu thuần, không phải MonoBehaviour), `SurvivalStats`, `SurvivalConfig` (SO), `SurvivalHUD` (IMGUI tạm), `KeyPressed` | Máu, đói, năng lượng, nhiệt độ; `SurvivalStats.All` là danh sách người chơi cho AI; `FoodFilter` để cơ quan lấy phần thức ăn |
| `Player/` | `SlimeController`, `ThirdPersonCamera` | Trườn, nước rút, nảy, quay mặt theo hướng đi, hồi sinh (R); camera quỹ đạo |
| `Abilities/` | `AbilitySO`, `AbilityCaster`, `BurstDashAbility` | Dị Năng: rút ngẫu nhiên từ danh sách, phím Q, hồi chiêu, tốn năng lượng |
| `Organs/` | `OrganSO`, `OrganTier`, `OrganSocket`, `OrganHost`, `OrganInteractor`, `OrganPickup`, `DropTableSO` | Cơ quan, bậc, điểm gắn, nuôi lớn bằng thức ăn, bảng hỏi lắp/thay/hấp thụ, tháo bằng G, bảng rơi đồ có trọng số |
| `Creatures/` | `Prey`, `PreySpawner`, `Predator`, `PredatorSpawner` | Con mồi bỏ chạy và mệt; kẻ săn mồi tuần tra, đuổi, cắn, bị gai và cú lao làm mất máu |
| `World/` | `Nutrient`, `MonochromeVision`, `VibrationSense`, `TemperatureZone`, `ClimateSensor` | Thức ăn, thị giác đen trắng tăng dần theo mắt, giác quan rung, vùng nhiệt độ |
| `Editor/` | `PrototypeSceneBuilder` | Menu **Evolve → Build Prototype Scene** dựng scene `Scenes/Prototype_01` |

**Lưu ý quan trọng về công cụ dựng scene:**
- Nó **tạo lại scene** từ đầu, nhưng **dùng lại** material, prefab và ScriptableObject đã có. Vì vậy đổi giá trị mặc định trong code **không ảnh hưởng** prefab hay asset cũ.
- Muốn áp giá trị mới cho asset đã có: sửa trực tiếp file YAML của asset trong repo (khi chủ dự án đã commit asset đó), hoặc bảo chủ dự án xóa asset rồi dựng lại.
- Các hàm `EnsureOrgans` và nâng cấp `Prey.prefab` có sẵn để bổ sung dữ liệu mà không ghi đè thông số đã chỉnh.

## 4. Đã làm được (đã merge vào `main`, chủ dự án đã chơi thử đến hết 0.2)

**0.1: Slime sống sót**
- Trườn **1**, nước rút (Shift) **×1.5**. Hai giá trị này do chủ dự án chỉnh, đã lưu trong scene và là mặc định trong code.
- Nảy (Space) tốn 8 năng lượng. Đứng yên thì hồi năng lượng.
- Đói **chỉ giảm khi di chuyển** (0.5 mỗi giây); nước rút tốn thêm 1.5 thức ăn mỗi giây.
- Nhìn đen trắng, sương mù 12 m. Giác quan rung: chấm xanh = thức ăn, đỏ = con mồi, tím = kẻ săn mồi.
- Bảng phím trên màn hình (H để ẩn/hiện). Phím V bật/tắt mắt để thử nghiệm.

**0.2: Tiến hóa**
- Con mồi: bỏ chạy **1.8**, phát hiện trong 6 m, chạy 5 giây thì mệt, nghỉ 1.5 giây.
- Cơ quan rơi ra theo `DropTable_Prey`: 60% có rơi, trọng số bậc C/B/A/S = 70/22/7/1. Chủ dự án nói tỉ lệ này ổn.
- Cơ quan **lớn bằng thức ăn**: mỗi cơ quan cần `GrowthFoodRequired` (C: 30, B: 45, A: 70); 50% mỗi bữa ăn nuôi cơ quan, hoặc 100% nếu đã no. Mỗi cơ quan đang lớn có thanh tiến độ màu tím.
- **Người chơi tự quyết định:** chạm cơ quan thì hiện bảng hỏi. **E** = lắp / thay (cơ quan cũ rơi ra đất) / hấp thụ (+10 thức ăn); **X** = bỏ qua. **G** mở bảng Cơ thể, phím **1–9** để tháo. Tháo ra thì mất tiến độ nuôi.

**Danh sách cơ quan hiện có:**

| Cơ quan | Bậc | Vị trí | Hiệu ứng |
|---|---|---|---|
| Mắt sơ khai | C | Mắt | Có màu, nhìn xa |
| Chân giả | C | Chân | ×1.4 tốc độ |
| Chân giả cơ bắp | B | Chân | ×1.7 tốc độ |
| Chân chạy | A | Chân | ×2.0 tốc độ |
| Màng chịu nhiệt | C | Da | Chịu nóng thêm 20°C |
| Gai độc | B | Lưng | Phản 15 sát thương khi bị cắn |

**0.3: Thế giới phản kháng** (pull request #12, đã merge; **chủ dự án CHƯA chơi thử**)
- Kẻ săn mồi:
  - Phát hiện trong 7 m (×0.4 nếu người chơi đứng yên). Đuổi 1.4, mệt sau 6 giây.
  - Cắn 15 máu kèm hất văng. Có 40 máu.
  - Bị gai đâm lại, hoặc bị lao vào với vận tốc ≥ 4, thì mất 10 máu.
  - Khi chết: +35 thức ăn, chắc chắn rơi cơ quan theo `DropTable_Predator` (trọng số 30/40/25/5).
- Nhiệt độ: thân nhiệt dần theo nhiệt độ không khí. Ngoài khoảng 5–35°C (cộng khả năng chịu nóng từ cơ quan) thì mất 0.2 máu mỗi giây cho mỗi độ vượt.
- Hoang mạc: phía đông map (x từ 25 đến 50), 50°C, thêm 12 thức ăn, 2 kẻ săn mồi. Đồng bằng có 1 kẻ săn mồi ở góc tây bắc.
- Chết: bấm R để hồi sinh, **giữ cơ quan**.

**Sau 0.3** (pull request #13, #14 của một phiên khác, đã merge; **chưa chơi thử**)
- `DropTableSO`: bốc hai bước (`Distribution()`, `Roll(isOwned)`) và `OwnedWeightMultiplier = 0.8`. Lưu ý: `Roll` giờ nhận hàm **isOwned**, không còn là hàm **allowed** như trước.
- `SurvivalState`: `MaxLives = 3`, `Lives`, `CanRespawn`, `IsCorpse`, `ResetLives()`. Mỗi lần chết trừ 1 mạng.
- R hồi sinh khi còn mạng. Hết mạng thì thành xác; bấm **Y** để chơi lại từ đầu (tạm, vì chưa có đồng đội cứu). HUD hiện số mạng.

## 5. Khác biệt giữa prototype và thiết kế (cần lưu ý)

- Prototype đang **gắn cơ quan nhặt trực tiếp**. Thiết kế đã chốt dùng **blueprint nghiên cứu một lần + tái tạo bằng tài nguyên**, và có **chất lượng mẫu (Q)** cùng Integrity/Trauma (xem BIS mục H). Phần này chưa làm.
- Chưa có: thanh **EXP tiến hóa**, độ ẩm, Thiên Phú, Khiếm Khuyết, màn hình tạo nhân vật, lưu/tải, tổ, multiplayer.
- Luật chết mới chỉ làm phần 3 mạng. Phần **cứu, chuyền máu, rơi đồ 50% khi xác chết hẳn, chế độ quan sát** cần multiplayer (0.4).
- Dị Năng duy nhất hiện có là "Bộc Phá Màng" (lao tới). Nó **không nằm** trong danh sách Dị Năng v2.0 của GDD; v2.0 có "Co Rút" và "Nảy Bật" là gần nhất.
- `README.md` vẫn ghi "chưa có project Unity", cần cập nhật. Dòng phiên bản Unity trong README cũng chưa ghi `6000.6.9f1`.
- HUD là IMGUI tạm thời, sau này thay bằng UI Toolkit.

## 6. Việc tiếp theo

1. **Hỗ trợ chủ dự án chơi thử 0.3 + mạng sống:** pull → dựng lại scene → Play. Sửa lỗi biên dịch nếu có, rồi chỉnh độ khó (kẻ săn mồi, tốc độ nóng lên) theo phản hồi.
2. Cân nhắc làm **prototype BIS** theo `BIOLOGICAL_INTEGRITY.md` mục H: mắt, chân, giáp; vùng trúng đòn; Integrity; blueprint; tái tạo tại tổ. Cần hỏi chủ dự án trước khi làm, vì công thức còn là đề xuất.
3. **0.4 Co-op** (`GAME_DESIGN.md` mục 8): thử Netcode for GameObjects với 2 người, kèm cứu đồng đội và chế độ quan sát theo mục 6.1. Phần dữ liệu đã được tách sẵn (`SurvivalState`, `SurvivalStats.All`, `OrganHost`) để đồng bộ ID cơ quan và trạng thái.
4. Nhờ chủ dự án chốt các mục trong "Chưa quyết" của `GAME_DESIGN.md`, đặc biệt là **chết thì mất gì**.
