# File Quan Trọng & Lệnh Thường Dùng

## File Quan Trọng

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

## Lệnh Thường Dùng

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

> Migration tự động apply khi app khởi động (trong `UseAppMiddlewareAndSeedDatabase()`).

### Frontend

```bash
# Admin
cd admin && pnpm dev      # http://localhost:5173
cd admin && pnpm build

# Client
cd client && pnpm dev     # http://localhost:5174
cd client && pnpm build
```

### Docker

```bash
# Toàn bộ stack
docker-compose -f docker-compose.dev.yml up --build

# Chỉ database
docker-compose -f docker-compose.dev.yml up cafe-db

# Với override
cp docker-compose.dev.override.yml.example docker-compose.dev.override.yml
docker-compose -f docker-compose.dev.yml -f docker-compose.dev.override.yml up
```
