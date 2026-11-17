# Project Evolve — Bộ phận, Quái vật & Tier (Bestiary v0.1)

Trạng thái: **nguồn thiết kế chính** (đang chốt). Ngày: 2026-11-17. Ký hiệu: **[ĐÃ CHỐT]** = đã quyết hoặc đã có trong code · **[ĐỀ XUẤT]** = chưa duyệt · **[MỞ]** = cần chốt.

**Phạm vi hiện tại [ĐÃ CHỐT]:** đang làm phần **cơ chế** (mục 1–5, 8). Phần **thế giới** (mục 6 Bestiary theo vùng, mục 7 Sinh thái) giữ nguyên là nháp, sẽ cập nhật sau khi cơ chế hoàn thành.

Tài liệu này mở rộng 4 vị trí cơ quan và 2 loại sinh vật đang có trong code thành 12 vị trí, 4 bậc quái + boss, 9 kiểu xã hội và 29 loài cho 4 vùng. Nhãn **\[ĐÃ CHỐT\]** ở mục 1–8 nghĩa là lấy từ code hiện có; phần còn lại là **\[ĐỀ XUẤT\]**.

## 1. Nền tảng từ repo

Code hiện có 6 cơ quan, 2 loại sinh vật và 1 vùng nóng; tài liệu đã chốt nguồn cơ quan theo bậc. Đây là điểm neo cho toàn bộ hệ thống bên dưới.

*Đã đối chiếu với repo `gametienhoa` ngày 2026-11-17 (`GAME_DESIGN.md`, `BIOLOGICAL_INTEGRITY.md`, `PROJECT_STATUS.md` và code `Assets/_Project`). Tài liệu này bổ sung cho `GAME_DESIGN.md`, không thay thế: điểm nào chốt ở đây thì cập nhật ngược lại `GAME_DESIGN.md`.*

**Cơ quan \[ĐÃ CHỐT\]** (`OrganSO`, mỗi vị trí chỉ 1 cơ quan):

| Cơ quan | Vị trí | Bậc | Hiệu ứng | EXP để lớn (mầm → Lv1) |
| --- | --- | --- | --- | --- |
| Mắt sơ khai | Mắt | C | Nhìn màu, nhìn xa | 30 |
| Chân giả | Chân | C | Tốc độ ×1.4 | 30 |
| Màng chịu nhiệt | Da | C | +20°C chịu nhiệt | 30 |
| Chân giả cơ bắp | Chân | B | Tốc độ ×1.7 | 45 |
| Gai độc | Lưng | B | Phản 15 sát thương khi bị cắn | 45 |
| Chân chạy | Chân | A | Tốc độ ×2 | 70 |

**Sinh vật \[ĐÃ CHỐT\]:**

| Loại | Hành vi | Chỉ số chính |
| --- | --- | --- |
| Prey (con mồi) | Lang thang, thấy người chơi trong 6 m thì chạy, mệt sau 5 s, nghỉ 1.5 s | Đi 0.5, chạy 1.8, cho 15 thức ăn, rơi đồ 60% |
| Predator (kẻ săn) | Tuần tra lãnh thổ 12 m, phát hiện 7 m (chỉ 40% nếu mục tiêu đứng yên), đuổi 6 s rồi nghỉ 2.5 s, cắn rồi lùi 1 s | HP 40, cắn 15, đẩy lùi 4, bị húc ở tốc độ ≥ 4 nhận 10, cho 35 thức ăn, rơi đồ chắc chắn và hiếm hơn |

**Quy tắc đã chốt \[ĐÃ CHỐT\]:**

- Bậc C/B/A/S, màu trắng / xanh lá / xanh dương / vàng. Bảng rơi Prey: 60% rơi, trọng số 70/22/7/1. Bảng rơi Predator (`DropTable_Predator`): 100% rơi, trọng số 30/40/25/5.
- **Cơ quan lớn bằng EXP tiến hóa**, không còn bằng thức ăn trực tiếp: mầm → Lv1 (cơ quan hoàn chỉnh) → Lv3; mỗi cấp trên 1 thêm +25% hiệu ứng. Lv1→Lv2 tốn 1× và Lv2→Lv3 tốn 2× `GrowthExpRequired`. Ăn cho 0.2 EXP mỗi đơn vị thức ăn; hấp thụ cơ quan C/B/A/S cho 15/30/60/120 EXP. Cấp không đổi bậc.
- Chết: mỗi người 3 mạng; còn mạng thì cơ quan rơi tại chỗ, mỗi cái 50% bị mất. Hết mạng thành xác, đồng đội có 120 s để cứu; không kịp thì xác chết hẳn, đồ rơi ra và mỗi cái nhặt có 50% bị mất.
- Nguồn: **C** từ sinh vật nhỏ yếu, **B** từ sinh vật thường, **A** từ đại thú, **S** từ boss/cự thú. Quái bậc cao vẫn mang cơ quan bậc thấp.
- Rơi đồ: chọn bậc trước rồi chọn cơ quan trong bậc; cơ quan đã có giữ 0.8 trọng số.
- Hệ BIS: vùng trúng đòn riêng cho từng cơ quan, Integrity, Trauma, chặt đứt (sever), chất lượng mẫu; bậc không bao giờ bị hạ.
- Vùng: đồng bằng, hoang mạc (50°C trong code), băng tuyết, hang động. Giác quan thay thế: cảm rung (`VibrationSense`).
- Vận động có thể tiến hóa: trườn, đi, bơi, leo, lượn, bay. Mỗi bộ phận phải có mặt trái.

## 2. Hệ thống bộ phận

Mở rộng từ 4 lên 12 vị trí, chia 6 hệ chức năng; mỗi cơ quan có một lợi ích, một mặt trái và một loài nguồn. Quái vật dùng **cùng bộ cơ quan** với người chơi: cơ quan nào con quái mang trên người thì đó là thứ nó có thể rơi, và cũng là nơi nó có thể bị đánh hỏng.

**Nguyên tắc thiết kế cơ quan:**

1. **Mỗi cơ quan có chi phí duy trì**: khối lượng (chậm, nặng), năng lượng tiêu hao liên tục, hoặc điểm yếu mới. Không có cơ quan "chỉ có lợi".
2. **Bậc cao khác về cơ chế, không chỉ về số**: C tăng chỉ số, B thêm một hành động, A đổi cách chơi, S mở cơ chế riêng (giống định hướng Dị Năng).
3. **Cơ quan chủ động cấp kỹ năng**: nhiều cơ quan cấp `AbilitySO` (lớp này đã có trong code). Cơ quan bị phá = khóa kỹ năng đó, áp dụng cả cho quái.
   - **[ĐÃ CHỐT]** Cơ quan khi lên cấp mở thêm **kỹ năng chủ động**, không chỉ hiệu ứng bị động. Người chơi có **5 ô kỹ năng**, bấm bằng phím số **1–5**, và tự chọn kỹ năng nào đặt vào ô nào. "Lên cấp" là cấp cơ quan của repo (mầm → Lv1 → Lv3, nuôi bằng EXP tiến hóa, mục 1). Phím 1–5 đang rỗng trong code (`KeyPressed.Digit` có sẵn nhưng chưa ai dùng).
   - **[ĐỀ XUẤT]** Lv1 chỉ có hiệu ứng bị động; **Lv2 mở kỹ năng chủ động**, **Lv3 nâng cấp kỹ năng đó** (tầm, thời gian, hồi chiêu) cùng +25% hiệu ứng bị động như hiện nay. Kỹ năng tốn năng lượng và có hồi chiêu như `AbilitySO`.
   - **[MỞ]** Cơ quan bậc C có kỹ năng chủ động không, hay chỉ từ B? **[MỞ]** Dị Năng khởi đầu (đang là phím Q, `AbilityCaster` giữ đúng 1) có chiếm 1 trong 5 ô không, hay Q vẫn riêng? **[MỞ]** Khi có hơn 5 kỹ năng thì chọn vào ô ở đâu (bảng Cơ thể G?).
