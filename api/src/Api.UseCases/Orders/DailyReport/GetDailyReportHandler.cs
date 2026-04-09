using Api.Core.Aggregates.OrderAggregate;
using Api.Core.Aggregates.OrderAggregate.Specifications;
using Api.Core.Aggregates.ProductAggregate;
using Api.Core.Aggregates.ProductAggregate.Specifications;
using Api.UseCases.Orders.DTOs;

namespace Api.UseCases.Orders.DailyReport;

public class GetDailyReportHandler(
  IReadRepositoryBase<Order> orderRepository,
  IReadRepositoryBase<Product> productRepository)
  : IQueryHandler<GetDailyReportQuery, Result<DailyReportDto>>
{
  public async ValueTask<Result<DailyReportDto>> Handle(
    GetDailyReportQuery request, CancellationToken ct)
  {
    // ── Date boundaries (UTC midnight) ────────────────────────────────────
    var dateFrom      = request.Date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
    var dateTo        = dateFrom;
    var yesterdayFrom = dateFrom.AddDays(-1);
    var yesterdayTo   = yesterdayFrom;
    var d7From        = dateFrom.AddDays(-7);
    var d7To          = dateFrom.AddDays(-1);
    var d30From       = dateFrom.AddDays(-30);
    var d30To         = dateFrom.AddDays(-1);

    // ── Today: revenue by payment method ─────────────────────────────────
    var cashSpec    = new PaidOrdersTotalSpec(PaymentMethod.Cash,        dateFrom: dateFrom, dateTo: dateTo);
    var bankSpec    = new PaidOrdersTotalSpec(PaymentMethod.BankTransfer, dateFrom: dateFrom, dateTo: dateTo);
    var cashAmounts = await orderRepository.ListAsync(cashSpec, ct);
    var bankAmounts = await orderRepository.ListAsync(bankSpec, ct);
    var cashRev     = cashAmounts.Sum();
    var bankRev     = bankAmounts.Sum();
    var totalRev    = cashRev + bankRev;

    // ── Today: order counts ───────────────────────────────────────────────
    var totalOrders    = await orderRepository.CountAsync(new OrdersCountSpec(null,          null, dateFrom, dateTo), ct);
    var completedCount = await orderRepository.CountAsync(new OrdersCountSpec("COMPLETED",   null, dateFrom, dateTo), ct);
    var cancelledCount = await orderRepository.CountAsync(new OrdersCountSpec("CANCELLED",   null, dateFrom, dateTo), ct);
    var pendingCount   = await orderRepository.CountAsync(new OrdersCountSpec("PENDING",     null, dateFrom, dateTo), ct);

    // ── Yesterday revenue ─────────────────────────────────────────────────
    var yCash = (await orderRepository.ListAsync(new PaidOrdersTotalSpec(PaymentMethod.Cash,        dateFrom: yesterdayFrom, dateTo: yesterdayTo), ct)).Sum();
    var yBank = (await orderRepository.ListAsync(new PaidOrdersTotalSpec(PaymentMethod.BankTransfer, dateFrom: yesterdayFrom, dateTo: yesterdayTo), ct)).Sum();
    var yesterdayRev = yCash + yBank;

    // ── 7-day totals (also used for hourlyAvg7Days) ───────────────────────
    var d7CashHourly = await orderRepository.ListAsync(new PaidOrdersDailySpec(PaymentMethod.Cash,        d7From, d7To), ct);
    var d7BankHourly = await orderRepository.ListAsync(new PaidOrdersDailySpec(PaymentMethod.BankTransfer, d7From, d7To), ct);
    var avg7Days     = (d7CashHourly.Sum(x => x.Amount) + d7BankHourly.Sum(x => x.Amount)) / 7m;

    // ── 30-day totals ─────────────────────────────────────────────────────
    var d30Cash = (await orderRepository.ListAsync(new PaidOrdersTotalSpec(PaymentMethod.Cash,        dateFrom: d30From, dateTo: d30To), ct)).Sum();
    var d30Bank = (await orderRepository.ListAsync(new PaidOrdersTotalSpec(PaymentMethod.BankTransfer, dateFrom: d30From, dateTo: d30To), ct)).Sum();
    var avg30Days = (d30Cash + d30Bank) / 30m;

    // ── Month averages (30 days before today) ─────────────────────────────
    var d30CompletedCount  = await orderRepository.CountAsync(new OrdersCountSpec("COMPLETED", null, d30From, d30To), ct);
    var d30ItemsData       = await orderRepository.ListAsync(new CompletedOrdersDailyItemsSpec(d30From, d30To), ct);
    var d30TotalItems      = d30ItemsData.Sum(x => x.Count);
    var d30TotalRev        = d30Cash + d30Bank;
    var revPerOrderMonthAvg   = d30CompletedCount > 0 ? d30TotalRev / d30CompletedCount : 0m;
    var itemsPerOrderMonthAvg = d30CompletedCount > 0 ? (double)d30TotalItems / d30CompletedCount : 0d;

    // ── Today: completed orders with items (top products, categories) ─────
    var completedWithItems = await orderRepository.ListAsync(
      new CompletedOrdersInRangeWithItemsSpec(dateFrom, dateTo), ct);

    var todayItemsFlat = completedWithItems
      .SelectMany(o => o.Items.Where(i => !i.IsFreeGift))
      .ToList();

    var totalItemsQty  = todayItemsFlat.Sum(i => i.Quantity);
    var revenuePerOrder   = completedCount > 0 ? totalRev / completedCount : 0m;
    var itemsPerOrder     = completedCount > 0 ? (double)totalItemsQty / completedCount : 0d;

    // ── Top 5 products by quantity ────────────────────────────────────────
    var topProducts = todayItemsFlat
      .GroupBy(i => new { i.ProductId, i.ProductName })
      .Select(g => new DailyReportTopProductDto(
        g.Key.ProductId.ToString(),
        g.Key.ProductName,
        g.Sum(i => i.Quantity),
        g.Sum(i => (i.UnitPrice - i.Discount) * i.Quantity)
      ))
      .OrderByDescending(p => p.Quantity)
      .Take(5)
      .ToList();

    // ── Top categories ────────────────────────────────────────────────────
    List<DailyReportTopCategoryDto> topCategories = [];
    if (todayItemsFlat.Count > 0)
    {
      var productIds    = todayItemsFlat.Select(i => i.ProductId).Distinct().ToList();
      var products      = await productRepository.ListAsync(new ProductsByIdsWithCategorySpec(productIds), ct);
      var catMap        = products.ToDictionary(p => p.Id, p => p.Category?.Name ?? "Khác");
      var totalTodayRev = todayItemsFlat.Sum(i => i.TotalPrice);

      topCategories = todayItemsFlat
        .GroupBy(i => catMap.GetValueOrDefault(i.ProductId, "Khác"))
        .Select(g =>
        {
          var catRev = g.Sum(i => i.TotalPrice);
          var pct    = totalTodayRev > 0 ? (int)Math.Round((double)(catRev / totalTodayRev * 100)) : 0;
          return new DailyReportTopCategoryDto(g.Key, pct, catRev);
        })
        .OrderByDescending(c => c.Revenue)
        .ToList();
    }

    // ── Today: hourly revenue ─────────────────────────────────────────────
    var cashHourly = await orderRepository.ListAsync(new PaidOrdersDailySpec(PaymentMethod.Cash,        dateFrom, dateTo), ct);
    var bankHourly = await orderRepository.ListAsync(new PaidOrdersDailySpec(PaymentMethod.BankTransfer, dateFrom, dateTo), ct);

    var hourlyRevenue = Enumerable.Range(8, 15)  // 08:00–22:00
      .Select(h =>
      {
        var cash = cashHourly.Where(d => (d.Date.ToUniversalTime().Hour + 7) % 24 == h).Sum(d => d.Amount);
        var bank = bankHourly.Where(d => (d.Date.ToUniversalTime().Hour + 7) % 24 == h).Sum(d => d.Amount);
        return new DailyReportHourlyDto($"{h:D2}:00", cash + bank);
      })
      .ToList();

    // ── Peak hour ─────────────────────────────────────────────────────────
    var peakEntry   = hourlyRevenue.MaxBy(h => h.Revenue);
    var peakRevenue = peakEntry?.Revenue ?? 0m;
    DailyReportPeakHourDto? peakHour = null;
    if (peakRevenue > 0 && peakEntry is not null)
    {
      var pct = totalRev > 0 ? (int)Math.Round((double)(peakRevenue / totalRev * 100)) : 0;
      peakHour = new DailyReportPeakHourDto(peakEntry.Hour, peakRevenue, pct);
    }

    // ── 7-day hourly average ──────────────────────────────────────────────
    var hourlyAvg7Days = Enumerable.Range(8, 15)
      .Select(h =>
      {
        var cash = d7CashHourly.Where(d => (d.Date.ToUniversalTime().Hour + 7) % 24 == h).Sum(d => d.Amount);
        var bank = d7BankHourly.Where(d => (d.Date.ToUniversalTime().Hour + 7) % 24 == h).Sum(d => d.Amount);
        return new DailyReportHourlyDto($"{h:D2}:00", (cash + bank) / 7m);
      })
      .ToList();

    return Result.Success(new DailyReportDto(
      request.Date,
      new DailyReportRevenueDto(totalRev, cashRev, bankRev, yesterdayRev, avg7Days, avg30Days),
      new DailyReportOrdersDto(totalOrders, completedCount, cancelledCount, pendingCount),
      new DailyReportAveragesDto(revenuePerOrder, itemsPerOrder, revPerOrderMonthAvg, itemsPerOrderMonthAvg),
      peakHour,
      topProducts,
      topCategories,
      hourlyRevenue,
      hourlyAvg7Days
    ));
  }
}
