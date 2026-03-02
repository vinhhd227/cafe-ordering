namespace Api.Core.Aggregates.OrderAggregate.Specifications;

/// <summary>
///   Lấy danh sách Orders có phân trang, tùy chọn lọc theo Status và khoảng ngày, include Items
/// </summary>
public class OrdersListSpec : Specification<Order>
{
  public OrdersListSpec(string? status = null,
    DateTime? dateFrom = null, DateTime? dateTo = null,
    int page = 1, int pageSize = 20)
  {
    Query.Include(o => o.Items);
    Query.OrderByDescending(o => o.OrderDate);

    if (!string.IsNullOrWhiteSpace(status))
      Query.Where(o => o.Status.Name == status);

    if (dateFrom.HasValue)
      Query.Where(o => o.OrderDate >= dateFrom.Value);

    if (dateTo.HasValue)
      Query.Where(o => o.OrderDate < dateTo.Value.Date.AddDays(1));

    Query.Skip((page - 1) * pageSize).Take(pageSize);
  }
}
