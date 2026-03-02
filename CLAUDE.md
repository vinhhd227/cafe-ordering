# CLAUDE.md — Cafe Ordering System

## Rules

- Do NOT run dev server or preview automatically
- Ask me first before running any long-running process (dev server, watch mode, etc.)
- I will decide when to start the preview server
- When creating a new endpoint, always create a corresponding Summary class in the same folder
  + Class name: `{EndpointName}Summary`
  + Inherit from `Summary<{EndpointName}Endpoint>`
  + Must include: Summary, Description, ResponseExamples for 200, and all possible Response codes with descriptions

## 1. Project Overview

Hệ thống đặt món quán cafe với kiến trúc monorepo gồm 3 phần:

| Phần | Công nghệ | Mô tả |
|------|-----------|-------|
| `api/` | .NET 10 / ASP.NET Core | REST API backend |
| `admin/` | Vue 3 + Vite | Giao diện quản trị (staff/admin) |
| `client/` | Vue 3 + Vite | Giao diện khách hàng đặt món |

**Tech stack chính:**
- **Backend:** .NET 10, FastEndpoints 6, Entity Framework Core 10, PostgreSQL 16, ASP.NET Identity, JWT, Serilog, Ardalis libraries (Result, Specification, SharedKernel), Mediator source generator
- **Frontend:** Vue 3 (Composition API), Vite 7, PrimeVue 4, Tailwind CSS 4, Pinia 3, Vee-Validate + Zod, Axios 1
- **Database:** PostgreSQL với 2 schema riêng: `business` (dữ liệu nghiệp vụ) và `identity` (users/roles)
- **Container:** Docker + docker-compose

---

## 2. Cấu Trúc Thư Mục

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
├── docker-compose.dev.yml
└── CLAUDE.md
```

### Api.Web — Endpoints theo feature

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

### Api.Core — Domain Aggregates

```
api/src/Api.Core/Aggregates/
├── TableAggregate/        ← Table, TableStatus (Available/Occupied/Cleaning)
├── ProductAggregate/      ← Product với options (nhiệt độ, đá, đường)
├── CategoryAggregate/     ← Category sản phẩm
├── OrderAggregate/        ← Order + OrderItem
├── CustomerAggregate/     ← Customer profile, loyalty tier
└── GuestSessionAggregate/ ← Session cho khách vãng lai
```

### Api.UseCases — Commands/Queries theo feature

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

---

## 3. Patterns & Conventions

### Backend Patterns

#### Clean Architecture
Phụ thuộc chỉ đi vào trong: `Web → UseCases → Core ← Infrastructure`

#### Endpoint (FastEndpoints)
```csharp
// Một endpoint = một file, đặt trong Endpoints/[Feature]/
public class CreateTable(IMediator mediator) : Endpoint<CreateTableRequest, TableDto>
{
    public override void Configure()
    {
        Post("/api/admin/tables");
        Policies("table.create");   // Policy-based authorization
        DontAutoTag();
        Description(b => b.WithTags("Tables"));
    }

    public override async Task HandleAsync(CreateTableRequest req, CancellationToken ct)
    {
        var result = await mediator.Send(new CreateTableCommand(req.Number, req.Code), ct);
        await this.SendResultAsync(result, ct);  // Extension method xử lý Ardalis.Result
    }
}
```
- Endpoint không có body: kế thừa `Ep.Req<TRequest>.NoRes` — **bắt buộc gửi `{}` body khi gọi PUT/POST từ frontend** (FastEndpoints 6 yêu cầu Content-Type: application/json ngay cả khi chỉ bind từ route)
- Endpoint có response: kế thừa `Endpoint<TRequest, TResponse>`
- Endpoint không cần request: `EndpointWithoutRequest<TResponse>`

#### CQRS (Mediator source generator)
```csharp
// Command
public record CreateTableCommand(int Number, string Code) : ICommand<Result<TableDto>>;

