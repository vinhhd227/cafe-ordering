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
    var order = await repository.FirstOrDefaultAsync(
      new OrderByIdWithItemsAndPromotionsSpec(request.OrderId), ct);

    if (order is null)
      return Result.NotFound($"Order {request.OrderId} not found.");

    string? tableCode = null;
    GuestSession? session = null;
    if (order.SessionId.HasValue)
    {
      session = await sessionRepository.FirstOrDefaultAsync(new SessionByIdSpec(order.SessionId.Value), ct);
      if (session?.TableId.HasValue == true)
      {
        var table = await tableRepository.FirstOrDefaultAsync(new TableByIdSpec(session.TableId.Value), ct);
        tableCode = table?.Code;
      }
    }

    var isManual = session?.Source == GuestSessionSource.Manual;

    return Result.Success(order.ToOrderDto(tableCode, isManual));
  }
}
