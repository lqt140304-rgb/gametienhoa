# PROJECT EVOLVE — Game Design Document (GDD) v0.1

> **Trạng thái:** Tài liệu thiết kế; phân biệt rõ **[ĐÃ CHỐT]** và **[ĐỀ XUẤT]**. Chưa duyệt để triển khai toàn bộ.

## 1. Tầm nhìn [ĐÃ CHỐT]

Game sinh tồn tiến hóa co-op 4–8 người. Mỗi người khởi đầu bằng cùng một slime nguyên sinh; lối sống và cơ thể về sau do người chơi quyết định. Mục tiêu: sống sót, tiến hóa, khám phá, săn quái/boss, xây tổ và cộng đồng. Không có một hình thái hoàn hảo; mỗi bộ phận và năng lực có mặt trái. PvP chưa ưu tiên; độ chênh lệch sức mạnh giữa các thành viên được chấp nhận.

## 2. Slime nguyên thủy [ĐÃ CHỐT]

- Không mắt, miệng, chân hoặc tay, không có hệ tiêu hóa hoàn chỉnh.
- Trườn và nảy chậm, thân gel mềm với lõi sinh học.
- Cảm nhận hình ảnh đen trắng, tầm nhìn ngắn; thị giác có màu phải tiến hóa mới nhận được.
- Chỉ hấp thụ được lượng nhỏ dưỡng chất qua bề mặt cơ thể.

## 3. Tạo nhân vật [ĐÃ CHỐT]

1. **Dị Năng chủ động:** rút ngẫu nhiên 1 (chấp nhận yếu/mạnh, hardcore).
2. **Thiên Phú bị động:** nhìn 3 lựa chọn ngẫu nhiên, chọn 1.
3. **Khiếm Khuyết:** nhìn 3 lựa chọn ngẫu nhiên, chọn 1; **vĩnh viễn và không thể xóa**. Có thể phát triển cơ quan để bù đắp nhưng khi cơ quan hỏng điểm yếu vẫn tồn tại.

**[ĐỀ XUẤT]** Kho Dị Năng chủ động v2.0 gồm 15 C, 12 B, 6 A và 4 S. Các bậc SS, SSS chưa phát triển. Tỷ lệ rút, hiệu ứng chi tiết và cân bằng chưa duyệt.

### Danh sách ứng viên Dị Năng v2.0 [ĐỀ XUẤT]

**C (15):** Phát Tán Bào Tử; Co Rút; Bám Dính; Phun Dịch Nhầy; Dồn Lõi; Thẩm Thấu; Nảy Bật; Tiết Mùi; Giả Xác; Tự Lột Màng; Rung Màng; Hút Ẩm; Phồng Thân; Phân Tiết Chất Đắng; Đảo Trọng Tâm.

**B (12):** Tiếng Vọng; Phóng Tơ Sinh Chất; Phản Chấn; Dịch Ăn Mòn; Dẫn Truyền Sinh Lực; Kết Tinh Bề Mặt; Bong Bóng Sinh Học; Nhiễu Loạn Giác Quan; Ngụy Trang Sinh Học; Kén Tái Cấu Trúc; Bẫy Nhầy; Hút Dinh Dưỡng Cưỡng Bức.

**A (6):** Nuốt Chửng; Ký Sinh; Tái Tạo Cưỡng Bức; Đồng Hóa; Chuyển Hóa Độc Tố; Xung Kích Sinh Học.

**S (4):** Song Sinh Thể (tách hai cơ thể); Vô Định Thể (hóa dòng dịch sống); Nghịch Nhiệt Hạch (điều khiển truyền nhiệt); Hỗn Mang Thể (tái bố trí cơ quan đã sở hữu).

Định hướng phân bậc: C/B đại trà, yếu và giới hạn rõ; A/S ít hơn nhưng khác biệt về cơ chế chứ không chỉ tăng chỉ số.

## 4. Tiến hóa mở [ĐÃ CHỐT về hướng]

