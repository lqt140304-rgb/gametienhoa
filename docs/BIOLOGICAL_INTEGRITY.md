# PROJECT EVOLVE — Biological Integrity System (BIS) v1.1

> Ngày 2026-11-17. Trạng thái: **[ĐÃ CHỐT]** nguyên tắc, **[ĐỀ XUẤT]** công thức và thông số chờ kiểm thử. Tài liệu bổ sung [GAME_DESIGN.md](GAME_DESIGN.md).

## A. Quyết định được duyệt

1. **[ĐÃ CHỐT]** Sát thương **kết hợp vùng trúng đòn và lan truyền** giữa bộ phận kết nối / HP thân.
2. **[ĐÃ CHỐT]** Bản thiết kế cơ quan đã nghiên cứu **không mất khi cơ quan bị phá hủy**. Có thể tái tạo bằng tài nguyên, kể cả cơ quan A/S; không buộc săn lại boss.
3. **[ĐÃ CHỐT]** Nguồn cơ quan theo bậc: **C** từ sinh vật nhỏ; **B** từ sinh vật thông thường; **A** từ đại thú fantasy; **S** từ boss/cự thú huyền thoại. Boss cao cấp vẫn có thể chứa bộ phận bậc thấp hơn.
4. **[ĐÃ CHỐT hướng]** Cơ quan có thể bị tổn thương, mất chức năng, ảnh hưởng đến gameplay; chất lượng mẫu liên quan cách săn và mức hư hại. Cơ quan mới phải được khám phá/thu thập, không chỉ mở bằng EXP.

## B. Mô hình dữ liệu [ĐỀ XUẤT]

- `CreatureBodyHP`: HP toàn thân; khi về 0, xử lý trạng thái tử vong theo hệ thống riêng chưa chốt.
- `OrganIntegrity`: HP riêng của từng cơ quan, khoảng 0..`maxIntegrity`.
- `Trauma`: tổn thương cấu trúc 0..100, chữa chậm hơn Integrity.
- `FunctionalState`: Normal / Impaired / Disabled / Destroyed.
- `MaterialTraits`: kháng cắt, xuyên, đập, nhiệt, độc và lượng giáp.
- `Connections`: danh sách liên kết cơ quan với hệ số lan truyền có hướng.
- `HarvestCriticalZones`: vùng vi cấu trúc quan trọng để đánh giá chất lượng mẫu.
- `BlueprintID`: thiết kế nghiên cứu đã mở khóa ở cấp nhân vật hoặc save, **không gắn với sự tồn tại của cơ quan**.
- `OrganTier`: C/B/A/S, không tự hạ bậc do bị thương.

Với thân slime không có cơ quan, tổn thương đánh vào thân sẽ tác động trực tiếp lên BodyHP. Cơ quan gắn thêm có vùng nhận đòn riêng.

## C. Công thức sát thương [ĐỀ XUẤT]

### C1. Kháng và giáp

```text
EffectiveDamage = max(0, RawDamage * (1 - Resistance) - EffectiveArmor)
EffectiveArmor  = max(0, Armor - PenetrationFlat) * (1 - PenetrationRatio)
```

- `Resistance` trong khoảng 0..0.75 theo loại sát thương.
- `PenetrationRatio` trong khoảng 0..1.
- Tính kháng và giáp **một lần** tại điểm bị đánh.

### C2. Chia vị trí và lan truyền

Với cơ quan trúng đòn:
```text
LocalOrganDamage = EffectiveDamage * Alpha
BodyHPDamage     = EffectiveDamage * (1 - Alpha)
SecondaryDamage(j) = LocalOrganDamage * Coupling(i,j)
```

