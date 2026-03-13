# Promotion System — Flow & Architecture

> Tài liệu này mô tả toàn bộ luồng khuyến mãi: data model, business rules, API endpoints, và admin UI.
> Cập nhật lần cuối: 2026-03-13

---

## 1. Data Model

### `Promotion` (aggregate root)

**File:** `api/src/Api.Core/Aggregates/PromotionAggregate/Promotion.cs`
**EF Config:** `api/src/Api.Infrastructure/Data/Config/PromotionConfiguration.cs`
**Table:** `business."Promotions"`

| Property | Type | Mô tả |
|---|---|---|
| `Id` | int | PK |
| `Name` | string (text) | Tên hiển thị, không giới hạn ký tự |
| `Code` | string? | Mã nhập tay (uppercase). **Null = tự động áp dụng** |
| `Description` | string? | Mô tả |
| `DiscountType` | SmartEnum | `PERCENTAGE` / `FIXED` / `BUY_X_GET_Y` |
| `DiscountValue` | decimal | % hoặc VND (BUY_X_GET_Y không dùng) |
| `BuyQuantity` | int? | Chỉ dùng khi BUY_X_GET_Y |
| `GetQuantity` | int? | Chỉ dùng khi BUY_X_GET_Y |
| `Scope` | SmartEnum | `ORDER` / `PRODUCT` / `CATEGORY` |
| `ApplicableProductIds` | `List<int>` | JSON, chỉ dùng khi Scope = PRODUCT |
| `ApplicableCategoryIds` | `List<int>` | JSON, chỉ dùng khi Scope = CATEGORY |
| `StackPolicy` | SmartEnum | `EXCLUSIVE` / `STACKABLE` (mặc định: EXCLUSIVE) |
| `MinOrderAmount` | decimal? | Tổng order tối thiểu để áp dụng. Null = không yêu cầu |
| `StartDate` | DateTime (UTC) | Ngày bắt đầu hiệu lực |
| `EndDate` | DateTime? (UTC) | Ngày hết hạn. Null = không hết hạn |
| `MaxUsage` | int? | Giới hạn tổng lượt dùng. Null = không giới hạn |
| `CurrentUsage` | int | Số lượt đã dùng (tăng khi áp dụng vào order) |
| `IsActive` | bool | Flag bật/tắt thủ công |
| `IsDeleted` | bool | Soft delete |
| `RowVersion` | byte[] | Concurrency token (PostgreSQL bytea) |

**Validation helpers trên entity:**
```csharp
promo.IsValidAt(utcNow)        // IsActive && !IsDeleted && utcNow >= StartDate && (EndDate == null || utcNow <= EndDate)
promo.HasUsageLeft()           // MaxUsage == null || CurrentUsage < MaxUsage
promo.IsApplicableTo(amount)   // MinOrderAmount == null || amount >= MinOrderAmount
promo.IncrementUsage(orderId)  // CurrentUsage++ + registers PromotionUsedEvent (event not dispatched, see §6)
```

---

### `OrderPromotion` (entity trong Order aggregate)

**File:** `api/src/Api.Core/Aggregates/OrderAggregate/OrderPromotion.cs`
**EF Config:** `api/src/Api.Infrastructure/Data/Config/OrderPromotionConfiguration.cs`
**Table:** `business."OrderPromotions"`

| Property | Type | Ghi chú |
|---|---|---|
| `OrderId` | int | FK → Orders |
| `PromotionId` | int | FK → Promotions |
| `PromoCode` | string (max 200) | Snapshot: `promo.Name` (auto-apply) hoặc `promo.Code` (manual) |
| `DiscountAmount` | decimal (18,2) | Số tiền giảm thực tế đã tính |
| `StackPolicy` | SmartEnum | Snapshot tại thời điểm áp dụng để enforce rule |

> **Lưu ý:** `PromoCode` lưu `promo.Name` cho auto-apply (vì không có Code), và `promo.Code` cho manual.
> Max length là 200 (từng là 50 — đã fix vì gây DB constraint lỗi).

---

## 2. Enums (SmartEnum, lưu dạng UPPERCASE string trong DB)

```
DiscountType:  PERCENTAGE | FIXED | BUY_X_GET_Y
PromotionScope: ORDER | PRODUCT | CATEGORY
StackPolicy:   EXCLUSIVE | STACKABLE
```

---

## 3. Business Rules

