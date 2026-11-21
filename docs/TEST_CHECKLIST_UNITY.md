# Danh sách việc cần làm và kiểm thử trong Unity

> **Nhánh `claude/unity-test-maze`** = nhánh `claude/unity-test` + toàn bộ việc của bên làm hang (`maze-step3`: mê cung, 8 hang, 32 sinh vật có mô hình và hoạt hình). Dùng nhánh này để thử với mô hình thật; `claude/unity-test` giữ nguyên để thử riêng phần của Claude. Thêm trên nhánh này: menu **`Evolve → Attach Creature Brains`** (chạy một lần sau khi chạy các menu tạo dữ liệu của bên làm hang để họ có prefab): gắn não và phòng vệ vào 32 prefab sinh vật. Bảng mê cung của bên làm hang mở bằng **F10**; menu thần của Claude vẫn là **F1**. Hai test của bên làm hang (`MazeGenerator_IncludesWave2NongCaves`, `MazeGenerator_IncludesAllFourGiuaCaves`) báo đỏ trên chính nhánh của họ: không phải do Claude.

Mọi thứ đã làm trong phiên này đã gộp vào **một nhánh duy nhất: `claude/unity-test`**. Phần Unity **chưa từng biên dịch**: việc đầu tiên là xem Console. Khi gặp lỗi hoặc thứ gì lạ, chụp ảnh gửi cho Claude (ảnh Console hoặc video), ghi rõ làm bước nào.

## 1. Kéo nhánh
1. Đóng Unity. Trong thư mục dự án: `git fetch origin` rồi `git checkout claude/unity-test` (hoặc kéo nhánh này bằng GitHub Desktop).
2. `git lfs pull` (các file âm thanh `.mp3` lưu bằng Git LFS: thiếu thì tiếng kỹ năng im lặng).
3. Mở Unity, chờ biên dịch xong. **Nếu Git báo CONFLICT thì dừng, đừng chạy `git add .`**, báo Claude.
4. Console đỏ: chụp ảnh gửi Claude trước khi làm bước sau.

Các nhánh nhỏ (chỉ cần nếu muốn tách): `claude/quirky-carson-xnvto6` (kỹ năng hang, hình ảnh), `claude/encounter-rewards` (phần thưởng kỳ ngộ), `claude/prey-defense` (con mồi chống trả), `claude/ecology-b1` (mô hình sinh thái), `claude/ecology-b2` (hành vi sinh vật). `claude/unity-test` đã gộp hết.

## 2. Chạy các menu tạo dữ liệu (một lần, theo thứ tự)
1. `Evolve → Create Traits` (nếu chưa chạy).
2. `Evolve → Create Cave Skills`: tạo 12 gene, 12 bảng rơi, 9 chiêu và 2 vật liệu hình ảnh trong `Resources/CaveSkills`. Console báo "Đã tạo hoặc cập nhật 12 kỹ năng hang".
3. `Evolve → Create Encounter Genes`: tạo 2 gene tạm cho kỳ ngộ.
4. Lưu dự án (Ctrl+S) rồi **Play**.

## 3. Test Runner
`Window → General → Test Runner → EditMode → Run All`. Báo lại số test đỏ (nếu có) kèm tên. Ngoài Unity, 226 test thuần đã xanh.

## 4. Menu thần (F1; bảng mê cung của bên làm hang là F10) có gì mới
Menu thần giờ chia **6 tab**: Bản thân (bất tử, hồi, mạng, khối slime, tốc độ game), Kỹ năng (học kỹ năng, danh sách cơ quan, phần thưởng kỳ ngộ), Sinh vật (hành vi, cỡ và độ khó, tạo quái thử), Combat (Vồ, chiêu của quái, pha hiện tại), Thế giới (dịch chuyển, mê cung, sinh thái), Khác (bệnh, Túi, thử tỉ lệ rơi). Menu tự phóng to theo độ cao màn hình; nút **A−** và **A+** chỉnh cỡ thêm, nhớ lại lần sau. Danh sách dài (cơ quan, con mồi, kẻ săn, Chúa Hang, phần thưởng, thử tỉ lệ rơi) nằm trong mục thu gọn, bấm tiêu đề để mở.
Mục **"Kiểm thử"** (cuộn xuống dưới "Sinh vật"):
- **Kích cỡ slime:** nút 3, 10, 20, 40, 80 để đặt khối.
- **Kỹ năng hang:** học 9 kỹ năng chủ động ★1, lên ★3 hết, gắn tối đa bị động hang.
- **Sinh vật thử:** tạo bất kỳ con mồi (22), kẻ săn (11), Chúa Hang (11) ngay trước mặt slime (hình cầu đặt tên đúng loài nên tự có phòng vệ và não); tạo xác lớn hoặc nhỏ; xóa sinh vật thử.
- **Phần thưởng kỳ ngộ:** từng hàm một nút (gene Hiếm+, Gene Lạ, Gene Cổ, +5 khối, đổi Khuyết Điểm, +8 °C chịu nhiệt, Lõi mạnh lên, mở màu Cầu Vồng, câu Tiếng Vọng, nhiễm bệnh thử / hết bệnh, hồi đầy, bỏ 2 món vào Túi).
- **Mô phỏng sinh thái 2 giờ chơi:** in bảng quần thể từng loài ra Console.
- Ô bật tắt "Sinh vật săn nhau, ăn xác, theo kích cỡ slime (B2)" ở mục "Sinh vật".
- Dòng đầu mục hiện máu, kháng nhiệt, Lõi và hệ số, Khuyết Điểm.
- Có sẵn: "Dị Năng không hồi chiêu", "Vô hạn năng lượng", "Bất tử". **Bật những cái này trước khi thử kỹ năng.**

## 5. Danh sách kiểm thử

### A. Biên dịch (làm đầu tiên)
- [ ] Console không đỏ sau khi mở Unity và sau khi Play.
- [ ] Chạy ba menu ở mục 2 không báo lỗi.

