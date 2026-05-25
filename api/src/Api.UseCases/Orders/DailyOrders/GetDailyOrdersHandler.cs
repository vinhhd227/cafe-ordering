using Api.Core.Aggregates.GuestSessionAggregate;
using Api.Core.Aggregates.GuestSessionAggregate.Specifications;
using Api.Core.Aggregates.OrderAggregate;
using Api.Core.Aggregates.OrderAggregate.Specifications;
using Api.Core.Aggregates.TableAggregate;
using Api.Core.Aggregates.TableAggregate.Specifications;
using Api.UseCases.Orders.DTOs;

namespace Api.UseCases.Orders.DailyOrders;

public class GetDailyOrdersHandler(
  IReadRepositoryBase<Order> orderRepository,
  IReadRepositoryBase<GuestSession> sessionRepository,
  IReadRepositoryBase<Table> tableRepository)
  : IQueryHandler<GetDailyOrdersQuery, Result<DailyOrdersResponseDto>>
{
  public async ValueTask<Result<DailyOrdersResponseDto>> Handle(
    GetDailyOrdersQuery request, CancellationToken ct)
  {
    var dateFrom = request.Date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
    var dateTo   = dateFrom;

    // ── Load all orders for the day with their items ──────────────────────
    var orders = await orderRepository.ListAsync(
      new OrdersInDateRangeWithItemsSpec(dateFrom, dateTo), ct);

    if (orders.Count == 0)
      return Result.Success(new DailyOrdersResponseDto([]));

    // ── Resolve table numbers via Session → Table ─────────────────────────
    var sessionIds = orders.Where(o => o.SessionId.HasValue).Select(o => o.SessionId!.Value).Distinct().ToList();
    var sessions   = await sessionRepository.ListAsync(new SessionsByIdsSpec(sessionIds), ct);
    var sessionMap = sessions.ToDictionary(s => s.Id, s => s.TableId);

    var tableIds = sessions.Select(s => s.TableId).OfType<int>().Distinct().ToList();
    Dictionary<int, int> tableNumberMap = [];
    if (tableIds.Count > 0)
    {
      var tables = await tableRepository.ListAsync(new TablesByIdsSpec(tableIds), ct);
      tableNumberMap = tables.ToDictionary(t => t.Id, t => t.Id);
    }

    // ── Map to DTOs ───────────────────────────────────────────────────────
    var dtos = orders.Select(o =>
    {
      int? tableNumber = null;
      if (o.SessionId.HasValue
          && sessionMap.TryGetValue(o.SessionId.Value, out var tid) && tid.HasValue
          && tableNumberMap.TryGetValue(tid.Value, out var tNum))
        tableNumber = tNum;

      var items = o.Items
        .Select(i => new DailyOrderItemSnapshotDto(i.ProductName, i.Quantity))
        .ToList();

      var totalAmount = o.Items.Sum(i => (i.UnitPrice - i.Discount) * i.Quantity)
                        - o.Promotions.Sum(p => p.DiscountAmount);

      var paymentMethod = o.PaymentMethod.Name switch
      {
        "BANK_TRANSFER" => "transfer",
        "CASH"          => "cash",
        _               => o.PaymentMethod.Name.ToLowerInvariant(),
      };

      return new DailyOrderDto(
        o.Id,
        o.OrderDate,
        tableNumber,
        items,
        totalAmount,
        paymentMethod,
        o.PaymentStatus.Name.ToLowerInvariant(),
        o.Status.Name.ToLowerInvariant()
      );
    }).ToList();

    return Result.Success(new DailyOrdersResponseDto(dtos));
  }
}
