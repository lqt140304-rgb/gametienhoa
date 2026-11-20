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
- [ ] "Gắn tối đa bị động hang": Da Dày +10 máu tối đa; Vỏ Cứng bị cắn mất ít hơn; Sóng Dò Mạnh (chưa có tác dụng: đang chờ bên làm hang).
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

### I. Chưa làm được, đừng thử
Co-op với nhiều người; Sóng Dò Mạnh kéo dài tầm quét; mô hình giàu của B1 chưa nối vào vùng hồ (`Region.cs` vẫn mô hình cũ); kẻ săn vòng qua vật cản; mesh Blender; âm thanh kỳ ngộ.

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