### B. 12 kỹ năng hang (mục 41 của PROJECT_STATUS)
Bật "Vô hạn năng lượng" và "Không hồi chiêu". Bấm "Học kỹ năng chủ động ★1", mở menu `K` để xếp vào phím 1–5 (tối đa 5 ô: đổi từng đợt). Tạo vài con mồi thử trước mặt để có mục tiêu.
- [ ] **Ợ Chấn:** sinh vật quanh 4 m bị đẩy văng; vòng sóng và bụi bắt đầu từ mép thân.
- [ ] **Châm Choáng:** thân kéo ra một gai nhọn đâm con gần nhất, nó choáng 1 giây, có sao quay trên đầu; không gây sát thương.
- [ ] **Lưỡi Dài:** thân kéo ra lưỡi dính con mồi trong 6 m và kéo về; lưỡi liền với thân, cùng màu thân, không thấy vết nối.
- [ ] **Lách Né:** lách ngang ~2 m, không nhận sát thương lúc lách; gió phía sau.
- [ ] **Thu Mình:** thân co nhỏ 3 giây, không di chuyển, nhận ít hơn nửa sát thương (thử nhận đòn từ kẻ săn thử).
- [ ] **Khiên Quặng:** mảnh tinh thể xoay quanh thân; đòn đầu tiên không mất máu, mảnh vỡ ra.
- [ ] **Phát Sáng:** có đèn quanh slime 10 giây; kẻ săn phát hiện từ xa hơn.
- [ ] **Cắm Gai:** bụi gai tinh thể tím mọc ở **trước mặt** slime, sinh vật dẫm vào mất máu, 20 giây thì tan.
- [ ] **Bám Vách:** lao vào vách rồi dùng, slime bò lên, thân ấn "chân" thạch vào vách.
- [ ] **★3:** bấm "Lên ★3 hết": mỗi kỹ năng có thêm cơ chế nhỏ (xem mô tả trong K).
- [ ] **Năng lượng:** đầy thanh (100) dùng được 3 kỹ năng liền, không được 4.
- [ ] **Tiếng:** mỗi kỹ năng chủ động có một tiếng khi dùng (bị động thì im).

### C. Hình ảnh kỹ năng: "phải liền như thân"
- [ ] Lưỡi, gai, chân bám kéo ra từ chính thân, **không có vật thể nào nằm trong thân nhìn xuyên qua**.
- [ ] Đổi màu slime (F3): các bộ phận đổi theo.
- [ ] Lưỡi dài 6 m: có bị gãy khúc, có lộ cạnh không (báo lại).
- [ ] Chạy Nước Rút (chân bước) và Phóng Gai (gai trên lưng): kéo ra từ thân, không gắn model riêng.
- [ ] Hiệu năng: bật F4 xem có tụt khung hình khi dùng nhiều kỹ năng.

### D. Bị động và 5 ô (Biến Hình trong hang trú)
- [ ] "Gắn tối đa bị động hang": Da Dày +10 máu tối đa; Vỏ Cứng bị cắn mất ít hơn; Sóng Dò Mạnh: vòng sóng dò (phím quét) đi xa hơn rõ, khoảng gấp 1,5.
- [ ] Bảng Biến Hình hiện **5 ô bị động** thay cho các ô cơ thể; nặn, tháo hoạt động đúng.

### E. Con mồi chống trả (mục 43)
Tạo con mồi thử rồi lại gần.
- [ ] **Thỏ Lá:** chạy ngoằn ngoèo, lách né.
- [ ] **Ốc Hồ:** chạm vào thì rụt vỏ, **chưa nuốt được trong 3 giây**.
- [ ] **Bọ Gai Tím:** rải bẫy gai sau lưng khi bị đuổi; bị cắn mất máu nhiều.
- [ ] **Dế Gió:** cả đàn quanh 12 m cùng chạy (tạo vài con).
- [ ] **Chim Non:** bị đánh thì kêu cứu, **Chim Hang Mẹ** (tạo bằng nút Chúa Hang, tên bắt đầu `Chim Hang Mẹ`) lao tới.
- [ ] **Tắc Kè Đá:** bị tóm thì thoát, để lại hình giả.
- [ ] Slime nhỏ (khối 3) thì con mồi dám trả đũa; slime lớn (khối 40) thì chạy sớm hơn.
- [ ] Sên Rêu làm chậm slime khi trả đũa (có thể quá mạnh hoặc quá yếu: báo lại).

### F. Hành vi sinh vật (B2, mục 46)
Tạo **cùng hang**: ví dụ kẻ săn `Thằn Lằn Bờ Hồ` + con mồi `Bọ Cánh Cứng Nhỏ`. Có thể bật "Đóng băng" rồi tắt để dàn cảnh. Đợi 1–3 phút cho kẻ săn đói.
- [ ] Kẻ săn đói đuổi và ăn con mồi (không chỉ đuổi slime). Con mồi chết để lại **xác**.
- [ ] `Giòi Trắng`, `Kền Kền Hang`, `Bọ Ăn Xác` tạo cạnh một xác thì đi tới ăn.
- [ ] `Chó Xương` + "Xác lớn gần": nó canh xác, đuổi mọi thứ lại gần kể cả slime.
- [ ] `Cóc Hồ Già` (Chúa Hang) ăn cả kẻ săn `Thằn Lằn Bờ Hồ` và `Bọ Cánh Cứng Nhỏ`.
- [ ] Hai kẻ săn khác loài gặp nhau: tránh hoặc đánh nhau.
- [ ] Đặt khối slime 3: kẻ săn đuổi slime; khối 40–80: kẻ săn né slime.
- [ ] Tắt ô "B2" ở mục "Sinh vật": về cách cũ (chỉ đuổi slime).

### G. Phần thưởng kỳ ngộ (mục 45)
- [ ] **Gene Hiếm+:** học thêm một gene, hiện trong bảng kỹ năng.
- [ ] **Gene Lạ / Gene Cổ:** hiện tên gene (bản tạm).
- [ ] **Đổi Khuyết Điểm:** danh sách đổi, không ra lại đúng cái cũ (bấm vài lần).
- [ ] **+8 °C chịu nhiệt:** HUD hiện tăng 8, tối đa +20 °C.
- [ ] **Lõi mạnh lên:** hệ số Lõi trong dòng đầu mục tăng ×1,15 → 1,3 → 1,45 rồi dừng.
- [ ] **Mở màu Cầu Vồng:** F3 hiện ô "Cầu Vồng"; bật thì thân đổi sắc liên tục; tắt rồi bật lại Play vẫn còn khóa.
- [ ] Nhiễm bệnh thử rồi Hết bệnh; Hồi đầy; Bỏ 2 món vào Túi (mở Túi kiểm tra); câu Tiếng Vọng hiện trên màn hình.

### G2. Di chuyển (nhánh `claude/unity-test-maze`)
- [ ] Cùng loài không cắn nhau: tạo 3 Thằn Lằn Bờ Hồ gần nhau, để chúng đói (khoảng 1–2 phút) rồi xem. Bẫy gai của Bọ Gai Tím không làm hại Bọ Gai Tím.
- [ ] Sinh vật không đi xuyên nhau. Tạo 5 con cùng một chỗ: chúng tách dần ra, không bật văng.
- [ ] Sinh vật không đi xuyên vách, đá lớn. Gặp vách thì đi vòng hoặc quay hướng khác, không đứng đâm đầu mãi.
- [ ] Không còn con nhảy lên rồi rơi xuống liên tục trước bục cao. Con dưới gầm cầu không vọt lên mặt cầu.
- [ ] Đi trên sàn gồ ghề: hoạt hình không giật.
- [ ] Loài khác nhau di chuyển khác nhau:
  - Thỏ Lá, Dế Gió nhảy lên được bục khoảng 0,9 m.
  - Nhện, Thằn Lằn leo được bậc khoảng 1,5 m.
  - Dơi, Đom Đóm không sợ vực.
  - Ốc Hồ chỉ qua bậc thấp.
