# Cập nhật từ phần lõi gửi bên làm hang (2026-11-20, tối)

Nhánh chung để thử: `claude/unity-test-maze`. Đã gộp commit `f928881` của các bạn, 276/276 test thuần đạt.

## Mình đã đọc việc của các bạn (`f928881`) và thấy ổn
- Sàn khoang lưới 180×180 với đường chéo xen kẽ: đúng thứ mình nhờ cho mặt sàn mượt hơn.
- Collider `*_COL` gắn đúng trên node con, không gắn lên root.
- `GroundHeight.At` giờ ưu tiên tia dò collider thật, chọn mặt gần chiều cao ước tính nhất.
  - Hợp với `CreatureMotor` bên mình, vì motor chỉ dùng hàm này khi tia của nó không trúng gì.
  - Lưu ý hiệu năng: hàm này giờ bắn một tia mỗi lần gọi, mà `Prey` và `Predator` gọi nó khi sinh ra và khi trả xác. Chưa thấy chỗ nào gọi mỗi khung hình, nhưng đừng gọi nó mỗi khung hình cho hàng chục con.
- `SlimeController`: SphereCast mượt hơn và ngưỡng dốc đi được hạ xuống 0,45. Đoạn bò qua đá nhỏ (`StepUp`) của mình dùng cùng ngưỡng, nên hai bên khớp.

## Phần lõi đã làm, các bạn không phải sửa gì (đã gộp vào `claude/unity-test-maze`)
1. **`CreatureMotor`:** sinh vật dò sàn và vách bằng collider thật, bị vách chặn thì đi vòng, bậc và vực giới hạn theo loài (đi bộ, nhảy, leo, bay). Prefab của các bạn không cần sửa. Chi tiết: `2026-11-20-movement-for-cave-team.md`.
2. **Sinh vật không đi xuyên nhau:** con đang đi bị đẩy ngang ra khỏi con khác. Vẫn đi xuyên **vật không có collider hoặc collider đặt trigger**: nhờ các bạn kiểm tra lại với cầu, bục, vách.
3. **Tổ:** con mồi cùng loài, cùng chỗ, từ 5 con trở lên chia việc (cặp đi kiếm ăn, con do thám, con giữ tổ báo động).
4. **Cùng loài không đánh nhau:** kẻ săn không coi đồng loại là đối thủ hay món ăn, bẫy gai của Bọ Gai Tím không hại Bọ Gai Tím.
5. **Slime bò qua đá nhỏ:** bậc tới khoảng 40% bề ngang thân; vách cao hơn thì không leo được.
6. **Sóng Dò Mạnh** có tác dụng (vòng sóng dò đi xa gấp 1,5).
7. **Hệ sinh thái chạy thật:** `Region` của hang nào (nhận ra qua tên loài như `ThanLanBoHo`) dùng quần thể B1 của hang đó. Đường nối giữa các hang đi theo đồ thị mê cung của các bạn (`MazeGraph` qua `IMazeAdjacency`).
   - Hang nhận ra qua chủ đề khoang (`MazeEcologyLinks.CaveOf`). Hóa Thạch Slime Cổ và Hang Tiếng Thở chưa có chủ đề khoang riêng, nên chưa nối với hang nào. Khi các bạn thêm hai chủ đề này, báo mình thêm một dòng.

## Cập nhật (chủ dự án đã quyết): con của bộ tạo bầy giờ tính vào quần thể
- `Region.Adopt`: con do `PreySpawner` / `PredatorSpawner` thả ra (nằm dưới `Region` của hang) chiếm một chỗ trong quần thể. `Region` chỉ tạo thêm phần còn thiếu, giết một con thì quần thể giảm một. Cảnh của các bạn không phải đổi.
- Con thả ở chỗ không có `Region` ở trên (các khoang dùng `PlaceCreatures` của `MazeChamberDecorator`) vẫn là sinh vật thường, không tính vào mô hình.

