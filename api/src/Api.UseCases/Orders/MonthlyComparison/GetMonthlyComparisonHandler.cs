using Api.Core.Aggregates.OrderAggregate;
using Api.Core.Aggregates.OrderAggregate.Specifications;
using Api.UseCases.Orders.DTOs;

namespace Api.UseCases.Orders.MonthlyComparison;

public class GetMonthlyComparisonHandler(IReadRepositoryBase<Order> repository)
  : IQueryHandler<GetMonthlyComparisonQuery, Result<MonthlyComparisonDto>>
{
  public async ValueTask<Result<MonthlyComparisonDto>> Handle(
    GetMonthlyComparisonQuery request, CancellationToken ct)
  {
    // ── Date ranges ───────────────────────────────────────────────────────
    var currentStart = new DateTime(request.Year, request.Month, 1, 0, 0, 0, DateTimeKind.Utc);
    var currentEnd   = currentStart.AddMonths(1);
    var prevStart    = currentStart.AddMonths(-1);

    // ── Single DB fetch for both months ───────────────────────────────────
    var spec   = new PaidCompletedOrdersInRangeWithItemsSpec(prevStart, currentEnd);
    var orders = await repository.ListAsync(spec, ct);

    var currentOrders = orders.Where(o => o.OrderDate >= currentStart).ToList();
    var prevOrders    = orders.Where(o => o.OrderDate <  currentStart).ToList();

    // ── Month summaries ───────────────────────────────────────────────────
    var current  = BuildSummary(currentOrders);
    var previous = BuildSummary(prevOrders);

    // ── Top products by revenue (current month, excl. free gifts) ─────────
    var currentItems = currentOrders.SelectMany(o => o.Items.Where(i => !i.IsFreeGift)).ToList();
    var prevItems    = prevOrders.SelectMany(o => o.Items.Where(i => !i.IsFreeGift)).ToList();

    // Build ranked product list (by revenue desc) for current month
    var currentProductGroups = currentItems
      .GroupBy(i => i.ProductName)
      .Select(g => new { Name = g.Key, Qty = g.Sum(i => i.Quantity), Revenue = g.Sum(i => i.TotalPrice) })
      .OrderByDescending(x => x.Revenue)
      .Select((x, idx) => new { x.Name, x.Qty, x.Revenue, Rank = idx + 1 })
      .ToList();

    var prevProductRanks = prevItems
      .GroupBy(i => i.ProductName)
      .Select(g => new { Name = g.Key, Revenue = g.Sum(i => i.TotalPrice) })
      .OrderByDescending(x => x.Revenue)
      .Select((x, idx) => new { x.Name, Rank = idx + 1 })
      .ToDictionary(x => x.Name, x => x.Rank);

    var topProducts = currentProductGroups
      .Take(10)
      .Select(p =>
      {
        var prevRank = prevProductRanks.TryGetValue(p.Name, out var pr) ? (int?)pr : null;
        var change   = prevRank.HasValue ? prevRank.Value - p.Rank : 0;
        return new ProductComparisonDto(p.Name, p.Qty, p.Revenue, p.Rank, prevRank, change);
      })
      .ToList();

    // ── Worst sellers (bottom 5 by qty, current month) ────────────────────
    var worstSellers = currentProductGroups
      .OrderBy(x => x.Qty)
      .Take(5)
      .Select(p =>
      {
        var prevRank = prevProductRanks.TryGetValue(p.Name, out var pr) ? (int?)pr : null;
        var change   = prevRank.HasValue ? prevRank.Value - p.Rank : 0;
        return new ProductComparisonDto(p.Name, p.Qty, p.Revenue, p.Rank, prevRank, change);
      })
      .ToList();

    // ── Peak hours (current month, 0-23) ──────────────────────────────────
    var hourlyLookup = currentOrders
      .GroupBy(o => o.OrderDate.Hour)
      .ToDictionary(
        g => g.Key,
        g => new { Revenue = g.Sum(o => o.FinalAmount), Count = g.Count() });

    var peakHours = Enumerable.Range(0, 24)
      .Select(h =>
      {
        var d = hourlyLookup.TryGetValue(h, out var v) ? v : null;
        return new HourlyDataDto(h, d?.Revenue ?? 0, d?.Count ?? 0);
      })
      .ToList();

    // ── Weekday pattern (current month, ISO: 1=Mon … 7=Sun) ──────────────
    var weekdayLookup = currentOrders
      .GroupBy(o => IsoWeekday(o.OrderDate.DayOfWeek))
      .ToDictionary(
        g => g.Key,
        g => new { Revenue = g.Sum(o => o.FinalAmount), Count = g.Count() });

    var weekdayPattern = Enumerable.Range(1, 7)
      .Select(d =>
      {
        var v = weekdayLookup.TryGetValue(d, out var val) ? val : null;
        return new WeekdayDataDto(d, v?.Revenue ?? 0, v?.Count ?? 0);
      })
      .ToList();

    // ── Takeaway ratio ────────────────────────────────────────────────────
    var takeawayRatio = 0m;
    if (currentOrders.Count > 0)
    {
      var takeawayCount = currentOrders.Count(o => o.Items.Any(i => i.IsTakeaway));
      takeawayRatio = Math.Round((decimal)takeawayCount / currentOrders.Count * 100, 1);
    }

    return Result.Success(new MonthlyComparisonDto(
      request.Year,
      request.Month,
      current,
      previous,
      topProducts,
      worstSellers,
      peakHours,
      weekdayPattern,
      takeawayRatio));
  }

  // ── Helpers ───────────────────────────────────────────────────────────────

  private static MonthSummaryDto BuildSummary(List<Order> orders)
  {
    var totalRevenue = orders.Sum(o => o.FinalAmount);
    var cashRevenue  = orders
      .Where(o => o.PaymentMethod == PaymentMethod.Cash)
      .Sum(o => o.FinalAmount);
    var bankRevenue  = orders
      .Where(o => o.PaymentMethod == PaymentMethod.BankTransfer)
      .Sum(o => o.FinalAmount);
    var totalItems   = orders.SelectMany(o => o.Items.Where(i => !i.IsFreeGift)).Sum(i => i.Quantity);
    var totalGuests  = orders.Sum(o => o.GuestCount ?? 0);
    var avgOrder     = orders.Count > 0 ? Math.Round(totalRevenue / orders.Count, 0) : 0;

    return new MonthSummaryDto(
      totalRevenue, cashRevenue, bankRevenue,
      orders.Count, totalItems, totalGuests, avgOrder);
  }

  /// <summary>Converts .NET DayOfWeek (Sun=0) to ISO weekday (Mon=1, Sun=7).</summary>
  private static int IsoWeekday(DayOfWeek dow)
    => dow == DayOfWeek.Sunday ? 7 : (int)dow;
}
