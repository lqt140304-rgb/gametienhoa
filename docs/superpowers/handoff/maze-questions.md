# Câu hỏi & ghi chú giao tiếp giữa bên làm Hang (Antigravity) và phần Lõi Game (Claude)

## 1. `Evolve.Traits.EncounterRewards` (Giai đoạn 1B — 3 hang đầu tiên)
- Trên nhánh hiện tại chưa có lớp `Evolve.Traits.EncounterRewards`.
- Bên làm hang đã đặt sẵn cầu nối `Evolve.Maze.EncounterRewardBridge` gọi qua reflection với chữ ký theo `2026-11-20-core-systems-brief.md`:
  - `EncounterRewards.Say(string line)`
  - `EncounterRewards.RefillStats(GameObject player)`
  - `EncounterRewards.UnlockColor(GameObject player, Color color)`
  - `EncounterRewards.GrantGene(GameObject player, int minRarity)`
- Khi phần lõi game gộp `EncounterRewards.cs` vào `Scripts/Traits/`, 3 kỳ ngộ (*Tia Nắng Đầu Tiên*, *Cầu Vồng Trong Thân*, *Hợp Âm Tinh Thể*) sẽ tự động gọi thẳng vào các hàm trên mà không cần sửa code.

## 2. Giao diện hỏi hang kề nhau cho Hệ Sinh Thái (`IMazeAdjacency`)
- Đã thêm `Evolve.Maze.IMazeAdjacency` (`GetAdjacentChamberIds(int chamberId)`, `AreAdjacent(int aChamberId, int bChamberId)`):
  - `MazeGraph` triển khai trực tiếp `IMazeAdjacency`.
  - `MazeWorldManager` có sẵn `Adjacency` (`IMazeAdjacency`) và `GetAdjacentChambers(int chamberId)`.

## 3. Prefab 12 loài sinh vật Đợt 1 & `drops`
- Đã tạo 12 prefab tại `Assets/_Project/Prefabs/Wildlife/*.prefab` và 12 `SpeciesSO` tại `Assets/_Project/ScriptableObjects/Ecology/Species/*.asset` cho **Hang Khởi Sinh**, **Vườn Quặng Cầu Vồng**, **Vực Nứt Tím**.
- Ô `drops` trên toàn bộ `Prey` / `Predator` đang được **để trống (`null`)** theo đúng brief để phần lõi game gán `DropTableSO`.
