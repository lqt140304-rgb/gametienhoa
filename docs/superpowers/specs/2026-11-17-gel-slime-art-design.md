# Thiết kế: Slime gel 3D (bước 1 của "map + nhân vật 3D")

> Ngày: 2026-11-17 · Trạng thái: **chờ chủ dự án duyệt**
> Nguồn: concept "Slime nguyên sinh, lõi sống trong màng gel" do chủ dự án gửi; `2026-11-17-underroot-survival-slice-design.md` mục 5.2, 6.5, 6.8.
> Nhãn: **[ĐÃ CHỐT]** chủ dự án quyết; **[ĐỀ XUẤT]** Claude đề xuất; **[BUILDER]** người làm tự quyết.

## 1. Ý định
Thay quả cầu và các khối primitive bằng slime có hình dáng và chất liệu như concept, để người chơi nhận ra nhân vật và thấy cơ quan "mọc" từ gel. Map 3D theo cùng phong cách là **bước 2**, brainstorm riêng sau khi bước 1 chơi được.

## 2. Phương án (đã chọn C)
- A. Tạo hết bằng code: nhanh, không cần phần mềm ngoài, nhưng kém model vẽ tay.
- B. Chủ dự án tự tạo model: đẹp nhất nhưng phụ thuộc người làm model.
- **C. [ĐÃ CHỐT] Kết hợp:** làm A để có bản chơi được; mỗi phần là prefab độc lập để sau thay bằng model thật mà không sửa code game.

## 3. Mục tiêu
- Slime nguyên sinh: thân vòm đa giác dẹt, viền lượn, không mặt/mắt/chân; màng gel trong mờ xanh rêu; lõi hổ phách phát sáng bên trong. [ĐÃ CHỐT từ concept; màu là đề xuất mỹ thuật, chưa chốt]
- Nảy theo concept: nén, kéo giãn khi nhảy, bè ra khi đáp (nâng cấp `AnimateVisual`, vẫn giữ thể tích).
- 3 cơ quan gel liền thân đại diện 3 hướng thích nghi: **Giác quan** (cuống mắt), **Vận động** (chân giả cong), **Phòng thủ** (gai + mảng giáp). Dùng cùng chất liệu gel với thân.
- Giữ nguyên ổ gắn (`OrganSocket`), `OrganSO.VisualPrefab`, logic cơ quan, co-op (`RemoteAvatar`), tầm nhìn đơn sắc.

## 4. Không làm / thất bại
- Không làm map, không đổi sinh vật săn/mồi (bước sau).
- Không viết shader HLSL tùy chỉnh trong bước này (không biên dịch được ở đây, rủi ro cao); dùng URP Lit trong mờ + emissive.
- Không đổi luật cơ quan, tier, EXP.
- **Thất bại dù chạy được:** slime không nhận ra là slime khi nhìn từ xa (quá trong hoặc quá tối trong `MonochromeVision`); cơ quan trông như vật gắn rời, không liền thân; tụt FPS khi 4–8 người.

## 5. Thiết kế
- **Mesh thân** (`Visuals/SlimeMeshBuilder`, C# thuần tạo `Mesh`): icosphere ~80–150 mặt, nén dẹt theo Y, nắn viền (vòng ngoài lượn nhẹ bằng nhiễu có seed cố định), flat shading (đỉnh không dùng chung). Tạo lúc dựng prefab và lưu thành asset mesh. [BUILDER: số mặt cụ thể]
- **Material gel** (`SlimeGel.mat`): URP Lit, Surface = Transparent, alpha ~0,45, smoothness cao, màu xanh rêu nhạt `(0.62,0.80,0.55)`, tắt ghi độ sâu. Hàm tạo material đặt cùng chỗ với `PrototypeSceneBuilder.Mat`.
- **Lõi** (`SlimeCore`): khối cầu nhỏ trong thân, màu hổ phách, emission; script nhỏ làm lõi đập nhẹ (nhịp theo máu: máu thấp đập nhanh/mờ). Vân mạch bên trong là **[BUILDER]**: thử vài quả cầu/ống mảnh xanh lá phát sáng nhạt, không làm texture.
- **Nảy**: `SlimeController.AnimateVisual` thêm pha "bè ra khi đáp" (squash âm ngắn) và căng theo vận tốc dọc khi đang nhảy; vẫn bảo toàn thể tích.
- **Cơ quan gel** (prefab trong `OrganSO.VisualPrefab`, cùng material gel, hơi đậm hơn thân): cuống mắt (capsule mảnh + cầu mắt đen/trắng, cầu mắt không gel), chân giả (4 capsule cong ra ngoài, đế bè), gai (cone trong mờ) + mảng giáp dẹt. Dựng bằng helper `Part` có sẵn; có thể thêm "gốc gel" phình ở chân mỗi cơ quan để trông liền thân. Cơ quan hiện có (`Organ_Eyes`, `Organ_Pseudopods`, `Organ_ToxicSpikes`, ...) được gắn lại prefab mới tương ứng; các cơ quan khác tạm dùng prefab gel gần nhất.
- **Thay thế dễ dàng:** thân, lõi, từng cơ quan là prefab riêng trong `Assets/_Project/Prefabs/Slime/`; thay model = thay mesh/prefab, không sửa code.
- **Co-op:** `RemoteAvatar` dùng cùng prefab thân (tô màu theo người chơi trên gel, giữ lõi hổ phách).
- **Cài đặt:** builder `PrototypeSceneBuilder` / `UnderrootLakeBuilder` gọi một hàm chung dựng slime; thêm menu **Evolve → Rebuild Slime Visuals** để làm lại mà không phải dựng lại scene.

## 6. Việc để người làm
Số mặt mesh, tham số nhiễu, độ trong cuối cùng, nhịp đập lõi, độ cong chân; kiểm tra bằng chơi thử và chỉnh theo ảnh chụp màn hình của chủ dự án.

## 7. Kiểm tra
Chủ dự án chạy menu, Play: slime nhận ra được ở cả Prototype_01 và Underroot_Lake; gắn từng cơ quan (God menu F1) thấy liền thân; co-op F2 thấy slime người kia; Console không lỗi đỏ; FPS ổn.
