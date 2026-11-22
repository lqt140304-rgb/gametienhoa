# Thiết kế 3 Phòng Trùm: Golem Thạch Anh, Chim Hang Mẹ, Bọ Cạp Thạch Anh

**Tài liệu liên quan:** [BOSS_ROOMS_BRIEF.md](../../BOSS_ROOMS_BRIEF.md), [golem-thach-anh.md](../../lore/golem-thach-anh.md) (Lore cách A: không nói ai làm cửa và rìu), [2026-11-22-chim-hang-me-moveset-design.md](./2026-11-22-chim-hang-me-moveset-design.md), [2026-11-22-bo-cap-thach-anh-moveset-design.md](./2026-11-22-bo-cap-thach-anh-moveset-design.md).

---

## 1. Quy chuẩn & Hợp đồng chung cho cả 3 phòng

- **Độc lập với `Slime_Cavern.unity`:** mỗi phòng xuất thành 1 Scene riêng (`Assets/_Project/Scenes/BossRoom_{Golem,Bird,Scorpion}.unity`) và 1 Prefab phòng riêng (`Assets/_Project/Prefabs/BossRooms/BossRoom_{Golem,Bird,Scorpion}.prefab`), gốc phòng đặt tại `(0, 0, 0)` và gắn sẵn component `BossRoom` (`Assets/_Project/Scripts/World/BossRoom.cs`) theo [BOSS_ROOM_CONTRACT.md](../../BOSS_ROOM_CONTRACT.md).
- **Kích thước đạt chuẩn:**
  - Sàn phẳng dùng được: bán kính `32 m` (đường kính `64 m ≥ 60 m`), `y ≈ 0`, có `MeshCollider` liền khối (`*_Shell_COL`), không có hồ, không dốc đáng kể.
  - Tường & trần kín 360° liền mạch, không có lỗ thủng trên trần, không khe hở trên tường.
  - Lối vào duy nhất ở hướng Nam (`z = -31.5 m`): lòng cửa rộng `4.8 m`, cao `7.2 m` (`4–5 × 6–8 m`), kèm sảnh kín sâu `7 m` phía sau.
- **Cánh cửa thép dùng chung (`Assets/_Project/Models/Underroot/BossRoom_SteelDoor.fbx`):**
  - Tấm cửa thép gỉ dày `5.4 × 7.2 × 0.7 m`, bản lề to, giằng chữ X, hai dây xích sắt kéo lên khe trần; không đánh dấu `Static` để `BossRoom` trượt mượt từ `Door_Open_Pos` `(0, 11.2, -31.5)` xuống `Door_Closed_Pos` `(0, 3.6, -31.5)`.
- **Hợp đồng GameObject con trực tiếp (chữ hoa/thường chính xác):**
  - `Player_Entry` `(0, 0, -26.0)` quay `+Z`
  - `Boss_Wait` (quay `180°` quanh Y nhìn về phía cửa)
  - `Door_Mouth` `(0, 0, -31.5)` quay `+Z` hướng vào trong phòng
  - `Door_Steel`, `Door_Open_Pos`, `Door_Closed_Pos`
  - `Activate_Zone`: `BoxCollider` (`isTrigger = true`), phủ `z ∈ [-23, +29]` (`8.5 m` sau `Door_Mouth` > `6 m`)
  - `Room_Bounds`: `BoxCollider` (`isTrigger = true`), bao trọn toàn bộ phòng và sảnh cửa
  - `Prop_Pillar_*`, `Prop_Crystal_*`, `Prop_Perch_*` có `MeshCollider`.

---

## 2. Chi tiết từng phòng

### 2.1. Phòng 1 — Golem Thạch Anh ("Lò Tinh Thể")
- **Tông màu & kích thước:** xám thạch anh tối, vàng cam ấm của lõi, đỏ gỉ thép; trần kín giữa phòng `34.5 m`.
- **Cửa vào:** vòm đá cổ góc cạnh khảm tinh thể vàng cam ấm, bản lề to, vết trầy sâu trên sàn trước ngưỡng cửa.
- **Cây rìu & khối đá lõi (`Axe_Anchor` tại `(0, 0.06, 0)`):**
  - Đặt trực tiếp prefab `Assets/_Project/Resources/Props/GolemAxe_Planted.prefab` chạm mặt trên khối đá lõi thấp (`0.06 m`), **không collider, không bậc thang hay chỗ đứng gần**.
  - Quanh khối lõi là mạng nhện vết nứt tỏa phát sáng ấm bán kính `5 m` và mảnh tinh thể nhỏ sát sàn.
- **Kể chuyện môi trường (Lore cách A — không chữ viết, không tranh vẽ, không xương người):**
  - Đường ray sắt gỉ chạy sát sàn từ cửa vào tới `z = -15.5 m` rồi gãy đứt ngang; xe goòng gỉ nghiêng ở `(x = -8.2, z = -22.5)` chở tinh thể cắt vuông.
  - `Prop_Pillar_01..06` (`r = 1.35–1.5 m`, cách tâm `16–17.2 m`): nứt chân, quấn vòng xích gỉ; `Prop_Pillar_03` đổ nghiêng, `Prop_Pillar_04` gãy đôi.
  - `Prop_Crystal_01..08` sát tường (`r = 27.8–28.9 m`): 2 cụm bị đập vỡ rải vụn quanh chân.
  - 3 tượng Golem nhỏ thất bại nửa chìm gần tường; nhũ đá trần gãy nham nhở (vết Giáng Thạch); khối đá lớn `Boss_Wait` ở `(0, 0, 23)`.

### 2.2. Phòng 2 — Chim Hang Mẹ ("Tổ Kền Kền")
- **Tông màu & kích thước:** đá phiến lạnh + ánh xanh ngọc lạnh; **trần vòm cao `38.5 m` ở giữa**, thoáng tuyệt đối trong toàn bộ khoảng không phía trên để Chim Bổ Nhào (`9 m`) và Quắp (`6.5 m`).
- **Cửa vào:** khung vòm xương sườn thú cổ đại uốn cong kèm sọ thú sừng dài và tinh thể xanh ngọc lạnh.
- **Bố cục:** ổ lông + xương lớn làm `Boss_Wait` ở `(0, 0, 21)`, đống xương thấp sát sàn ở giữa phòng, **6 ụ đá/xương `Prop_Perch_01..06` (`cao 2.8–5.8 m`)** đặt ở vòng ngoài `r = 22–23.8 m`, và `Prop_Crystal_01..04`.

### 2.3. Phòng 3 — Bọ Cạp Thạch Anh ("Bãi Cát Tinh Thể", tạm)
- **Tông màu & kích thước:** bãi cát nâu đỏ, sa thạch phân tầng xói mòn và tinh thể thạch anh tím lởm chởm; trần kín `31.5 m`.
- **Cửa vào:** vòm đá cát bị gió xói mòn phân tầng bất đối xứng khảm tinh thể tím.
- **Bố cục:** vùng cát lún xoáy bán kính `8.5 m` ở `(0, -0.1, 10)` làm chỗ `Boss_Wait` chôn mình trồi lên; **6 tảng đá thấp `Prop_Pillar_01..06` (`cao 1.5–1.95 m`)** ở `r = 15.5–17.8 m` làm chỗ núp trước Phóng Gai / đâm kẹt càng Lao Kẹp; và `Prop_Crystal_01..08` tím lởm chởm quanh chân tường.
