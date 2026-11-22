# PROJECT EVOLVE: quy tắc cho mọi phiên làm việc (Claude Code)

Đây là game slime co-op trong hang (Unity 6 URP). Chủ dự án nói tiếng Việt: **báo cáo và tài liệu bằng tiếng Việt; mã và chú thích mã bằng tiếng Anh.** Nhiều phiên cùng làm trên một nhánh, nên:

1. **Đọc `docs/PROJECT_BOARD.md` trước khi làm gì** (khi phiên bắt đầu, `tools/session_brief.sh` tự in bảng đó và những commit mới của phiên khác). Nó nói ai sở hữu file nào, trùm nào đang ở đâu và việc tồn đọng. Khi xong hoặc đổi việc, **sửa dòng của phiên bạn trong bảng đó** (một dòng, ngắn).
2. **Nhánh:** làm trên `claude/unity-test-maze` (gộp từ `claude/movement`). Chạy `git pull --no-rebase origin claude/unity-test-maze` **trước khi sửa và trước khi push**, vì phiên khác và nhóm cave cũng đẩy vào đó. Không tạo PR trừ khi chủ dự án bảo. Không đẩy lên nhánh khác.
3. **Chạy `tools/cstest/run.sh` trước khi push** (cần .NET 8, phải ra `Passed!`). GitHub cũng tự chạy lại ở `.github/workflows/tests.yml` sau mỗi push; nếu nó đỏ do commit của bạn thì sửa ngay.
4. **Logic thuần C#** (`Assets/_Project/Scripts/**/Pure/`) làm theo **TDD**: viết test, thấy đỏ, rồi mới viết mã, thấy xanh.
5. **Phần Unity không chạy được trong môi trường làm việc**: mọi thứ về cảnh, prefab, menu Editor, hiển thị là "chưa kiểm chứng", ghi thẳng điều đó trong báo cáo; chủ dự án test trong Unity và gửi Console hoặc ảnh. Checklist test: `docs/TEST_CHECKLIST_UNITY.md`.
6. **File sở hữu:** mỗi phiên chỉ sửa file thuộc phần việc của mình (bảng trong `PROJECT_BOARD.md`). File dùng chung (`MovesetSeed.cs`, `PassiveRules.cs`, `CreatureCombat.cs`, `BossKitTests.cs`, các docs chung) chỉ **thêm một khối nhỏ của mình**, commit nhỏ, đẩy sớm. Không viết lại file của phiên khác.
7. **Không bao giờ commit khóa API** (ElevenLabs, Meshy): chúng nằm trong secret của môi trường. **Hỏi chủ dự án trước mỗi lần tạo Meshy** (kèm số credit); đề xuất bộ chiêu hoặc hình dáng trùm và chờ duyệt trước khi dựng.
8. **Bộ chiêu chuẩn của trùm:** 2 đánh thường gần, 1 đánh xa (từ 5 m), 1 nội tại, 1 đặc biệt có thử thách (né, không mini game ấn nút), 1 ultimate khi máu ≤ 25%, tối đa 1 chiêu combo. Chi tiết: `docs/superpowers/specs/2026-11-21-boss-standard-kit.md`. Bộ kiểm tra: `BossKitTests`.
9. Không tạo sub-agent hay phiên phụ nếu chủ dự án không yêu cầu.
