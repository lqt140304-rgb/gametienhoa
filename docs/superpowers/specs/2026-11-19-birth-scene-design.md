# Cảnh Ra Đời: Tiếng Vọng chia bài số mệnh (bước 2)

**Ngày:** 2026-11-19
**Trạng thái:** đã thảo luận từng điểm với chủ dự án; chờ duyệt bản viết này.
**Thuộc:** `docs/superpowers/specs/2026-11-19-reincarnation-survival-design.md` mục 2, 3, 4 (bước 2 của mục 16).

## 1. Ý định và lý do

Người chơi vừa chuyển sinh thành slime thì **số mệnh rút Lõi và Khuyết Điểm cho họ, không ai được chọn**. Cảnh phải khiến họ thấy hồi hộp, kiểu *"omg mình sẽ nhận được gì"*. Chủ dự án muốn nó **như chia bài, thẻ úp rồi lật**, và **Lõi càng mạnh càng hiếm**, để lúc lật trúng thẻ hiếm thì thấy "đã".

Màn chọn đầu ván hiện có (`TraitPicker`: chọn 1 trong 3 thẻ Lõi, rồi chọn Khuyết Điểm) **bị thay hẳn** bằng cảnh này.

## 2. Mục tiêu, không phải mục tiêu, điều tránh

**Mục tiêu**
- Cảnh 15–25 giây: giọt rơi, Tiếng Vọng thông báo sáu Ân Điển, chia bài Lõi, chia bài Khuyết Điểm, tổng kết.
- Rút Lõi theo độ hiếm; Khuyết Điểm rút trong 3 cái đi kèm Lõi đó.
- Kết quả quyết định bằng hạt giống **trước khi** cảnh chạy; cảnh chỉ là hiệu ứng.
- Co-op: mỗi người tự rút, có thể trùng Lõi; máy chủ biết kết quả của từng người.
- Làm lại hai Lõi cực hiếm đã lỗi thời cho hợp luật mới.

**Không phải mục tiêu** (để sau)
- **Dị Tượng** (spec mục 10): hiệu ứng cần Chu Kỳ Lũ và hệ sinh thái (bước 3 và 4); rút mà không có tác dụng là nói dối người chơi.
- Hiệu ứng cho 9 Lõi và 8 Khuyết Điểm đang ghi "chưa có hiệu ứng": làm dần ở các bước sau.
- Scene riêng "Ra Đời" với ánh sáng và camera riêng (nâng cấp sau).
- Chuyển sinh chạy lại cảnh (bước 6); ở đây chỉ cần cảnh gọi lại được.

**Điều tránh:** cảnh lặp lại mỗi lần chết và hồi sinh trong cùng vòng chơi; thẻ chưa có hiệu ứng mà không báo; hai máy co-op thấy hai kết quả khác nhau.

## 3. Ràng buộc từ code hiện có

- `Scripts/Traits/`: `TraitSeed` (12 Lõi, 12 Khuyết Điểm dạng dữ liệu thuần), `CoreSO`/`FlawSO` (asset do menu `Evolve → Create Traits` tạo), `TraitCatalog` (nạp asset), `PlayerTraits` (`Set(coreId, flawIds)`, giữ Lõi và Khuyết Điểm của người chơi), `TraitNet` (cầu nối tới assembly mạng; `Local`).
- Màn cũ đóng băng slime bằng `SurvivalStats.Frozen = true`, `State.Invulnerable = true`, `SurvivalHUD.ExternalMenuOpen = true`. Cảnh mới dùng lại đúng ba cờ này.
- Co-op: `Net/TraitsSync` giữ phòng `TraitRoom` trên máy chủ; client gửi `evolve.tr.pick` (Lõi và Khuyết Điểm), máy chủ kiểm bằng `TraitChoice.IsValid` rồi `GiveTraitsToAvatar`. Assembly `Evolve.Net` tách riêng nên `Evolve` không gọi thẳng, chỉ qua móc nối của `TraitNet`.
- Không có Unity trong môi trường làm việc của người viết kế hoạch: phần luật thuần phải chạy được bằng `tools/cstest/run.sh`; phần IMGUI và mạng chỉ kiểm được trong Unity.
- Mọi chữ hiện trong game bằng tiếng Việt.

## 4. Các hướng dựng đã cân nhắc

