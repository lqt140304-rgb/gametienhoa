# Gameplay: các điểm cần chốt ý tưởng (để trao đổi với Codex)

**Ngày:** 2026-11-24. **Mục đích:** gom những chỗ gameplay còn mâu thuẫn hoặc chưa quyết, kèm đề xuất, để chủ dự án và Codex thống nhất **trước khi code thêm**. Mọi mục dưới đây là **đề xuất**, chưa chốt.

## Hiện trạng (đã có, đã có luật thuần + test)

Vồ → Ghì Chặt → Nuốt, Túi Không Đáy, Giải Mã, kỹ năng bị động (5 ô) và chủ động (phím 1–5), Biến Hình ở hang trú, bộ chiêu quái (Windup → Active → Recover), hệ sinh thái theo vùng. Chi tiết: `docs/SLIME_MECHANICS.md`.

## A. Mâu thuẫn giữa hai bản thiết kế (ưu tiên cao nhất)

`docs/GAME_DESIGN.md` (bản cũ) và `docs/superpowers/specs/2026-11-19-reincarnation-survival-design.md` (bản "chuyển sinh") nói khác nhau:

| Chủ đề | GAME_DESIGN (cũ) | Chuyển sinh (mới, code đang theo) |
|---|---|---|
| Chết | 3 mạng, cơ quan rơi, mỗi cái 50% mất | 3 lần tái tạo, để lại giọt lõi, mất đồ trong bụng, giữ kỹ năng |
| Hồi sinh co-op | Cứu trong 120 s, chuyền máu 3:1 | Đồng đội nuốt giọt lõi để hồi sinh ngay |
| Cơ quan | Nhặt, gắn lên socket, lên cấp bằng EXP | Không gắn; gene học được là **kỹ năng** |
| Co-op | 4–8 người | 2–4 người |
| Chỉ số | máu, đói, năng lượng, nhiệt, khát, 5 chất, bệnh | khối lượng + độ ẩm (2 nhu cầu) |

**Câu hỏi:** bản nào là nguồn đúng? Đề xuất: lấy bản chuyển sinh làm chuẩn, đánh dấu các mục cũ trong `GAME_DESIGN.md` là *đã thay thế*, và hỏi rõ chủ dự án hệ khát, 5 chất dinh dưỡng, bệnh tật có giữ hay bỏ.

## B. Vòng chơi một ván

1. Một vòng dài bao lâu? Thắng khi nào (lối ra chương 1?), thua khi nào?
2. Hết 3 lần tái tạo thì **mất kỹ năng** nhưng giữ sổ ký ức. Mức đó có quá nặng cho người mới? Đề xuất: lần chuyển sinh đầu tặng sẵn 1 kỹ năng gốc ngẫu nhiên.
3. Khối lượng vừa là máu, vừa là giới hạn nuốt, vừa là giá Biến Hình. Ba vai trò dùng chung một số có gây "kẹt" không (nhỏ đi thì yếu, yếu thì khó ăn để lớn lại)? Cần chốt cơ chế kéo ngược.

## C. Combat

1. Ghì Chặt kiểu câu cá: thử tay xem có đủ vui khi lặp lại hàng chục lần không? Có cần cách đánh thay thế (không mini game) cho quái nhỏ?
2. Chồn Khe có Vồ Xa, kỹ năng hang tác động lên mini game: chưa quyết kỹ năng nào ảnh hưởng thế nào.
3. Boss: là trận có giai đoạn riêng (đã chốt ở bestiary). Chưa có thiết kế giai đoạn, phần thưởng, hang ổ.
4. Co-op combat: client chưa thấy Ghì Chặt, kỹ năng client chưa tác động lên quái ở host. Quyết định: ai "sở hữu" mini game khi 2 người cùng bám một quái?

## D. Kỹ năng và tiến hóa

1. Kỹ năng có điều kiện (Đạn Nước, Tan Chảy): **chưa chốt cách mở khóa**.
2. Hơn 5 kỹ năng bị động thì chọn ô thế nào (đang dùng bảng G, tạm).
3. Dạng tiến hóa "không biết trước": công thức pha trộn (nuốt nhiều nhất, cách sống, Lõi, ngẫu nhiên) cần số cụ thể và cách cho người chơi **đoán được** mà không lộ cây.
4. Cân bằng Lõi (~12) và Khuyết Điểm (~10): bù trừ hay chấp nhận lệch?

## E. Mê cung và nhịp khám phá

1. Chu Kỳ Lũ, hang trú, chướng ngại "cơ thể là chìa khóa": tần suất và độ phạt khi bị lũ bắt gặp.
2. Không bản đồ: người chơi có công cụ nhớ đường nào (dấu bằng Phân Thân, vết nhờn)?
3. Bên làm hang và bên làm lõi đang chia việc: giao diện `EncounterRewards`, `IMazeAdjacency`, `drops` còn trống cần chốt chủ sở hữu.

## F. Đề xuất thứ tự làm (nếu Codex đồng ý)

1. Chốt mục A (không tốn code, chặn mọi thứ khác).
2. Vòng chết và chuyển sinh (giọt lõi, 3 lần tái tạo): thiếu nó thì chưa có "vòng chơi".
3. Thử tay Vồ/Ghì Chặt, chỉnh theo phản hồi (mục C1).
4. Tiến hóa kén + chọn Ân Huệ.
5. Co-op đồng bộ combat.

## Cách trao đổi gợi ý

Gửi Codex file này kèm `docs/SLIME_MECHANICS.md` và spec chuyển sinh; nhờ Codex trả lời từng mục theo dạng **chọn / sửa / bỏ** và ghi lại quyết định vào `GAME_DESIGN.md`. Lưu ý: môi trường cloud của mình không có Unity, nên mọi phần cảm giác tay phải do chủ dự án thử.
