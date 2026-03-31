---
name: commit
description: Tạo git commit tự động theo Conventional Commits dựa trên các thay đổi hiện tại. Dùng khi user muốn commit code.
argument-hint: "[message tùy chọn]"
allowed-tools: Bash
---

Tạo git commit cho các thay đổi hiện tại theo quy tắc sau:

## Quy tắc commit (từ git.md của project)

Format: `type: mô tả ngắn gọn`
- Lowercase, không viết hoa chữ đầu
- Không dấu chấm cuối
- Không scope (KHÔNG viết `feat(tables): ...`)
- Ngắn gọn, mô tả được **cái gì** và **tại sao**

Types: `feat`, `fix`, `chore`, `docs`, `refactor`, `test`, `style`

## Các bước thực hiện

1. Chạy `git status` để xem untracked files
2. Chạy `git diff` để xem staged + unstaged changes
3. Chạy `git log --oneline -5` để xem style commit gần đây
4. Phân tích thay đổi và chọn type phù hợp
5. Stage tất cả thay đổi liên quan (dùng tên file cụ thể, KHÔNG dùng `git add -A` hoặc `git add .` trừ khi rõ ràng an toàn)
6. Tạo commit với message theo format trên

Nếu `$ARGUMENTS` được cung cấp, dùng làm gợi ý hoặc message trực tiếp (vẫn kiểm tra format).

**KHÔNG push** sau khi commit — chỉ commit local.

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>