// Handler
public class CreateTableHandler(IRepositoryBase<Table> repo)
    : ICommandHandler<CreateTableCommand, Result<TableDto>>
{
    public async ValueTask<Result<TableDto>> Handle(CreateTableCommand cmd, CancellationToken ct)
    {
        // Dùng Specification để query
        var existing = await repo.FirstOrDefaultAsync(new TableByNumberSpec(cmd.Number), ct);
        if (existing is not null) return Result.Invalid(...);

        var table = Table.Create(cmd.Number, cmd.Code);  // Factory method trên aggregate
        await repo.AddAsync(table, ct);
        return Result.Success(new TableDto(...));
    }
}
```

#### Domain Entities
```csharp
// BaseEntity<TId> → AuditableEntity<TId> → SoftDeletableEntity<TId>
public class Table : SoftDeletableEntity<int>, IAggregateRoot
{
    public int Number { get; private set; }  // Setter private

    public static Table Create(int number, string code) { ... }  // Factory method
    public void Activate() { IsActive = true; }                   // Behavior methods
    public void OpenSession(Guid id) { RegisterDomainEvent(...); } // Domain events
}
```
- Tất cả setters là `private` — thay đổi state qua behavior methods
- Tạo entity qua static factory `Create(...)`
- Soft delete: `Delete()` / `Restore()` thay vì xóa vật lý

#### Result Pattern (Ardalis.Result)
```csharp
return Result.Success(dto);
return Result.NotFound();
return Result.Invalid(new ValidationError("field", "message"));
return Result.Error("something went wrong");
// this.SendResultAsync(result) tự map sang HTTP status code tương ứng
```

#### Specification Pattern
```csharp
// Đặt trong Aggregates/[Feature]Aggregate/Specifications/
public class TableByNumberSpec : SingleResultSpecification<Table>
{
    public TableByNumberSpec(int number) => Query.Where(t => t.Number == number);
}
```

#### Authorization
- Policy names khớp với permission claims trong JWT: `"table.create"`, `"product.read"`, `"user.deactivate"`, ...
- Role-based: `Admin`, `Staff`

---

### Frontend Patterns

#### Auto-imports (vite.config.js)
Các symbol sau **không cần import thủ công** trong bất kỳ `.vue` file nào:
```js
// Vue core
ref, reactive, computed, watch, onMounted, ... (tất cả vue APIs)
useRouter, useRoute (vue-router)
useField, useForm (vee-validate)
toTypedSchema (/@vee-validate/zod)
z (zod)

// Pinia
useStore

// PrimeVue
useToast, useConfirm

// UI constants (admin/src/layout/ui.js)
btnIcon, inputCustom, labelCustom, passwordCustom

// Từ src/composables/ và src/stores/ (tự động scan)
useTableCache, usePermission, useAuthStore, useThemeStore, ...
```

#### Axios Service Pattern
```js
// services/axios.js — singleton instance với interceptors
// - Request: tự động thêm Bearer token
// - Request: xóa Content-Type nếu không có body (tránh 415 khi retry)
// - Response: tự động refresh token khi 401, retry request gốc