- `Alpha` mặc định thử nghiệm **0.70** (cơ quan lộ ngoài); 0.40..0.90 theo loại và vị trí.
- `Coupling(i,j)` mặc định 0..0.15, chỉ dành cho các cơ quan thực sự liên kết.
- **Chỉ lan một bước (one hop), không lan tiếp từ j sang k**; một đòn không thể kích hoạt vòng lặp.
- Phần lan là sát thương bổ sung lên các cơ quan nối trực tiếp, **không tự động nhân thêm BodyHPDamage**; giới hạn tổng hệ số coupling ở 0.30 mỗi điểm trúng.
- Nếu trúng thân mà không trúng cơ quan: `BodyHPDamage = EffectiveDamage`. Có thể thêm shock ngẫu nhiên lên cơ quan nối thân ở phiên bản sau.

Ví dụ `RawDamage=40, Resistance=0.2, EffectiveArmor=5` -> `EffectiveDamage=27`; `Alpha=0.7` -> cơ quan nhận **18.9**, thân **8.1**; khớp nối với coupling 0.15 nhận **2.835**. Đó là giá trị raw damage theo điểm HP, không phải phần trăm Integrity.

### C3. Trauma

```text
NewTrauma = clamp(OldTrauma + TraumaGain, 0, 100)
TraumaGain = 100 * (LocalOrganDamage / MaxIntegrity) * TraumaFactor
```

- TraumaFactor minh họa: đập nặng 1.0, cắt 0.7, xuyên 0.8, nhiệt 0.6 (cần cân bằng theo cơ quan).
- Mỗi đòn chỉ sinh Trauma tại những cơ quan bị đánh, không tính lại theo BodyHPDamage.
- Trauma không tự giảm nhanh cùng Integrity; cần sửa chữa/tái tạo cấu trúc.

### C4. Trạng thái và hiệu suất

```text
IntegrityRatio = clamp(Integrity / MaxIntegrity, 0, 1)
TraumaRatio = Trauma / 100
ContinuousEfficiency = IntegrityRatio * (1 - 0.5 * TraumaRatio)
```

- `Integrity == 0`: **Destroyed**; chức năng = 0.
- `Trauma >= 80`: **Disabled** cho tới khi được sửa (trừ cơ quan có quy tắc ngoại lệ).
- `IntegrityRatio < 0.40` hoặc `Trauma >= 40`: **Impaired**; áp dụng hiệu suất / khóa khả năng theo loại cơ quan.
- Còn lại: **Normal** hoặc damaged cosmetic khi Integrity dưới 0.8.
- Công thức `ContinuousEfficiency` dùng cho chân, mang, cánh, cảm biến liên tục. Các cơ quan kích hoạt (tuyến độc, sừng, lõi) áp dụng ngưỡng chức năng riêng.

### C5. Các ngoại lệ về sinh học

- **Chân:** giảm vận tốc, mất cân bằng; hỏng nặng thì khóa chạy/nhảy tùy số chân còn hoạt động.
- **Cánh:** Integrity thấp giảm lực nâng; dưới ngưỡng an toàn thì không bay, có thể lượn nếu còn màng.
- **Mắt:** thị lực từ mắt đó giảm; nếu toàn bộ giác quan mắt mất, quay về cảm nhận nguyên thủy phù hợp thiết kế.
- **Mang:** giảm trao đổi oxy, nguy cơ ngạt trong nước.
- **Tuyến độc:** mất tiết độc, có thể rò rỉ tùy Trauma.
- **Giáp:** giáp bị thủng làm giảm `EffectiveArmor` ở vùng đó.
- **Lõi sống:** đòn trúng lõi đặc biệt nguy hiểm; quy tắc chết cuối cùng chưa duyệt.

## D. Phá hủy, tách rời và mô hình vết thương [ĐỀ XUẤT]

- `Destroyed`: Integrity về 0; dừng chức năng. Không phải mọi Destroyed đều có mesh tách rời.
- `Severed`: một số cơ quan ngoài (sừng, đuôi, cánh, chi) có thể bị tách nếu trúng khớp nối bằng loại sát thương phù hợp; phải qua điều kiện `CanSever` và `SeverThreshold`.
- Các cơ quan nội tạng (tim, lõi) không được tách từ boss đang sống nếu loài đó không có đặc tính riêng cho phép.
- `HarvestCriticalZones`: tấn công làm hỏng bộ phận vi cấu trúc quan trọng có thể giảm chất lượng mẫu **độc lập** với tình trạng chức năng.
- Thể hiện hình ảnh bằng shader, mesh variant, blend shape và bật/tắt prefab; **không mô phỏng phá từng polygon** trong prototype.
- Kỹ năng hồi HP không tự chữa Trauma nếu không có chức năng tái cấu trúc.