4. **Ngoài / trong**: cơ quan ngoài (sừng, đuôi, cánh, chi) có thể bị chặt đứt và rơi ngay khi quái còn sống; cơ quan trong (tiêu hóa, hô hấp, tuyến, tim) chỉ thu được khi quái chết.

**12 vị trí (4 vị trí đầu đã có trong code):**

| Hệ | Vị trí | Ngoài/trong | Vai trò |
| --- | --- | --- | --- |
| Cảm giác | Mắt | Ngoài | Thị giác, màu, tầm nhìn |
| Cảm giác | Cảm quan phụ (râu, tai, hố nhiệt) | Ngoài | Rung, âm thanh, nhiệt, mùi |
| Vận động | Chân | Ngoài | Trườn, đi, chạy, leo |
| Vận động | Cánh / vây | Ngoài | Lượn, bay, bơi |
| Vận động | Đuôi | Ngoài | Cân bằng, đánh, bám |
| Tấn công | Miệng / hàm | Ngoài | Ăn, cắn, nuốt |
| Tấn công | Chi trước | Ngoài | Bắt, kẹp, chém |
| Phòng thủ | Da / màng | Ngoài (toàn thân) | Nhiệt, ẩm, ngụy trang, kháng |
| Phòng thủ | Lưng | Ngoài | Giáp, gai, bướu |
| Nội tạng | Tiêu hóa | Trong | Hiệu suất ăn, kiểu thức ăn |
| Nội tạng | Hô hấp | Trong | Oxy, nước, độ cao, khí độc |
| Nội tạng | Tuyến | Trong | Độc, tơ, nhầy, axit, bào tử |

**[ĐÃ CHỐT]** Có đủ 12 vị trí, nhưng số vị trí dùng được cùng lúc tùy kích thước (nhỏ ~4, tối đa ~8), nên không ai lắp được cả 12. Mỗi vị trí chỉ 1 cơ quan, kể cả Tuyến.

### 2.1 Cảm giác

| Cơ quan | Bậc | Vị trí | Lợi ích | Mặt trái | Loài nguồn (mục 6) |
| --- | --- | --- | --- | --- | --- |
| Mắt sơ khai \[ĐÃ CHỐT\] | C | Mắt | Nhìn màu, xa hơn | Bị chói khi ánh sáng mạnh | Bọ Nhảy Cỏ |
| Mắt kép | B | Mắt | Góc nhìn 300°, thấy chuyển động | Tầm xa ngắn hơn Mắt sơ khai | Nhện Tơ Rung |
| Mắt nhiệt | A | Mắt | Thấy thân nhiệt xuyên ngụy trang, bụi, đêm | Mù màu; bị lóa ở hoang mạc ban ngày | Cú Tuyết Trắng |
| Mắt Thấu Sinh | S | Mắt | Thấy vùng trúng đòn và vùng mẫu quan trọng của quái (giữ chất lượng mẫu cao) | Tiêu năng lượng khi bật; dễ bị phá (Alpha cao) | Hậu Mẫu Ký Sinh |
| Râu cảm rung | C | Cảm quan phụ | Tăng bán kính cảm rung gấp đôi | Nhiễu khi đứng giữa đàn | Dơi Hang |
| Tai màng | B | Cảm quan phụ | Nghe định hướng, báo quái sau lưng | Tiếng động lớn gây choáng | Hươu Sừng Rung |
| Định vị tiếng vọng | A | Cảm quan phụ | "Nhìn" bản đồ trong bóng tối bằng xung âm | Mỗi xung làm lộ vị trí cho quái có tai | Sâu Cát Khổng Lồ / Dơi Hang chúa |
| Cơ quan Từ Cảm | S | Cảm quan phụ | Thấy mạch khoáng, lối hang, vị trí đồng đội mọi nơi | Vô dụng gần quái/vùng nhiễu từ | Nhuyễn Thể Đá Hang (hiếm) / boss hang |

### 2.2 Vận động

| Cơ quan | Bậc | Vị trí | Lợi ích | Mặt trái | Loài nguồn |
| --- | --- | --- | --- | --- | --- |
| Chân giả \[ĐÃ CHỐT\] | C | Chân | ×1.4 tốc độ | Không leo được | Bọ Nhảy Cỏ |
| Chân giả cơ bắp \[ĐÃ CHỐT\] | B | Chân | ×1.7 tốc độ | Tăng đói khi di chuyển | Sói Gai |
| Chân móc leo | B | Chân | Leo vách, trần hang | Chậm trên mặt phẳng (×1.1) | Nhện Tơ Rung |
| Chân chạy \[ĐÃ CHỐT\] | A | Chân | ×2 tốc độ | Gãy chân = ngã (Integrity thấp) | Hươu Sừng Rung |
| Chân Địa Chấn | S | Chân | Dậm tạo sóng rung gây choáng vùng | Rất nặng, không nhảy, làm đất báo vị trí | Mẹ Bầy Ngàn Chân |
| Màng lượn | C | Cánh | Lượn từ trên cao, giảm sát thương rơi | Gió mạnh đẩy lệch | Kền Kền Xương (non) |
| Vây bơi | C | Cánh | Bơi nhanh ×2 trong nước | Trên cạn ×0.9 | Cá Băng Lặn |
| Cánh màng | B | Cánh | Bay ngắn (thanh năng lượng) | Thủng màng = rơi; tiêu nhiều năng lượng | Kền Kền Xương |
| Cánh lông vũ cường lực | A | Cánh | Bay dài, lao bổ nhào | Cần thân nhẹ: phạt nặng nếu có giáp lưng | Cú Tuyết Trắng (chúa) |
| Cánh Bão | S | Cánh | Vỗ cánh tạo gió đẩy quái, bay qua bão cát | Không thể ẩn nấp (tiếng ồn) | Hung Thần Kết Tinh (biến dị) |
| Đuôi cân bằng | C | Đuôi | Không bị ngã khi bị đẩy lùi | Thêm 1 vùng bị cắn | Thằn Lằn Cát |
| Đuôi rụng | C | Đuôi | Khi sắp chết, tự rụng đuôi để thoát 1 lần | Phải nuôi lại từ đầu | Thằn Lằn Cát |

