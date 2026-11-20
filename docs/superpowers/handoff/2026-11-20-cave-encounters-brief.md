# Giao việc: 12 hang, kỳ ngộ, quái đặc trưng (đã duyệt 2026-11-20)

Dành cho phiên làm mê cung (Antigravity). Đọc kèm `2026-11-19-maze-step3-brief.md`; ranh giới thư mục vẫn như cũ. Chủ dự án đã duyệt toàn bộ danh sách dưới đây.

## Ý chủ dự án
- **Mỗi hang một kỳ ngộ riêng**, kỳ ngộ là thứ làm nên hang đó (không gán ngẫu nhiên).
- **Mỗi hang có quái đặc trưng**; giết rồi nuốt xác thì Giải Mã ra **kỹ năng của loài đó**.
- **Kỹ năng chương 1 đơn giản, mỗi cái làm đúng một việc.** Sau này đạt cấp nhất định thì mở khóa **dung hợp** hai kỹ năng thành kỹ năng mới (chưa làm ở chương 1).
- **Tạm chưa làm nội dung dưới nước** (không lặn, không hầm ngập, không quái dưới nước). Bơi nổi trên mặt nước giữ như hiện tại. Nước dâng / Chu Kỳ Lũ đã bỏ.

## Luật chung
- 12 hang chia 3 tầng (Nông, Giữa, Sâu); càng sâu càng tối, ít đồ ăn, quái mạnh hơn. Vị trí các hang trong mê cung **xáo mỗi vòng** (theo hạt giống), nhưng hang nào cũng luôn có kỳ ngộ và quái của nó. Hang Khởi Sinh luôn là điểm xuất phát; Khoang Phong Ấn luôn ở tầng Sâu.
- **Kỳ ngộ:** không đánh dấu bản đồ, không nhiệm vụ. Có **dấu hiệu** nhìn hoặc nghe thấy được; kích hoạt bằng **hành động của cơ thể** (nuốt, đứng yên, Sóng Dò, chui khe…), không bằng nút tương tác; **một lần mỗi vòng**. Khi kích hoạt: Tiếng Vọng nói một câu (`Echo.Say`) và có một tiếng hiệu ứng.
- **Quái:** dựng trên khung `Creatures/Predator` (săn) hoặc `Creatures/Prey` (bị săn) đang có. Cả hai có ô `drops` (`DropTableSO`): khi chết rơi xác mang gene theo bảng đó. Giữ đúng đường đi hiện tại: giết → nuốt xác → Giải Mã trong Túi → học kỹ năng.

## 12 hang

Ký hiệu: **CĐ** = kỹ năng chủ động (phím 1–5), **BĐ** = bị động (mang tối đa 3). **Họ** dùng cho dung hợp về sau.

### Tầng Nông

**1. Hang Khởi Sinh** (đã có: hồ, lỗ trần, tia nắng)
- Kỳ ngộ *Tia Nắng Đầu Tiên*: nổi trong tia nắng qua lỗ trần lần đầu → Tiếng Vọng chào, một lần no nhẹ. Dạy cách chơi.
- Quái: **Thằn Lằn Bờ Hồ** (săn, nhỏ; rình trên bờ, lao bắt côn trùng, chạy khi slime to hơn).
- Kỹ năng: **Lưỡi Dài** (CĐ, họ Linh Hoạt): phóng lưỡi kéo con mồi nhỏ ở xa về miệng để nuốt.

**2. Vườn Quặng Cầu Vồng** (đã có: cây cối lung linh sắc màu, suối, quặng nhiều màu)
- Kỳ ngộ *Cầu Vồng Trong Thân*: dấu hiệu là quặng mỗi màu phát sáng một kiểu, suối lấp lánh. Nuốt đủ quặng **5 màu khác nhau** trong hang (mỗi màu một viên) rồi ngâm mình xuống suối → thân slime đổi sắc cầu vồng, **mở khóa một màu mới** trong bảng màu slime, thêm một gene.
- Quái: **Bọ Giáp Quặng** (săn, chậm, vỏ cứng, ăn quặng); **Đom Đóm Sắc Màu** (bị săn, bay lượn, phát sáng).
- Kỹ năng: **Vỏ Cứng** (BĐ, họ Đá): nhận ít sát thương hơn 15%. **Phát Sáng** (CĐ, họ Âm Ảnh): tỏa sáng quanh mình 10 giây.

