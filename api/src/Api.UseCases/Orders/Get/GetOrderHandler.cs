using Api.Core.Aggregates.GuestSessionAggregate;
using Api.Core.Aggregates.GuestSessionAggregate.Specifications;
using Api.Core.Aggregates.OrderAggregate;
using Api.Core.Aggregates.OrderAggregate.Specifications;
using Api.Core.Aggregates.TableAggregate;
using Api.Core.Aggregates.TableAggregate.Specifications;

using Api.UseCases.Orders.DTOs;

namespace Api.UseCases.Orders.Get;

public class GetOrderHandler(
  IReadRepositoryBase<Order> repository,
  IReadRepositoryBase<GuestSession> sessionRepository,
  IReadRepositoryBase<Table> tableRepository)
  : IQueryHandler<GetOrderQuery, Result<OrderDto>>
{
  public async ValueTask<Result<OrderDto>> Handle(GetOrderQuery request, CancellationToken ct)
  {
    var spec  = new OrderByIdWithItemsSpec(request.OrderId);
    var order = await repository.FirstOrDefaultAsync(spec, ct);

    if (order is null)
      return Result.NotFound($"Order {request.OrderId} not found.");

    // Look up table code: Order → Session → Table
    string? tableCode = null;
    var session = await sessionRepository.FirstOrDefaultAsync(new SessionByIdSpec(order.SessionId), ct);
    if (session?.TableId.HasValue == true)
    {
      var table = await tableRepository.FirstOrDefaultAsync(new TableByIdSpec(session.TableId.Value), ct);
      tableCode = table?.Code;
    }

    var dto = new OrderDto(
      order.Id,
      order.OrderNumber,
      order.Status.Name.ToUpperInvariant(),
      order.PaymentStatus.Name.ToUpperInvariant(),
      order.PaymentMethod.Name.ToUpperInvariant(),
      order.AmountReceived,
      order.TipAmount,
      order.TotalAmount,
      order.OrderDate,
      order.SessionId,
      tableCode,
      order.Items.Select(i => new OrderItemDto(
        i.ProductId,
        i.ProductName,
        i.UnitPrice,
        i.Quantity,
        i.TotalPrice
      )).ToList()
    );

    return Result.Success(dto);
  }
}
