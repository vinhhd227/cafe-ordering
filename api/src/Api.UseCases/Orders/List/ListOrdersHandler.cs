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

    var countSpec = new OrdersCountSpec(request.Status, request.PaymentStatus,
      request.DateFrom, request.DateTo, filteredSessionIds,
      request.MinAmount, request.MaxAmount, request.OrderNumber);

    var spec = new OrdersListSpec(request.Status, request.PaymentStatus,
      request.DateFrom, request.DateTo, request.Page, request.PageSize,
      filteredSessionIds, request.MinAmount, request.MaxAmount, request.OrderNumber);

    var cashSpec = new PaidOrdersTotalSpec(PaymentMethod.Cash,
      request.Status, request.DateFrom, request.DateTo,
      filteredSessionIds, request.MinAmount, request.MaxAmount, request.OrderNumber);

    var bankSpec = new PaidOrdersTotalSpec(PaymentMethod.BankTransfer,
      request.Status, request.DateFrom, request.DateTo,
      filteredSessionIds, request.MinAmount, request.MaxAmount, request.OrderNumber);

    // Chạy song song: count + list + 2 aggregate totals
    var countTask     = repository.CountAsync(countSpec, ct);
    var listTask      = repository.ListAsync(spec, ct);
    var cashTask      = repository.ListAsync(cashSpec, ct);
    var bankTask      = repository.ListAsync(bankSpec, ct);
    await Task.WhenAll(countTask, listTask, cashTask, bankTask);

    var totalCount  = countTask.Result;
    var orders      = listTask.Result;
    var cashAmounts = cashTask.Result;
    var bankAmounts = bankTask.Result;

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
          i.TotalPrice
        )).ToList()
      );
    }).ToList();

    var cashTotal         = cashAmounts.Sum();
    var bankTransferTotal = bankAmounts.Sum();

    return Result.Success(new PagedOrdersDto(dtos, totalCount, request.Page, request.PageSize,
      cashTotal, bankTransferTotal));
  }
}
