---
name: commit-deploy
description: Commit thay đổi trên branch hiện tại, merge vào main, rồi deploy bằng docker-compose.prod.yml. Dùng khi sẵn sàng release lên production.
allowed-tools: Bash
---

⚠️ **Lệnh này sẽ deploy lên PRODUCTION.** Thực hiện theo đúng thứ tự sau và dừng lại nếu bất kỳ bước nào thất bại.

## Các bước thực hiện

### Bước 1 — Kiểm tra trạng thái
```bash
git status
git branch --show-current
git log --oneline -5
```
Ghi nhớ tên branch hiện tại (thường là `dev`).

### Bước 2 — Commit thay đổi
1. Xem `git diff` để phân tích thay đổi
2. Stage các file liên quan (dùng tên file cụ thể)
3. Tạo commit theo Conventional Commits:
   - Format: `type: mô tả ngắn gọn`
   - Lowercase, không dấu chấm, không scope
   - Types: `feat`, `fix`, `chore`, `docs`, `refactor`, `test`, `style`

### Bước 3 — Push branch hiện tại
```bash
git push  # hoặc git push -u origin <branch> nếu chưa có upstream
```

### Bước 4 — Merge vào main
```bash
git checkout main
git pull origin main          # sync main mới nhất
git merge <branch> --no-ff   # merge commit (giữ history theo git.md)
git push origin main
```

### Bước 5 — Deploy
```bash
cd /Users/vinhhd227/Source/cafe-ordering
docker-compose -f docker-compose.prod.yml up --build -d
```
Chờ các container khởi động. Kiểm tra health:
```bash
docker-compose -f docker-compose.prod.yml ps
```

### Bước 6 — Quay lại branch gốc
```bash
git checkout <branch-gốc-từ-bước-1>
```

### Bước 7 — Báo cáo kết quả
Tóm tắt: commit hash, branch đã merge, trạng thái các container.

---

**Nếu bất kỳ bước nào thất bại:** dừng lại ngay, báo cáo lỗi cho user, KHÔNG tự ý tiếp tục hoặc workaround.

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>
