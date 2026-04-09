namespace Api.Core.Aggregates.OrderAggregate.Specifications;

/// <summary>
///   Loads all Orders with their Items within an optional date range (any status).
///   Used for daily orders list report.
/// </summary>
public class OrdersInDateRangeWithItemsSpec : Specification<Order>
{
  public OrdersInDateRangeWithItemsSpec(DateTime? dateFrom = null, DateTime? dateTo = null)
  {
    Query
      .Include(o => o.Items)
      .Include(o => o.Promotions)
      .OrderBy(o => o.OrderDate);

    if (dateFrom.HasValue)
      Query.Where(o => o.OrderDate >= dateFrom.Value);

    if (dateTo.HasValue)
      Query.Where(o => o.OrderDate < dateTo.Value.AddDays(1));
  }
}