- [ ] Ô "Di chuyển mới cho quái" ở mục Kiểm thử: tắt thì quay về kiểu cũ, để so sánh.
- [ ] **Tổ:** tạo khoảng 8 con mồi cùng loài một chỗ (nút trong "Sinh vật thử"). Phải thấy:
  - 2 cặp đi cạnh nhau ra ngoài kiếm ăn, khoảng 20 giây đổi hướng.
  - 1–2 con chạy vòng mép.
  - Số còn lại quanh tổ.
  - Slime đến gần tổ khoảng 10 m thì cả tổ bỏ chạy.
- [ ] **Slime:**
  - Bò qua đá nhỏ, bậc thấp (khoảng 40% bề ngang thân), chậm và đều, không đứng đâm đầu.
  - Vách cao hơn thì không leo được.
  - Slime lớn qua được đá lớn hơn.

### H. Hệ sinh thái (số liệu)
- [ ] "Mô phỏng sinh thái 2 giờ": Console in bảng 11 hang; con mồi trung bình 7–9, kẻ săn 1–2, Chúa Hang 1, không loài nào 0 liên tục.

### H2. Sinh thái sống trong game (B1 nối vào hang)
- [ ] Vào một hang có sinh vật. Bấm "In quần thể đang sống của mọi hang". Console phải in đủ 11 hang:
  - mỗi hang có danh sách hang kề trong ngoặc. Khi có mê cung, danh sách này đi theo đường nối thật, sau khoảng 5 giây chơi;
  - mỗi loài có số con, sức chứa và % no.
- [ ] Đợi vài phút rồi in lại: số con thay đổi hợp lý, kẻ săn không ăn sạch con mồi, có loài di cư sang hang kề (ghi "(khách)").
- [ ] "Mô phỏng sinh thái 2 giờ": dòng đầu ghi "đường nối theo mê cung thật (N cặp hang)" khi đang trong mê cung.
- [ ] Không có lỗi đỏ khi vào hoặc ra hang, kể cả khi chuyển qua lại nhiều hang.

### H3. Khoang rộng đỡ trống (phải dựng lại mê cung trước)
Chạy `Evolve → Build Maze World (Step 3)` để dựng lại. Các thay đổi này nằm ở bước dựng, nên cảnh cũ không đổi.
- [ ] Ở một khoang 75 m (ví dụ Vườn Quặng Cầu Vồng), đi ra xa tâm: ở ba vùng quanh sàn đều gặp sinh vật, không chỉ quanh tâm.
- [ ] Trong cảnh (Hierarchy, "Hệ Sinh Thái Hang") mỗi loài có 3 tổ, riêng Chúa Hang chỉ 1. Hang Khởi Sinh vẫn 1 tổ mỗi loài như cũ.
- [ ] Nhiều cây, đá tảng và thức ăn hơn, nhìn không còn trống. Cụm cảnh chủ đề (nấm khổng lồ, tinh thể, đá vách…) lặp lại ở hai vòng ngoài, không đặt ngay trước cửa hầm.
- [ ] "In quần thể đang sống của mọi hang": hang rộng có số con gần gấp đôi hang cũ (con mồi 15 thay vì 8), Chúa Hang vẫn 1.
- [ ] Không có lỗi đỏ khi dựng lại. FPS không tụt rõ so với trước: nếu tụt, nói cho mình biết để giảm.
- [ ] Con vừa thả ra (từ bộ tạo bầy) giờ tính vào quần thể: giết một con rồi in lại quần thể, số con trong hang giảm đúng một.

### H4. Kích cỡ, độ khó theo tầng, vùng trong khoang (dựng lại mê cung trước)
Menu thần (F1), mục Kiểm thử, có thanh "Cỡ quái ×" và nút "Độ khó: theo hang / 0 / 1 / 2 / 3". Chỉ quái tạo sau khi đổi mới nhận hệ số, nên đổi rồi chờ thêm hoặc vào hang khác.
- [ ] **Cỡ:** con mồi nhỏ, kẻ săn lớn hơn, Chúa Hang to hơn hẳn slime lớn nhất (rộng từ 3 m). Slime nặng 35 đứng cạnh Chúa Hang vẫn nhỏ hơn rõ. Chúa Hang đi qua hầm được, không kẹt vách.
- [ ] **Hang đầu an toàn:** quanh chỗ slime xuất hiện (25 m) kẻ săn không đuổi. Ra xa hơn thì có 1 Thằn Lằn (không phải 2) và nó cắn nhẹ hơn.
- [ ] **Cóc Hồ Già hiền:** không đuổi slime. Đánh nó thì nó đánh trả (khoảng 30 giây), rất đau. Chạy xa và đợi thì nó bình tĩnh lại.
- [ ] **Càng sâu càng khó:** đặt "Độ khó" lần lượt 0, 1, 2, 3 rồi tạo thêm kẻ săn thử: sát thương và tầm phát hiện tăng, ở 3 có nhiều kẻ săn hơn, Chúa Hang nhanh hơn.
- [ ] **Khoang rộng có nhịp** (chỉ các khoang dựng bằng bộ dựng chủ đề: Rêu Thở, Tổ Bỏ Hoang, Nấm Huỳnh Quang, Suối Nước Nóng, Khe Gió Hú...):
  - bước vào từ cửa hầm chính thấy ngay một cụm cảnh lớn có đèn phía đối diện;
  - đèn mờ dẫn từ cụm cảnh tới các tổ;
  - không có tổ ngay trước cửa hầm;
  - một góc ẩn xa cửa hầm: vòng đá tảng hở một lối quay ra ngoài, trong có khoáng.
- [ ] **FPS** trong khoang rộng không tụt rõ so với trước (đèn mờ tối đa 12 mỗi khoang).
- [ ] Vườn Quặng Cầu Vồng và Vực Nứt Tím (khoang dựng sẵn bằng mô hình Blender) **không** có cụm cảnh, đèn và góc ẩn, nhưng có cỡ và độ khó mới. Báo mình nếu tổ ở đó nằm sai chỗ.

