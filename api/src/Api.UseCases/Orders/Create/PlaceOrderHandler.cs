using Api.Core.Aggregates.GuestSessionAggregate;
using Api.Core.Aggregates.GuestSessionAggregate.Specifications;
using Api.Core.Aggregates.OrderAggregate;
using Api.Core.Aggregates.OrderAggregate.Specifications;
using Api.Core.Aggregates.ProductAggregate;
using Api.Core.Aggregates.ProductAggregate.Specifications;
using Api.Core.Aggregates.PromotionAggregate;
using Api.Core.Aggregates.PromotionAggregate.Specifications;
using Api.UseCases.Orders.DTOs;
using Api.UseCases.Promotions.Apply;
using Microsoft.Extensions.Configuration;

namespace Api.UseCases.Orders.Create;

public class PlaceOrderHandler(
  IRepositoryBase<Order> orderRepository,
  IReadRepositoryBase<GuestSession> sessionRepository,
  IRepositoryBase<Promotion> promotionRepository,
  IReadRepositoryBase<Product> productRepository,
  IConfiguration configuration)
  : ICommandHandler<PlaceOrderCommand, Result<PlaceOrderResponseDto>>
{
  public async ValueTask<Result<PlaceOrderResponseDto>> Handle(
    PlaceOrderCommand request, CancellationToken ct)
  {
    // 1. Parse order type
    if (!OrderType.TryFromName(request.OrderType, true, out var orderType))
      return Result.Invalid(new ValidationError("OrderType", $"Invalid order type: {request.OrderType}"));

    // 2. DineIn requires a valid active session; Takeaway/Delivery does not
    if (orderType == OrderType.DineIn)
    {
      if (request.SessionId is null)
        return Result.Invalid(new ValidationError("SessionId", "SessionId is required for DineIn orders."));

      var session = await sessionRepository.FirstOrDefaultAsync(new SessionByIdSpec(request.SessionId.Value), ct);
      if (session is null)
        return Result.NotFound($"Session {request.SessionId} not found.");
      if (session.Status == GuestSessionStatus.Closed)
        return Result.Conflict("Cannot place order on a closed session.");

      // Order cooldown -- bypass for admin/staff
      if (!request.BypassCooldown)
      {
        var cooldownSeconds = configuration.GetValue<int>("OrderCooldown:Seconds", 30);
        if (cooldownSeconds > 0)
        {
          var lastOrder = await orderRepository.FirstOrDefaultAsync(
            new LatestOrderBySessionIdSpec(request.SessionId.Value), ct);
          if (lastOrder is not null)
          {
            var elapsed = DateTime.UtcNow - lastOrder.OrderDate;
            if (elapsed.TotalSeconds < cooldownSeconds)
            {
              var remaining = (int)(cooldownSeconds - elapsed.TotalSeconds) + 1;
              return Result.Invalid(new ValidationError("SessionId",
                $"Vui long cho {remaining} giay truoc khi dat them."));
            }
          }
        }
      }
    }
    else if (orderType == OrderType.Delivery)
    {
      if (string.IsNullOrWhiteSpace(request.DeliveryAddress))
        return Result.Invalid(new ValidationError("DeliveryAddress",
          "Delivery address is required for Delivery orders."));
    }

    // 3. Validate items
    if (request.Items is null || request.Items.Count == 0)
      return Result.Invalid(new ValidationError("Items", "Order must contain at least one item."));

    // 4. Load products with option groups for validation and price snapshots
    var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
    var products = await productRepository.ListAsync(new ProductsByIdsWithVariantGroupsSpec(productIds), ct);
    var productMap = products.ToDictionary(p => p.Id);

    // 5. Create order entity
    var orderNumber = $"ORD-{DateTime.UtcNow:yyyyMMddHHmmss}";
    Order order;
    if (orderType == OrderType.DineIn)
    {
      order = Order.Create(request.SessionId!.Value, orderNumber, guestCount: request.GuestCount);
    }
    else
    {
      order = Order.CreateStandalone(orderType, orderNumber,
        request.CustomerName, request.CustomerPhone,
        request.DeliveryAddress, request.DeliveryNote,
        request.GuestCount);
    }

    await orderRepository.AddAsync(order, ct);

    // 6. Add items with option snapshots
    foreach (var item in request.Items)
    {
      if (!productMap.TryGetValue(item.ProductId, out var product))
        return Result.NotFound($"Product {item.ProductId} not found.");

      var pricing = ProductVariantPricingResolver.Resolve(product, item.SelectedVariantValueIds);
      if (pricing is null)
        return Result.Invalid(new ValidationError("SelectedVariantValueIds",
          $"Invalid option value IDs for product '{product.Name}'."));

      var orderItem = order.AddItem(item.ProductId, product.Name, pricing.UnitPrice, item.Quantity,
        pricing.Options, item.IsTakeaway, item.IsFreeGift, item.Note);

      // Add ProductOptionGroup value selections (topping-style)
      if (item.SelectedOptionValues is { Count: > 0 })
      {
        foreach (var sel in item.SelectedOptionValues)
        {
          var mapping = product.OptionGroupMappings
            .FirstOrDefault(m => m.Group is not null && m.Group.Values.Any(v => v.Id == sel.OptionValueId));
          if (mapping?.Group is null) continue;

          var optValue = mapping.Group.Values.First(v => v.Id == sel.OptionValueId);
          orderItem.AddSelectedOptionValue(
            optValue.Id, mapping.Group.Name, optValue.Name, optValue.Price,
            sel.Quantity > 0 ? sel.Quantity : 1);
        }
      }
    }

    // 7. Raise OrderCreatedEvent after items are added
    order.NotifyCreated();
    await orderRepository.UpdateAsync(order, ct);

    // 8. Apply promo if provided (best-effort)
    if (!string.IsNullOrWhiteSpace(request.PromoCode))
      await TryApplyPromoAsync(order, request.PromoCode, ct);

    return Result.Success(new PlaceOrderResponseDto(order.Id, order.OrderNumber, order.TotalAmount));
  }

  private async Task TryApplyPromoAsync(Order order, string code, CancellationToken ct)
  {
    try
    {
      var promo = await promotionRepository.FirstOrDefaultAsync(new PromotionByCodeSpec(code), ct);
      if (promo is null || !promo.IsValidAt(DateTime.UtcNow) || !promo.HasUsageLeft()) return;
      if (!promo.IsApplicableTo(order.TotalAmount)) return;

      Dictionary<int, int?>? productCategoryMap = null;
      var needsCategoryMap = (promo.Scope == PromotionScope.Category && promo.ApplicableCategoryIds.Any())
                          || (promo.GetFromCategoryIds is { Count: > 0 });
      if (needsCategoryMap)
      {
        var productIds = order.Items.Select(i => i.ProductId).ToList();
        var products   = await productRepository.ListAsync(new ProductsByIdsSpec(productIds), ct);
        productCategoryMap = products.ToDictionary(p => p.Id, p => p.CategoryId);
      }

      IReadOnlyList<Product>? externalFreeProducts = null;
      if (promo.DiscountType == DiscountType.BuyXGetY && promo.GetFromProductIds is { Count: > 0 })
        externalFreeProducts = await productRepository.ListAsync(new ProductsByIdsSpec(promo.GetFromProductIds), ct);

      var discountResult = PromotionCalculator.Calculate(promo, order.Items, productCategoryMap, externalFreeProducts);
      if (discountResult.TotalDiscount <= 0) return;

      if (discountResult.FreeGifts is { Count: > 0 })
      {
        order.RemoveFreeGiftItems();
        foreach (var gift in discountResult.FreeGifts)
          order.AddItem(gift.ProductId, gift.ProductName, gift.UnitPrice, gift.Quantity, isFreeGift: true);
      }

      order.ApplyPromotion(promo.Id, promo.Code, discountResult.TotalDiscount);
      promo.IncrementUsage(order.Id);

      await orderRepository.UpdateAsync(order, ct);
      await promotionRepository.UpdateAsync(promo, ct);
    }
    catch
    {
      // Promo error must not fail the order -- silently ignored
    }
  }
}
