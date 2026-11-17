# PROJECT EVOLVE — Thế giới (World) v0.1

> Ngày 2026-11-17. Trạng thái: **đang chốt từng điểm** cùng chủ dự án. Ký hiệu: **[ĐÃ CHỐT]** · **[ĐỀ XUẤT]** · **[MỞ]**.
> Bổ sung cho [GAME_DESIGN.md](GAME_DESIGN.md) và [BESTIARY.md](BESTIARY.md) (mục 6 bestiary theo vùng, mục 7 sinh thái là bản nháp chờ tài liệu này).

## 1. Nơi sinh ra và hồi sinh

**[ĐÃ CHỐT]**
- Lần sinh ra đầu tiên, mỗi người chơi xuất hiện ở **một hồ nước trong hang động**, vị trí **ngẫu nhiên** trên map, khác nhau giữa các ván.
- Sau đó người chơi có thể **lưu điểm hồi sinh** tại một **địa điểm đặc biệt**. Có điểm đã lưu thì chết sẽ hồi sinh ở đó, **không còn ngẫu nhiên**.
- Chưa lưu điểm nào mà chết thì **quay lại hồ hang lúc mới sinh** (hồ này là điểm hồi sinh mặc định cho đến khi lưu điểm mới).

- Cả bầy khi **bắt đầu vào map** sinh ra **gần nhau** (cùng khu hồ hang). Hồ khởi đầu của mỗi người chính là điểm hồi sinh mặc định của người đó.

**[ĐỀ XUẤT]**
- Địa điểm đặc biệt để lưu điểm hồi sinh chính là **tổ** của bầy (đã chốt có xây tổ ở `GAME_DESIGN.md` mục 6), hoặc một công trình nhỏ đặt ở các vùng. Cả bầy lưu cùng một nơi thì hồi sinh gần nhau, không tách bầy.

**[MỞ]**
- Gần nhau là cùng một hồ hay các hồ liền kề; người vào giữa ván thì sinh ở đâu?
- **Cơ chế "cược khi chết":** máu về 0 thì chọn chết như thường, hoặc đánh một trận với quái mạnh hơn để sống tiếp với 1 máu (thắng) hoặc chết (thua). Đang hỏi: thắng thì giữ được mạng và cơ quan không, có giới hạn số lần cược không. Đề xuất: thắng thì không mất mạng, không rơi cơ quan; thua thì chết như luật thường; mỗi mạng chỉ cược một lần.
- Map có tạo ngẫu nhiên mỗi ván không, hay là map cố định có nhiều hồ hang?
- Địa điểm lưu điểm cụ thể là gì, tốn gì để dựng?

## 2. Hệ quả cho các tài liệu khác

- `GAME_DESIGN.md` mục 6.1 đang ghi "hồi sinh chủ động tại **điểm xuất phát**"; cần đổi thành luật ở mục 1 này.
- `BESTIARY.md` mục 6.1 gọi **đồng bằng** là vùng khởi đầu. Nếu mọi người sinh ra trong hang thì Dơi Hang, Nấm Đi, Giun Phát Quang là quái đầu tiên gặp, và cơ quan "vé vào vùng" của hang (Râu cảm rung, Lọc khí độc) phải dễ có ngay từ đầu. Thứ tự vùng cần viết lại. **[MỞ]**
- Slime mù chỉ có cảm rung hợp với hang (mắt gần như vô dụng), nên giai đoạn mù vẫn chơi được.
- `SurvivalState`/`SlimeController` hiện hồi sinh bằng phím R ở điểm cố định; sẽ cần điểm hồi sinh có thể thay đổi. Chưa làm.

## 3. Còn hoãn từ Bestiary

- Quái thích nghi theo loại sát thương người chơi dùng: bật từ đầu hay để sau?
- Đặt tên loài và cơ quan: giữ kiểu mô tả sinh học hay đặt tên riêng?
