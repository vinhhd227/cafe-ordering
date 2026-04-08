---
title: Auth Flow
tags: [auth, jwt, security, identity]
updated: 2026-04-08
---

# Auth Flow

Hệ thống dùng **JWT Access Token + Refresh Token** với cơ chế xoay vòng (token rotation).

| Token         | Thời hạn | Lưu trữ                                            |
| ------------- | -------- | -------------------------------------------------- |
| Access Token  | 15 phút  | Memory (Pinia store)                               |
| Refresh Token | 7 ngày   | HttpOnly cookie (`refreshToken`, path `/api/auth`) |

> `expiresAt` trong response là thời hạn của **refresh token** (7 ngày), không phải access token.
> Access token hết hạn được xử lý qua reactive 401 path (interceptor tự refresh).

---

## Roles & Permissions

- **Roles:** `Admin`, `Staff` (và custom roles — quản lý qua `/api/admin/roles`)
- **Authorization**: policy-based — tên policy = tên permission claim trong JWT
- Claim ví dụ: `table.create`, `product.read`, `user.deactivate`
- Claims lấy từ `identity.RoleClaims` theo role của user
- **App-level permission**: `admin.access` bắt buộc để đăng nhập admin app; `customer.access` cho client app

---

## Đăng nhập

```
POST /api/admin/auth/login     ← admin app
POST /api/client/auth/login    ← client app
{ "username": "...", "password": "..." }
```

Backend xử lý (`LoginAdminEndpoint` / `LoginClientEndpoint` → `LoginHandler` → `IdentityService.LoginAsync`):
1. Tìm user theo username — `401` nếu không tồn tại hoặc `IsActive = false`
2. Kiểm tra lockout (sai mật khẩu nhiều lần) — `401` nếu bị khóa
3. Xác thực mật khẩu (`lockoutOnFailure: true`)
4. Lấy roles và permission claims từ DB
5. Kiểm tra app-level permission (`admin.access` / `customer.access`) — `403` nếu thiếu
6. Tạo JWT access token (HS256)
7. Tạo refresh token (64-byte random, lưu vào `identity.RefreshTokens`)

**JWT claims:** `sub` (userId), `username`, `fullName`, `ClaimTypes.Role` (nhiều claim nếu nhiều role), `permission` (nhiều claim), `staffId`?, `customerId`?

```json
// Response
{
  "accessToken": "eyJhbGc...",
  "expiresAt": "2026-04-15T10:00:00Z"   // refresh token expiry (7 ngày)
}
// Set-Cookie: refreshToken=...; HttpOnly; SameSite=Strict; Path=/api/auth; Max-Age=604800
// (Secure flag chỉ bật khi không phải Development environment)
```

Frontend lưu:
- `accessToken` → Pinia store (memory only, không localStorage)
- `user` (parse từ JWT claims) → Pinia store + `localStorage` (chỉ để hiển thị UI)
- `expiresAt` → Pinia store (dùng để schedule rotate refresh token)
- Gọi `scheduleTokenRefresh()` → tự động gọi refresh 30 giây trước khi **refresh token** hết hạn

---

## Gửi request có xác thực

Request interceptor (`axios.js`) tự động thêm header:
```
Authorization: Bearer <accessToken>
```

Lưu ý: nếu request không có body (PUT/POST không payload), interceptor xóa `Content-Type` để tránh lỗi `415` từ FastEndpoints khi retry.

---

## Refresh Token

Có 2 trường hợp trigger:

**Proactive** (30 giây trước khi refresh token hết hạn — 7 ngày):
```
setTimeout fires → POST /api/auth/refresh (cookie tự đính kèm)
```
Mục đích: rotate refresh token trước khi nó expire, giữ session sống lâu dài.
Nếu proactive fail → reset `refreshAttempts = 0` để reactive path vẫn hoạt động bình thường.

**Reactive** (khi access token hết hạn — 15 phút):
```
API trả 401 → Axios interceptor → POST /api/auth/refresh → Retry request gốc
```
Mục đích: lấy access token mới khi 401, retry request tự động.

Backend xử lý:
1. Đọc `refreshToken` từ cookie
2. Không tồn tại / đã bị revoke → nghi ngờ token theft → revoke toàn bộ token của user → `401`
3. Hết hạn / user inactive → `401`
4. Revoke token cũ (`IsRevoked = true`)
5. Tạo access token mới + refresh token mới (token rotation)
6. Set cookie mới

Frontend dedup: nhiều request 401 đồng thời chỉ gọi refresh 1 lần, dùng combo `refreshing` flag + `refreshAttempts` counter + `_refreshPromise` cache. `refreshing` được set `true` **trước** khi tăng `refreshAttempts` để tránh race condition.

---

## Khởi động app (Hydration)

`main.js` gọi `hydrateFromRefresh()` trước khi mount app:

```
App load → POST /api/auth/refresh (browser tự gửi cookie)
         ↓ thành công → khôi phục session, schedule rotate refresh token
         ↓ thất bại  → giữ trạng thái chưa đăng nhập
```

Route guards đợi hydration hoàn thành (`hydrated = true`) trước khi đánh giá `isAuthenticated`.

---

## Đăng xuất

```
POST /api/auth/logout   (AllowAnonymous)
```

Backend: Đọc cookie `refreshToken`, revoke token trong DB, xóa cookie (`Path=/api/auth`).
Frontend: Xóa `accessToken`, `user`, `localStorage`, hủy timer, reset state, redirect về `/login`. Gọi logout API fire-and-forget (`.catch(() => {})`).

---

## Bảo mật

| Tình huống | Xử lý |
|-----------|-------|
| Sai mật khẩu nhiều lần | ASP.NET Identity lockout |
| Không có `admin.access` permission | 403 Forbidden tại login |
| Token bị revoke dùng lại | Revoke toàn bộ token của user (all devices logout) |
| Đổi mật khẩu | Revoke toàn bộ token của user |
| Reset mật khẩu (admin) | Revoke toàn bộ token của user |
| Deactivate tài khoản | Revoke toàn bộ token của user |

---

## Route Guards (Frontend)

- `requiresAuth: true` → redirect về `/login` nếu chưa đăng nhập
- `adminOnly: true` → kiểm tra role Admin
- `requiredClaim: "feature.action"` → kiểm tra trong `user.permissions` array

---

## File liên quan

| File | Vai trò |
|------|---------|
| `Api.Web/Endpoints/Auth/LoginAdmin.cs` | Endpoint đăng nhập admin |
| `Api.Web/Endpoints/Auth/LoginClient.cs` | Endpoint đăng nhập client |
| `Api.Web/Endpoints/Auth/RefreshToken.cs` | Endpoint refresh (`/api/auth/refresh`) |
| `Api.Web/Endpoints/Auth/Logout.cs` | Endpoint đăng xuất (`/api/auth/logout`) |
| `Api.Infrastructure/Identity/IdentityService.cs` | Logic xác thực, quản lý token |
| `Api.Infrastructure/Identity/JwtService.cs` | Tạo JWT (HS256, 15 phút) |
| `admin/src/stores/auth.js` | State, login/logout/refresh/hydration |
| `admin/src/services/auth.service.js` | Axios wrappers cho auth endpoints |
| `admin/src/services/axios.js` | Interceptors (Bearer token, 401 → refresh) |
