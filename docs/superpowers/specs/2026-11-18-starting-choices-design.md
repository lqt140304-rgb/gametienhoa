# Lựa chọn đầu game: Dị Năng, Lõi, Khuyết Điểm — thiết kế

Ngày: 2026-11-18. Trạng thái: **bản hoàn chỉnh chờ duyệt** (đóng băng để bắt đầu cài đặt; vẫn sửa được). Mọi con số là **số tạm** để thử nghiệm.

## 1. Ý định và lý do
Người chơi bắt đầu từ một slime nguyên thủy giống nhau. Ba lựa chọn đầu game cho mỗi người một **bản sắc riêng ngay từ phút đầu**, trước khi có cơ quan hay class (class đầu tiên ở Level 15): một kỹ năng chủ động bẩm sinh (**Dị Năng**), một luật bị động của cơ thể (**Lõi**), và một cái giá vĩnh viễn (**Khuyết Điểm**). Mục tiêu của người thiết kế: mỗi người một lối chơi khác nhau ngay đầu ván, cả phòng co-op không ai giống ai, và có chuyện để nói: "ai có Lõi gì".

## 2. Mục tiêu, không phải mục tiêu, và điều làm hỏng thiết kế
Mục tiêu:
- Lõi **tự chạy**, đổi một luật của cơ thể; người chơi chơi theo lối nào đó là Lõi phát huy, không bấm nút.
- Khuyết Điểm là **luật xấu tự chạy**, gây phiền chứ không gây đau; nhìn thấy được.
- Phòng co-op (tối đa 8 người) không trùng Lõi.
- Phần chọn đầu game gọn, nhanh, đẹp, theo phong cách menu chọn lõi của TFT.

Không phải mục tiêu (làm sau): thiết kế cơ chế đổi Lõi và Dị Năng (cần vật phẩm hiếm và dụng cụ chưa thiết kế); biểu tượng quả cầu chính thức; Lõi điều khiển trang bị; class, mốc tiến hóa.

Điều làm hỏng thiết kế (anti-goals):
- Khuyết Điểm triệt tiêu Lõi (chọn Lõi thành gánh nặng): cấm.
- Khuyết Điểm làm người chơi thua chắc, hoặc mất máu theo đồng hồ, hoặc ép quay lại một nơi: cấm.
- Lõi giống một nút bấm (kỹ năng): sai loại, thuộc về Dị Năng.

## 3. Ràng buộc
- Thanh kỹ năng giữ **6 ô: Q (Dị Năng) + 5 ô từ cơ quan và class** (khớp `SkillBar`, `AbilityCaster` hiện có). Tài liệu gameplay V1.1 mục 5.4 ("2 ô chủ động") **bị bỏ**.
- Co-op 4–8 người qua `CoopManager`, `PlayerNetState`, `WorldSync`; host là trọng tài, kết quả đồng nhất trên mọi máy.
- Dữ liệu Lõi và Khuyết Điểm đặt trong ScriptableObject riêng; số liệu nằm trong từng tệp.
- Mất cơ quan khi chết theo 3 mạng (luật hiện có) và `DropTableSO`/`OrganTier` hiện có; Lõi Nhân Con Bạc và Nhân Giữ Gốc dựa trên chúng.
- `AbilityTier` hiện có C, B, A, S; bậc cơ quan D, C, B, A, S (`OrganTier`).