### Stack Policy
- **EXCLUSIVE** (mặc định): không thể combine với bất kỳ promo nào khác
  - Nếu order đã có promo EXCLUSIVE → không thể thêm promo mới
  - Nếu promo mới là EXCLUSIVE → không thể thêm nếu order đã có bất kỳ promo nào
- **STACKABLE**: có thể combine với promo STACKABLE khác (không thể combine với EXCLUSIVE)

### Điều kiện áp dụng
1. Order phải ở trạng thái `PENDING`
2. Promotion phải `IsValidAt(now)` — active, không xoá, trong khoảng StartDate/EndDate
3. Promotion phải `HasUsageLeft()`
4. Order total phải >= `MinOrderAmount` (nếu có)
5. `PromotionCalculator.Calculate` phải trả về `TotalDiscount > 0`

### Tính discount — `PromotionCalculator`
**File:** `api/src/Api.UseCases/Promotions/Apply/PromotionCalculator.cs`

```
Scope filter:
  ORDER    → tất cả items
  PRODUCT  → items có ProductId trong ApplicableProductIds
  CATEGORY → items có CategoryId trong ApplicableCategoryIds

PERCENTAGE:
  discountPerUnit = Round(UnitPrice * DiscountValue / 100, 0, AwayFromZero)  // VND, không thập phân
  totalDiscount   = sum(discountPerUnit * Quantity)

FIXED + ORDER scope:
  discount = Min(DiscountValue, orderTotal)

FIXED + PRODUCT/CATEGORY scope:
  discountPerUnit = Min(DiscountValue, UnitPrice)
  totalDiscount   = sum(discountPerUnit * Quantity)

BUY_X_GET_Y:
  groups = totalScopedQty / (buyQty + getQty)
  freeUnits = groups * getQty
  free items = cheapest items first (sort by UnitPrice ASC)
```

---

## 4. Luồng Auto-Apply (không cần mã)

### Điều kiện: `Promotion.Code == null`

```
[Create Order - POST /api/admin/orders (hoặc /api/orders)]
  → PlaceOrderHandler: tạo Order + Items, lưu DB

[Admin Frontend - Create.vue]
  → autoApplyPromotions(orderId)   // gọi ngay sau khi tạo order thành công
  → POST /api/admin/orders/{orderId}/promotions/auto
  → AutoApplyPromotionsHandler:
      1. Load order (kèm Items + Promotions) — OrderByIdWithItemsAndPromotionsSpec
      2. Load all active no-code promos — ActiveNoCodePromotionsSpec
         (filter: !IsDeleted && IsActive && Code == null)
      3. Load products cho category map (dùng cho Scope=CATEGORY)
      4. Foreach promo:
           - IsValidAt(now) && HasUsageLeft() && IsApplicableTo(order.TotalAmount)
           - PromotionCalculator.Calculate → nếu TotalDiscount <= 0 → skip
           - order.ApplyPromotion(promo.Id, promo.Name, discount, stackPolicy)
             (ném exception nếu vi phạm StackPolicy → catch silently → skip)
           - promo.IncrementUsage(orderId)   ← tracked entity, saved as side-effect
      5. Nếu anyApplied: orderRepo.UpdateAsync(order) → SaveChangesAsync
         (lưu cả Order changes + tracked Promotion.CurrentUsage changes)
  → Trả về OrderDto (kèm applied promotions)
  → Frontend redirect sang Detail page
```

**Spec:** `api/src/Api.Core/Aggregates/PromotionAggregate/Specifications/ActiveNoCodePromotionsSpec.cs`
**Endpoint:** `api/src/Api.Web/Endpoints/Orders/AutoApplyPromotions.cs` — `POST /api/admin/orders/{orderId}/promotions/auto`
**Handler:** `api/src/Api.UseCases/Orders/AutoApplyPromotions/AutoApplyPromotionsHandler.cs`
**Policy:** `StaffOrAdmin`

> **Side-effect tracking**: `promoRepo` là `IReadRepositoryBase<Promotion>` nhưng không dùng `AsNoTracking()`,
> nên `promo.IncrementUsage()` được save tự động qua shared `AppDbContext` khi `orderRepo.UpdateAsync` gọi `SaveChangesAsync()`.
> Đây là design hiện tại — không lý tưởng nhưng hoạt động đúng.

---

## 5. Luồng Manual Promo Code

