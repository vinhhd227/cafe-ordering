# Hệ thống Thông báo (Notifications)

## Tổng quan

Hệ thống thông báo cho phép gửi thông báo đến staff/admin theo từng loại sự kiện. Thông báo được lưu trong DB theo từng user (có lịch sử, trạng thái đọc/chưa đọc), đồng thời gửi push notification tới thiết bị di động nếu user đã subscribe.

**Đặc điểm:**
- Lịch sử thông báo lưu tối đa 30 ngày
- Trạng thái đọc/chưa đọc theo từng user
- Role-based targeting: mỗi loại thông báo có thể config gửi đến role nào
- Push notification tới thiết bị khi app không mở

---

## Kiến trúc

```
Sự kiện (Domain Event / Business Logic)
    ↓
INotificationService.SendAsync(type, title, body, url, referenceId?)
    ↓
NotificationService (Infrastructure)
    ├── Tra cứu NotificationConfig → kiểm tra enabled + target roles
    ├── Lấy danh sách userId theo roles (UserManager.GetUsersInRoleAsync)
    ├── Tạo Notification record cho từng user → lưu vào DB
    └── Gửi push notification tới PushSubscription của các user đó
```

---

## Domain Layer (`Api.Core`)

### `NotificationType` SmartEnum

File: `Api.Core/Aggregates/NotificationAggregate/NotificationType.cs`

| Tên | Giá trị | Ý nghĩa |
|-----|---------|---------|
| `OrderCreated` | `ORDER_CREATED` | Có đơn mới đặt qua QR |
| `OrderCancelled` | `ORDER_CANCELLED` | Đơn bị hủy |
| `OrderCompleted` | `ORDER_COMPLETED` | Đơn hoàn thành |
| `PaymentReceived` | `PAYMENT_RECEIVED` | Thanh toán thành công |
| `ManualOrderCreated` | `MANUAL_ORDER_CREATED` | Đơn tạo thủ công bởi staff |
| `LowStock` | `LOW_STOCK` | Nguyên liệu sắp hết |
| `SystemAlert` | `SYSTEM_ALERT` | Cảnh báo hệ thống |

### `Notification` Aggregate

File: `Api.Core/Aggregates/NotificationAggregate/Notification.cs`

```csharp
// Tạo thông báo mới cho một user
Notification.Create(userId, type, title, body, url?, referenceId?)

// Đánh dấu đã đọc
notification.MarkRead()  // sets IsRead = true, ReadAt = UtcNow
```

| Property | Kiểu | Mô tả |
|----------|------|-------|
| `Id` | `int` | Primary key |
| `UserId` | `string` | User nhận thông báo (Guid → string) |
| `Type` | `NotificationType` | Loại thông báo |
| `Title` | `string` | Tiêu đề |
| `Body` | `string` | Nội dung |
| `Url` | `string?` | Deep link trong admin app |
| `ReferenceId` | `int?` | ID entity liên quan (ví dụ: OrderId) |
| `IsRead` | `bool` | Đã đọc chưa |
| `ReadAt` | `DateTime?` | Thời điểm đọc |
| `CreatedAt` | `DateTime` | Thời điểm tạo |

### `NotificationConfig` Entity

File: `Api.Core/Aggregates/NotificationAggregate/NotificationConfig.cs`

Mỗi `NotificationConfig` là config cho một `NotificationType`. Seeded mặc định khi startup.

| Property | Kiểu | Mô tả |
|----------|------|-------|
| `Id` | `int` | Primary key |
| `Type` | `string` | Tên NotificationType (unique) |
| `TargetRoles` | `List<string>` | Danh sách role nhận thông báo (jsonb) |
| `IsEnabled` | `bool` | Bật/tắt loại thông báo này |

```csharp
// Cập nhật config
config.Update(targetRoles: ["Admin", "Staff"], isEnabled: true)
```

### Specifications

| Spec | Mô tả |
|------|-------|
| `NotificationsByUserSpec(userId, page, pageSize)` | Paged list thông báo của user, mới nhất trước |
| `NotificationsCountByUserSpec(userId)` | Tổng số thông báo |
| `UnreadCountByUserSpec(userId)` | Số thông báo chưa đọc |
| `NotificationConfigByTypeSpec(typeName)` | Tìm config theo type name |
| `AllNotificationConfigsSpec()` | Tất cả config, sắp xếp theo Type |
| `OldNotificationsSpec(cutoff)` | Thông báo cũ hơn ngày cutoff (dùng cho cleanup) |
| `PushSubscriptionsByUserIdsSpec(userIds)` | Push subscriptions của danh sách user |

