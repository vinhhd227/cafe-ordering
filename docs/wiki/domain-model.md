---
title: Domain Model
tags: [domain, aggregates, entities, ddd]
updated: 2026-05-11
---

# Domain Model

Tất cả aggregates nằm trong `api/src/Api.Core/Aggregates/`. Mỗi aggregate có thư mục riêng gồm entity, events, và specifications.

---

## Quy tắc chung

- **Tạo entity**: dùng static factory `Entity.Create(...)`, không dùng `new`
- **Thay đổi state**: gọi behavior method (`Activate()`, `Close()`, `Process()`...), không set property trực tiếp (tất cả setter là `private`)
- **Domain events**: đăng ký qua `RegisterDomainEvent(new SomeEvent(...))`
- **Soft delete**: gọi `Delete()` / `Restore()`, không xóa vật lý — các entity kế thừa `SoftDeletableEntity<TId>` có `IsDeleted`, `DeletedAt`

### Hierarchy base classes

```
EntityBase<TId>
  └── AuditableEntity<TId>           ← CreatedAt, UpdatedAt, CreatedBy, UpdatedBy
        └── SoftDeletableEntity<TId> ← + IsDeleted, DeletedAt, IsActive
```

---

## Table

**File:** `TableAggregate/Table.cs`
**Base:** `SoftDeletableEntity<int>`, `IAggregateRoot`

| Property | Type | Mô tả |
|----------|------|-------|
| `Code` | string | Mã bàn (ví dụ: "A1", "B3") |
| `IsActive` | bool | Bàn có đang hoạt động không |
| `Status` | `TableStatus` | `Available` / `Occupied` / `Cleaning` |
| `ActiveSessionId` | `Guid?` | FK đến GuestSession đang mở |
| `ZoneId` | `int?` | FK đến Zone |
| `QrToken` | `Guid` | Token để tạo QR code |

**Behaviors:**
```csharp
Table.Create(code, zoneId?)     // factory
table.UpdateCode(code)
table.Activate() / Deactivate()
table.AssignZone(zoneId?)
table.OpenSession(sessionId)    // → Status = Occupied, đăng ký TableSessionOpenedEvent
table.CloseSession()            // → Status = Cleaning, đăng ký TableSessionClosedEvent
table.MarkAvailable()           // → Status = Available
table.RegenerateQrToken()       // QrToken = Guid.NewGuid()
```

**Events:** `TableSessionOpenedEvent`, `TableSessionClosedEvent`

---

## GuestSession

**File:** `GuestSessionAggregate/GuestSession.cs`
**Base:** `AuditableEntity<Guid>`, `IAggregateRoot`

| Property | Type | Mô tả |
|----------|------|-------|
| `TableId` | `int?` | FK đến Table |
| `Status` | `GuestSessionStatus` | `Active` / `Closed` |
| `Source` | `GuestSessionSource` | `QrCode` / `Manual` |
| `OpenedAt` | `DateTime` | Thời điểm mở session |
| `ClosedAt` | `DateTime?` | Thời điểm đóng session |
| `CustomerId` | `string?` | Nếu khách đăng nhập, merge session với customer |

**Behaviors:**
```csharp
GuestSession.Create(tableId)         // → Status = Active, đăng ký SessionOpenedEvent → Table.OpenSession()
GuestSession.CreateManual(tableId)   // → Status = Active, Source = Manual, KHÔNG đăng ký event
session.Close()                      // → Status = Closed, đăng ký SessionClosedEvent → Table.CloseSession()
session.MergeWithCustomer(customerId)// → CustomerId = ..., đăng ký SessionMergedWithCustomerEvent
```

**Enums:**
- `GuestSessionStatus`: `Active`, `Closed`
- `GuestSessionSource`: `QrCode`, `Manual`

Xem thêm: [[session-flow]]

---

## Order

**File:** `OrderAggregate/Order.cs`
**Base:** `AuditableEntity<int>`, `IAggregateRoot`

