using Api.Core.Aggregates.OrderAggregate;
using Api.Core.Aggregates.OrderAggregate.Specifications;
using Api.Core.Aggregates.ProductAggregate;
using Api.Core.Aggregates.ProductAggregate.Specifications;
using Api.Core.Aggregates.PromotionAggregate;
using Api.Core.Aggregates.PromotionAggregate.Specifications;
using Api.UseCases.Orders.DTOs;
using Api.UseCases.Promotions.Apply;

namespace Api.UseCases.Orders.AutoApplyPromotions;

public class AutoApplyPromotionsHandler(
  IRepositoryBase<Order> orderRepo,
  IReadRepositoryBase<Promotion> promoRepo,
  IReadRepositoryBase<Product> productRepo)
  : ICommandHandler<AutoApplyPromotionsCommand, Result<OrderDto>>
{
  public async ValueTask<Result<OrderDto>> Handle(AutoApplyPromotionsCommand cmd, CancellationToken ct)
  {
    var order = await orderRepo.FirstOrDefaultAsync(new OrderByIdWithItemsAndPromotionsSpec(cmd.OrderId), ct);
    if (order is null)
      return Result.NotFound($"Order {cmd.OrderId} not found.");

    var noCodePromos = await promoRepo.ListAsync(new ActiveNoCodePromotionsSpec(), ct);
    if (noCodePromos.Count == 0)
      return Result.Success(MapToDto(order));

    var now = DateTime.UtcNow;

    var productIds = order.Items.Select(i => i.ProductId).ToList();
    var products = await productRepo.ListAsync(new ProductsByIdsSpec(productIds), ct);
    var productCategoryMap = products.ToDictionary(p => p.Id, p => p.CategoryId);

    bool anyApplied = false;

    foreach (var promo in noCodePromos)
    {
      if (!promo.IsValidAt(now) || !promo.HasUsageLeft()) continue;
      if (!promo.IsApplicableTo(order.TotalAmount)) continue;

      var discountResult = PromotionCalculator.Calculate(promo, order.Items, productCategoryMap);
      if (discountResult.TotalDiscount <= 0) continue;

      try
      {
        order.ApplyPromotion(promo.Id, promo.Name, discountResult.TotalDiscount, promo.StackPolicy);
        promo.IncrementUsage(order.Id);
        anyApplied = true;
      }
      catch
      {
        // Stack policy conflict or already applied — skip silently
      }
    }

    if (anyApplied)
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
      i.IsTakeaway)).ToList(),
    order.Promotions.Select(p => new AppliedPromotionDto(p.PromotionId, p.PromoCode, p.DiscountAmount)).ToList()
  );
}
