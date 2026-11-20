# Chuyển Sinh Trong Hang: thiết kế sinh tồn, nuốt-học và mê cung khoang

**Ngày:** 2026-11-19
**Trạng thái:** đã thảo luận và chốt từng mục với chủ dự án; chờ duyệt bản viết này.
**Cảm hứng:** cơ chế và cảm giác của thể loại "chuyển sinh thành slime" (anime), TFT, *So I'm a Spider, So What?*, Rain World, Subnautica, Outer Wilds, Hades. Chỉ lấy **cơ chế**; mọi tên gọi, nhân vật, cốt truyện là của riêng game.

## 1. Tầm nhìn

Người chơi là một slime vừa **chuyển sinh** trong một hang sâu. Ngoài kia là một thế giới rộng lớn, nhìn thấy được qua lỗ trần, nhưng không ra được. Phải **tự mò đường** qua một mê cung khoang không bản đồ, sống sót bằng cách **nuốt mọi thứ để học**, và **tiến hóa thành một thứ không ai biết trước**.

Ba cảm giác cốt lõi:
1. **Sinh tồn thật:** đói ẩm, lũ, kẻ săn mồi, phải về hang trú đúng lúc.
2. **Mạnh dần nhờ nuốt:** mỗi sinh vật là một bí ẩn "nó cho mình cái gì?".
3. **Số mệnh:** "omg mình sẽ thành gì sau tiến hóa?".

Thiết kế này **không dựa** vào hệ thống cũ khi hệ cũ mâu thuẫn; hệ cơ quan nhặt-gắn hiện tại được thay (mục 6).

## 2. Ra đời: cảnh Tiếng Vọng

Một giọng vô hình, **Tiếng Vọng**, là "tiếng nói của thế giới". Nó thông báo mọi sự kiện lớn (học kỹ năng, tiến hóa, danh hiệu).

Cảnh ra đời, khoảng 15–20 giây, có thể bỏ qua:
1. Tối hoàn toàn; một giọt nhờn rơi từ trần, tiếng "tách" vang trong hang.
2. *«Xác nhận… Cá thể mới hình thành.»*
3. *«Ban tặng Ân Điển Khởi Sinh.»* Sáu thiên phú cố định (mục 3) lần lượt khắc vào thân, mỗi cái một nhịp sáng.
4. *«Phát hiện Lõi độc nhất: […]»*: ký hiệu sáng rực, **rút ngẫu nhiên**.
5. *«Phát hiện khiếm khuyết: […]»*: ký hiệu tối, nứt, đến chậm hơn, **rút ngẫu nhiên**.
6. *«Dị Tượng của hang: […]»* (mục 10).
7. Slime tỉnh dậy trong bóng tối.

Co-op: mỗi người có cảnh riêng cùng lúc, rồi thấy nhau ra đời.

## 3. Ân Điển Khởi Sinh (cố định, mọi slime đều có)

Thứ cơ bản, **dùng nhiều thì lên cấp và biến tấu**.

| Thiên phú | Làm gì | Biến tấu khi thành thạo |
|---|---|---|
| **Túi Không Đáy** | Kho đồ trong bụng | Chứa nhiều hơn; bắn đồ ra như đạn; giữ một phần đồ khi chết |
| **Nuốt Chửng** | Nuốt thứ nhỏ hơn mình; thứ to hơn thì bám vào hòa tan dần (chậm, ồn) | Hòa tan nhanh; nuốt thứ to hơn; kéo con mồi từ xa |
| **Giải Mã** | Phân tích thứ đã nuốt để học | Học nhanh; mở thông tin tỉ lệ (mục 5.3); thấy điểm yếu; đoán đòn |
| **Biến Hình** | Nặn cơ quan đã học lên người | Đổi nhanh; thêm ô cơ thể; đổi 1 cơ quan ngoài hang trú; giả dạng sinh vật khác |
| **Sóng Dò** | Quét sóng âm để "nhìn" khi chưa có mắt (phím C hiện tại) | Quét xa; thấy xuyên vách mỏng; nghe nhịp tim kẻ trốn |
| **Phân Thân** | Tách một mảnh nhỏ ra, nuốt lại được | Mồi nhử; đánh dấu đường; điều khiển mảnh đi trinh sát (về sau: đàn Phân Thân) |

## 4. Số mệnh: Lõi và Khuyết Điểm

