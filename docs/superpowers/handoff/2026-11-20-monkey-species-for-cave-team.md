# Giao bên làm hang: loài mới Khỉ Hang (2026-11-20)

Phần chiến đấu của Khỉ Hang đã làm xong ở phần lõi (`Combat/CreatureCombat`, bộ chiêu `khihang` trong `MovesetSeed`). Loài này **chưa có** trong dữ liệu sinh thái, mô hình hay prefab; hiện chỉ gặp được qua menu thần (quả cầu đặt tên "Khỉ Hang"). Chủ dự án đồng ý đặt Khỉ Hang ở **Vườn Đá Hổ Phách** (tầng Nông, nhiều đá để leo, hiện chưa có sinh vật).

## Cần các bạn làm
1. **Mô hình, bộ xương, clip** (theo bảng ở `2026-11-20-monster-tell-poses-for-cave-team.md`, mục "Bổ sung lát 3"). Khỉ dáng thon, tay dài, đuôi dài; cỡ gốc rộng khoảng 0,7 m, dài thân khoảng 0,9 m (khối 12 kg).
2. **`SpeciesSO` và prefab** `KhiHang` trong `Assets/_Project/Prefabs/Wildlife`, tên prefab là `KhiHang` (mã nhận loài theo tên: `SpeciesNames.Key`). Kẻ săn: thành phần `Predator` như các loài khác, rồi chạy `Evolve → Attach Creature Brains`. Số liệu gợi ý theo lát 3: khối 12, máu 40, cắn 8, tốc độ đi 1,3 và chạy 3,0.
3. **Dữ liệu sinh thái:** một hang mới `vuon_da` (Vườn Đá Hổ Phách) trong `CaveEcologySeed` với Khỉ Hang là kẻ săn (`Floor` 1, sức chứa 3, như kẻ săn các hang khác), cùng con mồi và Chúa Hang của hang đó do các bạn và chủ dự án chọn. Thêm ánh xạ chủ đề khoang `AmberPebbleGarden` sang `vuon_da` trong `MazeEcologyLinks.CaveOf`, và `vuon_da` vào tầng 1 (Nông) trong `CaveTier`. Chưa làm hết các bước này thì `CaveTier.RoleOf` coi Khỉ Hang là con mồi (sai hệ số kích cỡ).
4. **Bảng rơi gene** (`DropTableSO`) cho Khỉ Hang: kỹ năng nên là **Ném Đá** (kỹ năng ném vật, một chiêu bắn ra) khi người chơi nuốt hoặc Giải Mã; chủ dự án quyết kỹ năng cụ thể. Trigger hoạt hình xem bảng clip.
5. **Đặt vào khoang:** `CaveWave1ScenePopulator` hiện chỉ có các hàm cho vài chủ đề; thêm hàm cho Vườn Đá Hổ Phách theo mẫu hiện có (một tổ và một bộ tạo bầy mỗi loài, 3 tổ nếu khoang rộng 75 m).

## Phần lõi làm sẵn
- Ném đá theo cung (viên đá bay 0,9 giây tới chỗ người chơi đứng lúc ném), cắn rồi chạy lùi, nhảy lên bục cao 1,5 đến 3 m ở phía xa người chơi để ném từ trên (tự tìm bục bằng tia từ trên xuống), hú gọi đàn. Không cần điểm leo đặt tay.
- Nếu muốn khỉ leo cây hoặc vách đứng thật, cần các bạn đặt điểm leo và báo lại để lõi nối vào.