### 2.3 Tấn công

| Cơ quan | Bậc | Vị trí | Lợi ích | Mặt trái | Loài nguồn |
| --- | --- | --- | --- | --- | --- |
| Miệng hút | C | Miệng | Ăn nhanh hơn hấp thụ qua màng | Chỉ ăn được mồi mềm | Vi Nhầy |
| Hàm răng cưa | B | Miệng | Cắn chủ động, gây chảy máu | Răng mẻ khi cắn giáp | Sói Gai |
| Hàm nghiền | A | Miệng | Xuyên giáp, ăn được xương/khoáng | Cắn chậm (hồi chiêu dài) | Sâu Cát Khổng Lồ |
| Họng Vực | S | Miệng | Nuốt sống mục tiêu nhỏ hơn, tiêu hóa dần | Mục tiêu trong bụng đánh từ bên trong | Nhuyễn Thể Đá Hang (chúa) |
| Xúc tu bắt | C | Chi trước | Kéo mồi nhỏ về phía mình | Lực yếu, đứt dễ | Vi Nhầy (biến thể) |
| Càng kẹp | B | Chi trước | Giữ chân mục tiêu 2 s | Chậm, làm nặng thân | Bọ Cạp Ống |
| Lưỡi hái xương | A | Chi trước | Chém cắt, dễ chặt đứt cơ quan quái | Làm giảm chất lượng mẫu nhiều hơn | Bọ ngựa hái (Hang, biến thể) |
| Đuôi roi độc | B | Đuôi | Đâm từ xa 2 m, gây độc | Cần Tuyến độc để có độc | Bọ Cạp Ống |
| Đuôi búa | A | Đuôi | Đập giáp, làm choáng | Xoay chậm, hở lưng | Tê Giác Đá |

### 2.4 Phòng thủ

| Cơ quan | Bậc | Vị trí | Lợi ích | Mặt trái | Loài nguồn |
| --- | --- | --- | --- | --- | --- |
| Màng chịu nhiệt \[ĐÃ CHỐT\] | C | Da | +20°C chịu nóng | Kém chịu lạnh | Bọ Hút Sương |
| Màng giữ ẩm | C | Da | Giảm mất ẩm 50% | Giảm hấp thụ qua màng | Vi Nhầy |
| Lớp mỡ | C | Da | +20°C chịu lạnh | Nặng, chậm 10% | Bọ Băng |
| Da đổi màu | B | Da | Ngụy trang khi đứng yên (giảm phát hiện thêm) | Vô dụng với quái có Mắt nhiệt | Thằn Lằn Cát (đầu đàn) |
| Lông băng | A | Da | +40°C chịu lạnh, kháng cắt | Quá nhiệt nhanh ở hoang mạc | Gấu Băng Hà |
| Vảy phản nhiệt | A | Da | Kháng nhiệt 50%, phản ánh sáng chói | Lấp lánh: dễ bị thấy từ xa | Hung Thần Kết Tinh |
| Da Kim Cương Sống | S | Da | Giáp toàn thân tự vá | Không hấp thụ qua màng; cần Miệng để ăn | Hung Thần Kết Tinh |
| Bướu dự trữ | C | Lưng | Trữ thêm 30 thức ăn | Bướu bị cắn = mất thức ăn trữ | Ốc Mai Đất |
| Gai độc \[ĐÃ CHỐT\] | B | Lưng | Phản 15 sát thương | Không nằm/lăn được; đồng đội khó cứu | Sói Gai |
| Tấm giáp xương | A | Lưng | Giáp lớn vùng lưng | Nặng; không dùng chung với Cánh lông vũ | Tê Giác Đá |
| Mai Thạch Hóa | S | Lưng | Thu mình thành đá, miễn sát thương 4 s | Không nhìn, không di chuyển khi thu | Nhuyễn Thể Đá Hang (chúa) |

### 2.5 Nội tạng (chỉ thu khi quái chết)

| Cơ quan | Bậc | Vị trí | Lợi ích | Mặt trái | Loài nguồn |
| --- | --- | --- | --- | --- | --- |
| Dạ dày đơn | C | Tiêu hóa | +30% dinh dưỡng mỗi bữa | Ăn thịt thối bị độc | Nhiều loài C |
| Dạ dày nhai lại | B | Tiêu hóa | Ăn được thực vật cứng, xác cũ | Tiêu hóa chậm: dinh dưỡng vào dần | Hươu Sừng Rung |
| Dạ dày axit | A | Tiêu hóa | Ăn khoáng, xương; kháng độc thức ăn | Đói nhanh hơn 20% | Sâu Cát Khổng Lồ |
| Mang | C | Hô hấp | Thở dưới nước | Khô nhanh trên cạn | Cá Băng Lặn |
| Phổi túi | B | Hô hấp | +30% thanh năng lượng | Hít bào tử/khí độc nhiều hơn | Sói Băng |
| Lọc khí độc | A | Hô hấp | Miễn khí độc hang, bào tử | Thở nặng: hồi năng lượng chậm | Nấm Đi (chúa) |
| Phổi Sương Giá | S | Hô hấp | Thở ra vùng đóng băng, làm chậm quái | Tự làm lạnh thân | Mẹ Băng Hà Thở |
| Tuyến bào tử | C | Tuyến | Phun bào tử gây nhiễu giác quan | Gây nhiễu cả đồng đội | Nấm Đi |
| Tuyến độc | B | Tuyến | Thêm độc cho Gai, Răng, Đuôi | Hỏng tuyến = rò rỉ, tự nhiễm độc | Bọ Cạp Ống |
| Tuyến tơ | B | Tuyến | Giăng tơ bắt rung / leo / bẫy | Tốn thức ăn mỗi lần giăng | Nhện Tơ Rung |
| Tuyến axit | B | Tuyến | Phun axit làm thủng giáp | Bắn gần thì tự bị bắn | Kiến Lửa Cát |
| Tim tái sinh | A | Tuyến | Mọc lại cơ quan bị phá ngoài tổ (chậm) | Tiêu thức ăn liên tục | Kỳ Giông Mù |
| Tim Phân Đốt | S | Tuyến | Khi chết, phần thân còn lại sống thêm 20 s (kéo dài cửa sổ cứu) | Máu tối đa giảm 15% | Mẹ Bầy Ngàn Chân |
| Tuyến Ký Sinh | S | Tuyến | Cấy bào tử vào quái, điều khiển quái C/B 30 s | Quái bị điều khiển có thể quay lại cắn chủ | Hậu Mẫu Ký Sinh |

Tổng: 54 cơ quan (6 đã có + 48 đề xuất), phủ đủ 4 bậc ở hầu hết vị trí. Tên và số là nháp để bạn cắt gọt.

## 3. Tier quái vật

Quái dùng cùng thang C/B/A/S với cơ quan, cộng một bậc **Boss** riêng cho cự thú có nhiều giai đoạn. Lấy Predator trong code (HP 40, cắn 15) làm mốc **B = 1.0**.

