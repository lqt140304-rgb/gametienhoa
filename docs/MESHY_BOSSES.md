# Trùm Chúa Hang làm bằng Meshy: quy trình và trạng thái

Cập nhật 2026-11-21. Mô hình 3D tạo bằng Meshy (khóa API nằm ở mạng của môi trường, không ở file), đưa vào Unity qua script Blender trong `tools/blender/`. **Chưa mô hình nào được kiểm trong Unity; danh sách thử nằm cuối.**

## Quy trình
1. **Hỏi dáng, màu, chi tiết** với chủ dự án, chốt mô tả rồi mới tạo (mỗi bản xem trước 20 credit, làm màu 10, rig tự động 5).
2. Meshy text-to-3d: `preview` (hình) rồi `refine` (màu). Meshy rig tự động **chỉ nhận hình người hai chân** (xem Golem).
3. Xương: tự gắn bằng script Blender (xương theo tên mà `CreatureLocomotion` tìm), hoặc chủ dự án tự rig trong Blender; ảnh màu tách ra `_albedo.png`, `_mask.png` (không dùng normal map: lộ vết nứt đường nối UV).
4. Unity: menu `Evolve → Fix Meshy Materials` (tự chạy khi nhập), rồi menu dựng prefab. Prefab có hậu tố "(Meshy)" và **được menu thần (F1) ưu tiên** khi tạo quái thử.

## Trạng thái từng trùm
| Trùm | Mô hình | Rig | Prefab / hiệu ứng |
|---|---|---|---|
| Cáo Gió | `Models/Wildlife/Meshy/Creature_CaoGio_Meshy.fbx` (bản cũ, có thể bị thay bằng bản mới 1 đuôi lớn) | tự gắn: `rig_quadruped.py` | menu `Build Cáo Gió (Meshy) Prefab`, `FoxRigFx` (đuôi, chớp mắt, trang sức) |
| Chim Hang Mẹ | `Creature_ChimHangMe_Meshy.fbx` | `rig_bird.py` (kit tay: `rig_bird_kit.py`); cánh tên **Pinion** (không phải Wing, để chân vẫn bước) | menu `Build Chim Hang Mẹ (Meshy) Prefab`, `BirdRigFx` (cánh, mỏ) |
| Golem Thạch Anh | `Creature_GolemThachAnh_Meshy.fbx` + `_Walk.fbx`, `_Run.fbx` | rig tự động của Meshy (24 xương kiểu Mixamo) + 2 clip, qua `rig_meshy_humanoid.py` | menu `Build Golem Thạch Anh (Meshy) Prefab` (Animator Idle/Đi/Chạy theo `Speed`); chưa có chiêu |
| Cóc Hồ Già, Rắn Dung Nham, Bọ Cạp Thạch Anh, Chó Xương | `Creature_<Tên>_Meshy.fbx` (tĩnh, chân sát Z=0, `export_static.py`); Chó Xương đã chỉnh màu khô xương và xích đen xanh lạnh | **chưa rig**: Cóc (xương họng phập phồng), Rắn (chuỗi xương, mang quạt, lưỡi), Bọ Cạp (8 chân, 2 càng), Chó Xương (đã gắn xương từ metarig của chủ dự án bằng `rig_from_metarig.py`: Root, Head, Tail1-5, Leg_FL/FR/BL/BR, jaw + JawTip); bộ chiêu: `docs/superpowers/specs/2026-11-21-cho-xuong-moveset-design.md` | menu `Build Meshy Static Prefabs`: prefab "(Meshy)" tĩnh để xem hình, cỡ, màu (Chó Xương dựng từ đầu vì bên làm hang chưa có prefab) |
| Cáo Gió bản mới (dữ hơn, 1 đuôi lớn) | đã tạo hình và màu, **chưa vào repo, chưa rig** | cần rig riêng (xương theo cáo cũ không khớp) | chưa |

## Lưu ý kỹ thuật đã biết
- `CreatureLocomotion` coi mọi xương tên `Wing*` là loài bay và **bỏ qua bước chân**; xương hàm dưới và cánh phải đo hướng lúc chạy (có xương con `BeakTip`).
- Há hàm cho mô hình mõm khép kín **làm méo mõm**; mô hình nào cần há miệng thì tạo sẵn miệng hé có khe thật.
- Mô hình Meshy ở vị trí gốc giữa thân: nâng chân lên Z=0 trước khi xuất.
- Credit tháng của Meshy hết hạn 21/12/2026 và không cộng dồn.

## Thử trong Unity (chưa ai làm)
Console sạch sau khi pull; chạy menu vật liệu rồi menu dựng prefab; tạo trùm từ menu thần; kiểm màu, cỡ, bước đi/vỗ cánh, trang sức; báo Console nếu có lỗi.
