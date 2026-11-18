# Thành phố người cây ở Chân Rễ Cổ Thụ (thiết kế, đợt 2 của "nền văn minh")

Tiếp nối `2026-11-17-treefolk-grudge-design.md`. Đợt 1 chỉ có 12 người cây tạm trong hang trống; đợt này dựng **thành phố hùng vĩ** để có nơi cho giao dịch, thông tin, nâng cấp, đấu trường (các đợt sau).

## Ý định
Người chơi thấy khu của người cây quá bé và trống ("chỉ có một cái chòi bé"). Muốn **hùng vĩ**, trải rộng cả khu 02 (lõi 70 × 60 m), lấy cảm hứng **người lùn**: sảnh khổng lồ trong lòng cây/hang, nhà khoét vào rễ và vách đá, tượng và phù điêu, nhiều tầng, đèn hổ phách.

## Bố cục (đề xuất của tôi, người chơi đã đồng ý hướng "trải rộng cả khu 02, hang là trái tim")
Tọa độ theo tâm khu 02 (-165,-15); đông = hướng ra hồ.
1. **Đại Sảnh (trong hang)**: mở rộng hang lên **58 × 44 m, cao 18 m** (cũ 38 × 30 × 12): 8 cột rễ khổng lồ chia sảnh thành gian giữa và hai hành lang; **bục chạm khắc** ở giữa (đường kính 8 m, 3 bậc) với **ngai gỗ hóa đá** của tộc trưởng; **lò ủ nhựa** (hổ phách phát sáng) ở cuối sảnh; đèn hổ phách treo trên cột; vòm đất + rễ như cũ.
2. **Khu phố vách rễ**: hai khối rễ/vách đá lớn phía bắc và nam hang, mỗi khối khoét **16–20 hốc nhà** thành **3 tầng**, cửa vòm tròn (cao 1,6 m, rộng 1 m), **cầu thang rễ** và hành lang đá nối các tầng, có ban công; hốc có đèn nhỏ, một nửa sáng.
3. **Phố thợ**: một vách đá phía tây, **6 xưởng** (hốc rộng 4 × 3 m) có đống gỗ hóa đá, hổ phách, **tượng dang dở**, bệ đục.
4. **Đường vào và cổng**: **đường đá phiến** rộng 6 m từ cửa hang ra đông, dài ~45 m, hai bên **hàng tượng** (12 pho, 2–3 m, khắc người cây cầm đèn) và cột đá khắc ở ngã ba; **cổng chào**: hai rễ khổng lồ cong thành vòm cao 10 m ở đầu đường.
5. **Vườn**: vườn rêu và nấm nhỏ có chủ ý (luống) hai bên đường; vài bể nước (hố nông ốp đá) có đèn.
6. **Dân**: 28 người cây (cũ 12): 4 lính gác (cổng chào ×2, cửa hang, khe hẹp), 24 dân chia nhóm (nhà, xưởng, sảnh, vườn); điểm sinh hoạt (home points) theo các khu trên; sảnh làm "hall" khi hốt hoảng. Giữ nguyên hệ ghi thù, rễ bít cửa (cửa hang và khe) và đá tạ lỗi (dời ra ngoài cổng chào, ≥ 25 m khỏi lính).

## Không làm (đợt này)
Giao dịch, trao đổi thông tin, nâng cấp, đấu trường, class thật, nội thất bên trong từng hốc (chỉ có ánh sáng).

## Yêu cầu kỹ thuật
- Mọi thứ dựng bằng code (Editor tool `Dress Lake: Treefolk City`, thay thế phần đặt người cây cũ của `Dress Lake: Landmarks` cho hang 02), idempotent, đặt theo độ cao địa hình thật.
- Hiệu năng: chia chunk 64 m + LOD như hiện có (xa chỉ còn sảnh, cổng, tượng lớn); mục tiêu FPS không tụt quá ~15% so với trước khi nhìn vào thành phố.
- Va chạm: sảnh, cột, tượng lớn có collider; hốc nhà chỉ là mặt lõm (slime chui vào được hốc nông ≥ 1,3 m, không vào được nhà).
- Slime (~1 m) đi được đường, sảnh, hành lang; khe hẹp phía bắc giữ 1,35 m.
- Mở rộng hang đụng `LakeLandmarks.RootCave` và `ZoneLayout.CaveRadii/CaveHeight`, tunnel 02 → 03 và test liên quan: cập nhật cùng lúc.

## Thứ tự làm (mỗi bước một PR)
1. **Sảnh**: mở rộng hang, cột, bục, ngai, lò, đèn. 2. **Khu phố vách rễ** (hốc nhà, tầng, thang, ban công). 3. **Phố thợ + tượng dang dở**. 4. **Đường, cổng chào, hàng tượng, vườn, bể**. 5. **Dân đông hơn (28), điểm sinh hoạt, dời đá tạ lỗi**; cập nhật tài liệu.

## Giao cho người làm
Hình khối chi tiết của tượng, cột, hốc (lathe/tube từ mesh sẵn có), số hốc chính xác, cách bố trí bậc thang.
