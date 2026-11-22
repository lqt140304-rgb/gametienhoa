# Thiết kế 4 Phòng Trùm Đợt 2: Chó Xương, Cáo Gió, Cóc Hồ Già, Rắn Dung Nham

**Tài liệu liên quan:** [BOSS_ROOMS_BRIEF.md](../../BOSS_ROOMS_BRIEF.md), [BOSS_ROOM_CONTRACT.md](../../BOSS_ROOM_CONTRACT.md), [2026-11-22-boss-rooms-design.md](./2026-11-22-boss-rooms-design.md), [2026-11-21-boss-standard-kit.md](./2026-11-21-boss-standard-kit.md), [2026-11-21-cho-xuong-moveset-design.md](./2026-11-21-cho-xuong-moveset-design.md), [2026-11-20-caves-creatures-ecosystem-design.md](./2026-11-20-caves-creatures-ecosystem-design.md), [MESHY_BOSSES.md](../../MESHY_BOSSES.md).

---

## 1. Mục tiêu & Quy chuẩn Hợp đồng chung (Phương án A)

Mở rộng hệ thống phòng trùm cho **4 Chúa Hang đã có mô hình Meshy** trong `Assets/_Project/Prefabs/Wildlife/` và `Assets/_Project/Models/Wildlife/Meshy/`:
1. **Chó Xương (`BossRoom_Hound`)** — *Nghĩa Địa Cự Thú*
2. **Cáo Gió (`BossRoom_Fox`)** — *Khe Gió Hú*
3. **Cóc Hồ Già (`BossRoom_Toad`)** — *Đầm Rễ Cổ (Hang Khởi Sinh)*
4. **Rắn Dung Nham (`BossRoom_Snake`)** — *Hố Mạch Dung Nham (Suối Nước Nóng)*

Cả 4 phòng kế thừa toàn bộ quy chuẩn hình học và hợp đồng `BossRoom` từ Đợt 1:
- **Độc lập với `Slime_Cavern.unity`:** Mỗi phòng xuất thành 1 Scene riêng (`Assets/_Project/Scenes/BossRoom_{Hound,Fox,Toad,Snake}.unity`) và 1 Prefab phòng sạch (`Assets/_Project/Prefabs/BossRooms/BossRoom_{Hound,Fox,Toad,Snake}.prefab`), gốc phòng tại `(0, 0, 0)`.
- **Mặt sàn & chi tiết môi trường phẳng:**
  - Sàn collider `*_Shell_COL` phẳng, bán kính `32 m` (`đường kính 64 m >= 60 m`), `y ≈ 0`, **không có hồ nước bơi, không hố sâu làm kẹt di chuyển**.
  - Mọi chi tiết kể chuyện sát sàn (vũng bùn/chất nhầy, rãnh dung nham nóng chảy, ổ xương chui, rãnh móng cào/trượt bùn, vân cát xoáy) giữ độ cao `<= 0.06 m` và liệt kê trong `NonColliderObjectNames` để slime đi trơn tru qua không bị vấp.
- **Tường & trần kín hoàn toàn:** Vách đứng/dốc kín 360° không khe hở, trần vòm kín 100% không lỗ thủng (`31.5–36.0 m` tùy phòng).
- **Lối vào duy nhất & cửa thép dùng chung:**
  - Cửa Nam tại `Door_Mouth` `(0, 0, -31.5)` quay `+Z` vào trong phòng, lòng cửa `4.8 × 7.2 m` kèm sảnh kín sâu `7 m` và khung cổng riêng đặc trưng từng loài.
  - Dùng chung `Assets/_Project/Models/Underroot/BossRoom_SteelDoor.fbx`: `Door_Steel` không đánh dấu `Static`, có `BoxCollider` chặn (`isTrigger = false`), treo ở `Door_Open_Pos` `(0, 11.2, -31.5)` và trượt kín xuống `Door_Closed_Pos` `(0, 3.6, -31.5)`.