- Không có cây loài hoặc class tiến hóa cố định.
- Người chơi chọn phát triển, kết hợp, thay đổi bộ phận/cơ quan.
- **Cơ quan mới phải được tìm kiếm hoặc thu thập qua khám phá, săn mồi, đánh boss**; không tự mở tất cả vì đạt đủ EXP.
- Nguồn thức ăn, kẻ thù, môi trường sống và hành vi tạo áp lực thích nghi.
- Môi trường ban đầu: **đồng bằng, hoang mạc, băng tuyết, hang động**.
- Vận động có thể tiến hóa: trườn, đi bộ, bơi, leo, lượn, bay.
- Mỗi bộ phận có lợi ích, nhược điểm và điều kiện sử dụng riêng.

**[ĐỀ XUẤT]** Mô hình cơ thể mô-đun: mỗi cơ quan có định nghĩa, instance, prefab hiển thị, tương thích vùng neo, chi phí sinh khối/năng lượng, khối lượng và khả năng nâng đỡ. Tiến hóa có thể thay đổi hình dạng dần hoặc thêm cơ quan. Dấu ấn thích nghi mở các biến thể nhưng không thay thế điều kiện tìm được cơ quan.

## 4.1. Phân cấp nguồn cơ quan [ĐÃ CHỐT]

- **D** **[ĐÃ CHỐT 2026-11-17]**: cơ quan từ sinh vật gần như vô hại, cuối chuỗi thức ăn (khu ngầm chỉ có D/C/B). Bậc thấp nhất, màu xám tối, EXP hấp thụ thấp nhất. Thứ tự bậc thành D, C, B, A, S (giá trị số của C/B/A/S giữ nguyên để asset cũ không lệch). Chi tiết: `docs/superpowers/specs/2026-11-17-underroot-survival-slice-design.md` mục 5.
- **C**: cơ quan từ sinh vật nhỏ, yếu.
- **B**: cơ quan từ sinh vật thông thường.
- **A**: cơ quan từ đại thú fantasy, sinh vật lớn.
- **S**: cơ quan từ boss hoặc cự thú huyền thoại.
- Sinh vật cao cấp vẫn có thể mang cơ quan bậc thấp hơn. Mẫu hư hỏng không thay đổi tier gốc.

## 4.1b. Quần thể theo vùng [ĐÃ CHỐT 2026-11-17]

- Khu ngầm **không dùng spawner** thả quái ngẫu nhiên. Mỗi vùng (`Region`) giữ **con số quần thể** theo loài (số lượng, sinh sản, sức chứa, thức ăn của vùng, danh sách tổ/hang).
- Gần người chơi: sinh vật hiện thân từ tổ/hang; xa: chỉ là con số, chạy phép tính sinh sản/chết đói/bị săn, không AI, không vật lý. Giết quái thì quần thể giảm thật; phá tổ thì loài đó mất nguồn sinh.
- **Kẻ săn B không bao giờ sinh gần người chơi**: nó đi vào từ Rìa Săn Mồi khi con mồi trong lãnh thổ cạn, có tín hiệu báo trước (rung lớn, sinh vật D bỏ chạy). Xác sinh vật phân hủy theo 3 giai đoạn.
- Co-op: host chạy mô phỏng quần thể. Chi tiết và bản 0.5a–0.5e: "+SPEC+" mục 6.

## 4.1c. Quy mô khu [ĐÃ CHỐT 2026-11-17]

