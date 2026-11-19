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

## Thức ăn trong hang (đã cài; chưa chạy thử)
Yêu cầu của chủ dự án: **không phải loại cỏ nào cũng ăn được, và loại ăn được thì tốt theo nhiều cách khác nhau**.
- **Không ăn được (chỉ để nhìn):** thảm cỏ nhỏ phủ quanh hồ, đá thường, nấm xanh lam phát sáng nhỏ.
- **Ăn được: 12 loại, mỗi loại một công thức dinh dưỡng riêng** (dùng `FoodKind` và `FoodProfile` có sẵn) và một nơi mọc riêng (`CavernForageData`, `CavernForage`):

| Loại | Mọc ở | Cho (chính) |
|---|---|---|
| Lau Ven Hồ | mép nước | chất béo, đạm |
| Súng Nước | nước nông | vitamin |
| Rêu Sáng | mép hồ ẩm | vitamin |
| Cỏ Ngọt | dải ven hồ | đạm, chất béo |
| Dương Xỉ Đắng | nửa đất ngoài | sắt, vitamin |
| Quả Than Hồng | nửa đất ngoài | sắt |
| Nấm Béo | chân vách | chất béo |
| Túi Bào Tử | chân vách | canxi, vitamin |
| Thạch Anh Sáng (quặng) | các cụm quặng | sắt, vitamin |
| Đá Vôi (quặng) | các cụm quặng | canxi |
| Quặng Sắt | các cụm quặng | sắt |
| Muối Đá (quặng) | các cụm quặng | canxi, ít sắt |

- Cả năm chất dinh dưỡng đều là chất chính của ít nhất một loại. Khoáng chất cho nhiều hơn mỗi lần ăn (24), hấp thụ chậm hơn, và 10 phút mới mọc lại; cây cho ít hơn (8 đến 18), hấp thụ nhanh, mọc lại sau 2 phút rưỡi.
- Khoảng 130 cái ăn được, cách nhau ít nhất 2,2 m, dưới giới hạn đồng bộ co-op (1000). Menu **Evolve → Dress Cavern: Forage** chạy lại được riêng.
- **Mô hình làm bằng Blender** (theo yêu cầu): 9 loại dùng `Forage_*` có sẵn (`build_forage.py`), 3 loại mới (`Forage_MossClump`, `Forage_EmberBerry`, `Forage_SporePod`, `build_cavern_forage.py`) và 6 tảng đá `Rock_0..5` dựng bằng Blender; phần phát sáng là object riêng có chữ `Glow`, mỗi loại có chất liệu phát sáng riêng của hang. Mô hình bằng code chỉ là dự phòng khi thiếu FBX. Thảm cỏ nhỏ (hàng nghìn nhúm) và nấm trang trí vẫn là mesh dựng bằng code, vì quá nhiều và quá nhỏ để làm từng cái trong Blender.
- **Làm trông thật hơn:** nước dùng shader nước của dự án (`Evolve/LakeWater`: màu theo độ sâu, sóng gợn, bọt ở mép, khúc xạ) qua `WaterSurface`; hậu xử lý kiểu phim (ACES, bloom nhẹ, vignette, hạt phim, tăng tương phản) và SSAO ở các khe; nền đất bốn lớp nhiễu 256 px kèm sỏi lấm tấm; bụi quả than hồng làm lại với lá thật (dải lá cong, thuôn).
- **Chi tiết hơn cho cây và nấm:** thảm cỏ nay là **cỏ 3D thật** (cùng hệ `Evolve/GrassBlade` của bản đồ cũ: lá cỏ thuôn cong, tối ở gốc sáng ở ngọn, đung đưa theo gió, rẽ ra khi Slime đi qua; trồng bằng terrain details, `CavernGrass`, mật độ `CavernLayout.GrassDensity`); nấm trang trí là bốn cụm Blender chi tiết (`CaveShroom_0..3`: chân phình, vòng váy, mũ gồ ghề cuộn mép, nếp mang, đốm; hai cụm có mũ phát sáng xanh ngọc và hổ phách); nấm béo ăn được dùng mô hình mới `Forage_FatCap`.
- Chưa làm: gợi ý nhìn thấy loại nào ăn được (người chơi phải thử), loại độc hại.

