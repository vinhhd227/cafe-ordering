using Api.Core.Aggregates.OrderAggregate;

namespace Api.UseCases.Orders.Delete;

public class DeleteOrderHandler(IRepositoryBase<Order> repository)
  : ICommandHandler<DeleteOrderCommand, Result>
{
  public async ValueTask<Result> Handle(DeleteOrderCommand request, CancellationToken ct)
  {
    var order = await repository.GetByIdAsync(request.OrderId, ct);
    if (order is null)
      return Result.NotFound($"Order {request.OrderId} not found.");

    if (order.Status != OrderStatus.Cancelled)
      return Result.Invalid(new ValidationError("Status", "Chỉ có thể xoá đơn đã bị huỷ."));

    await repository.DeleteAsync(order, ct);
    return Result.Success();
  }
}