- **Đi ngang một khu mất khoảng 8 phút** ở tốc độ trườn 1 m/s, nên một khu rộng khoảng **480 × 480 m**. Con số 120 × 120 m cũ của Hồ Khởi Sinh *(đổi ngày 2026-11-17)* trở thành **vùng lõi phẳng 130 × 130 m** (nơi xuất hiện) nằm giữa khu. Con số 600 × 600 m cho cả thế giới trong tài liệu bàn giao không còn đúng; kích thước toàn thế giới quyết định dần theo từng khu.
- Địa hình bằng Unity Terrain (đồi nhấp nhô, vách ở rìa) kèm mái hang vòm; chưa có hang chồng tầng (cần mô hình Blender sau này). Thảm thực vật rải dọc đường để ăn trên đường đi. Chi tiết: `docs/superpowers/specs/2026-11-17-underroot-survival-slice-design.md` mục 6.9.
- **[ĐÃ CHỐT 2026-11-17] 480 m là kích thước của MỘT khu**; thế giới ngầm gồm nhiều khu, **tổng kích thước chưa chốt**, các khu sau làm từng khu một. Chủ dự án đã chơi thử và thấy cỡ khu 480 m này là ổn.
- **Thực vật [ĐÃ CHỐT hướng fantasy huyền bí, phát quang; chi tiết ĐỀ XUẤT] (0.6c):** thức ăn thực vật rải dọc đường và trong từng vùng, mỗi vùng một loại cây phát quang: Rêu Lân Quang (đường mòn, vùng hoang), Nụ Hổ Phách (Bãi Mùn Rễ), Bọc Bào Tử Tím (Vườn Bào Tử, nhấp nháy), Giọt Sương Rễ (Chân Rễ, hiếm), Quả Than Hồng (Rìa Săn Mồi, thưa). Dày dọc đường mòn và ở Bãi Mùn Rễ, thưa ở vùng hoang và Rìa Săn Mồi. Mỗi cây cho ít dinh dưỡng (12) và mọc lại chậm (180 giây). Trần 380 cây mỗi khu vì đồng bộ co-op tối đa 400 cục thức ăn.
- **Bố cục khu đầu (0.6b) [ĐỀ XUẤT]:** vùng lõi ở giữa; Chân Rễ Cổ Thụ ở bắc, Bãi Mùn Rễ ở đông, Vườn Bào Tử ở tây (mỗi nơi cách tâm 170 m); Khe Thủy Mạch chạy xuống nam; Rìa Săn Mồi ở đông nam (cách điểm xuất hiện ~240 m). Mỗi nơi đứng trên một bệ phẳng y = 0; đường mòn nối các nơi, có đường vòng tới Rìa Săn Mồi.

## 4.2. Hư hỏng và tái tạo cơ quan [ĐÃ CHỐT hướng]

- **Sát thương kết hợp vị trí và lan truyền** sang cơ quan liên kết / HP cơ thể.
- **Bản thiết kế đã nghiên cứu được giữ vĩnh viễn**. Cơ quan bị phá hủy có thể tái tạo bằng tài nguyên, không buộc săn lại boss.
- **[ĐÃ CHỐT 2026-11-17] Cơ quan là gel mô phỏng**: giết quái, rơi cơ quan, slime **hấp thụ** (học bản mẫu); khi trang bị, slime dùng gel trên thân mô phỏng hình dạng cơ quan. Logic ổ gắn, EXP, cấp giữ nguyên. *(Đổi ngày 2026-11-17, trước đây là gắn nguyên khối.)* Chi tiết: `docs/superpowers/specs/2026-11-17-underroot-survival-slice-design.md` mục 5 và 6.5.
- Các công thức Damage, Integrity, Trauma, chất lượng mẫu và chi phí sửa chữa hiện **[ĐỀ XUẤT]**; xem [Biological Integrity System v1.1](BIOLOGICAL_INTEGRITY.md).

## 4.3. Rơi cơ quan khi săn [ĐÃ CHỐT]

- Cơ quan người chơi **đã có** thì **giảm tỉ lệ rơi**, không bị loại khỏi bảng rơi. Bản trùng vẫn có giá trị (hấp thụ lấy thức ăn, hoặc thay bản đang yếu).

- **Cách roll hai bước:** chọn bậc trước theo trọng số bậc (C/B/A/S), rồi chọn cơ quan trong bậc đó, nên tỉ lệ bậc đúng như bảng.
- Cơ quan đã có chỉ còn **0.8** trọng số so với cơ quan chưa có cùng bậc (chỉnh được trong Inspector). Nếu cả bậc chỉ còn cơ quan đã có thì chúng vẫn rơi bình thường.
- **[ĐÃ CHỐT 2026-11-17]** Cơ quan rơi ra là **bản mẫu để hấp thụ**; người chơi học bản mẫu rồi mô phỏng bằng gel (xem 4.2). Ở 0.5a vật phẩm cơ quan vẫn như cũ, phần gel làm ở 0.5e.
- "Đã có" tính theo cơ quan **đang gắn trên người**; tháo ra thì tính là chưa có, tỉ lệ rơi trở lại bình thường.

## 4.4. EXP tiến hóa và cấp cơ quan [ĐÃ CHỐT, thông số ĐỀ XUẤT]

