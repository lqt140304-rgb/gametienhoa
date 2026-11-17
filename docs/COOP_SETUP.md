# Co-op 0.4a: cài đặt và thử

Bản này là **nền móng**: hai (hoặc nhiều) người vào cùng một phòng và **thấy nhau** (vị trí, cơ quan, máu, mạng). Mỗi người vẫn chơi trong thế giới của riêng mình; quái, thức ăn và cơ quan rơi **chưa dùng chung** (làm ở 0.4b).

Mô hình mạng đã chọn: **một người làm chủ phòng (Host), người khác nhập IP** (Netcode for GameObjects, không cần Relay hay máy chủ riêng).

## 1. Cài gói Netcode (làm một lần)

Mã co-op nằm trong assembly riêng `Evolve.Net`, **chỉ được biên dịch khi gói đã cài**. Chưa cài thì game chạy bình thường, bấm F2 sẽ có thông báo hướng dẫn.

1. Trong Unity: **Window → Package Manager**.
2. Chọn **Unity Registry** ở cột bên trái, gõ `Netcode for GameObjects` vào ô tìm kiếm.
3. Bấm **Install**. Unity 6.6 dùng nhánh **2.x** của gói (bản 3.0 cần Unity 6.7). Gói **Unity Transport** và **Collections** sẽ tự được cài kèm.
   - Nếu không thấy gói: **Edit → Project Settings → Package Manager**, bật **Enable Pre-release Packages**, rồi tìm lại.
4. Đợi Unity biên dịch xong và xem tab **Console**. Nếu có **lỗi đỏ**, chụp màn hình gửi lại (mã mạng được viết mà chưa thể biên dịch thử, nên lỗi lần đầu là có thể xảy ra).
5. Commit hai file Unity vừa sửa: `Packages/manifest.json` và `Packages/packages-lock.json`.

## 2. Thử trên một máy (cách nhanh nhất)

Cần hai bản game chạy cùng lúc: **Unity Editor** và **một bản build**.

1. **Tạo bản build:** File → Build Profiles (hoặc Build Settings), kiểm tra scene `Prototype_01` có trong danh sách (công cụ dựng scene tự thêm), chọn Windows, bấm **Build** vào thư mục `D:\gametienhoa\Build` (thư mục này đã được Git bỏ qua). Lần đầu Windows hỏi tường lửa: bấm **Allow access**.
2. **Bản build:** chạy file `.exe`, bấm **F2**, đặt tên, bấm **Mở phòng (Host)**.
3. **Unity Editor:** bấm Play, **F2**, để địa chỉ `127.0.0.1`, bấm **Vào phòng (Join)**.
4. Hai bên sẽ thấy nhau: một slime màu khác, tên và thanh máu phía trên. Thử lắp cơ quan ở bên này, bên kia sẽ thấy cơ quan mọc ra.

Có thể đảo vai (Editor làm Host, bản build Join).

## 3. Chơi hai máy

- **Cùng mạng LAN:** máy Host bấm F2, menu hiện sẵn địa chỉ IP của máy đó (dạng `192.168.x.x`). Máy kia nhập địa chỉ này rồi Join. Cổng mặc định `7777`.
- **Qua internet:** cách đơn giản nhất là cả hai cùng dùng một phần mềm mạng riêng ảo như Tailscale hoặc Radmin VPN, rồi nhập IP ảo của máy Host. Chơi thẳng qua internet cần mở cổng UDP 7777 trên router của máy Host.

## 4. Những gì đã đồng bộ

| Có | Chưa |
|---|---|
| Vị trí và hướng nhìn, mượt hơn nhờ nội suy | Quái (con mồi, kẻ săn mồi), thức ăn, cơ quan rơi |
| Cơ quan đang mang và cấp của chúng | Gây sát thương cho nhau hay cho quái của người khác |
| Máu, số mạng, đã chết hay chưa | Cứu đồng đội, chuyền máu, chế độ quan sát |
| Tên người chơi | Lưu phòng, quản lý nhiều phòng |

Người chơi khác được vẽ như một "bóng": không có va chạm, quái trong thế giới của bạn **không** thấy họ. Khi chưa có mắt, thế giới tối nên bạn chỉ thấy họ khi lại gần, nhưng **tên và thanh máu luôn hiện** trên màn hình (tạm thời, để dễ thử).

## 5. Cách hoạt động (cho người làm tiếp)

- Không dùng `NetworkObject` hay prefab. Mỗi người gửi **trạng thái** (`PlayerNetState`) khoảng 10 lần mỗi giây bằng tin nhắn có tên của Netcode (`CustomMessagingManager`): client gửi `evolve.up` cho Host; Host hiển thị rồi chuyển tiếp `evolve.dn` (kèm id người gửi) cho các client khác; Host cũng gửi trạng thái của chính mình.
- Người chơi khác là `RemoteAvatar`: một slime tạo bằng code, dùng lại `OrganHost` để mọc cơ quan thật (`OrganHost.MirrorOrgan`), không có `SurvivalStats`.
- `CoopManager` tự thêm khi Play (như Menu God) và tự tạo `NetworkManager` lúc chạy, nên **không phải sửa scene**. `NetworkConfig.EnableSceneManagement = false`.
- Cờ `SurvivalHUD.ExternalMenuOpen` làm menu co-op hành xử như menu HUD: con trỏ hiện ra, kỹ năng và xoay camera bị chặn (nhưng nhân vật vẫn đi được khi gõ chữ).
- Bước tiếp theo (0.4b): đưa quái, thức ăn và cơ quan rơi về Host điều khiển bằng `NetworkObject`, kèm quản lý sát thương.

## 6. Khi gặp sự cố

| Hiện tượng | Cách xử lý |
|---|---|
| Bấm F2 chỉ hiện thông báo "chưa cài gói" | Cài gói ở bước 1 |
| Lỗi đỏ ở Console sau khi cài gói | Chụp Console gửi lại; có thể là khác biệt tên hàm giữa các phiên bản gói |
| "Không kết nối được" sau 10 giây | Kiểm tra địa chỉ, cổng, tường lửa của máy Host; thử `127.0.0.1` trên cùng một máy |
| "Không mở được phòng (cổng đang bận?)" | Đã có một bản khác đang Host cổng đó; đổi cổng hoặc tắt bản kia |
| Bản không bấm vào có vẻ đứng yên | Game tự chạy nền khi vào phòng; nếu vẫn đứng, bấm vào cửa sổ đó |