- **Hợp đồng GameObject con trực tiếp trên gốc phòng:**
  - `Player_Entry` `(0, 0, -26.0)` quay `+Z`
  - `Boss_Wait` (quay `180°` quanh Y hướng về phía cửa)
  - `Door_Mouth`, `Door_Steel`, `Door_Open_Pos`, `Door_Closed_Pos`
  - `Activate_Zone`: `BoxCollider` (`isTrigger = true`), phủ `y ∈ [-2, +32]`, `z ∈ [-23, +29]` (cách `Door_Mouth` `8.5 m > 6 m`, ôm trọn sàn để `ClosestPoint` luôn bắt đúng khi slime đứng trên sàn)
  - `Room_Bounds`: `BoxCollider` (`isTrigger = true`), kích thước `68 × 42 × 74 m`
  - `Prop_Pillar_*`, `Prop_Crystal_*`, `Prop_Perch_*`: đều có `MeshCollider`.
  - Gắn sẵn component `Evolve.World.BossRoom` (`Assets/_Project/Scripts/World/BossRoom.cs`) lên GameObject gốc của phòng.
- **Quy tắc đặt Trùm trong Scene (tách biệt với Prefab phòng):**
  - Prefab `BossRoom_*.prefab` giữ **sạch** (không chứa instance trùm bên trong để sau này ghép đại hang không bị trùng quái).
  - Trong Scene `BossRoom_*.unity`, builder tự đặt prefab trùm tương ứng (ưu tiên `<Tên> (Meshy).prefab`, fallback `<Tên>.prefab`) tại đúng vị trí/góc quay của `Boss_Wait` và gán thẳng vào trường `boss` trên component `BossRoom`, giúp bấm Play bước qua `Activate_Zone` là thử được đóng cửa thép và đánh thức trùm ngay.

---

## 2. Thiết kế chi tiết từng phòng (gắn liền sinh thái, chuỗi thức ăn & bộ chiêu)

### 2.1. Phòng 1 — `BossRoom_Hound` (Chó Xương — "Nghĩa Địa Cự Thú")
- **Tệp chính:** `Assets/_Project/Models/Underroot/BossRoom_Hound.fbx`, `BossRoom_Hound.prefab`, `BossRoom_Hound.unity`.
- **Thông số chính:** Trần giữa kín `33.5 m`, `Boss_Wait` tại `(0, 0, 21.0)`, prefab trùm gắn trong Scene: `ChoXuong (Meshy).prefab` (fallback `ChoXuong.prefab`).
- **Tông màu & ánh sáng:** Đất bùn hóa thạch xám tro tối, xương khô trắng ngà, ánh xanh lục/lam lạnh ma quái (đồng bộ với sợi xích đen-xanh lạnh quanh cổ Chó Xương bản Meshy).
- **Khung cổng vào (riêng từng loài):** **Vòm Hàm & Xương Đùi Cự Thú** — hai xương đùi cự thú dựng làm cột hai bên, hàm răng thú khổng lồ úp trên đỉnh vòm, đoạn xích sắt đen-xanh đứt treo lủng lẳng hai bên ngưỡng cửa.
- **Kể chuyện môi trường & dấu vết bộ chiêu:**
  - **Ổ trùm (`Boss_Wait`):** Bộ khung xương lồng ngực & hộp sọ cự thú khổng lồ nửa vùi dưới bùn hóa thạch (gợi kỳ ngộ *Bữa Tiệc Cổ* mà Chó Xương canh giữ); ngay cạnh là một cọc trụ đá cổ bị giật gãy cùng đoạn xích đen-xanh lạnh đứt lìa.
  - **Dấu vết `Vồ (Pounce)` & `Cào Xé`:** Các vệt móng cào 4 rãnh sâu và vết trượt dài trên nền bùn quanh vùng giữa phòng (`cao <= 0.03 m`, không collider).
  - **Dấu vết `Phun Xương (Spit)` & Bọ Ăn Xác:** Đống mảnh xương vụn bị nhai nát quanh ổ và chân cột, xen các mảng đá bị axit của Bọ Ăn Xác ăn mòn lỗ chỗ.
  - **Dấu vết `Triệu Hồi (Summon)` (3 chó con đội đất chui lên):** 5 ổ đất nứt có xương sườn và sọ chó nhỏ trồi nửa vời sát sàn (`cao <= 0.05 m`, không collider) phân bố ở bán kính `6–10 m`.