| Bậc | Kích thước so slime | HP | Sát thương | Số cơ quan trên người | Thiết kế cho | Xuất hiện |
| --- | --- | --- | --- | --- | --- | --- |
| C — Sinh vật nhỏ | 0.5–1× | 10–25 | 0–8 | 1–2 (toàn C) | 1 slime mới sinh | Khắp nơi, theo đàn/bầy |
| B — Sinh vật thường | 1–2× | 40–90 | 12–20 | 2–4 (C + B) | 1 người có 2–3 cơ quan | Theo lãnh thổ, tổ |
| A — Đại thú | 3–6× | 250–600 | 30–50 | 4–6 (C, B, 1–2 A) | 2–4 người phối hợp | Khu vực riêng, ít cá thể |
| S — Cự thú | 8–15× | 1500–4000 | 60–100 | 6–10 (mọi bậc, 1 S) | 4–8 người | 1 con/vùng, hiếm gặp |
| Boss | 10–30× | Theo giai đoạn | Theo đòn | Nhiều cơ quan ngoài chặt được + 1–2 S | 4–8 người | Hang ổ cố định, có sự kiện thức giấc |

S và Boss khác nhau ở chỗ: **S** là sinh vật sống trong hệ sinh thái (đi lại, săn, ngủ); **Boss** là trận đánh có giai đoạn, địa hình riêng và cơ quan là vùng phá. **[ĐÃ CHỐT]** Boss là bậc riêng, tách khỏi S.

### 3.1 Cấp trong bậc (biến thể)

Mỗi loài có thể sinh ra cá thể mạnh hơn mà không đổi bậc. Đây là cách tạo độ sâu mà không cần thêm loài mới.

| Cấp | Tỉ lệ gặp | HP / sát thương | Khác biệt | Rơi đồ |
| --- | --- | --- | --- | --- |
| Thường | \~85% | ×1 | — | Bảng của loài |
| Con non | theo mùa | ×0.4 | Không tấn công; cả đàn bảo vệ nó | Ít, cơ quan chưa lớn (mẫu chất lượng thấp) |
| Đầu đàn (Alpha) | 1 mỗi bầy | ×2 | Thêm 1 kỹ năng xã hội (mục 5.7); chết → bầy tan | Chắc chắn rơi, +1 bậc tối đa |
| Tinh anh (Elite) | \~5% | ×3 | Thêm 1 cơ quan bậc cao hơn loài gốc | Chắc chắn rơi cơ quan thêm đó với tỉ lệ cao |
| Biến dị | \~1% | ×2 | Mang cơ quan của loài khác vùng (ví dụ Sói Gai có Lông băng) | Chỉ cơ quan của vùng bầy đã đến **[ĐÃ CHỐT]**; cách lấy lại cơ quan vùng cũ, không đi tắt |
| Chúa (Matriarch) | 1 mỗi tổ | ×4 | Chỉ ở loài thuộc địa; đẻ quân, không tự đánh | Cơ quan bậc cao nhất của loài |

### 3.2 Quy tắc rơi theo bậc quái

Giữ cách đã chốt: **chọn bậc trước, rồi chọn cơ quan trong bậc đó từ danh sách cơ quan mà con quái thật sự mang**; cơ quan đã có giữ 0.8 trọng số. Bậc quái chỉ định trọng số bậc:

| Bậc quái | Tỉ lệ rơi | C | B | A | S |
| --- | --- | --- | --- | --- | --- |
| C | 50% | 90 | 10 | — | — |
| B | 100% (như Predator) | 45 | 48 | 7 | — |
| A | 100%, 1–2 cơ quan | 15 | 45 | 38 | 2 |
| S | 100%, 2–3 cơ quan | 0 | 30 | 50 | 20 |
| Boss | 1 S chắc chắn + 2–4 khác | — | 30 | 60 | 10 |

- Quái chỉ rơi bậc có trong người nó; bậc trống thì chuyển trọng số xuống bậc thấp hơn.
- Cơ quan ngoài bị **chặt đứt** khi quái còn sống rơi ngay, không tính vào lượt rơi khi chết. Đây là cách "săn có chủ đích" cơ quan muốn có.
- Cơ quan bị phá hỏng (Integrity 0) trong trận chỉ cho sinh khối, không cho mẫu.
- Bảng trong code: Prey 70/22/7/1 (60% rơi) gần dòng C; **Predator 30/40/25/5 (100% rơi) lệch xa dòng B** ở trên (45/48/7/0): cho A tới 25% và S 5%. **[MỞ]** Có siết bảng Predator theo 3.2 không (chủ dự án mới chốt tỉ lệ Prey là ổn). Bảng Prey đang cho rơi Chân chạy (A) từ con mồi nhỏ; **[ĐÃ CHỐT]** bỏ A khỏi bảng con mồi.

### 3.3 Độ nguy hiểm theo số người

**[ĐÃ CHỐT]** Co-op 4–8 người: HP quái A/S/Boss nhân theo số người trong vùng: `HP = HP gốc × (0.6 + 0.2 × số người)` (4 người = ×1.4, 8 người = ×2.2). Quái C/B không nhân, vì chúng là thức ăn.

## 4. Hành vi xã hội

9 kiểu xã hội, mỗi kiểu buộc người chơi xử lý khác nhau. Kiểu xã hội là một trường của loài, tách khỏi bậc: có thể có Sói B (bầy) hay Kiến C (thuộc địa).

| Kiểu | Quy mô | Cấu trúc | Cách đánh | Khi mất thủ lĩnh | Người chơi khai thác |
| --- | --- | --- | --- | --- | --- |
| Đơn độc lãnh thổ | 1 | Vùng riêng, đánh dấu mùi | Tuần tra, đuổi kẻ xâm nhập (như Predator) | — | Tránh vùng, dụ ra biên |
| Đơn độc phục kích | 1 | Ẩn cố định (cát, tơ, trần hang) | Đứng yên, đánh một đòn mạnh, rồi ẩn lại | — | Giác quan phụ để thấy trước; ném mồi |
| Cặp | 2 | Đôi bạn đời hoặc mẹ – con | Một con vờ, một con đánh lén | Con còn lại nổi điên (+50% sát thương) | Tách đôi ra |
| Bầy săn | 3–7 | Alpha + thợ săn + trinh sát | Bao vây, luân phiên cắn; trinh sát hú gọi bầy | Tan rã, mỗi con chạy riêng | Giết Alpha trước; chặn trinh sát |
| Đàn di cư | 10–40 | Con đầu, con non ở giữa, con già sau | Chạy trốn tập thể; bị dồn thì giẫm đạp (stampede) | Đàn đi lạc, dễ bắt | Bắt con tụt lại cuối; đừng đứng trước đàn |
| Thuộc địa / tổ | 20–200 | Chúa, lính, thợ, trinh sát | Báo động bằng pheromone/rung, lính tràn ra | Chúa chết = tổ ngừng sinh, lính hung hăn 60 s rồi chết dần | Đánh lén thợ ở xa tổ; xuống tận chúa |
| Đàn bầy (swarm) | 30–100+ nhỏ | Không thủ lĩnh, theo nhau | Phủ mục tiêu, sát thương nhỏ liên tục; có thể hợp thể | — | Đòn vùng, nhiệt, bào tử |
| Cộng sinh | 2 loài | Một loài đỡ đòn, một loài nhìn/chữa | Bên này bị đánh thì bên kia giúp | Loài còn lại yếu đi rõ | Giết loài "mắt" trước |
| Ký sinh / ăn xác | 1–12 | Bám vật chủ hoặc theo sau trận đánh | Không chủ động đánh con khỏe; cướp xác, đánh con sắp chết | — | Bảo vệ xác đồng đội |

