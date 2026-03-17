using Api.Core.Aggregates.OrderAggregate;
using Api.Core.Aggregates.OrderAggregate.Specifications;
using Api.Core.Aggregates.ProductAggregate;
using Api.Core.Aggregates.ProductAggregate.Specifications;
using Api.UseCases.Orders.DTOs;

namespace Api.UseCases.Orders.Summary;

public class GetOrdersSummaryHandler(
  IReadRepositoryBase<Order> repository,
  IReadRepositoryBase<Product> productRepository)
  : IQueryHandler<GetOrdersSummaryQuery, Result<OrdersSummaryDto>>
{
  public async ValueTask<Result<OrdersSummaryDto>> Handle(
    GetOrdersSummaryQuery request, CancellationToken ct)
  {
    var dateFrom = request.DateFrom.HasValue
      ? request.DateFrom.Value.ToUniversalTime()
      : (DateTime?)null;
    var dateTo = request.DateTo.HasValue
      ? request.DateTo.Value.ToUniversalTime()
      : (DateTime?)null;

    // ── Aggregate totals ──────────────────────────────────────────────────
    var cashSpec         = new PaidOrdersTotalSpec(PaymentMethod.Cash,        dateFrom: dateFrom, dateTo: dateTo);
    var bankSpec         = new PaidOrdersTotalSpec(PaymentMethod.BankTransfer, dateFrom: dateFrom, dateTo: dateTo);
    var cashTipSpec      = new PaidOrdersTipSpec(PaymentMethod.Cash,        dateFrom, dateTo);
    var bankTipSpec      = new PaidOrdersTipSpec(PaymentMethod.BankTransfer, dateFrom, dateTo);
    var totalSpec        = new OrdersCountSpec(null, null, dateFrom, dateTo);
    var completedSpec    = new OrdersCountSpec("COMPLETED",  null, dateFrom, dateTo);
    var cancelledSpec    = new OrdersCountSpec("CANCELLED",  null, dateFrom, dateTo);
    var pendingSpec      = new OrdersCountSpec("PENDING",    null, dateFrom, dateTo);
    var processingSpec   = new OrdersCountSpec("PROCESSING", null, dateFrom, dateTo);

    var cashAmounts      = await repository.ListAsync(cashSpec, ct);
    var bankAmounts      = await repository.ListAsync(bankSpec, ct);
    var cashTips         = await repository.ListAsync(cashTipSpec, ct);
    var bankTips         = await repository.ListAsync(bankTipSpec, ct);
    var totalOrders      = await repository.CountAsync(totalSpec, ct);
    var completedOrders  = await repository.CountAsync(completedSpec, ct);
    var cancelledOrders  = await repository.CountAsync(cancelledSpec, ct);
    var pendingOrders    = await repository.CountAsync(pendingSpec, ct);
    var processingOrders = await repository.CountAsync(processingSpec, ct);

    var cashRevenue = cashAmounts.Sum();
    var bankRevenue = bankAmounts.Sum();
    var totalTips   = cashTips.Sum() + bankTips.Sum();

    // ── Daily breakdown ───────────────────────────────────────────────────
    var cashDailySpec       = new PaidOrdersDailySpec(PaymentMethod.Cash,        dateFrom, dateTo);
    var bankDailySpec       = new PaidOrdersDailySpec(PaymentMethod.BankTransfer, dateFrom, dateTo);
    var allDatesSpec        = new OrdersDailyDateSpec(null,        dateFrom, dateTo);
    var completedDatesSpec  = new OrdersDailyDateSpec("COMPLETED", dateFrom, dateTo);
    var completedItemsSpec  = new CompletedOrdersDailyItemsSpec(dateFrom, dateTo);

    var cashDailyAmounts   = await repository.ListAsync(cashDailySpec, ct);
    var bankDailyAmounts   = await repository.ListAsync(bankDailySpec, ct);
    var allOrderDates      = await repository.ListAsync(allDatesSpec, ct);
    var completedDates     = await repository.ListAsync(completedDatesSpec, ct);
    var completedDailyItems = await repository.ListAsync(completedItemsSpec, ct);

    // Tổng hợp tất cả dates có xuất hiện trong bất kỳ query nào
    var allDates = allOrderDates
      .Select(d => d.ToUniversalTime().Date)
      .Concat(cashDailyAmounts.Select(d => d.Date.ToUniversalTime().Date))
      .Concat(bankDailyAmounts.Select(d => d.Date.ToUniversalTime().Date))
      .Concat(completedDailyItems.Select(d => d.Date.ToUniversalTime().Date))
      .Distinct()
      .OrderBy(d => d)
      .ToList();

    var completedDateList = completedDates.Select(d => d.ToUniversalTime().Date).ToList();

    var dailyRevenue = allDates.Select(date =>
    {
      var cash         = cashDailyAmounts.Where(d => d.Date.ToUniversalTime().Date == date).Sum(d => d.Amount);
      var bank         = bankDailyAmounts.Where(d => d.Date.ToUniversalTime().Date == date).Sum(d => d.Amount);
      var orderCount   = allOrderDates.Count(d => d.ToUniversalTime().Date == date);
      var completedCnt = completedDateList.Count(d => d == date);
      var itemsSold    = completedDailyItems.Where(d => d.Date.ToUniversalTime().Date == date).Sum(d => d.Count);
      return new DailyRevenueDto(DateOnly.FromDateTime(date), cash + bank, cash, bank, orderCount, completedCnt, itemsSold);
    }).ToList();

    var totalItemsSold = completedDailyItems.Sum(d => d.Count);

    // ── Top products & Top categories ─────────────────────────────────────
    var completedWithItemsSpec = new CompletedOrdersInRangeWithItemsSpec(dateFrom, dateTo);
    var completedOrdersWithItems = await repository.ListAsync(completedWithItemsSpec, ct);

    var totalGuestCount = completedOrdersWithItems.Sum(o => o.GuestCount ?? 0);

    var itemsFlat = completedOrdersWithItems
      .SelectMany(o => o.Items.Where(i => !i.IsFreeGift))
      .ToList();

    // Top 5 products by quantity sold
    var productGroups = itemsFlat
      .GroupBy(i => new { i.ProductId, i.ProductName })
      .Select(g => new { g.Key.ProductName, TotalQty = g.Sum(i => i.Quantity) })
      .OrderByDescending(x => x.TotalQty)
      .Take(5)
      .ToList();

    var maxQty = productGroups.FirstOrDefault()?.TotalQty ?? 1;
    var topProducts = productGroups
      .Select(p => new TopProductDto(p.ProductName, p.TotalQty,
        (int)Math.Round(p.TotalQty * 100.0 / maxQty)))
      .ToList();

    // Top categories by revenue share
    List<TopCategoryDto> topCategories = [];
    if (itemsFlat.Count > 0)
    {
      var productIds = itemsFlat.Select(i => i.ProductId).Distinct().ToList();
      var products   = await productRepository.ListAsync(new ProductsByIdsWithCategorySpec(productIds), ct);
      var catMap     = products.ToDictionary(p => p.Id, p => p.Category?.Name ?? "Khác");

      var totalRev = itemsFlat.Sum(i => i.TotalPrice);
      topCategories = itemsFlat
        .GroupBy(i => catMap.GetValueOrDefault(i.ProductId, "Khác"))
        .Select(g => new { Name = g.Key, Revenue = g.Sum(i => i.TotalPrice) })
        .OrderByDescending(x => x.Revenue)
        .Select(x => new TopCategoryDto(x.Name,
          totalRev > 0 ? (int)Math.Round((double)(x.Revenue / totalRev * 100)) : 0))
        .ToList();
    }

    return Result.Success(new OrdersSummaryDto(
      cashRevenue,
      bankRevenue,
      cashRevenue + bankRevenue,
      totalTips,
      totalOrders,
      completedOrders,
      cancelledOrders,
      pendingOrders,
      processingOrders,
      totalItemsSold,
      totalGuestCount,
      dailyRevenue,
      topProducts,
      topCategories
    ));
  }
}