- Mỗi slime nhận **1 Lõi + 1 Khuyết Điểm**, rút ngẫu nhiên, **thấy ngay** trong cảnh Tiếng Vọng, không được chọn.
- Kho ban đầu: **~12 Lõi**, **~10 Khuyết Điểm**. Kho **mở rộng qua các lần chuyển sinh** (mục 11).
- Ví dụ Lõi: *Màng Thẩm Thấu* (đứng yên trên rêu thì no dần), *Dòng Máu Tham Lam* (+50% tỉ lệ kỹ năng hiếm).
- Ví dụ Khuyết Điểm: *Mau Khô* (mất ẩm nhanh gấp đôi).

## 5. Vòng chơi cốt lõi: Nuốt, Bụng, Giải Mã

### 5.1 Nhu cầu sinh tồn (chỉ 2)
- **Khối lượng:** vừa là máu, vừa là giới hạn nuốt. Bị đánh thì bắn mất khối và nhỏ đi. Nặn cơ quan cũng tốn khối.
- **Độ ẩm:** chỗ khô làm hao khối; nước, rêu ướt, hầm ngập thì hồi.

### 5.2 Bụng (Túi Không Đáy)
Sức chứa có hạn. Mỗi thứ nuốt vào có ba cách dùng:
- **Tiêu hóa:** lớn lên và hồi lại.
- **Phân tích:** học.
- **Giữ lại:** nhả ra làm công cụ (đá chặn hang, nước giữ ẩm, xác làm mồi). Trong co-op có thể nhả cho nhau.

### 5.3 Giải Mã
- Phân tích **chạy chậm khi di chuyển**, **nhanh gấp nhiều lần khi nghỉ ở hang trú**.
- Mỗi loài có **bảng di truyền**: các cơ quan và kỹ năng học được, mỗi dòng có độ hiếm.
- Mỗi mẫu là một lần **may mắn** (có thể ra ngay) cộng một lần **tích lũy** (thanh đầy là chắc chắn có).

| Độ hiếm | Cơ hội mỗi mẫu | Tích lũy mỗi mẫu | Chắc chắn sau |
|---|---|---|---|
| ⚪ Thường | 25% | +15% | 7 mẫu |
| 🟢 Hiếm | 8% | +7% | 15 mẫu |
| 🔵 Quý | 2% | +4% | 25 mẫu |
| 🟡 Huyền thoại | 10%, chỉ từ **con đầu đàn** | +34% | 3 con đầu đàn |

Số liệu là đề xuất, sẽ chỉnh khi chơi thử.

**Chất lượng mẫu:**
- Nuốt sống hoặc vừa giết: ×1.
- Xác cũ: ×0.5.
- Xác thối: ×0.2, có nguy cơ ngộ độc.
- Bị loài khác ăn một phần: có thể mất hẳn vài dòng.

**Tăng tỉ lệ:** cấp Giải Mã, Lõi, Danh Hiệu, Ân Huệ, Dị Tượng.

**Hiển thị mở dần theo Giải Mã:** ban đầu chỉ thấy thanh tích lũy và dòng "???"; lên cấp thì thấy tên; cao hơn nữa thì thấy % cơ hội.

**Thông báo:** *«Phân tích hoàn tất 40%…»*, *«Đã thu được [Nhả Tơ]»*.

### 5.4 Lên Sao
Sau khi học được một thứ, mẫu cùng loài dùng để lên sao: **3 mẫu ★2, 9 mẫu ★3**. Đã ★3 thì mẫu chỉ còn là thức ăn.

### 5.5 Dung Hợp
Hai nguyên liệu trong Túi Không Đáy ghép thành một vật phẩm mạnh, ví dụ Tơ Nhện + Quặng Sắt = Dây Thép (bám vách). Bảng công thức để người chơi tự khám phá.

## 6. Cơ thể: cơ quan học từ việc nuốt (thay hệ nhặt-gắn cũ)

- **Không còn vật phẩm cơ quan rơi ra.** Muốn có mắt thì phải nuốt sinh vật có mắt.
- Cơ quan đã học được **nặn từ khối cơ thể**: tốn khối, không tốn EXP; cơ quan càng mạnh càng tốn.
- Giữ **4 ô**: Mắt, Chân, Da, Lưng. Mỗi lần tiến hóa **mở thêm 1 ô**.
- **Đổi cơ quan tự do ở hang trú** từ kho đã học. Biến tấu của Biến Hình cho phép đổi 1 cơ quan ở ngoài.
- **Cộng Hưởng:** mỗi cơ quan hoặc kỹ năng thuộc một trong **4 họ** (ví dụ Hang Ẩm, Săn Đêm, Đá, Độc). Gắn **2, 4, 6** thứ cùng họ thì mở hiệu ứng mạnh dần.
- Ban đầu khoảng **20 kỹ năng hoặc cơ quan** chia trong 4 họ.
- Khi chưa có mắt: thế giới đơn sắc, chỉ thấy bằng Sóng Dò (hệ thị giác hiện có giữ nguyên).

