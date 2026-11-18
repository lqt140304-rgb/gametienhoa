# Thiết kế: Thảm thực vật, cây cối và ánh sáng chất lượng cao (hướng A: tài sản quét thật + Blender kỳ ảo)

> Ngày: 2026-11-18 · Nhãn: **[ĐÃ CHỐT]** chủ dự án quyết; **[ĐỀ XUẤT]** Claude đề xuất (chủ dự án sửa được); **[BUILDER]** người làm tự quyết.

## 1. Ý định và lý do

Chủ dự án thấy map **xấu, chưa chân thật, trống và tối**. Đợt này **[ĐÃ CHỐT]**:

- **Giữ nguyên bố cục** (6 khu, hồ, suối, đường mòn, cửa bắc; `ZoneLayout`). Làm lại **thảm thực vật và cây cối** cho môi trường bớt trống, và **thêm nguồn sáng**.
- **Chất lượng hình ảnh tầm The Witcher 3**. Đồ vật nói chung tối thiểu phải chi tiết như ảnh tham khảo 1 (đồ nội thất hoạt hình gọn, vật liệu đọc được). Môi trường có chiều sâu như ảnh tham khảo 2 (khối hữu cơ điêu khắc, đá xốp thủng lỗ, cột cong, các lớp tiền, trung, hậu cảnh, nền lạnh xanh tím, điểm sáng ấm).
- Trang trí **chăm chút, có logic theo từng khu, từng đoạn ánh sáng**: vật nằm ở đâu phải có lý do (ẩm, khô, gần nước, có sáng hay không, gần rễ, gỗ mục), không rải đều.
- **Loài cây**: cây có thật sống trong hang, loài phát sáng thật và kỳ ảo, kèm vài yếu tố trang trí kỳ ảo.
- **Độ sáng**: khi slime đã có mắt, **thấy rõ cả khung cảnh trong khoảng 40 đến 60 m**, nền tối xanh lạnh nhưng mọi khối đều đọc được, điểm sáng ấm làm nhấn và dẫn đường (như ảnh 2). Chưa có mắt vẫn tối như hiện nay.
- **Hướng A**: dùng **tài sản quét từ thật, miễn phí, giấy phép CC0** (Poly Haven; ambientCG cho texture) cho cây, dương xỉ, rêu, đá, gỗ, đất; **Blender** cho phần kỳ ảo không có sẵn, nướng ra bộ texture đầy đủ để khớp chất lượng. Chủ dự án đồng ý xét lại quyết định cũ "không dùng tài sản ngoài" (spec `2026-11-18-blender-props-kit-design.md`) cho riêng việc này.

## 2. Mục tiêu, không mục tiêu, điều phải tránh

**Mục tiêu**
- Ba tầng thực vật có logic: tầng đất (đã có: cỏ, rêu, nấm nhỏ, cụm sáng), **tầng bụi 1 đến 3 m**, **tầng cây 5 đến 15 m**, lấp khoảng trống giữa đất và vòm.
- Bộ loài (danh sách đầu, mục 5.2) với mô hình và texture PBR đầy đủ (màu, pháp tuyến, độ nhám, AO; lá bằng tấm ảnh có alpha), LOD, lung lay theo gió.
- Bản đồ **điều kiện sống** cho mỗi điểm (ẩm, gần nước, ánh sáng từ lỗ trần, gần rễ hoặc gỗ mục, khu) và **luật mọc** của từng loài; cụm, dải, tầng, có chỗ trống.
- **Cảnh dựng có chủ đích** ở vài chỗ quan trọng: dưới mỗi lỗ sáng, cổng vào từng khu, bờ hồ.
- Ánh sáng: nguồn sáng chủ yếu là thực vật và trang trí phát quang, vũng sáng dưới lỗ trần, tông màu riêng mỗi khu; nâng ánh sáng chung tới mức "thấy rõ 40 đến 60 m".

**Không mục tiêu**
- Không đổi bố cục, địa hình, nước, bơi, `GroundHeight`, đồng bộ co-op, luật sinh tồn.
- Không làm lại nhân vật, sinh vật, cơ quan, làng người cây (đồ do thợ trong làng làm là việc sau).
- Không dựng lại toàn bộ nền đất bằng Blender trong đợt này (đã bàn: hang khoét, mỏm nhô cần mesh điêu khắc; để đợt sau).

