# Kế hoạch chiều sâu cho trùm (đề xuất, chờ chủ dự án duyệt)

**Vấn đề:** trùm hiện chỉ có "di chuyển" và "tấn công": người chơi học xong bộ chiêu là thắng bằng cách lặp lại một cách né. Mục tiêu: trùm **đọc được người chơi, đổi hành vi theo trận đấu, có điểm yếu và nhịp căng-thả**, nhưng vẫn theo bộ chiêu chuẩn (1 xa, 1 nội tại, 2 thường, 1 đặc biệt, 1 ultimate, tối đa 1 combo).

## 6 trụ cột (xếp theo giá trị trên công sức)

| # | Trụ cột | Ý tưởng | Thử trên Golem | Công sức |
|---|---|---|---|---|
| 1 | **Giai đoạn (phase)** | Mốc máu 66% và 33% đổi hành vi: thêm biến thể chiêu, đổi nhịp, có khoảnh khắc chuyển phase (gầm, rung đất, bất khả xâm phạm 1,5 s). Người chơi thấy trùm "thay đổi". | 66%: tinh thể mọc (đã có) + Húc Đầu nối 4 đòn thành 5, Giáng Thạch thêm cột thứ 5. 33%: đi nhanh hơn, bớt đi vòng, Quét Tay nối Quét Tay. | Nhỏ-vừa |
| 2 | **Thăng bằng (poise) và choáng** | Mỗi trúng đòn tích điểm; đầy thì trùm choáng/khụy, mở cơ hội đánh mạnh. Chiêu có "áo giáp" thì không bị cắt ngang. Thưởng cho người chơi biết chọn lúc đánh. | Đâm tường (đã có) vẫn choáng; thêm: trúng liên tục khi nó nghỉ thở thì ngã nhào. | Vừa |
| 3 | **Điểm yếu và bộ phận** | Lõi ngực lộ ra khi nghỉ thở hoặc sau Lõi Nổ (sát thương x2); tinh thể trên lưng vỡ được, vỡ thì mất giảm sát thương. | Đánh vào tinh thể lưng/vai làm rơi vật phẩm; lõi ngực là điểm yếu lúc khụy. | Vừa-lớn |
| 4 | **Đọc người chơi (AI thích nghi)** | Trùm đếm hành vi gần đây: đứng xa thì ném/Giáng Thạch, dí sát thì Quét Tay, chạy vòng thì Húc Đầu, núp sau tường thì phá hoặc đi vòng; né cùng một hướng 3 lần thì dự đoán. Trọng số chiêu theo tình huống thay vì ngẫu nhiên. | Hàm thuần `ReadRules.Weights(situation)` có test, thay chọn ngẫu nhiên trong `MoveScheduler.Pick`. | Vừa (dễ test thuần) |
| 5 | **Môi trường là vũ khí** | Dụ trùm đâm vào cột/tường (choáng), đá của nó phá được vật che, gai đá Giáng Thạch ở lại làm vật cản/che chắn tạm. | Cột Giáng Thạch còn đứng sau khi trùm ném xong thì chặn đường chạy của Húc Đầu (nó vấp, choáng). | Vừa |
| 6 | **Thông điệp thị giác, âm thanh và nhạc** | Màu báo hiệu thống nhất (đỏ = né, vàng = nhảy/chặn, xanh = không chặn được); nhạc nền có lớp tăng dần theo phase; lời nói/tiếng gầm cá tính khi vào phase. | Nhạc `golem_battle` có bản phase 2 (nhanh hơn); tiếng gầm vào phase. | Nhỏ-vừa |

## Thêm (nếu còn sức)
- **Tính cách và hành vi sinh thái:** trùm què khi máu thấp, rút lui hồi máu, đi tuần giữ lãnh thổ, săn con khác trong hang; mỗi trùm một nhịp "căng - thả".
- **Co-op:** chọn mục tiêu theo vai (gần nhất, yếu nhất, người đang hồi máu), bị kéo aggro, chiêu hất người khác mục tiêu.
- **Phần thưởng theo cách thắng:** vật phẩm khác nhau nếu phá bộ phận trước, thắng không bị trúng đòn nào, v.v.

## Thứ tự đề xuất
1. **Phase 1 (nền):** trụ 1 + 6 (phase và nhạc/tiếng) làm cho Golem, rồi nhân cho Chim và Chó Xương. Công cụ chung: `PhaseRules` thuần có test.
2. **Phase 2:** trụ 4 (đọc người chơi) và 2 (poise): đổi `MoveScheduler.Pick` và thêm điểm choáng.
3. **Phase 3:** trụ 3 và 5 (bộ phận yếu, môi trường).

Mỗi trụ cột làm theo TDD (test thuần trước), thử trên Golem trước rồi mới nhân sang boss khác. Mọi thứ phía Unity chỉ kiểm chứng được khi chủ dự án test.

## Cần chủ dự án quyết
1. Trụ cột nào làm trước? (đề xuất: 1 + 4)
2. Độ khó mong muốn: trùm nên **bắt người chơi học và thích nghi** (khó hơn) hay **chỉ đa dạng hơn** (dễ chịu hơn)?
3. Có muốn **điểm yếu bộ phận** (trụ 3) không? (kéo theo thêm model hoặc vùng va chạm cho từng bộ phận)
