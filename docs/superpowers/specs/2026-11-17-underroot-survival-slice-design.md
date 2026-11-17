# Thiết kế: Lát cắt dọc Hồ Khởi Sinh + sinh tồn sâu (0.5a → 0.5e)

> Ngày: 2026-11-17 · Trạng thái: **chờ chủ dự án duyệt lần cuối**
> Nguồn: thảo luận với chủ dự án + [`docs/world/UNDERROOT_WORLD_DESIGN.md`](../../world/UNDERROOT_WORLD_DESIGN.md) (bàn giao v1.2).
> Nhãn: **[ĐÃ CHỐT]** = chủ dự án quyết; **[ĐỀ XUẤT]** = Claude đề xuất, chủ dự án có thể sửa; **[BUILDER]** = để người làm tự quyết khi code.

## 1. Mục đích và lý do

Chủ dự án muốn game **thiên về sinh tồn hơn**: người chơi phải lo nhiều thứ hơn vài thanh chỉ số, và mỗi nỗi lo phải có **nguyên nhân cụ thể trong một thế giới có chiều sâu**. Hồ Khởi Sinh là nơi đầu tiên chứng minh điều đó: một khu vực mà đói, khát, kẻ săn, nơi trú, nhiệt độ, dinh dưỡng và bệnh tật đều gắn với địa điểm và sinh vật thật.

**Thành công nghĩa là:** một nhóm co-op sống được lâu trong khu hồ, phải đi lại giữa các vi sinh cảnh để kiếm đủ chất, sợ kẻ săn B vì nó *đến có lý do*, và thấy được hậu quả của lựa chọn sai (uống nước bẩn, ăn thiếu chất, bị tấn công liên tục).

## 2. Mục tiêu / Không làm / Thất bại

**Mục tiêu**
- Khu Hồ Khởi Sinh 120 × 120 m chơi được, có địa hình (blockout).
- Sinh vật đến từ quần thể của vùng, không từ máy đẻ quái.
- Thêm Khát, Nơi trú, Căng thẳng, 5 chất dinh dưỡng, 2–3 bệnh.
- Cơ quan được thể hiện bằng gel mô phỏng; khi chết rơi bản mẫu.
- Co-op 0.4a vẫn hoạt động.

**Không làm trong đợt này**
- 6 quần xã còn lại, boss bầy biến dị, lối lên mặt đất.
- Mô phỏng L1 (cấp nhóm) — chỉ có L0 (đầy đủ) và L2 (thống kê).
- Hệ "tiến hóa về chất" (loại gel, sinh khối, Morph Stability) — đang tạm gác.
- Lưu game đầy đủ / host rớt mạng.
- Art hoàn chỉnh: chỉ khối thô + màu.

**Coi là thất bại dù chạy được**
- Kẻ săn B sinh ra ngay cạnh người chơi.
- Quái chết rồi "mọc lại" ngay tại chỗ, hoặc tổ bị phá vẫn đẻ ra quái.
- Quá nhiều thanh khiến người chơi không hiểu mình đang thiếu gì (cảnh báo phải rõ, nhìn slime là đoán được).
- Thiếu chất làm cơ quan biến mất đột ngột không báo trước.
- Lag/giật khi có 4–8 người.

## 3. Ràng buộc

- Unity 6.6 (`6000.6.9f1`), URP, assembly `Evolve` / `Evolve.Editor` / `Evolve.Net`.
- Môi trường agent **không có Unity, không biên dịch được**. Mỗi bước phải nhỏ, chủ dự án biên dịch và báo lỗi Console.
- Scene dựng bằng **Evolve → Build Prototype Scene** (`PrototypeSceneBuilder`). Địa hình hồ cũng dựng bằng code Editor.
- Tránh API lỗi thời của Unity 6.6 (`GetInstanceID`, `FindObjectsByType` có tham số sort).
- Không ghi đè hệ đã chốt trong `GAME_DESIGN.md` (rơi cơ quan 2 bước, EXP/cấp, 3 mạng/cứu hộ, BIS) trừ những điểm đổi dưới đây.
- Mỗi bước làm trên nhánh `claude/...`, mở PR, chủ dự án bảo mới merge.

## 4. Phương án đã xem xét