| Property | Type | Mô tả |
|----------|------|-------|
| `OrderNumber` | string | Số đơn (ví dụ: "ORD-001") |
| `SessionId` | `Guid` | FK đến GuestSession |
| `CustomerId` | `string?` | FK đến Customer (null nếu khách vãng lai) |
| `DeviceToken` | `string?` | Anonymous device token từ client |
| `Status` | `OrderStatus` | Pending / Processing / Completed / Cancelled |
| `PaymentStatus` | `PaymentStatus` | Unpaid / Paid |
| `PaymentMethod` | `PaymentMethod` | `Unknown` / `Cash` / `BankTransfer` |
| `GuestCount` | `int?` | Số khách tại bàn |
| `AmountReceived` | `decimal?` | Tiền khách đưa |
| `TipAmount` | `decimal` | Tiền tip |
| `OrderDate` | `DateTime` | Ngày giờ đặt hàng (có thể chỉnh bởi admin) |
| `CompletedAt` | `DateTime?` | Thời điểm hoàn thành |
| `PaidAt` | `DateTime?` | Thời điểm thanh toán |
| `TotalAmount` | `decimal` | Tổng tiền (tính từ items) |
| `TotalDiscount` | `decimal` | Tổng giảm giá (từ promotions) |
| `FinalAmount` | `decimal` | Thực thu = Max(0, TotalAmount - TotalDiscount) |
| `Items` | `IReadOnlyCollection<OrderItem>` | Danh sách món |
| `Promotions` | `IReadOnlyCollection<OrderPromotion>` | Danh sách khuyến mãi đã áp |

**Behaviors (state machine):**
```
Pending → Processing → Completed
       ↘             ↗
         Cancelled
```
```csharp
Order.Create(sessionId, orderNumber, deviceToken?, customerId?, guestCount?)
order.NotifyCreated()           // đăng ký OrderCreatedEvent (gọi SAU khi add items)
order.AddItem(productId, ...)   // thêm item, đăng ký OrderItemAddedEvent
order.Process()                 // Pending → Processing
order.Complete()                // Processing → Completed, đăng ký OrderCompletedEvent
order.Cancel()                  // Pending/Processing → Cancelled
order.UpdatePayment(...)        // cập nhật PaymentStatus, Method, AmountReceived
order.ApplyPromotion(...)       // áp khuyến mãi (chỉ khi Pending)
order.RemovePromotion(...)      // xóa khuyến mãi (chỉ khi Pending)
```

**Admin edit (bypass state machine):**
```csharp
order.UpdateManually(orderedAt?, guestCount)  // xóa hết items + promotions, cập nhật metadata
order.AddItemManual(...)                      // thêm item bỏ qua status guard
order.ForceSetStatus(status)                  // set trực tiếp, không qua state machine
order.SetItemQuantity(productId, ...)         // set số lượng (0 = xóa item)
order.UpdateOrderDate(newDate)                // cập nhật ngày đặt (điều chỉnh doanh thu theo ngày)
order.UpdateGuestCount(value)                 // cập nhật số khách
order.ClearAllItems()                         // xóa toàn bộ items + promotions
order.RemoveFreeGiftItems()                   // xóa tất cả free gift items (khi item thường bị xóa)
order.ResetAllItemDiscounts()                 // reset item-level discounts về 0
```

**Merge/Split:**
```csharp
order.AddItemForMerge(...)  // merge: thêm item, cộng dồn quantity nếu trùng productId
order.RemoveItem(...)       // split: giảm/xóa item
order.CancelAsMerged()      // đánh dấu cancelled khi bị merge vào order khác
order.AddGuestCount(...)    // cộng GuestCount từ secondary order
```

Xem thêm: [[order-flow]], [[promotions]]

---

## OrderItem

**File:** `OrderAggregate/OrderItem.cs`
Không phải aggregate root — thuộc Order aggregate.

| Property | Type | Mô tả |
|----------|------|-------|
| `ProductId` | int | FK đến Product |
| `ProductName` | string | Snapshot tên sản phẩm tại thời điểm đặt |
| `UnitPrice` | decimal | Snapshot giá gốc sản phẩm |
| `OptionAdjustment` | decimal | Tổng price adjustment của tất cả options đã chọn (denormalized) |
| `Quantity` | int | Số lượng |
| `IsTakeaway` | bool | Mang về |
| `IsFreeGift` | bool | Từ promotion BUY_X_GET_Y |
| `Note` | string? | Ghi chú riêng |
| `Discount` | decimal | Item-level discount (từ promotion) |
| `TotalPrice` | decimal | `(UnitPrice + OptionAdjustment - Discount) * Quantity` |
| `SelectedOptions` | `IReadOnlyCollection<OrderItemOption>` | Các option đã chọn (snapshot) |