### 4.1 Vai trò trong nhóm

- **Đầu đàn / Alpha:** chọn mục tiêu cho cả nhóm, mang kỹ năng xã hội (hú, lệnh bao vây). Khác biệt ngoại hình rõ để người chơi nhận ra.
- **Trinh sát:** tầm phát hiện ×1.5, không đánh, báo vị trí. Giết trinh sát trước khi nó báo = cả nhóm không biết.
- **Lính:** giáp và sát thương cao, không rời xa tổ.
- **Thợ:** yếu, đi xa kiếm ăn, mang thức ăn về tổ. Mồi ngon và an toàn cho người chơi mới.
- **Con non:** không đánh; đụng vào con non là kéo cả đàn.
- **Chúa:** đẻ quân theo chu kỳ, là nguồn cơ quan bậc cao nhất của loài.

### 4.2 Cơ chế chung cho nhóm

- **Tinh thần (morale):** mỗi thành viên chết giảm tinh thần nhóm; dưới ngưỡng thì bỏ chạy. Giúp trận không kết thúc bằng việc giết hết.
- **Tín hiệu báo động là sóng rung:** tiếng hú, dậm chân, rung tơ đều phát `VibrationPulse`. Slime chưa có mắt vẫn "nghe" được bầy đang tới, nên giai đoạn mù vẫn chơi được.
- **Đứng yên = khó bị thấy** (đã có trong code) áp dụng cho mọi loài nhìn bằng mắt; loài săn bằng rung thì ngược lại: chạy mới bị thấy, đứng yên là an toàn.
- **Nhớ kẻ thù:** bầy/tổ bị tấn công nhớ người chơi đó trong một thời gian, tăng tuần tra gần tổ người chơi.
- **Ăn xác và xác người chơi [ĐÃ CHỐT]:** ăn dần, ~5 s mất 1 cơ quan; loài ăn xác tìm tới xác người chơi đã **chết hẳn** (hết 120 s cứu, `GAME_DESIGN.md` 6.1; trong 120 s đó đồ vẫn nằm trên xác) và có thể làm mất cơ quan rơi. Nối với quy tắc xác nằm tại chỗ và 50/50 mất cơ quan bạn đã chốt: đồng đội phải đuổi kền kền trước khi nhặt.
- **Người chơi cũng là bầy:** kỹ năng xã hội của quái (hú, giữ ấm theo bầy) có thể là cơ quan người chơi thu được, giúp co-op có vai trò.

## 5. Kỹ năng đặc biệt của quái

Mỗi kỹ năng gắn với một cơ quan cụ thể: phá cơ quan là khóa kỹ năng, nhưng làm giảm chất lượng mẫu của chính cơ quan đó. Đây là lựa chọn chiến thuật cốt lõi: **đánh an toàn hay đánh để lấy mẫu đẹp**.

Quy tắc số lượng: quái C có 0–1 kỹ năng, B có 1–2, A có 2–3, S có 3–5, Boss có kỹ năng theo giai đoạn. Quái C/B luôn có dấu hiệu báo trước (rung, phồng, kêu) tối thiểu 0.6 s.

### 5.1 Di chuyển

| Kỹ năng | Bậc từ | Cơ quan | Khắc chế |
| --- | --- | --- | --- |
| Nhảy vọt | C | Chân giả / Chân chạy | Đứng sát vách |
| Lặn cát / đào đất | C | Chi trước, Da | Cảm rung; đứng trên đá |
| Leo trần rơi xuống | B | Chân móc leo | Mắt kép / nhìn lên |
| Xung phong húc | B | Chân + Tấm giáp | Né ngang; dụ húc vào đá để choáng |
| Bổ nhào từ trên cao | A | Cánh lông vũ | Chỗ có mái che |

### 5.2 Tấn công

| Kỹ năng | Bậc từ | Cơ quan | Khắc chế |
| --- | --- | --- | --- |
| Cắn đẩy lùi \[ĐÃ CHỐT\] | B | Hàm | Đuôi cân bằng; Gai độc phản đòn |
| Cắn chảy máu | B | Hàm răng cưa | Hồi phục, rút lui sớm |
| Phun axit / nọc | B | Tuyến axit | Né sau dấu hiệu phồng họng |
| Đâm đuôi độc tê liệt | B | Đuôi roi + Tuyến độc | Chặt đuôi; kháng độc |
| Kẹp giữ | B | Càng kẹp | Đồng đội đánh vào càng để nhả |
| Nuốt sống | A | Họng Vực | Đánh từ bên trong; đồng đội đánh bụng |
| Dậm địa chấn | S | Chân Địa Chấn | Nhảy/bay đúng lúc |

### 5.3 Phòng thủ

| Kỹ năng | Bậc từ | Cơ quan | Khắc chế |
| --- | --- | --- | --- |
| Rụt mai | C | Bướu / Mai | Chờ; lật ngửa bằng đòn đập |
| Rụng đuôi thoát thân | C | Đuôi rụng | Đánh lần hai; đuôi rụng là cơ quan rơi |
| Phản gai \[ĐÃ CHỐT\] | B | Gai độc | Đánh từ xa, không húc |
| Phồng thân | B | Phổi túi | Đâm xuyên khi phồng = nổ choáng |
| Thạch hóa | S | Mai Thạch Hóa | Dùng 4 s đó để cứu đồng đội |

### 5.4 Ẩn nấp và giác quan

| Kỹ năng | Bậc từ | Cơ quan | Khắc chế |
| --- | --- | --- | --- |
| Giả xác | C | — (hành vi) | Đánh thêm một đòn trước khi ăn |
| Ngụy trang đứng yên | B | Da đổi màu | Mắt nhiệt, cảm rung |
| Săn theo rung | B | Râu / Định vị | Đứng yên; ném vật gây rung giả |
| Xung tiếng vọng | A | Định vị tiếng vọng | Trốn sau vật mềm; âm thanh lớn gây nhiễu |
| Mồi nhử ánh sáng | B | Da phát quang | Không đi theo ánh sáng trong hang |

### 5.5 Khống chế

| Kỹ năng | Bậc từ | Cơ quan | Khắc chế |
| --- | --- | --- | --- |
| Phun bào tử nhiễu giác quan | C | Tuyến bào tử | Lọc khí độc; ra khỏi đám mây |
| Giăng tơ / bẫy tơ | B | Tuyến tơ | Nhiệt đốt tơ; đồng đội kéo ra |
| Tiếng rít gây choáng | A | Tai màng (quái) | Người chơi có Tai màng bị choáng lâu hơn: mặt trái cơ quan |
| Điều khiển vật chủ | S | Tuyến Ký Sinh | Giết vật chủ nhanh; kháng bào tử |