// Mỗi feature có file service riêng:
// services/[feature].service.js
export const activateUser = (id) => api.put(`/admin/users/${id}/activate`, {})
//                                                                          ^^
//                               Luôn gửi {} cho PUT/POST không có body thực sự
```

#### Pinia Auth Store
- `hydrateFromRefresh()` gọi trong `main.js` khi khởi động app
- Token refresh tự động 30 giây trước khi hết hạn (`scheduleTokenRefresh`)
- `doRefreshToken()` dedup concurrent calls bằng `_refreshPromise` cache

#### Router Guards
- `requiresAuth: true` — redirect về `/login` nếu chưa đăng nhập
- `adminOnly: true` — kiểm tra role Admin
- `requiredClaim: "feature.action"` — kiểm tra trong `user.permissions` array

#### UI Components
- **PrimeVue** với prefix `prime-` (ví dụ: `<prime-button>`, `<prime-data-table>`)
- **Iconify** cho icons: `<iconify icon="ph:user-bold" />`
- **Tailwind** với prefix `tw:` để tránh conflict với PrimeVue: `class="tw:flex tw:gap-2"`
- **btnIcon** = `'tw:w-8! tw:h-8! tw:p-0! tw:flex tw:items-center tw:justify-center'` — dùng cho icon-only buttons

---

## 4. Các File Quan Trọng

### Backend

| File | Vai trò |
|------|---------|
| `api/src/Api.Web/Program.cs` | Entry point, DI registration |
| `api/src/Api.Web/Configurations/` | Auth, CORS, Swagger, Middleware config |
| `api/src/Api.Web/Extensions/ResultExtensions.cs` | Map `Ardalis.Result` → HTTP response |
| `api/src/Api.Infrastructure/Data/AppDbContext.cs` | EF Core context, schema `business`, audit trail |
| `api/src/Api.Infrastructure/Identity/AppIdentityDbContext.cs` | Identity context, schema `identity` |
| `api/src/Api.Infrastructure/Identity/JwtService.cs` | JWT generation/validation |
| `api/src/Api.Infrastructure/Identity/IdentityService.cs` | User management (create, activate, deactivate, ...) |
| `api/src/Api.Core/Entities/AuditableEntity[TId].cs` | Base với CreatedAt/UpdatedAt/CreatedBy/UpdatedBy |
| `api/src/Api.Core/Entities/SoftDeletableEntity[TId].cs` | Base với IsDeleted/DeletedAt soft delete |
| `api/src/Api.Web/appsettings.Development.json` | Config local (DB, JWT, SMTP, CORS) |

### Admin Frontend

| File | Vai trò |
|------|---------|
| `admin/src/main.js` | App entry, hydrate auth session |
| `admin/src/router/index.js` | Routes + guards (claims, roles) |
| `admin/src/stores/auth.js` | Auth state, token refresh logic |
| `admin/src/services/axios.js` | Axios instance + request/response interceptors |
| `admin/src/layout/ui.js` | UI class constants (btnIcon, inputCustom, ...) |
| `admin/src/layout/nav.js` | Navigation menu config |
| `admin/vite.config.js` | Vite config, auto-import setup, Tailwind, proxy |
| `admin/.env` | `VITE_API_BASE_URL=http://localhost:5095/api` |

---

## 5. Các Lệnh Thường Dùng

### Backend (.NET)

```bash
# Chạy API locally (phải đứng ở thư mục src/Api.Web/)
cd api/src/Api.Web
ASPNETCORE_ENVIRONMENT=Development dotnet run

# Build
dotnet build api/src/Api.Web/Api.Web.csproj

# Chạy tests
dotnet test api/tests/Api.UnitTests/
dotnet test api/tests/Api.IntegrationTests/
dotnet test api/tests/Api.FunctionalTests/

# Tạo migration mới (business DB)
cd api/src/Api.Web
dotnet ef migrations add <MigrationName> \
  --project ../Api.Infrastructure \
  --context AppDbContext \
  --output-dir Data/Migrations

# Tạo migration mới (identity DB)
dotnet ef migrations add <MigrationName> \
  --project ../Api.Infrastructure \
  --context AppIdentityDbContext \
  --output-dir Identity/Migrations

# Apply migration thủ công
dotnet ef database update --context AppDbContext
dotnet ef database update --context AppIdentityDbContext
```
> **Lưu ý:** Migration tự động apply khi app khởi động (trong `UseAppMiddlewareAndSeedDatabase()`).

### Frontend

```bash
# Admin
cd admin
pnpm install       # Cài dependencies (hoặc npm install)
pnpm dev           # Dev server tại http://localhost:5173
pnpm build         # Production build
pnpm preview       # Preview bản build

# Client
cd client
pnpm install
pnpm dev           # Dev server tại http://localhost:5174
pnpm build
```

### Docker