**`OrderItemOption`** — snapshot attribute tại thời điểm đặt hàng:

| Property | Mô tả |
|----------|-------|
| `OptionValueId` | ID của `ProductAttributeValue` gốc (tham chiếu, không FK cứng) |
| `GroupName` | Snapshot tên nhóm (ví dụ: "Nhiệt độ") |
| `Label` | Snapshot tên giá trị (ví dụ: "Nóng") |
| `PriceAdjustment` | Snapshot giá điều chỉnh tại thời điểm đặt |

> **Lý do snapshot:** Thay đổi attribute sau này không ảnh hưởng đơn hàng cũ. `OptionAdjustment` trên `OrderItem` được cộng dồn mỗi khi `AddOption()` được gọi — tránh lazy-load khi tính `TotalPrice`.

**Behaviors:**
```csharp
OrderItem.Create(productId, productName, unitPrice, quantity, isTakeaway, isFreeGift, note)
item.AddOption(optionValueId, groupName, label, priceAdjustment)  // cộng vào OptionAdjustment
item.ApplyDiscount(amount)
item.UpdateQuantity(qty)
item.UpdateNote(note)
item.UpdateTakeaway(isTakeaway)
```

---

## Product

**File:** `ProductAggregate/Product.cs`
**Base:** `SoftDeletableEntity<int>`, `IAggregateRoot`

| Property | Type | Mô tả |
|----------|------|-------|
| `Name` | string | Tên sản phẩm |
| `Description` | string? | Mô tả |
| `Price` | decimal | Giá bán gốc (cho phép = 0) |
| `CostPrice` | decimal? | Giá vốn |
| `DiscountPrice` | decimal? | Giá khuyến mãi |
| `Sku` | string? | Mã SKU nội bộ |
| `Barcode` | string? | Barcode sản phẩm |
| `ImageUrl` | string? | URL ảnh |
| `CategoryId` | int? | FK đến Category (nullable — sản phẩm có thể không thuộc danh mục) |
| `IsActive` | bool | Đang bán hay không |
| `IsAccompaniment` | bool | Món đi kèm (không tính vào doanh thu chính) |
| `EstimatedPrepMinutes` | int? | Thời gian pha chế ước tính |
| `AttributeGroups` | `IReadOnlyCollection<ProductAttributeGroup>` | Các nhóm attribute của sản phẩm |

**Behaviors:**
```csharp
Product.Create(name, price, categoryId?, description?, imageUrl?, isAccompaniment?, costPrice?, discountPrice?, sku?, barcode?)
product.UpdateDetails(name, price, description?, imageUrl?)
product.SetCostPrice(value?) / SetDiscountPrice(value?)
product.SetSku(value?) / SetBarcode(value?)
product.SetEstimatedPrepTime(minutes?)
product.UpdateAccompaniment(value)
product.ChangeCategory(categoryId?)
product.Activate() / Deactivate()
product.Delete() / Restore()
product.ReplaceAttributeGroups(groups)  // clear + recreate toàn bộ attribute groups
```

**Events:** `ProductCreatedEvent`, `ProductUpdatedEvent`, `ProductActivatedEvent`, `ProductDeactivatedEvent`
→ Mọi sự kiện đều invalidate public menu cache qua `InvalidateMenuCacheHandler`.

### ProductAttributeGroup

Nhóm attribute của sản phẩm (ví dụ: "Nhiệt độ", "Size", "Mức đường").

**DB table:** `business.ProductAttributeGroups`

