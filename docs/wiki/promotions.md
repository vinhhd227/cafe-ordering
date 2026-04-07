---
title: Hệ thống Khuyến mãi
tags: [promotion, discount, order, domain]
updated: 2026-04-07
---

# Hệ thống Khuyến mãi

Xem thêm: [[order-flow]], [[domain-model]]

---

## Promotion Entity

**File:** `Api.Core/Aggregates/PromotionAggregate/Promotion.cs`
**Table:** `business."Promotions"`

| Property | Type | Mô tả |
|---|---|---|
| `Name` | string | Tên hiển thị |
| `Code` | string? | Mã nhập tay (uppercase). **Null = tự động áp dụng** |
| `Description` | string? | Mô tả |
| `DiscountType` | SmartEnum | `PERCENTAGE` / `FIXED` / `BUY_X_GET_Y` |
| `DiscountValue` | decimal | % hoặc VND (BUY_X_GET_Y không dùng field này) |
| `BuyQuantity` | int? | Chỉ dùng khi BUY_X_GET_Y |
| `GetQuantity` | int? | Chỉ dùng khi BUY_X_GET_Y |
| `Scope` | SmartEnum | `ORDER` / `PRODUCT` / `CATEGORY` |
| `ApplicableProductIds` | `List<int>` | JSON — chỉ khi Scope = PRODUCT |
| `ApplicableCategoryIds` | `List<int>` | JSON — chỉ khi Scope = CATEGORY |
| `StackPolicy` | SmartEnum | `EXCLUSIVE` / `STACKABLE` (default: EXCLUSIVE) |
| `MinOrderAmount` | decimal? | Tổng order tối thiểu. Null = không yêu cầu |
| `StartDate` | DateTime (UTC) | Ngày bắt đầu |
| `EndDate` | DateTime? (UTC) | Ngày hết hạn. Null = không hết hạn |
| `MaxUsage` | int? | Giới hạn lượt dùng. Null = không giới hạn |
| `CurrentUsage` | int | Số lượt đã dùng |

**Validation helpers:**
```csharp
promo.IsValidAt(utcNow)       // active + không xoá + trong khoảng StartDate/EndDate
promo.HasUsageLeft()          // MaxUsage == null || CurrentUsage < MaxUsage
promo.IsApplicableTo(amount)  // MinOrderAmount == null || amount >= MinOrderAmount
promo.IncrementUsage(orderId) // CurrentUsage++ + đăng ký PromotionUsedEvent
```

---

## Business Rules

### Stack Policy
- **EXCLUSIVE** (default): không combine với bất kỳ promo nào
  - Nếu order đã có promo EXCLUSIVE → không thêm được promo mới
  - Nếu promo mới là EXCLUSIVE → không thêm nếu order đã có bất kỳ promo
- **STACKABLE**: combine được với promo STACKABLE khác (không combine với EXCLUSIVE)

### Điều kiện áp dụng
1. Order phải ở trạng thái `PENDING`
2. `promo.IsValidAt(now)` → true
3. `promo.HasUsageLeft()` → true
4. `order.TotalAmount >= promo.MinOrderAmount` (nếu có)
5. `PromotionCalculator.Calculate()` trả về `TotalDiscount > 0`

---

## Tính Discount — PromotionCalculator

**File:** `Api.UseCases/Promotions/Apply/PromotionCalculator.cs`

```
Scope filter:
  ORDER    → tất cả items
  PRODUCT  → items có ProductId trong ApplicableProductIds
  CATEGORY → items có CategoryId trong ApplicableCategoryIds

PERCENTAGE:
  discountPerUnit = Round(UnitPrice × DiscountValue / 100, 0, AwayFromZero)
  totalDiscount   = sum(discountPerUnit × Quantity)

FIXED + ORDER scope:
  discount = Min(DiscountValue, orderTotal)

FIXED + PRODUCT/CATEGORY scope:
  discountPerUnit = Min(DiscountValue, UnitPrice)
  totalDiscount   = sum(discountPerUnit × Quantity)

BUY_X_GET_Y:
  groups    = totalScopedQty / (BuyQuantity + GetQuantity)
  freeUnits = groups × GetQuantity
  free items = cheapest items first (sort by UnitPrice ASC)
  → thêm IsFreeGift = true items vào order
```

