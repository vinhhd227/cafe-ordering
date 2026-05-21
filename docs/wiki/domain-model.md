---
title: Domain Model
tags: [domain, aggregates, entities, ddd]
updated: 2026-05-11
---

# Domain Model

Táº¥t cáº£ aggregates náº±m trong `api/src/Api.Core/Aggregates/`. Má»—i aggregate cÃ³ thÆ° má»¥c riÃªng gá»“m entity, events, vÃ  specifications.

---

## Quy táº¯c chung

- **Táº¡o entity**: dÃ¹ng static factory `Entity.Create(...)`, khÃ´ng dÃ¹ng `new`
- **Thay Ä‘á»•i state**: gá»i behavior method (`Activate()`, `Close()`, `Process()`...), khÃ´ng set property trá»±c tiáº¿p (táº¥t cáº£ setter lÃ  `private`)
- **Domain events**: Ä‘Äƒng kÃ½ qua `RegisterDomainEvent(new SomeEvent(...))`
- **Soft delete**: gá»i `Delete()` / `Restore()`, khÃ´ng xÃ³a váº­t lÃ½ â€” cÃ¡c entity káº¿ thá»«a `SoftDeletableEntity<TId>` cÃ³ `IsDeleted`, `DeletedAt`

### Hierarchy base classes

```
EntityBase<TId>
  â””â”€â”€ AuditableEntity<TId>           â† CreatedAt, UpdatedAt, CreatedBy, UpdatedBy
        â””â”€â”€ SoftDeletableEntity<TId> â† + IsDeleted, DeletedAt, IsActive
```

---

## Table

**File:** `TableAggregate/Table.cs`
**Base:** `SoftDeletableEntity<int>`, `IAggregateRoot`

| Property | Type | MÃ´ táº£ |
|----------|------|-------|
| `Code` | string | MÃ£ bÃ n (vÃ­ dá»¥: "A1", "B3") |
| `IsActive` | bool | BÃ n cÃ³ Ä‘ang hoáº¡t Ä‘á»™ng khÃ´ng |
| `Status` | `TableStatus` | `Available` / `Occupied` / `Cleaning` |
| `ActiveSessionId` | `Guid?` | FK Ä‘áº¿n GuestSession Ä‘ang má»Ÿ |
| `ZoneId` | `int?` | FK Ä‘áº¿n Zone |
| `QrToken` | `Guid` | Token Ä‘á»ƒ táº¡o QR code |

**Behaviors:**
```csharp
Table.Create(code, zoneId?)     // factory
table.UpdateCode(code)
table.Activate() / Deactivate()
table.AssignZone(zoneId?)
table.OpenSession(sessionId)    // â†’ Status = Occupied, Ä‘Äƒng kÃ½ TableSessionOpenedEvent
table.CloseSession()            // â†’ Status = Cleaning, Ä‘Äƒng kÃ½ TableSessionClosedEvent
table.MarkAvailable()           // â†’ Status = Available
table.RegenerateQrToken()       // QrToken = Guid.NewGuid()
```

**Events:** `TableSessionOpenedEvent`, `TableSessionClosedEvent`

---

## GuestSession

**File:** `GuestSessionAggregate/GuestSession.cs`
**Base:** `AuditableEntity<Guid>`, `IAggregateRoot`

| Property | Type | MÃ´ táº£ |
|----------|------|-------|
| `TableId` | `int?` | FK Ä‘áº¿n Table |
| `Status` | `GuestSessionStatus` | `Active` / `Closed` |
| `Source` | `GuestSessionSource` | `QrCode` / `Manual` |
| `OpenedAt` | `DateTime` | Thá»i Ä‘iá»ƒm má»Ÿ session |
| `ClosedAt` | `DateTime?` | Thá»i Ä‘iá»ƒm Ä‘Ã³ng session |
| `CustomerId` | `string?` | Náº¿u khÃ¡ch Ä‘Äƒng nháº­p, merge session vá»›i customer |