**Chuyển đổi từ hệ cũ:** `OrganSO`, `OrganHost` (ô, cấp, hiệu ứng) được tái dùng làm dữ liệu cơ quan. `OrganPickup` và EXP nuôi cơ quan bị thay bằng học qua Giải Mã và chi phí khối. Chi tiết nằm trong plan.

### 6.1 Sửa đổi sau chơi thử (2026-11-19): kỹ năng thay cho cơ quan

Cơ thể slime **trơn, không gắn gì cố định**. Mỗi gene học được là một **kỹ năng**:

- **Bị động** (mắt, màng chịu nhiệt…): luôn bật; tối đa **3**; chỉ nặn hoặc tháo ở **hang trú**, tốn khối như trên. Không bị động nào hiện hình, kể cả mắt (đổi ý sau chơi thử): thân slime luôn trơn.
- **Chiêu chủ động** (phóng gai, chạy nước rút, lao tới…): gắn phím **1–5 ở bất cứ đâu**, không tốn gì. Mỗi lần dùng tốn năng lượng và có hồi chiêu. Bộ phận **chỉ hiện hình lúc vận chiêu**: mọc ra, dùng, rồi tan vào thân.
  - Gai phản đòn chỉ có tác dụng khi gai đang trồi.
  - Chân (chân giả, chân chạy) cho chiêu **Chạy Nước Rút**: ×tốc độ của loại đó trong vài giây.
- Học xong là dùng được ngay (★1). ★2 và ★3 mạnh hơn.
- Bảng G: Bị động. Bảng K: Chiêu.

## 7. Thức ăn và Mẫu Sống

- **Thức Ăn** (rêu thường, nước, côn trùng nhỏ): để sống; khá nhiều; hồi lại theo chu kỳ lũ; **không cho kỹ năng**.
- **Mẫu Sống** gồm quái vật và **cây đặc biệt**, mỗi loài có bảng di truyền.
- Cây cũng cho kỹ năng: có cái vô dụng, có cái hai mặt. Ví dụ:
  - *Rêu Phát Quang:* Tỏa Sáng Nhẹ (thấy đường, nhưng bị thấy).
  - *Nấm Độc Tía:* Kháng Độc.
  - *Hoa Hương Hang:* Tỏa Hương (dụ quái).
  - *Dây Leo Ẩm:* Hút Ẩm.
  - *Cỏ Gai Đá:* Da Gai.
- Phần lớn cây cỏ đang có trong map trở thành **trang trí**.
- Mật độ mỗi khoang: **4–6 loài quái, tổng 10–20 con**, và **2–3 loài cây đặc biệt**.
- Ban đầu: **~8 loài quái**, **~6 loài cây đặc biệt**.

## 8. Hệ sinh thái sống

- Sinh vật **săn lẫn nhau** kể cả khi không có người chơi; nấp chờ chúng đánh nhau rồi nuốt xác là chiến thuật hợp lệ.
- Xác nằm lâu sẽ bị **loài khác tranh ăn**.
- Mỗi loài có **ổ** và **đường đi** giữa các khoang.
- **Sinh sản sau mỗi chu kỳ lũ**, đến mức trần của khoang. Không tuyệt chủng.
- **Loài Cảnh Giác** (chống farm): săn một loài càng nhiều, nó càng cảnh giác.
  - **Mức 1:** thấy là chạy.
  - **Mức 2:** đi theo đàn.
  - **Mức 3:** phục kích ở hầm hẹp.

  Để yên vài chu kỳ thì cảnh giác giảm dần.
- **Con đầu đàn:** mỗi khoang tối đa 1, không hồi sinh, là nguồn huyền thoại duy nhất.
- **Nghe trước khi thấy:** sinh vật lớn gầm vang qua hầm trước khi tới; Sóng Dò bắt được bóng lớn ở xa; một số khoang có vọng âm.

## 9. Mê cung khoang