### H5. Combat của slime: Vồ và Ghì Chặt
Menu thần (F1), mục Kiểm thử, dòng "Combat": tạo con mồi to, kẻ săn, hoặc Chúa Hang thử trước mặt slime. Cho slime vô hạn năng lượng và bất tử nếu cần thử lâu.
- [ ] **Vồ ngoài tầm:** bấm chuột trái khi không có quái trong khoảng 4 m phía trước camera: không có gì xảy ra, năng lượng không giảm.
- [ ] **Vồ con nhỏ hơn slime** (khối từ 0,8 lần khối slime trở xuống, ví dụ Ốc Hồ khối 3 khi slime khối 10): slime bật tới và nuốt sống ngay.
- [ ] **Vồ con to hơn:** slime bật tới, leo lên lưng quái, hiện thanh Ghì Chặt (vùng xanh, khúc vàng, chấm trắng, "còn N lần sai").
- [ ] **Chơi mini game:** giữ A/D hoặc kéo chuột để kéo chấm về vùng xanh. Trong vùng xanh quái mất máu đều (khoảng 8 mỗi giây, gấp đôi trên khúc vàng). Ngoài vùng quá 0,25 giây thì sai một lần; hết lượt thì quái hất slime xuống và bỏ chạy vài giây.
- [ ] **Cú giãy mạnh** (kẻ săn, Chúa Hang): hiện chữ đỏ "GIÃY MẠNH! BẤM SPACE" khoảng nửa giây. Bấm Space đúng lúc thì vùng xanh rộng ra vài giây. Không bấm thì sai một lần.
- [ ] **Buông:** bấm Space khi không có cảnh báo thì slime nhảy xuống. Quái được thả, không kẹt.
- [ ] **Quái chết khi đang bám:** slime nuốt tại chỗ (cảnh chậm và camera áp sát khoảng 2 giây, Escape hoặc chuột trái để bỏ qua). Mở Túi: có xác mới nhất, Giải Mã cho gene.
- [ ] **Quái nhớ slime:** quái thoát chạy xa hơn và lần sau cảnh giác hơn (con mồi bỏ chạy từ xa hơn khoảng 1,5 lần).
- [ ] **Quá to:** quái nặng hơn 4 lần khối slime thì slime bật ra, hiện dòng "Quá to để vồ".
- [ ] **Hết năng lượng** giữa trận: vùng xanh hẹp lại, trận vẫn chạy, không lỗi.
- [ ] Không có lỗi đỏ trong Console, không bị kẹt trên không sau khi bám hay buông. Khi bấm vào menu thần hay HUD không vô tình vồ.

### H6. Chiến đấu của quái: chiêu báo trước
Menu thần (F1), mục Kiểm thử, dòng "Combat quái": tạo Thằn Lằn, Bọ Giáp, Rùa Pha Lê, Bầy Muỗi. Dòng bên dưới hiện pha hiện tại (Idle, Windup, Active, Recover) của quái trong 25 m. Học Lách Né để thử né. Hang đầu chậm hơn: đổi "Độ khó" thử từng tầng.
- [ ] **Thằn Lằn (Rình-Vọt):** đứng im cúi người cuộn lại khoảng 0,6 giây (thân thấp rõ, hơi lùi), rồi phóng 3 m. Né sang bên thì nó trượt qua, rồi ngồi kiệt sức khoảng 1 giây: đánh nó lúc này thấy đau gấp rưỡi.
- [ ] **Bọ Giáp (Húc):** cào đất cúi sừng 0,9 giây rung nhẹ, rồi lao thẳng 7 m. Né sang bên thì nó chạy qua. Lúc cúi sừng và lao thì **vồ bị bật ra** (có giáp); lúc kiệt sức thì vồ được. Cho nó đâm vào vách: nó choáng khoảng 2 giây rồi mới kiệt sức.
- [ ] **Rùa Pha Lê:** pha 1 có Cắn Cổ (tầm gần) và Rụt Mai–Lao (rụt vào mai rồi lao, có giáp). Máu dưới 66%: thêm Xoay Mai (có vòng đỏ trên đất báo tầm, đẩy lùi). Máu dưới 33%: thêm Gọi Bầy (Bọ Giáp trong 25 m kéo tới). Giữa các đòn nó có khoảng kiệt sức 1,5 giây.
- [ ] **Dấu hiệu rõ ở hình dáng:** nhìn từ camera sau lưng slime, ở khoảng 8 m, vẫn nhận ra quái sắp ra đòn nhờ tư thế thân (không cần nhìn thanh máu hay chữ).
- [ ] **Bầy Muỗi:** vài con bay tới slime (một con phát hiện thì cả bầy trong 15 m cùng tới), đậu lên người và hút máu (khoảng 1,5 mỗi giây). Thoát bằng: nhảy (Space), dùng kỹ năng, đánh trúng nó, hoặc chạy nhanh khoảng nửa giây (khoảng 3 m/s). Muỗi bị đuổi thì bay đi, **30 giây sau** nếu slime còn trong 15 m thì quay lại hút tiếp; ra xa thì không quay lại.
- [ ] Quái không có mục tiêu (slime trong vùng an toàn hang đầu, Chúa Hang hiền ở hang đầu) thì không gồng, không ra đòn.
- [ ] Không có lỗi đỏ khi quái chết giữa lúc gồng; quái không đứng đơ mãi (Busy kẹt) sau khi bị choáng, bị hất hay mất mục tiêu.
- [ ] Vồ vào quái đang kiệt sức: bám được và vào mini game như thường.

