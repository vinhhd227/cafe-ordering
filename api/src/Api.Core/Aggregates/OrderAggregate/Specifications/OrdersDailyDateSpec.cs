namespace Api.Core.Aggregates.OrderAggregate.Specifications;

/// <summary>
/// Projection spec: trả về OrderDate của từng order (tùy chọn filter theo status).
/// Dùng để group-by-day trong memory tại handler để đếm số đơn mỗi ngày.
/// </summary>
public class OrdersDailyDateSpec : Specification<Order, DateTime>
{
  public OrdersDailyDateSpec(
    string? status = null,
    DateTime? dateFrom = null,
    DateTime? dateTo = null)
  {
    if (!string.IsNullOrWhiteSpace(status))
    {
      var target = OrderStatus.FromName(status, true);
      Query.Where(o => o.Status == target);
    }

    Query.Select(o => o.OrderDate);

    if (dateFrom.HasValue)
      Query.Where(o => o.OrderDate >= dateFrom.Value);

    if (dateTo.HasValue)
      Query.Where(o => o.OrderDate < dateTo.Value.AddDays(1));
  }
}
