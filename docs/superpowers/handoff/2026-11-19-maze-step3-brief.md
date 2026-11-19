# Giao việc: Mê cung khoang, hang trú, Chu Kỳ Lũ (bước 3)

Dành cho một phiên làm việc khác (Antigravity). Làm trên nhánh riêng, không đẩy thẳng lên nhánh của phiên chính.

## Bối cảnh
- Game Unity 6.6 URP, co-op slime "PROJECT EVOLVE". Chủ dự án nói tiếng Việt: **mọi chữ hiện trong game, comment tài liệu và tin nhắn báo cáo bằng tiếng Việt**.
- Đọc trước: `docs/superpowers/specs/2026-11-19-reincarnation-survival-design.md`, **mục 9** (mê cung khoang, hang trú, Chu Kỳ Lũ) và mục 16 (bước 3). Mục 6.1 chỉ để biết cơ thể slime thế nào; không liên quan việc này.
- Nhánh gốc: `claude/youthful-mendel-yw0yza`. Tạo nhánh của bạn từ đó: `git checkout -b maze-step3 origin/claude/youthful-mendel-yw0yza`.

## Việc cần làm (theo thứ tự, mỗi ý là một nhóm commit)
1. **Luật thuần của mê cung** (không dùng Unity): đồ thị 12–16 khoang chia 3 tầng sâu (Nông, Giữa, Sâu), mỗi khoang 2–3 hầm, luôn có đường từ khoang xuất phát đến khoang cuối; **xáo theo hạt giống** (cùng seed ra cùng mê cung); có lối giả (ngõ cụt). Test đầy đủ.
2. **Chu Kỳ Lũ** (luật thuần + nối vào thế giới): 12 phút một chu kỳ, cảnh báo 1 phút trước (tiếng ầm, nước rỉ), mực nước dâng ở tầng thấp, gờ cao an toàn. Bị lũ bắt ngoài chỗ cao thì mất khối và trôi sang khoang khác. Test cho thời gian và mực nước.
3. **Hang trú thật**: khoang nhỏ cao hơn mực lũ, lối vào hẹp (sinh vật lớn không vào được), có rêu ẩm. Thay **tổ tạm** `Editor/CavernLife.cs` (đang dùng `ShelterZone` + `ShelterSensor` để bật `SurvivalState.InShelter`; **giữ nguyên hai lớp này và cách chúng hoạt động**, chỉ đổi cách dựng).
4. **Kích thước hầm và chướng ngại**: khe nhỏ chỉ slime nhỏ lọt (đọc `SlimeBody.Mass` / `BodySize.ScaleForMass`); chướng ngại tự nhiên mở bằng kỹ năng (hầm ngập cần thở nước, vách cao cần bám vách...). Chỉ dựng **khung và điều kiện**; các kỹ năng đó chưa tồn tại, để chỗ gắn (interface) sẵn.
5. **Hoán đổi ẩn giữa hầm**: một scene, prefab khoang đặt xa nhau, chỉ khoang hiện tại và khoang kề được bật, dịch chuyển khi người chơi ở đoạn hầm khuất.
6. **Vách Hang Khởi Sinh**: miệng hầm nhiều cỡ, gờ đá trú lũ, khe nứt, một hang trú đầu tiên. Slime **vẫn xuất hiện ở bờ hồ** như `CavernLayout.SpawnPoint()` hiện tại.

## Ranh giới (rất quan trọng, để hai phiên không giẫm lên nhau)
- **Được sửa:** `Assets/_Project/Scripts/World/`, `Assets/_Project/Editor/Cavern*.cs`, thư mục mới của bạn (ví dụ `Scripts/Maze/`, `Editor/Maze*.cs`), test mới trong `Assets/_Project/Tests/EditMode/`.
- **Không đụng:** `Scripts/Traits/`, `Scripts/Swallow/`, `Scripts/Organs/`, `Scripts/Abilities/`, `Scripts/Survival/` (kể cả HUD), `Scripts/Player/`. Cần gì ở đó thì **ghi vào `docs/superpowers/handoff/maze-questions.md`** thay vì sửa.
- Phiên chính sẽ đang làm cảnh ra đời (Tiếng Vọng) ở các thư mục trên. Khi xong, báo để gộp.

## Cách kiểm
- Phần thuần (đồ thị, xáo, lũ) viết trong thư mục `Scripts/Maze/Pure/` (hoặc tương tự) và test trong `Tests/EditMode/.../Pure/`, rồi thêm vào `tools/cstest/cstest.csproj` để chạy bằng `tools/cstest/run.sh` (không cần mở Unity). Xem cách `Scripts/Swallow/Pure` đã làm.
- Mọi test EditMode phải xanh trong Unity Test Runner (hiện 462 test, đang xanh).
- Menu dựng (`Evolve/...`) phải **chạy lại được nhiều lần** và không làm hỏng scene.
- Console sạch (không lỗi đỏ) khi chạy Play.

## Báo cáo khi xong mỗi nhóm
Nói ngắn: đã làm gì, test nào, chưa làm gì, điều gì không chắc. **Không tự nói "xong" nếu chưa mở Unity kiểm.**