---

## Infrastructure Layer (`Api.Infrastructure`)

### `NotificationService`

File: `Api.Infrastructure/Services/NotificationService.cs`

Implements `INotificationService`. Orchestrates toàn bộ flow khi cần gửi thông báo:

1. Tra cứu `NotificationConfig` theo type
2. Kiểm tra `IsEnabled` và `TargetRoles` có dữ liệu
3. Lấy `userId` list từ `UserManager.GetUsersInRoleAsync` cho từng role
4. Tạo `Notification` record cho từng user, lưu batch
5. Tìm `PushSubscription` của các user đó
6. Gửi Web Push tới tất cả subscriptions

### `NotificationCleanupService`

File: `Api.Infrastructure/Services/NotificationCleanupService.cs`

`BackgroundService` chạy mỗi 24 giờ, xóa các thông báo cũ hơn 30 ngày.

### EF Core Configuration

- `Notifications` table: index composite `(UserId, IsRead)` và `(UserId, CreatedAt DESC)`
- `NotificationConfigs` table: `TargetRoles` lưu dạng **jsonb**, unique index trên `Type`

---

## Use Cases (`Api.UseCases`)

### Commands/Queries

| Use case | Kiểu | Mô tả |
|----------|------|-------|
| `ListNotificationsQuery` | Query | Lấy danh sách + count |
| `GetUnreadCountQuery` | Query | Số thông báo chưa đọc |
| `MarkNotificationReadCommand` | Command | Đánh dấu 1 thông báo đã đọc |
| `MarkAllNotificationsReadCommand` | Command | Đánh dấu tất cả đã đọc |
| `ListNotificationConfigsQuery` | Query | Lấy cấu hình tất cả notification types |
| `UpdateNotificationConfigCommand` | Command | Cập nhật config (target roles, enabled) |

### DTOs

```csharp
record NotificationDto(
    int Id, string Type, string Title, string Body,
    string? Url, int? ReferenceId,
    bool IsRead, DateTime? ReadAt, DateTime CreatedAt
)

record NotificationListDto(
    IReadOnlyList<NotificationDto> Items,
    int TotalCount,
    int UnreadCount
)

record NotificationConfigDto(
    int Id, string Type, List<string> TargetRoles, bool IsEnabled
)
```

---

## HTTP API Endpoints

Base path: `/api/admin/`

| Method | Path | Policy | Mô tả |
|--------|------|--------|-------|
| `GET` | `notifications` | StaffOrAdmin | Lấy danh sách thông báo (paged) |
| `GET` | `notifications/unread-count` | StaffOrAdmin | Số thông báo chưa đọc |
| `PUT` | `notifications/{id}/read` | StaffOrAdmin | Đánh dấu đã đọc |
| `PUT` | `notifications/read-all` | StaffOrAdmin | Đánh dấu tất cả đã đọc |
| `GET` | `notification-configs` | AdminOnly | Lấy cấu hình notification |
| `PUT` | `notification-configs/{id}` | AdminOnly | Cập nhật cấu hình |

### `GET /api/admin/notifications`

Query params:
- `page` (default: 1)
- `pageSize` (default: 20)

Response `200`:
```json
{
  "items": [
    {
      "id": 42,
      "type": "ORDER_CREATED",
      "title": "Đơn mới #ORD-001",
      "body": "Bàn A1 · 3 món",
      "url": "/orders/123",
      "referenceId": 123,
      "isRead": false,
      "readAt": null,
      "createdAt": "2025-03-01T10:30:00Z"
    }
  ],
  "totalCount": 45,
  "unreadCount": 12
}
```

### `PUT /api/admin/notification-configs/{id}`

Request body:
```json
{
  "targetRoles": ["Admin", "Staff"],
  "isEnabled": true
}
```

---

## Frontend

### Service (`admin/src/services/notification.service.js`)

