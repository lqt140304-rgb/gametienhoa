# Thiết kế: Trần vòm rễ và ánh sáng ba lớp (làm lại lớp trần, ánh sáng hoàng hôn dịu)

> Ngày: 2026-11-18 · Chủ dự án chọn hướng **A (vòm đất đá tối có sườn rễ lớn)**, ánh sáng **dịu như hoàng hôn**, **ba lớp nguồn sáng**, các yếu tố kèm theo phải **chỉnh chu, đẹp và có logic**.
> Nhãn: **[ĐÃ CHỐT]** chủ dự án quyết; **[ĐỀ XUẤT]** Claude đề xuất, chờ duyệt; **[BUILDER]** người làm tự quyết.

## 1. Ý định

- Lớp trần hiện nay là một tấm rễ dày đan nhau phủ kín (`LakeCanopy`, mức 25 m): nhìn từ dưới như đống ống nâu lổn nhổn, "kinh khủng". Cần một trần **đẹp, có cấu trúc và có lý do tồn tại**. **[ĐÃ CHỐT]**
- Ánh sáng bản đồ phải khớp tài liệu thế giới ("càng ra cửa hang càng tươi sáng") và concept (vùng sáng dịu có màu), không phải hang tối gắt. Chủ dự án muốn **dịu như hoàng hôn**: thấy được xung quanh, không đen. **[ĐÃ CHỐT]**
- Ba lớp ánh sáng **[ĐÃ CHỐT]**: (1) ánh sáng sinh học của rêu, nấm, bào tử là nguồn chính bên trong; (2) mặt trời yếu lọt qua kẽ rễ làm điểm nhấn; (3) ánh sáng tự nhiên từ ngoài, mạnh dần về phía lối ra (khu 06, phía bắc).
- Thứ tự độ sáng theo khu (tối → sáng) **[ĐỀ XUẤT, theo đề cập của chủ dự án]**: 02 Lõi Rễ < 03 Bãi Mùn < 04 Vườn Bào Tử < 01 Khởi Sinh < 05 Khe Thủy Mạch < 06 Rìa Săn Mồi.

## 2. Mục tiêu, không mục tiêu, điều phải tránh

**Mục tiêu**
- Trần mới: một **vòm đất đá tối, mịn**, trên đó các **sườn rễ lớn** tỏa ra từ một tâm hợp lý, chia nhánh dần thành rễ mảnh, rủ xuống rễ và rêu phát quang. Các kẽ giữa sườn rễ là nơi ánh sáng lọt.
- Ánh sáng nền hoàng hôn, khác nhau theo khu; vầng sáng sinh học có màu theo khu; một nguồn sáng tự nhiên ấm ở lối ra.
- Giữ thị lực: chưa có mắt vẫn đen trắng, tối, chỉ sáng quanh người.

**Không mục tiêu**
- Không đổi model nhân vật, sinh vật, cơ quan; không đổi địa hình, nước, bơi.
- Không dựng lại thành phố người cây (`LakeTreefolkCity`, `LakeClassHouses`).
- Không sương khối thật.

**Điều phải tránh**
- Trần nhìn lộn xộn hoặc lặp lại rõ ràng (cùng một đoạn rễ chép nhiều lần).
- Tụt FPS (đã đo: bóng đổ thật của mái ít tốn; giữ trần ≤ vài trăm nghìn mặt, có LOD).
- Ánh sáng làm mất đọc thị lực hoặc làm thức ăn và kẻ thù khó thấy.
- Sườn rễ chặn lối đi hay đè lên thành phố người cây.

## 3. Ràng buộc

