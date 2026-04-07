---
title: Order Flow
tags: [order, domain, flow]
updated: 2026-04-07
---

# Order Flow

Vòng đời của một đơn hàng, từ lúc khách đặt đến khi hoàn tất thanh toán.

Xem thêm: [[domain-model]], [[session-flow]], [[promotions]]

---

## State Machine

```
              ┌──────────┐
              │  PENDING  │◄──── Khách đặt / Admin tạo thủ công
              └─────┬─────┘
                    │ Process()
              ┌─────▼─────┐
              │PROCESSING │  ◄── Staff xác nhận đang làm
              └─────┬─────┘
                    │ Complete()
              ┌─────▼─────┐
              │ COMPLETED │  ◄── Xong, có thể thanh toán
              └───────────┘

   Cancel() có thể gọi từ PENDING hoặc PROCESSING
              ┌───────────┐
              │ CANCELLED │
              └───────────┘
```

---

## Luồng 1: Khách đặt qua QR (Client App)

```
1. Khách quét QR → mở /client → chọn bàn
2. Frontend gọi POST /api/sessions (GetOrCreateSession)
   → Backend tìm GuestSession Active của bàn hoặc tạo mới
3. Khách chọn món → giỏ hàng (Pinia cart store)
4. Khách xác nhận → POST /api/orders
   → PlaceOrderHandler: tạo Order + Items, lưu DB
   → order.NotifyCreated() → OrderCreatedEvent → SSE broadcast đến admin
5. Admin nhận notification, xem đơn trên màn hình
```

**Endpoint khách:** `POST /api/orders` (không cần auth, dùng sessionId từ QR token)

---

## Luồng 2: Admin tạo đơn thủ công

```
1. Admin vào trang Create Order (/admin/orders/create)
2. Chọn bàn → backend GetOrCreateSession → lấy sessionId
3. Admin chọn món, số khách, ngày giờ
4. POST /api/admin/orders
5. Ngay sau đó: autoApplyPromotions(orderId) → POST /api/admin/orders/{id}/promotions/auto
6. Redirect sang trang Detail
```

**Endpoint admin:** `POST /api/admin/orders` (cần claim `order.create`)

---

## Luồng 3: Xử lý và hoàn thành

```
Staff/Admin nhìn thấy đơn mới trên danh sách (SSE push hoặc polling)
  → Click "Xác nhận" → PUT /api/admin/orders/{id}/status
     body: { "status": "Processing" }
  → order.Process()

Làm xong:
  → Click "Hoàn thành" → PUT /api/admin/orders/{id}/status
     body: { "status": "Completed" }
  → order.Complete() + OrderCompletedEvent
```

---

## Thanh toán

```
PUT /api/admin/orders/{id}/payment
{
  "paymentStatus": "Paid",
  "paymentMethod": "Cash",
  "amountReceived": 150000,
  "tipAmount": 0
}
→ order.UpdatePayment(...) → OrderPaymentUpdatedEvent
```

Quy tắc: nếu `paymentStatus = Paid` thì `paymentMethod` không được là `Unknown`.

**PaymentMethod values:** `Unknown`, `Cash`, `BankTransfer`

---

## Merge Orders (gộp bàn)

```
POST /api/admin/orders/{primaryId}/merge
{ "secondaryOrderIds": [2, 3] }
```

Logic:
1. Load primary order + secondary orders
2. Với mỗi secondary: `primaryOrder.AddItemForMerge(...)` + `primaryOrder.AddGuestCount(...)`
3. Secondary orders: `secondaryOrder.CancelAsMerged()` (bypass state machine)
4. Save

Kết quả: items được gộp (cộng dồn quantity nếu trùng productId), secondary bị Cancelled.

---

## Split Orders (tách bàn)

```
POST /api/admin/orders/{sourceId}/split
{
  "items": [{ "productId": 1, "quantity": 2 }],
  "newSessionId": "guid-..."
}
```

Logic: tạo Order mới với items được tách ra, giảm/xóa items tương ứng từ source order.

---

## Admin Manual Edit

Admin có thể edit order ở bất kỳ trạng thái nào (bypass state machine):

```
PUT /api/admin/orders/{id}/edit
{
  "orderedAt": "2026-04-07T10:00:00Z",
  "guestCount": 3,
  "items": [...]
}
```

Gọi `order.UpdateManually(orderedAt, guestCount)` → xóa hết items + promotions, rồi re-add.

---

## Tính toán giá

```
TotalAmount  = sum(item.UnitPrice × item.Quantity - item.DiscountAmount)
TotalDiscount = sum(promotion.DiscountAmount)
FinalAmount  = max(0, TotalAmount - TotalDiscount)
```

Xem chi tiết cách tính discount: [[promotions]]

---

## Endpoints liên quan

| Method | Path | Mô tả |
|--------|------|-------|
| `POST` | `/api/orders` | Khách đặt đơn (guest) |
| `POST` | `/api/admin/orders` | Admin tạo đơn thủ công |
| `GET` | `/api/admin/orders` | Danh sách đơn (paged, filter) |
| `GET` | `/api/admin/orders/{id}` | Chi tiết đơn |
| `PUT` | `/api/admin/orders/{id}/status` | Cập nhật trạng thái |
| `PUT` | `/api/admin/orders/{id}/payment` | Cập nhật thanh toán |
| `PUT` | `/api/admin/orders/{id}/edit` | Edit toàn bộ đơn |
| `POST` | `/api/admin/orders/{id}/merge` | Gộp đơn |
| `POST` | `/api/admin/orders/{id}/split` | Tách đơn |
| `DELETE` | `/api/admin/orders/{id}` | Xóa đơn hàng |
| `PUT` | `/api/admin/orders/{id}/items` | Edit toàn bộ items của đơn |
| `PUT` | `/api/admin/orders/{orderId}/items/{productId}` | Cập nhật một item |
| `PATCH` | `/api/admin/orders/{id}/order-date` | Cập nhật ngày đặt hàng |
| `GET` | `/api/admin/orders/stream` | SSE stream đơn hàng realtime |
| `POST` | `/api/admin/orders/{id}/promotions` | Áp khuyến mãi thủ công |
| `POST` | `/api/admin/orders/{id}/promotions/auto` | Auto-apply khuyến mãi |
| `DELETE` | `/api/admin/orders/{id}/promotions/{promoId}` | Xóa khuyến mãi |
