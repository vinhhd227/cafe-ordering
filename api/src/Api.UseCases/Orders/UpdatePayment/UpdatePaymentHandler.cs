using Api.Core.Aggregates.OrderAggregate;
using Api.Core.Aggregates.OrderAggregate.Specifications;
using Api.Core.Domain.Enums;
using Api.UseCases.Sessions.AutoClose;
using IMediator = Mediator.IMediator;

namespace Api.UseCases.Orders.UpdatePayment;

public class UpdatePaymentHandler(IRepositoryBase<Order> repository, IMediator mediator)
  : ICommandHandler<UpdatePaymentCommand, Result>
{
  public async ValueTask<Result> Handle(UpdatePaymentCommand request, CancellationToken ct)
  {
    var spec = new OrderByIdWithItemsSpec(request.OrderId);
    var order = await repository.FirstOrDefaultAsync(spec, ct);

    if (order is null)
      return Result.NotFound($"Order {request.OrderId} not found.");

    if (!Enum.TryParse<PaymentStatus>(request.PaymentStatus, ignoreCase: true, out var paymentStatus))
      return Result.Invalid(new ValidationError("PaymentStatus", $"Unknown payment status: {request.PaymentStatus}"));

    if (!Enum.TryParse<PaymentMethod>(request.PaymentMethod, ignoreCase: true, out var paymentMethod))
      return Result.Invalid(new ValidationError("PaymentMethod", $"Unknown payment method: {request.PaymentMethod}"));

    try
    {
      order.UpdatePayment(paymentStatus, paymentMethod, request.AmountReceived, request.TipAmount);
    }
    catch (InvalidOperationException ex)
    {
      return Result.Conflict(ex.Message);
    }

    await repository.UpdateAsync(order, ct);
    await mediator.Send(new TryAutoCloseSessionCommand(order.SessionId), ct);
    return Result.Success();
  }
}