1. **Phủ lên màn hình ngay tại chỗ sinh ra** *(chủ dự án đồng ý)*: màn tối, slime đứng yên ở bờ hồ, giọt rơi là hạt sáng trong thế giới thật, chữ và thẻ vẽ bằng IMGUI (như HUD hiện có). Rẻ, khớp phong cách sẵn có, không chuyển cảnh.
2. **Scene riêng "Ra Đời"**: đẹp hơn nhưng phải chuyển scene và đồng bộ co-op hai nơi; để sau.
3. **Camera sát slime trong hang**: đẹp nhất nhưng phải can thiệp vào camera điều khiển, dễ lỗi; có thể nâng cấp lên sau mà không làm lại phần chữ và rút số mệnh.

Chọn hướng 1. Chia bài thay cho vòng quay (CS:GO) theo ý chủ dự án sau cùng.

## 5. Thiết kế

### 5.1 Trình tự cảnh (khoảng 22 giây; có thể bỏ qua)

| Giây | Cảnh |
|---|---|
| 0–2 | Tối hẳn. Một giọt nhờn rơi từ trần, tiếng "tách" nếu dự án đã có hệ âm thanh. |
| 2–7 | Tiếng Vọng, từng dòng: *«Xác nhận… Cá thể mới hình thành.»*, *«Ban tặng Ân Điển Khởi Sinh.»*. Sáu Ân Điển lần lượt hiện và khắc vào thân, mỗi cái một nhịp sáng: Túi Không Đáy, Nuốt Chửng, Giải Mã, Biến Hình, Sóng Dò, Phân Thân (tên và một dòng tác dụng, theo spec mục 3). |
| 7–14 | **Chia bài Lõi** (5.2). |
| 14–19 | **Chia bài Khuyết Điểm** (5.3). |
| 19–22 | Tổng kết: *«Lõi độc nhất: […]. Khiếm khuyết: […].»*, hai thẻ nằm cạnh nhau, rồi cảnh tan, slime tỉnh dậy. |

- **Bỏ qua:** bấm Space hoặc Enter sau 3 giây đầu: nhảy thẳng tới tổng kết (kết quả vẫn giữ nguyên).
- Cảnh chạy **một lần** khi vào vòng chơi mới; hồi sinh sau khi chết **không** chạy lại.

### 5.2 Chia bài Lõi

- Nhiều thẻ úp được chia ra lần lượt thành một **hình quạt** ở giữa màn hình, rồi xáo nhanh. Lưng thẻ có **viền phát sáng theo độ hiếm** (trắng, xanh dương, vàng) nhưng chỉ lờ mờ, không tiết lộ thẻ nào.
- Thẻ số mệnh trượt ra giữa, **lật lên** hiện tên, một dòng tóm tắt, màu theo độ hiếm. Thẻ vàng có thêm hiệu ứng sáng rực và rung nhẹ.
- Thẻ chưa có hiệu ứng ghi nhỏ **"chưa có hiệu ứng"**.

### 5.3 Chia bài Khuyết Điểm

- Ba thẻ úp (ba lựa chọn đi kèm Lõi vừa trúng; Lõi nặng thì mỗi thẻ là một cặp Khuyết Điểm), tối và nứt, trải ra rồi xáo.
- Thẻ số mệnh lật lên, hiện tên và mô tả. Tối, trầm, tiếng nặng nếu có âm thanh.

### 5.4 Rút số mệnh (luật thuần)

- **Độ hiếm của Lõi** (tạm, chỉnh khi chơi thử):

  | Độ hiếm | Tỉ lệ | Lõi |
  |---|---|---|
  | Thường | 65% | Màng Thẩm Thấu, Ăn Mòn, Quang Hợp, Cộng Sinh, Thích Nghi, Bám Dính |
  | Hiếm | 25% | Thần Tốc, Nguỵ Sắc, Phản Chấn, Quen Đất |
  | Cực hiếm | 10% | Con Bạc (làm lại), Giữ Gốc (làm lại) |

  Chọn bậc theo tỉ lệ, rồi chọn đều trong bậc (như `DropTableSO`). Nếu một bậc rỗng thì tỉ lệ của nó chia lại cho các bậc còn lại.
- **Kho rút gồm cả 12 Lõi**, kể cả Lõi chưa có hiệu ứng (đã chọn A).
- **Khuyết Điểm:** chọn đều trong 3 lựa chọn của Lõi đó (đã chọn A).
- Hàm thuần `FateRoll.Roll(seed, pool)` trả về `(coreId, flawIds)`; cùng hạt giống cho cùng kết quả. Mỗi người chơi có hạt giống riêng: hạt chung của vòng chơi trộn với số hiệu người chơi.
- Dữ liệu độ hiếm nằm trong `TraitSeed`/`CoreDef`/`CoreSO` (thêm trường `Rarity`); menu `Evolve → Create Traits` cập nhật asset đã có mà không làm mất chỉnh sửa tay.

