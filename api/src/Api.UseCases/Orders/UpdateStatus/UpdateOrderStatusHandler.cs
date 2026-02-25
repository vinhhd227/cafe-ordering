using Api.Core.Aggregates.OrderAggregate;
using Api.Core.Aggregates.OrderAggregate.Specifications;

namespace Api.UseCases.Orders.UpdateStatus;

public class UpdateOrderStatusHandler(IRepositoryBase<Order> repository)
  : ICommandHandler<UpdateOrderStatusCommand, Result>
{
  public async ValueTask<Result> Handle(UpdateOrderStatusCommand request, CancellationToken ct)
  {
    var spec = new OrderByIdWithItemsSpec(request.OrderId);
    var order = await repository.FirstOrDefaultAsync(spec, ct);

    if (order is null)
      return Result.NotFound($"Order {request.OrderId} not found.");

    try
    {
      if (request.Status == OrderStatus.Processing.Name)
        order.Process();
      else if (request.Status == OrderStatus.Completed.Name)
        order.Complete();
      else if (request.Status == OrderStatus.Cancelled.Name)
        order.Cancel();
      else
        return Result.Invalid(new ValidationError("Status", $"Unknown status: {request.Status}"));
    }
    catch (InvalidOperationException ex)
    {
      return Result.Conflict(ex.Message);
    }

    await repository.UpdateAsync(order, ct);
    return Result.Success();
  }
}
