# Di chuyển sinh vật và slime: gửi bên làm hang (2026-11-20)

## Phía lõi đã làm (nhánh `claude/movement`, đã gộp vào `claude/unity-test-maze`)
- `Creatures/CreatureMotor.cs` (Prey và Predator tự thêm): sinh vật đọc **collider thật** thay vì chỉ `GroundHeight`.
  - Tia dò sàn bắt đầu chỉ cao hơn chân một bậc, nên mặt cầu ở trên đầu không bị coi là sàn: hết vọt lên cầu.
  - Quét phía trước để gặp vách thì dừng hoặc đi vòng (±40°, ±80°).
  - Bậc cao hơn mức của loài thì không lên. Vực sâu hơn mức của loài thì không bước xuống.
  - Độ cao được làm mượt nên chỗ gồ trên sàn không làm giật.
  - Khi không tia nào trúng thì dùng lại `GroundHeight` (bộ lấy mẫu của khoang) như cũ.
- Đặc tính di chuyển theo loài nằm ở `Creatures/Pure/MovementRules.cs` (đi bộ, nhảy, leo, bay). Bảng được tra theo tên prefab không dấu.
- Prefab của các bạn **không cần sửa gì**.
- `CreatureAnimDriver` của các bạn đã tính tốc độ theo phương ngang, nên giữ nguyên.

## Nhờ các bạn
1. **Sàn khoang dùng collider mượt.** Sàn khoang hiện là MeshCollider nhiều gờ nhỏ, slime đi một bước lại vướng một gờ.
   - Nên dùng một MeshCollider riêng cho sàn, ít tam giác (lưới thô, đã làm mượt, không theo chi tiết đá nhỏ).
   - Phần trang trí đặt ở lớp không va chạm.
   - Đá nhỏ (dưới khoảng 0,4 m) có thể bỏ collider hoặc giữ cũng được: slime giờ tự bò qua chúng.
2. **Vách, bục, cầu phải có collider.** Sinh vật giờ chỉ dừng trước thứ có collider. Vật không có collider thì vẫn bị đi xuyên.
3. **Collider không đặt `isTrigger`** cho vách, đá, cầu: tia dò bỏ qua trigger.
4. **Collider của sinh vật** (trên prefab, cùng Rigidbody) giữ như hiện tại.
   - Tia dò bỏ qua mọi collider có Rigidbody, nên sinh vật không coi nhau hay slime là sàn hoặc vách.
   - Collider con không có Rigidbody riêng vẫn tính là gắn với Rigidbody cha, nên không sao.
5. **Bảng loài:** nếu có loài mới bay, nhảy hoặc leo, báo tên để thêm vào `MovementRules`. Loài không có trong bảng mặc định là đi bộ (bậc 0,35 m, xuống tối đa 1 m).
6. **`CreatureLocomotion.SamplePhysicalGroundY`** (của các bạn, đã gộp chung với `CreatureMotor` trên `claude/unity-test-maze`, hai cái không đụng nhau):
   - Tia đang bắt đầu cao hơn thân 1,8 m. Con đứng dưới cầu thấp hơn 1,8 m sẽ trúng mặt cầu, nên **mô hình** bị nâng hoặc nghiêng theo cầu (thân vật lý thì không).
   - Tia cũng có thể trúng sinh vật khác hoặc slime.
   - Đề xuất: bắt đầu tia ở khoảng `worldPos.y + 0.5f` và bỏ qua hit có `attachedRigidbody != null`, giống `CreatureMotor.Floor`.
