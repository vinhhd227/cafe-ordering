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
  IReadRepositoryBase<Promotion> promoRepo,
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

    // 5. Calculate discount
    Dictionary<int, int>? productCategoryMap = null;

    var needsCategoryMap = (promo.Scope == PromotionScope.Category && promo.ApplicableCategoryIds.Any())
                        || (promo.GetFromCategoryIds is { Count: > 0 });

    if (needsCategoryMap)
    {
      var productIds = order.Items.Select(i => i.ProductId).ToList();
      var products   = await productRepo.ListAsync(new ProductsByIdsSpec(productIds), ct);
      productCategoryMap = products.ToDictionary(p => p.Id, p => p.CategoryId);
    }

    var discountResult = PromotionCalculator.Calculate(promo, order.Items, productCategoryMap);

    if (discountResult.TotalDiscount <= 0)
      return Result.Invalid(new ValidationError("Promotion",
        "This promotion does not apply to any items in the order."));

    // 6. Apply to domain
    try
    {
      order.ApplyPromotion(promo.Id, promo.Code, discountResult.TotalDiscount);
    }
    catch (Exception ex)
    {
      return Result.Invalid(new ValidationError("Promotion", ex.Message));
    }

    // 7. Increment usage
    promo.IncrementUsage(order.Id);

    await orderRepo.UpdateAsync(order, ct);

    return Result.Success(MapToDto(order));
  }

  private static OrderDto MapToDto(Order order) => new(
    order.Id,
    order.OrderNumber,
    order.Status.Name.ToUpperInvariant(),
    order.PaymentStatus.Name.ToUpperInvariant(),
    order.PaymentMethod.Name.ToUpperInvariant(),
    order.AmountReceived,
    order.TipAmount,
    order.TotalAmount,
    order.TotalDiscount,
    order.FinalAmount,
    order.OrderDate,
    order.SessionId,
    null,
    order.Items.Select(i => new OrderItemDto(
      i.ProductId, i.ProductName, i.UnitPrice, i.Quantity, i.Discount, i.TotalPrice,
      i.Temperature?.Name.ToUpperInvariant(),
      i.IceLevel?.Name.ToUpperInvariant(),
      i.SugarLevel?.Name.ToUpperInvariant(),
      i.IsTakeaway, i.IsFreeGift)).ToList(),
    order.Promotions.Select(p => new AppliedPromotionDto(p.PromotionId, p.PromoCode, p.DiscountAmount)).ToList()
  );
}