**[ĐÃ CHỐT]** (chủ dự án, 2026-11-17)
- Thanh **EXP tiến hóa** là nguồn để nuôi và nâng cơ quan. **Ăn** cho **ít** EXP; **hấp thụ cơ quan** cho **nhiều** EXP (bậc càng cao càng nhiều).
- Cách chi: **chủ động + tự động**. Người chơi tự đổ EXP vào cơ quan muốn (bảng Cơ thể), hoặc đánh dấu **1 cơ quan ưu tiên** để EXP mới nhận tự chảy vào.
- Cơ quan mới là **mầm**; đủ EXP thành cơ quan hoàn chỉnh (**cấp 1**), nâng tiếp được đến **cấp 3**. Cấp không đổi bậc C/B/A/S.

**[ĐỀ XUẤT]** (đang dùng trong prototype, chỉnh được)
- Ăn: 0.2 EXP mỗi đơn vị thức ăn (`OrganHost.foodExpRate`); toàn bộ thức ăn vẫn vào thanh đói.
- Hấp thụ cơ quan (phím F, luôn chọn được): C 15, B 30, A 60, S 120 EXP.
- Chi phí: mầm → Lv1 = `GrowthExpRequired` (C 30, B 45, A 70); Lv1 → Lv2 = 1×; Lv2 → Lv3 = 2×.
- Mỗi cấp trên 1 tăng hiệu ứng +25% (`levelBonus`). Mắt cấp 2–3 nhìn xa hơn.
- EXP chưa chi được **giữ khi chết**; cơ quan rơi ra thì mất cấp và tiến độ.

## 4.5. Quyết định từ Bestiary (2026-11-17) [ĐÃ CHỐT]

Chi tiết (12 vị trí, 54 cơ quan, tier quái, 9 kiểu xã hội, kỹ năng quái, 29 loài) nằm trong `docs/BESTIARY.md` (đang ở nhánh `docs/bestiary`, chưa vào `main`). Đây là các điểm chủ dự án **đã chốt** ở đó; phần còn lại của Bestiary vẫn là đề xuất. Hiện chỉ làm phần **cơ chế**; phần **thế giới** (bestiary theo vùng, sinh thái) giữ là nháp.

**Cơ quan và kỹ năng**
- **12 vị trí cơ quan**, mỗi vị trí **1 cơ quan** (kể cả Tuyến). Số vị trí dùng được cùng lúc tùy kích thước slime: nhỏ khoảng 4, lớn tối đa khoảng 8, nên không ai lắp đủ 12. Quy tắc kích thước → số khe: **[MỞ]**.
- Cơ quan khi lên cấp mở thêm **kỹ năng chủ động**, không chỉ hiệu ứng bị động. Người chơi có **5 ô kỹ năng**, bấm phím **1–5**, tự chọn kỹ năng đặt vào ô nào. Dị Năng khởi đầu (phím Q) là **ô riêng**, không chiếm trong 5 ô này.
- **Cơ quan bậc C không có kỹ năng chủ động**, chỉ có hiệu ứng bị động; kỹ năng chủ động bắt đầu từ **bậc B**. Bốn kỹ năng gắn cơ quan C (Hút ẩm xung quanh, Rụng đuôi thoát thân, Phun bào tử, Rụt mai) đã nâng lên "bậc từ B". **[MỞ]** Nhảy vọt, Lặn cát, Pheromone báo động cần nâng tương tự; khi có hơn 5 kỹ năng thì chọn vào ô ở đâu (prototype tạm dùng bảng Cơ thể G, xem bên dưới).
- **[ĐỀ XUẤT]** Lv1 chỉ bị động; Lv2 mở kỹ năng; Lv3 nâng cấp kỹ năng (tầm, thời gian, hồi chiêu) cùng +25% bị động.
- **Triển khai prototype [ĐỀ XUẤT]** (các số chỉnh được trong asset):
  - Kỹ năng mở ở Lv2 (`AbilityUnlockLevel`), tự vào ô trống đầu tiên; đổi ô bằng bảng Cơ thể (G). Mất cơ quan thì mất kỹ năng.
  - Hồi chiêu tính theo từng cơ quan, nên đổi ô không làm mới hồi chiêu. Lv3: hồi chiêu ngắn hơn 25%, hiệu lực mạnh hơn 50%.
  - Chân giả cơ bắp (B) → **Xung Phong Húc**: lao tới lực 9, 20 năng lượng, hồi 6 giây, đủ nhanh để húc kẻ săn mồi.
  - Gai độc (B) → **Xả Gai**: bắn một chùm 5 gai (Lv3: 7 gai, mạnh hơn 50%) bay thẳng về phía hồng tâm, mỗi gai 6 sát thương cho kẻ săn mồi **và con mồi** trúng đòn (con mồi có 12 máu: một loạt gai hạ được, thưởng giống như bắt), tầm 14 m, 20 năng lượng, hồi 8 giây.
  - **Chế độ ngắm** [chủ dự án yêu cầu 2026-11-17]: **Chế độ chuột** (mặc định): con trỏ tự do, giữ chuột phải để xoay camera, kỹ năng bắn/lao **theo hướng con trỏ**. **Alt** vào **chế độ ngắm**: hồng tâm giữa màn hình, chuột xoay camera trực tiếp, camera lệch qua vai (để hồng tâm không đè lên nhân vật), kỹ năng đi theo hồng tâm. **F5** đổi **góc nhìn thứ nhất** (camera ở mắt, ẩn thân, luôn có hồng tâm). Điểm ngắm là vật đầu tiên tia từ camera chạm; nếu là mặt đất hoặc không chạm gì thì bắn ngang theo hướng đó. Bảng cơ quan, menu HUD và Menu God tự trả con trỏ.

