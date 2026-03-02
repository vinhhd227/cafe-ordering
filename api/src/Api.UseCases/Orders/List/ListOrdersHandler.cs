using Api.Core.Aggregates.GuestSessionAggregate;
using Api.Core.Aggregates.GuestSessionAggregate.Specifications;
using Api.Core.Aggregates.OrderAggregate;
using Api.Core.Aggregates.OrderAggregate.Specifications;
using Api.Core.Aggregates.TableAggregate;
using Api.Core.Aggregates.TableAggregate.Specifications;
using Api.UseCases.Orders.DTOs;

namespace Api.UseCases.Orders.List;

public class ListOrdersHandler(
  IReadRepositoryBase<Order> repository,
  IReadRepositoryBase<GuestSession> sessionRepository,
  IReadRepositoryBase<Table> tableRepository)
  : IQueryHandler<ListOrdersQuery, Result<PagedOrdersDto>>
{
  public async ValueTask<Result<PagedOrdersDto>> Handle(
    ListOrdersQuery request, CancellationToken ct)
  {
    var countSpec = new OrdersCountSpec(request.Status, request.DateFrom, request.DateTo);
    var totalCount = await repository.CountAsync(countSpec, ct);

    var spec   = new OrdersListSpec(request.Status, request.DateFrom, request.DateTo, request.Page, request.PageSize);
    var orders = await repository.ListAsync(spec, ct);

    // Build sessionId → tableId map
    var sessionIds = orders.Select(o => o.SessionId).Distinct().ToList();
    var sessions   = await sessionRepository.ListAsync(new SessionsByIdsSpec(sessionIds), ct);
    var sessionMap = sessions.ToDictionary(s => s.Id, s => s.TableId);

    // Build tableId → tableCode map — only load tables referenced by current page
    var tableIds = sessions.Select(s => s.TableId).OfType<int>().Distinct().ToList();
    var tables   = await tableRepository.ListAsync(new TablesByIdsSpec(tableIds), ct);
    var tableMap = tables.ToDictionary(t => t.Id, t => t.Code);

    var dtos = orders.Select(o =>
    {
      string? tableCode = null;
      if (sessionMap.TryGetValue(o.SessionId, out var tableId) && tableId.HasValue)
        tableMap.TryGetValue(tableId.Value, out tableCode);

      return new OrderDto(
        o.Id,
        o.OrderNumber,
        o.Status.Name,
        o.PaymentStatus.ToString(),
        o.PaymentMethod.ToString(),
        o.AmountReceived,
        o.TipAmount,
        o.TotalAmount,
        o.OrderDate,
        o.SessionId,
        tableCode,
        o.Items.Select(i => new OrderItemDto(
          i.ProductId,
          i.ProductName,
          i.UnitPrice,
          i.Quantity,
          i.TotalPrice
        )).ToList()
      );
    }).ToList();

    return Result.Success(new PagedOrdersDto(dtos, totalCount, request.Page, request.PageSize));
  }
}
