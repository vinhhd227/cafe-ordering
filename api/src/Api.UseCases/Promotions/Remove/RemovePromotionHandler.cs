using Api.Core.Aggregates.OrderAggregate;
using Api.Core.Aggregates.OrderAggregate.Specifications;
using Api.Core.Aggregates.PromotionAggregate;
using Api.UseCases.Orders.DTOs;

namespace Api.UseCases.Promotions.Remove;

public class RemovePromotionHandler(
  IRepositoryBase<Order> orderRepo,
  IReadRepositoryBase<Promotion> promoRepo)
  : ICommandHandler<RemovePromotionCommand, Result<OrderDto>>
{
  public async ValueTask<Result<OrderDto>> Handle(RemovePromotionCommand cmd, CancellationToken ct)
  {
    var order = await orderRepo.FirstOrDefaultAsync(new OrderByIdWithItemsAndPromotionsSpec(cmd.OrderId), ct);
    if (order is null)
      return Result.NotFound($"Order {cmd.OrderId} not found.");

    if (cmd.SessionId.HasValue && order.SessionId != cmd.SessionId.Value)
      return Result.Forbidden();

    try
    {
      order.RemovePromotion(cmd.PromotionId);
    }
    catch (Exception ex)
    {
      return Result.Invalid(new ValidationError("Promotion", ex.Message));
    }

    await orderRepo.UpdateAsync(order, ct);

    return Result.Success(new OrderDto(
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
    ));
  }
}
