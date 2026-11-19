# Hang Khởi Sinh của Slime: bản đồ mới sạch (thiết kế ngắn)

Ngày: 2026-11-19. Trạng thái: **đã cài, chưa chạy thử trong Unity** (môi trường viết code không có Unity). Chủ dự án yêu cầu: làm sạch, **quên môi trường dự án cũ**, làm map mới; bản đồ chỉ có **một nơi để Slime sinh ra là hồ nước**; phía trên là **lỗ trời không cần quá to**; **hồ sâu hơn một chút**; quanh hồ là **thảm thực vật nhỏ cùng các quặng đá**; lấy ý tưởng từ anime "chuyển sinh thành slime" (Slime thức dậy trong hang tối có hồ, quặng phát sáng).

## Ý định
Bắt đầu một ván trong một hang nhỏ, ngay bên bờ hồ. Hang tối, chỉ có một cột nắng rơi qua lỗ trời xuống mặt hồ và vài cụm quặng xanh phát sáng; quanh hồ là thảm cỏ nhỏ, nấm phát sáng và đá rêu.

## Đã chốt (theo chủ dự án)
- **Một nơi duy nhất**: cái hồ; Slime xuất hiện ở bờ hồ. Không có các khu Bãi Mùn Rễ, Vườn Bào Tử, Rìa Săn Mồi, Khe Thủy Mạch, Chân Rễ, thành phố người cây.
- **Hồ sâu 8 m** ở giữa (trước đó vài mét), bờ nông và thoải để lội được.
- **Lỗ trời không to** (bán kính 6,5 m) trên hồ.
- **Thảm cây nhỏ và quặng đá quanh hồ**.
- Kích thước nhỏ: hang rộng khoảng 90 m, đất vuông 128 m (chủ dự án nói "không quá rộng", trước đó nêu 200 m).

## Quyết định của Claude (chủ dự án sửa được)
- Hang có **mái kín** với một lỗ trời; nắng chiếu qua lỗ rơi **đúng vào giữa hồ** (vị trí lỗ được tính từ góc nắng: `CavernLayout.HoleCenter`).
- Hang **tối**: ánh sáng nền rất thấp, sương mù đen xanh; chỉ cột nắng, cụm quặng phát sáng (tối đa 8 đèn điểm) và nấm phát sáng.
- **Quặng**: tối đa 20 cụm tinh thể xanh phát sáng đứng trên đá, cách nhau ít nhất 10 m; **đá** rêu rải rác (khoảng 190 hòn); **cỏ** khoảng 2.000 nhúm; **nấm phát sáng** tối đa 160 cây.
- Nền: bùn quanh nước, rêu trên đất, đá trên vách; vách hang cao 30 m, dốc.
- Chưa có sinh vật, thức ăn, người cây, kẻ săn: chỉ có Slime, hồ, thảm cây, quặng. Chủ dự án thêm sau.

## Đã cài (dự án này)
- `Scripts/World/CavernLayout.cs`: toàn bộ bố cục thuần (hồ, độ sâu, độ cao nền, vách, mái, lỗ trời, vị trí xuất hiện, mật độ cây, nấm, quặng, đá, rải điểm có seed).
- `Scripts/World/WaterField.cs` + `CavernWater.cs`: cho cảnh khai báo nước của riêng nó; `SlimeController`, `UnderwaterTint`, `SwimRipple` đọc qua đây (mặc định vẫn là bảng cũ, nên các cảnh cũ không đổi).
- `Editor/CavernBuilder.cs`: menu **Evolve → Build Slime Cavern (new map)** dựng scene `Assets/_Project/Scenes/Slime_Cavern.unity` (địa hình 128 m với 3 lớp nền, mặt nước, mái có lỗ, trời và nắng, cột sáng, cây, nấm, đá, quặng, Slime).
- Test: `CavernLayoutTests` (hồ sâu, bờ thoải, vách cao, vị trí xuất hiện khô, nắng rơi vào hồ, mái, cây quanh hồ, quặng hiếm và cách nhau, rải điểm có seed, `WaterField`).

## Chưa làm, cần biết
- **Các bước dựng của map cũ** (`Rebuild Lake (all steps)`, `Dress Lake: ...`) **không dùng** cho map này; chúng vẫn còn trong dự án và dựa vào bản đồ cũ. Có thể xóa sau khi chủ dự án chốt bỏ map cũ.
- Thị giác Slime không mắt (`MonochromeVision`) vẫn làm tối cảnh khi chưa có mắt.
- Chỉ có Slime: chưa có thức ăn, quái, hệ sinh thái. Màn chọn Lõi vẫn chạy khi vào cảnh.
- Ánh sáng và mặt nước là bản đầu: có thể cần chỉnh độ sáng cột nắng, màu nước, mật độ cây.
