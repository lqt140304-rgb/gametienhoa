# Thiết kế + kế hoạch: Combat của slime, Vồ – Bám – Tiêu Hóa (đã duyệt 2026-11-20)

Dành cho phiên làm tiếp (phiên thiết kế hết lượt). Bối cảnh chung, cách kiểm khi không có Unity, quy ước tiếng Việt: xem phần đầu `docs/superpowers/handoff/2026-11-20-core-systems-brief.md`. Một phiên khác đang làm **combat của quái** (quái có các giai đoạn tung chiêu và kỹ năng khác nhau): mục 6 là phần hai bên phải khớp.

## 1. Vì sao
Combat của slime đang quá đơn: không có đòn đánh cơ bản (chỉ có húc khi lao ≥ 4 m/s, da gai, kỹ năng phím 1–5 / Q), phòng thủ chỉ nằm trong kỹ năng, cảm giác trúng đòn mỏng (chỉ `HitFlash`). Khi quái có chiêu theo giai đoạn, người chơi cần một cách đánh **mang bản sắc slime**, đòi hỏi kỹ năng, và **cho thấy đầu game khổ** rồi mạnh dần.

## 2. Cơ chế chính (chủ dự án chọn)
1. **Vồ** là hành động cơ bản, có từ đầu: bấm một phím, slime bật tới quái gần trước mặt (khoảng 4 m) và bám lên thân nó.
   - Con **nhỏ hơn** slime (luật `MassRules.CanSwallowWhole` đang có): nuốt sống ngay như hiện tại.
   - Con **to hơn**: bám lên và vào **mini game Ghì Chặt**.
   - Quá to (đề xuất: khối quái > 4 lần khối slime): vồ bật ra, không bám được.
2. **Mini game Ghì Chặt** (kiểu câu cá Stardew Valley):
   - Một thanh ngang có **vùng xanh**; quái giãy làm **chấm** của người chơi trôi sang hai bên; người chơi kéo chuột hoặc A/D ngược lại để giữ chấm trong vùng.
   - Chấm trong vùng xanh: **gây sát thương liên tục** (tiêu hóa) và hút lại một ít dinh dưỡng.
   - Chấm trượt ra ngoài (quá một khoảng ân hạn ngắn, đề xuất 0,25 giây): **một lỗi**. Đủ số lỗi của loài (**3–5**) thì quái **giãy thoát**, slime bị hất xuống.
   - **Quái càng khỏe, mini game càng khó**: vùng hẹp hơn, giật mạnh và nhanh hơn, khó đoán hơn, số lỗi tối đa ít hơn.
   - Bấm nhảy để buông ra bất cứ lúc nào. Khi đang bám, quái khác vẫn đánh được slime.
   - Thân slime và quái **lắc theo hướng giãy** để nhìn thấy quái vùng vẫy.
3. **Cải tiến đã duyệt (chương 1):**
   - **Điểm yếu:** một khúc vàng nhỏ trôi trong vùng xanh; chấm ở đó thì tiêu hóa nhanh gấp đôi.
   - **Cú giãy mạnh có báo trước:** quái gồng mình (đỏ, rung, khoảng 0,5 giây); bấm **Space đúng lúc** thì **siết chặt**: quái khựng, vùng xanh rộng ra vài giây. Trật hoặc không bấm: một lỗi.
   - **Giai đoạn tuyệt vọng:** máu quái dưới 30% thì giãy điên cuồng hơn. Giữ tới khi quái chết thì **nuốt trọn tại chỗ** (xem 2.4), xác tươi nhất, Giải Mã ra gene chất lượng cao nhất.
   - **Năng lượng và kích cỡ:** ghì tốn năng lượng; hết năng lượng thì vùng xanh hẹp dần. Slime càng to so với quái thì vùng càng rộng.
   - **Hậu quả khi thoát:** quái bị thương bỏ chạy và **nhớ** slime: lần sau cảnh giác hơn, khó vồ hơn.