### 9.1 Cấu trúc
- **12–16 khoang**, chia **3 tầng sâu**: Nông, Giữa, Sâu. Càng sâu càng tối, khô, ít đồ ăn, quái mạnh hơn.
- Mỗi khoang có **2–3 đường hầm**. Đường lên chưa chắc lên mặt đất; đường xuống chưa chắc đi xuống.
- **Không bản đồ, không chỉ dẫn.**
- **Xáo mỗi vòng chơi**, và **giữ nguyên qua các lần tái tạo** trong cùng vòng.
- **Kỹ thuật:** một scene; prefab khoang đặt xa nhau. Chỉ khoang hiện tại và khoang kề được bật. Hoán đổi ẩn ở **giữa đường hầm**: dịch chuyển khi người chơi ở đoạn hầm khuất.

### 9.2 Hang trú
- Khoang nhỏ, **cao hơn mực lũ**, có rêu ẩm, lối vào hẹp (quái lớn không vào được).
- Là nơi duy nhất để: tránh lũ, phân tích nhanh, đổi cơ quan, kết kén, tái tạo.
- Rải **vừa đủ thưa** để mỗi chuyến đi luôn có câu hỏi "có kịp về trước lũ không?".

### 9.3 Chu Kỳ Lũ
- Khoảng **12 phút** một chu kỳ. Trước 1 phút có **tiếng ầm cảnh báo**, nước rỉ ra từ vách.
- Mỗi khoang có **tầng thấp** (ngập) và **gờ cao** (an toàn). Hầm đi xuống ngập trước; hầm đi lên an toàn hơn nhưng khô hơn.
- Bị lũ bắt ngoài chỗ cao thì bị cuốn đi: mất khối, trôi sang khoang khác.
- Lũ mang **thức ăn mới** về và kích hoạt sinh sản.

### 9.4 Kích thước hầm
- **Khe nhỏ:** chỉ slime nhỏ lọt, an toàn.
- **Hầm lớn:** quái to đi lại.
- Slime lớn lên thì mạnh nhưng **mất dần lối khe nhỏ**. Tách Phân Thân để tạm nhỏ lại.

### 9.5 Chướng ngại tự nhiên (cơ thể là chìa khóa)
Không có cửa, không có nhiệm vụ.
- Hầm ngập cần thở nước.
- Vách cao cần bám vách.
- Vách đá vụn cần tiết axit.
- Hầm nóng khô cần màng chịu nhiệt.
- Vùng tối đặc cần Sóng Dò mạnh.

### 9.6 Khoang đặc biệt
- **Hang Khởi Sinh** (map hiện có) là khoang xuất phát.
  - Hồ là nguồn lũ đầu tiên.
  - Lỗ trần thấy bầu trời là lời trêu: thế giới ngoài kia không với tới được.
  - Vách hang cần có: miệng hầm nhiều cỡ, gờ trú lũ, khe nứt, và một hang trú đầu tiên.
- **Khoang Phong Ấn** ở tầng Sâu: nơi của **sinh vật cổ đại** bị phong ấn, có bộ xương rồng. Tiếng thở của nó nghe được từ nhiều nơi.
- **Lối giả:** dẫn lên cao nhưng là ngõ cụt.

### 9.7 Lối ra (kết chương 1)
Lối thật nằm sau Khoang Phong Ấn. Sinh vật cổ đại chỉ đường khi slime đã **tiến hóa ít nhất 2 lần**. Thế giới bên ngoài là phần sau.

## 10. Dị Tượng

Mỗi vòng chơi, hang có một điều kỳ lạ ngẫu nhiên, công bố trong cảnh Tiếng Vọng. Ví dụ:
- *Hạn hán:* vũng nước co lại, lũ yếu.
- *Mùa sinh sản:* ổ sinh vật đông gấp đôi.

## 11. Chết và chuyển sinh

- **3 lần tái tạo** mỗi vòng.
- Chết thì để lại **giọt lõi** tại chỗ; tái tạo ở hang trú gần nhất với cơ thể nhỏ.
- **Mất đồ trong bụng** (trừ phần Túi Không Đáy giữ được); **giữ kỹ năng và cơ quan đã học**.
- Quay lại nuốt giọt lõi thì lấy lại phần khối đã mất, nếu chưa bị sinh vật khác ăn.
- Co-op: đồng đội **nuốt giọt lõi** của bạn để hồi sinh bạn ngay, không tốn mạng.
- **Hết mạng thì chuyển sinh:** vòng mới, mê cung xáo lại, số mệnh rút lại, mất kỹ năng.
- **Giữ qua các vòng:**
  - **Sổ ký ức:** dạng tiến hóa đã thành, loài đã phân tích; dạng chưa gặp là ô bí ẩn.
  - **Danh Hiệu** đã mở.
  - **Lời kể của sinh vật cổ đại:** nó nhớ bạn và mỗi lần kể thêm một mảnh bí mật.
  - **Lõi mới được thêm vào kho số mệnh.**