**Behaviors:**
```csharp
GuestSession.Create(tableId)         // â†’ Status = Active, Ä‘Äƒng kÃ½ SessionOpenedEvent â†’ Table.OpenSession()
GuestSession.CreateManual(tableId)   // â†’ Status = Active, Source = Manual, KHÃ”NG Ä‘Äƒng kÃ½ event
session.Close()                      // â†’ Status = Closed, Ä‘Äƒng kÃ½ SessionClosedEvent â†’ Table.CloseSession()
session.MergeWithCustomer(customerId)// â†’ CustomerId = ..., Ä‘Äƒng kÃ½ SessionMergedWithCustomerEvent
```

**Enums:**
- `GuestSessionStatus`: `Active`, `Closed`
- `GuestSessionSource`: `QrCode`, `Manual`

Xem thÃªm: [[session-flow]]

---

## Order

**File:** `OrderAggregate/Order.cs`
**Base:** `AuditableEntity<int>`, `IAggregateRoot`

| Property | Type | MÃ´ táº£ |
|----------|------|-------|
| `OrderNumber` | string | Sá»‘ Ä‘Æ¡n (vÃ­ dá»¥: "ORD-001") |
| `SessionId` | `Guid` | FK Ä‘áº¿n GuestSession |
| `CustomerId` | `string?` | FK Ä‘áº¿n Customer (null náº¿u khÃ¡ch vÃ£ng lai) |
| `DeviceToken` | `string?` | Anonymous device token tá»« client |
| `Status` | `OrderStatus` | Pending / Processing / Completed / Cancelled |
| `PaymentStatus` | `PaymentStatus` | Unpaid / Paid |
| `PaymentMethod` | `PaymentMethod` | `Unknown` / `Cash` / `BankTransfer` |
| `GuestCount` | `int?` | Sá»‘ khÃ¡ch táº¡i bÃ n |
| `AmountReceived` | `decimal?` | Tiá»n khÃ¡ch Ä‘Æ°a |
| `TipAmount` | `decimal` | Tiá»n tip |
| `OrderDate` | `DateTime` | NgÃ y giá» Ä‘áº·t hÃ ng (cÃ³ thá»ƒ chá»‰nh bá»Ÿi admin) |
| `CompletedAt` | `DateTime?` | Thá»i Ä‘iá»ƒm hoÃ n thÃ nh |
| `PaidAt` | `DateTime?` | Thá»i Ä‘iá»ƒm thanh toÃ¡n |
| `TotalAmount` | `decimal` | Tá»•ng tiá»n (tÃ­nh tá»« items) |
| `TotalDiscount` | `decimal` | Tá»•ng giáº£m giÃ¡ (tá»« promotions) |
| `FinalAmount` | `decimal` | Thá»±c thu = Max(0, TotalAmount - TotalDiscount) |
| `Items` | `IReadOnlyCollection<OrderItem>` | Danh sÃ¡ch mÃ³n |
| `Promotions` | `IReadOnlyCollection<OrderPromotion>` | Danh sÃ¡ch khuyáº¿n mÃ£i Ä‘Ã£ Ã¡p |

**Behaviors (state machine):**
```
Pending â†’ Processing â†’ Completed
       â†˜             â†—
         Cancelled
```
```csharp
Order.Create(sessionId, orderNumber, deviceToken?, customerId?, guestCount?)
order.NotifyCreated()           // Ä‘Äƒng kÃ½ OrderCreatedEvent (gá»i SAU khi add items)
order.AddItem(productId, ...)   // thÃªm item, Ä‘Äƒng kÃ½ OrderItemAddedEvent
order.Process()                 // Pending â†’ Processing
order.Complete()                // Processing â†’ Completed, Ä‘Äƒng kÃ½ OrderCompletedEvent
order.Cancel()                  // Pending/Processing â†’ Cancelled
order.UpdatePayment(...)        // cáº­p nháº­t PaymentStatus, Method, AmountReceived
order.ApplyPromotion(...)       // Ã¡p khuyáº¿n mÃ£i (chá»‰ khi Pending)
order.RemovePromotion(...)      // xÃ³a khuyáº¿n mÃ£i (chá»‰ khi Pending)
```

