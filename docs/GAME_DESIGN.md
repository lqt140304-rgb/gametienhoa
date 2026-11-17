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

- **C**: cơ quan từ sinh vật nhỏ, yếu.
- **B**: cơ quan từ sinh vật thông thường.
- **A**: cơ quan từ đại thú fantasy, sinh vật lớn.
- **S**: cơ quan từ boss hoặc cự thú huyền thoại.
- Sinh vật cao cấp vẫn có thể mang cơ quan bậc thấp hơn. Mẫu hư hỏng không thay đổi tier gốc.

## 4.2. Hư hỏng và tái tạo cơ quan [ĐÃ CHỐT hướng]

- **Sát thương kết hợp vị trí và lan truyền** sang cơ quan liên kết / HP cơ thể.
- **Bản thiết kế đã nghiên cứu được giữ vĩnh viễn**. Cơ quan bị phá hủy có thể tái tạo bằng tài nguyên, không buộc săn lại boss.
- Các công thức Damage, Integrity, Trauma, chất lượng mẫu và chi phí sửa chữa hiện **[ĐỀ XUẤT]**; xem [Biological Integrity System v1.1](BIOLOGICAL_INTEGRITY.md).

## 4.3. Rơi cơ quan khi săn [ĐÃ CHỐT]

- Cơ quan người chơi **đã có** thì **giảm tỉ lệ rơi**, không bị loại khỏi bảng rơi. Bản trùng vẫn có giá trị (hấp thụ lấy thức ăn, hoặc thay bản đang yếu).

- **Cách roll hai bước:** chọn bậc trước theo trọng số bậc (C/B/A/S), rồi chọn cơ quan trong bậc đó, nên tỉ lệ bậc đúng như bảng.
- Cơ quan đã có chỉ còn **0.8** trọng số so với cơ quan chưa có cùng bậc (chỉnh được trong Inspector). Nếu cả bậc chỉ còn cơ quan đã có thì chúng vẫn rơi bình thường.
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
  - Gai độc (B) → **Xả Gai**: gây 12 sát thương kẻ săn mồi trong 3 m, 20 năng lượng, hồi 8 giây.

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

**[ĐÃ CHỐT]** Máu; thanh đói; thanh năng lượng; thanh kinh nghiệm tiến hóa; thanh nhiệt độ.

**[ĐỀ XUẤT]** Độ ẩm (rất quan trọng cho slime), sinh khối để phát triển bộ phận, khối lượng, ổn định cấu trúc, chuyển hóa, cảm nhận, kháng môi trường, oxy theo tình huống và độc tố.

## 6. Co-op và xây dựng

**[ĐÃ CHỐT hướng]** Co-op online 4–8 người, vai trò tự hình thành nhờ tiến hóa; xây tổ và công trình theo những cơ quan hiện có.

**[ĐỀ XUẤT]** Truyền dưỡng chất, giữ ấm theo bầy, cứu hộ lõi nguy kịch, tổ dùng để lưu trữ và tiến hóa. Chưa chốt quy tắc chết/hồi sinh và đồng bộ.

## 6.1. Mạng, chết và cứu hộ

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
- Giao diện chọn đặc tính đầu game; cách lưu thế giới, topology multiplayer.
- Cân bằng nhiệt độ, độ ẩm, đói và nguồn thức ăn.

Mọi thay đổi thiết kế cần được cập nhật tại đây trước khi triển khai lớn.