### H7. Chiến đấu của quái, lát 2: Chồn Khe và Cáo Gió
Menu thần (F1), mục Kiểm thử, dòng "Combat quái": nút "Chồn Khe (rình, 2 con)" và "Cáo Gió (Chúa Hang)". Cho slime học Lách Né để thử né. Với slime nặng từ khoảng 19 trở lên, Chồn Khe không còn coi slime là mồi (đúng luật kích cỡ).
- [ ] **Chồn Khe rình:** vừa tạo ra thì không thấy hình, chỉ thấy **đôi mắt sáng nhỏ màu vàng** và một vầng sáng mờ. Đi tới trong khoảng 5 m thì nó hiện ra và phóng tới rất nhanh (tín hiệu ngắn, khoảng 0,2 giây).
- [ ] **Chồn Khe vồ xa rồi chạy:** sau cú vồ 8 m nó **bỏ chạy 6 m** trong 1,5 giây (khoảng kiệt sức) rồi quay lại. Hai con thì cú vồ của hai con lệch nhịp. Sau khi không có người chơi quanh đó khoảng 20 giây, nó nằm im ẩn mình lại.
- [ ] **Không vồ được chồn đang ẩn:** bấm chuột trái khi chỉ có chồn ẩn gần: không có mục tiêu, không tốn năng lượng.
- [ ] **Cáo Gió, Vồ Cắn:** cúi thân rồi lao 4 m; né sang bên.
- [ ] **Cáo Gió to đúng cỡ trùm khi thử:** từ giờ quái tạo bằng menu thần nhận cỡ, độ khó và trang phục như trong hang (đặt "Độ khó" trong tab Sinh vật để đổi tầng; tầng càng sâu trùm càng to). Cáo Gió phải to hơn hẳn slime lớn nhất.
- [ ] **Trang phục Cáo Gió:** vương miện vàng 5 mũi với viên ngọc xanh nhạt ở trán, khăn vòng cổ xanh nhạt, **áo choàng gió** xanh nhạt phấp phới sau lưng với ghim vàng ở vai, bùa ngực xanh nhạt phát sáng có vòng vàng xoay quanh. Nếu vị trí lệch (mũ chìm vào đầu, áo choàng treo sai chỗ), vào tab Combat, mục "Trang phục trùm", kéo các thanh cho đúng rồi tạo cáo mới và gửi mình các số bạn thấy hợp.
- [ ] **Giáo Gió** (cách 4 đến 16 m): cáo đưa tay lên, **3 mũi giáo gió **xanh da trời nhạt (kiểu Rasengan)** xoay vòng liên tục phía trên lưng (chỗ đuôi)** khoảng 1,3 giây kèm tiếng vù nhỏ. Rồi cáo phóng lần lượt 3 mũi, cách nhau 0,3 giây, mỗi mũi bay thẳng về **chỗ bạn đứng lúc nó được phóng**, có vệt sáng và tiếng **xé gió**. Trúng thì mất 26 máu mỗi mũi và bị **hất lùi** khoảng 3 m. Chạy sang bên là tránh được; giáo dừng khi chạm vách. Sau khi phóng cáo kiệt sức khoảng 1,6 giây.
- [ ] Cáo bị choáng hoặc bị hạ gục lúc giáo đang xoay: giáo biến mất, không lơ lửng.
- [ ] **Gọi Bầy** (máu dưới 33%): Chồn Khe trong 30 m kéo tới.
- [ ] Chồn ẩn bị choáng hoặc bị hạ gục khi đang ẩn: hình hiện lại, mắt biến mất, không đứng đơ.
- [ ] Không có lỗi đỏ khi chồn ẩn rồi hiện nhiều lần, hoặc khi cục băng rơi lúc slime đã chết hoặc đã chạy xa.

### H8. Chiến đấu của quái, lát 3: Khỉ Hang (loài mới)
Menu thần (F1), mục Kiểm thử, dòng "Combat quái": nút "Khỉ Hang (3)" (con thử hình cầu, đặt tên đúng loài). Khỉ Hang chưa có trong dữ liệu sinh thái, mô hình hay prefab: bên làm hang sẽ thêm (xem `docs/superpowers/handoff/2026-11-20-monkey-species-for-cave-team.md`).
- [ ] **Ném Đá** (cách 5 đến 14 m): khỉ ngả người giơ tay ra sau khoảng 0,9 giây, rồi một viên đá nhỏ nâu bay theo cung tới **chỗ bạn đứng lúc ném**, khoảng 0,9 giây. Đứng yên thì trúng (9 máu), bước sang bên thì tránh được.
- [ ] **Cắn Vặt** (dưới 2,5 m): cúi người rồi phóng 2 m cắn, sau đó **chạy lùi 4 m**.
- [ ] **Nhảy Bục:** khi bạn ở trong 6 m và quanh khỉ có bục hoặc gờ đá cao 1,5 đến 3 m (trong 7 m, phía xa bạn hơn): khỉ cúi đạp chân rồi nhảy lên theo cung. Trên đó khoảng 8 giây nó chỉ ném đá (tầm tới 16 m) và hú, không cắn. Rồi nhảy xuống về chỗ cũ. Không có bục thì nó không nhảy, không tốn hồi chiêu.
- [ ] **Hú Báo Động** (máu dưới 66%): khỉ ngửa đầu hú, các Khỉ Hang trong 30 m kéo tới.
- [ ] Khỉ bị choáng hoặc bị hạ gục khi đang trên bục: không treo lơ lửng, nhảy xuống hoặc biến mất sạch. Viên đá đang bay mà bạn chết hoặc khỉ bị hủy: viên đá tự biến mất, không lỗi đỏ.
- [ ] Vồ vào khỉ đang kiệt sức thì được; vồ lúc khỉ ở trên bục (trong 4 m) cũng thử được.

### Khu thử sạch sẽ (TestArena)
Menu `Evolve → Build Test Arena` dựng scene `Assets/_Project/Scenes/TestArena.unity`: sàn phẳng 60 m có lưới 3 m, tường cao 5 m bao quanh, ánh sáng sáng đều (không sương mù, không tối, không bộ lọc xám của hang), không có quái sẵn (tạo bằng menu thần F1). Mở scene này rồi bấm Play. Các vật thử:
- **Đá nhỏ** cao 0,2, 0,3, 0,45, 0,6, 0,9 m, ở phía đông (x từ 5): slime phải bò qua đá thấp, không qua được đá cao.
- **Tường cao 2 m** phía tây bắc (-8, 6): không leo được.
- **Dốc 12 độ** phía bắc (0, 15).
- **Bục cao 1,5 m** (14, -8) và **2,5 m** (-14, -8): chỗ khỉ nhảy lên.
- **Cầu** cao 3 m phía nam (-10, -20) có khoảng trống bên dưới: thử quái đi dưới cầu không vọt lên.
- **Tường thấp** màu đỏ (12, -20): thử Bọ Giáp Húc đâm vách thì choáng.
- **8 cột** quanh vòng bán kính 20 m: chỗ nấp, chỗ Chồn Khe rình.
- **8 cục thức ăn** quanh slime.
Slime xuất hiện ở giữa (0, 0). Nhớ đóng F1 trước khi bấm chuột trái để Vồ.

### I. Chưa làm được, đừng thử
Co-op với nhiều người; kẻ săn vòng qua vật cản; mesh Blender; âm thanh kỳ ngộ.

## 6. Gửi lại cho Claude
- Ảnh Console (cả cảnh báo vàng nếu lặp nhiều).
- Kỹ năng nào hình xấu, chói, sai chỗ, kèm video ngắn nếu được.
- Danh sách mục không đạt ở mục 5.