### 5.5 Làm lại hai Lõi cực hiếm

Giữ nguyên `Id` (`core.con_bac`, `core.giu_goc`) để không làm hỏng dữ liệu, đổi tên hiển thị và tác dụng:
- **Con Bạc → Dòng Máu Tham Lam:** nhân **×1,5** cơ hội của dòng gene Hiếm, Quý, Huyền thoại trong Giải Mã (spec mục 5.3). Hai Khuyết Điểm đi kèm giữ như hiện có. Thay các chỗ gọi `LuckRules`/`TraitTuning.LuckBonus` cũ, vốn nói về tỉ lệ rơi cơ quan không còn tồn tại.
- **Giữ Gốc → Dạ Dày Bất Diệt:** chết khi còn mạng thì **giữ nguyên các món trong Túi Không Đáy** ở hai lần chết đầu (thay `DeathLossRules.KeepsEquippedOrgans` bằng `KeepsStomach`).

### 5.6 Co-op

- Mỗi máy rút số mệnh của mình trong cảnh, rồi gọi `PlayerTraits.Set`.
- Client gửi kết quả lên máy chủ bằng `evolve.tr.pick` đang có; máy chủ kiểm: Lõi có trong danh mục, Khuyết Điểm đúng là một lựa chọn của Lõi đó (`TraitChoice.IsValid`), rồi gắn vào avatar. **Máy chủ không kiểm độ hiếm hay hạt giống** (chấp nhận tin tưởng người chơi, game co-op bạn bè).
- **Bỏ:** `TraitRoom`, `CardDealer`, tin `evolve.tr.hand` và `evolve.tr.result`, kết quả "bị tranh, đổi thẻ", cùng các test của chúng.
- Một người vào giữa chừng thì tự chạy cảnh của mình khi vào.

### 5.7 Kết quả lưu ở đâu

- `PlayerTraits` giữ Lõi và Khuyết Điểm suốt vòng chơi, kể cả khi chết và hồi sinh (đã đúng như vậy).
- Khi chuyển sinh (bước 6): xóa và rút lại.

## 6. Quyết định do người viết tự đặt (chủ dự án xem lại)

- Thang độ hiếm 65/25/10 và việc xếp từng Lõi vào bậc nào (chủ dự án chỉ nói "Lõi quá mạnh thì hiếm").
- Làm lại hai Lõi thành Dòng Máu Tham Lam và Dạ Dày Bất Diệt, hệ số ×1,5.
- Cảnh 22 giây, bỏ qua sau 3 giây; chữ Tiếng Vọng tự đặt theo giọng spec.
- Máy chủ co-op không kiểm độ hiếm.
- Dị Tượng không có trong bước này.
- Không có âm thanh nếu dự án chưa có hệ âm thanh sẵn (người làm kiểm tra rồi quyết).

## 7. Để người làm kế hoạch quyết

- Cách vẽ thẻ: bằng IMGUI, theo phong cách thẻ gel của `TraitPicker` cũ (có thể tái dùng kiểu vẽ, khung, gradient).
- Tên file và cách chia lớp trong `Scripts/Traits/` (đề xuất: `FateRoll` thuần, `BirthScene` MonoBehaviour thay `TraitPicker`).
- Hiệu ứng giọt rơi: hạt sáng trong thế giới hoặc chỉ chớp sáng trong màn hình, tùy cái nào đỡ rủi ro.
- Cách thể hiện 6 Ân Điển: danh sách nhỏ hiện dần, hay sáu biểu tượng khắc vào thân.
- Nếu chưa chạy menu `Evolve → Create Traits` (danh mục asset rỗng): rút thẳng từ dữ liệu `TraitSeed` trong bộ nhớ, hoặc bỏ cảnh kèm cảnh báo; chọn cái đỡ rủi ro hơn. Hạt giống chung của vòng chơi (ví dụ `Environment.TickCount` như phòng cũ) cũng do người làm quyết.
- Không có phím tắt "chơi không Lõi" như màn cũ (Esc): bấm bỏ qua chỉ rút ngắn cảnh, kết quả vẫn được giao.