```
[Admin Frontend - Create.vue]
  → validatePromotion(code, cartTotal)
  → GET /api/promotions/validate/{code}?orderAmount={amount}
  → Hiển thị estimatedDiscount, tên khuyến mãi

[Sau khi tạo order]
  → applyPromotionAdmin(orderId, code)
  → POST /api/admin/orders/{orderId}/promotions
  → ApplyPromotionHandler:
      1. Load order + promo theo code
      2. Validate (IsValidAt, HasUsageLeft, IsApplicableTo, StackPolicy)
      3. PromotionCalculator.Calculate
      4. order.ApplyPromotion(promo.Id, promo.Code, discount, stackPolicy)
      5. promo.IncrementUsage(orderId)
      6. Save
```

**Endpoint validate:** `api/src/Api.Web/Endpoints/Promotions/Validate.cs` — `GET /api/promotions/validate/{code}`
**Endpoint apply:** `api/src/Api.Web/Endpoints/Promotions/Apply...` *(xem thư mục Apply)*

---

## 6. Key Files

### Backend

| File | Mô tả |
|---|---|
| `Api.Core/.../PromotionAggregate/Promotion.cs` | Domain entity + behaviors |
| `Api.Core/.../OrderAggregate/Order.cs` | `ApplyPromotion()`, `RemovePromotion()` methods |
| `Api.Core/.../OrderAggregate/OrderPromotion.cs` | Snapshot entity trong Order aggregate |
| `Api.Core/.../PromotionAggregate/Specifications/ActiveNoCodePromotionsSpec.cs` | Filter promo tự động |
| `Api.Core/.../OrderAggregate/Specifications/OrderByIdWithItemsAndPromotionsSpec.cs` | Load order kèm Items + Promotions |
| `Api.UseCases/Promotions/Apply/PromotionCalculator.cs` | Pure static calculator |
| `Api.UseCases/Orders/AutoApplyPromotions/AutoApplyPromotionsHandler.cs` | Handler auto-apply |
| `Api.UseCases/Promotions/Apply/ApplyPromotionHandler.cs` | Handler manual code |
| `Api.UseCases/Promotions/Validate/ValidatePromotionHandler.cs` | Handler validate code |
| `Api.Infrastructure/Data/Config/OrderPromotionConfiguration.cs` | PromoCode max 200 |
| `Api.Infrastructure/Data/Config/OrderConfiguration.cs` | HasMany Items + Promotions |

### Admin Frontend

| File | Mô tả |
|---|---|
| `admin/src/views/orders/Create.vue` | UI tạo order: auto-apply sau create, nhập manual code, nút "Check" xem promo tự động |
| `admin/src/views/orders/Detail.vue` | Hiển thị applied promotions khi `totalDiscount > 0` |
| `admin/src/views/promotions/List.vue` | CRUD promotions |
| `admin/src/services/order.service.js` | `autoApplyPromotions(orderId)`, `applyPromotionAdmin(orderId, code)` |
| `admin/src/services/promotion.service.js` | `getPromotions(params)`, `validatePromotion(code, amount)` |

---

## 7. Migrations Liên Quan

| Migration | Nội dung |
|---|---|
| `20260309093927_AddPromotionSystem` | Tạo bảng Promotions + OrderPromotions |
| `20260313163534_IncreasePromoCodeMaxLength` | `OrderPromotions.PromoCode`: varchar(50) → varchar(200) |

---

## 8. Gotchas & Known Issues

1. **`MediatorDomainEventDispatcher` bị comment out** — domain events (`PromotionUsedEvent`, `OrderPromotionAppliedEvent`, v.v.) được đăng ký nhưng không bao giờ dispatch.

2. **`CurrentUsage` saved via side-effect** — `IReadRepositoryBase<Promotion>` không force AsNoTracking, nên tracked entity thay đổi được save cùng lúc với Order. Nếu sau này thêm `AsNoTracking()` vào `ActiveNoCodePromotionsSpec`, cần đổi sang `IRepositoryBase<Promotion>` + explicit `UpdateAsync`.

3. **StartDate timezone** — `CreatePromotionHandler` dùng `DateTime.SpecifyKind(cmd.StartDate, DateTimeKind.Utc)` (treat-as-UTC, không convert). Frontend cần gửi đúng UTC ISO string.

4. **Auto-apply là best-effort** — lỗi trong `autoApplyPromotions` bị `Create.vue` bỏ qua silently. Nếu cần biết lý do fail, kiểm tra API log.