## 7. Với mô hình thật của bên làm hang (nhánh `claude/unity-test-maze`)
1. Chạy các menu tạo dữ liệu của bên làm hang (xem `docs/superpowers/handoff/maze-questions.md` và `PROJECT_STATUS.md`) để có prefab trong `Assets/_Project/Prefabs/Wildlife`, rồi `Evolve → Attach Creature Brains`. Console báo "CreatureBrain gắn vào N prefab, PreyDefense vào M, bảng rơi gene vào 12 prefab" (phải chạy `Create Cave Skills` trước, nếu không báo thiếu bảng rơi). Chạy lại sau khi bên làm hang dựng lại prefab.
2. F10: dịch chuyển tới một hang có sinh vật (Hang Khởi Sinh, Vực Nứt Tím…), tạo sinh vật thật từ bảng của họ.
- [ ] Kẻ săn thật (ví dụ Thằn Lằn Bờ Hồ) đói thì đuổi và ăn con mồi thật (Bọ Cánh Cứng Nhỏ) cùng hang; xác để lại.
- [ ] **Bảng rơi:** nuốt xác hoặc con mồi sống của 12 loài đầu (Bọ Cánh Cứng Nhỏ, Ốc Hồ, Thằn Lằn Bờ Hồ, Cóc Hồ Già, Đom Đóm, Thỏ Lá, Bọ Giáp Quặng, Rùa Pha Lê, Dơi Âm Thoa, Bọ Gai Tím, Nhện Tinh Thể, Bọ Cạp Thạch Anh) rồi để Giải Mã: học được **đúng kỹ năng của loài đó** (ví dụ Ốc Hồ cho Thu Mình). Loài khác chưa có bảng rơi.
- [ ] Con mồi thật dùng được phòng vệ (Ốc Hồ rụt vỏ, Thỏ Lá ngoằn ngoèo…). Hoạt hình của họ (chớp mắt, hàm, lưỡi) có chạy cùng không.
- [ ] Kích cỡ: slime mới sinh bị kẻ săn đuổi, slime lớn thì kẻ săn né (khối prefab của họ khác khối thử của mình).
- [ ] Ba kỳ ngộ có sẵn của họ (Tia Nắng Đầu Tiên, Cầu Vồng Trong Thân, Hợp Âm Tinh Thể) **giờ nhận phần thưởng thật**: no, mở màu Cầu Vồng (F3), gene Hiếm trở lên.


### H9. Trùm làm bằng Meshy (chưa ai thử trong Unity)
Quy trình và trạng thái: `docs/MESHY_BOSSES.md`. Nhánh `claude/unity-test-maze`.

**Chuẩn bị (một lần):** pull, đợi Unity biên dịch, Console không đỏ. Đã chạy bước tạo sinh vật hang của bên làm hang (phải có sẵn `Assets/_Project/Prefabs/Wildlife/CaoGio.prefab`, `ChimHangMe.prefab`, `CocHoGia.prefab`, `RanDungNham.prefab`, `BoCapThachAnh.prefab`). Rồi chạy theo thứ tự:
1. `Evolve → Fix Meshy Materials` (nếu mô hình còn trắng/trơn).
2. `Evolve → Build Cáo Gió (Meshy) Prefab`, `Build Chim Hang Mẹ (Meshy) Prefab`, `Build Golem Thạch Anh (Meshy) Prefab`, `Build Meshy Static Prefabs (Cóc, Rắn, Bọ Cạp, Chó Xương)`. Console in `[Meshy] Đã tạo ...` cho từng cái; lỗi đỏ thì chụp gửi.
3. Menu thần F1, tab Sinh vật, mục Chúa Hang: tạo từng trùm. Menu ưu tiên prefab có "(Meshy)" (Console in dòng `[Menu] dùng prefab ...`). Đổi "Độ khó" để thử cỡ.

**Cho mọi trùm:**
- [ ] Có màu đúng (không trắng, không hồng/tím lòe loẹt), đứng trên mặt đất (không lún, không lơ lửng), mặt hướng về phía đi.
- [ ] Cỡ hợp lý so với slime và hang (không quá bé/to); collider không quá lệch so với thân (đứng cạnh slime: nó có đâm xuyên không).
- [ ] Không lỗi đỏ Console khi tạo, khi đi, khi chết, khi bị Vồ.
- [ ] FPS không tụt rõ khi có 1–2 trùm trong tầm nhìn (ảnh màu 2048, mô hình 20 nghìn mặt).

**Cáo Gió (Meshy):** đuôi vẫy mạnh hơn khi đi/ra đòn; thỉnh thoảng chớp mắt; vòng chân/vòng đuôi/ngọc/vòng gió bám đúng chỗ (nếu lệch: chụp ảnh); không còn khăn/áo choàng cũ. Giáo Gió vẫn bắn (mục H8).
**Chim Hang Mẹ (Meshy):** chân bước khi đi; cánh rung nhẹ khi đứng, vỗ khi đi, vỗ mạnh khi ra đòn; mỏ khép lúc nghỉ, há và rung khi đòn hoặc thỉnh thoảng; mỏ không méo mặt. Cánh vỗ ngược hướng thì báo.
**Golem Thạch Anh (Meshy):** đi và chạy bằng clip của Meshy (chuyển từ đứng sang đi khi di chuyển, sang chạy khi nhanh); không trượt chân quá; chưa có chiêu đánh riêng (chỉ cắn).
**Cóc, Rắn, Bọ Cạp, Chó Xương (tĩnh):** chỉ xem hình dáng, màu, cỡ; chúng đi cứng đơ vì chưa có xương. Báo cái nào cần chỉnh (màu, cỡ, dáng) trước khi mình rig.