**Điều phải tránh (thất bại dù chạy được)**
- Lẫn phong cách: cây quét thật cạnh khối kỳ ảo bóng nhựa; màu và độ nhám không khớp.
- Mất FPS nặng (mục tiêu: giảm không quá 25% so với hiện nay trên máy chủ dự án, đo bằng `PerfOverlay`).
- Cây chắn đường mòn, cửa nơi trú, mọc trong nước sâu, lơ lửng hay chìm.
- Rải ngẫu nhiên đều tay, không có lý do theo khu.
- Repo phình to (Git LFS bị chặn với phiên agent).

## 3. Ràng buộc

- Unity 6.6, **URP**; agent **không có Unity**, chỉ kiểm bằng ảnh Blender và ảnh chủ dự án chụp.
- **Mạng của agent**: đã thêm `api.polyhaven.com`, `dl.polyhaven.org`, `cdn.polyhaven.com` vào môi trường, nhưng lệnh tải danh mục bị bộ duyệt quyền tự động chặn. Cần chủ dự án cho phép (hoặc chủ dự án tự chọn tài sản) trước bước chọn loài.
- **Không đẩy file nhị phân lớn lên git** (LFS bị chặn; các bản `.b64` chỉ hợp với file nhỏ). Tài sản quét nặng hàng chục đến hàng trăm MB, nên **tải về trên máy chủ dự án** bằng một menu Unity đọc danh sách tài sản (manifest) trong repo; thư mục tải về được `.gitignore`.
- Giấy phép CC0: ghi nguồn trong `docs/art/CREDITS.md` (không bắt buộc nhưng nên).
- Phối hợp với phiên trang trí (các file `Editor/Lake*.cs`); bố cục và lớp nền do phiên kia.

## 4. Các hướng đã cân nhắc

| Hướng | Nội dung | Được | Mất |
|---|---|---|---|
| **A (chọn)** | Tài sản quét CC0 (Poly Haven, ambientCG) + Blender kỳ ảo nướng texture PBR + luật mọc theo môi trường + cảnh dựng có chủ đích | gần Witcher nhất mà miễn phí; có logic, làm lại được | phải chọn lọc cho đồng phong cách; nặng máy hơn; tải tài sản trên máy chủ dự án |
| B | Gói tài sản trả phí Unity Asset Store | đẹp, đồng bộ, nhanh | tốn tiền; chủ dự án tự mua và nhập |
| C | Giữ dựng bằng code, nâng cấp nướng texture, lá tấm ảnh | không phụ thuộc bên ngoài | chỉ mức indie khá, xa Witcher |

Trước đó đã loại: dựng tay từng khu trong Blender (agent không nhìn được Unity để canh với địa hình, chậm, đổi map phải làm lại); chỉ dùng cây có sẵn của Unity Terrain (không đạt chất lượng).

## 5. Thiết kế

### 5.1. Dây chuyền tài sản

- `tools/assets/flora_manifest.json` **[ĐỀ XUẤT]**: mỗi mục là một tài sản (id Poly Haven hoặc ambientCG, loại model hoặc texture, độ phân giải 1k hoặc 2k, vai trò trong game, loài). File nhỏ, trong git.
- Menu **Evolve → Install Scanned Assets** (Editor, chạy trên máy chủ dự án): đọc manifest, tải qua API của Poly Haven (định dạng FBX hoặc glTF cho model, PNG cho texture), lưu vào `Assets/_Project/ThirdParty/Scanned/` (gitignore), dựng vật liệu URP Lit (màu, pháp tuyến, độ nhám, AO; lá dùng alpha clip, hai mặt), sinh LOD nếu thiếu, đặt cờ tĩnh. Chạy lại an toàn (bỏ qua file đã có).
- Phần kỳ ảo dựng trong Blender (`tools/blender/`), **nướng texture** (màu, pháp tuyến từ chi tiết cao, độ nhám, phát sáng) ra PNG nhỏ, đi kèm FBX theo cách `.b64` hiện có, hoặc nếu quá lớn thì cũng qua bước cài trên máy.
- Shader lá cây: dùng chung hướng `Evolve/GrassBlade` (gió, rẽ khi đi qua) mở rộng cho lá có texture alpha, hoặc URP Lit với gió đỉnh **[BUILDER]**.

