# Phòng trùm: giao việc cho phiên dựng map

**Làm trước 3 phòng: Golem Thạch Anh, Chim Hang Mẹ, Bọ Cạp.** Các trùm khác tính sau. Phiên này (làm Golem) viết logic chiến đấu và khối `BossRoom`; phiên dựng map chỉ lo hình học và đồ đặt trong phòng. Hai bên nối với nhau bằng **tên GameObject** (mục 3), không cần gắn script.

## 1. Ý đồ chung
Mỗi trùm ở trong **một căn phòng riêng**. Trùm **chưa xuất hiện** cho đến khi người chơi **kích hoạt** phòng (bước vào vùng kích hoạt). Trùm dùng đồ trong phòng để tấn công (Golem: cây rìu cắm giữa phòng; các đồ khác để sau). Phòng phải đủ rộng và cao để mọi chiêu của trùm chạy được.

## 2. Kích thước (theo yêu cầu: "to một chút, bằng tầm map hồi sinh ngay đầu")
Map hồi sinh (`Slime_Cavern`, xem `CavernLayout.cs`): **sàn bán kính 34 m** (đường kính 68 m), tường cao 30 m, trần 36 m. Phòng trùm lấy cùng cỡ:

| Thông số | Giá trị | Vì sao |
|---|---|---|
| Sàn phẳng dùng được | **bán kính ≥ 30 m** (đường kính ~60 m), không hồ, không dốc đáng kể | Golem: Húc Đầu chạy 12 m, Bổ Đất dải sóng 14 m, ném cột 20 m, đi vòng đánh giá ở 6,5 m, Giáng Thạch rải cột trong 6,5 m quanh chỗ đáp |
| Chiều cao trần | **≥ 30 m** ở giữa (tối thiểu 24 m) | Golem nhảy Giáng Thạch lên **14 m**; Chim Bổ Nhào leo **9 m** và quắp nhấc người chơi lên 6,5 m (chim tự hạ thấp khi trần thấp nhưng cần chỗ bay) |
| Tường | thẳng đứng hoặc dốc đứng, có collider liền, **không có khe hở** | Húc Đầu đâm tường là choáng; đá ném vỡ khi chạm tường |
| Mặt sàn | gần phẳng, **có collider** (không phải địa hình trống) | chân Golem tính theo độ cao mặt đất (`GroundHeight`) |

Dùng cùng mặt phẳng y = 0 ở giữa phòng; đặt gốc phòng ở (0,0,0) trong cảnh con để dễ gắn.

## 3. Hợp đồng chung (tên GameObject rỗng trong phòng; chữ hoa thường đúng như viết)
| Tên | Loại | Ý nghĩa |
|---|---|---|
| `Player_Entry` | rỗng | chỗ người chơi xuất hiện khi vào phòng; **đặt cách xa trùm**, quay mặt vào giữa phòng |
| `Boss_Wait` | rỗng | chỗ trùm nằm chờ / xuất hiện khi được kích hoạt |
| `Activate_Zone` | collider **trigger** (hộp hoặc cầu) | người chơi bước vào thì phòng kích hoạt, trùm xuất hiện; không có thì kích hoạt ngay |
| `Room_Bounds` | collider trigger bao cả phòng | giới hạn phòng (để đóng cửa, ngắt giao tranh khi người chơi chạy ra) |
| `Prop_Pillar_*`, `Prop_Crystal_*`, `Prop_Perch_*` | đồ có collider | cột, tinh thể, ụ đậu: chỉ cần có mặt và có collider; hành vi để sau |

**Riêng Golem:** thêm `Axe_Anchor` (rỗng, đặt **giữa phòng**, y = mặt sàn) cùng một **cây rìu cắm ở đó** làm đồ trang trí **không có collider chặn người chơi** (code của phiên Golem sẽ thay bằng rìu động khi cần). Bệ đá nhỏ quanh rìu bán kính ~1,5 m được.

## 4. Từng phòng
### 4.1. Golem Thạch Anh ("Lò Tinh Thể")
- Chủ đề: hầm đá cổ, tinh thể phát sáng ấm (vàng cam) trên tường và trần, ánh sáng mờ.
- Bố cục: hình tròn hoặc bầu dục, sàn đá phẳng. **Vài cột đá to** (bán kính ≥ 1,2 m) rải cách giữa phòng 10–20 m (Golem Húc Đầu có thể đâm vào cột; sau này tinh thể rơi). `Axe_Anchor` giữa phòng. `Boss_Wait`: một **khối đá lớn** phía xa lối vào (Golem thức dậy từ khối đó).
- `Activate_Zone`: ngay sau lối vào, hoặc một vòng ngoài khoảng 12 m quanh `Axe_Anchor`.
- Lưu ý: giữ khoảng giữa phòng thoáng (ít nhất 20 m đường kính) để Giáng Thạch và vòng đi vòng chạy tốt.