### 5.6 Môi trường

| Kỹ năng | Bậc từ | Cơ quan | Khắc chế |
| --- | --- | --- | --- |
| Hút ẩm xung quanh | C | Màng giữ ẩm | Rời vùng; slime mất ẩm nhanh |
| Tạo vùng nóng / hội tụ nắng | S | Vảy phản nhiệt | Màng chịu nhiệt; đánh lúc mây/đêm |
| Thở sương giá | S | Phổi Sương Giá | Lớp mỡ/Lông băng; giữ ấm theo bầy |
| Đào sập hang | A | Hàm nghiền | Không đứng dưới trần nứt |

### 5.7 Xã hội (chủ yếu cho Alpha, Chúa)

| Kỹ năng | Bậc từ | Cơ quan | Khắc chế |
| --- | --- | --- | --- |
| Hú gọi bầy | B | Phổi túi | Giết trinh sát / Alpha trước |
| Lệnh bao vây | B | — (Alpha) | Đứng lưng vào vách; cùng đồng đội |
| Pheromone báo động | C | Tuyến | Giết xa tổ; mùi mạnh hơn che lấp |
| Giữ ấm theo đàn | B | Lớp mỡ | Tách đàn ra trong bão tuyết |
| Đẻ quân | A | — (Chúa) | Phá trứng/kén trước |

### 5.8 Sinh học đặc biệt

| Kỹ năng | Bậc từ | Cơ quan | Khắc chế |
| --- | --- | --- | --- |
| Hợp thể (swarm gộp thành con lớn) | C | — (loài Vi Nhầy) | Đánh vùng trước khi gộp |
| Phân thân khi bị chém | B | Tim Phân Đốt | Sát thương đập thay vì cắt |
| Mọc lại chi | A | Tim tái sinh | Phá Tim; đánh dồn dập |
| Ăn xác hồi máu | B | Dạ dày nhai lại | Dọn xác, dụ ra xa xác |
| Tiến hóa theo trận | S | — (cự thú) | Quái S nhận kháng loại sát thương bị đánh nhiều nhất: đổi cách đánh |

### 5.9 Cơ chế Boss

- **Giai đoạn theo cơ quan, không theo % HP:** mỗi cơ quan ngoài bị phá/chặt làm boss đổi hành vi (mất chân → bò, dùng đòn khác). Người chơi quyết định thứ tự giai đoạn.
- **Vùng phá có giới hạn mẫu:** phá cơ quan S ngay đầu trận làm boss yếu đi nhưng mất mẫu S đó.
- **Địa hình hang ổ:** mỗi boss có 1 yếu tố địa hình đồng đội khai thác được (trụ đá dụ húc, hồ nước làm nguội, trần sập).
- **Thức giấc:** boss ngủ cho tới khi vùng bị khai thác quá mức (mục 7.3).

## 6. Bestiary theo vùng

29 loài, mỗi vùng 7–8 loài từ C tới S/Boss. Mỗi vùng có một **áp lực chính** buộc người chơi tiến hóa theo hướng riêng, và các cơ quan giải áp lực đó rơi từ chính quái vùng bên cạnh hoặc ở rìa vùng.

### 6.1 Đồng bằng — áp lực: bị săn, cần tốc độ và mắt

Vùng khởi đầu, dạy mọi cơ chế. Hai loài đã có trong code được đặt tên ở đây.

| Loài | Bậc | Xã hội | Kỹ năng | Rơi |
| --- | --- | --- | --- | --- |
| Vi Nhầy | C | Đàn bầy 20–40 | Hợp thể khi ≥ 8 con gần nhau | Miệng hút, Màng giữ ẩm, Xúc tu bắt (C) |
| Bọ Nhảy Cỏ (= Prey) | C | Đàn di cư 5–12 | Chạy trốn, biết mệt \[ĐÃ CHỐT\]; nhảy vọt | Mắt sơ khai, Chân giả (C) |
| Ốc Mai Đất | C | Đơn độc | Rụt mai | Bướu dự trữ, Dạ dày đơn (C) |
| Sói Gai (= Predator) | B | Hiện tại đơn độc lãnh thổ \[ĐÃ CHỐT\]; đề xuất bầy săn 3–5 | Tuần tra, cắn đẩy lùi \[ĐÃ CHỐT\]; hú gọi bầy (Alpha) | Gai độc, Chân giả cơ bắp, Hàm răng cưa (B) |
| Hươu Sừng Rung | B | Đàn di cư 10–25 | Giẫm đạp, sừng phát rung báo đàn | Tai màng, Dạ dày nhai lại (B); Chân chạy (A, từ đầu đàn) |
| Tê Giác Đá | A | Đơn độc lãnh thổ | Xung phong húc, giáp lưng | Tấm giáp xương, Đuôi búa (A) |
| Mẹ Bầy Ngàn Chân | Boss | Chúa, đẻ rết con | Mỗi đốt thân là vùng phá; đốt đứt sống thành quái con; dậm địa chấn | Tim Phân Đốt, Chân Địa Chấn (S) |

### 6.2 Hoang mạc — áp lực: nóng 50°C và mất nước

Ngày nóng, đêm lạnh. Quái săn bằng rung dưới cát nên quy tắc "đứng yên" đảo ngược so với đồng bằng.

| Loài | Bậc | Xã hội | Kỹ năng | Rơi |
| --- | --- | --- | --- | --- |
| Bọ Hút Sương | C | Đàn 6–15, ra ban đêm | Hút ẩm xung quanh | Màng chịu nhiệt, Màng giữ ẩm (C) |
| Thằn Lằn Cát | C | Đơn độc | Lặn cát, rụng đuôi thoát thân | Đuôi cân bằng, Đuôi rụng (C); Da đổi màu (B, từ Tinh anh) |
| Bọ Cạp Ống | B | Đơn độc phục kích | Đâm đuôi tê liệt, kẹp giữ | Đuôi roi độc, Càng kẹp, Tuyến độc (B) |
| Kiến Lửa Cát | C (thợ) / B (lính, chúa) | Thuộc địa 50–200 | Pheromone báo động, phun axit, đẻ quân | Tuyến axit (B); Chúa: Dạ dày axit (A) |
| Kền Kền Xương | B | Ăn xác 3–8 | Theo sau trận đánh, cướp xác, bổ nhào con sắp chết | Màng lượn (C), Cánh màng (B) |
| Sâu Cát Khổng Lồ | A | Đơn độc | Săn theo rung, đội cát nuốt, đào sập | Hàm nghiền, Định vị tiếng vọng, Dạ dày axit (A) |
| Hung Thần Kết Tinh | Boss | Đơn độc, ngủ dưới đụn cát | Vỏ thủy tinh hội tụ nắng tạo vùng 80°C; giai đoạn đêm yếu hơn | Vảy phản nhiệt (A), Da Kim Cương Sống (S); Biến dị: Cánh Bão (S) |

