# Cấu Trúc Thư Mục

```
.
├── api/
│   ├── src/
│   │   ├── Api.Web/            ← HTTP layer (endpoints, middleware, DI setup)
│   │   ├── Api.UseCases/       ← Application logic (commands, queries, handlers, DTOs)
│   │   ├── Api.Core/           ← Domain (entities, aggregates, domain events, specs)
│   │   └── Api.Infrastructure/ ← Data access (EF Core, Identity, JWT, email)
│   └── tests/
│       ├── Api.UnitTests/
│       ├── Api.IntegrationTests/
│       └── Api.FunctionalTests/
├── admin/
│   └── src/
│       ├── main.js             ← App entry, hydrate auth từ refresh token
│       ├── App.vue
│       ├── router/index.js     ← Route guards theo claims/role
│       ├── stores/             ← Pinia: auth.js, theme.js, tableState.js
│       ├── services/           ← Axios wrappers theo feature
│       ├── views/              ← Pages theo feature
│       ├── components/         ← Shared components (AppTable.vue, ...)
│       ├── composables/        ← useTableCache.js, usePermission.js, ...
│       ├── layout/             ← Layout.vue, nav.js, ui.js
│       └── plugins/            ← PrimeVue, Iconify setup
├── client/
│   └── src/                    ← Cấu trúc tương tự admin
├── docs/
│   ├── rules/                  ← Claude instructions (load qua @import)
│   ├── wiki/                   ← Knowledge base (đọc khi cần)
│   ├── raw/                    ← Nguyên liệu thô (gitignored)
│   └── outputs/                ← Phân tích, Q&A (gitignored)
└── docker-compose.dev.yml
```

## Api.Web — Endpoints theo feature

```
api/src/Api.Web/Endpoints/
├── Auth/         ← Login, Register, RefreshToken, Logout, ChangePassword, CheckUsername
├── Users/        ← CRUD + activate/deactivate/roles/reset-password (admin)
├── Staff/        ← Tạo tài khoản staff, deactivate (staff-level)
├── Roles/        ← Quản lý roles và claims
├── Products/     ← CRUD + toggle active
├── Categories/   ← CRUD + activate/deactivate
├── Tables/       ← CRUD + toggle active + mark available
├── Orders/       ← Tạo/đọc/cập nhật orders
├── Sessions/     ← Guest session cho khách đặt món
├── Menu/         ← Public menu (không cần auth)
└── Health/       ← Health check endpoint
```

## Api.Core — Domain Aggregates

```
api/src/Api.Core/Aggregates/
├── TableAggregate/        ← Table, TableStatus (Available/Occupied/Cleaning)
├── ProductAggregate/      ← Product với options (nhiệt độ, đá, đường)
├── CategoryAggregate/     ← Category sản phẩm
├── OrderAggregate/        ← Order + OrderItem
├── CustomerAggregate/     ← Customer profile, loyalty tier
└── GuestSessionAggregate/ ← Session cho khách vãng lai
```

## Api.UseCases — Commands/Queries theo feature

```
api/src/Api.UseCases/
├── Auth/          ← Login, Register, DeactivateUser, ActivateUser, ...
├── Tables/
│   ├── Create/    ← CreateTableCommand.cs + CreateTableHandler.cs
│   ├── List/      ← ListTablesQuery.cs + ListTablesHandler.cs
│   ├── Update/
│   ├── Delete/
│   └── DTOs/      ← TableDto.cs
├── Products/
├── Categories/
├── Orders/
└── Common/        ← Shared behaviors (ValidationBehavior)
```