| Phương án | Nội dung | Đánh đổi |
|---|---|---|
| A. Sinh tồn trước | Thêm chỉ số trên scene phẳng hiện có, dựng thế giới sau | Nhanh có cảm giác khó; nhưng chỉ số không có nguyên nhân, phải cân lại khi có bản đồ |
| **B. Lát cắt dọc Hồ Khởi Sinh** | Mỗi bản nhỏ thêm một lớp, tất cả nằm trong khu hồ | Mỗi chỉ số có nguyên nhân trong thế giới, chơi thử được sau mỗi bước; bước đầu chậm hơn |
| C. Kiến trúc trước | WorldState, LOD, đồng bộ, lưu game đầy đủ rồi mới làm nội dung | Nền vững; nhưng rất lâu mới chơi được, rủi ro lớn khi không biên dịch được |

**Chủ dự án chọn B.** Kiến trúc hiệu năng làm *vừa đủ* trong 0.5a (L0 + L2), không làm trước toàn bộ.

## 5. Các quyết định đã chốt

1. **[ĐÃ CHỐT] Bậc D**: sinh vật gần như vô hại, cuối chuỗi thức ăn. `OrganTier` thành **D, C, B, A, S**. Khu ngầm chỉ có D/C/B.
2. **[ĐÃ CHỐT] Cơ quan bằng gel**: giết quái → tỉ lệ rơi cơ quan → slime **hấp thụ** (học) → khi trang bị, slime **dùng gel trên thân mô phỏng hình dạng** cơ quan, không gắn nguyên khối. Logic ổ gắn, EXP, cấp giữ nguyên; đổi cách thể hiện (mô hình nhuộm màu/chất liệu gel, liền thân).
3. **[ĐÃ CHỐT] Khi chết**: người chết **giữ bản mẫu** đã học nhưng **mọi cơ quan gel đang mô phỏng tan ra**; muốn mô phỏng lại phải đủ chất và EXP. Tại chỗ chết rơi **bản sao bản mẫu** để người khác nhặt và học.
4. **[ĐÃ CHỐT] Spawner → quần thể theo vùng** (mục 6.2).
5. **[ĐÃ CHỐT] Nỗi sợ sinh tồn**: đầu game là **đói, khát, kẻ thù, nơi trú ẩn, nhiệt độ**; về lâu dài là **dinh dưỡng**; thêm chỉ số **Căng thẳng**.
6. **[ĐÃ CHỐT] Dinh dưỡng là điều kiện cơ quan**, thiếu kéo dài thì cơ quan đang có **yếu dần, cuối cùng tiêu biến**, có cảnh báo (phương án b).
7. **[ĐÃ CHỐT] 5 chất, tên kiểu slime**:

| Tên trong game | Chất thật | Thiếu thì | Điều kiện cơ quan | Nguồn [ĐỀ XUẤT] |
|---|---|---|---|---|
| **Sợi Nhầy** | Đạm | Cơ yếu, hồi máu chậm | Cơ, chi vận động, miệng/hàm | Thịt sinh vật D/C, côn trùng |
| **Tinh Vôi** | Canxi | Xương giòn, dễ gãy | Xương, gai, vỏ | Vỏ ốc, xương, khoáng đá vôi/nước ngầm |
| **Huyết Thiết** | Sắt | Máu tối đa giảm, mau mệt | Tim, tuần hoàn, sức bền | Máu con mồi, rêu khoáng đỏ |
| **Mỡ Nhờn** | Chất béo | Lạnh nhanh, cạn dự trữ | Lớp mỡ chịu lạnh, cơ quan tích trữ | Ấu trùng, sinh vật béo, hạt nấm |
| **Tinh Chất Sống** | Vitamin | Giác quan kém, dễ bệnh | Mắt, khứu giác, cảm nhận | Rong, nấm, thực vật |

Tooltip ghi chất thật tương ứng. Thiếu chất thì gel đổi màu/chất liệu (thiếu Tinh Vôi: xương mô phỏng mờ đục, nứt; thiếu Huyết Thiết: gel nhạt).

8. **[ĐÃ CHỐT] Đói** vẫn là calo tổng; 5 chất là "chất lượng" bữa ăn.

## 6. Thiết kế theo từng bản

### 6.1. Bản 0.5a — Địa hình hồ, bậc D, quần thể