- **Vật cản chiến đấu (`Prop_*` có `MeshCollider`):**
  - `Prop_Pillar_01..06` (`r = 15.0–18.0 m`): 6 cột xương sườn cự thú hóa thạch cắm cong từ sàn, mang vết răng cắn và vòng xích đứt — dùng để chắn mảnh **Phun Xương** và dụ Chó Xương **Vồ** hụt ngã lăn.
  - `Prop_Perch_01..04` (`r = 10.5–13.5 m`, cao `0.9–1.4 m`, mặt trên phẳng): 4 bục hộp sọ & đốt sống lưng cự thú khớp tầm nhảy `0.3–1.5 m` của chiêu **Nhảy Vượt (`Perch`)**.
  - `Prop_Crystal_01..06` (`r = 27.5–29.0 m`): 6 cụm tinh thể ma trơi xanh lam/lục lạnh mọc từ hốc xương ven tường.

### 2.2. Phòng 2 — `BossRoom_Fox` (Cáo Gió — "Khe Gió Hú")
- **Tệp chính:** `Assets/_Project/Models/Underroot/BossRoom_Fox.fbx`, `BossRoom_Fox.prefab`, `BossRoom_Fox.unity`.
- **Thông số chính:** Trần giữa kín `36.0 m`, `Boss_Wait` tại `(0, 0, 22.0)`, prefab trùm gắn trong Scene: `CaoGio (Meshy).prefab` (fallback `CaoGio.prefab`).
- **Tông màu & ánh sáng:** Đá phiến xám bạc bị gió bào mòn lượn sóng, ánh trắng bạc – xanh lam gió lạnh.
- **Khung cổng vào (riêng từng loài):** **Vòm Đá Xoáy Phong Thạch** — các phiến đá mỏng uốn cong như luồng lốc xoáy ôm lấy cửa, khảm tinh thể phong thạch xanh bạc phát sáng.
- **Kể chuyện môi trường & dấu vết bộ chiêu:**
  - **Ổ trùm (`Boss_Wait`):** Bệ đá phiến phẳng hình lưỡi liềm bị gió mài nhẵn, phía sau có khe nứt vách đá hẹp rít gió (gợi kỳ ngộ *Túi Của Kẻ Đi Trước*); quanh chân bệ rải vỏ Dế Gió và xương Chuột Gió (con mồi trong chuỗi thức ăn).
  - **Dấu vết `Gọi Bầy (Call)` (gọi đàn Chồn Khe):** 6 hốc đá / cửa hang thấp sát chân tường (`Chồn Khe Burrows`) có xương mồi nhỏ đặt trước cửa hốc.
  - **Dấu vết `Giáo Gió (Spears 26 m/s)` & `Bão Giáo`:** Các vết chém gió cắt xiên sâu trên thân cột đá và vách đối diện, kèm các mảnh phong thạch sắc nhọn găm nghiêng vào đá.
  - **Dấu vết `Vòng Xoáy` & chuyển động nhanh:** Các dải vân cát bạc xoáy đồng tâm sát sàn (`cao <= 0.03 m`, không collider).
- **Vật cản chiến đấu (`Prop_*` có `MeshCollider`):**
  - `Prop_Pillar_01..06` (`r = 14.0–17.2 m`, đường kính `~2.5 m`): 6 cột đá phiến bị gió xoáy tiện rãnh xoắn ốc và chi chít vết Giáo Gió chém — làm lá chắn sống còn khi Cáo Gió phóng loạt 3 mũi **Giáo Gió**.
  - `Prop_Perch_01..04` (`r = 21.0–24.0 m`, cao `1.8–3.2 m`): 4 mỏm đá nghiêng theo chiều gió ở vòng ngoài.
  - `Prop_Crystal_01..06` (`r = 27.5–29.0 m`): 6 cụm tinh thể phong thạch xanh bạc mọc nghiêng `45°` theo hướng gió xoáy dọc tường.