## Mình đã sửa thẳng trong file Editor của các bạn (xem khi gộp)
Chủ dự án thấy các khoang rộng 75 m còn trống so với Hang Khởi Sinh, nên mình thêm `Maze/Pure/ChamberDensity.cs` (số lượng và vị trí tổ theo bán kính khoang, có test) và dùng nó ở hai file:
- `CaveWave1ScenePopulator.PopulateEcosystem`: hang rộng có 3 tổ mỗi loài rải khắp sàn (Chúa Hang 1 tổ), tổng số con nhân khoảng 1,875 lần. Hang Khởi Sinh (bán kính 40) không đổi.
- `MazeChamberDecorator`: cây cỏ, đá tảng, thức ăn và số con trong `PlaceCreatures` nhân theo bán kính. Cụm cảnh chủ đề (`BuildGrandThemeLandmarks`) được lặp lại trên hai vòng ngoài (`ScatterLandmarkCopies`).
- Mô hình sinh thái cũng được nhân tương ứng cho hang rộng (`CaveEcologySeed.Build(scaleOf)`, 2 giờ mô phỏng vẫn đạt).
- Nếu các bạn đang sửa hai file này, gộp nhánh cẩn thận. Muốn chỉnh độ dày thì chỉnh `ChamberDensity.Multiplier`.
- Giới hạn: hang rộng nào chưa có `Populate*` riêng thì chỉ được tăng cây, đá, thức ăn và số con ở `PlaceCreatures`; vị trí tổ phụ dùng chiều cao tạm của khoang (chưa bám sàn thật).

## Cần các bạn xem (có thể là lỗi ở cảnh)
**(Đã xử lý ở trên)** Trước đó mỗi loài bị tạo hai lần.
- Bộ tạo bầy `PreySpawner` / `PredatorSpawner` do `CaveWave1ScenePopulator` và `MazeChamberDecorator` đặt thả ngay một bầy khi vào hang.
- `Region` lại tạo thêm con từ tổ (`Den`) theo số liệu quần thể.
- Con của bộ tạo bầy không nằm trong quần thể, nên slime ăn chúng thì quần thể không giảm.
- Mình đang chờ chủ dự án chọn hướng sửa. Đề xuất là giữ bộ tạo bầy của các bạn và tính những con nó tạo vào quần thể (phía mình làm, các bạn không phải đổi cảnh). Các bạn đừng sửa chỗ này trước khi có quyết định.

## Mình cần các bạn (tiếp theo)
1. Kiểm tra mọi vách, bục, cầu có collider và không đặt `isTrigger`.
2. `CreatureLocomotion.SamplePhysicalGroundY`: tia bắt đầu cao hơn thân 1,8 m nên hình con dưới cầu thấp có thể bị nâng lên theo mặt cầu. Đề xuất bắt đầu ở khoảng `worldPos.y + 0.5f` và bỏ qua `attachedRigidbody != null`.
3. Tầng Sâu (Nghĩa Địa, Hóa Thạch, Tiếng Thở, Khoang Phong Ấn): khi có khoang và prefab thì chạy lại `Evolve → Attach Creature Brains` để gắn não, phòng vệ và bảng rơi.

## Cập nhật: kích cỡ, độ khó theo tầng, vùng trong khoang (nhánh `claude/unity-test-maze`)
Thiết kế: `docs/superpowers/specs/2026-11-20-wide-caves-size-danger-design.md`.
- **Chúng mình sửa file của các bạn:**
  - `Predator.cs` (chung): thêm `Gentle`, `ScaleDanger`, `SafeZone` và trạng thái "hiền".
  - `CreatureMotor.Resize` và `CreatureLocomotion.ScaleBy` (thêm vài dòng).
  - `CaveWave1ScenePopulator`: vùng an toàn ở hang đầu, số kẻ săn theo tầng, nhận thêm `tunnelAnglesDeg` và `plan` ở năm hàm `Populate*Chamber` của khoang thủ tục.
  - `MazeChamberDecorator`: cụm cảnh có đèn đối diện cửa hầm chính (`PlaceBeacon`), đường mòn đèn mờ và góc ẩn có khoáng (`PlaceTrailAndNook`).
- **Mình cần các bạn:**
  - Hai khoang dựng sẵn bằng Blender (Vườn Quặng, Vực Nứt Tím) chưa có cụm cảnh, đường mòn, góc ẩn. Nếu muốn có thì hàm `BuildHandcrafted*Chamber` cần truyền các hướng hầm.
  - Cho mình biết bán kính thật của hai khoang đó: mình đang giả định 75 m để đặt tổ.
- **Sinh vật to hơn:** Chúa Hang rộng đến 3 đến 4,5 m. Nếu hầm hay lối nào nhỏ hơn thế thì báo mình.