**3. Hang Rêu Thở**
- Kỳ ngộ *Hơi Thở Của Rêu*: thảm rêu sáng tối theo nhịp như đang thở. Nằm yên trên rêu đúng lúc rêu "thở ra" → hồi đầy và khỏi bệnh.
- Quái: **Sên Rêu** (bị săn, rất chậm, để lại vệt nhầy).
- Kỹ năng: **Tiết Nhớt** (CĐ, họ Độc): để lại vũng nhầy làm kẻ địch đi chậm.

**4. Tổ Bỏ Hoang**
- Kỳ ngộ *Quả Trứng Còn Sống*: ổ trứng nằm trong khe. Nuốt quả trứng còn sống; vài phút sau trứng nở trong Túi → một dòng gene lạ (không gặp ở nơi khác).
- Quái: **Chim Hang Mẹ** (săn, canh tổ, bổ nhào từ trên cao).
- Kỹ năng: **Nhảy Cao** (CĐ, họ Linh Hoạt): nảy cao gấp đôi, lên được gờ cao.

### Tầng Giữa

**5. Vực Nứt Tím** (đã có: vết nứt tinh thể tím u ám, gai nhọn)
- Kỳ ngộ *Hợp Âm Tinh Thể*: ba tinh thể lớn ngân lên khi trúng Sóng Dò, mỗi cái một cao độ. Dùng Sóng Dò vào ba tinh thể theo thứ tự **từ trầm lên cao** → một vết nứt lớn rung, nứt toác, lộ **tinh thể lõi**; nuốt nó → chắc chắn một gene Hiếm trở lên. Gai nhọn khắp hang làm việc đi tìm có nguy hiểm.
- Quái: **Nhện Tinh Thể** (săn, nấp trong vết nứt, bò trên vách); **Dơi Âm Thoa** (bị săn, bay theo đàn, kêu thành sóng âm).
- Kỹ năng: **Bám Vách** (CĐ, họ Linh Hoạt): leo vách đứng vài giây. **Sóng Dò Mạnh** (BĐ, họ Âm Ảnh): Sóng Dò đi xa hơn 50%.

**6. Hang Nấm Huỳnh Quang**
- Kỳ ngộ *Vòng Nấm*: vòng nấm phát sáng theo nhịp, bào tử bay. Đứng giữa vòng đúng lúc bào tử sáng rực → **đổi một Khuyết Điểm** (rút lại ngẫu nhiên, có thể tệ hơn).
- Quái: **Ếch Độc Đốm** (săn, nhảy, phun độc).
- Kỹ năng: **Phun Độc** (CĐ, họ Độc): phun một tia độc, gây sát thương dần.

**7. Suối Nước Nóng** (vũng nóng để ngâm trên mặt, không phải nước sâu)
- Kỳ ngộ *Tôi Luyện*: hơi nóng bốc, nước sủi. Ngâm qua đợt nóng mà không chết → chịu nhiệt tốt hơn suốt vòng chơi.
- Quái: **Cua Đá Nóng** (săn, đi ngang, vỏ nóng).
- Kỹ năng: **Chịu Nhiệt** (BĐ, họ Đá): ở chỗ nóng lâu hơn.

**8. Khe Gió Hú**
- Kỳ ngộ *Túi Của Kẻ Đi Trước*: tiếng gió rít từ một khe rất hẹp, chỉ slime nhỏ lọt (đọc `SlimeBody.Mass` / `BodySize`). Bên trong là Túi của một kẻ chuyển sinh trước: vài món vào bụng và một câu chuyện qua Tiếng Vọng.
- Quái: **Chuột Gió** (bị săn, nhỏ, chạy rất nhanh, chui khe).
- Kỹ năng: **Thu Nhỏ** (CĐ, họ Linh Hoạt): co nhỏ 8 giây, lọt được khe hẹp.

### Tầng Sâu

**9. Nghĩa Địa Cự Thú**
- Kỳ ngộ *Bữa Tiệc Cổ*: một bộ xác khổng lồ. Giữ F hòa tan suốt khoảng 60 giây, vừa làm vừa bị quấy → nhiều khối và một gene của loài cổ.
- Quái: **Bọ Ăn Xác** (săn theo đàn nhỏ, tụ quanh xác).
- Kỹ năng: **Tiết Axit** (CĐ, họ Độc): làm tan vách đá vụn, hòa tan xác nhanh.