### 6.3 Băng tuyết — áp lực: lạnh và đêm dài

Slime bị đông cứng nếu nhiệt độ thân xuống thấp; đứng sát đồng đội là cách giữ ấm. Săn theo mùi máu nên bị thương rất nguy hiểm.

| Loài | Bậc | Xã hội | Kỹ năng | Rơi |
| --- | --- | --- | --- | --- |
| Bọ Băng | C | Đàn 10–20, tụ thành cục | Giữ ấm theo đàn | Lớp mỡ (C) |
| Cá Băng Lặn | C | Đàn dưới lớp băng | Trốn xuống lỗ băng | Mang, Vây bơi (C) |
| Cú Tuyết Trắng | B | Cặp, săn đêm | Ngụy trang trắng, bổ nhào im lặng | Cánh màng (B); Mắt nhiệt (A, từ con mái); Chúa: Cánh lông vũ cường lực (A) |
| Sói Băng | B | Bầy săn 4–7 | Hú gọi bầy, lệnh bao vây, theo mùi máu | Phổi túi, Hàm răng cưa (B) |
| Voi Ma Mút Tinh Thể | A | Đàn gia đình 3–6, con non giữa | Xung phong húc, bảo vệ con non | Lớp mỡ (C), Dạ dày nhai lại (B), Lông băng (A) |
| Gấu Băng Hà | A | Đơn độc, ngủ đông | Ngủ đông trong hang; bị đánh thức thì nổi điên | Lông băng (A), Bướu dự trữ (C) |
| Mẹ Băng Hà Thở | S | Đơn độc, di chuyển khắp vùng | Thở sương giá đóng băng vùng; đi qua đâu tạo bão tuyết | Phổi Sương Giá (S), Lông băng (A) |

### 6.4 Hang động — áp lực: bóng tối và khí độc

Mắt gần như vô dụng, nên slime mù (chỉ cảm rung) có lợi thế bất ngờ. Người có mắt phải đổi sang giác quan phụ.

| Loài | Bậc | Xã hội | Kỹ năng | Rơi |
| --- | --- | --- | --- | --- |
| Dơi Hang | C | Đàn bầy 20–50 trên trần | Xung tiếng vọng (đàn), bầy ùa xuống khi bị chói | Râu cảm rung (C); Chúa: Định vị tiếng vọng (A) |
| Nấm Đi | C | Cộng sinh với Dơi Hang (sống nhờ phân) | Phun bào tử nhiễu giác quan | Tuyến bào tử (C); Chúa: Lọc khí độc (A) |
| Giun Phát Quang | C | Đàn bầy trên trần | Mồi nhử ánh sáng, dây nhầy | Da phát quang (C, mới) |
| Nhện Tơ Rung | B | Đơn độc phục kích | Cảm rung trên tơ, bẫy tơ, leo trần rơi xuống | Tuyến tơ, Mắt kép, Chân móc leo (B) |
| Kỳ Giông Mù | B | Đơn độc dưới nước ngầm | Mọc lại chi, cắn kéo xuống nước | Mang (C); Tim tái sinh (A, hiếm) |
| Bọ Ngựa Hái | B | Đơn độc phục kích | Ngụy trang đá, chém chặt chi | Lưỡi hái xương (A, từ Tinh anh) |
| Nhuyễn Thể Đá Hang | A | Đơn độc, ăn khoáng | Thạch hóa, nuốt sống | Hàm nghiền (A); Chúa: Họng Vực, Mai Thạch Hóa, Cơ quan Từ Cảm (S) |
| Hậu Mẫu Ký Sinh | Boss | Thuộc địa nấm, điều khiển quái khác | Điều khiển vật chủ, đẻ bào tử, luồng mạch nấm nối hang | Tuyến Ký Sinh, Mắt Thấu Sinh (S) |

Nhuyễn Thể Đá Hang có phiên bản Chúa ở tầng sâu là quái S của hang; nếu muốn mỗi vùng có đúng một S thì tách thành loài riêng.

## 7. Sinh thái thế giới

Tier quái trùng với bậc trong chuỗi thức ăn, nên tiến hóa của người chơi là leo lên chuỗi này. Quái cũng ăn nhau, không chỉ đứng chờ người chơi.

&#91;embedded content: chuỗi thức ăn · 5 bậc + nhánh ăn xác/ký sinh\]

Nhánh ăn xác và ký sinh không nằm trên một bậc: chúng lấy từ mọi bậc, kể cả xác người chơi đã chết hẳn.

### 7.1 Quái săn nhau

- Quái B săn quái C theo lịch (ví dụ Sói Gai đuổi Bọ Nhảy Cỏ). Người chơi nhìn thấy cảnh săn và có thể cướp mồi hoặc đánh con săn đang mệt.
- Xác quái chết tự nhiên để lại cơ quan với chất lượng mẫu thấp, kéo kẻ ăn xác tới.
- Quái A/S đi qua làm quái C/B bỏ chạy: dấu hiệu có đại thú gần đó trước khi người chơi thấy nó.

### 7.2 Áp lực tiến hóa theo vùng

| Vùng | Áp lực | Cơ quan giải | Lấy ở đâu |
| --- | --- | --- | --- |
| Đồng bằng | Bị săn, đất trống | Chân, Mắt, Gai | Ngay tại đồng bằng |
| Hoang mạc | Nóng, khô | Màng chịu nhiệt, Màng giữ ẩm | Bọ Hút Sương ở rìa hoang mạc (an toàn) |
| Băng tuyết | Lạnh, đêm dài | Lớp mỡ, Lông băng, Mắt nhiệt | Bọ Băng ở rìa vùng |
| Hang động | Tối, khí độc | Râu cảm rung, Định vị, Lọc khí độc | Dơi Hang, Nấm Đi ở cửa hang |

Mỗi vùng có một loài C ở rìa rơi cơ quan "vé vào vùng", nên người chơi không bị kẹt. Hai vùng băng và hoang mạc đối nghịch: cơ quan giúp sống ở vùng này làm khổ ở vùng kia (Lông băng quá nhiệt ở hoang mạc).

### 7.3 Quần thể và sự kiện thế giới

- **Quần thể động:** mỗi vùng có số lượng mục tiêu cho từng loài. Săn quá nhiều mồi C → kẻ săn B đói, lan sang gần tổ người chơi.
- **Thức giấc của Boss:** khi mức khai thác vùng (số quái bị giết, tổ bị phá) vượt ngưỡng, Boss vùng thức và đi tuần. Người chơi tự chọn lúc gây chiến.
- **Mùa sinh sản:** xuất hiện con non; cả đàn hung hăn hơn nhưng cũng là lúc gặp Tinh anh nhiều hơn.
- **Di cư:** đàn Hươu Sừng Rung đi qua đồng bằng theo chu kỳ, kéo theo bầy Sói Gai.
- **Thời tiết:** bão cát (mắt vô dụng, cảm rung lên ngôi), bão tuyết (phải tụ bầy giữ ấm), đêm hoang mạc (Boss yếu hơn).
- **Quái thích nghi với người chơi:** khi một loại sát thương chiếm đa số trong vùng (ví dụ độc), thế hệ quái sau có tỉ lệ kháng loại đó. Thế giới cũng tiến hóa, đúng chủ đề game.