---

## Auto-Apply (Code = null)

Khuyến mãi tự động áp dụng sau khi tạo order, không cần nhập mã.

```
PlaceOrderHandler → tạo Order + Items → lưu DB

Frontend (Admin Create.vue):
  autoApplyPromotions(orderId)
  → POST /api/admin/orders/{orderId}/promotions/auto
  → AutoApplyPromotionsHandler:
      1. Load order (kèm Items + Promotions)
      2. Load tất cả active no-code promos (Code == null)
      3. Load products để map CategoryId (cho Scope = CATEGORY)
      4. Foreach promo: validate → Calculate → order.ApplyPromotion(...)
         (StackPolicy vi phạm → bỏ qua silently)
         promo.IncrementUsage(orderId) ← tracked entity, saved as side-effect
      5. orderRepo.UpdateAsync(order) → SaveChangesAsync
  → Trả về OrderDto có kèm applied promotions
```

> **Lưu ý side-effect**: `promo.IncrementUsage()` được save tự động vì `Promotion` entity đang được tracked bởi cùng `AppDbContext`. Nếu sau này thêm `AsNoTracking()` vào spec thì phải explicit `UpdateAsync`.

---

## Manual Promo Code

```
Admin nhập mã → validatePromotion(code, cartTotal)
  → GET /api/promotions/validate/{code}?orderAmount={amount}
  → Hiển thị estimatedDiscount, tên khuyến mãi

Sau khi tạo order → applyPromotionAdmin(orderId, code)
  → POST /api/admin/orders/{orderId}/promotions
  → ApplyPromotionHandler: validate + Calculate + order.ApplyPromotion + promo.IncrementUsage
```

---

## OrderPromotion (snapshot trong Order)

**File:** `Api.Core/Aggregates/OrderAggregate/OrderPromotion.cs`
**Table:** `business."OrderPromotions"`

| Property | Mô tả |
|----------|-------|
| `OrderId` | FK → Orders |
| `PromotionId` | FK → Promotions |
| `PromoCode` | Snapshot: `promo.Name` (auto) hoặc `promo.Code` (manual) |
| `DiscountAmount` | Số tiền giảm thực tế |
| `StackPolicy` | Snapshot StackPolicy tại thời điểm áp dụng |

> `PromoCode` max 200 ký tự (đã fix từ 50 — migration `20260313163534_IncreasePromoCodeMaxLength`).

---

## Gotchas

1. **Domain events không dispatch** — `PromotionUsedEvent`, `OrderPromotionAppliedEvent` được đăng ký nhưng `MediatorDomainEventDispatcher` đang bị comment out.
2. **Auto-apply là best-effort** — lỗi trong `autoApplyPromotions` bị Frontend bỏ qua silently (chỉ log).
3. **StartDate timezone** — CreatePromotionHandler treat-as-UTC, không convert. Frontend phải gửi đúng UTC ISO string.
4. **Xóa promo từ order**: `order.RemovePromotion(promotionId)` → reset item-level discounts, xóa free gift items liên quan.

---

## File liên quan

| File | Mô tả |
|------|-------|
| `Api.Core/.../PromotionAggregate/Promotion.cs` | Domain entity |
| `Api.UseCases/Promotions/Apply/PromotionCalculator.cs` | Pure calculator |
| `Api.UseCases/Orders/AutoApplyPromotions/AutoApplyPromotionsHandler.cs` | Handler auto-apply |
| `Api.UseCases/Promotions/Apply/ApplyPromotionHandler.cs` | Handler manual code |
| `Api.UseCases/Promotions/Validate/ValidatePromotionHandler.cs` | Handler validate |
| `admin/src/views/orders/Create.vue` | UI tạo order + promo |
| `admin/src/services/order.service.js` | `autoApplyPromotions`, `applyPromotionAdmin` |
| `admin/src/services/promotion.service.js` | `validatePromotion`, `getPromotions` |
