# log.md

Lá»‹ch sá»­ thay Ä‘á»•i wiki. Append-only â€” khÃ´ng xÃ³a dÃ²ng cÅ©.

Format: `YYYY-MM-DD | [action] | mÃ´ táº£`

---

2026-04-07 | INIT | Khá»Ÿi táº¡o cáº¥u trÃºc wiki
2026-04-07 | CREATE | architecture.md â€” kiáº¿n trÃºc tá»•ng thá»ƒ, tech stack, Clean Architecture, database schema
2026-04-07 | CREATE | domain-model.md â€” aggregates: Table, GuestSession, Order, OrderItem, Product, Category, Customer, Zone, Expense
2026-04-07 | CREATE | auth-flow.md â€” JWT access token + refresh token, roles, permissions, route guards
2026-04-07 | CREATE | order-flow.md â€” vÃ²ng Ä‘á»i Ä‘Æ¡n hÃ ng: QR flow, admin manual, state machine, merge/split, payment
2026-04-07 | CREATE | session-flow.md â€” GuestSession lifecycle, Table status machine, QR vs Manual session
2026-04-07 | CREATE | promotions.md â€” auto-apply, manual code, PromotionCalculator, StackPolicy, OrderPromotion snapshot
2026-04-07 | CREATE | notifications.md â€” NotificationType, NotificationConfig, SSE, push notification, cleanup service
2026-04-07 | CREATE | frontend-patterns.md â€” auto-import, Tailwind prefix, PrimeVue, Axios interceptors, Pinia auth store
2026-04-07 | CREATE | api-conventions.md â€” FastEndpoints, CQRS mediator, Result pattern, Specification, Guard clauses
2026-04-07 | CREATE | devops.md â€” Docker environments, EF migrations (2 DbContext), seed data, deploy scripts
2026-04-07 | UPDATE | index.md â€” cáº­p nháº­t má»¥c lá»¥c vá»›i 10 file wiki
2026-04-07 | LINK   | architecture.md â†” domain-model.md â†” order-flow.md â†” session-flow.md â†” promotions.md â†” notifications.md â†” frontend-patterns.md â†” api-conventions.md â†” devops.md â†” auth-flow.md
2026-04-08 | UPDATE | auth-flow.md â€” fix login path, expiresAt semantics, app-level permission check, JWT claims, Secure flag, dedup mechanism
2026-04-08 | UPDATE | domain-model.md â€” fix DrinkTemperature (no Warm), IceLevel (no None), PaymentMethod (Cash/BankTransfer only); thÃªm OrderDate/CompletedAt/PaidAt; thÃªm admin behaviors
2026-04-08 | UPDATE | order-flow.md â€” fix PaymentMethod values; thÃªm endpoints: DELETE /{id}, PUT /{id}/items, PATCH /{id}/order-date, GET /stream
2026-05-11 | UPDATE | domain-model.md â€” rename ProductOptionGroup/Value â†’ ProductVariantGroup/Value; CategoryId nullable; thÃªm CostPrice, DiscountPrice, Sku, Barcode vÃ o Product; cáº­p nháº­t behaviors vÃ  API examples
