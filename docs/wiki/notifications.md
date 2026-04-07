---
title: Hệ thống Thông báo
tags: [notification, sse, push, realtime]
updated: 2026-04-07
---

# Hệ thống Thông báo

Thông báo được lưu DB theo từng user (có lịch sử, trạng thái đọc/chưa đọc) và gửi push notification đến thiết bị nếu user đã subscribe.

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
    ├── Tạo Notification record cho từng user → lưu DB batch
    └── Gửi push notification tới PushSubscription của các user
```

---

## NotificationType (SmartEnum)

| Tên | Giá trị | Trigger khi |
|-----|---------|------------|
| `OrderCreated` | `ORDER_CREATED` | Khách đặt đơn qua QR |
| `OrderCancelled` | `ORDER_CANCELLED` | Đơn bị hủy |
| `OrderCompleted` | `ORDER_COMPLETED` | Đơn hoàn thành |
| `PaymentReceived` | `PAYMENT_RECEIVED` | Thanh toán thành công |
| `ManualOrderCreated` | `MANUAL_ORDER_CREATED` | Staff tạo đơn thủ công |
| `LowStock` | `LOW_STOCK` | Nguyên liệu sắp hết |
| `SystemAlert` | `SYSTEM_ALERT` | Cảnh báo hệ thống |

---

## NotificationConfig (cấu hình per type)

Mỗi NotificationType có một config, seeded khi startup. Admin có thể thay đổi qua UI.

| Property | Mô tả |
|----------|-------|
| `Type` | Tên NotificationType (unique) |
| `TargetRoles` | Danh sách role nhận thông báo (jsonb) |
| `IsEnabled` | Bật/tắt loại thông báo |

**Default config khi seed:**

| Type | Target Roles | Enabled |
|------|-------------|---------|
| `ORDER_CREATED` | Admin, Staff | ✅ |
| `ORDER_CANCELLED` | Admin, Staff | ✅ |
| `ORDER_COMPLETED` | Admin, Staff | ✅ |
| `PAYMENT_RECEIVED` | Admin | ✅ |
| `MANUAL_ORDER_CREATED` | Admin, Staff | ✅ |
| `LOW_STOCK` | Admin | ✅ |
| `SYSTEM_ALERT` | Admin | ✅ |

---

## Notification Entity

| Property | Type | Mô tả |
|----------|------|-------|
| `UserId` | string | User nhận thông báo |
| `Type` | `NotificationType` | Loại thông báo |
| `Title` | string | Tiêu đề |
| `Body` | string | Nội dung |
| `Url` | string? | Deep link trong admin app |
| `ReferenceId` | int? | ID entity liên quan (ví dụ: OrderId) |
| `IsRead` | bool | Đã đọc chưa |
| `ReadAt` | DateTime? | Thời điểm đọc |
| `CreatedAt` | DateTime | Thời điểm tạo |

**Lifecycle:** Lưu tối đa 30 ngày — `NotificationCleanupService` (BackgroundService) chạy mỗi 24h để xóa thông báo cũ.

---

## API Endpoints

| Method | Path | Policy | Mô tả |
|--------|------|--------|-------|
| `GET` | `/api/admin/notifications` | StaffOrAdmin | Danh sách (paged) |
| `GET` | `/api/admin/notifications/unread-count` | StaffOrAdmin | Số chưa đọc |
| `PUT` | `/api/admin/notifications/{id}/read` | StaffOrAdmin | Đánh dấu đã đọc |
| `PUT` | `/api/admin/notifications/read-all` | StaffOrAdmin | Đánh dấu tất cả đã đọc |
| `GET` | `/api/admin/notification-configs` | AdminOnly | Lấy cấu hình |
| `PUT` | `/api/admin/notification-configs/{id}` | AdminOnly | Cập nhật cấu hình |

---

## Frontend

### Notification Store (`admin/src/stores/notifications.js`)

```js
store.unreadCount   // số chưa đọc → badge trên NotificationBell
store.soundEnabled  // bật/tắt âm thanh
store.creatingOrder // suppress toast khi đang tạo đơn thủ công

await store.fetchNotifications(page?, pageSize?)
await store.fetchUnreadCount()
await store.markRead(id)
await store.markAllRead()
await store.onNewOrder()  // gọi khi SSE báo có đơn mới
```

### NotificationBell.vue

- Badge đỏ hiển thị `unreadCount`
- Click → popover danh sách thông báo
- Click item → `markRead(id)` + navigate đến `item.url`
- Toggle push notification, toggle sound
- "Load more" khi còn thông báo
- Chấm màu chỉ trạng thái SSE connection

---

## SSE (Server-Sent Events)

Admin frontend subscribe SSE để nhận event real-time:
- Khi order mới → SSE push → `store.onNewOrder()` → fetchNotifications + fetchUnreadCount
- Toast hiển thị 6 giây
- Âm thanh beep/chime nếu `soundEnabled`

---

## Thêm Notification Type mới

**Backend:**
```csharp
// 1. Thêm vào NotificationType.cs
public static readonly NotificationType LowStock = new("LOW_STOCK", 5);

// 2. Gọi từ handler/service
await notificationService.SendAsync(
    NotificationType.LowStock,
    title: $"Nguyên liệu sắp hết: {name}",
    body: $"Còn {remaining} {unit}",
    url: "/inventory",
    referenceId: ingredientId,
    ct: ct);

// 3. Seed config (nếu cần default khác mặc định)
// SeedData.cs → SeedNotificationConfigsAsync()
new NotificationConfig { Type = "LOW_STOCK", TargetRoles = ["Admin"], IsEnabled = true }
```

**Frontend:**
```js
// Thêm icon vào NotificationBell.vue
function typeIcon(type) {
  const map = { LOW_STOCK: 'ph:warning-bold', ... }
  return map[type] ?? 'ph:bell-bold'
}
```

---

## File liên quan

| File | Mô tả |
|------|-------|
| `Api.Core/.../NotificationAggregate/NotificationType.cs` | SmartEnum types |
| `Api.Core/.../NotificationAggregate/Notification.cs` | Domain entity |
| `Api.Core/.../NotificationAggregate/NotificationConfig.cs` | Config entity |
| `Api.Infrastructure/Services/NotificationService.cs` | Orchestrator |
| `Api.Infrastructure/Services/NotificationCleanupService.cs` | Cleanup background service |
| `admin/src/stores/notifications.js` | Pinia store |
| `admin/src/services/notification.service.js` | Axios wrappers |
| `admin/src/components/NotificationBell.vue` | UI component |
