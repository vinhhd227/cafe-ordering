using Api.Core.Aggregates.OrderAggregate;
using Api.Core.Aggregates.OrderAggregate.Specifications;
using Api.UseCases.Sessions.AutoClose;
using IMediator = Mediator.IMediator;

namespace Api.UseCases.Orders.UpdateStatus;

public class UpdateOrderStatusHandler(IRepositoryBase<Order> repository, IMediator mediator)
  : ICommandHandler<UpdateOrderStatusCommand, Result>
{
  public async ValueTask<Result> Handle(UpdateOrderStatusCommand request, CancellationToken ct)
  {
    var spec = new OrderByIdWithItemsSpec(request.OrderId);
    var order = await repository.FirstOrDefaultAsync(spec, ct);

    if (order is null)
      return Result.NotFound($"Order {request.OrderId} not found.");

    if (!OrderStatus.TryFromName(request.Status, true, out var target))
      return Result.Invalid(new ValidationError("Status", $"Unknown status: {request.Status}"));

    try
    {
      if (target == OrderStatus.Processing)
        order.Process();
      else if (target == OrderStatus.Completed)
        order.Complete();
      else if (target == OrderStatus.Cancelled)
        order.Cancel();
      else
        return Result.Invalid(new ValidationError("Status", $"Cannot transition to: {request.Status}"));
    }
    catch (InvalidOperationException ex)
    {
      return Result.Conflict(ex.Message);
    }

    await repository.UpdateAsync(order, ct);

    if (target == OrderStatus.Cancelled)
      await mediator.Send(new TryAutoCloseSessionCommand(order.SessionId), ct);

    return Result.Success();
  }
}