- Mái hiện: `LakeCanopy.cs` (phiên trang trí) dựng các ô 64 m `Roof x,z` bằng `RootMeshes.Lattice` ở độ cao tối đa 25 m, bỏ lỗ ở `ZoneLayout.LightShafts` (9 lỗ bán kính 8 m); `LakeSun` (của tôi) đang bật đổ bóng cho các renderer dưới `Lake Canopy (Dressed)/Roof`. Công cụ mới **thay** lớp mái này bằng bộ vòm mới và giữ đúng tên `Roof` để `LakeSun` vẫn tìm thấy; không sửa `LakeCanopy.cs`.
- Đại sảnh người cây ở tâm khu 02 (`ZoneLayout.RootFoot.Center`, vòm riêng cao 18 m, `ZoneLayout.InsideCave`): vòm mới phải nằm trên và không đè lên nó.
- `MonochromeVision` giữ ambient, sương và mặt trời theo thị lực; `ZoneAtmosphere` giữ sương theo khu; mọi thay đổi ánh sáng theo khu phải chồng lên hai hệ này, không thay chúng.
- Blender (`bpy` 5.3) cho vật thể; FBX cài từ `tools/blender/out/*.fbx.b64` (Git LFS bị chặn khi agent push). Agent không có Unity: kiểm bằng ảnh render Blender và ảnh Unity chủ dự án gửi.
- Không `git add .`; giữ dòng netcode trong `Packages/manifest.json`.

## 4. Các hướng đã cân nhắc (đã chốt A)

| Hướng | Nội dung | Kết quả |
|---|---|---|
| **A (chọn)** | Vòm đất đá tối + sườn rễ lớn tỏa ra | thoáng, có cấu trúc, kẽ ánh sáng tự nhiên |
| B | Thưa bớt lớp rễ cũ | nhanh nhưng vẫn là rễ đan |
| C | Giữ hình, chỉ làm tối và che sương | trần mất chi tiết |

## 5. Thiết kế **[ĐỀ XUẤT]**

### 5.1. Cấu trúc trần (logic của thế giới)

Cả hệ rễ là một: "cây cổ thụ" có **cội nằm ở Đại sảnh** (tâm khu 02). Mọi sườn rễ lớn **tỏa ra từ đó** như tán rễ ngược, đi lên vòm rồi lan khắp bản đồ, chia nhánh nhỏ dần về phía rìa. Vì thế người chơi luôn thấy sườn rễ chạy "về phía cội", dễ định hướng và có lý do hình học.

- **Vòm:** một mặt cong lớn (lấy độ cao từ `LakeLayout.CeilingY`) bằng lưới thưa, vật liệu đất tối mịn, có nhiễu nhẹ và vài đường nứt; **lỗ sáng** ở 9 chỗ `LightShafts` (viền bo tròn, rễ bao quanh miệng lỗ). Tên đối tượng `Roof ...` để `LakeSun` bật đổ bóng.
- **Sườn rễ lớn:** 9–11 sườn chính (bán kính 2,5–4 m ở gốc, thon dần), mỗi sườn là một đường cong 3D tính bằng code (bám mặt vòm, hơi nhô xuống), tỏa từ Đại sảnh tới rìa; chia 2–3 nhánh cấp hai (0,8–1,6 m) rồi nhánh cấp ba (0,3–0,6 m) theo quy tắc chia nhánh; các nhánh không cắt nhau, chừa kẽ rộng ≥ 6 m ở chỗ lỗ sáng. Dựng bằng **các đoạn Blender** ghép nối (đoạn thẳng, đoạn cong, chỗ rẽ nhánh, đầu tận) để vỏ rễ chi tiết (rãnh, bướu, rêu) mà vẫn lắp được: `Rib_Straight_N`, `Rib_Bend_N`, `Rib_Fork_N`, `Rib_Tip_N`.
- **Rễ mảnh rủ:** nhiều kiểu, độ dài khác nhau, dày hơn gần sườn rễ lớn, thưa dần; có **màn rễ** (nhiều rễ mảnh sát nhau) ở rìa các kẽ sáng.
- **Điểm nhấn trên trần theo khu (logic theo môi trường):** 01 túi phát quang xanh ngọc rủ xuống mặt hồ; 02 rễ già và bụi gỗ lơ lửng; 03 nấm tầng bám sườn rễ; 04 bọc bào tử tím phát quang; 05 nhũ đá vôi (Blender) lẫn rễ; 06 vòm cao hơn, rễ thưa và khô hơn gần lối ra.

