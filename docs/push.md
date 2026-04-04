# Web Push Notifications

Tài liệu này mô tả hệ thống **Web Push** — phần đảm nhận việc gửi notification tới thiết bị khi admin app không mở. Khác với in-app notification (lưu DB, hiển thị trong bell), web push là thông báo native của browser/OS.

Xem thêm: [notifications.md](./notifications.md) — hệ thống in-app notification đi kèm.

---

## Tổng quan

```
NotificationService (Infrastructure)
    └── IPushNotificationService.SendToSubscriptionsAsync(...)
            ↓
        PushNotificationService (WebPush library)
            ├── Ký payload bằng VAPID private key
            └── POST tới browser push endpoint (Google FCM / Apple APNs)
                        ↓
                Service Worker (admin/public/sw.js)
                    ├── showNotification() → native OS notification
                    └── postMessage('PUSH_NEW_ORDER') → play chime nếu tab đang mở
```

**Điều kiện hoạt động:**
- User đã grant `Notification` permission trên browser
- Browser đã subscribe và endpoint được lưu trong DB
- VAPID keys được cấu hình đúng trên server

---

## Cấu hình VAPID

VAPID (Voluntary Application Server Identification) là cặp key EC P-256 dùng để xác thực server khi gửi push.

### Sinh key mới

```bash
npx web-push generate-vapid-keys
```

### Cấu hình

| Môi trường | Cách cấu hình |
|-----------|--------------|
| Local dev | `appsettings.Development.json` (gitignored) |
| Production | Env var `VAPID_PUBLIC_KEY` / `VAPID_PRIVATE_KEY` trong `.env.prod` |
| UAT | Env var `VAPID_PUBLIC_KEY` / `VAPID_PRIVATE_KEY` trong `.env.uat` |

```json
// appsettings.Development.json
"Vapid": {
  "Subject": "mailto:dev@cafe-ordering.com",
  "PublicKey": "<base64url public key>",
  "PrivateKey": "<base64url private key>"
}
```

> **Lưu ý:** Đổi `PublicKey` sẽ làm mất hết push subscription hiện tại. Browser sẽ tự re-subscribe khi user load lại trang.

---

## Domain Layer

### `PushSubscription` Aggregate

File: `api/src/Api.Core/Aggregates/PushSubscriptionAggregate/PushSubscription.cs`

Mỗi browser/thiết bị có một subscription riêng, unique bởi `Endpoint`.

| Property | Kiểu | Mô tả |
|----------|------|-------|
| `Id` | `int` | Primary key |
| `UserId` | `string` | User sở hữu subscription (Guid → string) |
| `Endpoint` | `string` | Push endpoint URL cấp bởi browser vendor (Google FCM / Apple APNs) |
| `P256dh` | `string` | ECDH public key của client (base64url) |
| `Auth` | `string` | Auth secret của client (base64url) |
| `CreatedAt` | `DateTime` | Từ `AuditableEntity` |
| `UpdatedAt` | `DateTime` | Từ `AuditableEntity` |

```csharp
// Tạo mới
var sub = PushSubscription.Create(userId, endpoint, p256dh, auth);

// Cập nhật keys (khi browser renew subscription cùng endpoint)
sub.Update(p256dh, auth);
```

### Specifications

| Spec | Mô tả |
|------|-------|
| `PushSubscriptionByEndpointSpec(endpoint)` | Tìm subscription theo endpoint URL (unique) |
| `PushSubscriptionsByUserIdsSpec(userIds)` | Tất cả subscription của danh sách user |
| `AllPushSubscriptionsSpec()` | Tất cả subscriptions |

---

## Infrastructure Layer

### `PushNotificationService`

File: `api/src/Api.Infrastructure/Services/PushNotificationService.cs`

Implements `IPushNotificationService`. Dùng thư viện `WebPush` (.NET) để gửi push.

**Hai entry points:**

```csharp
// Gửi tới danh sách PushSubscription entity (đã load từ DB)
// Tự xóa các subscription expired (410 Gone) khỏi DB
Task SendToSubscriptionsAsync(
    IEnumerable<PushSubscription> subscriptions,
    string title, string body, string? url = null,
    CancellationToken ct = default)

// Gửi tới snapshots (không cần load entity — dùng khi đã có data từ query join)
Task SendToSnapshotsAsync(
    IEnumerable<PushSubSnapshot> snapshots,
    string title, string body, string? url = null, string? detail = null,
    CancellationToken ct = default)
```

**Xử lý subscription hết hạn:**

Khi browser xóa subscription (user revoke permission, clear browser data), push endpoint trả về `410 Gone`. Service tự động xóa subscription đó khỏi DB.

**Payload gửi tới browser:**

```json
{ "title": "Đơn mới #ORD-001", "body": "Bàn A1 · 3 món", "url": "/orders/123" }
```

### `VapidSettings`

File: `api/src/Api.Infrastructure/Services/VapidSettings.cs`

```csharp
public class VapidSettings
{
    public string Subject { get; set; }    // mailto: URI
    public string PublicKey { get; set; }  // base64url EC P-256 public key
    public string PrivateKey { get; set; } // base64url EC P-256 private key
}
```

---

## Use Cases

### `SubscribePushCommand`

File: `api/src/Api.UseCases/Push/Subscribe/`

**Upsert logic:** Nếu `Endpoint` đã tồn tại trong DB → cập nhật `P256dh` và `Auth`. Nếu chưa có → tạo mới. Điều này xử lý trường hợp browser renew subscription giữ nguyên endpoint nhưng đổi keys.

### `UnsubscribePushCommand`

