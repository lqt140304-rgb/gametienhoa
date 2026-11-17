# HUD/UI — bản triển khai để chơi thử

Ngày: 2026-11-17. Nhánh: `codex/hud-ui`.

## Thay đổi

- HUD tối giản ở hai góc dưới. Nhóm chỉ số bên trái không có nền bảng hoặc viền.
- Nút **Giao diện**: chọn cố định (mặc định) hoặc đi theo nhân vật. Lựa chọn được lưu bằng PlayerPrefs.
- Khi đi theo: hai thanh máu/năng lượng gọn dưới nhân vật, độ no hiện khi còn ≤25%; nhiệt độ và mạng hiển thị nhỏ. Nếu nhân vật ở ngoài màn hình/sau camera hoặc thiếu camera, quay về HUD cố định.
- **K** mở kho kỹ năng: liệt kê kỹ năng đã mở từ cơ quan đang gắn, mô tả/chi phí và nút gán vào ô 1–5. Tối đa 5 kỹ năng trang bị; Q/C là khả năng riêng, không chiếm ô.
- Thanh kỹ năng Q · 1–5 · C nằm giữa phía dưới, hiện hồi chiêu/trạng thái thiếu năng lượng; ô trống hiển thị mờ. Góc phải giữ EXP và lối mở menu.
- **G** mở bảng cơ quan bên phải; cuộn khi dài. Giữ đầu tư EXP, ưu tiên, tháo và gán kỹ năng vào ô 1–5.
- **Esc** đóng menu; **H** mở trợ giúp. Chỉ một menu mở. Khi mở menu, nhân vật vẫn đi/chạy/nảy bình thường. Thi triển/nhặt cơ quan và điều khiển camera bị chặn để tránh thao tác nhầm; thế giới vẫn tiếp tục chạy.
- Scene cũ được bổ sung SkillBar trong bộ nhớ nếu thiếu; không lưu hoặc dựng lại scene.

## Đã xác minh

- Biên dịch toàn bộ runtime C# bằng Roslyn và assembly đi kèm Unity 6000.6.9f1: không lỗi ở cả Input System và nhánh input cũ.
- Cảnh báo compiler ngoài Unity còn CS0649 (field được gán qua Inspector) và CS1701 (reference netstandard), cùng loại đã có ở baseline.
- `pwsh -NoProfile -File tools/check-hud.ps1`: 6 kiểm tra bố cục/chuyển menu đạt. Đã chạy test thất bại trước khi sửa logic giới hạn và toggle.
- Đã rà soát độc lập và bổ sung hiển thị hồi chiêu 1–5 trong lúc chiến đấu.
- Không sửa scene, prefab hay thông số cơ quan.

## Chưa xác minh trực tiếp

Unity batchmode đã thử nhưng thoát mã 1 ngay sau bước mở project, trước khi chạy test scene. Chưa xác minh hình ảnh hoặc thao tác trong Play Mode; compile không thay thế nghiệm thu này.

## Chơi thử trong Unity

1. Mở project `D:/gametienhoa`, chờ compile, mở scene Prototype_01 và Play. Không cần Build Prototype Scene.
2. Xem HUD ở 1280×720, 1920×1080, cửa sổ hẹp; nền sáng/tối. Kiểm tra chữ, thanh, bảng không bị cắt hoặc chồng nhau.
3. Đổi hai chế độ trong Giao diện, dừng/chạy Play lại để kiểm tra lưu lựa chọn. Đi, chạy, xoay camera và đưa nhân vật sát mép màn hình.
4. Nhấn K/G/K/Esc, mở bằng nút chuột; kiểm tra không đồng thời mở hai menu và không vô tình dùng Q/C/1–5 khi menu đang mở.
5. Thử cơ quan mầm, Lv1, Lv2, Lv3; đầu tư EXP, chọn/bỏ ưu tiên, tháo; gán kỹ năng sang ô khác và kiểm tra hồi chiêu không reset.
6. Kiểm tra danh sách trống/dài, nhặt/lắp/thay/hấp thụ cơ quan và thông báo tiến hóa.
7. Chết khi đang mở menu: menu đóng, màn hình chết/hồi sinh vẫn hoạt động. Thử người chơi khác không bị khóa đầu vào theo.

## Test scene tự động (chỉ khi Unity đã đóng project)

`Assets/_Project/Editor/HudSmokeCheck.cs` cung cấp `HudSmokeCheck.Run` cho batchmode: tải scene không lưu, kiểm tra đăng ký HUD, chuyển menu, chặn đầu vào, tương thích SkillBar và đóng khi chết. Kết quả thành công có dòng `HUD_SMOKE_PASS` trong log. Test chưa chạy thành công ở môi trường phiên này.