### 2.3. Phòng 3 — `BossRoom_Toad` (Cóc Hồ Già — "Đầm Rễ Cổ")
- **Tệp chính:** `Assets/_Project/Models/Underroot/BossRoom_Toad.fbx`, `BossRoom_Toad.prefab`, `BossRoom_Toad.unity`.
- **Thông số chính:** Trần giữa kín `32.0 m`, `Boss_Wait` tại `(0, 0, 20.0)`, prefab trùm gắn trong Scene: `CocHoGia (Meshy).prefab` (fallback `CocHoGia.prefab`).
- **Tông màu & ánh sáng:** Đá bùn nâu lục ẩm, mạng rễ đại thụ đan kín trần vòm (kín 100%, không lỗ thủng), ánh xanh lục – vàng cốm từ rêu và nấm/quặng sinh học phát quang.
- **Khung cổng vào (riêng từng loài):** **Vòm Rễ Cổ Thụ Đan Đá** — những chùm rễ cổ thụ to như thân cột quấn chặt khối đá rêu ẩm hai bên cửa, điểm nấm và tinh thể phát quang xanh lục.
- **Kể chuyện môi trường & dấu vết bộ chiêu:**
  - **Ổ trùm (`Boss_Wait`):** Đệm bùn rêu trũng nông ôm giữa hai chùm rễ cổ thụ; xung quanh vương vãi vỏ **Ốc Hồ** rỗng bị axit ăn thủng, vỏ **Bọ Cánh Cứng Nhỏ** và xương **Thằn Lằn Bờ Hồ** do Cóc nhổ ra sau **Nuốt Chửng**.
  - **Dấu vết `Lưỡi Bắn (6–12 m, kéo con mồi)`:** Các rãnh trượt bùn thẳng tắp trên mặt sàn hướng về miệng ổ Cóc (`cao <= 0.04 m`, không collider) như vết mồi bị lưỡi lôi xềnh xệch.
  - **Dấu vết `Da Nhầy`, `Phình Nổ (mây độc)` & `Ợ Chấn (đẩy lùi 4 m)`:** Các vũng bùn/chất nhầy phát quang xanh lục đục sát sàn (`cao <= 0.05 m`, không collider), gợn sóng bùn đồng tâm quanh ổ Cóc và các túi nấm bùn độc sát vách.
- **Vật cản chiến đấu (`Prop_*` có `MeshCollider`):**
  - `Prop_Pillar_01..06` (`r = 13.0–17.0 m`, cao `1.8–2.8 m`): 6 trụ rễ đại thụ đan vào tảng đá rêu, phần gốc bám nhầy và vỏ Ốc Hồ — giúp người chơi núp cú quất **Lưỡi Bắn 12 m** và tì lưng chống bị **Ợ Chấn** đẩy văng.
  - `Prop_Crystal_01..08` (`r = 27.0–29.0 m`): 8 cụm nấm & quặng sinh học phát quang xanh cốm – vàng dịu mọc trên các chùm rễ ven tường.

### 2.4. Phòng 4 — `BossRoom_Snake` (Rắn Dung Nham — "Hố Mạch Dung Nham")
- **Tệp chính:** `Assets/_Project/Models/Underroot/BossRoom_Snake.fbx`, `BossRoom_Snake.prefab`, `BossRoom_Snake.unity`.
- **Thông số chính:** Trần giữa kín `31.5 m`, `Boss_Wait` tại `(0, 0, 21.0)`, prefab trùm gắn trong Scene: `RanDungNham (Meshy).prefab` (fallback `RanDungNham.prefab`).
- **Tông màu & ánh sáng:** Đá bazan đen tím phân cột lục giác, mạch dung nham & hơi lưu huỳnh đỏ cam rực cháy.
- **Khung cổng vào (riêng từng loài):** **Vòm Cột Bazan Nứt Dung Nham** — các cột đá bazan lục giác xếp bậc gãy góc, các khe giữa khối đá rỉ ánh dung nham đỏ cam phát sáng.
- **Kể chuyện môi trường & dấu vết bộ chiêu:**
  - **Ổ trùm (`Boss_Wait`):** Bệ đá bazan cuộn tròn như ổ rắn viền quanh bởi mạch dung nham đỏ rực; bên cạnh có vỏ **Cua Đá Nóng** và vỏ **Ốc Lưu Huỳnh** bị nhiệt nung chảy dính vào đá.
  - **Dấu vết `Thân Nóng` & Lột Xác:** Các rãnh trườn hình chữ S uốn lượn phát sáng đỏ cam sát mặt sàn (`cao <= 0.05 m`, không collider) nơi thân rắn trườn qua làm đá nóng chảy; dọc vách Đông-Bắc có một bộ **xác vỏ rắn lột hóa đá/kết tinh dung nham khổng lồ** vắt qua các khối bazan.
  - **Dấu vết `Phun Lửa Hình Nón (6 m)` & `Phun Nọc Dung Nham`:** Các mảng đá cháy đen hình rẻ quạt trước ổ rắn và mặt trong của các cột bazan.