**Địa hình** (dựng trong `PrototypeSceneBuilder` hoặc builder mới cho scene `Underroot_Lake`):
- Khu lõi 120 × 120 m, mở và thoáng, trần rễ phía trên.
- Vi sinh cảnh [ĐỀ XUẤT, theo bàn giao 4.1]: Vũng Sinh Mệnh (spawn, nước nông), Chân Rễ Cổ Thụ (hốc trú), Bãi Mùn Rễ, Vườn Bào Tử, Khe Thủy Mạch (2 hốc sâu), Rìa Săn Mồi (lối vào của B).
- Nước nông là vùng trigger (đi chậm, uống được); hốc sâu [BUILDER] chặn hoặc làm chậm mạnh.
- **Nhiệt độ** [quyết định của Claude]: dùng `TemperatureZone` đã có — hốc nước sâu và Chân Rễ lạnh, Bãi Mùn Rễ ấm (phân hủy sinh nhiệt), nơi trú ấm hơn bên ngoài.
- **Scene** [quyết định của Claude]: scene mới `Underroot_Lake`, scene phẳng `Prototype_01` giữ lại để thử; co-op (F2) và GodMenu (F1) tự thêm khi Play nên chạy ở cả hai.
- 4–8 điểm spawn co-op không chồng collider, gần nhau trong tầm cảm nhận đầu game.

**Bậc D**: thêm `D` vào `OrganTier` (màu xám tối, EXP hấp thụ thấp nhất). [BUILDER] Thêm vào đầu enum sẽ đổi giá trị số đã serialize — phải xử lý để asset cũ không lệch bậc (ví dụ thêm vào cuối enum với giá trị rõ ràng, hoặc gán giá trị số tường minh).

**Quần thể theo vùng** (thay `PreySpawner`/`PredatorSpawner` trong khu hồ):
- `Region` (MonoBehaviour hoặc dữ liệu) giữ cho từng loài: số lượng hiện có, tốc độ sinh sản, sức chứa tối đa; lượng thức ăn của vùng; danh sách tổ/hang trú.
- **L0**: khi có người chơi trong bán kính kích hoạt, sinh vật được hiện thân từ **tổ/hang trú** của vùng, số lượng lấy từ con số quần thể. Ra khỏi bán kính thì cất lại thành số (không mất con nào).
- **L2**: không ai gần → mỗi vài giây chạy phép tính: sinh sản theo thức ăn, chết đói nếu thiếu, kẻ săn ăn bớt con mồi. Không AI, không vật lý.
- Giết quái → quần thể giảm thật. Tổ bị phá → loài đó ở vùng không còn nguồn sinh cho đến khi có tổ mới [ĐỀ XUẤT: tổ tự tái lập sau một thời gian dài nếu quần thể còn].
- **Kẻ săn B không bao giờ sinh gần người chơi**: nó *đi vào* từ Rìa Săn Mồi khi con mồi trong lãnh thổ của nó cạn; có tín hiệu báo trước (rung động lớn, sinh vật D bỏ chạy).
- Nội dung tối thiểu: 1 loài D, 1 loài C, 1 kẻ săn B. Có thể tái dùng `Prey`/`Predator` hiện có, đổi cách sinh.
- Co-op: **host** chạy mô phỏng quần thể; client chỉ thấy kết quả. [BUILDER] đồng bộ sinh vật qua Netcode ở mức tối thiểu.

### 6.2. Bản 0.5b — Khát, nước, nơi trú, Căng thẳng

- **Khát**: thanh mới trong `SurvivalState`, giảm theo thời gian (nhanh hơn khi nóng/nước rút). Uống ở vùng nước.
- **Nước sạch / bẩn** [ĐỀ XUẤT]: nước chảy (Khe Thủy Mạch) sạch; nước đọng gần xác/bùn thối là bẩn — giải khát nhưng có xác suất mắc bệnh (dùng ở 0.5d; trước đó chỉ đánh dấu).
- **Nơi trú ẩn**: vùng trigger (hốc rễ, khe hẹp). Ở trong: hồi năng lượng nhanh hơn, giảm Căng thẳng, kẻ săn B không theo vào được nếu khe hẹp hơn kích thước nó.
- **Căng thẳng** (0–100): tăng khi bị tấn công, khi đói/khát lâu, khi có cơ quan hư hỏng nặng (BIS). Giảm khi ở nơi trú, khi no đủ.
- **[ĐÃ CHỐT] Không có hậu quả nặng ở 100**: chỉ hiệu ứng tăng dần, giới hạn ở mức cao nhất.
- Hiệu ứng tăng dần [ĐỀ XUẤT]: >40 camera rung nhẹ; >60 tầm cảm nhận/rung thu hẹp; >80 hồi năng lượng chậm. Ngưỡng [BUILDER] trong `SurvivalConfig`.