```js
getNotifications(params)           // GET /admin/notifications
getUnreadCount()                   // GET /admin/notifications/unread-count
markRead(id)                       // PUT /admin/notifications/{id}/read
markAllRead()                      // PUT /admin/notifications/read-all
getNotificationConfigs()           // GET /admin/notification-configs
updateNotificationConfig(id, data) // PUT /admin/notification-configs/{id}
```

### Store (`admin/src/stores/notifications.js`)

```js
const store = useNotificationStore()

store.items           // NotificationDto[]
store.totalCount      // tổng số
store.unreadCount     // số chưa đọc (dùng cho badge)
store.loading         // loading state
store.soundEnabled    // bật/tắt âm thanh
store.creatingOrder   // suppress toast khi đang tạo order thủ công

await store.fetchNotifications(page?, pageSize?)
await store.fetchUnreadCount()
await store.markRead(id)
await store.markAllRead()
await store.onNewOrder()  // gọi khi SSE báo có đơn mới
```

### Component `NotificationBell.vue`

- Hiển thị badge đỏ với số thông báo chưa đọc
- Click bell → popover overlay
- Popover: danh sách thông báo từ API, icon theo type, thời gian
- Click item → gọi `markRead(id)` → navigate đến `item.url`
- Nút đánh dấu tất cả đã đọc
- Nút toggle push notification
- Nút toggle sound
- "Load more" khi còn thông báo chưa hiển thị
- Chấm màu chỉ trạng thái SSE connection

---

## Mở rộng — Thêm Notification Type mới

### 1. Backend

Thêm constant vào `NotificationType`:

```csharp
// Api.Core/Aggregates/NotificationAggregate/NotificationType.cs
public static readonly NotificationType LowStock = new("LOW_STOCK", 5);
```

Gọi từ business logic:

```csharp
await notificationService.SendAsync(
    NotificationType.LowStock,
    title: $"Nguyên liệu sắp hết: {ingredientName}",
    body: $"Còn {remaining} {unit}",
    url: "/inventory",
    referenceId: ingredientId,
    ct: ct);
```

SeedData sẽ tự tạo config mặc định khi khởi động lần đầu (nếu chưa tồn tại). Để thêm seed manual:

```csharp
// Api.Infrastructure/Data/SeedData.cs — trong SeedNotificationConfigsAsync()
new NotificationConfig { Type = "LOW_STOCK", TargetRoles = ["Admin"], IsEnabled = true },
```

### 2. Frontend

Thêm icon mapping trong `NotificationBell.vue`:

```js
function typeIcon(type) {
  const map = {
    // ...
    LOW_STOCK: 'ph:warning-bold',
  }
  return map[type] ?? 'ph:bell-bold'
}
```

---

## Default Config (Seed)

Khi khởi động lần đầu, các config sau được tạo tự động:

| Type | Target Roles | Enabled |
|------|-------------|---------|
| `ORDER_CREATED` | Admin, Staff | ✅ |
| `ORDER_CANCELLED` | Admin, Staff | ✅ |
| `ORDER_COMPLETED` | Admin, Staff | ✅ |
| `PAYMENT_RECEIVED` | Admin | ✅ |
| `MANUAL_ORDER_CREATED` | Admin, Staff | ✅ |
| `LOW_STOCK` | Admin | ✅ |
| `SYSTEM_ALERT` | Admin | ✅ |

Admin có thể thay đổi qua giao diện Settings → Notification Configs (endpoint `GET/PUT /api/admin/notification-configs`).

---

## Lifecycle của một Thông báo

```
1. Sự kiện xảy ra (đặt đơn, hủy đơn, ...)
2. Handler gọi INotificationService.SendAsync(...)
3. NotificationService:
   a. Đọc NotificationConfig → kiểm tra enabled + roles
   b. UserManager trả về userIds theo role
   c. Tạo Notification records → INSERT batch
   d. PushSubscription query → Web Push gửi tới thiết bị
4. Frontend (admin app):
   a. SSE nhận event → store.onNewOrder() → fetchNotifications()
   b. Bell badge cập nhật unreadCount
   c. Toast hiện thị ngắn (6 giây)
   d. Âm thanh beep/chime nếu soundEnabled
5. User click notification → markRead(id) → navigate(url)
6. NotificationCleanupService xóa sau 30 ngày
```
