# Golem dạng rìu và phòng trùm: thiết kế (chờ chủ dự án duyệt)

## Ý định và lý do
Mỗi trùm có **một căn phòng riêng**, nơi nó ở; trùm chỉ xuất hiện khi người chơi **kích hoạt** phòng, và dùng đồ trong phòng để tấn công. Với Golem, đồ đó là **một cây rìu cắm giữa phòng**. Còn nửa máu, Golem đi rút rìu (nhọc nhằn, 3 nhịp) và đổi sang **dạng rìu**: đánh thường dày hơn, có chiêu mới, và có thể **ném rìu đi rồi đi nhặt lại**. Mục đích: trận đánh có chiều sâu và nhịp "căng - thả": mỗi lần Golem đi rút hoặc nhặt rìu là khoảng hở cho người chơi, nhưng chỉ **đánh xa** được (lại gần thì bị hất văng).

Map phòng do **một phiên khác** dựng; phiên này chỉ làm Golem và logic, nối với map qua một hợp đồng đơn giản (mục "Hợp đồng với map").

## Mục tiêu / không phải mục tiêu / điều phải tránh
- Mục tiêu: dạng rìu đổi hẳn nhịp trận; mỗi lần Golem rút/nhặt rìu tạo cơ hội đánh xa; người chơi chọn được Golem phải đi đâu nhặt rìu (nhờ rìu ném ngắm theo chỗ người chơi đang chạy tới).
- Không phải mục tiêu (lần này): người chơi tác động được vào rìu (đã chốt: **không**); Golem dùng cột/tinh thể trong phòng để tấn công (chỉ đặt tên điểm, hành vi để sau); mô hình Meshy cho rìu (tạm bằng hình khối); dựng map.
- Phải tránh: dạng rìu làm Golem khó hơn một cách vô lý (đổi dạng giữa lúc nó đang gầm chuyển phase, rìu rơi chỗ không tới được làm trận kẹt); bộ chiêu mất cân bằng so với bộ chuẩn.

## Ràng buộc
Unity chưa chạy được trong môi trường làm việc, nên mọi thứ phía Unity chỉ kiểm chứng được khi chủ dự án test; phần logic thuần C# làm theo TDD và chạy bằng `tools/cstest/run.sh`. Code bằng tiếng Anh, tài liệu bằng tiếng Việt. Không phá hệ phase (66%/33%), đọc người chơi, đi vòng đánh giá, hay bộ chiêu chuẩn.

## Các cách làm đã cân nhắc (và lựa chọn)
1. **Đổi dạng:** (A) một danh sách chiêu chung, lọc theo dạng bằng `Allow` **[chọn]**; (B) hoán đổi cả bộ chiêu; (C) bộ não riêng. Chọn A vì ít code nhất và giữ hồi chiêu.
2. **Đi rút / nhặt rìu:** (A) khối máy trạng thái chung "đi lấy đồ" (đi tới, kéo nhọc nhằn) **[chọn]**; (B) viết riêng. Ghi chú: `VolleyFsm` của Giáng Thạch **chưa được gộp vào khối chung** trong lần này (rủi ro hồi quy); gộp sau nếu cần.
3. **Hất người lại gần lúc đang kéo:** (A) vòng hất kiểm tra mỗi bước + cú vung tay ngắn chồng lên tư thế **[chọn]**; (B) chiêu thật cắt ngang việc kéo.

## Thiết kế
### Luồng
```
Phòng chờ (Golem ẩn) -(vào Activate_Zone)-> Golem thức dậy, tay không (phase 66%/33% như cũ)
  máu <= 50% (và đang rảnh) -> đi tới rìu -> rút 3 nhịp -> DẠNG RÌU
  Dạng rìu -> Ném Rìu -> tay không (rìu cắm tại điểm rơi) -> đi nhặt -> rút 3 nhịp -> DẠNG RÌU
```
Đổi dạng là **một chiều lên dạng rìu một lần ở 50%**; sau đó Golem chuyển qua lại giữa "cầm rìu" và "tay không" theo việc rìu đang ở đâu.

### Trạng thái cây rìu (`AxeFsm`, thuần)
`Stuck(at)` (cắm ở chỗ nào đó; ban đầu ở `Axe_Anchor`) → `Fetching` (Golem đi tới) → `Pulling` (3 nhịp) → `Held` → `Flying` (đang ném) → `Stuck(at điểm rơi)`. Quy tắc: bắt đầu đi lấy khi (máu ≤ 50% và rìu `Stuck` và Golem rảnh và không đang gầm chuyển phase) hoặc (rìu `Stuck` sau khi ném và đã chiến đấu tay không ít nhất 1,5 giây).

### Kéo nhọc nhằn (3 nhịp)
Mở rộng `PullRules` để nhận số nhịp (hiện cố định 4): `Lift(t01, heaves)`, `Strain`, `Heave`. Rìu: **3 nhịp, 3,5 giây**, mỗi nhịp một tiếng gằn, bụi, rìu rung; nhịp cuối bật rìu lên. Trong lúc kéo Golem **không bị cắt ngang** (`Armored`) và **không có thời gian nghỉ thở**.