4. **Cảnh nuốt chửng** (ý chủ dự án): khoảnh khắc kết liễu là điểm nhấn.
   - Khi quái chết trong lúc đang bám (hoặc khi nuốt sống một con đủ lớn), phát một **cảnh ngắn 1,5–2,5 giây**: thời gian chậm lại, camera áp sát và xoay nhẹ quanh slime, thân slime phình ra trùm kín quái, một cú **"ực"** (tiếng nuốt, rung màn hình nhẹ, chớp sáng), quái mờ dần bên trong lớp gel, slime co lại và nảy lên một cái.
   - Bỏ qua được bằng một phím. Không phát cho con mồi nhỏ (chỉ con to hoặc khi kết liễu từ Ghì Chặt), để không lặp đến nhàm. Co-op: chỉ phát trên máy người nuốt; người khác thấy hình nuốt bình thường.
5. **Làm sau khi khung chạy ổn:**
   - **Kỹ năng hang tác động lên mini game:** Bám Vách (vùng rộng hơn), Nọc Tê (quái giãy chậm hơn), Tiết Axit (tiêu hóa nhanh hơn), Vỏ Cứng (thêm một lỗi được phép), Phun Độc khi đang bám (quái mất máu thêm).
   - **Co-op:** đồng đội đánh con quái đang bị bám thì quái phân tâm, vùng xanh rộng ra; hai slime cùng bám thì mỗi người một chấm.
6. **Đổi tên:** kỹ năng "Vồ" của Chồn Khe đổi thành **Vồ Xa** (vồ từ 8 m) để không trùng hành động cơ bản.

## 3. Kỹ năng có điều kiện (chủ dự án đồng ý ý tưởng, chưa chốt cách mở)
Các cơ chế cơ thể khác trở thành kỹ năng cần điều kiện: **Đạn Nước** (nhả đồ trong bụng ra làm đạn), **Tan Chảy** (tan thành vũng để né). Cách mở khóa **chưa chốt**; đề xuất của phiên thiết kế: **học bằng cách làm** (ví dụ nhả đồ 10 lần thì học Đạn Nước; sống sót 5 đòn khi máu thấp thì học Tan Chảy), sau đó **dùng theo hoàn cảnh** (Đạn Nước cần có đồ trong bụng). **Hỏi chủ dự án trước khi làm phần này.**

## 4. Số tạm (chỉnh khi chơi)
| Thông số | Quái yếu | Quái thường | Chúa Hang |
|---|---|---|---|
| Độ rộng vùng xanh (phần thanh) | 40% | 28% | 18% |
| Số lỗi tối đa | 5 | 4 | 3 |
| Tần số giãy | chậm, đều | vừa, giật cục | nhanh, hai pha |
| Cú giãy mạnh | không | thỉnh thoảng | thường xuyên |
| Sát thương tiêu hóa | 8/giây | 8/giây | 8/giây (máu cao nên lâu hơn) |
Hệ số theo kích cỡ: vùng × clamp(khối slime / khối quái, 0,6; 1,5). Điểm yếu ×2. Năng lượng ghì: 4/giây.

