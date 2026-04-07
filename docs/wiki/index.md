---
title: Wiki Index — Cafe Ordering System
tags: [index]
updated: 2026-04-07
---

# Wiki — Cafe Ordering System

Hệ thống đặt món quán cafe. Monorepo gồm backend .NET 10 + 2 frontend Vue 3.

---

## Bắt đầu nhanh

1. Đọc [[architecture]] để nắm tổng thể hệ thống
2. Đọc [[devops]] để setup môi trường dev
3. Đọc [[domain-model]] để hiểu các entity chính

---

## Mục lục

| File | Nội dung |
|------|----------|
| [[architecture]] | Kiến trúc tổng thể, tech stack, Clean Architecture layers, sơ đồ hệ thống |
| [[domain-model]] | Tất cả domain aggregates & entities (Table, Order, Session, Product...) |
| [[auth-flow]] | JWT auth, refresh token, roles & permissions, route guards |
| [[order-flow]] | Vòng đời đơn hàng: tạo, xử lý, hoàn thành, thanh toán, merge/split |
| [[session-flow]] | GuestSession & Table lifecycle: QR scan, mở/đóng bàn |
| [[promotions]] | Hệ thống khuyến mãi: auto-apply, manual code, tính discount |
| [[notifications]] | Thông báo realtime: SSE, push notification, notification configs |
| [[frontend-patterns]] | Vue conventions: auto-import, Tailwind prefix, Axios service, Pinia |
| [[api-conventions]] | Backend conventions: FastEndpoints, CQRS, Result pattern, Specification |
| [[devops]] | Docker, environments, EF migrations, deploy scripts |

---

## Cấu trúc repo tóm tắt

```
cafe-ordering/
├── api/src/
│   ├── Api.Core/           ← Domain layer (entities, aggregates, specs)
│   ├── Api.UseCases/       ← Application layer (commands, queries, handlers)
│   ├── Api.Infrastructure/ ← Infrastructure (EF Core, Identity, services)
│   └── Api.Web/            ← HTTP layer (FastEndpoints, middleware)
├── admin/                  ← Vue 3 dashboard (staff/admin)
├── client/                 ← Vue 3 app khách đặt món
├── docs/                   ← Tài liệu gốc (auth-flow, promotions, notifications, rules)
└── WIKI_PIPELINE.md        ← Hướng dẫn quy trình wiki này
```
