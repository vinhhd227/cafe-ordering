using Api.Core.Aggregates.OrderAggregate;
using Api.Core.Aggregates.OrderAggregate.Specifications;

namespace Api.UseCases.Orders.CancelByGuest;

public class CancelOrderByGuestHandler(IRepositoryBase<Order> repository)
  : ICommandHandler<CancelOrderByGuestCommand, Result>
{
  private const int CancelWindowSeconds = 120;

  public async ValueTask<Result> Handle(CancelOrderByGuestCommand request, CancellationToken ct)
  {
    var spec  = new OrderByIdWithItemsSpec(request.OrderId);
    var order = await repository.FirstOrDefaultAsync(spec, ct);

    if (order is null)
      return Result.NotFound($"Order {request.OrderId} not found.");

    if (order.SessionId != request.SessionId)
      return Result.Forbidden();

    if (order.Status != OrderStatus.Pending)
      return Result.Invalid(new ValidationError("Status", "Chỉ có thể huỷ đơn đang chờ xử lý."));

    var elapsed = DateTime.UtcNow - order.OrderDate;
    if (elapsed.TotalSeconds > CancelWindowSeconds)
      return Result.Invalid(new ValidationError("OrderDate",
        $"Chỉ có thể huỷ đơn trong vòng {CancelWindowSeconds / 60} phút sau khi đặt."));

    order.Cancel();
    await repository.UpdateAsync(order, ct);

    return Result.Success();
  }
}
