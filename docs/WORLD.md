# PROJECT EVOLVE — Thế giới (World) v0.1

> Ngày 2026-11-17. Trạng thái: **đang chốt từng điểm** cùng chủ dự án. Ký hiệu: **[ĐÃ CHỐT]** · **[ĐỀ XUẤT]** · **[MỞ]**.
> Bổ sung cho [GAME_DESIGN.md](GAME_DESIGN.md) và [BESTIARY.md](BESTIARY.md) (mục 6 bestiary theo vùng, mục 7 sinh thái là bản nháp chờ tài liệu này).

## 1. Nơi sinh ra và hồi sinh

**[ĐÃ CHỐT]**
- Lần sinh ra đầu tiên, mỗi người chơi xuất hiện ở **một hồ nước trong hang động**, vị trí **ngẫu nhiên** trên map, khác nhau giữa các ván.
- Sau đó người chơi có thể **lưu điểm hồi sinh** tại một **địa điểm đặc biệt**. Có điểm đã lưu thì chết sẽ hồi sinh ở đó, **không còn ngẫu nhiên**.
- Chưa lưu điểm nào mà chết thì **quay lại hồ hang lúc mới sinh** (hồ này là điểm hồi sinh mặc định cho đến khi lưu điểm mới).
- Cả bầy khi **bắt đầu vào map** sinh ra **gần nhau** (cùng khu hồ hang). Hồ khởi đầu của mỗi người chính là điểm hồi sinh mặc định của người đó.
- **Làm map theo hai giai đoạn:** (1) **map cố định** để chơi thử cơ chế, có sẵn nhiều hồ hang và bốc ngẫu nhiên hồ khởi đầu; (2) về sau mới làm **map ngẫu nhiên** hoàn toàn mỗi ván. Dữ liệu vùng/hồ nên tách khỏi cảnh để chuyển sang bước 2 không phải viết lại.
- **Địa điểm lưu điểm hồi sinh là tổ, mỗi người một tổ riêng** (xây tổ đã chốt ở `GAME_DESIGN.md` mục 6). Chết thì hồi sinh ở tổ của chính mình.
- **Điều kiện dựng tổ:** chỉ dựng được khi **không đang bị tấn công** và **không ở gần khu Boss**. **Nguyên liệu xây tổ lấy từ quái** (rơi khi đánh quái).

**[MỞ]**
- Gần nhau là cùng một hồ hay các hồ liền kề; người vào giữa ván thì sinh ở đâu?
- Map ngẫu nhiên hoàn toàn: sinh ra những gì, đảm bảo luôn có đủ bốn vùng, hồ hang, nguồn cơ quan "vé vào vùng" và đường đi hợp lý (làm sau).
- Nguyên liệu xây tổ: loại nào, từ quái bậc nào, số lượng bao nhiêu. "Bị tấn công" tính trong bao lâu gần nhất; "gần khu Boss" là bán kính bao nhiêu.
- Vì mỗi người một tổ nên bầy có thể bị tách khi hồi sinh: có cho dựng tổ sát cạnh tổ đồng đội, hay giới hạn khoảng cách giữa các tổ của cùng bầy? Tổ bị phá thì về hồ khởi đầu của mình (đề xuất, chờ xác nhận).

### 1.1 Đối đầu Thần Chết

**[ĐÃ CHỐT]**
- Khi chết, người chơi được **chọn đánh một trận với Thần Chết**, một **boss cố định** (luôn là cùng một boss).
- **Thắng** thì sống tiếp tại chỗ với **1 máu**, **không mất mạng** và **giữ đầy đủ cơ quan** (không rơi, không mất 50%).
- **Thua** thì chết thường: **mất 1 mạng** và cơ quan rơi theo luật 50/50 như `GAME_DESIGN.md` mục 6.1.
- Không chọn đánh thì chết thường, giống thua.
- **[ĐÃ CHỐT]** Mỗi mạng chỉ được cược Thần Chết **một lần**.
- **[ĐÃ CHỐT]** Độ khó Thần Chết **cố định**, không nhân theo số người chơi. Trận cược là 1 người đấu 1 Thần Chết, khác với quái A/S/Boss nhân HP theo số người.
- **[ĐÃ CHỐT]** Thần Chết thuộc **bậc Boss** của Bestiary (giai đoạn theo cơ quan, vùng phá có giới hạn mẫu). Khác Boss thường: không nằm trong vùng nào, trận 1 người, và **không rơi cơ quan S**.

**[MỞ]**
- Thắng thì vẫn giữ mạng đó, nên bộ đếm "đã cược" gắn với mạng hiện tại và chỉ reset khi mất mạng (thua hoặc chết thật). Xác nhận cách hiểu này.
- Độ khó cố định ở mức nào, có chỉnh theo cấp/cơ quan của người cược không?

## 2. Hệ quả cho các tài liệu khác

- `GAME_DESIGN.md` mục 6.1 đang ghi "hồi sinh chủ động tại **điểm xuất phát**"; cần đổi thành luật ở mục 1 này.
- `BESTIARY.md` mục 6.1 gọi **đồng bằng** là vùng khởi đầu. Nếu mọi người sinh ra trong hang thì Dơi Hang, Nấm Đi, Giun Phát Quang là quái đầu tiên gặp, và cơ quan "vé vào vùng" của hang (Râu cảm rung, Lọc khí độc) phải dễ có ngay từ đầu. Thứ tự vùng cần viết lại. **[MỞ]**
- Slime mù chỉ có cảm rung hợp với hang (mắt gần như vô dụng), nên giai đoạn mù vẫn chơi được.
- `SurvivalState`/`SlimeController` hiện hồi sinh bằng phím R ở điểm cố định; sẽ cần điểm hồi sinh có thể thay đổi. Chưa làm.

## 3. Còn hoãn từ Bestiary

- Quái thích nghi theo loại sát thương người chơi dùng: bật từ đầu hay để sau?
- Đặt tên loài và cơ quan: giữ kiểu mô tả sinh học hay đặt tên riêng?
