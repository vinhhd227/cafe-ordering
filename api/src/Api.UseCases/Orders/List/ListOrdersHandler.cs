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
    // Pre-resolve TableCode → tableIds → sessionIds (nếu có filter tableCode)
    IReadOnlyList<Guid>? filteredSessionIds = null;
    if (!string.IsNullOrWhiteSpace(request.TableCode))
    {
      var matchedTables   = await tableRepository.ListAsync(new TablesByCodePatternSpec(request.TableCode), ct);
      var matchedTableIds = matchedTables.Select(t => t.Id).ToList();
      if (matchedTableIds.Count > 0)
      {
        var matchedSessions = await sessionRepository.ListAsync(new SessionsByTableIdsSpec(matchedTableIds), ct);
        filteredSessionIds = matchedSessions.Select(s => s.Id).ToList();
      }
      else
      {
        filteredSessionIds = []; // không tìm thấy bàn → trả về rỗng
      }
    }

    // Npgsql chỉ chấp nhận DateTimeKind.Utc cho timestamp with time zone
    var dateFrom = request.DateFrom.HasValue
      ? DateTime.SpecifyKind(request.DateFrom.Value, DateTimeKind.Utc)
      : (DateTime?)null;
    var dateTo = request.DateTo.HasValue
      ? DateTime.SpecifyKind(request.DateTo.Value, DateTimeKind.Utc)
      : (DateTime?)null;

    var countSpec = new OrdersCountSpec(request.Status, request.PaymentStatus,
      dateFrom, dateTo, filteredSessionIds,
      request.MinAmount, request.MaxAmount, request.OrderNumber);

    var spec = new OrdersListSpec(request.Status, request.PaymentStatus,
      dateFrom, dateTo, request.Page, request.PageSize,
      filteredSessionIds, request.MinAmount, request.MaxAmount, request.OrderNumber);

    var cashSpec = new PaidOrdersTotalSpec(PaymentMethod.Cash,
      request.Status, dateFrom, dateTo,
      filteredSessionIds, request.MinAmount, request.MaxAmount, request.OrderNumber);

    var bankSpec = new PaidOrdersTotalSpec(PaymentMethod.BankTransfer,
      request.Status, dateFrom, dateTo,
      filteredSessionIds, request.MinAmount, request.MaxAmount, request.OrderNumber);

    // Thực hiện tuần tự — EF Core DbContext không hỗ trợ concurrent operations
    var totalCount  = await repository.CountAsync(countSpec, ct);
    var orders      = await repository.ListAsync(spec, ct);
    var cashAmounts = await repository.ListAsync(cashSpec, ct);
    var bankAmounts = await repository.ListAsync(bankSpec, ct);

    // Build sessionId → tableId map (chỉ load sessions cho trang hiện tại)
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
        o.Status.Name.ToUpperInvariant(),
        o.PaymentStatus.Name.ToUpperInvariant(),
        o.PaymentMethod.Name.ToUpperInvariant(),
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
          i.TotalPrice,
          i.Temperature?.Name.ToUpperInvariant(),
          i.IceLevel?.Name.ToUpperInvariant(),
          i.SugarLevel?.Name.ToUpperInvariant(),
          i.IsTakeaway
        )).ToList()
      );
    }).ToList();

    var cashTotal         = cashAmounts.Sum();
    var bankTransferTotal = bankAmounts.Sum();

    return Result.Success(new PagedOrdersDto(dtos, totalCount, request.Page, request.PageSize,
      cashTotal, bankTransferTotal));
  }
}
