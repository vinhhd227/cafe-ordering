namespace Api.Core.Aggregates.OrderAggregate.Specifications;

/// <summary>
///   Lấy danh sách Orders, tùy chọn lọc theo Status, include Items
/// </summary>
public class OrdersListSpec : Specification<Order>
{
  public OrdersListSpec(string? status = null)
  {
    Query.Include(o => o.Items);
    Query.OrderByDescending(o => o.OrderDate);

    if (!string.IsNullOrWhiteSpace(status))
    {
      Query.Where(o => o.Status.Name == status);
    }
  }
}
