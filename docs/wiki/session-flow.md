---
title: Session & Table Lifecycle
tags: [session, table, flow, domain]
updated: 2026-04-07
---

# Session & Table Lifecycle

`GuestSession` đại diện cho một lần ngồi của khách tại bàn. Mỗi Order phải thuộc về một Session.

Xem thêm: [[domain-model]], [[order-flow]]

---

## Table Status

```
Available ◄─────────────────────────── MarkAvailable()
    │                                         ▲
    │ OpenSession()                           │ CloseSession() → Cleaning → staff dọn xong
    ▼                                         │
 Occupied ─────────────────────────────────►Cleaning
```

- `Available` → Khách vào quét QR
- `Occupied` → Đang có session active
- `Cleaning` → Session đã đóng, đang dọn bàn chờ khách mới

---

## Session Lifecycle

### Tạo qua QR (luồng khách)

```
Khách quét QR (chứa /tables/{qrToken})
  → Client app gọi POST /api/sessions
  → GetOrCreateSessionHandler:
      1. Tìm Table theo qrToken
      2. Kiểm tra Table.IsActive
      3. Nếu Table.ActiveSessionId != null → load session đó (trả về existing)
      4. Nếu không → GuestSession.Create(tableId)
                   → SessionOpenedEvent → Table.OpenSession(sessionId)
                   → Table.Status = Occupied, Table.ActiveSessionId = session.Id
  → Trả về { sessionId, tableCode }
```

**QR Token**: mỗi bàn có một `QrToken` (Guid). Khi QR hết hiệu lực, gọi `RegenerateQrToken()` để tạo token mới mà không ảnh hưởng session đang mở.

### Tạo thủ công (admin tạo order)

```
Admin tạo order thủ công → chọn bàn
  → GetOrCreateSessionHandler (cùng handler)
  → Nếu bàn đã có session active → dùng session đó
  → Nếu không → GuestSession.CreateManual(tableId)
              → Source = Manual, KHÔNG đăng ký SessionOpenedEvent
              → KHÔNG thay đổi Table.Status (bàn không chuyển sang Occupied)
```

Lý do: đơn thủ công thường được dùng để ghi lại đơn ngoài bàn (takeaway, giao hàng, v.v.) hoặc điều chỉnh sổ sách. Không muốn ảnh hưởng trạng thái bàn vật lý.

---

## Đóng Session

```
Admin click "Đóng bàn" → POST /api/admin/sessions/{id}/close
  → CloseSessionHandler (hoặc TryAutoCloseSessionHandler):
      1. Load GuestSession
      2. Kiểm tra tất cả orders của session: phải Completed hoặc Cancelled
         (nếu còn Pending/Processing → từ chối đóng)
      3. session.Close()
         → SessionClosedEvent → Table.CloseSession()
         → Table.Status = Cleaning, Table.ActiveSessionId = null
```

**Auto-close**: `TryAutoCloseSessionHandler` — được gọi sau khi order cuối cùng hoàn thành/hủy để tự động đóng session nếu không còn order active.

---

## Merge Session với Customer

```
session.MergeWithCustomer(customerId)
  → CustomerId = customerId
  → SessionMergedWithCustomerEvent
```

Dùng khi khách vãng lai (guest) sau đó đăng nhập — liên kết session ẩn danh với tài khoản customer.

---

## Quy tắc quan trọng

- Một bàn chỉ có tối đa **1 session active** tại một thời điểm
- Session `QrCode` → cập nhật Table.Status
- Session `Manual` → KHÔNG cập nhật Table.Status
- Không thể đóng session khi còn order chưa xử lý
- Xóa session không được — chỉ Close

---

## Endpoints

| Method | Path | Mô tả |
|--------|------|-------|
| `POST` | `/api/sessions` | Tạo/lấy session theo QR token |
| `GET` | `/api/admin/sessions` | Danh sách sessions (admin) |
| `GET` | `/api/admin/sessions/{id}` | Chi tiết session |
| `POST` | `/api/admin/sessions/{id}/close` | Đóng session |
| `GET` | `/api/tables/public` | Danh sách bàn cho khách (không cần auth) |
| `GET` | `/api/admin/tables` | Danh sách bàn cho admin |
| `PUT` | `/api/admin/tables/{id}/available` | Đánh dấu bàn Available |
| `PUT` | `/api/admin/tables/{id}/qr-token` | Tái tạo QR token |
