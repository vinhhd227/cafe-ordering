using Api.Core.Aggregates.OrderAggregate;
using Api.Core.Aggregates.OrderAggregate.Specifications;
using Api.UseCases.Orders.DTOs;

namespace Api.UseCases.Promotions.Remove;

public class RemovePromotionHandler(
  IRepositoryBase<Order> orderRepo)
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

    return Result.Success(order.ToOrderDto(null, false));
  }
}
