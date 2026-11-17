# Co-op 0.4: cài đặt và thử

- **0.4a (đã chạy được):** vào cùng một phòng và **thấy nhau** (vị trí, cơ quan, máu, mạng).
- **0.4b đợt 1 (bản này, chưa chạy thử):** **thế giới chung**. Chủ phòng (Host) điều khiển con mồi, kẻ săn mồi, thức ăn và cơ quan rơi; người vào phòng thấy đúng thế giới đó, cùng bị cắn, cùng bắn trúng, cùng ăn và nhặt đồ. Cứu đồng đội, chuyền máu và chế độ quan sát để đợt 2.

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

0. **Cả hai bản phải cùng phiên bản code và cùng scene.** Sau khi lấy code mới, **tạo lại bản build** và dùng cùng scene `Prototype_01` (nếu khác nhau, thức ăn sẽ không đồng bộ và Console báo cảnh báo).
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
| Vị trí và hướng nhìn của người chơi, cơ quan và cấp, máu, mạng, tên | Cứu đồng đội, chuyền máu, chế độ quan sát |
| **Con mồi và kẻ săn mồi** (Host điều khiển, người vào phòng thấy bản sao mượt) | Thanh máu của kẻ săn mồi hiển thị cho người vào phòng |
| Kẻ săn mồi **đuổi và cắn cả hai người**; cắn người vào phòng thì họ mất máu và bị hất văng | Hồi sinh và mất mạng đồng bộ cho đồng đội |
| Người vào phòng **bắn và húc** kẻ săn mồi: sát thương gửi về Host | Quái nhìn thấy người vào phòng bằng va chạm (hiện dùng khoảng cách) |
| Người vào phòng **bắt con mồi, hạ kẻ săn mồi**: nhận thức ăn, cơ quan rơi xuất hiện cho cả hai | Lưu phòng, quản lý nhiều phòng |
| **Thức ăn** dùng chung (cục nào bị ăn thì cả hai thấy co lại) | |
| **Cơ quan rơi** dùng chung; nhặt thì biến mất ở bên kia; người vào phòng chết thì cơ quan rơi cũng xuất hiện ở cả hai | |

Người chơi khác được vẽ như một "bóng": không có va chạm với bạn. Khi chưa có mắt, thế giới tối nên bạn chỉ thấy họ khi lại gần, nhưng **tên và thanh máu luôn hiện** trên màn hình (tạm thời, để dễ thử).

Khi **vào phòng**, thế giới riêng của bạn (quái, thức ăn, cơ quan rơi) được **ẩn đi** và thay bằng thế giới của Host; khi **thoát phòng** thì thế giới riêng hiện lại như cũ. Cơ quan, EXP và chỉ số của chính bạn luôn là của bạn và được giữ nguyên.

## 5. Cách hoạt động (cho người làm tiếp)

- Không dùng `NetworkObject` hay prefab. Mỗi người gửi **trạng thái** (`PlayerNetState`) khoảng 10 lần mỗi giây bằng tin nhắn có tên của Netcode (`CustomMessagingManager`): client gửi `evolve.up` cho Host; Host hiển thị rồi chuyển tiếp `evolve.dn` (kèm id người gửi) cho các client khác; Host cũng gửi trạng thái của chính mình.
- Người chơi khác là `RemoteAvatar`: một slime tạo bằng code, dùng lại `OrganHost` để mọc cơ quan thật (`OrganHost.MirrorOrgan`), không có `SurvivalStats`.
- `CoopManager` tự thêm khi Play (như Menu God) và tự tạo `NetworkManager` lúc chạy, nên **không phải sửa scene**. `NetworkConfig.EnableSceneManagement = false`.
- Cờ `SurvivalHUD.ExternalMenuOpen` làm menu co-op hành xử như menu HUD: con trỏ hiện ra, kỹ năng và xoay camera bị chặn (nhưng nhân vật vẫn đi được khi gõ chữ).
- **Thế giới chung (0.4b đợt 1)** vẫn không dùng `NetworkObject`, dùng thêm các tin nhắn có tên (`WorldSync`): Host → client gửi vị trí quái (10 lần/giây), thức ăn còn lại và cơ quan rơi (2 lần/giây), cú cắn và thức ăn nhận được; client → Host gửi sát thương lên kẻ săn mồi, cơ quan đã nhặt, và yêu cầu tạo cơ quan rơi (khi người chơi chết).
- Phía client, `Prey`/`Predator` là **bản sao** (`IsReplica`): không chạy AI, chỉ đi theo vị trí Host báo (`ReplicaMotion`), và gửi sát thương về Host. Người chơi từ xa là `SurvivalStats` có `IsRemote` nên quái trên Host đuổi, cắn và bị bắt **theo khoảng cách** (`NetWorld.BiteRemote`, `NetWorld.FeedRemote`).
- Cú nhặt cơ quan được áp dụng ngay phía client rồi báo Host. Hai người nhặt cùng lúc có thể cùng được cơ quan đó (bản đầu chấp nhận).
- Đợt 2: cứu đồng đội, chuyền máu, quan sát.

## 6. Khi gặp sự cố

| Hiện tượng | Cách xử lý |
|---|---|
| Bấm F2 chỉ hiện thông báo "chưa cài gói" | Cài gói ở bước 1 |
| Lỗi đỏ ở Console sau khi cài gói | Chụp Console gửi lại; có thể là khác biệt tên hàm giữa các phiên bản gói |
| "Không kết nối được" sau 10 giây | Kiểm tra địa chỉ, cổng, tường lửa của máy Host; thử `127.0.0.1` trên cùng một máy |
| "Không mở được phòng (cổng đang bận?)" | Đã có một bản khác đang Host cổng đó; đổi cổng hoặc tắt bản kia |
| Bản không bấm vào có vẻ đứng yên | Game tự chạy nền khi vào phòng; nếu vẫn đứng, bấm vào cửa sổ đó |
| Quái bên người vào phòng không giống Host | Bản build cũ hơn code hiện tại: tạo lại bản build |
| Console: "số cục thức ăn khác nhau" | Hai bên khác scene; dùng cùng `Prototype_01` |
| Người vào phòng thấy hai lớp quái | Báo lại kèm ảnh; quái riêng của họ lẽ ra bị ẩn khi vào phòng |
| Menu God tạo quái ở bên người vào phòng | Chỉ nên dùng Menu God ở bên Host; bên người vào phòng quái tạo ra không đồng bộ |