**Admin edit (bypass state machine):**
```csharp
order.UpdateManually(orderedAt?, guestCount)  // xÃ³a háº¿t items + promotions, cáº­p nháº­t metadata
order.AddItemManual(...)                      // thÃªm item bá» qua status guard
order.ForceSetStatus(status)                  // set trá»±c tiáº¿p, khÃ´ng qua state machine
order.SetItemQuantity(productId, ...)         // set sá»‘ lÆ°á»£ng (0 = xÃ³a item)
order.UpdateOrderDate(newDate)                // cáº­p nháº­t ngÃ y Ä‘áº·t (Ä‘iá»u chá»‰nh doanh thu theo ngÃ y)
order.UpdateGuestCount(value)                 // cáº­p nháº­t sá»‘ khÃ¡ch
order.ClearAllItems()                         // xÃ³a toÃ n bá»™ items + promotions
order.RemoveFreeGiftItems()                   // xÃ³a táº¥t cáº£ free gift items (khi item thÆ°á»ng bá»‹ xÃ³a)
order.ResetAllItemDiscounts()                 // reset item-level discounts vá» 0
```

**Merge/Split:**
```csharp
order.AddItemForMerge(...)  // merge: thÃªm item, cá»™ng dá»“n quantity náº¿u trÃ¹ng productId
order.RemoveItem(...)       // split: giáº£m/xÃ³a item
order.CancelAsMerged()      // Ä‘Ã¡nh dáº¥u cancelled khi bá»‹ merge vÃ o order khÃ¡c
order.AddGuestCount(...)    // cá»™ng GuestCount tá»« secondary order
```

Xem thÃªm: [[order-flow]], [[promotions]]

---

## OrderItem

**File:** `OrderAggregate/OrderItem.cs`
KhÃ´ng pháº£i aggregate root â€” thuá»™c Order aggregate.

| Property | Type | MÃ´ táº£ |
|----------|------|-------|
| `ProductId` | int | FK Ä‘áº¿n Product |
| `ProductName` | string | Snapshot tÃªn sáº£n pháº©m táº¡i thá»i Ä‘iá»ƒm Ä‘áº·t |
| `UnitPrice` | decimal | Snapshot giÃ¡ bÃ¡n cá»§a sáº£n pháº©m/variant táº¡i thá»i Ä‘iá»ƒm Ä‘áº·t |
| `OptionAdjustment` | decimal | Tá»•ng giÃ¡ cá»™ng thÃªm tá»« variants hoáº·c addon/topping (denormalized) |
| `Quantity` | int | Sá»‘ lÆ°á»£ng |
| `IsTakeaway` | bool | Mang vá» |
| `IsFreeGift` | bool | Tá»« promotion BUY_X_GET_Y |
| `Note` | string? | Ghi chÃº riÃªng |
| `Discount` | decimal | Item-level discount (tá»« promotion) |
| `TotalPrice` | decimal | `(UnitPrice + OptionAdjustment - Discount) * Quantity` |
| `SelectedOptions` | `IReadOnlyCollection<OrderItemOption>` | CÃ¡c option Ä‘Ã£ chá»n (snapshot) |

**`OrderItemOption`** â€” snapshot variant táº¡i thá»i Ä‘iá»ƒm Ä‘áº·t hÃ ng:

| Property | MÃ´ táº£ |
|----------|-------|
| `OptionValueId` | ID cá»§a `ProductVariantValue` gá»‘c (tham chiáº¿u, khÃ´ng FK cá»©ng) |
| `GroupName` | Snapshot tÃªn nhÃ³m (vÃ­ dá»¥: "Nhiá»‡t Ä‘á»™") |
| `Label` | Snapshot tÃªn giÃ¡ trá»‹ (vÃ­ dá»¥: "NÃ³ng") |
| `PriceAdjustment` | Snapshot giÃ¡ Ä‘iá»u chá»‰nh táº¡i thá»i Ä‘iá»ƒm Ä‘áº·t; legacy cho cÆ¡ cháº¿ cá»™ng thÃªm |