### 5.2. Ánh sáng ba lớp

1. **Sinh học (nguồn chính):** các điểm phát quang (rêu, nấm, bào tử, túi trên trần) mỗi nơi một đèn điểm rất nhẹ không bóng (tối đa vài chục đèn đang hoạt động gần người chơi, tắt xa), màu theo khu. Dùng lại đèn nấm của `LakeLook` làm gốc.
2. **Mặt trời qua kẽ rễ:** `LakeSun` giữ, nhưng cường độ hạ theo "dịu như hoàng hôn" (khoảng 0,6), màu ấm hơn; bóng của vòm cho mảng sáng khớp kẽ.
3. **Tự nhiên từ ngoài:** một đèn ấm (điểm hoặc spot) nhỏ chiếu từ lối vào phía bắc (`ZoneLayout.NorthEntry`) và vùng ambient sáng dần về khu 06.
- **Ambient theo khu (hoàng hôn):** `ZoneAtmosphere` thêm bảng ambient theo khu (xanh lục dịu 0,08–0,20; 02 tối nhất, 06 sáng nhất, 04 hơi tím), trộn theo trọng số như sương, chỉ khi có mắt đầy đủ (`sight ≥ 0,999`) và giữ trần `Min(0,45, ambient khu)` khi thị lực dở dang như `MonochromeVision` đã sửa.

### 5.3. Công cụ và nhịp làm

- Blender: `tools/blender/build_vault.py` (đoạn sườn rễ, nhánh, rễ rủ, túi phát quang, nhũ đá, lỗ sáng viền), `test_vault.py`.
- Unity Editor: `Editor/LakeVault.cs`, menu **Evolve → Dress Lake: Root Vault** (thay "Lake Canopy (Dressed)/Roof", giữ tên `Roof`, chạy lại được, có Restore), thêm vào Rebuild Lake (sau Root Canopy, trước Dappled Sun).
- Ánh sáng: cập nhật `DappledSunMath` (cường độ), `ZoneAtmosphereMath` (ambient theo khu, thử bằng EditMode), `Editor/LakeLook.cs` (đèn sinh học theo khu, đèn lối ra).
- Nhịp (mỗi bước một PR): (1) ánh sáng ba lớp và ambient theo khu (nhanh, dùng ngay); (2) vòm và sườn rễ (xem ảnh render rồi duyệt); (3) rễ rủ, túi phát quang, nhũ đá và điểm nhấn từng khu.

### 5.4. Kiểm chứng

- Python (`bpy`): kích thước, số mặt, đầu nối khớp (hai đoạn kề nhau có miệng ống trùng tâm và bán kính), không lấp lòng lỗ sáng.
- C#: test cho phần thuần (đường sườn rễ tỏa từ tâm, không cắt nhau, chia nhánh đúng cấp, ambient theo khu trong khoảng an toàn).
- Thủ công: ảnh render Blender duyệt trước; ảnh Unity từng khu; FPS bằng `PerfOverlay`; thử chưa có mắt và có mắt.

## 6. Rủi ro

- Ghép đoạn rễ trơn tru (không thấy mối nối) là phần khó nhất; nếu khó, dùng ít loại đoạn hơn và che mối bằng bướu và rêu.
- Vòm lớn: số mặt cần LOD; kiểm bóng đổ vẫn rẻ.
- Phiên trang trí đang sở hữu mái hiện tại: báo họ biết công cụ mới thay mái (không sửa file của họ).

## 7. Để lại cho người làm

- Dạng hình học chính xác của đoạn rễ (modifier, nhiễu), số lượng biến thể, quy tắc chia nhánh chi tiết, cường độ và tầm của đèn sinh học, giá trị ambient cuối cùng sau khi xem ảnh.
