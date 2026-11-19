# Hố lộ thiên: Hồ Khởi Sinh dưới bầu trời mở (thiết kế ngắn)

Ngày: 2026-11-19. Trạng thái: **đã cài, chưa chạy thử trong Unity** (môi trường viết code không có Unity). Chủ dự án bỏ qua phần hỏi đáp và yêu cầu dựng luôn; bản này ghi lại những gì đã chốt trong hội thoại và những quyết định của Claude.

## Ý định
Nơi Slime khởi sinh không còn nằm dưới mái rễ trong hang. Nó là **một cái hố sâu lộ thiên**: người chơi ở đáy, bên hồ; phía trên là cả bầu trời với ánh nắng như ngoài đời; xung quanh miệng hố là thế giới thật (đồi, rừng, chân trời) chỉ nhìn thấy, không đi lên được. Hang tối trở thành **mê cung ngoằn ngoèo nhiều khoang** (dự án 2, làm bằng Blender, chưa làm).

## Đã chốt (theo lời chủ dự án)
- Thấy **cả bầu trời, ánh nắng, môi trường xung quanh như thế giới thật bên trên**; người chơi chỉ ở dưới.
- **Miệng hố cao hơn và dốc hơn.**
- Hang không còn là một hang to: **mê cung nhiều khoang ngoằn ngoèo, làm bằng Blender**.
- Chia khu: ngoài trời dưới nắng là Hồ, ven hồ, Bãi Mùn Rễ, Vườn Bào Tử, Rìa Săn Mồi; **Chân Rễ Cổ Thụ cùng Đại Sảnh và thành phố người cây chuyển vào mê cung** (khoang sâu nhất).
- Cách C: địa hình Unity làm nền hố và chân dốc, **mesh Blender phủ mặt vách** (các mảnh `Cliff_0..3` của người làm Blender), trời và nắng bằng code.
- Dừng làm vòm rễ cho khu hố; rễ, gân, trụ dùng lại trong mê cung. Mái cũ chỉ bị **ẩn**, không xóa.

## Đã cài trong lượt này (dự án 1)
- `TerrainShape`: vách rìa **55 m** (trước 20 m), bắt đầu từ 214 m (trước 200 m), `TerrainHeight` 80 m; dốc trung bình khoảng 57 độ, tối đa khoảng 66 độ.
- `Editor/LakeOpenSky.cs`, menu **Evolve → Dress Lake: Open Sky** (bước cuối của Rebuild): ẩn mái lưới rễ, tấm đất trần, mái vòm, rễ rủ, vòm rễ, tia sáng xiên; đặt bầu trời `LakeSky` (Skybox/Procedural), nắng ấm nghiêng 50 độ có bóng mềm (bóng vách đổ lên đáy hố), môi trường lấy từ trời; dựng **thế giới trên miệng hố** (`Surface World`: mặt đất đồi đến 650 m và khoảng 700 cây thông xa). **Evolve → Restore Lake Roof (undo)** trả lại mọi thứ.
- `World/SurfaceWorld.cs`: hàm độ cao của thế giới trên miệng hố (phẳng ở mép vách rồi nhấp nhô tới khoảng 53 m; cửa bắc kéo dài thành một hẻm).
- `ZoneAtmosphere`: khi scene có bầu trời `LakeSky` thì sương mù là màn trắng xanh nhạt tới 600 m (không mắt: đen và 80 m như cũ) và không ép ánh sáng nền kiểu hang.
- Test: `TerrainShapeTests` (vách 45 m trở lên, dốc trên 1,5 m mỗi mét, vừa heightmap), `SurfaceWorldTests`.

## Chưa làm và những điều cần biết
- **Phần Blender của vách:** chưa đặt các mảnh `Cliff_*` lên mặt vách; hiện vách là địa hình thuần (mịn). Cần công cụ đặt mảnh Blender (người làm Blender).
- **Thị giác của Slime không mắt:** `MonochromeVision` vẫn đưa nắng, ánh sáng nền và sương mù về đen khi chưa có mắt, nên khi chưa có mắt bầu trời và nắng không hiện. Có đổi luật này cho cảnh hố hay không là câu hỏi mở.
- **Nhân Quang Hợp** dựa vào tia sáng xiên (`ZoneLayout.LightShafts`); các tia này bị ẩn, nên Lõi này cần đổi thành "đứng dưới nắng, ngoài bóng vách".
- Tầm nhìn xa của camera (far clip) có thể cần tăng để thấy thế giới trên miệng hố (tới 650 m); chưa kiểm tra.
- Vòng ngày đêm: chưa có (nắng cố định); cây phát quang ban ngày kém nổi.
- Các bước dựng khác (Roots and Light, Dappled Sun...) vẫn chạy trong Rebuild rồi bị ẩn hoặc ghi đè bởi bước cuối. Có thể bỏ bớt sau.
- Mê cung hang (dự án 2): chưa thiết kế.