- **Vật cản chiến đấu (`Prop_*` có `MeshCollider`):**
  - `Prop_Pillar_01..06` (`r = 14.0–18.0 m`, cao `2.2–4.5 m`): 6 cụm cột đá bazan lục giác, mặt hướng về tâm phòng nung đỏ sém, mặt ngoài nguội đen làm chỗ núp **Phun Nọc / Phun Lửa**.
  - `Prop_Crystal_01..08` (`r = 27.0–29.0 m`): 8 ụ ống khói địa nhiệt & tinh thể lưu huỳnh vàng-đỏ mọc dọc chân tường (nơi Ốc Lưu Huỳnh cư trú).

---

## 3. Kiến trúc Code & Kiểm chứng tự động

- **Tái sử dụng `tools/blender/boss_room_common.py`:** Dùng `build_watertight_shell`, `build_steel_door`, `parallel_tube_rings`, vật liệu đỉnh `Shader Graphs / RockVertexColor`, bổ sung thêm 4 script độc lập:
  - `tools/blender/build_boss_room_hound.py`
  - `tools/blender/build_boss_room_fox.py`
  - `tools/blender/build_boss_room_toad.py`
  - `tools/blender/build_boss_room_snake.py`
  Mỗi script sinh FBX vào `Assets/_Project/Models/Underroot/` và 4 ảnh render Cycles kiểm tra vào `docs/art/previews/`.
- **Mở rộng `Assets/_Project/Editor/BossRoomBuildCommon.cs`:**
  - Bổ sung trường `string[] BossPrefabPaths` vào `BossRoomSpec`.
  - Sau bước lưu `BossRoom_*.prefab` sạch (`PrefabUtility.SaveAsPrefabAssetAndConnect`), nếu `BossPrefabPaths` có phần tử tồn tại trên đĩa thì tự động instantiate prefab trùm đầu tiên tìm thấy ngay tại `Boss_Wait` (`position`, `rotation`) trong Scene `.unity` (làm sibling ngoài prefab phòng để không làm bẩn prefab phòng) và gán tham chiếu vào trường `boss` của `BossRoom`.
  - Bổ sung kiểm tra `ValidateContract` cho `BossRoom` Scene instance / hợp đồng chung (trần giữa >= `MinCeilingRequired`, 40 điểm sàn trên `*_Shell_COL`, 24 tia vách kín, toàn bộ `Prop_*` có `Collider`, `Activate_Zone` `y ∈ [-2, +32]` và cách `Door_Mouth >= 5.5 m`).
- **4 Editor builder & Batch runner mới:**
  - `Assets/_Project/Editor/BossRoom{Hound,Fox,Toad,Snake}Builder.cs` (`MenuItem` dựng & kiểm tra từng phòng trong `Evolve/Boss Rooms/...`).
  - Mở rộng `Assets/_Project/Editor/BossRoomBatchRunner.cs` chạy cả Đợt 1 + Đợt 2 (hoặc chạy riêng Đợt 2 `BuildWave2AndCaptureAll`) và chụp ảnh thật trong Unity URP nhìn từ `Player_Entry` & `Door_Mouth` lưu vào `docs/art/previews/unity_bossroom_{hound,fox,toad,snake}_{entry,door}.png`.
- **Tài liệu thử nghiệm:** Cập nhật mục kiểm tra Play Mode (`H15`) vào `docs/TEST_CHECKLIST_UNITY.md`.
