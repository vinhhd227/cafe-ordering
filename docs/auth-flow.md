# Authentication Flow

## Tổng quan

Hệ thống dùng **JWT Access Token + Refresh Token** với cơ chế xoay vòng (token rotation).

| Token | Thời hạn | Lưu trữ |
|-------|----------|---------|
| Access Token | 15 phút | Memory (Pinia store) |
| Refresh Token | 7 ngày | HttpOnly cookie (`refreshToken`, path `/api/auth`) |

---

## 1. Đăng nhập

```
POST /api/auth/login
{ "username": "...", "password": "..." }
```

**Backend xử lý** (`LoginEndpoint` → `LoginHandler` → `IdentityService.LoginAsync`):
1. Tìm user theo username — trả `401` nếu không tồn tại hoặc `IsActive = false`
2. Kiểm tra lockout (sai mật khẩu nhiều lần) — trả `401` nếu bị khóa
3. Xác thực mật khẩu (`lockoutOnFailure: true`)
4. Lấy roles và permission claims từ DB
5. Tạo JWT access token (HS256, claims: `sub`, `username`, `fullName`, `role`, `permission`)
6. Tạo refresh token (64-byte random, lưu vào bảng `identity.RefreshTokens`)

**Response:**
```json
{
  "accessToken": "eyJhbGc...",
  "expiresAt": "2026-03-24T15:45:30Z"
}
```
`Set-Cookie: refreshToken=...; HttpOnly; Secure; SameSite=Strict; Path=/api/auth; Max-Age=604800`

**Frontend lưu** (`auth.js`):
- `accessToken` → Pinia store (memory only)
- `user` (parse từ JWT claims) → Pinia store + `localStorage` (chỉ để hiển thị)
- `expiresAt` → Pinia store
- Gọi `scheduleTokenRefresh()` để tự động refresh trước 30 giây

---

## 2. Gửi request có xác thực

Request interceptor (`axios.js`) tự động thêm header:
```
Authorization: Bearer <accessToken>
```

Nếu request không có body (PUT/POST không có payload), interceptor xóa `Content-Type` để tránh lỗi 415 từ FastEndpoints khi retry.

---

## 3. Refresh Token

Có 2 trường hợp trigger refresh:

### 3a. Proactive (chủ động — 30 giây trước khi hết hạn)

```
setTimeout fires → POST /api/auth/refresh (cookie tự đính kèm)
```

### 3b. Reactive (phản ứng — khi nhận 401)

```
API trả 401 → Axios interceptor → POST /api/auth/refresh → Retry request gốc
```

**Backend xử lý** (`RefreshTokenEndpoint` → `IdentityService.RefreshTokenAsync`):
1. Đọc `refreshToken` từ cookie
2. Tìm trong DB:
   - Không tồn tại hoặc đã bị revoke → **nghi ngờ token bị đánh cắp** → revoke toàn bộ token của user → `401`
   - Đã hết hạn → `401`
   - User không tồn tại hoặc inactive → `401`
3. Revoke token cũ (`IsRevoked = true`)
4. Tạo access token mới + refresh token mới (token rotation)
5. Set cookie mới

**Frontend** (`auth.js` — `doRefreshToken`):
- Dedup: nhiều request 401 đồng thời chỉ gọi refresh 1 lần (dùng `_refreshPromise`)
- Tối đa 1 lần retry — nếu refresh thất bại: logout
- Sau refresh: cập nhật `accessToken`, lên lịch refresh tiếp theo

---

## 4. Khởi động lại app (Hydration)

`main.js` gọi `hydrateFromRefresh()` trước khi mount app:

```
App load → POST /api/auth/refresh (browser tự gửi cookie)
         ↓ thành công → khôi phục session, lên lịch refresh
         ↓ thất bại  → giữ trạng thái chưa đăng nhập
```

Route guards đợi hydration hoàn thành trước khi đánh giá `isAuthenticated`.

---

## 5. Đăng xuất

```
POST /api/auth/logout
```

**Backend**: Revoke refresh token trong DB, xóa cookie.
**Frontend**: Xóa `accessToken`, `user`, `localStorage`, hủy timer, redirect về `/login`.

---

## 6. Các tính năng bảo mật

| Tình huống | Xử lý |
|-----------|-------|
| Sai mật khẩu nhiều lần | ASP.NET Identity lockout |
| Token bị revoke dùng lại | Revoke toàn bộ token của user (all devices logout) |
| Đổi mật khẩu | Revoke toàn bộ token của user |
| Deactivate tài khoản | Revoke toàn bộ token của user |

---

## 7. Sơ đồ luồng

```
ĐĂNG NHẬP
──────────────────────────────────────────────────────────────
Frontend          Backend                    Database
   │                  │                          │
   │─ POST /login ───►│                          │
   │                  │─ Verify credentials ────►│
   │                  │─ Generate JWT            │
   │                  │─ Store refresh token ───►│
   │◄─ accessToken ───│                          │
   │◄─ cookie ────────│                          │
   │                  │                          │
   │ [lưu accessToken vào memory]
   │ [scheduleTokenRefresh sau 14:30]


REFRESH (proactive hoặc reactive 401)
──────────────────────────────────────────────────────────────
Frontend          Backend                    Database
   │                  │                          │
   │─ POST /refresh ─►│ (cookie tự đính kèm)    │
   │                  │─ Lookup token ──────────►│
   │                  │─ Revoke old token ──────►│
   │                  │─ Store new token ───────►│
   │◄─ new accessToken│                          │
   │◄─ new cookie ────│                          │
   │                  │                          │
   │ [cập nhật accessToken]
   │ [scheduleTokenRefresh mới]


ĐĂNG XUẤT
──────────────────────────────────────────────────────────────
Frontend          Backend                    Database
   │                  │                          │
   │─ POST /logout ──►│                          │
   │                  │─ Revoke token ──────────►│
   │                  │─ Delete cookie           │
   │◄─ 204 ───────────│                          │
   │                  │                          │
   │ [xóa state, redirect /login]
```

---

## 8. Các file liên quan

**Backend**

| File | Vai trò |
|------|---------|
| `Api.Web/Endpoints/Auth/Login.cs` | Endpoint đăng nhập |
| `Api.Web/Endpoints/Auth/RefreshToken.cs` | Endpoint refresh |
| `Api.Web/Endpoints/Auth/Logout.cs` | Endpoint đăng xuất |
| `Api.Infrastructure/Identity/IdentityService.cs` | Logic xác thực, quản lý token |
| `Api.Infrastructure/Identity/JwtService.cs` | Tạo/validate JWT |
| `Api.Infrastructure/Identity/RefreshToken.cs` | Entity refresh token |

**Frontend**

| File | Vai trò |
|------|---------|
| `admin/src/stores/auth.js` | State, login/logout/refresh/hydration |
| `admin/src/services/auth.service.js` | Axios wrappers cho auth endpoints |
| `admin/src/services/axios.js` | Request interceptor (Bearer token), response interceptor (401 → refresh) |
