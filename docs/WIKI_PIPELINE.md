# WIKI_PIPELINE.md

Hướng dẫn quy trình quản lý wiki cho AI assistant (Claude).  
Paste file này vào đầu conversation khi muốn làm việc với wiki.

---

## Vai trò

- **Claude** — đọc raw material, tổng hợp, viết và cập nhật `wiki/`, quản lý liên kết và index
- **Người dùng** — dump raw material vào `raw/`, review wiki, đặt câu hỏi
- **Gemma4:e4b (local)** — chỉ đọc `wiki/` để trả lời nhanh trong lúc code

---

## Cấu trúc thư mục

```
project-root/
├── raw/              ← Nguyên liệu thô (gitignore)
├── wiki/             ← Claude maintain, commit lên git
│   ├── index.md      ← Mục lục tổng, cập nhật mỗi khi có file mới
│   └── log.md        ← Lịch sử thay đổi wiki (append-only)
├── outputs/          ← Phân tích, Q&A quan trọng (gitignore hoặc commit tùy)
└── WIKI_PIPELINE.md  ← File này
```

---

## Quy trình

### 1. Người dùng dump raw material
Paste trực tiếp vào chat, hoặc mô tả bằng lời. Không cần format đẹp.  
Ví dụ: architecture decisions, domain notes, bug đã fix, convention đã chọn.

### 2. Claude xử lý
Khi nhận raw material, Claude sẽ:
- Phân loại nội dung vào đúng file wiki (tạo file mới nếu cần)
- Viết hoặc cập nhật file `.md` tương ứng trong `wiki/`
- Dùng `[[wiki-links]]` để liên kết giữa các bài
- Cập nhật `wiki/index.md`
- Ghi một dòng vào `wiki/log.md`

### 3. Khi dữ liệu thay đổi
Người dùng thông báo thay đổi → Claude cập nhật file liên quan, giữ nguyên các phần còn đúng.

### 4. Review định kỳ (tùy chọn)
Yêu cầu Claude: *"Review toàn bộ wiki, tìm mâu thuẫn, chỗ thiếu liên kết, hoặc thông tin cũ."*

---

## Convention file wiki

```markdown
---
title: Tên bài
tags: [domain, architecture, convention, ...]
updated: YYYY-MM-DD
---

Nội dung...
```

- Tên file: `kebab-case.md` (ví dụ: `domain-model.md`, `order-flow.md`)
- Backlinks dùng cú pháp Obsidian: `[[tên-file]]`
- Mỗi file có YAML frontmatter với `title`, `tags`, `updated`
- Không viết quá dài — mỗi file tập trung một khái niệm

---

## Format log.md

```
YYYY-MM-DD | [action] | mô tả ngắn
```

Ví dụ:
```
2026-04-07 | CREATE | domain-model.md — entities Order, MenuItem, Table
2026-04-07 | UPDATE | architecture.md — thêm quyết định dùng FastEndpoints
2026-04-07 | LINK   | order-flow.md ↔ domain-model.md
```

---

## Lưu ý cho Claude

- Không tự ý xóa thông tin cũ — nếu thông tin thay đổi, ghi rõ phần nào đã deprecated
- Ưu tiên liên kết hơn là lặp lại nội dung
- `index.md` phải luôn phản ánh đúng các file đang tồn tại
- Nếu raw material mâu thuẫn với wiki hiện tại, hỏi người dùng trước khi ghi đè