**Quái vật**
- **Boss là bậc riêng**, tách khỏi S. S là sinh vật sống trong hệ sinh thái; Boss là trận đánh có giai đoạn, hang ổ riêng.
- Giữ đủ **6 cấp trong bậc**: Thường, Con non, Đầu đàn (Alpha), Tinh anh, Biến dị, Chúa. Quái **Biến dị** chỉ mang cơ quan của vùng mà bầy đã từng đến.
- Bảng trọng số rơi theo bậc quái (Bestiary mục 3.2) được **giữ làm mốc chơi thử**, chỉnh theo dữ liệu chơi thật.
- **Bỏ Chân chạy (A) khỏi bảng rơi của con mồi C**; nguồn đúng là đầu đàn Hươu Sừng Rung. Đã sửa trong `DropTable_Prey` và công cụ dựng scene. **[ĐÃ CHỐT]** (2026-11-17) Bảng rơi của kẻ săn mồi (Sói Gai) **siết theo mục 3.2**: bậc C/B/A/S = **45/48/7/0**, luôn rơi (trước đây 30/40/25/5).
- Hai loài đang có trong code có tên chính thức: con mồi = **Bọ Nhảy Cỏ**, kẻ săn mồi = **Sói Gai**. Sói Gai hiện là đơn độc lãnh thổ; "bầy săn 3–5 con" mới là đề xuất.
- Tuyến chỉ gắn 1 cơ quan như mọi vị trí; "build hóa học" là vai trò được chia trong bầy.

**Co-op**
- Máu quái **A, S và Boss** nhân theo số người trong vùng: `HP = HP gốc × (0.6 + 0.2 × số người)` (4 người ×1.4, 8 người ×2.2). Quái C/B không nhân vì chúng là thức ăn.
- **Ăn xác người chơi:** loài ăn xác tìm tới xác người chơi đã **chết hẳn** (hết 120 giây cứu, mục 6.1; trong 120 giây đó đồ vẫn nằm trên xác), ăn dần, mỗi **~5 giây mất 1 cơ quan** (cộng với luật 50% mất). Đồng đội cần đuổi loài ăn xác đi trước khi nhặt đồ.

**Còn hoãn (thuộc phần thế giới):** quái thích nghi theo loại sát thương người chơi dùng, bật từ đầu hay để sau; đặt tên riêng cho từng loài hay giữ tên mô tả sinh học.

## 5. Chỉ số [ĐÃ CHỐT / ĐỀ XUẤT]