## E. Chất lượng mẫu thu hoạch [ĐỀ XUẤT]

### E1. Tách độ bền và chất lượng mẫu

Khả năng hoạt động của cơ quan và chất lượng mẫu thu hoạch là hai hệ thống riêng. Ví dụ cánh rách màng mất khả năng bay nhưng khung cánh còn tốt; ngược lại tuyến bị nghiền ở vùng quan trọng có thể hoạt động đến gần lúc chết nhưng không còn mẫu tốt.

### E2. Công thức minh họa

```text
DamageLostPercent = 100 * (1 - FinalIntegrity / MaxIntegrity)
CriticalDamagePercent = 0..100 // do hệ thống hit zone tích lũy và giới hạn
SampleQuality = clamp(100 - 0.6 * DamageLostPercent - 0.4 * CriticalDamagePercent, 0, 100)
```

Không lấy Trauma thay CriticalDamagePercent để tránh mô tả sai: Trauma thể hiện khả năng vận hành, còn tổn thương vùng nghiên cứu quyết định mẫu có giữ được thông tin cấu trúc.

- Q >= 80: **Mẫu nguyên vẹn**.
- 40 <= Q < 80: **Mẫu tổn thương**, có thể yêu cầu ghép nhiều mẫu / sửa chữa.
- 1 <= Q < 40: **Mảnh mô**, chủ yếu dùng làm nguyên liệu (có thể bổ sung nghiên cứu mảnh sau).
- Q = 0: **Không có mẫu nghiên cứu**, nhưng còn có thể cho sinh khối.
- **Tier luôn giữ nguyên**, không biến S thành B vì Q thấp.
- Mẫu rơi không bảo đảm 100%: xác suất thu hồi phụ thuộc loại cơ quan, trạng thái boss, phương pháp thu thập, rủi ro săn; **các xác suất vẫn chưa chốt**.

Ví dụ: cánh Integrity còn 35%, critical damage 10% -> Q = 100 - 0.6*65 - 0.4*10 = **57** (mẫu tổn thương). Tuyến Integrity 70%, critical damage 95% -> Q = **44** (mẫu tổn thương sát ngưỡng mảnh).

### E3. Cấp quyền nghiên cứu

- Thiết kế chỉ được mở khóa lần đầu từ mẫu đủ chất lượng hoặc ghép/nghiên cứu nhiều mẫu theo quy tắc loài.
- Sau khi `BlueprintID` được mở khóa, **giữ vĩnh viễn**, kể cả khi cơ quan đã gắn bị phá hủy.
- Thu được mẫu mới có thể là tài nguyên hoặc biến thể, nhưng không bắt buộc săn lại đúng boss để tái tạo blueprint đã biết.

## F. Phục hồi và tái tạo [ĐỀ XUẤT]

### F1. Ba chế độ

1. **Hồi tự nhiên**: chữa Integrity nhẹ bằng dinh dưỡng/năng lượng/độ ẩm; không chữa Trauma nặng.
2. **Sửa chữa tại tổ**: tiêu hao sinh khối + nguyên liệu phổ thông/tương thích để hồi Integrity và giảm Trauma; có thời gian phục hồi.
3. **Tái tạo cơ quan bị phá hủy**: sử dụng blueprint đã nghiên cứu và vật liệu cần thiết; có thể khôi phục từ 0 kể cả bậc S; không bắt người chơi săn lại quái gốc. Nếu thiếu vật liệu, cơ quan tiếp tục ở trạng thái hỏng.

### F2. Công thức chi phí tham khảo

