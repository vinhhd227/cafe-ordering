namespace Api.Core.Aggregates.OrderAggregate.Specifications;

/// <summary>
///   Đếm tổng số Orders khớp với bộ lọc (dùng cho phân trang)
/// </summary>
public class OrdersCountSpec : Specification<Order>
{
  public OrdersCountSpec(string? status = null,
    DateTime? dateFrom = null, DateTime? dateTo = null)
  {
    if (!string.IsNullOrWhiteSpace(status))
      Query.Where(o => o.Status.Name == status);

    if (dateFrom.HasValue)
      Query.Where(o => o.OrderDate >= dateFrom.Value);

    if (dateTo.HasValue)
      Query.Where(o => o.OrderDate < dateTo.Value.Date.AddDays(1));
  }
}