## 12. Tiến hóa

- **Điều kiện:** đủ khối lượng và đủ số kỹ năng hoặc cơ quan đã học.
- Kết kén ở hang trú. Lúc này rất yếu, đây là một khoảnh khắc sinh tồn.
- **Trước đó** có dấu hiệu nhỏ trên thân (màu ngả, vân lạ, dáng hơi đổi) để người tinh ý đoán được phần nào.
- **Dạng mới không biết trước**, không có cây tiến hóa. Nó là pha trộn của:
  - **những gì đã nuốt nhiều nhất**,
  - cách sống (ướt/khô, sáng/tối, lạnh/ấm),
  - Lõi,
  - một phần ngẫu nhiên.
- Phá kén: Tiếng Vọng đọc *«Tiến hóa thành công. Chủng tộc mới: […]»* và cho **chọn 1 trong 3 Ân Huệ** ngẫu nhiên (bạc, vàng, cầu vồng); cơ thể **mở thêm 1 ô**.
- **3 lần tiến hóa** mỗi vòng, mỗi lần rẽ nhánh.
- Cảnh phá kén dùng cùng ngôn ngữ hình ảnh với cảnh ra đời.

## 13. Danh Hiệu

Làm một việc đủ nhiều thì Tiếng Vọng ban danh hiệu kèm kỹ năng hoặc chỉ số. Ví dụ:
- *Kẻ Ăn Đồng Loại:* ăn 50 con cùng loài.
- *Kẻ Không Chịu Chết:* sống sót 10 lần dưới 10% khối.
- *Thợ Săn Nhện:* tăng tỉ lệ từ nhện.

Danh Hiệu được giữ qua các vòng.

## 14. Co-op

- **2–4 người.** Mỗi người có Lõi và Khuyết Điểm riêng; ai nuốt người đó học, không chia sẻ tiến độ.
- Nhả đồ cho nhau, hồi sinh bằng giọt lõi.
- *(Để sau)* **Kho Báu Chia Nhau:** gặp ổ trứng hay xác lớn thì lần lượt chọn, người yếu nhất chọn trước.

## 15. Để sau

- **Tự vẽ bản đồ:** đánh dấu bằng Phân Thân, nuốt "vỏ ký ức".
- **Đàn Phân Thân.**
- **Kho Báu Chia Nhau.**
- **Thế giới bên ngoài hang.**

## 16. Thứ tự làm

1. **Vòng lõi:** Nuốt Chửng, Túi Không Đáy, Giải Mã với tỉ lệ, Biến Hình đổi cơ quan ở hang trú. Chuyển hệ cơ quan cũ sang học qua Giải Mã.
2. **Cảnh Tiếng Vọng:** Ân Điển, Lõi, Khuyết Điểm. Kho số mệnh.
3. **Mê cung khoang:** hoán đổi ẩn giữa hầm, hang trú, Chu Kỳ Lũ, kích thước hầm, chướng ngại. Vách Hang Khởi Sinh.
4. **Hệ sinh thái:** săn lẫn nhau, sinh sản theo lũ, Loài Cảnh Giác, con đầu đàn, nghe trước khi thấy.
5. **Tiến hóa** với Ân Huệ, Danh Hiệu, Cộng Hưởng, Lên Sao, Dung Hợp.
6. **Chết, tái tạo, chuyển sinh;** sinh vật cổ đại; Dị Tượng; Sổ ký ức.

Mỗi bước là một plan riêng, có test EditMode cho phần thuần (tỉ lệ, tích lũy, cảnh giác, lũ, xáo mê cung).

## 17. Chỉ tiêu kỹ thuật

- Máy mục tiêu: RTX 3050 6 GB, Full HD, ≥ 90 FPS.
- Chỉ khoang hiện tại và khoang kề được bật.
- Số liệu nằm trong ScriptableObject hoặc lớp thuần để chỉnh khi chơi thử.