**[ĐÃ CHỐT 2026-11-17] Năm chất dinh dưỡng (0.5c)**, tên kiểu slime, ghi chú nêu chất thật: **Sợi Nhầy** (đạm), **Tinh Vôi** (canxi), **Huyết Thiết** (sắt), **Mỡ Nhờn** (chất béo), **Tinh Chất Sống** (vitamin). Mỗi chất là một kho 0 đến 100 hao dần theo thời gian và hao nhanh hơn với mỗi cơ quan đang trang bị cần chất đó. Thức ăn cho một bộ chất tùy loại (thịt: Sợi Nhầy và Huyết Thiết; Rêu Lân Quang: Tinh Chất Sống; Nụ Hổ Phách: Mỡ Nhờn; Giọt Sương Rễ: Tinh Vôi; Quả Than Hồng: Huyết Thiết; xác thối cho ít hơn nhiều).
- **Thiếu chất làm cơ thể suy yếu (dưới 40) [ĐỀ XUẤT số liệu]:** thiếu Sợi Nhầy hồi máu chậm (tới 0,4 lần); thiếu Tinh Vôi đòn đánh gây nhiều sát thương hơn (tới 1,4 lần); thiếu Huyết Thiết giảm máu tối đa (tới 0,7 lần) và hồi năng lượng chậm (tới 0,6 lần); thiếu Mỡ Nhờn nhiệt độ thân theo không khí nhanh hơn (tới 1,8 lần); thiếu Tinh Chất Sống giác quan kém (tới 0,6 lần).
- **Bệnh tật [ĐÃ CHỐT hướng, số liệu ĐỀ XUẤT] (0.5d):** ba bệnh, mỗi bệnh là một đồng hồ đếm ngược tự hết và hết nhanh hơn khi có cách chữa. **Nhiễm Nước Đọng** (uống nước đọng bẩn): khát nhanh hơn 1,6 lần, hồi máu chậm còn 0,6; chữa bằng nghỉ ở nơi trú (nhanh gấp đôi) và uống nước sạch (nhanh gấp ba). **Thối Gel** (ăn xác thối, bị cắn nặng, đứng gần người đang bệnh; **lây**): không tự hồi máu và máu tụt chậm (dừng ở 10% tối đa, không giết người), cơ quan hồi chậm một nửa; chữa bằng ăn thức ăn giàu Tinh Chất Sống. **Mờ Bào Tử** (đứng trong đám bào tử ở Vườn Bào Tử): vòng cảm nhận rung co còn 0,6, căng thẳng tăng; tự khỏi, nhanh gấp đôi khi đủ Tinh Chất Sống. Thiếu Tinh Chất Sống làm mọi lần nhiễm dễ xảy ra tới 3 lần.
- **Cơ quan cần chất [ĐÃ CHỐT hướng (b)]:** Mắt cần Tinh Chất Sống, Chân cần Sợi Nhầy, Da cần Mỡ Nhờn, Lưng (gai, vỏ) cần Tinh Vôi (đặt được riêng cho từng cơ quan). Thiếu chất thì **không trang bị được**; thiếu kéo dài (dưới 25) thì cơ quan yếu dần (còn 20% sức) rồi tan sau khoảng 5 phút, ăn đủ thì hồi dần. Cơ quan tan rơi ra đất như vật nhặt được (bản mẫu thật ở 0.5e). Huyết Thiết chưa gắn cơ quan nào (chưa có ô tim) mà tác động thẳng lên thân.

**[ĐÃ CHỐT]** Máu; thanh đói; thanh năng lượng; thanh kinh nghiệm tiến hóa; thanh nhiệt độ.

**[ĐÃ CHỐT 2026-11-17]** Thêm chỉ số **Khát** (0.5b), **Căng thẳng** 0–100 (0.5b, không có hậu quả nặng ở 100, chỉ hiệu ứng tăng dần) và **5 chất dinh dưỡng** tên kiểu slime (0.5c): **Sợi Nhầy** (đạm), **Tinh Vôi** (canxi), **Huyết Thiết** (sắt), **Mỡ Nhờn** (chất béo), **Tinh Chất Sống** (vitamin). Dinh dưỡng là điều kiện của cơ quan: thiếu kéo dài thì cơ quan yếu dần rồi tiêu biến (bản mẫu vẫn còn). Đói vẫn là calo tổng. Chi tiết: `docs/superpowers/specs/2026-11-17-underroot-survival-slice-design.md` mục 5, 6.2, 6.3.