### H10. Chó Xương (Meshy): bộ chiêu (chưa ai thử trong Unity)
Thiết kế: `docs/superpowers/specs/2026-11-21-cho-xuong-moveset-design.md`. Chạy lại menu `Build Meshy Static Prefabs`, tạo Chó Xương từ menu thần (Chúa Hang), mở tab Combat để xem pha hiện tại.
- [ ] **Chạy đuổi và đi:** clip Walk: chân chéo luân phiên, gối gập, đuôi vẫy; tốc độ bước theo tốc độ thật (không trượt chân quá); đứng yên thì thở nhẹ (clip Idle). Menu phải in `[Meshy] Đã tạo ...`, không báo thiếu clip.
- [ ] **Vồ (clip Pounce):** nén thân rồi lao ~5 m, **bốn chân duỗi thẳng lúc lao (chân trước vươn ra, chân sau thẳng ra sau, không chạy chân)**, đầu vươn lên, hàm há; vồ hụt thì chó **lăn ra nằm nghiêng ngất ~2 s** rồi bật dậy (đánh nó lúc này thấy đau gấp rưỡi).
- [ ] **Vồ trúng (đầu hướng về slime):** chó cắn chặt vào slime, hai chân trước ép lên; slime đứng yên và mất máu liên tục (~6/giây); hiện thanh vàng và chữ "ẤN A RỒI D LIÊN TỤC"; ấn xen kẽ A và D thì thanh đầy, slime thoát, chó bị hất lùi và loạng choạng ~1,2 s (đánh nó lúc này thấy đau gấp rưỡi); ấn một phím liên tục không có tác dụng; không ấn thì sau 8 s chó tự nhả.
- [ ] **Cắn Nghiền** (gần): hàm há rộng rồi đớp khép lại thật nhanh.
- [ ] **Chỉ nhảy khi đủ tầm:** đứng cách chó 3–5,5 m thì nó mới nén thân rồi vồ; đứng xa hơn thì nó tiến lại gần (hoặc phun xương) chứ không nhảy; nó đã nén thân mà bạn chạy ra xa quá ~7,5 m thì **nó hủy, đứng dậy chứ không lao vào khoảng không**; vồ rơi đúng chỗ bạn đứng (không vọt quá).
- [ ] **Phun Xương** (cách 6–16 m): ngửa đầu há miệng rồi hất đầu, mảnh xương trắng xoay bay theo vòng cung kèm vệt trắng, trúng thì mất ~14 máu và vỡ tung; tránh được bằng cách chạy ngang.
- [ ] **Âm thanh:** gầm khi báo trước Vồ, tiếng lao nặng, tiếng đập khi tiếp đất kèm bụi; Cắn có tiếng hàm đớp; Gầm có tiếng gầm dài và vòng sóng; Triệu Hồi có tiếng hú và đất bùng bụi; Phun Xương có tiếng vút. Âm là **tiếng thật (ElevenLabs)**, dứt khoát và nặng; chó trúng đòn thì có tiếng gừ ngắn. Báo lại tiếng nào chưa hợp (ghi tên chiêu) hoặc quá to nhỏ: mình tạo lại từng tiếng bằng `ONLY=hound_roar tools/voice/generate_hound_sfx.sh`.
- [ ] **Nhạc nền:** khi chó bắt đầu tung chiêu hoặc bị đánh, nhạc chiến đấu vang lên (từ đầu bản); vẫn trao đòn thì nhạc giữ nguyên; dừng đánh khoảng 10 giây thì nhạc nhỏ dần trong ~5 giây rồi tắt; đánh lại thì nhạc bật lại; giết chó thì nhạc tắt dần ~1,5 giây. Nhạc không át tiếng hiệu ứng; chỗ nối lặp (sau ~56 giây) không bị khựng.
- [ ] **Cào Xé** (gần, ≤ 3,5 m): ngẩng đầu và giơ một chân trước lên rồi quét xuống; lần sau đổi chân kia.
- [ ] Động tác rõ, **không giật**: báo trước đủ chậm để nhìn ra (nhanh hơn thì báo, ghi chiêu nào).
- [ ] Cỡ Chó Xương bằng khoảng một nửa bản đầu; hồi chiêu dài hơn trước (không ra đòn dồn dập).
- [ ] Chân bước đầu gối gập nhẹ khi vung chân (thấy chân cử động tự nhiên hơn bản trước).
- [ ] Xoay chân trước (khi chạy): xương sườn và lưng **không** bị kéo theo chân.
- [ ] **Nhảy Vượt:** gần một bục thấp (≤ 1,5 m) khi đuổi thì nó nhảy lên; không có bục thì không nhảy.
- [ ] **Gầm** (khi máu dưới 66%): ngửa đầu há miệng, rung, vòng đỏ 8 m dưới đất; con mồi nhỏ trong 8 m bỏ chạy ~4 s; slime nhỏ hơn bị đẩy lùi và đứng đơ ~0,8 s.
- [ ] **Triệu Hồi** (máu dưới 66%): ngửa đầu hú ~1,4 s, vòng nâu dưới đất; **3 con Chó Xương nhỏ (nửa cỡ) chui từ đất lên quanh nó** trong ~1,3 s, rồi lao vào cắn và cào (yếu hơn); sau ~25 s chìm xuống; trùm chết thì chúng chìm theo; không quá 6 con.
- [ ] **Trúng đòn:** đầu giật nhẹ; đòn mạnh (≥ 12% máu) thì loạng choạng ~0,6 s.
- [ ] Mõm không méo khi há (nếu méo: chụp ảnh).
- [ ] Không lỗi đỏ Console; hàm há đúng chiều (xuống), cổ ngửa lên khi gầm (không cúi xuống).

**Cảnh Ra Đời đang tắt mặc định để đỡ tốn thời gian thử** (`BirthScene.SkipScene`): vào game là có sẵn một Lõi và Khuyết Điểm ngẫu nhiên, không có cảnh chọn. Bật lại ở menu thần F1, tab Khác, ô "Bỏ qua cảnh Ra Đời" (bỏ tick), có hiệu lực từ lần vào game sau.

### H11. Golem Thạch Anh (Meshy): bộ chiêu (chưa ai thử trong Unity)
Thiết kế: `docs/superpowers/specs/2026-11-21-golem-moveset-design.md`. Chạy lại `Build Golem Thạch Anh (Meshy) Prefab` (khi đã dừng Play), tạo Golem từ menu thần (Chúa Hang), mở tab Combat để xem pha hiện tại.
- [ ] **Đi và chạy** vẫn dùng clip Meshy; không thu nhỏ, không đứng đơ.
- [ ] **Bước chân nặng:** mỗi bước chân chạm đất có tiếng đạp trầm, bụi dưới chân, màn hình rung nhẹ; khi đứng yên không có tiếng. Chân không còn lết, không kẹt vào vật cản (cỡ giờ giới hạn ~1,2 lần).
- [ ] **Sức nặng:** mọi chiêu báo chậm rõ, ra đòn thì màn hình rung mạnh (Đập Đất, Lõi Nổ rung nhất), slime bị **hất văng** và **đứng đơ một chút** (~0,6–0,8 s), tảng đá rơi xuống đất cũng gây sát thương trong ~2,5 m.
- [ ] **Đập Đất** (trong 5,5 m): giơ hai tay qua đầu và ngả ra sau (chậm, ~1,5 s), rồi bổ cả hai xuống; vòng nâu báo tầm 5,5 m, ba vòng sóng; bụi và sóng đất; né ra ngoài 5 m thì trượt. Có giáp lúc đập nên Vồ bị bật ra; lúc hồi 2 s đánh nó đau gấp rưỡi.
- [ ] **Ném Đá** (cách 5–18 m): tay phải kéo ra sau rồi quăng; tảng đá to bay vòng cung; trúng ~16 máu; vỡ tung có bụi và tiếng đập đá.
- [ ] **Giáp Tinh Thể** (máu dưới 66%): co hai tay che ngực, lõi sáng, **tinh thể vàng mọc ra trên hai vai và lưng**, tia tinh thể; trong 4 s đánh nó thấy mất ít máu hẳn (30%); sau đó hở.
- [ ] **Lõi Nổ** (máu dưới 33%, trong 7 m): dang rộng hai tay, lõi ngực sáng dần rồi chớp, rồi nổ vòng 7 m với tiếng nổ lớn.
- [ ] **Lõi sáng:** đèn ấm trong ngực đập nhẹ lúc nghỉ, chớp khi trúng đòn; hướng đặt đèn đúng ngực (báo nếu lệch).
- [ ] **Nhạc riêng:** nhạc trống nặng, chậm, hợp "núi đá", vang lên khi bắt đầu giao chiến, nhỏ dần sau 10 giây yên, bật lại khi có đòn; không lẫn nhạc của Chó Xương.
- [ ] **Âm thanh:** gằn khi báo trước, tiếng đập đất lớn, tiếng ném, tiếng đá vỡ, tiếng giáp tinh thể, tiếng nạp và nổ lõi, tiếng đau khi trúng đòn. Báo tiếng nào chưa hợp.
- [ ] Tay xoay đúng hướng (giơ lên chứ không xoắn ra sau); không lỗi đỏ Console.

