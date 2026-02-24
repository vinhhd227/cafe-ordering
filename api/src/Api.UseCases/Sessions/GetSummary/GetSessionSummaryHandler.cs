using Api.Core.Aggregates.GuestSessionAggregate;
using Api.Core.Aggregates.GuestSessionAggregate.Specifications;
using Api.Core.Aggregates.OrderAggregate;
using Api.Core.Aggregates.OrderAggregate.Specifications;
using Api.UseCases.Sessions.DTOs;

namespace Api.UseCases.Sessions.GetSummary;

public class GetSessionSummaryHandler(
  IReadRepositoryBase<GuestSession> sessionRepository,
  IReadRepositoryBase<Order> orderRepository)
  : IQueryHandler<GetSessionSummaryQuery, Result<SessionSummaryDto>>
{
  public async ValueTask<Result<SessionSummaryDto>> Handle(
    GetSessionSummaryQuery request, CancellationToken ct)
  {
    var sessionSpec = new SessionByIdSpec(request.SessionId);
    var session = await sessionRepository.FirstOrDefaultAsync(sessionSpec, ct);

    if (session is null)
      return Result.NotFound($"Session {request.SessionId} not found.");

    var ordersSpec = new OrdersBySessionIdSpec(request.SessionId);
    var orders = await orderRepository.ListAsync(ordersSpec, ct);

    var orderLines = orders
      .Select(o => new OrderLineDto(
        o.Id,
        o.OrderNumber,
        o.TotalAmount,
        o.Status.Name,
        o.Items
          .Select(i => new OrderItemLineDto(
            i.ProductId,
            i.ProductName,
            i.UnitPrice,
            i.Quantity,
            i.TotalPrice))
          .ToList()))
      .ToList();

    var grandTotal = orderLines.Sum(o => o.TotalAmount);

    return Result.Success(new SessionSummaryDto(
      session.Id,
      session.TableId,
      session.OpenedAt,
      session.Status,
      orderLines,
      grandTotal));
  }
}