```text
RepairIntegrityCost = BaseOrganBiomassCost * MissingIntegrityRatio * 0.65
RepairTraumaCost = BaseOrganBiomassCost * TraumaRatio * 0.35
RegrowBiomassCost = BaseOrganBiomassCost * TierMultiplier
RegrowTime = BaseRegrowTime * TierMultiplier * SizeMultiplier
```

Hệ số bậc thử nghiệm: C = 1.0; B = 1.4; A = 2.2; S = 3.5. **Không phải tỷ lệ rơi** và chưa chốt cân bằng. Đề xuất tái tạo S cần nhiều nguyên liệu hiếm nhưng các nguyên liệu này nên có **nguồn thay thế** (khai thác, trao đổi hoặc quái khác), không yêu cầu quay lại boss gốc.

Ví dụ: blueprint cơ quan A có giá sinh khối cơ bản 100, bị phá hủy -> tái tạo cần **220 sinh khối** cộng vật liệu tương thích theo thiết kế; nếu cơ quan còn 50% Integrity, Trauma 40% -> sửa chữa khoảng **46.5 sinh khối** (32.5 + 14), chưa gồm vật liệu phụ.

### F3. Khiếm Khuyết vĩnh viễn

- Một cơ quan có thể **bù đắp** Khiếm Khuyết (ví dụ màng chống khô), nhưng **không xóa Khiếm Khuyết** khỏi dữ liệu nhân vật.
- Cơ quan bù đắp bị hỏng/phá hủy -> tác động của Khiếm Khuyết quay trở lại tương ứng.

## G. Tương tác co-op và multiplayer [ĐỀ XUẤT]

- Máy chủ/host là bên tính trúng đòn, Integrity, Trauma, trạng thái cơ quan và vật rơi.
- Không gửi toàn bộ vertex/mesh qua mạng. Chỉ đồng bộ `organInstanceId`, `Integrity`, `Trauma`, `Destroyed`, `Severed`, biến thể hình ảnh và blueprint unlock event.
- Mỗi đòn chỉ xử lý một lần trên server, chống nhân đôi loot khi boss chết và khi sever.
- Snapshot save bao gồm trạng thái cơ quan và các blueprint đã mở khóa.
- Quái có thể mất khả năng chạy/bay khi chân/cánh hỏng; cập nhật AI state và animation.

## H. Phạm vi prototype [ĐỀ XUẤT]

Triển khai trước **3 cơ quan**: mắt, chân, giáp.

1. Collider / hit zone riêng cho từng cơ quan.
2. Sát thương chia local-body, lan một bước.
3. Integrity + Trauma, giảm khả năng mắt/chân/giáp.
4. Loot Q tính từ Integrity và critical damage.
5. Mở khóa blueprint một lần; phá hủy cơ quan, tái tạo tại tổ bằng sinh khối.
6. Save/load và kiểm thử loại sát thương (đập/cắt), không cần hư hỏng từng polygon.

### Tiêu chí kiểm thử

- Đòn đánh cùng vị trí cho kết quả xác định; không bị tính sát thương đôi vào HP.
- Đánh gãy một chân chỉ ảnh hưởng cơ quan đó và liên kết hợp lệ.
- Q giảm khi damage hoặc hit vùng quan trọng tăng, không phụ thuộc chỉ vào HP boss.
- Q thấp **không đổi tier** của cơ quan.
- Sau khi mở blueprint A/S, phá hủy cơ quan vẫn tái tạo được nếu đủ tài nguyên.
- Nếu cơ quan bù đắp Khiếm Khuyết bị phá hủy, hiệu ứng Khiếm Khuyết thể hiện lại.

## I. Các thông số còn phải duyệt

- `Alpha`, `Coupling`, hệ số giáp, hệ số Trauma và ngưỡng disable.
- Công thức Q, các ngưỡng Q, xác suất thu mẫu và điều kiện sever.
- Chi phí/tốc độ tái tạo, nguyên liệu thay thế cho A/S.
- Có cho phép thu được mẫu còn sống mà không giết boss hay không, tùy loài.
