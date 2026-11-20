# Thiết kế: khoang rộng có nhịp, kích cỡ quái theo vai trò, độ khó theo tầng (2026-11-20)

Chủ dự án đã duyệt hướng làm (B cho khoang, A cho kích cỡ, C cho độ khó) trong hội thoại ngày 2026-11-20. Bản này ghi lại những gì đã thống nhất và những gì mình tự quyết.

## Ý chủ dự án và lý do
- Các khoang mê cung rộng 75 m nhưng sinh vật và công trình ít, nên bản đồ trống so với Hang Khởi Sinh. Bản nhân số lượng đã làm (mục 52 PROJECT_STATUS) chỉ rải thêm cùng một thứ, không có nhịp.
- Con nhỏ trông gần bằng con to (nhỏ nhất rộng 0,56 m, Chúa Hang 1,56 m, chỉ chênh khoảng 3–4 lần), và slime lớn nhất (khối 35, gấp đôi lúc mới sinh) to ngang Chúa Hang. Chủ dự án muốn Chúa Hang **to hơn hẳn slime**.
- Độ khó đang phẳng: hang đầu đã có 2 kẻ săn và một Chúa Hang đuổi slime mới sinh. `DangerTier` có trong code nhưng chưa ai dùng. Chủ dự án muốn **hang đầu an toàn hơn, các hang sau nguy hiểm dần**.
- (Phần chiến đấu đa dạng là dự án riêng, làm sau, bản thiết kế riêng.)

## Mục tiêu / không mục tiêu
- Mục tiêu:
  1. Mỗi khoang rộng có vùng có vai trò (cụm cảnh nhìn thấy từ cửa hầm, các khoảng có tổ, đường chuyển tiếp thưa, góc ẩn có thưởng), thay cho rải đều.
  2. Kích cỡ nhìn thấy được theo vai trò và tầng: con mồi nhỏ, kẻ săn vừa, Chúa Hang rộng ít nhất 1,5 lần slime lớn nhất (3,0 m).
  3. Độ khó tăng theo bốn mức: hang đầu, Nông, Giữa, Sâu. Hang đầu có vùng an toàn quanh điểm xuất hiện và Chúa Hang hiền.
- Không mục tiêu: đòn tấn công mới, trùm nhiều pha (dự án chiến đấu); mô hình 3D mới; âm thanh mới; cân bằng cuối cùng (mọi số là bản tạm, chỉnh được trong menu thần).
- Chống mục tiêu: nặng máy; hang đầu thành chán vì quá an toàn; Chúa Hang to đến mức không lọt hầm.

## Các cách đã cân nhắc (chủ dự án đã chọn)
- Khoang: (A) chỉ tăng mật độ, đã làm, không có nhịp; **(B) chia vùng có vai trò, chọn**; (C) khoảng trống có chuyển động (đàn di cư, tuần tra), để sau, dựa vào tổ và sinh thái đã có.
- Kích cỡ: **(A) hệ số theo vai trò áp lúc chạy, chọn**; (B) sửa số trong bộ dựng của bên làm hang rồi dựng lại prefab, làm sau nếu ưng hệ số; (C) chỉ tạo tương phản bằng ánh sáng và âm thanh, không giải quyết "trông giống nhau".
- Độ khó: (A) chỉ một bảng hệ số theo tầng; (B) chỉ thiết kế riêng hang đầu; **(C) cả hai cộng kích cỡ theo tầng, chọn**.

## Quyết định đã thống nhất
- Bảng độ khó (bản tạm):

| Tầng | Sát thương kẻ săn | Tầm phát hiện slime | Số kẻ săn | Chúa Hang |
|---|---|---|---|---|
| Hang đầu (Khởi Sinh) | ×0,6 | ×0,6 | ×0,5 | hiền: không đuổi slime, đánh trả khi bị đánh |
| Nông | ×0,8 | ×0,8 | ×1 | đuổi, chậm (×0,85 tốc độ) |
| Giữa | ×1,0 | ×1,0 | ×1 | đuổi bình thường |
| Sâu | ×1,4 | ×1,4 | ×1,5 | nhanh (×1,15), cắn mạnh |