### H12. Bộ kỹ năng chuẩn: Golem và Chó Xương (chưa ai thử trong Unity)
Thiết kế: `docs/superpowers/specs/2026-11-21-boss-standard-kit.md`. Dừng Play, chạy lại `Build Golem Thạch Anh (Meshy) Prefab` và `Build Meshy Static Prefabs`, tạo trùm mới.
- [ ] **Golem, chân bước:** đi thì hai chân **luân phiên bước thật** (đùi vung, gối gập, thân hạ thấp mỗi bước, tay đung đưa ngược chân); dừng thì đứng thẳng; không lết, không trượt chân; mỗi bước có tiếng đạp, bụi, màn hình rung nhẹ. **Cỡ lớn hơn lần trước khoảng 20%.**
- [ ] **Golem, 6 chiêu:** **Nện Đấm** (gần, xoay thân rồi đấm); **Quét Tay** (đánh thường tầm gần ≤ 3,6 m, dang tay rộng rồi quét ngang, thay Đập Đất); **Ném Đá** (xa 5–18 m: cúi xuống nhổ một tảng đá to từ đất lên, nâng bằng hai tay qua đầu rồi gập người ném, tảng đá bay ra từ tay); **Húc Đầu** (combo, 4–14 m: hai tay che đầu, người thấp, lao tới CHẬM rồi NHANH DẦN; đâm vào tường thì choáng ~3 s, loạng choạng, rung đất, đá bắn; trúng người chơi thì hất văng); **Giáng Thạch** (4–16 m: giơ tay chỉ về phía bạn, lấy đà ngồi thấp, rồi nhảy vọt lên trời; vòng đỏ dưới đất không đuổi theo; đáp xuống khụy gối, đứng dậy từ từ ~1,2 s, rồi mới đi tới từng cột đá, dang hai tay ôm rút cột lên từ từ ~1 s, tư thế phi giáo rồi phi; lần lượt 4 cột); **Lõi Nổ** chỉ khi máu ≤ 25%.
- [ ] **Golem, chuyển động chi tiết:** đi nặng (thân đảo nhẹ theo từng bước); chạy tới gai thì sải chân rộng, gối gập sâu, người chúi về trước; Nện Đấm: xoay thân ra sau rồi vặn xuyên qua kèm bước chân trước; Ném Đá: chân trước chống, xoay người như người ném; Đập Đất: hạ thấp người theo cú đập; Giáng Thạch: ngồi thấp lấy đà, bật lên, co chân trên trời, duỗi chân xuống rồi khuỵu gối đỡ cú đáp; trúng đòn thì giật người ra sau rồi ổn định lại. Ghi lại động tác nào còn cứng hoặc xuyên hình.
- [ ] **Lõi sáng:** đèn ấm trong ngực đập nhẹ lúc nghỉ, chớp khi trúng đòn; hướng đặt đèn đúng ngực (báo nếu lệch).
- [ ] **Nhạc riêng:** nhạc trống nặng, chậm, hợp "núi đá", vang lên khi bắt đầu giao chiến, nhỏ dần sau 10 giây yên, bật lại khi có đòn; không lẫn nhạc của Chó Xương.
- [ ] **Âm thanh:** gằn khi báo trước, tiếng đập đất lớn, tiếng ném, tiếng đá vỡ, tiếng giáp tinh thể, tiếng nạp và nổ lõi, tiếng đau khi trúng đòn. Báo tiếng nào chưa hợp.
- [ ] Tay xoay đúng hướng (giơ lên chứ không xoắn ra sau); không lỗi đỏ Console.

### H12. Bộ kỹ năng chuẩn: Golem và Chó Xương (chưa ai thử trong Unity)
Thiết kế: `docs/superpowers/specs/2026-11-21-boss-standard-kit.md`. Dừng Play, chạy lại `Build Golem Thạch Anh (Meshy) Prefab` và `Build Meshy Static Prefabs`, tạo trùm mới.
- [ ] **Golem, chân bước:** đi thì hai chân **luân phiên bước thật** (đùi vung, gối gập, thân hạ thấp mỗi bước, tay đung đưa ngược chân); dừng thì đứng thẳng; không lết, không trượt chân; mỗi bước có tiếng đạp, bụi, màn hình rung nhẹ. **Cỡ lớn hơn lần trước khoảng 20%.**
- [ ] **Golem, 5 chiêu:** Nện Đấm (gần, đấm một tay), Đập Đất (vùng 5,5 m), Ném Đá (xa 5–18 m), **Chộp Nghiền** (cách 3–7 m: chộp và bóp, hiện thanh và dòng "BỊ BÓP! ẤN SPACE LIÊN TỤC", thoát được thì Golem loạng choạng, không thoát sau ~8 s thì bị đập xuống ~22 sát thương và văng), **Lõi Nổ** chỉ khi máu ≤ 25%.
- [ ] **Golem, nội tại:** trúng đòn mất ít máu hơn bình thường (−20%); máu ≤ 66% thì **tinh thể mọc ra trên vai và lưng** và còn nhận ít hơn nữa; để yên ≥ 8 giây thì máu hồi dần.
- [ ] **Chó Xương, 5 chiêu:** Cắn Nghiền và Cào Xé (thường), Phun Xương (xa), **Vồ** (đặc biệt: trúng thì cắn chặt, ấn xen kẽ A/D), **Triệu Hồi** chỉ khi máu ≤ 25%. Không còn Gầm và Nhảy Vượt.
- [ ] **Chó Xương, nội tại Khát Máu:** máu ≤ 50% thì chạy nhanh hơn rõ.