> **LÃ½ do snapshot:** Thay Ä‘á»•i variant/addon sau nÃ y khÃ´ng áº£nh hÆ°á»Ÿng Ä‘Æ¡n hÃ ng cÅ©. CÆ¡ cháº¿ hiá»‡n táº¡i váº«n cá»™ng `PriceAdjustment` vÃ o `OptionAdjustment`; náº¿u variant cáº§n giÃ¡ cá»©ng theo tá»• há»£p, handler nÃªn resolve variant vÃ  snapshot giÃ¡ cuá»‘i cÃ¹ng vÃ o `UnitPrice`.

**Behaviors:**
```csharp
OrderItem.Create(productId, productName, unitPrice, quantity, isTakeaway, isFreeGift, note)
item.AddOption(optionValueId, groupName, label, priceAdjustment)  // cá»™ng vÃ o OptionAdjustment
item.ApplyDiscount(amount)
item.UpdateQuantity(qty)
item.UpdateNote(note)
item.UpdateTakeaway(isTakeaway)
```

---

## Product

**File:** `ProductAggregate/Product.cs`
**Base:** `SoftDeletableEntity<int>`, `IAggregateRoot`

| Property | Type | MÃ´ táº£ |
|----------|------|-------|
| `Name` | string | TÃªn sáº£n pháº©m |
| `Description` | string? | MÃ´ táº£ |
| `Price` | decimal | GiÃ¡ bÃ¡n gá»‘c (cho phÃ©p = 0) |
| `CostPrice` | decimal? | GiÃ¡ vá»‘n |
| `DiscountPrice` | decimal? | GiÃ¡ khuyáº¿n mÃ£i |
| `Sku` | string? | MÃ£ SKU ná»™i bá»™ |
| `Barcode` | string? | Barcode sáº£n pháº©m |
| `ImageUrl` | string? | URL áº£nh |
| `CategoryId` | int? | FK Ä‘áº¿n Category (nullable â€” sáº£n pháº©m cÃ³ thá»ƒ khÃ´ng thuá»™c danh má»¥c) |
| `IsActive` | bool | Äang bÃ¡n hay khÃ´ng |
| `IsAccompaniment` | bool | MÃ³n Ä‘i kÃ¨m (khÃ´ng tÃ­nh vÃ o doanh thu chÃ­nh) |
| `EstimatedPrepMinutes` | int? | Thá»i gian pha cháº¿ Æ°á»›c tÃ­nh |
| `VariantGroups` | `IReadOnlyCollection<ProductVariantGroup>` | CÃ¡c nhÃ³m variant cá»§a sáº£n pháº©m |

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
product.ReplaceVariantGroups(groups)  // clear + recreate toÃ n bá»™ variant groups
```

**Events:** `ProductCreatedEvent`, `ProductUpdatedEvent`, `ProductActivatedEvent`, `ProductDeactivatedEvent`
â†’ Má»i sá»± kiá»‡n Ä‘á»u invalidate public menu cache qua `InvalidateMenuCacheHandler`.

### ProductVariantGroup

NhÃ³m option **inline theo tá»«ng sáº£n pháº©m** dÃ¹ng Ä‘á»ƒ táº¡o biáº¿n thá»ƒ cá»§a mÃ³n (vÃ­ dá»¥: "Nhiá»‡t Ä‘á»™", "Size", "Má»©c Ä‘Æ°á»ng"). TÃªn nÃ y thay cho cÃ¡ch gá»i attribute cÅ© Ä‘á»ƒ trÃ¡nh hiá»ƒu nháº§m Ä‘Ã¢y lÃ  generic metadata; nÃ³ lÃ  option cáº¥u thÃ nh variant cá»§a product.

> **Quy táº¯c Ä‘á»‹nh giÃ¡ khuyáº¿n nghá»‹:** Náº¿u má»™t sáº£n pháº©m cÃ³ nhiá»u `ProductVariantGroup`, sá»‘ tá»• há»£p giÃ¡ phÃ¡t sinh lÃ  tÃ­ch sá»‘ giÃ¡ trá»‹ cá»§a tá»«ng group (`v1 * v2 * ... * vN`). CÆ¡ cháº¿ `PriceAdjustment` chá»‰ há»£p lÃ½ khi giÃ¡ tháº­t sá»± lÃ  `base price + adjustment`. Náº¿u giÃ¡ phá»¥ thuá»™c vÃ o tá»• há»£p lá»±a chá»n, nÃªn thÃªm `ProductVariant`/`ProductVariantCombination` chá»©a táº­p `ProductVariantValueId` vÃ  `Price`, rá»“i snapshot `variant.Price` vÃ o `OrderItem.UnitPrice`.

VÃ­ dá»¥ CÃ  phÃª cÃ³ 2 group:

| Group | Values |
|-------|--------|
| Size | M, L |
| Nhiá»‡t Ä‘á»™ | NÃ³ng, ÄÃ¡ |

Sáº½ cÃ³ 4 biáº¿n thá»ƒ giÃ¡ cáº§n quáº£n lÃ½: M/NÃ³ng, M/ÄÃ¡, L/NÃ³ng, L/ÄÃ¡. Náº¿u L/ÄÃ¡ khÃ´ng báº±ng `giÃ¡ M/NÃ³ng + phá»¥ thu L + phá»¥ thu ÄÃ¡`, khÃ´ng nÃªn dÃ¹ng cá»™ng thÃªm; nÃªn khai bÃ¡o giÃ¡ cá»©ng cho tá»«ng tá»• há»£p.

**DB table:** `business.ProductVariantGroups`

| Property | Type | MÃ´ táº£ |
|----------|------|-------|
| `ProductId` | int | FK Ä‘áº¿n Product |
| `Name` | string | TÃªn nhÃ³m |
| `IsRequired` | bool | Báº¯t buá»™c chá»n |
| `SelectionType` | `OptionSelectionType` | `Single` (chá»n 1) / `Multiple` (chá»n nhiá»u) |
| `DisplayOrder` | int | Thá»© tá»± hiá»ƒn thá»‹ |
| `Values` | `IReadOnlyCollection<ProductVariantValue>` | CÃ¡c giÃ¡ trá»‹ trong nhÃ³m |

### ProductVariantValue

Má»™t giÃ¡ trá»‹ cá»¥ thá»ƒ trong nhÃ³m variant (vÃ­ dá»¥: "NÃ³ng", "Size L").

**DB table:** `business.ProductVariantValues`

| Property | Type | MÃ´ táº£ |
|----------|------|-------|
| `GroupId` | int | FK Ä‘áº¿n ProductVariantGroup |
| `Label` | string | TÃªn hiá»ƒn thá»‹ |
| `Price` | decimal | GiÃ¡ cá»§a giÃ¡ trá»‹ variant. Khi cÃ³ `ProductVariant`, giÃ¡ bÃ¡n cuá»‘i cÃ¹ng láº¥y tá»« `ProductVariant.Price` |
| `IsDefault` | bool | GiÃ¡ trá»‹ máº·c Ä‘á»‹nh |
| `DisplayOrder` | int | Thá»© tá»± hiá»ƒn thá»‹ |

**API cáº¥u hÃ¬nh variant groups:** `PUT /api/products/{id}/variant-groups` â€” thay toÃ n bá»™ variant groups (clear + recreate).

```json
{
  "groups": [
    {
      "name": "Nhiá»‡t Ä‘á»™", "isRequired": true, "selectionType": "Single",
      "values": [
        { "label": "NÃ³ng", "price": 0, "isDefault": true },
        { "label": "Láº¡nh", "price": 0, "isDefault": false }
      ]
    },
    {
      "name": "Size", "isRequired": false, "selectionType": "Single",
      "values": [
        { "label": "M", "price": 0,    "isDefault": true },
        { "label": "L", "price": 5000, "isDefault": false }
      ]
    }
  ]
}
```

**Luá»“ng khi khÃ¡ch Ä‘áº·t mÃ³n:**
1. Client gá»­i `selectedVariantValueIds: [1, 3]` (ID cá»§a `ProductVariantValue`)
2. Handler validate: má»—i ID pháº£i thuá»™c variant group cá»§a sáº£n pháº©m Ä‘Ã³
3. Handler resolve tá»• há»£p variant Ä‘Ã£ chá»n. Vá»›i cÆ¡ cháº¿ hiá»‡n táº¡i, `Order.AddItem(...)` nháº­n `List<OrderItemOptionData>` vÃ  má»—i item gá»i `item.AddOption(...)` Ä‘á»ƒ ghi snapshot + cá»™ng `OptionAdjustment`.
4. Vá»›i cÆ¡ cháº¿ giÃ¡ cá»©ng khuyáº¿n nghá»‹, handler tÃ¬m variant/combination tÆ°Æ¡ng á»©ng vÃ  truyá»n giÃ¡ cuá»‘i cÃ¹ng vÃ o `OrderItem.UnitPrice`; selected variants váº«n Ä‘Æ°á»£c snapshot Ä‘á»ƒ in bill, bÃ¡o báº¿p vÃ  audit.

---

## ProductOptionGroup

**File:** `ProductOptionGroupAggregate/ProductOptionGroup.cs`
**Base:** `SoftDeletableEntity<int>`, `IAggregateRoot`

NhÃ³m topping/addon **dÃ¹ng chung** (reusable) â€” tÃ¡ch biá»‡t vá»›i `ProductVariantGroup` (inline cá»§a tá»«ng sáº£n pháº©m). Gáº¯n vÃ o sáº£n pháº©m qua `ProductOptionGroupMapping`.

> **PhÃ¢n biá»‡t hai loáº¡i option:**
> - `ProductVariantGroup` â€” inline, chá»‰ thuá»™c 1 sáº£n pháº©m, dÃ¹ng Ä‘á»ƒ táº¡o biáº¿n thá»ƒ/combination cá»§a chÃ­nh sáº£n pháº©m Ä‘Ã³. NÃªn Ä‘á»‹nh giÃ¡ báº±ng giÃ¡ cá»©ng theo tá»• há»£p náº¿u nhiá»u group áº£nh hÆ°á»Ÿng giÃ¡.
> - `ProductOptionGroup` â€” standalone, cÃ³ thá»ƒ gáº¯n vÃ o nhiá»u sáº£n pháº©m, dÃ¹ng cho addon/topping cá»™ng thÃªm Ä‘á»™c láº­p (vÃ­ dá»¥: "Topping trÃ¢n chÃ¢u" dÃ¹ng cho cáº£ TrÃ  sá»¯a láº«n Smoothie).

| Property | Type | MÃ´ táº£ |
|----------|------|-------|
| `Name` | string | TÃªn nhÃ³m (vÃ­ dá»¥: "Topping", "ÄÃ¡") |
| `IsRequired` | bool | Báº¯t buá»™c chá»n Ã­t nháº¥t 1 giÃ¡ trá»‹ |
| `AllowMultiple` | bool | Cho phÃ©p chá»n nhiá»u giÃ¡ trá»‹ |
| `AllowQuantity` | bool | Cho phÃ©p nháº­p sá»‘ lÆ°á»£ng cho tá»«ng giÃ¡ trá»‹ |
| `IsActive` | bool | Äang hoáº¡t Ä‘á»™ng |
| `DisplayOrder` | int | Thá»© tá»± hiá»ƒn thá»‹ |
| `Values` | `IReadOnlyCollection<ProductOptionValue>` | Danh sÃ¡ch giÃ¡ trá»‹ |
| `Mappings` | `IReadOnlyCollection<ProductOptionGroupMapping>` | Sáº£n pháº©m Ä‘ang dÃ¹ng nhÃ³m nÃ y |

### ProductOptionValue

Má»™t giÃ¡ trá»‹ trong nhÃ³m topping (vÃ­ dá»¥: "TrÃ¢n chÃ¢u tráº¯ng", "Tháº¡ch phÃ´ mai").

**DB table:** `business.ProductOptionValues`

| Property | Type | MÃ´ táº£ |
|----------|------|-------|
| `GroupId` | int | FK Ä‘áº¿n ProductOptionGroup |
| `Name` | string | TÃªn hiá»ƒn thá»‹ |
| `Price` | decimal | GiÃ¡ cá»™ng thÃªm (0 = miá»…n phÃ­) |
| `CostPrice` | decimal? | GiÃ¡ vá»‘n |
| `IsInStock` | bool | CÃ²n hÃ ng â€” náº¿u false thÃ¬ khÃ´ng cho chá»n |
| `DisplayOrder` | int | Thá»© tá»± hiá»ƒn thá»‹ |

### ProductOptionGroupMapping

Junction entity liÃªn káº¿t Product â†” ProductOptionGroup (many-to-many tÆ°á»ng minh).

**DB table:** `business.ProductOptionGroupMappings`

| Property | Type | MÃ´ táº£ |
|----------|------|-------|
| `ProductId` | int | FK Ä‘áº¿n Product |
| `GroupId` | int | FK Ä‘áº¿n ProductOptionGroup |
| `DisplayOrder` | int | Thá»© tá»± hiá»ƒn thá»‹ nhÃ³m nÃ y trÃªn sáº£n pháº©m cá»¥ thá»ƒ Ä‘Ã³ |

---

## Category

**File:** `CategoryAggregate/Category.cs`
**Base:** `SoftDeletableEntity<int>`, `IAggregateRoot`

| Property | MÃ´ táº£ |
|----------|-------|
| `Name` | TÃªn danh má»¥c |
| `Description` | MÃ´ táº£ |
| `DisplayOrder` | Thá»© tá»± hiá»ƒn thá»‹ |
| `IsActive` | Äang hiá»ƒn thá»‹ hay khÃ´ng |

**Events:** `CategoryCreatedEvent`, `CategoryActivatedEvent`, `CategoryDeactivatedEvent`, `CategoryUpdatedEvent`

---

## Customer

**File:** `CustomerAggregate/Customer.cs`
**Base:** `AuditableEntity<string>` (Id lÃ  Guid dáº¡ng string)

| Property | MÃ´ táº£ |
|----------|-------|
| `Email` | Email khÃ¡ch hÃ ng |
| `FullName` | Há» tÃªn |
| `PhoneNumber` | Sá»‘ Ä‘iá»‡n thoáº¡i |
| `Tier` | `CustomerTier` SmartEnum (Regular/Silver/Gold/Platinum) |
| `TotalSpent` | Tá»•ng tiá»n Ä‘Ã£ chi |
| `LoyaltyPoints` | Äiá»ƒm tÃ­ch lÅ©y |

**Events:** `CustomerCreatedEvent`, `CustomerEmailChangedEvent`, `CustomerTierUpgradedEvent`

---

## Zone

**File:** `ZoneAggregate/Zone.cs`
Khu vá»±c cá»§a quÃ¡n (vÃ­ dá»¥: Táº§ng 1, NgoÃ i trá»i, VIP). Table thuá»™c vá» má»™t Zone.

---

## Expense

**File:** `ExpenseAggregate/Expense.cs`
**Base:** `AuditableEntity<int>`, `IAggregateRoot`
Quáº£n lÃ½ chi phÃ­ váº­n hÃ nh cá»§a quÃ¡n.

| Property | MÃ´ táº£ |
|----------|-------|
| `ItemName` | TÃªn khoáº£n chi |
| `Amount` | Sá»‘ tiá»n |
| `Category` | `ExpenseCategory` SmartEnum |
| `ExpenseDate` | NgÃ y chi |
| `Note` | Ghi chÃº |

---

## Notification

**File:** `NotificationAggregate/Notification.cs`
Xem chi tiáº¿t: [[notifications]]

---

## Promotion

**File:** `PromotionAggregate/Promotion.cs`
Xem chi tiáº¿t: [[promotions]]