## 5. Kế hoạch làm (theo thứ tự)
**Thư mục:** `Scripts/Abilities/` hoặc thư mục mới `Scripts/Combat/` (+ `Combat/Pure/`), móc nhỏ ở `Player/`, `Swallow/Swallower`, `Creatures/Predator`/`Prey`; test thuần trong `Tests/EditMode/Combat/Pure/` (thêm vào `tools/cstest/cstest.csproj`).
1. **Luật thuần `LatchGame`** (không Unity, có test): trạng thái chấm, vùng xanh, điểm yếu, cú giãy mạnh (cửa sổ bấm), đếm lỗi với ân hạn, giai đoạn tuyệt vọng, tiêu hao năng lượng, hệ số kích cỡ, kết quả (đang bám / thoát / quái chết). Đầu vào: một `StruggleProfile` của loài (độ rộng, số lỗi, nhịp giãy, xác suất cú mạnh) + hạt giống để test lặp lại được.
2. **Hành động Vồ**: phím (đề xuất chuột trái, hoặc E nếu chuột trái đang bận), chọn mục tiêu gần trước mặt theo hướng ngắm (`AimController`), quyết định nuốt sống / bám / bật ra theo khối.
3. **Bám trong cảnh**: gắn slime lên quái (theo xương hoặc tâm thân), khóa di chuyển của slime, quái vẫn chạy theo hành vi của nó (chiêu hất văng do phiên làm quái), lắc theo hướng giãy, rơi xuống khi thoát/buông.
4. **Giao diện mini game** (IMGUI như HUD hiện có, hoặc UGUI): thanh, vùng xanh, khúc vàng, chấm, báo cú giãy mạnh, số lỗi còn lại; tiếng hiệu ứng (ElevenLabs, nhẹ và sạch: tiếng tiêu hóa xèo khẽ, cú siết, cú giãy thoát).
5. **Cảnh nuốt chửng** (mục 2.4): chậm thời gian, camera áp sát, thân phình trùm quái, tiếng "ực", rung nhẹ, bỏ qua được.
6. **Kết quả**: quái chết thì vào Túi với chất lượng tươi nhất (`AnalysisRules.Fresh`); thoát thì quái bị thương bỏ chạy và có cờ "nhớ slime".
7. **Đổi tên** Vồ của Chồn Khe thành Vồ Xa (dữ liệu kỹ năng hang).
8. Sau đó: kỹ năng hang tác động mini game; co-op (mục 2.5).

Kiểm: luật thuần có test; phần Unity chủ dự án thử theo checklist ghi vào `docs/PROJECT_STATUS.md` (mục mới), có lối tắt trong menu thần (F1) để tạo một quái thử từng mức khó.

## 6. Giao diện với phiên làm combat của quái
- Mỗi loài khai báo **`StruggleProfile`**: độ rộng vùng, số lỗi tối đa (3–5), kiểu giãy (đều / giật cục / hai pha), tần số và độ mạnh, xác suất và báo trước của cú giãy mạnh. Đặt cạnh dữ liệu chiêu của quái.
- Quái biết mình **đang bị bám** (cờ hoặc sự kiện) để chọn chiêu riêng: giãy, lăn, lao vào vách để hất slime. Một chiêu hất văng trúng thì tính như một lỗi (hoặc thoát ngay nếu là chiêu mạnh của Chúa Hang).
- Khi quái đang trong chiêu có **giáp/không ngắt được**, vồ bật ra (không bám được); khi quái **hở sau chiêu** thì vồ dễ trúng hơn: đây là cách người chơi trừng phạt lúc quái hở.
- Quái thoát: gọi lại cho bên quái để chuyển sang trạng thái bỏ chạy và nhớ slime.
- Hai phiên thống nhất tên và chỗ đặt các trường này trước khi code; ghi vào `docs/superpowers/handoff/` nếu cần.

## Cập nhật thực hiện (2026-11-20)
Đã làm các bước 1 đến 6 của mục 5 (xem `docs/PROJECT_STATUS.md` mục 54 và kế hoạch `docs/superpowers/plans/2026-11-20-slime-combat-latch.md`). **Chưa làm:** bước 7 (đổi tên Vồ của Chồn Khe thành Vồ Xa), vì Chồn Khe thuộc đợt 3 và chưa có dữ liệu kỹ năng; bước 8 (kỹ năng hang tác động mini game, co-op). Cảnh nuốt chỉ gồm chậm thời gian và camera áp sát. Mọi số lấy từ mục 4 và 2.3. Né (dash) là kỹ năng học từ quái, không có sẵn.

