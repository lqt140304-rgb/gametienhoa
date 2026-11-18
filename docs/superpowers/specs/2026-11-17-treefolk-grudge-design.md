# Người cây Chân Rễ và hệ thống ghi thù (thiết kế, đợt 1 của "nền văn minh")

## Ý định và lý do
Người chơi muốn thế giới **sống động**: mỗi khu có một loài thông minh với nền văn minh riêng (loài mới, không phải D/C/B — những con đó vẫn là quái hoang dã ở xa thành phố, quanh hồ). Thành phố là nơi **hé lộ hướng phát triển ẩn** và là **một con đường phát triển** (gia nhập thành phố; giao dịch, trao đổi thông tin, nâng cấp trang bị, thi đấu). NPC không nhất thiết giống người: hình dạng theo môi trường sống. Hệ class do chủ dự án tự phát triển — thiết kế chỉ chừa chỗ gắn.

Khi bị tấn công, cả tộc **ghi thù**: tin lan từ con này sang con khác, cả tộc quay ra thù và đóng cửa thành. Có thể **chuộc lỗi**.

## Lộ trình cả dự án (đã thống nhất; mỗi đợt một thiết kế riêng)
1. **Đợt này:** ghi thù + một nhóm người cây tạm ở Chân Rễ để thử.
2. Thành phố Chân Rễ đẹp: nhà hốc rễ/hốc đá, tượng và phù điêu chạm khắc, sảnh (cảm hứng người lùn).
3. Giao dịch và trao đổi thông tin. 4. Nâng cấp trang bị. 5. Đấu trường. 6. Nhân rộng các khu khác (gợi ý: 03 loài đào mùn — nhà bùn/lá mục; 04 loài nấm — nhà thân/mũ nấm; 05 loài ven nước — nhà đá xếp; 01 để trống; 06 vùng hoang hoặc pháo đài).

## Người cây (đã chốt)
- Cao **khoảng 1–1,5 m** (slime 0,6 m theo concept, ~1 m trong game). Thân gỗ, rễ làm chân, rêu/nấm trên người.
- Cảm hứng **người lùn**: sống theo dòng tộc, sảnh trong lòng hang, thợ chạm khắc và chế tác, trọng danh dự, **thù rất dai**.
- Đề xuất của tôi (chưa bị phản đối): "lò rèn" là **lò ủ nhựa cây/hổ phách**, vật liệu là **gỗ hóa đá, hổ phách, đá vôi**.
- Nhà: **hốc trong rễ cây và hốc trong vách đá**, có vật chạm khắc trang trí (đợt 2).

## Đợt này làm gì
### Người cây tạm (để có ai mà đánh, mà ghi thù)
- **10–14 người cây** ở trong và quanh hang rễ khu 02: dân thường đi lại giữa vài điểm (hốc nhà tạm, sảnh, cửa hang), **2–3 lính gác** đứng/đi tuần ở cửa rộng và khe hẹp.
- Hình dạng tạm dựng bằng code (thân trụ gỗ + rễ chân + tán rêu, như các mesh rễ/nấm hiện có), cao 1–1,5 m, có máu, nhận sát thương như các sinh vật khác.
- Chừa chỗ **class** (một trường văn bản/ID trên mỗi người cây, chưa có tác dụng).

### Mức thiện cảm (đã chốt)
- Mỗi **tộc** nhớ **mỗi người chơi** bằng một số **−100 … +100**, ban đầu **0**.
- Các mức: **Thù** (≤ −50): lính gác tấn công khi thấy, dân bỏ chạy, **cửa và khe hang bị chặn** (rễ mọc bít); **Lạnh nhạt** (−50 … −10): dân tránh xa, lính bám theo canh chừng, không đánh; **Bình thường** (−10 … +30); **Thân thiện** (+30 … +70); **Thành viên** (≥ +70, dùng ở các đợt sau).
- Trừ điểm: đánh trúng một người cây **−15**, giết **−60** (đề xuất của tôi; dễ chỉnh).
- **Thù rất dai:** không tự nguôi theo thời gian.
- **Co-op:** mỗi người chơi một mức riêng; người đánh bị trừ đủ, đồng đội đứng gần (≤ 15 m) bị trừ **1/4** (ghét lây). Máy chủ quyết định, gửi kết quả cho các máy khác bằng named message như các hệ thống co-op hiện có.
- Quái hoang D/C/B không có thiện cảm.

### Tin lan từ con này sang con khác (đã chọn)
- Ai **bị đánh** và ai **thấy tận mắt** (trong ~20 m, có tầm nhìn) biết ngay **ai** đã đánh.
- Một con đang biết tin **chạy về phía đông người** (sảnh/cửa hang); gặp con chưa biết trong **~4 m** thì truyền tin (mất ~1 giây). Con mới biết cũng đi truyền tiếp. Cả thành phố biết trong khoảng **1–2 phút** tùy độ đông.
- **Xác người cây** là bằng chứng: con nào đi ngang trong ~6 m cũng biết (giết sạch nhân chứng không giấu được).
- Người chơi **thấy** tin lan: con báo tin có biểu hiện hốt hoảng (chạy nhanh, dấu "!" hoặc rung lá); có thể chặn đường kẻ báo tin.
- Mức thiện cảm của tộc với một người chơi chỉ áp dụng cho **những con đã biết**; con chưa biết vẫn đối xử theo mức cũ. Khi người chơi đi khuất ~2 phút, mọi con đều đã biết (không có lỗ hổng vĩnh viễn).

### Chuộc lỗi (đã chốt: có)
- **Đá tạ lỗi** chạm khắc ngoài cửa hang rộng. Người chơi đặt **đồ ăn quý** lên đó (dùng các loại `FoodKind` hiện có; mỗi loại có giá trị) → thiện cảm tăng, **tối đa đến "Lạnh nhạt" (−10)**. Lên cao hơn để dành cho làm việc cho tộc / đấu trường (các đợt sau).
- Khi đang **Thù**, lính gác vẫn tấn công nếu thấy bạn; đá tạ lỗi đặt **ngoài tầm** lính gác ở cửa (~25 m) để chuộc lỗi được.

## Không làm (đợt này)
Nhà cửa đẹp, tượng khắc, giao dịch, thông tin, nâng cấp, đấu trường, gia nhập, các tộc khác, class thật.

## Ràng buộc
Unity 6.6 URP, C# 9, Netcode for GameObjects 2.x (không NetworkObject cho sinh vật; đồng bộ qua `WorldSync` named messages như Prey/Predator). Sát thương hiện được gọi thẳng trên `Prey`/`Predator` (`SpikeProjectile`, `WorldSync`, `GodMenu`) → cần một giao diện chung (ví dụ `IDamageable`) để người cây nhận đòn từ mọi nguồn. Không có Unity trong môi trường làm việc: chủ dự án kiểm bằng ảnh/Console.

## Các cách đã cân nhắc
1. **Tin lan con → con (đã chọn):** sống động, thấy được, ngăn được; cần hành vi chạy báo tin.
2. Cả tộc biết sau một thời gian cố định: dễ, nhưng không thấy, không ngăn được.

## Phần để người làm quyết
Kiểu AI di chuyển (dùng lại cách của Prey/Predator), biểu hiện hốt hoảng, mesh người cây tạm, giá trị từng loại đồ ăn ở đá tạ lỗi, cách bít cửa hang khi Thù, cách lưu thiện cảm (chỉ trong phiên chơi ở đợt này).