**[ĐÃ CHỐT, đã code 0.5b]** **Khát** (0–100): chỉ bật ở scene có vùng nước (`WaterZone`), nên scene `Prototype_01` không bị khát. Giảm dần theo thời gian, nhanh hơn khi nước rút và khi trời nóng; đứng trong nước thì uống đầy lại. Nước sạch (Vũng Sinh Mệnh, Khe Thủy Mạch) và nước bẩn (vũng nước đọng ở Bãi Mùn Rễ) đều giải khát; nước bẩn chỉ được đánh dấu, bệnh do nước bẩn để 0.5d. Khát về 0 thì mất máu; khát dưới một nửa thì không hồi máu. Thân slime xám khô khi thiếu nước.

**[ĐÃ CHỐT, đã code 0.5b]** **Căng thẳng** (0–100): tăng khi bị tấn công (theo lượng sát thương), khi đói hoặc khát dưới 25%, khi thân nhiệt ngoài vùng dễ chịu; giảm khi no đủ và giảm nhanh khi ở **nơi trú** (hốc rễ, khe hẹp). Ở nơi trú năng lượng hồi gấp đôi. Hiệu ứng tăng dần, **không có hậu quả nặng ở 100**: camera rung, vòng quét rung co lại, năng lượng hồi chậm khi rất cao. Slime càng nhiều cơ quan thì càng to; to quá (từ 6 cơ quan) thì không chui lọt khe trú.

Nguồn số liệu: mọi con số (tốc độ khát, lượng uống, ngưỡng căng thẳng, hệ số nơi trú) nằm trong `SurvivalConfig` (`ScriptableObjects/SurvivalConfigSO.asset`, chỉnh được trong Inspector); thiết kế ở `docs/superpowers/specs/2026-11-17-underroot-survival-slice-design.md` mục 6.2 và 6.7.

**[ĐỀ XUẤT]** Độ ẩm (rất quan trọng cho slime), sinh khối để phát triển bộ phận, khối lượng, ổn định cấu trúc, chuyển hóa, cảm nhận, kháng môi trường, oxy theo tình huống và độc tố.

## 6. Co-op và xây dựng

**[ĐÃ CHỐT hướng]** Co-op online 4–8 người, vai trò tự hình thành nhờ tiến hóa; xây tổ và công trình theo những cơ quan hiện có.

**[ĐỀ XUẤT]** Truyền dưỡng chất, giữ ấm theo bầy, cứu hộ lõi nguy kịch, tổ dùng để lưu trữ và tiến hóa. Chưa chốt quy tắc chết/hồi sinh và đồng bộ.

## 6.1. Mạng, chết và cứu hộ

**[ĐÃ CHỐT 2026-11-17] Chết với cơ quan gel (từ 0.5e):** người chết **giữ bản mẫu** đã học nhưng mọi cơ quan gel đang mô phỏng **tan ra**; muốn mô phỏng lại phải đủ chất và EXP. Tại chỗ chết rơi **bản sao bản mẫu** để người khác nhặt và học (50% mất mỗi bản, cứu kịp trong 120 giây thì không tan, không rơi). Điều này thay cho việc rơi nguyên cơ quan ở dưới *(đổi ngày 2026-11-17; luật hiện tại trong prototype vẫn giữ đến khi làm 0.5e)*. Chi tiết: `docs/superpowers/specs/2026-11-17-underroot-survival-slice-design.md` mục 5, 6.5.