### 6.3. Bản 0.5c — Dinh dưỡng

- 5 chất là 5 thanh dự trữ (0–100), giảm chậm theo thời gian và theo **mọi cơ quan đang trang bị** cần chất đó [quyết định của Claude]; tháo cơ quan ra thì không tốn chất đó nữa (một lựa chọn chiến thuật khi thiếu).
- Thiếu Mỡ Nhờn: nhiệt độ cơ thể lệch về môi trường nhanh hơn.
- Mỗi nguồn thức ăn (`Nutrient`, xác sinh vật) cho calo + một bộ chất. Mỗi vi sinh cảnh mạnh về vài chất.
- Mỗi `OrganSO` khai báo chất nó cần. **Trang bị** đòi chất đó trên một ngưỡng.
- **Thiếu kéo dài**: dưới ngưỡng → cảnh báo (gel đổi màu, thông báo HUD) → sau một thời gian cơ quan giảm hiệu quả/integrity dần → cuối cùng tiêu biến (blueprint vẫn còn để mô phỏng lại khi đủ chất). Thời gian [BUILDER], đề xuất ban đầu vài phút ở mỗi giai đoạn.
- HUD: một khối gọn 5 biểu tượng, chỉ nổi bật cái đang thiếu.

### 6.4. Bản 0.5d — Bệnh tật

- 2–3 bệnh [ĐỀ XUẤT]:
  - **Nhiễm Nước Đọng** (uống nước bẩn): khát nhanh, hồi máu chậm. Chữa: nghỉ ở nơi trú + uống nước sạch.
  - **Thối Gel** (vết thương nhiễm, ăn xác thối): máu giảm chậm, cơ quan bị thương hồi chậm. Chữa: ăn nguồn Tinh Chất Sống.
  - **Mờ Bào Tử** (Vườn Bào Tử): tầm cảm nhận thu hẹp, Căng thẳng tăng. Tự khỏi theo thời gian, nhanh hơn khi đủ Tinh Chất Sống.
- Thiếu Tinh Chất Sống thì tăng xác suất mắc bệnh.
- Lây trong co-op [ĐỀ XUẤT]: bệnh có tag "lây" thì có xác suất nhỏ lây sang slime ở rất gần trong thời gian dài. Host quyết định.

### 6.5. Bản 0.5e — Gel mô phỏng cơ quan, rơi bản mẫu

- Mô hình cơ quan khi trang bị dùng material gel của slime (màu theo slime, trong mờ), gắn liền thân; có hiệu ứng "chảy thành hình" khi trang bị [BUILDER mức độ].
- Trạng thái thiếu chất/hư hỏng phản ánh lên material gel.
- **Khi chết**: người chết giữ bản mẫu, cơ quan gel tan ra. Rơi **bản sao bản mẫu** tại chỗ chết; ai cũng nhặt được để học (người đã có bản mẫu đó nhặt thì [ĐỀ XUẤT] hấp thụ lấy EXP). Áp dụng 50% mất cho mỗi bản sao rơi và luật cứu hộ 120 s như `GAME_DESIGN.md` 6.1 (cứu kịp thì không tan, không rơi). Organ được biểu diễn là "bản mẫu đã học + trạng thái gel" thay vì vật phẩm cơ quan.

## 6.6. Đồng bộ co-op [quyết định của Claude]

- Chỉ số của mỗi slime (khát, căng thẳng, 5 chất, bệnh) tính trên máy người sở hữu slime đó và gửi trạng thái tóm tắt lên mạng theo cách `PlayerNetState` 0.4a đang làm. Game co-op, không chống gian lận.
- Thế giới (quần thể, tổ, xác, thức ăn, lây bệnh) do **host** quyết định.

## 6.7. Bổ sung đã chốt (2026-11-17) [ĐÃ CHỐT]

