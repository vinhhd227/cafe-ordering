---
name: commit-push
description: Tạo git commit và push lên remote branch hiện tại. Dùng khi user muốn commit và đẩy code lên remote.
argument-hint: "[message tùy chọn]"
allowed-tools: Bash
---

Tạo git commit và push lên remote theo quy tắc sau:

## Quy tắc commit (từ git.md của project)

Format: `type: mô tả ngắn gọn`
- Lowercase, không viết hoa chữ đầu
- Không dấu chấm cuối
- Không scope (KHÔNG viết `feat(tables): ...`)

Types: `feat`, `fix`, `chore`, `docs`, `refactor`, `test`, `style`

## Các bước thực hiện

1. Chạy `git status` và `git diff` để xem thay đổi
2. Chạy `git log --oneline -5` để xem style commit gần đây
3. Chạy `git branch --show-current` để xem branch hiện tại
4. Phân tích thay đổi, chọn type và viết message
5. Stage các file liên quan (dùng tên file cụ thể)
6. Tạo commit với message theo format
7. Push lên remote: `git push` (nếu branch chưa có upstream: `git push -u origin <branch>`)
8. Báo cáo kết quả push

**KHÔNG force push** (`--force` hoặc `--force-with-lease`).
**KHÔNG push** lên `main` hoặc `dev` trực tiếp — nếu đang ở main/dev, cảnh báo user trước khi tiếp tục.

Nếu `$ARGUMENTS` được cung cấp, dùng làm gợi ý message.

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>
