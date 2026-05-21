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

/// <summary>
///   Táº¡o order má»›i trong má»™t session Ä‘ang active
/// </summary>
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
    // 1. Kiá»ƒm tra session tá»“n táº¡i vÃ  cÃ²n active
    var sessionSpec = new SessionByIdSpec(request.SessionId);
    var session = await sessionRepository.FirstOrDefaultAsync(sessionSpec, ct);

    if (session is null)
      return Result.NotFound($"Session {request.SessionId} not found.");

    if (session.Status == GuestSessionStatus.Closed)
      return Result.Conflict("Cannot place order on a closed session.");

    // 2. Order cooldown â€” bá» qua cho admin/staff (BypassCooldown = true)
    if (!request.BypassCooldown)
    {
      var cooldownSeconds = configuration.GetValue<int>("OrderCooldown:Seconds", 30);
      if (cooldownSeconds > 0)
      {
        var lastOrder = await orderRepository.FirstOrDefaultAsync(
          new LatestOrderBySessionIdSpec(request.SessionId), ct);

        if (lastOrder is not null)
        {
          var elapsed = DateTime.UtcNow - lastOrder.OrderDate;
          if (elapsed.TotalSeconds < cooldownSeconds)
          {
            var remaining = (int)(cooldownSeconds - elapsed.TotalSeconds) + 1;
            return Result.Invalid(new ValidationError("SessionId",
              $"Vui lÃ²ng chá» {remaining} giÃ¢y trÆ°á»›c khi Ä‘áº·t thÃªm."));
          }
        }
      }
    }

    // 3. Validate items
    if (request.Items is null || request.Items.Count == 0)
      return Result.Invalid(new ValidationError("Items", "Order must contain at least one item."));

    // 4. Load products kÃ¨m option groups/values Ä‘á»ƒ validate vÃ  build snapshots
    var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
    var products = await productRepository.ListAsync(new ProductsByIdsWithVariantGroupsSpec(productIds), ct);
    var productMap = products.ToDictionary(p => p.Id);

    // 5. Táº¡o order â€” chÆ°a cÃ³ items, save Ä‘á»ƒ EF sinh Id
    var orderNumber = $"ORD-{DateTime.UtcNow:yyyyMMddHHmmss}";
    var order = Order.Create(request.SessionId, orderNumber, guestCount: request.GuestCount);

    await orderRepository.AddAsync(order, ct);

    // 6. ThÃªm items vá»›i option snapshots
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
            optValue.Id,
            mapping.Group.Name,
            optValue.Name,
            optValue.Price,
            sel.Quantity > 0 ? sel.Quantity : 1);
        }
      }
    }

    // 7. ÄÄƒng kÃ½ OrderCreatedEvent sau khi items Ä‘Ã£ Ä‘Æ°á»£c thÃªm
    order.NotifyCreated();
    await orderRepository.UpdateAsync(order, ct);

    // 8. Ãp dá»¥ng promo náº¿u cÃ³ (best-effort)
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
      // Promo lá»—i khÃ´ng Ä‘Æ°á»£c fail order â€” bá» qua
    }
  }
}
