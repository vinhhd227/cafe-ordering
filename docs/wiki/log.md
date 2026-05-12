# log.md

Lịch sử thay đổi wiki. Append-only — không xóa dòng cũ.

Format: `YYYY-MM-DD | [action] | mô tả`

---

2026-04-07 | INIT | Khởi tạo cấu trúc wiki
2026-04-07 | CREATE | architecture.md — kiến trúc tổng thể, tech stack, Clean Architecture, database schema
2026-04-07 | CREATE | domain-model.md — aggregates: Table, GuestSession, Order, OrderItem, Product, Category, Customer, Zone, Expense
2026-04-07 | CREATE | auth-flow.md — JWT access token + refresh token, roles, permissions, route guards
2026-04-07 | CREATE | order-flow.md — vòng đời đơn hàng: QR flow, admin manual, state machine, merge/split, payment
2026-04-07 | CREATE | session-flow.md — GuestSession lifecycle, Table status machine, QR vs Manual session
2026-04-07 | CREATE | promotions.md — auto-apply, manual code, PromotionCalculator, StackPolicy, OrderPromotion snapshot
2026-04-07 | CREATE | notifications.md — NotificationType, NotificationConfig, SSE, push notification, cleanup service
2026-04-07 | CREATE | frontend-patterns.md — auto-import, Tailwind prefix, PrimeVue, Axios interceptors, Pinia auth store
2026-04-07 | CREATE | api-conventions.md — FastEndpoints, CQRS mediator, Result pattern, Specification, Guard clauses
2026-04-07 | CREATE | devops.md — Docker environments, EF migrations (2 DbContext), seed data, deploy scripts
2026-04-07 | UPDATE | index.md — cập nhật mục lục với 10 file wiki
2026-04-07 | LINK   | architecture.md ↔ domain-model.md ↔ order-flow.md ↔ session-flow.md ↔ promotions.md ↔ notifications.md ↔ frontend-patterns.md ↔ api-conventions.md ↔ devops.md ↔ auth-flow.md
2026-04-08 | UPDATE | auth-flow.md — fix login path, expiresAt semantics, app-level permission check, JWT claims, Secure flag, dedup mechanism
2026-04-08 | UPDATE | domain-model.md — fix DrinkTemperature (no Warm), IceLevel (no None), PaymentMethod (Cash/BankTransfer only); thêm OrderDate/CompletedAt/PaidAt; thêm admin behaviors
2026-04-08 | UPDATE | order-flow.md — fix PaymentMethod values; thêm endpoints: DELETE /{id}, PUT /{id}/items, PATCH /{id}/order-date, GET /stream
2026-05-11 | UPDATE | domain-model.md — rename ProductOptionGroup/Value → ProductAttributeGroup/Value; CategoryId nullable; thêm CostPrice, DiscountPrice, Sku, Barcode vào Product; cập nhật behaviors và API examples
