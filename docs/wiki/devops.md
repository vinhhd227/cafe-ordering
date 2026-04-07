---
title: DevOps — Docker & Environments
tags: [devops, docker, migration, deployment, environment]
updated: 2026-04-07
---

# DevOps — Docker & Environments

Xem thêm: [[architecture]]

---

## Environments

| Env | Compose file | Mô tả |
|-----|-------------|-------|
| `dev` | `docker-compose.dev.yml` + optional `dev.override.yml` | Local development |
| `uat` | `docker-compose.uat.yml` | Staging / test |
| `prod` | `docker-compose.prod.yml` | Production |

---

## Ports

| Service | Dev port | Mô tả |
|---------|----------|-------|
| API | `8080` | ASP.NET Core |
| Client (Vue) | `5173` | Khách đặt món |
| Admin (Vue) | `5174` | Staff/Admin dashboard |
| PostgreSQL | `5432` | Database |

---

## Setup lần đầu (Dev)

```bash
# 1. Clone repo
git clone <url>
cd cafe-ordering

# 2. Tạo config files
cp api/src/Api.Web/appsettings.Development.json.example \
   api/src/Api.Web/appsettings.Development.json
# → Thay toàn bộ giá trị CHANGE_ME (DB password, JWT key, SMTP)

cp docker-compose.dev.override.yml.example docker-compose.dev.override.yml
# → Điền credentials thực

cp client/.env.local.example client/.env.local
cp admin/.env.local.example admin/.env.local

# 3. Chạy
docker-compose -f docker-compose.dev.yml -f docker-compose.dev.override.yml up --build
```

Kiểm tra còn CHANGE_ME chưa được thay:
```bash
grep -r "CHANGE_ME" .
```

---

## Chạy từng service (không Docker)

```bash
# Backend
cd api/src/Api.Web
ASPNETCORE_ENVIRONMENT=Development dotnet run

# Admin frontend
cd admin && pnpm install && pnpm dev   # → :5174

# Client frontend
cd client && pnpm install && pnpm dev  # → :5173
```

> Cần đồng bộ CORS config trong `appsettings.Development.json` với origin frontend.

---

## Environment Variables

### API (`appsettings.Development.json`)

| Key | Mô tả |
|-----|-------|
| `ConnectionStrings:DefaultConnection` | PostgreSQL connection string |
| `Jwt:Key` | Secret key (min 32 ký tự) |
| `Jwt:Issuer` | JWT issuer |
| `Jwt:Audience` | JWT audience |
| `SmtpSettings:Username/Password` | Tài khoản SMTP (optional ở dev) |
| `AllowedOrigins` | CORS origins array |

### Frontend (`.env.local`)

| Key | Mô tả |
|-----|-------|
| `VITE_API_BASE_URL` | Base URL của API (ví dụ: `http://localhost:8080/api`) |

> `admin/.env` (không phải `.env.local`) được commit vào git vì không chứa secret — dùng `VITE_API_BASE_URL=http://localhost:5095/api` cho dev non-Docker.

---

## EF Core Migrations

Dự án có **2 DbContext riêng biệt** → 2 migration folder riêng.

### Business DB (`AppDbContext`)

```bash
cd api/src/Api.Web

# Tạo migration mới
dotnet ef migrations add <MigrationName> \
  --project ../Api.Infrastructure \
  --context AppDbContext \
  --output-dir Data/Migrations

# Apply thủ công
dotnet ef database update --context AppDbContext
```

### Identity DB (`AppIdentityDbContext`)

```bash
dotnet ef migrations add <MigrationName> \
  --project ../Api.Infrastructure \
  --context AppIdentityDbContext \
  --output-dir Identity/Migrations

dotnet ef database update --context AppIdentityDbContext
```

> **Auto-apply**: Khi app khởi động, migrations tự động được apply qua `UseAppMiddlewareAndSeedDatabase()`. Không cần chạy thủ công trong môi trường Docker.

---

## Seed Data

Chạy tự động khi startup:
- Admin user mặc định (nếu chưa tồn tại)
- Roles và permission claims (`Admin`, `Staff`)
- `NotificationConfig` defaults cho tất cả `NotificationType`

---

## Deploy Scripts

| Script | Mô tả |
|--------|-------|
| `deploy.sh` | Deploy lên production |
| `uat.sh` | Deploy lên UAT |
| `dev.sh` | Shortcut chạy dev stack |
| `scripts/backup-prod.sh` | Backup database production |
| `scripts/restore-prod.sh` | Restore database |

---

## GitHub Actions

**File:** `.github/workflows/deploy.yml`

CI/CD pipeline tự động deploy khi push lên nhánh chính. Xem file để biết chi tiết secrets cần config.

---

## Nginx

Cấu hình reverse proxy cho production:

| File | Mô tả |
|------|-------|
| `nginx/nginx.prod.conf` | Production — proxy tới API + static files |
| `nginx/nginx.dev.conf` | Dev variant |
| `admin/nginx.conf` | Nginx config bên trong container admin |
| `client/nginx.conf` | Nginx config bên trong container client |

---

## Gotchas

- **Architecture mismatch**: nếu gặp lỗi `rollup-linux-arm64-musl`, kiểm tra `.dockerignore` đã exclude `node_modules`
- **Truy cập từ thiết bị khác cùng mạng**: tạo `.env` ở root với `HOST_IP=<ip>` và `API_PORT=8080`
- **Migration conflict**: nếu 2 developer cùng tạo migration → cần rebase và rename file migration