```bash
# Chạy toàn bộ stack
docker-compose -f docker-compose.dev.yml up --build

# Chỉ database
docker-compose -f docker-compose.dev.yml up cafe-db

# Override config (copy từ example trước)
cp docker-compose.dev.override.yml.example docker-compose.dev.override.yml
docker-compose -f docker-compose.dev.yml -f docker-compose.dev.override.yml up
```

---

## 6. Những Điều Cần Lưu Ý

### Backend

**FastEndpoints 6 và Content-Type:**
- Endpoint dùng `Ep.Req<T>.NoRes` (chỉ bind từ route) **vẫn yêu cầu** `Content-Type: application/json` khi gọi từ client PUT/POST.
- Frontend **phải gửi `{}` body** cho các endpoint không có body thực sự (activate, deactivate, ...).
- Nếu không, backend trả về `415 Unsupported Media Type`.

**Ardalis.Result mapping:**
- `Result.Success` → `200 OK` (hoặc `204 No Content` cho `.NoRes`)
- `Result.NotFound()` → `404 Not Found`
- `Result.Invalid(...)` → `400 Bad Request`
- `Result.Unauthorized()` → `401 Unauthorized`
- `Result.Forbidden()` → `403 Forbidden`
- `Result.Error(...)` → `500 Internal Server Error`

**Hai DbContext riêng biệt:**
- `AppDbContext` — dữ liệu nghiệp vụ (inject khi cần repository cho entities)
- `AppIdentityDbContext` — user/role/claims (inject khi cần `UserManager`, `RoleManager`)
- Migrations của hai context nằm ở hai thư mục khác nhau: `Data/Migrations/` và `Identity/Migrations/`

**Domain entities:**
- Không tạo entity bằng `new`, dùng static factory `Entity.Create(...)`
- Không set property trực tiếp, gọi behavior method (`Activate()`, `UpdateCode()`, ...)
- Domain events đăng ký qua `RegisterDomainEvent(new SomeEvent(...))`

**Authorization:**
- Permission claims lấy từ `RoleClaims` table trong DB (seeded khi startup)
- Tên policy = tên claim: `"table.create"`, `"product.read"`, `"user.deactivate"`, ...

### Frontend

**Axios và 415 khi retry:**
- Sau khi token hết hạn (401) → interceptor refresh token → retry request gốc
- Khi retry, Axios re-merge headers từ defaults, có thể thêm lại `Content-Type: application/json` dù không có body
- Interceptor trong `axios.js` đã xử lý: tự động xóa `Content-Type` nếu `config.data` là `null/undefined`
- **Luôn truyền `{}` cho PUT/POST endpoint không có body** (`activateUser`, `deactivateUser`, `resetUserPassword`)

**Auto-imports:**
- Không import thủ công các symbol đã liệt kê trong mục 3 (sẽ bị linter báo duplicate)
- Khi thêm composable mới vào `src/composables/` hoặc store vào `src/stores/`, tự động được scan
- `vueTemplate: true` cho phép dùng trong `<template>` không chỉ `<script setup>`

**Tailwind prefix `tw:`:**
- Tất cả Tailwind classes phải có prefix `tw:` để tránh conflict với PrimeVue
- Ví dụ: `tw:flex`, `tw:gap-2`, `tw:text-sm`, `tw:rounded-xl`
- Modifier: `tw:hover:text-primary-400!`, `tw:focus:ring-2!`

**Dark mode:**
- Class `app-dark` thêm vào root `<div>` — PrimeVue tự nhận qua CSS selector
- `useThemeStore()` quản lý state, init trong `main.js`

**Token flow:**
1. App load → `hydrateFromRefresh()` → refresh token từ `localStorage` → lấy access token mới
2. Mọi request → Bearer token tự động thêm qua request interceptor
3. 401 → tự động refresh → retry (chỉ 1 lần, `_retry` flag)
4. Refresh thất bại → logout

**Environment:**
- `admin/.env` commit vào git (không chứa secret)
- `VITE_API_BASE_URL=http://localhost:5095/api` — trỏ thẳng vào backend, không qua Vite proxy
- Vite proxy `/api → localhost:5095` trong `vite.config.js` là fallback nếu không set env