### Vòng hất người lại gần (`PushAuraRules`, thuần)
Trong lúc kéo: người chơi trong **4 m** bị hất ra (lực 10), **6 sát thương**, **choáng 1 giây**; mỗi người tối đa 1 lần mỗi **1,5 giây**. Kèm cú vung tay ngắn chồng lên tư thế kéo (không ngắt nhịp).

### Chiêu theo dạng
`MoveDef.Form` (cả hai / tay không / rìu); bộ chọn chiêu dùng `Allow` lọc theo dạng.
| Dạng | Chiêu |
|---|---|
| Tay không | Nện Đấm, Quét Tay (thường); Húc Đầu (combo); Giáng Thạch (đặc biệt); Lõi Nổ (ultimate) — **như hiện tại** |
| Rìu | **Chém Dọc** (thường, gần, nối Chém Ngang rồi Chém Dọc), **Chém Ngang** (thường, quét quanh 4 m), **Ném Rìu** (đánh xa), **Bổ Đất** (đặc biệt, né được), Lõi Nổ (ultimate, dùng chung) |
Dạng rìu nghiêng về đánh thường (trọng số đòn thường x1,5). Mỗi dạng thỏa bộ chuẩn: 2 thường, 1 đặc biệt, 1 ultimate, và đánh xa/combo như bảng.

Số liệu tạm: Chém Dọc 0,8 / 0,25 / 1,2 s, sát thương 30; Chém Ngang 0,7 s, sát thương 26, hất 8, bán kính 4; Ném Rìu: gồng 1,4 s, bay 1,0 s, 6–20 m, sát thương 34, hồi 14 s; Bổ Đất: gồng 1,8 s, sóng chạy thẳng dài 14 m rộng 2,4 m tốc độ 14 m/s, sát thương 30, hồi 20 s.

### Ném Rìu
Ngắm vào chỗ người chơi **sắp chạy tới** (đọc người chơi). Trúng người: sát thương, rìu cắm tại chỗ người chơi vừa đứng. Trượt: rìu cắm ở điểm rơi (chiếu xuống đất); nếu điểm rơi không tới được (tường, hố) thì rơi xuống chân Golem. Rìu là vật thật bay (xoay, lưỡi dẫn), không dùng quả cầu thay thế.

### Bổ Đất
Chém xuống đất: một **dải sóng** chạy thẳng về phía trước (vết nứt, bụi, đá bắn), né bằng bước ngang hoặc nhảy. Kiểm tra va chạm là phép thử hình chữ nhật chuyển động (`ShockwaveRules`, thuần, có test).

## Hợp đồng với map (cho phiên dựng map)
Trong phòng đặt các GameObject rỗng đúng tên (code tìm theo tên, không cần gắn script): `Boss_Wait` (chỗ Golem nằm chờ), `Axe_Anchor` (chỗ cắm rìu, giữa phòng), `Activate_Zone` (collider trigger: người chơi bước vào thì kích hoạt), `Prop_Pillar_*` và `Prop_Crystal_*` (cột/tinh thể: **chưa có hành vi**, chỉ để sau). Phía code: khối `BossRoom` tìm các điểm đó, ẩn Golem trước khi kích hoạt, phát sự kiện `Activated`. Sẽ có danh sách làm theo trong `docs/`.

## Phần thuần C# (TDD) và phần Unity
Thuần: `AxeFsm`, `FetchFsm` (đi tới / kéo), `PullRules` theo số nhịp, `PushAuraRules`, `ShockwaveRules`, `MoveDef.Form` + hạt giống bộ chiêu dạng rìu, bộ kiểm tra chuẩn theo dạng. Unity (chưa kiểm chứng): `GolemAxe` (hình rìu tạm: cán và lưỡi; các trạng thái cắm / cầm / bay), tích hợp vào `CreatureCombat` (đổi dạng, chiêu ép đi lấy rìu), tư thế mới trong `GolemRigFx`/`GolemRules` (Chém Dọc, Chém Ngang, Ném Rìu, Bổ Đất, kéo rìu, vung tay hất), âm thanh mới (ElevenLabs) và hiệu ứng ba giai đoạn, `BossRoom` và danh sách cho map.

## Để người dựng quyết (các lựa chọn của tôi, chủ dự án xem lại)
- Số liệu tạm; 3 nhịp = 3,5 s; vòng hất 4 m / 6 sát thương / 1 s choáng / 1,5 s giãn cách.
- Đổi dạng ở 50% là một chiều; nếu Golem đang gầm chuyển phase thì đợi gầm xong rồi mới đi rút.
- Rìu ném ngắm theo đọc người chơi; điểm rơi không tới được thì rơi xuống chân Golem.
- Chưa làm: hành vi dùng cột/tinh thể; mô hình Meshy cho rìu; gộp `VolleyFsm` vào `FetchFsm`.
