# HUD và menu mới — 18/11/2026

Bắt đầu tại nhánh `codex/cellular-hud`. Trong lúc làm, tác vụ khác commit và chuyển checkout về `main`, đồng thời cập nhật remote. Phiên triển khai HUD này không chạy pull, push hoặc merge; giữ lại điều kiện GhostMode của cập nhật mới. Không sửa scene, prefab hoặc material để triển khai HUD.

## Đã đưa vào code

- Chỉ số máu, năng lượng, độ no luôn hiện; Tab mở nước/căng thẳng/thân nhiệt/mạng/môi trường. Chỉ số phụ nằm phía trên để ba chỉ số chính không nhảy vị trí.
- Menu một trang, tự co vừa màn hình và chừa chỗ cho HUD. Mở menu không thay đổi Time.timeScale hoặc khóa di chuyển.
- Kho kỹ năng 8 mục/trang, tìm tên kỹ năng/cơ quan không dấu, lọc đã gắn. Chọn kỹ năng vẫn giữ vùng trang bị bên phải.
- Mặc định hiển thị mô tả và thông số từ asset; Tab thay bằng cách tính của kỹ năng được chọn. Gai dùng SpikeDamage, SpikeCount và cấp thật; Charge/BurstDash có phần thông tin riêng.
- Cơ quan có danh sách, sơ đồ điểm chọn, tiến độ, đầu tư EXP và ưu tiên. Tháo có bước xác nhận vì sẽ mất tiến độ nuôi.
- Số trong hồ sơ cơ thể là tổng đang có; số ở cơ quan được ghi rõ là chỉ số gốc chưa tính cấp.
- Gỡ kỹ năng không bị tự gắn lại ở frame kế tiếp. SkillBar vẫn tự gắn kỹ năng mới mở; tự gắn lại khi cơ quan được tháo rồi gắn mới, sau khi Sync nhận biết lần tháo.

## Đã kiểm tra

- Roslyn đi kèm Unity 6000.6.9f1 biên dịch toàn bộ assembly Evolve với response file của Unity; đầu ra kiểm tra riêng, không ghi đè DLL gameplay.
- Biên dịch assembly Evolve.Tests.EditMode với hai test mới.
- Chạy 9 kiểm tra tìm kiếm/phân trang/công thức qua Add-Type với mã HudLayout.cs thật.
- Kiểm tra khung menu không che HUD ở 1280×800, 1366×768, 1920×1080, 800×600.
- Chạy regression với mã SkillBar.cs thật và engine giả tối thiểu: tự gắn, gỡ, gắn lại thủ công, tháo và gắn lại cơ quan. Không gọi đây là test trong Unity.

## Cần xem trực tiếp trong Unity

Chưa chạy Play Mode hoặc NUnit bằng Unity Test Runner. Chưa xác nhận bằng ảnh render. Các DLL kiểm tra biên dịch thành công không thay thế bước này.

1. Vào Play Mode tại scene đang thử, kiểm tra HUD trên nền sáng/tối. G/K/Esc đóng mở đúng, menu không che chỉ số khi giữ Tab.
2. Nhập tên có G/K/H vào ô tìm kiếm: menu không tự đóng. Thử tìm không dấu, không kết quả, lọc đã gắn và đổi trang.
3. Cơ quan cấp 2/3: chọn kỹ năng, gắn/đổi/gỡ ô; kiểm tra hồi chiêu không bị reset do đổi ô. Giữ Tab và đổi kỹ năng để xem thông tin tương ứng.
4. Nuôi cơ quan bằng EXP thật, đổi ưu tiên; tháo rồi hủy/xác nhận. Không mất thao tác nhặt bằng E/F/X khi menu đóng.
5. Chết khi mở menu, đóng menu; thử chỉ số đi theo nhân vật và các mép camera. Khi mở menu, HUD tạm cố định để dễ đọc; đóng lại sẽ theo nhân vật.
6. Đổi kích thước cửa sổ, xem khả năng đọc chữ và độ tràn của mô tả dài. Bộ biểu tượng/kết cấu hiện được vẽ bằng mã; đây là bản tích hợp đầu tiên, chưa phải mỹ thuật cuối.

## File thay đổi

- Assets/_Project/Scripts/Survival/SurvivalHUD.Drawing.cs
- Assets/_Project/Scripts/Survival/HudLayout.cs
- Assets/_Project/Scripts/Abilities/SkillBar.cs
- Assets/_Project/Tests/EditMode/HudCatalogTests.cs
- Assets/_Project/Tests/EditMode/SkillBarMenuTests.cs
- tools/check-cellular-hud.ps1
- tools/check-skillbar-menu.ps1

Chạy kiểm tra không cần Unity Editor:

```powershell
pwsh -NoProfile -File tools/check-cellular-hud.ps1
pwsh -NoProfile -File tools/check-skillbar-menu.ps1
```
