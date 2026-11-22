# n8n: thông báo GitHub lên Telegram (bản đầu, chưa thử trên n8n thật)

Workflow `github-to-telegram.json` nhận webhook của GitHub và nhắn Telegram khi: **có push** lên `claude/**` hoặc `main` (kèm số commit và số file đổi), **PR mở hoặc đóng hoặc gộp**, và **test GitHub Actions đỏ**. Các sự kiện khác (kể cả test xanh) bị bỏ qua để khỏi spam.

## Cài đặt (khoảng 15 phút)
1. **Telegram bot:** nhắn `@BotFather` lệnh `/newbot`, lấy **token**. Nhắn bot của bạn một tin bất kỳ, rồi mở `https://api.telegram.org/bot<TOKEN>/getUpdates` để lấy **chat id** (`message.chat.id`).
2. **n8n** cần một **địa chỉ công khai** để GitHub gọi vào: dùng n8n Cloud (đơn giản nhất), hoặc n8n chạy trên máy bạn kèm một đường hầm (cloudflared hoặc ngrok).
3. Trong n8n: **Workflows → Import from file** → chọn `github-to-telegram.json`.
4. Mở node **GitHub webhook**: đổi `Path` thành một chuỗi **dài và ngẫu nhiên** (ví dụ `evolve-3f9a...`); ai biết đường dẫn này là gửi được tin giả, nên đừng chia sẻ. Mở node **Telegram**: tạo credential bằng token ở bước 1 và điền `Chat ID`.
5. Bấm **Activate**, rồi copy **Production URL** của node GitHub webhook.
6. Trên GitHub: repo → **Settings → Webhooks → Add webhook**: `Payload URL` = Production URL; `Content type` = `application/json`; chọn **"Let me select individual events"** và bật **Pushes**, **Pull requests**, **Workflow runs**; lưu.
7. Thử: đẩy một commit nhỏ lên nhánh `claude/...` và xem Telegram.

## Giới hạn đã biết
- **Chưa kiểm chứng**: tôi viết tệp JSON mà không chạy được n8n; tên node hoặc phiên bản có thể cần chỉnh khi nhập.
- Bảo vệ chỉ bằng đường dẫn bí mật, chưa kiểm tra chữ ký GitHub (`X-Hub-Signature-256`). Muốn chặt hơn: thêm secret vào webhook và kiểm tra HMAC trong node Code (cần bật `NODE_FUNCTION_ALLOW_BUILTIN=crypto`).
- **Chưa có** phát hiện hai nhánh sửa cùng một file (cần gọi GitHub API bằng token); làm sau nếu cần.