**10. Hang Hóa Thạch Slime Cổ**
- Kỳ ngộ *Ký Ức Đá*: hình slime hóa đá trên vách. Bò lên và đứng yên 5 giây → Tiếng Vọng kể chuyện cũ; **Lõi mạnh lên một chút** trong vòng.
- Quái: **Slime Đá** (săn, chậm, rất cứng).
- Kỹ năng: **Hóa Đá** (CĐ, họ Đá): bất động 4 giây, không nhận sát thương.

**11. Hang Tiếng Thở** (gần Khoang Phong Ấn)
- Kỳ ngộ *Giấc Ngủ Cạnh Kẻ Cổ*: tiếng thở của sinh vật cổ to dần. Ngủ (đứng yên lâu) ngay trong hang mà không bị phát hiện → Tiếng Vọng gợi ý đường tới Khoang Phong Ấn.
- Quái: **Kẻ Rình Bóng Tối** (săn, mù, nghe tiếng động mà săn).
- Kỹ năng: **Ẩn Thân** (CĐ, họ Âm Ảnh): tàng hình 5 giây.

**12. Khoang Phong Ấn**
- Sinh vật cổ đại bị phong ấn (không giết được), bộ xương rồng. Chỉ lối ra khi slime đã tiến hóa ít nhất 2 lần (kết chương 1). Không có kỹ năng.

Hang trú (nhỏ, lối vào hẹp, có rêu ẩm) rải xen giữa các hang như cũ, không tính vào 12 hang.

## Kỹ năng là chìa khóa mở đường
Nhiều kỹ năng là cách vượt chướng ngại tự nhiên (thiết kế mục 9.5): **Bám Vách** cho vách cao, **Tiết Axit** cho vách đá vụn, **Chịu Nhiệt** cho hầm nóng, **Sóng Dò Mạnh** cho vùng tối đặc, **Thu Nhỏ** cho khe nhỏ, **Nhảy Cao** cho gờ cao. Nên đặt các chướng ngại đó ở lối vào các hang khác, để hang này cho chìa mở hang kia.

## Họ kỹ năng (cho dung hợp về sau)
- **Linh Hoạt:** Lưỡi Dài, Nhảy Cao, Bám Vách, Thu Nhỏ.
- **Đá:** Vỏ Cứng, Chịu Nhiệt, Hóa Đá.
- **Độc:** Tiết Nhớt, Phun Độc, Tiết Axit.
- **Âm Ảnh:** Phát Sáng, Sóng Dò Mạnh, Ẩn Thân.

## Chia việc
**Bên làm hang (Antigravity):**
1. Dựng các hang còn thiếu (9 hang) theo chủ đề trên, xếp vào mê cung 3 tầng.
2. Kỳ ngộ: dấu hiệu (ánh sáng, âm thanh, hình), vùng và điều kiện kích hoạt, luật thuần có test (thứ tự tinh thể, đủ 5 màu quặng, nhịp rêu, thời gian ngâm…). Khi đạt điều kiện thì gọi phần thưởng qua `EncounterRewards` (bên dưới).
3. Quái: hình dáng, hành vi, nơi sống, số lượng; dựng trên `Predator`/`Prey`. Để trống ô `drops`; phiên chính tạo bảng rơi và gán.
4. Chướng ngại ở lối vào hang: chỉ dựng **khung và điều kiện** (interface), vì kỹ năng chưa có.
5. Khai báo tia sáng trong hang bằng `Evolve.Traits.LightShafts.Register(center, radius, topY)` (Nhân Quang Hợp đọc nó).

**Phiên chính:**
1. 13 kỹ năng mới (gene, cách hoạt động, hình phần cơ thể mọc ra khi dùng) và bảng rơi `DropTableSO` cho từng quái.
2. `Evolve.Traits.EncounterRewards`: `GrantGene(player, minRarity)`, `GrantUniqueGene(player, id)`, `AddMass(player, kg)`, `RerollFlaw(player, seed)`, `AddHeatResistance(player, value)`, `RefillStats(player)`, `CureDiseases(player)`, `BoostCore(player)`, `UnlockColor(player, color)`, `GiveStomachItems(player, items)`, `Say(line)`.
3. Mở khóa dung hợp: để sau chương 1.

**Không đụng:** `Scripts/Traits`, `Swallow`, `Survival`, `Player`, `Abilities`. Cần gì ở đó thì ghi vào `docs/superpowers/handoff/maze-questions.md`.

## Báo cáo
Mỗi hang xong thì báo: đã dựng gì, kỳ ngộ kích hoạt thế nào, test nào, chưa làm gì. Không nói "xong" nếu chưa mở Unity kiểm. Đẩy lên nhánh riêng để phiên chính gộp.
