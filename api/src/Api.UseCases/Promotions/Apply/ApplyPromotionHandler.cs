using Api.Core.Aggregates.OrderAggregate;
using Api.Core.Aggregates.OrderAggregate.Specifications;
using Api.Core.Aggregates.ProductAggregate;
using Api.Core.Aggregates.ProductAggregate.Specifications;
using Api.Core.Aggregates.PromotionAggregate;
using Api.Core.Aggregates.PromotionAggregate.Specifications;
using Api.UseCases.Orders.DTOs;

namespace Api.UseCases.Promotions.Apply;

public class ApplyPromotionHandler(
  IRepositoryBase<Order> orderRepo,
  IRepositoryBase<Promotion> promoRepo,
  IReadRepositoryBase<Product> productRepo)
  : ICommandHandler<ApplyPromotionCommand, Result<OrderDto>>
{
  public async ValueTask<Result<OrderDto>> Handle(ApplyPromotionCommand cmd, CancellationToken ct)
  {
    // 1. Load Promotion
    Promotion? promo = null;

    if (!string.IsNullOrWhiteSpace(cmd.Code))
    {
      promo = await promoRepo.FirstOrDefaultAsync(new PromotionByCodeSpec(cmd.Code), ct);
      if (promo is null)
        return Result.NotFound($"Promotion with code '{cmd.Code}' not found.");
    }
    else if (cmd.PromotionId.HasValue)
    {
      promo = await promoRepo.FirstOrDefaultAsync(new PromotionByIdSpec(cmd.PromotionId.Value), ct);
      if (promo is null)
        return Result.NotFound($"Promotion {cmd.PromotionId} not found.");
    }
    else
    {
      return Result.Invalid(new ValidationError("Code", "Either Code or PromotionId must be provided."));
    }

    // 2. Validate promotion conditions
    var now = DateTime.UtcNow;

    if (!promo.IsValidAt(now))
      return Result.Invalid(new ValidationError("Promotion", "Promotion is not active or has expired."));

    if (!promo.HasUsageLeft())
      return Result.Invalid(new ValidationError("Promotion", "Promotion has reached its usage limit."));

    // 3. Load Order (with Items + Promotions)
    var order = await orderRepo.FirstOrDefaultAsync(new OrderByIdWithItemsAndPromotionsSpec(cmd.OrderId), ct);
    if (order is null)
      return Result.NotFound($"Order {cmd.OrderId} not found.");

    // Session ownership check
    if (cmd.SessionId.HasValue && order.SessionId != cmd.SessionId.Value)
      return Result.Forbidden();

    // 4. Check MinOrderAmount
    if (!promo.IsApplicableTo(order.TotalAmount))
      return Result.Invalid(new ValidationError("Promotion",
        $"Order total must be at least {promo.MinOrderAmount:N0} to apply this promotion."));

    // 5. Build productCategoryMap if needed
    Dictionary<int, int?>? productCategoryMap = null;

    var needsCategoryMap = (promo.Scope == PromotionScope.Category && promo.ApplicableCategoryIds.Any())
                        || (promo.GetFromCategoryIds is { Count: > 0 });

    if (needsCategoryMap)
    {
      var productIds = order.Items.Select(i => i.ProductId).ToList();
      var products   = await productRepo.ListAsync(new ProductsByIdsSpec(productIds), ct);
      productCategoryMap = products.ToDictionary(p => p.Id, p => p.CategoryId);
    }

    // 6. Load external free products for cross-product BUY_X_GET_Y
    IReadOnlyList<Product>? externalFreeProducts = null;

    if (promo.DiscountType == DiscountType.BuyXGetY && promo.GetFromProductIds is { Count: > 0 })
    {
      externalFreeProducts = await productRepo.ListAsync(
        new ProductsByIdsSpec(promo.GetFromProductIds), ct);
    }

    // 7. Calculate discount
    var discountResult = PromotionCalculator.Calculate(promo, order.Items, productCategoryMap, externalFreeProducts);

    if (discountResult.TotalDiscount <= 0)
      return Result.Invalid(new ValidationError("Promotion",
        "This promotion does not apply to any items in the order."));

    // 8. Add free gift items to order (cross-product BUY_X_GET_Y)
    if (discountResult.FreeGifts is { Count: > 0 })
    {
      order.RemoveFreeGiftItems();
      foreach (var gift in discountResult.FreeGifts)
        order.AddItem(gift.ProductId, gift.ProductName, gift.UnitPrice, gift.Quantity, isFreeGift: true);
    }

    // 9. Apply to domain
    try
    {
      order.ApplyPromotion(promo.Id, promo.Code, discountResult.TotalDiscount);
    }
    catch (Exception ex)
    {
      return Result.Invalid(new ValidationError("Promotion", ex.Message));
    }

    // 10. Increment usage
    promo.IncrementUsage(order.Id);

    await orderRepo.UpdateAsync(order, ct);
    await promoRepo.UpdateAsync(promo, ct);

    return Result.Success(MapToDto(order));
  }

  private static OrderDto MapToDto(Order order) => order.ToOrderDto(null, false);
}
