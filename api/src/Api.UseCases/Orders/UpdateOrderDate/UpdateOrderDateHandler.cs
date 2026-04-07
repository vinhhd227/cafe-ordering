using Api.Core.Aggregates.OrderAggregate;

namespace Api.UseCases.Orders.UpdateOrderDate;

public class UpdateOrderDateHandler(IRepositoryBase<Order> repository)
  : ICommandHandler<UpdateOrderDateCommand, Result>
{
  public async ValueTask<Result> Handle(UpdateOrderDateCommand request, CancellationToken ct)
  {
    var order = await repository.GetByIdAsync(request.OrderId, ct);
    if (order is null)
      return Result.NotFound($"Order {request.OrderId} not found.");

    order.UpdateOrderDate(request.OrderDate);
    await repository.UpdateAsync(order, ct);
    return Result.Success();
  }
}