File: `api/src/Api.UseCases/Push/Unsubscribe/`

Tìm subscription theo `Endpoint`, xóa khỏi DB. Trả về `404` nếu không tìm thấy.

---

## HTTP API Endpoints

Base path: `/api/admin/push/` — policy `StaffOrAdmin`

| Method | Path | Mô tả |
|--------|------|-------|
| `GET` | `push/vapid-public-key` | Lấy VAPID public key để frontend subscribe |
| `POST` | `push/subscribe` | Đăng ký push subscription của browser hiện tại |
| `DELETE` | `push/subscribe` | Hủy đăng ký push subscription |

### `GET /api/admin/push/vapid-public-key`

Response `200`: VAPID public key dạng string base64url.

```
"BCrq08eb9EEAf1_uePKzgIud..."
```

### `POST /api/admin/push/subscribe`

Request body:
```json
{
  "endpoint": "https://fcm.googleapis.com/fcm/send/...",
  "p256dh": "BNcRdreALRFXTkOOUHK...",
  "auth": "tBHItJI5svbpez7KI..."
}
```

Response `204` khi thành công.

### `DELETE /api/admin/push/subscribe`

Request body:
```json
{
  "endpoint": "https://fcm.googleapis.com/fcm/send/..."
}
```

Response `204` khi thành công, `404` nếu endpoint không tồn tại trong DB.

---

## Frontend

### Service (`admin/src/services/push.service.js`)

```js
getVapidPublicKey()           // GET /admin/push/vapid-public-key
subscribePush(data)           // POST /admin/push/subscribe
unsubscribePush(data)         // DELETE /admin/push/subscribe
```

### Composable `usePushNotifications`

File: `admin/src/composables/usePushNotifications.js`

```js
const {
  isSupported,     // boolean — browser hỗ trợ Web Push không
  permission,      // 'default' | 'granted' | 'denied'
  isSubscribed,    // boolean — browser hiện tại đã subscribe chưa
  loading,         // boolean — đang xử lý subscribe/unsubscribe
  toggle,          // subscribe nếu chưa, unsubscribe nếu đã có
  requestAndSubscribe, // xin permission → subscribe
  unsubscribe,
} = usePushNotifications()
```

**Subscribe flow:**

```
1. navigator.serviceWorker.register('/sw.js')
2. navigator.serviceWorker.ready → ServiceWorkerRegistration
3. GET /api/admin/push/vapid-public-key
4. pushManager.subscribe({ userVisibleOnly: true, applicationServerKey })
5. POST /api/admin/push/subscribe { endpoint, p256dh, auth }
```

**Trên `onMounted`:** Tự động register SW và kiểm tra subscription hiện tại. Lắng nghe `postMessage` từ SW để play chime khi có push tới.

**Chime âm thanh:** 3 nốt C5-E5-G5 (sine wave qua Web Audio API) khi nhận message `PUSH_NEW_ORDER` từ SW.

### Service Worker (`admin/public/sw.js`)

Xử lý hai event:

**`push`:**
- Parse JSON payload `{ title, body, url }`
- `showNotification(title, options)` — hiển thị notification native
- `postMessage({ type: 'PUSH_NEW_ORDER' })` tới tất cả tab admin đang mở → trigger chime

**`notificationclick`:**
- Đóng notification
- Tìm tab admin đang mở → focus và navigate tới `data.url`
- Không có tab → `clients.openWindow(fullUrl)`

**Notification options:**
```js
{
  body: "...",
  icon: '/apple-touch-icon-v2.png',
  badge: '/apple-touch-icon-v2.png',
  tag: data.url,    // group notifications cùng URL (không stack)
  renotify: true,   // vẫn play sound kể cả khi tag trùng
  data: { url }
}
```

---

## Lifecycle của một Push Subscription

```
1. User mở admin app lần đầu
   → SW tự register ngầm (usePushNotifications onMounted)
   → checkSubscription() → isSubscribed = false nếu chưa subscribe

2. User click toggle push trong NotificationBell
   → requestAndSubscribe():
       a. Notification.requestPermission() → browser hiện dialog xin quyền
       b. Nếu 'granted' → subscribe()
       c. pushManager.subscribe() → nhận PushSubscription từ browser
       d. POST /api/admin/push/subscribe → lưu vào DB

3. Server gửi notification (qua NotificationService.SendAsync)
   → PushNotificationService gửi Web Push tới endpoint
   → Browser nhận push → SW 'push' event
   → showNotification() + postMessage chime

4. User click notification
   → SW 'notificationclick' event → navigate tới url trong payload

5. User tắt push
   → unsubscribe(): gọi DELETE /api/admin/push/subscribe + sub.unsubscribe()
   → Browser xóa subscription

6. Subscription hết hạn tự nhiên (browser clear data, user revoke)
   → Server gửi push → browser trả 410 Gone
   → PushNotificationService tự xóa endpoint khỏi DB
```

---

## Lưu ý triển khai

- **HTTPS bắt buộc:** Web Push API chỉ hoạt động trên HTTPS (hoặc localhost). Không hoạt động trên HTTP.
- **iOS Safari:** Hỗ trợ từ iOS 16.4+, chỉ khi app được "Add to Home Screen".
- **Một user có thể có nhiều subscriptions** (nhiều browser/thiết bị). Server gửi tới tất cả.
- **VAPID key rotation:** Khi đổi public key, toàn bộ subscription hiện tại bị invalid. Frontend sẽ tự re-subscribe khi `pushManager.getSubscription()` trả về `null` — tuy nhiên cần user grant lại permission nếu browser đã block.