### 5.2. Danh sách loài đầu tiên **[ĐỀ XUẤT, chốt sau khi xem danh mục]**

| Tầng | Loài | Nguồn | Điều kiện sống |
|---|---|---|---|
| bụi | dương xỉ hang (lưỡi nai, tóc thần vệ nữ), quyển bá | quét | ẩm, có chút sáng, gần vách |
| bụi | rêu, rêu tản, địa y trên đá và gỗ | quét (texture) | mặt đá ướt, gỗ mục, khuất sáng |
| bụi | nấm kệ, nấm mật ong cụm | quét hoặc Blender | quanh gỗ mục, gốc rễ |
| bụi | bụi lá, dây leo, cây non | quét | chỉ dưới lỗ sáng |
| cao | rễ đa, rễ si buông thành mành, cuộn quanh đá | Blender (đã có cơ sở) + texture vỏ quét | gần vòm, rìa |
| cao | cây nấm phát quang thân cao, tán xòe (kỳ ảo) | Blender | khu 03, 04, ven đường |
| sáng | nấm Mycena xanh lục, gỗ mục phát lân quang | Blender + texture quét | gỗ mục, khu 02, 03 |
| sáng | mành tơ sáng của ấu trùng đom đóm hang | Blender | treo dưới trần, chỗ lặng gió |
| kỳ ảo | tinh thể khổng lồ, hoa đèn lồng khổng lồ, sứa nấm trôi, vòm rễ có ký hiệu cổ, đá xốp thủng lỗ có rêu sáng, hoa súng phát quang và đom đóm sát nước | Blender + texture quét | theo khu (mục 5.4) |
| đá, gỗ | đá vôi, đá sông, khúc gỗ mục, gốc cây | quét | theo khu |

### 5.3. Điều kiện sống và luật mọc

- `World/Habitat` (thuần, có test) **[ĐỀ XUẤT]**: tại (x, z) trả về độ ẩm (gần nước, vũng, suối), ánh sáng (gần lỗ trần, khe sáng; trong bóng sườn rễ), độ gần rễ hoặc gỗ mục, độ dốc, khu đang ảnh hưởng.
- Mỗi loài một hồ sơ ưa và ghét (giống cách `PlantCommunities` đã làm cho tầng đất), cộng kiểu mọc: cụm, dải theo bờ, tầng quanh vật chủ (rêu trên đá, nấm quanh gỗ). Mật độ có trần để giữ FPS.
- Thứ tự đặt: cảnh dựng có chủ đích trước, cây cao, bụi, rồi tầng đất (tầng đất hiện có giữ, chỉ điều chỉnh để ăn khớp).

### 5.4. Tính cách từng khu **[ĐỀ XUẤT]**

- **01 Vùng Khởi Sinh (bờ nam hồ):** dải sậy, súng phát quang, đom đóm sát nước, dương xỉ ở bờ ẩm, tông ngọc.
- **02 Lõi Rễ Cổ (Đại sảnh):** rễ đa buông mành, gỗ mục phát lân quang, vòm rễ có ký hiệu cổ gần làng, tông hổ phách.
- **03 Bãi Mùn Rễ:** gỗ mục, nấm kệ và nấm mật ong, cây nấm phát quang, hoa đèn lồng, tông vàng lục.
- **04 Vườn Bào Tử:** cây nấm cao, sứa nấm trôi, bào tử lơ lửng, tinh thể nhỏ, tông tím.
- **05 Khe Thủy Mạch:** tinh thể khổng lồ trên vách, rêu ướt, dương xỉ tóc thần vệ nữ bên thác, tông xanh băng.
- **06 Rìa Săn Mồi:** đá xốp thủng lỗ có rêu sáng, ít cây, dấu vết thú, tông đỏ dịu.
- Dưới mỗi lỗ sáng: cây non và bụi lá thật vươn lên, vũng sáng ấm, bụi bay trong chùm sáng.

### 5.5. Ánh sáng