**[ĐÃ CHỐT]**
- Mỗi người có **3 mạng** (tính riêng từng người). Còn mạng thì tự hồi sinh chủ động tại điểm xuất phát, nhưng **mất cơ quan**: toàn bộ cơ quan rơi ra **tại chỗ chết**, mỗi cơ quan **50% bị mất** (tính riêng từng cái), phần còn lại nằm trên đất, ai cũng nhặt được, nhặt lại phải nuôi từ mầm. *(Đổi ngày 2026-11-17, trước đây là giữ cơ quan.)*
- Chết đủ 3 lần thì **không tự hồi sinh được nữa**; người đó thành **xác nằm tại chỗ chết**.
- Nếu có đồng đội thì có thể **cứu**. Cứu kịp thì người chơi sống lại và đồ vẫn nằm trên người.
- **Không cứu được thì đồ mới rơi ra.** Khi xác chưa chết hẳn (còn trong thời hạn cứu), đồng đội **không nhặt trực tiếp** được đồ từ xác.
- Đồ rơi ai cũng nhặt được, kể cả đồng đội. Mỗi cơ quan nhặt có **50% bị mất, tính riêng từng bộ phận**.
- Việc đồng đội lạm dụng nhau (bỏ mặc để nhặt đồ) được **chấp nhận**: game thiên về chiều sâu và hardcore, không cần quá thân thiện.
- **Thời hạn cứu: 120 giây.** Hết 120 giây mà chưa cứu được thì xác chết hẳn và đồ rơi ra.
- **Chi phí cứu kiểu chuyền máu:** 1 máu của người được cứu bằng **3 máu** của người cứu. Người cứu **chuyền liên tục** cho đến khi thấy đủ thì tự buông.
- **Hai giai đoạn cứu:** (1) cứu thường, giữ tương tác khoảng **5 giây** để đưa người chết từ 0 lên **1 máu**; (2) sau đó chuyền máu 3:1.
- Giai đoạn chuyền máu: thời gian chuyền mỗi máu **giảm dần**, máu đầu tiên mất 5 giây, giảm đều đến mức nhanh nhất **0.5 giây** mỗi máu sau khoảng **10 máu**; sau đó giữ nguyên tốc độ đó.
- Người được cứu **sống tiếp mạng vừa rồi**, không được cộng thêm mạng. Chết lần nữa thì lại thành xác.
- Người hết mạng vào **chế độ quan sát**, xem từ góc nhìn (POV) của người còn sống.

**[ĐỀ XUẤT]** (chưa duyệt)
- Cứu bị ngắt nếu người cứu bị tấn công.
- Có thể giới hạn số lần cứu hoặc cho hồi lại mạng khi qua cột mốc (hạ boss, xây tổ). Tình huống không có đồng đội (chơi một mình) hết mạng là thua.

## 7. Công nghệ

**Định hướng hiện tại:** Unity **6.6** + URP, C#, Blender, 3D Stylized Low Poly. Editor build dự kiến `6000.6.9f1`; cần ghi chính xác vào `ProjectSettings/ProjectVersion.txt` khi tạo project.

**[ĐÃ CHỐT]** (2026-11-17) **Mô hình mạng:** Netcode for GameObjects (nhánh 2.x của Unity 6.6), **một người làm chủ phòng (Host), người khác nhập IP**. Chưa dùng Unity Relay hay máy chủ riêng. Bản 0.4a chỉ đồng bộ trạng thái người chơi; thế giới chung (quái, thức ăn, cơ quan rơi do Host điều khiển) làm ở 0.4b. Xem `docs/COOP_SETUP.md`.

**[ĐỀ XUẤT]** ScriptableObjects cho dữ liệu; prefab cơ quan gắn với vùng neo động; bone/Blend Shapes cho biến dạng; Shader Graph cho chất liệu slime; Unity Input System, Netcode for GameObjects cho multiplayer. Không đồng bộ toàn bộ mesh qua mạng; đồng bộ IDs cơ quan, tham số hình thái và trạng thái.

## 8. Roadmap prototype [ĐỀ XUẤT]

- **0.1:** scene test Unity URP; slime placeholder; trườn/nảy; camera thứ ba; nhìn đen trắng; thanh chỉ số; hấp thụ qua màng.
- **0.2:** shader slime, animation gel, 3–5 cơ quan mẫu, cơ chế gắn/thay và save/load cơ bản.
- **0.3:** thử nghiệm vòng sinh tồn + khám phá cơ quan với 1 quái và 1 môi trường.
- **0.4:** thử multiplayer 2 người, rồi mở rộng 4–8 sau khi đồng bộ ổn định.

## 9. Chưa quyết

- Thông số cụ thể và xác suất rút Dị Năng.
- Danh sách Thiên Phú bị động và Khiếm Khuyết cuối cùng.
- Chi phí và điều kiện cụ thể từng cơ quan; cách tháo/thay. Mất mát khi chết đã chốt ở mục 6.1 (3 mạng, xác, cứu, đồ rơi 50/50). Đã chốt giữ blueprint và tái tạo bằng tài nguyên khi cơ quan hỏng.
- Giao diện chọn đặc tính đầu game; cách lưu thế giới. (Mô hình mạng đã chốt, xem mục 7.)
- Cân bằng nhiệt độ, độ ẩm, đói và nguồn thức ăn.

Mọi thay đổi thiết kế cần được cập nhật tại đây trước khi triển khai lớn.