- Hang theo tầng (mình tự quyết theo chủ đề khoang của bên làm hang): hang đầu = Khởi Sinh; Nông = Vườn Quặng Cầu Vồng, Rêu Thở, Tổ Bỏ Hoang; Giữa = Vực Nứt Tím, Nấm Huỳnh Quang, Suối Nước Nóng, Khe Gió Hú; Sâu = Nghĩa Địa Cự Thú, Hóa Thạch Slime Cổ, Hang Tiếng Thở.
- Hang đầu có vùng an toàn bán kính 25 m quanh điểm xuất hiện của slime: kẻ săn bỏ qua slime trong vùng.
- Kích cỡ: con mồi ×0,85 ở mọi tầng; kẻ săn ×1,2 / 1,3 / 1,4 / 1,5; Chúa Hang ×2,4 / 2,6 / 2,9 / 3,2 (hang đầu, Nông, Giữa, Sâu). Chiều rộng Chúa Hang sau hệ số luôn từ 3,0 m trở lên (slime lớn nhất rộng 2,0 m: bán kính 0,5 m, tỉ lệ thân tối đa 2). Có test giữ điều này.
- Chúa Hang hiền ở hang đầu: Cóc Hồ Già chỉ đánh trả khi bị đánh (30 giây kể từ đòn cuối), và khi đó đánh bình thường.
- Khoang rộng (B): (1) một cụm cảnh lớn đặt ở phía đối diện cửa hầm chính, có đèn, nhìn thấy ngay khi bước vào; (2) 3 tổ mỗi loài trong các khoảng trống, tránh cửa hầm; (3) vùng yên tĩnh quanh cửa hầm (không đặt tổ); (4) đường mòn thưa có đèn mờ nối cụm cảnh với các tổ; (5) một góc ẩn có thưởng ở xa cửa hầm, vòng đá tảng có lối vào quay đi, trong có thức ăn khoáng.
- Hang đầu vẫn nhỏ (bán kính 40): giữ một tổ mỗi loài như cũ.

## Ràng buộc
- Phần lõi sửa file Editor của bên làm hang (`CaveWave1ScenePopulator`, `MazeChamberDecorator`) và `Predator.cs` dùng chung: ghi vào tài liệu bàn giao, báo họ khi gộp.
- Co-op: máy chủ quyết định đòn cắn, máy khách chỉ thấy tương đối. Hệ số kích cỡ áp ở cả hai bên theo bảng cố định nên khớp nhau.
- Kích cỡ áp sau khi `Region` biết hang của sinh vật. Con tạo ngoài `Region` (khoang dùng `PlaceCreatures`) giữ cỡ cũ và độ khó cũ.
- Phải chạy lại `Evolve → Build Maze World (Step 3)` để thấy phần khoang và tổ. Kích cỡ và độ khó có hiệu lực ngay khi chạy.

## Việc để người dựng quyết
- Chỉnh số trong menu thần: ô kéo hệ số kích cỡ theo vai trò và nút chọn mức độ khó để thử từng tầng.
- Vị trí và hình thức cụ thể của cụm cảnh nhìn thấy từ cửa hầm, đường mòn đèn mờ và góc ẩn (mình đề xuất: nhân bản cụm cảnh cao nhất của chủ đề, đèn điểm cường độ thấp, vòng đá tảng 5–6 viên).
- Hóa Thạch và Tiếng Thở chưa có khoang riêng: xếp vào tầng Sâu cho đến khi có.

## Còn lại cho người làm sau
- Chốt số kích cỡ vào bộ dựng của bên làm hang (cách B) khi chủ dự án ưng hệ số.
- Khoảng trống có chuyển động (đàn di cư, kẻ săn tuần giữa các tổ).
- Dự án chiến đấu đa dạng: đòn có dấu hiệu báo trước theo loài, trùm nhiều pha, yếu theo họ kỹ năng.