- Tăng mức sáng chung tới "thấy rõ 40 đến 60 m" khi có mắt: điều chỉnh ambient theo khu (`ZoneAtmosphereMath`), sương, phơi sáng hậu xử lý; giữ nguyên cơ chế mù của `MonochromeVision`.
- Nguồn sáng thật gắn với thực vật phát quang (đèn nhỏ có giới hạn, `LightByVision`), vũng sáng dưới lỗ trần; vật liệu phát sáng tự thân cho phần lớn điểm sáng, chỉ một phần có đèn thật để giữ FPS.
- Phân bổ số đèn và mức sáng cuối cùng chốt sau khi chủ dự án xem ảnh **[BUILDER]**.

### 5.6. Kiểm chứng

- Test EditMode cho `Habitat` và luật mọc (không mọc trong nước sâu, trên đường mòn, cửa nơi trú; đúng loài theo điều kiện; trần mật độ).
- Ảnh Blender cho phần kỳ ảo; ảnh trong Unity do chủ dự án chụp mỗi đợt.
- Đo FPS bằng `PerfOverlay` sau mỗi đợt.

## 6. Nhịp làm **[ĐỀ XUẤT]**

| Đợt | Nội dung |
|---|---|
| 0 | Chủ dự án cho phép tải danh mục Poly Haven (hoặc tự chọn); chốt danh sách loài và manifest |
| 1 | Dây chuyền cài tài sản trên máy + vật liệu URP + một loài mẫu mỗi tầng; chủ dự án duyệt chất lượng và FPS |
| 2 | `Habitat`, luật mọc, tầng bụi và tầng cây cho khu 01 và 03 |
| 3 | Phần kỳ ảo Blender nướng texture; khu 02, 04 |
| 4 | Khu 05, 06, cảnh dựng dưới lỗ sáng và cổng khu |
| 5 | Ánh sáng tổng thể, tinh chỉnh theo ảnh |

Mỗi đợt một PR.

## 7. Để lại cho người làm

- Chọn tài sản cụ thể trong danh mục, độ phân giải texture (1k hay 2k), số LOD.
- Shader lá: mở rộng `GrassBlade` hay URP Lit có gió đỉnh.
- Ngưỡng mật độ, số đèn, mức sáng cuối sau khi đo FPS và xem ảnh.

## 8. Chủ dự án đã chốt sau vòng kiểm (2026-11-18)

- **Phong cách: bán thực [ĐÃ CHỐT].** Tài sản quét được chỉnh màu và độ nhám (bớt bão hòa, dịch tông về xanh lạnh và ấm theo khu, độ nhám đồng đều hơn) để hơi vẽ tay, gần ảnh tham khảo 2; phần kỳ ảo Blender nướng texture cùng quy tắc màu và độ nhám đó. Không để ảnh chụp thô cạnh khối bóng nhựa.
- **Chọn tài sản: Claude tự chọn [ĐÃ CHỐT]** từ danh mục Poly Haven và ambientCG, rồi đưa danh sách kèm ảnh xem trước để chủ dự án duyệt.
- **Dung lượng tải trên máy: 1 đến 3 GB được chấp nhận [ĐÃ CHỐT].**
- **Máy mục tiêu [ĐÃ CHỐT]:** RTX 3050 6 GB, Full HD, hiện khoảng 150 FPS. Ngân sách: không dưới khoảng 90 FPS ở cùng chỗ đo (giảm tối đa khoảng 40%), cây cao dùng LOD và ảnh thay thế ở xa, đèn thật có giới hạn.

### Quyết định của Claude (chủ dự án kiểm)

- **Đặt cây bằng menu Editor** như các bước `Dress Lake` hiện có, lưu vào scene; co-op giống nhau vì dùng cùng scene, không sinh lúc chạy.
- **Dự phòng khi thiếu tài sản:** menu đặt cây báo thiếu và bỏ qua loài đó; map vẫn mở và chạy với lớp hiện có. Người cùng làm hoặc bạn co-op chạy menu cài tài sản là có.
- **Phân việc:** chỉ thêm file mới (`Editor/LakeScannedFlora*.cs`, `World/Habitat.cs`, manifest), không sửa lớp nền của phiên kia; chỉnh ánh sáng qua `ZoneAtmosphereMath` và các bước ánh sáng của phiên này.
- **Dữ liệu điều kiện sống** lấy từ dữ liệu có sẵn trong `ZoneLayout` (đường mòn, nước, lỗ sáng, hang) và vị trí rễ, gỗ mục trong scene.
- **Kiểm độ sáng:** 2 góc máy cố định mỗi khu để chụp so sánh trước và sau.
