---
title: Kiến trúc hệ thống
tags: [architecture, overview, tech-stack]
updated: 2026-04-07
---

# Kiến trúc hệ thống

Cafe Ordering là **monorepo** gồm 3 service chính, chạy trên Docker.

---

## Sơ đồ tổng thể

```
┌─────────────────────────────────────────────────────────────────┐
│                          MONOREPO                               │
│                                                                 │
│  ┌──────────────┐   ┌──────────────┐   ┌────────────────────┐   │
│  │   /client    │   │    /admin    │   │       /api         │   │
│  │  Vue 3+Vite  │   │  Vue 3+Vite  │   │    .NET 10 API     │   │
│  │  Khách hàng  │   │  Staff/Admin │   │  Clean Architecture│   │
│  │  :5173       │   │  :5174       │   │  :8080             │   │
│  └──────┬───────┘   └──────┬───────┘   └────────┬───────────┘   │
│         └──────────────────┴───────────────────►│               │
│                                                  │              │
│                                          ┌───────▼───────┐      │
│                                          │  PostgreSQL   │      │
│                                          │   :5432       │      │
│                                          │  schema:      │      │
│                                          │  business     │      │
│                                          │  identity     │      │
│                                          └───────────────┘      │
└─────────────────────────────────────────────────────────────────┘
```

---

## Tech Stack

| Layer | Công nghệ |
|-------|-----------|
| **Backend** | .NET 10, ASP.NET Core, FastEndpoints 6 |
| **ORM** | Entity Framework Core 10 |
| **Database** | PostgreSQL 16 |
| **Auth** | ASP.NET Identity + JWT (HS256) |
| **CQRS** | Mediator source generator |
| **Domain** | Ardalis SharedKernel, Ardalis Result, Ardalis Specification |
| **Logging** | Serilog |
| **Frontend** | Vue 3 (Composition API), Vite 7 |
| **UI** | PrimeVue 4, Tailwind CSS 4 |
| **State** | Pinia 3 |
| **Validation** | Vee-Validate + Zod |
| **HTTP Client** | Axios 1 |
| **Container** | Docker + docker-compose |

---

## Clean Architecture (Backend)

Phụ thuộc chỉ đi vào trong: `Web → UseCases → Core ← Infrastructure`

```
api/src/
├── Api.Core/           ← Domain layer
│   ├── Aggregates/     ← Entities, value objects, domain events, specs
│   ├── Entities/       ← Base classes (AuditableEntity, SoftDeletableEntity)
│   ├── Exceptions/     ← DomainException
│   └── Interfaces/     ← IRepository (từ Ardalis SharedKernel)
│
├── Api.UseCases/       ← Application layer
│   ├── [Feature]/      ← Commands, Queries, Handlers, DTOs theo feature
│   └── Common/         ← ValidationBehavior (Mediator pipeline)
│
├── Api.Infrastructure/ ← Infrastructure layer
│   ├── Data/           ← EF Core, AppDbContext, migrations business
│   ├── Identity/       ← AppIdentityDbContext, IdentityService, JwtService
│   └── Services/       ← NotificationService, EmailService, ...
│
└── Api.Web/            ← Presentation layer
    ├── Endpoints/      ← FastEndpoints theo feature
    ├── Configurations/ ← Auth, CORS, Swagger, Middleware
    ├── Extensions/     ← ResultExtensions (map Ardalis.Result → HTTP)
    └── Program.cs      ← Entry point, DI registration
```

---

## Database Schema

PostgreSQL dùng **2 schema riêng biệt**:

- `business` — dữ liệu nghiệp vụ: Tables, Orders, Products, Categories, Sessions, Promotions, Notifications, Expenses, Zones, WifiProfiles
- `identity` — user management: AspNetUsers, AspNetRoles, AspNetRoleClaims, RefreshTokens

Hai EF Core context tương ứng:
- `AppDbContext` → schema `business`
- `AppIdentityDbContext` → schema `identity`

Xem thêm: [[devops]] (migrations), [[auth-flow]] (identity schema)

---

## Giao tiếp realtime

Hệ thống dùng **SSE (Server-Sent Events)** để push thông báo real-time từ backend đến admin frontend:
- Khi có đơn mới → SSE event → admin app cập nhật danh sách đơn và badge thông báo

Xem thêm: [[notifications]]

---

## Luồng tổng quát

1. **Khách** quét QR → mở `/client` → chọn bàn → đặt món → tạo `GuestSession` + `Order`
2. **Backend** nhận order → lưu DB → SSE broadcast đến admin
3. **Staff/Admin** xem trên `/admin` → xử lý order → cập nhật trạng thái
4. **Admin** có thể tạo order thủ công, quản lý sản phẩm, bàn, khuyến mãi

Xem thêm: [[order-flow]], [[session-flow]], [[domain-model]]
