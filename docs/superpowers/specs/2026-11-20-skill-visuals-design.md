# Thiết kế: hình ảnh tạm cho kỹ năng hang đợt 1 (chủ dự án đã duyệt hướng làm 2026-11-20)

## Ý chủ dự án
- Mỗi kỹ năng chủ động phải **nhìn thấy được khi dùng** và **đẹp**, dù là hình tạm trước khi có model thật.
- Phong cách đề xuất (chủ dự án chưa phản đối): mềm, lung linh như thạch phát sáng trong hang, không nổ chói.
- Làm trước những gì **không cần mô hình 3D của quái** (bên khác đang làm).

## Mục tiêu / không mục tiêu
- Mục tiêu: 9 kỹ năng chủ động có hình; hiệu ứng "choáng" chung; nháy nhẹ khi Vỏ Cứng giảm đòn.
- Không làm: VFX Graph (chưa cài, thừa), hình thân mọc ra (`SkillVisuals`, cần prefab), Da Dày và Sóng Dò Mạnh (không có gì để thấy / thuộc `World/`), mesh Blender (bước sau).
- Chống mục tiêu: hạt dày đặc làm nặng máy; màu chói che tầm nhìn.

## Các cách đã cân nhắc
1. Chỉ hạt + khối đơn giản: nhanh nhưng nhìn thô.
2. **Hạt + shader riêng + vài mesh dựng bằng code (chọn)**: đẹp nhờ shader (viền sáng trong như thạch, tan dần có viền sáng), không cần file ngoài.
3. VFX Graph / Blender trước: đẹp nhất nhưng chậm, thêm phụ thuộc; để sau nếu hình tạm chưa đủ.

## Quy tắc thêm (chủ dự án, sau khi xem bản phác)
**Bộ phận cơ thể (lưỡi, ngòi, chân, vỏ) là chính thạch của slime**: cùng màu và chất liệu với thân (vật liệu thân slime, màu người chơi chọn), thò ra từ thân rồi thu lại vào thân; không phải vật thể màu riêng "mọc thêm". Hiệu ứng ngoài thân (vòng sóng, khiên tinh thể, hạt, sao choáng) vẫn theo màu họ kỹ năng.
Áp dụng: Lưỡi Dài (lưỡi thạch có núm dính), Châm Choáng (gai thạch nhọn), Bám Vách (chân thạch ấn vào vách), Thu Mình (vỏ thạch dày lên). Khiên Quặng vẫn là mảnh tinh thể (quặng slime đã nuốt): chủ dự án đã đồng ý.

## Cách làm (do mình quyết)
- `Shaders/SkillGel.shader`: viền sáng (Fresnel) + tan dần có đường biên sáng; dùng lại `GlowParticle` cho hạt lấp lánh.
- `Abilities/SkillFx.cs` + `SkillFxMeshes.cs`: bộ dụng cụ chung (vòng sóng, vỏ, mảnh xoay, lưỡi, ngòi, sao choáng, vệt mờ, hạt). Mỗi hiệu ứng là một vật thể ngắn tự dọn.
- Bảng màu theo họ kỹ năng + nhịp mở/giữ/tan và hàm nảy ở `Swallow/Pure/SkillFxMath.cs` (có test).
- Chiêu gọi hình trực tiếp trong khuôn (`AreaAbility`, `LeapAbility`, `PullAbility`, `TrapAbility`, `BodyStateAbility`); khuôn nào dùng lại cho kỹ năng hang sau thì hình theo luôn (màu theo họ).
- Kích cỡ và chiều cao đầu quái đọc từ collider, nên model mới thay vào tự khớp.

## Còn lại cho người làm sau
- Mesh Blender (ngòi, mảnh khiên, gai) thay khối dựng bằng code, khi bên kia xong model và có Blender.
- Chỉnh số hạt/độ sáng theo ảnh Play của chủ dự án.
- Thấy hiệu ứng của người chơi khác trong co-op (hiện chỉ máy người dùng chiêu thấy).