1. **Vết rung dấu chân** (0.5a): kẻ săn B di chuyển tạo rung động, càng gần càng rõ trên `VibrationSense`; sinh vật D bỏ chạy cũng tạo rung. Đây là tín hiệu báo trước chính của B.
2. **Xác chết thành nguồn sống** (0.5a tạo xác, 0.5c–0.5d nối chất/bệnh): xác nằm lại, qua 3 giai đoạn — **Tươi** (nhiều Sợi Nhầy, Huyết Thiết) → **Thối** (ăn dễ mắc Thối Gel; làm nước gần đó thành bẩn) → **Mùn** (tăng thức ăn/Tinh Chất Sống cho vùng). Xác do host quản lý; vùng không người chơi tính ở L2.
3. **Mỗi vi sinh cảnh có một thứ thiếu** (0.5c): không vùng nào đủ cả 5 chất + nước sạch + nơi trú. Ví dụ [ĐỀ XUẤT]: Vũng Sinh Mệnh thiếu Tinh Vôi; Bãi Mùn Rễ thiếu nước sạch; Chân Rễ thiếu thức ăn nhưng trú tốt.
4. **Kích thước slime và nơi trú** (0.5b, nối với 0.5e): mỗi cơ quan đang trang bị tăng kích thước slime; khe trú có kích thước giới hạn, slime quá to không chui lọt. Tháo bớt cơ quan để nhỏ lại.
5. **HUD tối giản** (0.5b trở đi): chỉ hiện thanh đang có vấn đề; còn lại thể hiện trên thân slime (gel nhạt, nứt, rung, đổi màu). Giữ một phím [BUILDER chọn phím] để xem bảng đầy đủ.

**Hoãn:** truyền chất giữa đồng đội và nghỉ chung giảm Căng thẳng nhanh hơn — xem xét sau 0.5e.

## 6.8. Thiết kế cơ quan (học cơ chế cấy ghép kiểu cyberpunk) [ĐÃ CHỐT]

Chỉ học cơ chế, không dùng tên/hình ảnh/nội dung của tác phẩm khác.

1. **Cơ quan là cách chơi, không chỉ chỉ số** (0.5e): mỗi cơ quan bậc B mở một **hành động** mới (ví dụ [ĐỀ XUẤT]: gai bắn, xúc tu bám trần rễ để leo, túi khí nổi trên nước sâu). Bậc C/D lo giác quan và chỉ số nền.
2. **Giới hạn cơ thể bằng hệ đã có**: không thêm thanh "sức chứa"; giới hạn là tiêu hao 5 chất + kích thước slime (6.7.4). Không có cơ chế mất kiểm soát.
3. **Đổi bố cục gel chỉ ở nơi trú an toàn** (0.5e): tháo/lắp/đổi cơ quan chỉ làm được khi ở trong vùng nơi trú; ngoài trú cơ thể cố định. Hấp thụ cơ quan rơi vẫn làm được ở mọi nơi [ĐỀ XUẤT].
4. Mỗi cơ quan có **hình dạng gel khác biệt rõ** trên thân, nhìn là biết.

**Để sau 0.5e:** cơ quan lõi (1 cái mỗi slime, tăng sức cơ quan cùng nhóm); cộng hưởng khi trang bị 2–3 cơ quan cùng loài.

## 7. Hiệu năng

- Sinh vật chỉ hiện thân (L0) gần người chơi; còn lại là số (L2).
- Giới hạn số sinh vật L0 đồng thời [BUILDER, đề xuất ~30 cho khu hồ].
- Không cấp phát bộ nhớ mỗi khung trong vòng lặp mô phỏng; tick L2 theo chu kỳ.
- Mỗi bản có một kiểm tra: 4 người co-op trong khu hồ, FPS ổn định, không lỗi Console.

## 8. Tài liệu cần cập nhật khi làm

- `GAME_DESIGN.md`: mục 4.1 (thêm D), 4.2/4.3 (gel + hấp thụ), 5 (chỉ số mới), 6.1 (rơi bản mẫu).
- `ROADMAP.md`: thêm 0.5a–0.5e.
- `docs/world/UNDERROOT_WORLD_DESIGN.md`: bản bàn giao gốc, giữ nguyên nhãn.

## 9. Câu hỏi mở

1. Tên chính thức các vi sinh cảnh và số hốc sâu.
2. Thông số cân bằng (tốc độ khát, ngưỡng thiếu chất, xác suất bệnh) — để chơi thử rồi chỉnh.