## 4. Các cách đã cân nhắc
Cách chọn Lõi và Khuyết Điểm: (A) chọn độc lập, (B) cặp cố định, **(C) chọn Lõi trước rồi chọn Khuyết Điểm trong 3 cái cùng chủ đề với Lõi đó (đã chọn)**. Lý do: giữ quyền "tự chọn cái giá" mà tránh các tổ hợp phá game của A.
Cách tổ chức co-op: (A) lần lượt, (B) cùng lúc rồi giành, **(C') phát thẻ riêng ở màn hình tải rồi mỗi người tự chọn trong bộ thẻ của mình (đã chọn)**. Lý do: không có tranh giành, nhanh, công bằng, không cần giới hạn thời gian.
Khuyết Điểm: (a) trừ số, (b) **luật xấu tự chạy (đã chọn)**. Lý do: trừ số bị quên sau vài phút; luật xấu đổi cách chơi.

## 5. Luồng cho mỗi người
1. Ở màn hình tải, host phát cho mỗi người một **Dị Năng ngẫu nhiên** và **3 thẻ Lõi riêng** (không trùng nhau khi kho đủ lớn).
2. Người chơi xem Dị Năng (không chọn), rồi **chọn 1 Lõi** trong 3 thẻ.
3. Hiện **3 lựa chọn Khuyết Điểm** theo Lõi đã chọn (Lõi nặng: 3 cặp hai Khuyết Điểm); **chọn 1**.
4. Xem lại, **xác nhận**, vào game.

Quy tắc phát thẻ:
- Mỗi người luôn nhận **3 thẻ**. Người thiết kế sẽ bổ sung Lõi để kho đủ lớn (24 Lõi thì phòng 8 người có 3 thẻ không trùng).
- Khi kho còn nhỏ (hiện 12 Lõi, phòng 5 người trở lên không đủ 3 thẻ riêng): ưu tiên chia không trùng; khi hết Lõi chưa chia thì cho phép trùng thẻ giữa các người ở bước nhận. Chống trùng vẫn giữ ở bước chọn: Lõi đã có người xác nhận thì bị khóa; người xác nhận sau (theo thứ tự host nhận) thấy thẻ đó mờ đi và được **thay bằng một thẻ khác chưa bị khóa**. Còn đủ Lõi để thay vì kho 12 lớn hơn tối đa 8 người.
- Người vào trễ hoặc rời phòng: thẻ của họ trả lại kho, chia lại bằng seed của host.
- Đổi Dị Năng: cần một dụng cụ đặc biệt; đổi Lõi: trả giá bằng một trang bị hiếm. **Cả hai chưa thiết kế**, ngoài phạm vi này.

## 6. Màn hình chọn
Phong cách: ba thẻ dọc kiểu gel trong suốt, có biểu tượng quả cầu, **gọn và tinh tế hơn** ảnh mẫu `docs/art/core-picker-reference.webp` (tên Lõi trong ảnh chỉ là minh họa), theo kiểu menu chọn lõi của TFT.
```
  Dị Năng của bạn: [Co Rút]                     (rút ngẫu nhiên, chỉ xem)

  CHỌN LÕI (1 trong 3)
  +------------+  +------------+  +------------+
  |    (o)     |  |    (o)     |  |    (o)     |
  | Màng Thẩm  |  | Nhân Ngụy  |  | Nhân Con   |   tên ở trên
  | Thấu       |  | Sắc        |  | Bạc        |
  | hút dưỡng  |  | hòa vào    |  | rơi nhiều  |   mô tả rút gọn ở dưới
  | chất       |  | nền        |  | đồ hơn     |
  +------------+  +------------+  +------------+
  [Tab] hiện chi tiết: luật đầy đủ, giới hạn, 3 Khuyết Điểm đi kèm

  CHỌN KHUYẾT ĐIỂM (hiện sau khi chọn Lõi, thẻ nhỏ hơn)
  [Run Rẩy]  [Mờ Trong Tối]  [Cơ Quan Mau Mòn]

                          [ Xác nhận ]
```
- **Tên Lõi ở trên, mô tả rút gọn ở dưới; Tab để hiện chi tiết** (không dùng rê chuột).
- Biểu tượng quả cầu **dùng tạm**: hình tròn đơn sắc theo chủ đề; bản chính thức làm sau.
- Thẻ bị khóa bởi người khác thì mờ đi, kèm dòng giải thích.
- Sau khi vào game, HUD hiện biểu tượng Khuyết Điểm cố định, kèm mô tả khi rê chuột.

## 7. Dị Năng
Kỹ năng chủ động ở phím **Q**, rút ngẫu nhiên đầu game từ kho Dị Năng (v2.0: 15 C, 12 B, 6 A, 4 S theo `docs/GAME_DESIGN.md`; tỉ lệ rút chưa chốt). Là "thiên phú chủ động" của tài liệu cũ. Chưa quyết có chống trùng Dị Năng giữa người chơi hay không.

## 8. Lõi
Nguyên tắc: **Lõi tự chạy, đổi một luật của cơ thể; Dị Năng mới là thứ bấm nút.** Lõi là thiên phú bị động (tên cũ "Thiên Phú bị động"), thuộc lõi sống bẩm sinh, khác với lõi điều khiển trang bị (tối đa 1) trong tài liệu V1.1. Mỗi Lõi chọn 3 Khuyết Điểm khác trục với hành vi mạnh của Lõi.

| Lõi | Cơ chế | 3 Khuyết Điểm (chọn 1) |
|---|---|---|
| Màng Thẩm Thấu | Đứng yên trong nước nông hoặc rêu thì hút dưỡng chất dần | Run Rẩy · Mờ Trong Tối · Cơ Quan Mau Mòn |
| Nhân Thần Tốc | Nảy đúng nhịp liên tiếp thì tăng tốc theo chuỗi | Đói Sớm · Cơ Quan Mau Mòn · Dạ Dày Kén |
| Nhân Ngụy Sắc | Đứng yên vài giây thì hòa vào nền | Ngại Đông · Dạ Dày Kén · Luống Cuống |
| Nhân Phản Chấn | Bị đánh trúng thì tích lực, đòn kế tiếp mạnh hơn | Đói Sớm · Lạ Vùng · Bám Bùn |
| Nhân Cộng Sinh | Ở gần đồng đội thì cùng hồi máu nhanh hơn | Vệt Mùi · Mờ Trong Tối · Mau Khô |
| Nhân Ăn Mòn | Chạm vật hữu cơ thì tiết dịch ăn dần, hút thêm dinh dưỡng | Run Rẩy · Lạ Vùng · Dễ Bị Chú Ý |
| Nhân Con Bạc | Tăng tỉ lệ rơi theo bậc cơ quan (kể cả boss) | (Đói Sớm + Mờ Trong Tối) · (Run Rẩy + Lạ Vùng) · (Cơ Quan Mau Mòn + Ngại Đông) |
| Nhân Thích Nghi | Chịu lặp lại một loại sát thương môi trường thì kháng loại đó tăng dần | Run Rẩy · Ngại Đông · Dễ Bị Chú Ý |
| Nhân Quang Hợp | Đứng hoặc đi trong tia sáng xuyên rễ thì hồi năng lượng | Bám Bùn · Vệt Mùi · Cơ Quan Mau Mòn |
| Nhân Bám Dính | Không bị hất văng khi đứng vững hoặc đi chậm; leo dốc không mất tốc | Mờ Trong Tối · Mau Khô · Dạ Dày Kén |
| Nhân Quen Đất | Đi lại một đường nhiều lần thì nhanh hơn trên đường đó | Run Rẩy · Đói Sớm · Luống Cuống |
| Nhân Giữ Gốc | Chết lần 1 hoặc 2 không mất cơ quan đang trang bị | (Đói Sớm + Lạ Vùng) · (Mờ Trong Tối + Ngại Đông) · (Dạ Dày Kén + Luống Cuống) |

Lõi nặng (Nhân Con Bạc, Nhân Giữ Gốc): mỗi lựa chọn Khuyết Điểm là **một cặp hai Khuyết Điểm** để bù cho sức mạnh lớn.
Đã bỏ khỏi bản nháp: Nhân Dò Rung, Nhân Tái Cấu (vô dụng), Nhân Tích Bào (giống kỹ năng bấm). Nhân Phát Quang đổi thành Nhân Quang Hợp.

### Cơ chế chi tiết từng Lõi
Mỗi Lõi: kích hoạt, hiệu ứng, giới hạn, tương tác, đồng bộ co-op.

**Nhân Quen Đất** (đi lại một đường nhiều lần thì nhanh hơn trên đường đó).
Đã duyệt: **Nhân Thích Nghi** (chịu lặp lại một loại sát thương môi trường thì kháng loại đó tăng dần, hết bị tác động thì giảm; Khuyết Điểm không dùng nhóm Sợ-).
Đã duyệt: **Nhân Giữ Gốc** = khi chết **không mất cơ quan đang trang bị** (không áp dụng cho vật phẩm mang theo hay kho). Quyết định của Claude, chờ phản hồi: không áp dụng ở lần chết thứ 3 (hết mạng); Khuyết Điểm nặng hơn mức thường, kiểu cộng hai Khuyết Điểm nhẹ.
Đã chỉnh lại theo phản hồi: Nhân Phát Quang thành Nhân Quang Hợp; Nhân Bám Dính thành luật không bị hất văng khi đứng vững. Đã duyệt.

## Cơ chế chi tiết của Lõi (bản nháp mẫu, chưa duyệt)
Mỗi Lõi sẽ được mô tả theo cùng khuôn: kích hoạt khi nào, giới hạn, tương tác (cơ quan, đồng đội, boss), đồng bộ co-op, số liệu tạm. Hai mẫu đầu:

**Nhân Quen Đất**
- Kích hoạt: mỗi lần đi qua một đoạn đường (ô lưới khoảng 4 m) thì tăng điểm "quen" của đoạn đó.
- Hiệu ứng: điểm quen cao thì tốc độ trên đoạn đó tăng tối đa +15% (số tạm).
- Giới hạn: điểm quen giảm dần khi bỏ đoạn đường nhiều phút; không cộng dồn khi đứng yên; không tác dụng trong nước sâu.
- Tương tác: đồng đội không chia sẻ điểm quen; ghi theo từng người; lưu theo ván.
- Co-op: tính toán tại máy mỗi người, host chỉ kiểm tra tốc độ tối đa.

**Nhân Giữ Gốc**
- Kích hoạt: khi chết ở lần thứ 1 hoặc 2.
- Hiệu ứng: cơ quan đang trang bị không bị mất, vẫn gắn vào thân sau hồi sinh.
- Giới hạn: không áp dụng lần chết thứ 3; không bảo vệ vật phẩm mang theo hay kho; cơ quan đã hỏng vẫn hỏng.
- Tương tác: không đổi hình phạt mất mạng; Khuyết Điểm của Lõi này là cặp hai Khuyết Điểm.
- Co-op: host quyết định lúc chết, gửi kết quả cho mọi người.

**Màng Thẩm Thấu**
- Kích hoạt: đứng yên (di chuyển dưới 0,3 m/s) trong nước nông hoặc trên rêu ẩm.
- Hiệu ứng: hút dưỡng chất dần, tương đương một phần nhỏ nguồn ăn (tạm 1,5% độ no mỗi giây); khi ngồi thấm thì ngừng hao độ no.
- Giới hạn: không hoạt động trong nước sâu khi đang bơi; tối đa lấy đầy 60% thanh no, phần còn lại phải ăn thật; bị đánh thì ngắt 3 giây.
- Tương tác: cơ quan tiêu hóa không đổi; tia sáng gắt không cộng thêm.
- Co-op: tính tại máy từng người, host kiểm tra tốc độ tăng độ no tối đa.

**Nhân Thần Tốc**
- Kích hoạt: mỗi cú nảy chạm đất đúng nhịp (bấm nảy trong khoảng 0,3–0,6 giây sau lần chạm trước).
- Hiệu ứng: mỗi nảy đúng nhịp cộng một cấp chuỗi; mỗi cấp +4% tốc độ, tối đa 5 cấp (tạm +20%).
- Giới hạn: nảy sai nhịp hoặc đứng yên quá 1 giây thì mất chuỗi; không hoạt động khi bơi hoặc bám vách.
- Tương tác: cơ quan vận động cộng bình thường; chuỗi không tăng sát thương.
- Co-op: chuỗi là trạng thái tại máy người chơi; host kiểm tra tốc độ tối đa cho phép.

**Nhân Ngụy Sắc**
- Kích hoạt: đứng yên 2 giây.
- Hiệu ứng: màu thân dần hòa với nền; kẻ địch phải lại gần hơn mới phát hiện (tạm giảm 60% tầm phát hiện).
- Giới hạn: di chuyển, tấn công hoặc bị đánh thì mất ngụy trang ngay; không hiệu quả ở nền quá sáng (tia sáng gắt) hoặc quá lạ màu.
- Tương tác: kẻ địch có khứu giác vẫn đánh hơi được; đồng đội không bị ảnh hưởng.
- Co-op: trạng thái ngụy trang do host đồng bộ để AI kẻ địch dùng chung một nguồn.

**Nhân Phản Chấn**
- Kích hoạt: nhận sát thương từ đòn trúng thân.
- Hiệu ứng: tích điểm lực theo lượng sát thương nhận; đòn tấn công kế tiếp mạnh hơn theo điểm tích (tạm +25% ở mức đầy).
- Giới hạn: điểm lực tối đa 3 lần tích; mất sau 6 giây không đánh; sát thương môi trường (độc, nhiệt) không tích.
- Tương tác: cộng với giáp nhưng giáp càng cao thì tích càng ít (vì sát thương nhận ít).
- Co-op: host tính điểm lực khi tính sát thương để tránh lệch.

**Nhân Cộng Sinh**
- Kích hoạt: có đồng đội trong bán kính 8 m.
- Hiệu ứng: cả hai hồi máu nhanh hơn (tạm +40% tốc độ hồi); người có Lõi này mạnh hơn khi nhiều người cùng đứng gần.
- Giới hạn: tối đa 3 đồng đội tính vào; đồng đội không có Lõi vẫn được hưởng hồi nhanh nhưng yếu hơn (một nửa); khi chơi một mình hoặc không có ai trong 8 m thì vẫn được hồi nhanh nhẹ (tạm +10%, bằng một phần tư mức gần đồng đội).
- Tương tác: không chồng với hồi máu của cơ quan; lấy mức cao hơn.
- Co-op: host tính khoảng cách giữa người chơi mỗi 0,5 giây.

**Nhân Ăn Mòn**
- Kích hoạt: chạm hoặc bao phủ vật hữu cơ (xác, thực vật, nấm).
- Hiệu ứng: tiết dịch làm vật hữu cơ phân hủy nhanh hơn, hút thêm dưỡng chất (tạm +30%).
- Giới hạn: chỉ vật hữu cơ; không tác dụng với vật cứng hoặc sinh vật còn sống; chỉ tính một lần cho mỗi vật.
- Tương tác: cộng với tuyến axit nhưng không chồng cùng loại dịch.
- Co-op: host quyết định lượng hút khi nhiều người cùng ăn một xác, chia theo thời gian.

**Nhân Con Bạc**
- Kích hoạt: mỗi lượt rơi chiến lợi phẩm của chính người này (cơ quan từ quái, đồ từ rương, kể cả boss).
- Hiệu ứng: tăng tỉ lệ rơi **theo bậc cơ quan** (tăng tương đối, số tạm): D +20% · C +16% · B +12% · A +8% · S +4% (bậc S do Claude đề xuất, chờ duyệt). Bậc càng hiếm thì tăng càng ít. Với boss áp dụng một nửa mức theo bậc.
- Giới hạn: có trần, không bao giờ đạt chắc chắn; không tăng bậc cơ quan; chỉ áp cho lượt của người đó.
- Tương tác: Khuyết Điểm của Lõi này là cặp hai Khuyết Điểm (nặng hơn mức thường).
- Co-op: host lăn xúc xắc rơi cho từng người; hiệu ứng nhẹ báo khi đồ hiếm rơi nhờ Lõi này.

**Nhân Thích Nghi**
- Kích hoạt: chịu liên tục một loại sát thương môi trường (độc, nhiệt, lạnh, axit).
- Hiệu ứng: kháng loại đó tăng dần theo thời gian chịu (tạm +4% mỗi 5 giây).
- Giới hạn: kháng tối đa 40%; giữ kháng của một loại tại một thời điểm; hết bị tác động 60 giây thì giảm dần về 0.
- Tương tác: Khuyết Điểm của Lõi này không dùng nhóm Sợ-; cộng với kháng từ cơ quan nhưng tổng không vượt 70%.
- Co-op: tính tại host khi tính sát thương môi trường.

**Nhân Quang Hợp**
- Kích hoạt: đứng hoặc đi trong tia sáng xuyên rễ (vùng LightShaft của bản đồ).
- Hiệu ứng: hồi năng lượng dần (tạm 3 điểm mỗi giây).
- Giới hạn: không hoạt động khi bị che (dưới nước sâu); thân có ngụy trang không cản; không cộng khi có cơ quan hồi năng lượng cùng lúc (lấy mức cao).
- Tương tác: Khuyết Điểm không dùng Hao Nền để tránh cùng trục.
- Co-op: mỗi người tính riêng; tia sáng là một phần bản đồ nên không cần đồng bộ thêm.

**Nhân Bám Dính**
- Kích hoạt: đứng vững hoặc đi chậm (dưới 40% tốc độ tối đa).
- Hiệu ứng: miễn nhiễm hất văng và đẩy lùi; leo dốc không mất tốc.
- Giới hạn: khi chạy hết tốc độ thì mất miễn nhiễm; không áp dụng với hiệu ứng làm choáng hoặc trói; đòn hất cực mạnh của boss vẫn đẩy được một đoạn ngắn.
- Tương tác: Khuyết Điểm không dùng Mô Nặng hoặc Nhảy Thấp.
- Co-op: host quyết định hất văng, kiểm tra điều kiện đứng vững.

## 9. Khuyết Điểm
Luật viết (đã duyệt): (1) đánh vào **một trục khác**, không đánh vào tình huống Lõi phát huy; (2) Lõi cộng Khuyết Điểm vẫn là **lợi ròng**; (3) **làm người chơi để ý, không làm họ thua**, không biến mất sau cấp 20, không phạt nặng mọi trận.
Khuyết Điểm vĩnh viễn, không xoá được, **không kèm lợi ích bù**. Cơ quan đúng vị trí giải phẫu **bù tối đa 70%** (cơ quan hỏng thì mất phần bù); 30% luôn ở lại.

| Khuyết Điểm | Luật xấu (tự chạy) | Cơ quan bù |
|---|---|---|
| Run Rẩy | Bị đánh trúng thì khựng 0,7 giây, không nảy được | Chân, Đuôi |
| Vệt Mùi | Đi qua đâu để lại vệt mùi 20 giây; kẻ địch trong khoảng 15 m có thể theo vệt | Tuyến |
| Mờ Trong Tối | Ở nơi tối thì tầm nhìn hẹp lại 15% | Mắt, Cảm quan phụ |
| Đói Sớm | Độ no tụt nhanh hơn 10%, báo đói sớm hơn; không mất thêm máu | Tiêu hóa |
| Bám Bùn | Đi qua bùn hoặc nước nông bị chậm thêm 2 giây sau khi ra | Chân, Vây/cánh |
| Luống Cuống | Máu dưới 25% thì xoay hướng chậm hơn rõ rệt | Đuôi, Cảm quan phụ |
| Ngại Đông | Có từ ba sinh vật trở lên gần nhau (trong 6 m) thì hồi năng lượng chậm đi 20% | Tuyến |
| Mau Khô | Ở xa nước hoặc rêu quá 90 giây thì thân khô, đi chậm hơn 8%; vào nước hoặc rêu là hết ngay | Da/màng |
| Cơ Quan Mau Mòn | Độ nguyên vẹn cơ quan mất nhanh hơn 20% khi trúng đòn | Lưng (giáp), Da/màng |
| Dễ Bị Chú Ý | Kẻ địch trong tầm 20 m bị thu hút về phía bạn | Da/màng, Tuyến |
| Lạ Vùng | Vào vùng mới thì 15 giây đầu tầm nhìn hẹp 15% | Mắt, Hô hấp |
| Dạ Dày Kén | Ăn thức ăn lạ lần đầu thì hấp thụ chậm hơn 30% trong 20 giây | Tiêu hóa |

Mỗi Khuyết Điểm có biểu tượng cố định trên HUD và mô tả khi rê chuột. Ràng buộc cài đặt: tổng bù từ nhiều cơ quan không vượt 70%; hai Khuyết Điểm trong một cặp không cùng loại; Nhân Thích Nghi không dùng nhóm Sợ-; Nhân Quang Hợp không dùng Hao Nền hay Hồi Chậm; Nhân Bám Dính không dùng Bám Bùn, Run Rẩy.

## 10. Việc để người cài đặt quyết
- Cấu trúc ScriptableObject cho Lõi, Khuyết Điểm và bộ thẻ; cách tách dữ liệu và logic để thêm Lõi mới chỉ cần thêm một tệp.
- Cách hiện Tab chi tiết, hoạt ảnh thẻ, tiếng, bố cục chính xác trong khuôn đã phác.
- Định dạng đồng bộ co-op cho bộ thẻ, xác nhận, khóa Lõi (theo mẫu `TreefolkSync`).
- Cách lưu Lõi và Khuyết Điểm của người chơi trong ván (và lưu theo dòng dõi là việc sau).
- Chuyển các số tạm thành giá trị chỉnh được.

## 11. Còn mở (không chặn cài đặt giai đoạn đầu)
- Vật phẩm hiếm đổi Lõi; dụng cụ đặc biệt đổi Dị Năng.
- Có chống trùng Dị Năng không; tỉ lệ rút Dị Năng.
- Bậc S của Nhân Con Bạc (+4%) và mức nặng chính xác của các cặp Khuyết Điểm.
- Lõi thêm cho kho 24; biểu tượng quả cầu chính thức.

## 12. Hợp nhất với các tài liệu khác (làm sau khi duyệt)
- Tài liệu gameplay V1.1: bỏ mục 5.4 (2 ô chủ động), ghi 6 ô = Q + 5; "thiên phú chủ động" = Dị Năng (ô A = Q); Lõi (thiên phú bị động) gắn lõi sống; cập nhật mục 10 và mục 14 câu 1.
- `docs/GAME_DESIGN.md` mục 3 (Tạo nhân vật): Dị Năng đổi được bằng dụng cụ; Thiên Phú bị động đổi tên **Lõi**; Khuyết Điểm chọn theo Lõi; chống trùng; 12 Lõi nháp.
