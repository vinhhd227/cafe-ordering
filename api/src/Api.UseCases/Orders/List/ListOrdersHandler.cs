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

    // ToUniversalTime() xử lý đúng khi server chạy ở timezone Vietnam:
    // ASP.NET model binding parse "2026-03-09T17:00:00Z" → Local (March 10 00:00 VN),
    // ToUniversalTime() convert lại về March 9 17:00 UTC đúng.
    var dateFrom = request.DateFrom.HasValue
      ? request.DateFrom.Value.ToUniversalTime()
      : (DateTime?)null;
    var dateTo = request.DateTo.HasValue
      ? request.DateTo.Value.ToUniversalTime()
      : (DateTime?)null;

    var countSpec = new OrdersCountSpec(request.Status, request.PaymentStatus,
      dateFrom, dateTo, filteredSessionIds,
      request.MinAmount, request.MaxAmount, request.OrderNumber, request.PaymentMethod);

    var spec = new OrdersListSpec(request.Status, request.PaymentStatus,
      dateFrom, dateTo, request.Page, request.PageSize,
      filteredSessionIds, request.MinAmount, request.MaxAmount, request.OrderNumber, request.PaymentMethod);

    var cashSpec = new PaidOrdersTotalSpec(PaymentMethod.Cash,
      request.Status, dateFrom, dateTo,
      filteredSessionIds, request.MinAmount, request.MaxAmount, request.OrderNumber, request.PaymentMethod);

    var bankSpec = new PaidOrdersTotalSpec(PaymentMethod.BankTransfer,
      request.Status, dateFrom, dateTo,
      filteredSessionIds, request.MinAmount, request.MaxAmount, request.OrderNumber, request.PaymentMethod);

    var tipSpec = new PaidTipTotalSpec(
      request.Status, dateFrom, dateTo,
      filteredSessionIds, request.MinAmount, request.MaxAmount, request.OrderNumber, request.PaymentMethod);

    var pendingSpec    = new OrdersCountSpec("PENDING",    null, dateFrom, dateTo, filteredSessionIds, request.MinAmount, request.MaxAmount, request.OrderNumber, request.PaymentMethod);
    var processingSpec = new OrdersCountSpec("PROCESSING", null, dateFrom, dateTo, filteredSessionIds, request.MinAmount, request.MaxAmount, request.OrderNumber, request.PaymentMethod);
    var completedSpec  = new OrdersCountSpec("COMPLETED",  null, dateFrom, dateTo, filteredSessionIds, request.MinAmount, request.MaxAmount, request.OrderNumber, request.PaymentMethod);
    var cancelledSpec  = new OrdersCountSpec("CANCELLED",  null, dateFrom, dateTo, filteredSessionIds, request.MinAmount, request.MaxAmount, request.OrderNumber, request.PaymentMethod);

    // Thực hiện tuần tự — EF Core DbContext không hỗ trợ concurrent operations
    var totalCount      = await repository.CountAsync(countSpec, ct);
    var pendingCount    = await repository.CountAsync(pendingSpec, ct);
    var processingCount = await repository.CountAsync(processingSpec, ct);
    var completedCount  = await repository.CountAsync(completedSpec, ct);
    var cancelledCount  = await repository.CountAsync(cancelledSpec, ct);
    var orders         = await repository.ListAsync(spec, ct);
    var cashAmounts    = await repository.ListAsync(cashSpec, ct);
    var bankAmounts    = await repository.ListAsync(bankSpec, ct);
    var tipAmounts     = await repository.ListAsync(tipSpec, ct);

    // Build sessionId → tableId/source map (chỉ load sessions cho trang hiện tại)
    var sessionIds   = orders.Where(o => o.SessionId.HasValue).Select(o => o.SessionId!.Value).Distinct().ToList();
    var sessions     = await sessionRepository.ListAsync(new SessionsByIdsSpec(sessionIds), ct);
    var sessionMap   = sessions.ToDictionary(s => s.Id, s => s.TableId);
    var sessionSource = sessions.ToDictionary(s => s.Id, s => s.Source);

    // Build tableId → tableCode map — only load tables referenced by current page
    var tableIds = sessions.Select(s => s.TableId).OfType<int>().Distinct().ToList();
    var tables   = await tableRepository.ListAsync(new TablesByIdsSpec(tableIds), ct);
    var tableMap = tables.ToDictionary(t => t.Id, t => t.Code);

    var dtos = orders.Select(o =>
    {
      string? tableCode = null;
      if (o.SessionId.HasValue
          && sessionMap.TryGetValue(o.SessionId.Value, out var tableId) && tableId.HasValue)
        tableMap.TryGetValue(tableId.Value, out tableCode);

      var isManual = o.SessionId.HasValue
        && sessionSource.TryGetValue(o.SessionId.Value, out var src)
        && src == GuestSessionSource.Manual;

      return o.ToOrderDto(tableCode, isManual);
    }).ToList();

    var cashTotal         = cashAmounts.Sum();
    var bankTransferTotal = bankAmounts.Sum();
    var tipTotal          = tipAmounts.Sum();

    return Result.Success(new PagedOrdersDto(dtos, totalCount, request.Page, request.PageSize,
      cashTotal, bankTransferTotal, tipTotal, pendingCount, processingCount, completedCount, cancelledCount));
  }
}
