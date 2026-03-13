namespace Api.Core.Aggregates.OrderAggregate.Specifications;

/// <summary>
/// Projection record dùng cho daily items grouping — trả về date + số lượng đồ uống mỗi order COMPLETED.
/// </summary>
public record OrderDateItems(DateTime Date, int Count);

/// <summary>
/// Projection spec: trả về (OrderDate, Items.Sum(Quantity)) của từng order COMPLETED.
/// Dùng để group-by-day trong memory tại handler để đếm số ly bán mỗi ngày.
/// </summary>
public class CompletedOrdersDailyItemsSpec : Specification<Order, OrderDateItems>
{
  public CompletedOrdersDailyItemsSpec(DateTime? dateFrom = null, DateTime? dateTo = null)
  {
    var completed = OrderStatus.Completed;

    Query
      .Where(o => o.Status == completed)
      .Select(o => new OrderDateItems(o.OrderDate, o.Items.Sum(i => i.Quantity)));

    if (dateFrom.HasValue)
      Query.Where(o => o.OrderDate >= dateFrom.Value);

    if (dateTo.HasValue)
      Query.Where(o => o.OrderDate < dateTo.Value.AddDays(1));
  }
}
