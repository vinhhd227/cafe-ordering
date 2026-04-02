# Git / PR Workflow

## Commit Messages — Conventional Commits

Format: `type: mô tả ngắn gọn` (lowercase, imperative mood)

```
feat: add QR token to tables for secure ordering
fix: increase nginx upload limit to 10MB for product images
chore: simplify dev environment and add backup scripts
docs: add authentication flow documentation
refactor: extract promotion calculator to separate service
test: add unit tests for order aggregate
style: format endpoint files
```

**Các type được dùng:**

| Type | Khi nào |
|------|---------|
| `feat` | Tính năng mới |
| `fix` | Sửa bug |
| `chore` | Maintenance, cấu hình, tooling |
| `docs` | Tài liệu |
| `refactor` | Tái cấu trúc code (không thêm feature, không fix bug) |
| `test` | Thêm/sửa tests |
| `style` | Format, whitespace (không thay đổi logic) |

**Quy tắc:**
- Không viết hoa chữ cái đầu
- Không có dấu chấm cuối
- Ngắn gọn, mô tả được **cái gì** và **tại sao** (không phải **như thế nào**)
- Không scope trong commit message (ví dụ KHÔNG viết `feat(tables): ...`)

## Branch Naming

```
feature/add-promotion-system
fix/order-payment-status-mapping
chore/update-dependencies
docs/authentication-flow
refactor/extract-jwt-service
```

- Dùng kebab-case
- Tên rõ ràng, mô tả được nội dung branch

## Nhánh chính

| Branch | Mục đích |
|--------|---------|
| `main` | Production-ready code |
| `dev` | Development, tích hợp features |

- Feature branches tách từ `dev`, merge vào `dev`
- `dev` merge vào `main` khi release
- **Không force push** lên `main` hoặc `dev`

## Pull Request

**Title:** Ngắn gọn <70 ký tự, mô tả rõ ràng

**Body template:**
```markdown
## Summary
- Mô tả ngắn gọn thay đổi (1-3 bullet points)

## Test plan
- [ ] Kiểm tra manually tính năng X
- [ ] Chạy unit tests
- [ ] Kiểm tra edge cases
```

**Quy tắc:**
- Mỗi PR tập trung vào 1 mục đích duy nhất
- PR nhỏ tốt hơn PR lớn — dễ review hơn
- Link issue nếu có: `Closes #123`
- Reviewer approve trước khi merge

## Merge Strategy

- `feature/*` → `dev`: **rebase** rồi fast-forward
  ```bash
  git checkout feature/my-feature
  git rebase dev
  git checkout dev
  git merge --ff-only feature/my-feature
  ```
- `dev` → `main`: **fast-forward only** (dev phải là linear extension của main)
  ```bash
  git checkout main
  git merge --ff-only dev
  git push origin main
  ```

**Quy tắc chung:**
- Không dùng merge commit — giữ history tuyến tính
- Rebase trước khi merge vào branch đích
- Không force push lên `main` hoặc `dev` (trừ trường hợp đặc biệt có thỏa thuận)