### 4.2. Chim Hang Mẹ ("Tổ Kền Kền") — con kền kền Dreadwing
- Chủ đề: hang cao, ánh xanh ngọc lạnh, đống xương và ổ lông.
- Bố cục: **trần cao nhất có thể (≥ 30 m)** và phòng **thoáng, ít vật cản thấp** (chim bổ nhào cần ngắm và hạ xuống). Một ổ lớn ở giữa hoặc sát tường làm `Boss_Wait`; vài ụ đá/xương cao `Prop_Perch_*` (cao 2–6 m) rải quanh làm chỗ chim đậu.
- `Activate_Zone`: quanh ổ trứng/đống xương ở giữa (chạm vào thì chim bay xuống từ trần).
- Lưu ý: không đặt vật cản trong khoảng 9 m trên đầu chỗ giữa phòng (chim leo lên đó khi Bổ Nhào).

### 4.3. Bọ Cạp Thạch Anh ("Bãi Cát Tinh Thể") — **tạm, chờ chủ dự án duyệt bộ chiêu Bọ Cạp**
- Bộ chiêu Bọ Cạp **chưa được duyệt**; theo đề xuất hiện có (xem `docs/superpowers/specs/2026-11-21-boss-standard-kit.md`) nó có Phóng Gai Tinh Thể, Giáng Đuôi, Kẹp Càng, Đào xuống đất.
- Chủ đề đề xuất: bãi cát và đá nâu đỏ, tinh thể mọc lởm chởm; **một vùng cát lún** (bán kính ~8 m) làm chỗ trùm chôn mình `Boss_Wait` và trồi lên.
- Bố cục: sàn phẳng bán kính ≥ 30 m, vài **tảng đá thấp (cao 1–2 m)** làm chỗ núp trước loạt gai; `Activate_Zone` ở rìa vùng cát; trần ≥ 24 m.
- Có thể thay đổi sau khi duyệt bộ chiêu (nên giữ các điểm tên chung để nối được ngay).

## 5. Cách dựng (đề nghị, phiên dựng map quyết)
- **Mỗi phòng một cảnh con** (ví dụ `BossRoom_Golem.unity`) hoặc một prefab phòng, **đừng nhét vào `Slime_Cavern.unity`** (đã nặng 63 MB). Cảnh lớn đi qua Git LFS.
- Dùng lại hạ tầng có sẵn nếu hợp: `CavernBuilder`/`MazeTerrainMath` (xem `Assets/_Project/Editor/`, `Scripts/Maze/`), cảnh `TestArena.unity` để thử nhanh. Mặt sàn và tường cần collider; địa hình tính độ cao qua `Evolve.World.GroundHeight`.
- **Không** viết hành vi chiến đấu của trùm: đó là việc của phiên Golem/Chim/Bọ Cạp. **Không** gắn trùm vào phòng; khối `BossRoom` do phiên Golem viết sẽ tìm các điểm theo tên (tài liệu chi tiết: `docs/superpowers/specs/2026-11-22-golem-axe-form-design.md`, mục "Hợp đồng với map").

## 6. Việc bàn giao và kiểm tra (cho mỗi phòng)
1. Cảnh/prefab phòng đủ các điểm tên ở mục 3, đo đúng kích thước mục 2.
2. Chạy thử trong Unity: đứng ở `Player_Entry` nhìn thấy cả phòng, không có chỗ hở trên tường; đi bộ giữa phòng không bị kẹt; trần không thấp hơn yêu cầu.
3. Chụp ảnh phòng (nhìn từ `Player_Entry`) gửi chủ dự án.
4. Ghi vào `docs/TEST_CHECKLIST_UNITY.md` cách thử phòng; commit và push lên `claude/unity-test-maze` (và `claude/movement`), nhớ `git pull --no-rebase origin claude/unity-test-maze` trước.

## 7. Câu hỏi mở (chủ dự án quyết, mặc định của tôi đặt trong ngoặc)
- Lối vào phòng: một cửa hẹp ở rìa (một lối vào duy nhất), hay thông với hang?
- Sau khi kích hoạt, có **đóng lối ra** cho đến khi trùm chết hoặc người chơi chết? (mặc định: không đóng, chỉ ngắt giao tranh khi người chơi ra khỏi `Room_Bounds`.)
- Co-op: nhiều người chơi cùng kích hoạt thì sao? (mặc định: người đầu tiên bước vào kích hoạt cho cả phòng.)
