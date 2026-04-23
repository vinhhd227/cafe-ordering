# CLAUDE.md — Cafe Ordering System

## Ngôn ngữ

- Trả lời bằng **tiếng Việt** trừ khi user hỏi bằng tiếng Anh
- Code, tên biến, tên file: luôn dùng tiếng Anh (theo chuẩn codebase)
- Comment trong code: tiếng Anh hoặc tiếng Việt tùy context

## Phong cách trả lời

- **Ngắn gọn, đi thẳng vào vấn đề** — không dẫn nhập dài dòng
- Không tóm tắt lại những gì vừa làm — user đọc được diff
- Không hỏi "Có muốn tôi tiếp tục không?" hay "Kết quả trông ổn không?" — cứ làm xong rồi báo cáo
- Chỉ giải thích khi logic không self-evident hoặc khi có trade-off quan trọng

## Dev Server & Long-running Process

- **Không tự khởi động** dev server, watch mode, hay bất kỳ process chạy dài
- **Hỏi trước** khi cần chạy: `pnpm dev`, `dotnet run`, `docker-compose up`, ...
- User quyết định khi nào start server

## Làm việc với Code

- **Đọc file trước** khi propose changes — không suggest sửa code chưa đọc
- Ưu tiên `Edit` tool (sửa file có sẵn) hơn `Write` (tạo mới)
- Không tạo file mới trừ khi thực sự cần thiết
- Không refactor code ngoài scope được yêu cầu
- Không thêm comments, type annotations, hay error handling cho code không được yêu cầu sửa

## Endpoint mới (.NET)

Khi tạo endpoint FastEndpoints mới, **luôn tạo kèm Summary class** trong cùng folder:
- Tên: `{EndpointName}Summary`
- Kế thừa: `Summary<{EndpointName}Endpoint>`
- Bắt buộc có: Summary, Description, ResponseExamples cho 200, tất cả Response codes có thể xảy ra

## Git / Commit

- **Không tự commit** sau mỗi thay đổi — chỉ commit khi user **yêu cầu rõ ràng**
- Không amend commit đã có — luôn tạo commit mới
- Không force push
- Không bypass hooks (`--no-verify`)

## Khi gặp vấn đề

- Không brute-force retry cùng một action khi bị block
- Tìm nguyên nhân gốc rễ, không workaround qua symptoms
- Nếu bị block và không biết hướng giải quyết → hỏi user

## Rules Files

@docs/rules/dotnet.md
@docs/rules/vue.md
@docs/rules/vue-list-page.md
@docs/rules/i18n.md
@docs/rules/git.md
@docs/rules/structure.md
@docs/rules/reference.md

## Wiki

Tài liệu tham khảo trong `docs/wiki/` — chỉ đọc khi task liên quan, không load tự động.

| File | Khi nào đọc |
|------|-------------|
| `docs/wiki/architecture.md` | Cần hiểu tổng thể hệ thống, tech stack, layers |
| `docs/wiki/domain-model.md` | Làm việc với entity/aggregate mới hoặc chưa quen |
| `docs/wiki/order-flow.md` | Liên quan đến lifecycle của Order |
| `docs/wiki/session-flow.md` | Liên quan đến GuestSession hoặc Table lifecycle |
| `docs/wiki/auth-flow.md` | Liên quan đến JWT, refresh token, permissions |
| `docs/wiki/promotions.md` | Liên quan đến hệ thống khuyến mãi |
| `docs/wiki/notifications.md` | Liên quan đến SSE, push notification |
| `docs/wiki/printing.md` | Liên quan đến in tem đồ uống, bill, cấu hình máy in thermal |
| `docs/wiki/api-conventions.md` | Cần ví dụ thêm về FastEndpoints/CQRS/Result |
| `docs/wiki/frontend-patterns.md` | Cần ví dụ thêm về Vue patterns, Pinia, Axios |
| `docs/wiki/devops.md` | Liên quan đến Docker, migrations, deploy |

## Tổng quan dự án

| Phần | Công nghệ | Mô tả |
|------|-----------|-------|
| `api/` | .NET 10 / ASP.NET Core | REST API backend |
| `admin/` | Vue 3 + Vite | Giao diện quản trị (staff/admin) |
| `client/` | Vue 3 + Vite | Giao diện khách hàng đặt món |

**Tech stack:** .NET 10, FastEndpoints 6, EF Core 10, PostgreSQL 16, ASP.NET Identity, Ardalis libraries — Vue 3, PrimeVue 4, Tailwind CSS 4, Pinia 3, Vee-Validate + Zod