## 8. Ánh xạ vào code

Khung hiện tại đủ mở rộng bằng ScriptableObject; cần 3 thay đổi chính: thêm vị trí và trường cho `OrganSO`, tạo `CreatureSO` dùng chung cho mọi quái, (`DropTableSO.Roll` đã chọn bậc trước rồi chọn cơ quan trong bậc, không phải làm lại).

| Phần | Hiện tại \[ĐÃ CHỐT\] | Đề xuất |
| --- | --- | --- |
| `OrganSlot` | Eyes, Legs, Skin, Back | Thêm Sensor, Wings, Tail, Mouth, Arms, Digestive, Respiratory, Gland |
| `OrganSO` | Tên, vị trí, bậc, DropWeight, `GrowthExpRequired`, 4 hiệu ứng (Sight, Speed, Heat, Contact). Cấp Lv1–3 do `OrganHost` giữ; `OrganHost.maxOrgans` (0 = không giới hạn) đã có, dùng làm trần số khe | Thêm: `IsExternal`, `CanSever`, `Mass`, `EnergyUpkeep`, `ColdResistance`, `AbilitySO GrantedAbility`, `List<StatModifier> Downsides`, `MaxIntegrity`, `Alpha` (BIS) |
| Sinh vật | 2 class riêng `Prey`, `Predator`, thông số nằm trong component | 1 component `Creature` đọc `CreatureSO`; hành vi là module: Wander, Flee (từ Prey), Hunt (từ Predator), Ambush, Herd, Pack, Colony |
| Nhóm | Không có | `CreatureGroup`: danh sách thành viên, Alpha, tinh thần, báo động (phát `VibrationPulse`) |
| Bảng rơi | Hai bước (bậc trước, rồi cơ quan trong bậc) và ×0.8 cho cơ quan đã có: **đã làm xong** | Chỉ còn gán bảng bậc theo mục 3.2 cho từng `CreatureSO` |
| Spawner | `PreySpawner`, `PredatorSpawner` | `BiomeSpawner` theo số lượng mục tiêu từng loài (mục 7.3) |

Về `Roll`: bản hiện tại đã chọn bậc trước nên tỉ lệ bậc cố định dù mỗi bậc có bao nhiêu cơ quan, kể cả khi lên 54 cơ quan. Việc còn lại là bảng bậc theo loại quái (mục 3.2).

**Phác thảo `CreatureSO`:**

```csharp
public enum SocialType { SolitaryTerritorial, SolitaryAmbush, Pair, Pack, Herd, Colony, Swarm, Symbiont, Scavenger }
public enum CreatureRank { Normal, Juvenile, Alpha, Elite, Mutant, Matriarch }

[CreateAssetMenu(menuName = "Evolve/Creature")]
public class CreatureSO : ScriptableObject
{
    public string DisplayName;
    public OrganTier Tier;              // C/B/A/S (Boss = cờ riêng)
    public bool IsBoss;
    public BiomeId Biome;
    public SocialType Social;
    public Vector2Int GroupSize = new(1, 1);

    [Header("Stats")] public float MaxHealth = 40, BiteDamage = 15;
    public float WanderSpeed = 0.8f, ChaseSpeed = 1.4f, Stamina = 6, RestTime = 2.5f;

    [Header("Senses")] public float SightRadius = 7, VibrationRadius;
    [Range(0, 1)] public float StillDetectFactor = 0.4f; // >1 = săn theo rung

    [Header("Body")] public List<OrganSO> Organs;  // cũng là danh sách có thể rơi
    public List<AbilitySO> Skills;                   // mỗi skill trỏ tới organ nguồn
    public DropTableSO TierTable;                    // trọng số bậc theo mục 3.2
    public float Nutrition = 35;
}
```

**Thứ tự làm gợi ý (khớp ROADMAP):**

1. Sau khi chơi thử 0.3 (ROADMAP còn mục "Chơi thử 0.3" chưa tích) và trước khi đồng bộ mạng ở 0.4: gộp Prey/Predator vào `Creature` + `CreatureSO`, giữ đúng thông số hiện tại để không phá bản chơi thử. Làm trước 0.4 để Netcode chỉ đồng bộ một lớp quái.
2. Cùng đợt: Bầy Sói Gai (Pack + Alpha + hú gọi) và 3 loài hoang mạc có sẵn cơ quan (Bọ Hút Sương, Thằn Lằn Cát, Bọ Cạp Ống).
3. 0.4–0.5: 5 ô kỹ năng gắn cấp cơ quan; BIS cho mắt, chân, giáp (`BIOLOGICAL_INTEGRITY.md` mục H); Boss đầu tiên (Mẹ Bầy Ngàn Chân, vì đốt thân là vùng phá rõ ràng).
4. Sau đó: thuộc địa, swarm, ký sinh, quần thể động.

## 9. Câu hỏi cần chốt

- [x] **[ĐÃ CHỐT]** Boss là bậc riêng, tách khỏi S (S = quái sống trong hệ sinh thái; Boss = trận đánh có giai đoạn, hang ổ riêng).
- [x] **[ĐÃ CHỐT]** Giữ 12 vị trí cơ quan; số vị trí dùng được cùng lúc tùy kích thước slime (nhỏ ~4, lớn tối đa ~8). Quy tắc kích thước → số khe: **[MỞ]**.
- [x] **[ĐÃ CHỐT]** Tuyến chỉ 1 cơ quan như mọi vị trí; build hóa học là vai trò chia trong bầy.
- [x] **[ĐÃ CHỐT]** Giữ đủ 6 cấp trong bậc. Biến dị chỉ mang cơ quan của vùng mà bầy đã từng đến.
- [x] **[ĐÃ CHỐT]** Giữ bảng trọng số mục 3.2 làm mốc chơi thử; chỉnh theo dữ liệu chơi thật.
- [x] **[ĐÃ CHỐT]** Bỏ Chân chạy (A) khỏi bảng rơi con mồi C; nguồn đúng là đầu đàn Hươu Sừng Rung. Sửa code ở 0.3.
- [x] **[ĐÃ CHỐT]** Quái S nhân HP theo số người trong vùng như A và Boss (mục 3.3).
- [x] **[ĐÃ CHỐT]** Kẻ ăn xác ăn dần xác người chơi đã chết hẳn: mất 1 cơ quan mỗi ~5 s (thêm vào 50/50), đồng đội có thời gian đuổi đi.
- [ ] **[HOÃN – phần thế giới]** Quái thích nghi theo loại sát thương người chơi dùng: bật từ đầu hay để sau?
- [ ] **[HOÃN – phần thế giới]** Tên loài và cơ quan: giữ kiểu tên hiện tại (mô tả sinh học) hay đặt tên riêng cho từng loài?
