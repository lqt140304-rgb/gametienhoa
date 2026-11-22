# Hợp đồng phòng trùm (cho phiên dựng map)

Bản đầy đủ và lý do: `docs/BOSS_ROOMS_BRIEF.md`. Đây là danh sách ngắn các tên mà code tìm trong phòng. Gắn script **`BossRoom`** (`Assets/_Project/Scripts/World/BossRoom.cs`) vào GameObject gốc của phòng; nó tìm các con theo tên (thiếu thì in một cảnh báo trong Console và bỏ qua).

| Tên con của phòng | Loại | Việc |
|---|---|---|
| `Boss_Wait` | rỗng | chỗ trùm nằm chờ; **đặt trùm (prefab của loài) gần chỗ này (trong 8 m)**: `BossRoom` tự tắt trùm cho đến khi kích hoạt (hoặc kéo trùm vào ô `Boss` của `BossRoom`) |
| `Activate_Zone` | collider trigger | phủ phần trong phòng (trừ ~6 m sát cửa): khi **mọi người chơi còn sống đều ở trong** thì đếm ngược 2 s, cửa đóng, 2 s sau trùm thức dậy |
| `Room_Bounds` | collider trigger | bao cả phòng (dùng sau này để ngắt giao tranh) |
| `Door_Steel` | cánh cửa thép, **có collider chặn** | code trượt nó giữa hai vị trí dưới đây (đóng trong 1,5 s) |
| `Door_Open_Pos`, `Door_Closed_Pos` | rỗng | vị trí mở và vị trí đóng của `Door_Steel`; để `Door_Steel` ban đầu ở vị trí mở |
| `Axe_Anchor` | rỗng | **chỉ phòng Golem**: chỗ cắm rìu (y = mặt sàn); đặt prefab `Props/GolemAxe_Planted` (trong `Assets/_Project/Resources/Props/`, tạo bằng menu **Evolve → Build Golem Axe Prefabs**) làm **con của `Axe_Anchor`** để code thay nó khi Golem rút rìu |
| `Player_Entry`, `Door_Mouth`, `Prop_*` | rỗng / có collider | chỗ người chơi vào, chỗ cửa, đồ trong phòng: chưa có hành vi |

**Khi chơi:** trùm chết thì cửa mở; mọi người chơi chết thì cửa mở và trùm quay về chờ (đầy máu). Một mình thì chỉ cần bạn bước vào `Activate_Zone`.