| Property | Type | Mô tả |
|----------|------|-------|
| `ProductId` | int | FK đến Product |
| `Name` | string | Tên nhóm |
| `IsRequired` | bool | Bắt buộc chọn |
| `SelectionType` | `OptionSelectionType` | `Single` (chọn 1) / `Multiple` (chọn nhiều) |
| `DisplayOrder` | int | Thứ tự hiển thị |
| `Values` | `IReadOnlyCollection<ProductAttributeValue>` | Các giá trị trong nhóm |

### ProductAttributeValue

Một giá trị cụ thể trong nhóm attribute (ví dụ: "Nóng", "Size L").

**DB table:** `business.ProductAttributeValues`

| Property | Type | Mô tả |
|----------|------|-------|
| `GroupId` | int | FK đến ProductAttributeGroup |
| `Label` | string | Tên hiển thị |
| `PriceAdjustment` | decimal | Thay đổi giá so với giá gốc (0 = không thêm) |
| `IsDefault` | bool | Giá trị mặc định |
| `DisplayOrder` | int | Thứ tự hiển thị |

**API cấu hình attribute groups:** `PUT /api/products/{id}/option-groups` — thay toàn bộ attribute groups (clear + recreate).

```json
{
  "groups": [
    {
      "name": "Nhiệt độ", "isRequired": true, "selectionType": "Single",
      "values": [
        { "label": "Nóng", "priceAdjustment": 0, "isDefault": true },
        { "label": "Lạnh", "priceAdjustment": 0, "isDefault": false }
      ]
    },
    {
      "name": "Size", "isRequired": false, "selectionType": "Single",
      "values": [
        { "label": "M", "priceAdjustment": 0,    "isDefault": true },
        { "label": "L", "priceAdjustment": 5000, "isDefault": false }
      ]
    }
  ]
}
```

**Luồng khi khách đặt món:**
1. Client gửi `selectedOptionValueIds: [1, 3]` (ID của `ProductAttributeValue`)
2. Handler validate: mỗi ID phải thuộc attribute group của sản phẩm đó
3. `Order.AddItem(...)` nhận `List<OrderItemOptionData>` — mỗi item gọi `item.AddOption(...)` để ghi snapshot + cộng `OptionAdjustment`

---

## Category

**File:** `CategoryAggregate/Category.cs`
**Base:** `SoftDeletableEntity<int>`, `IAggregateRoot`

| Property | Mô tả |
|----------|-------|
| `Name` | Tên danh mục |
| `Description` | Mô tả |
| `DisplayOrder` | Thứ tự hiển thị |
| `IsActive` | Đang hiển thị hay không |

**Events:** `CategoryCreatedEvent`, `CategoryActivatedEvent`, `CategoryDeactivatedEvent`, `CategoryUpdatedEvent`

---

## Customer

**File:** `CustomerAggregate/Customer.cs`
**Base:** `AuditableEntity<string>` (Id là Guid dạng string)

| Property | Mô tả |
|----------|-------|
| `Email` | Email khách hàng |
| `FullName` | Họ tên |
| `PhoneNumber` | Số điện thoại |
| `Tier` | `CustomerTier` SmartEnum (Regular/Silver/Gold/Platinum) |
| `TotalSpent` | Tổng tiền đã chi |
| `LoyaltyPoints` | Điểm tích lũy |

**Events:** `CustomerCreatedEvent`, `CustomerEmailChangedEvent`, `CustomerTierUpgradedEvent`

---

## Zone

**File:** `ZoneAggregate/Zone.cs`
Khu vực của quán (ví dụ: Tầng 1, Ngoài trời, VIP). Table thuộc về một Zone.

---

## Expense

**File:** `ExpenseAggregate/Expense.cs`
**Base:** `AuditableEntity<int>`, `IAggregateRoot`
Quản lý chi phí vận hành của quán.

| Property | Mô tả |
|----------|-------|
| `ItemName` | Tên khoản chi |
| `Amount` | Số tiền |
| `Category` | `ExpenseCategory` SmartEnum |
| `ExpenseDate` | Ngày chi |
| `Note` | Ghi chú |

---

## Notification

**File:** `NotificationAggregate/Notification.cs`
Xem chi tiết: [[notifications]]

---

## Promotion

**File:** `PromotionAggregate/Promotion.cs`
Xem chi tiết: [[promotions]]
