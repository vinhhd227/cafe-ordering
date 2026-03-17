namespace Api.Core.Aggregates.OrderAggregate.Specifications;

/// <summary>
///   Loads Completed orders with their Items within an optional date range.
///   Used for top-products / top-categories aggregation in reports.
/// </summary>
public class CompletedOrdersInRangeWithItemsSpec : Specification<Order>
{
  public CompletedOrdersInRangeWithItemsSpec(DateTime? dateFrom = null, DateTime? dateTo = null)
  {
    var completed = OrderStatus.Completed;

    Query
      .Where(o => o.Status == completed)
      .Include(o => o.Items);

    if (dateFrom.HasValue)
      Query.Where(o => o.OrderDate >= dateFrom.Value);

    if (dateTo.HasValue)
      Query.Where(o => o.OrderDate < dateTo.Value.AddDays(1));
  }
}
