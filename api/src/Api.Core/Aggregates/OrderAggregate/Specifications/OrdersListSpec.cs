namespace Api.Core.Aggregates.OrderAggregate.Specifications;

/// <summary>
///   Lấy danh sách Orders có phân trang, tùy chọn lọc theo nhiều tiêu chí, include Items
/// </summary>
public class OrdersListSpec : Specification<Order>
{
  public OrdersListSpec(
    string? status = null,
    string? paymentStatus = null,
    DateTime? dateFrom = null,
    DateTime? dateTo = null,
    int page = 1,
    int pageSize = 20,
    IReadOnlyList<Guid>? sessionIds = null,
    decimal? minAmount = null,
    decimal? maxAmount = null,
    string? orderNumber = null,
    string? paymentMethod = null)
  {
    Query.Include(o => o.Items);
    Query.Include(o => o.Promotions);
    Query.OrderByDescending(o => o.OrderDate);

    if (!string.IsNullOrWhiteSpace(status))
    {
      var target = OrderStatus.FromName(status, true);
      Query.Where(o => o.Status == target);
    }

    if (!string.IsNullOrWhiteSpace(paymentStatus))
    {
      var target = PaymentStatus.FromName(paymentStatus, true);
      Query.Where(o => o.PaymentStatus == target);
    }

    if (!string.IsNullOrWhiteSpace(paymentMethod))
    {
      var target = PaymentMethod.FromName(paymentMethod, true);
      Query.Where(o => o.PaymentMethod == target);
    }

    if (!string.IsNullOrWhiteSpace(orderNumber))
      Query.Where(o => o.OrderNumber.Contains(orderNumber));

    if (minAmount.HasValue)
      Query.Where(o => o.Items.Sum(i => (i.UnitPrice - i.Discount) * i.Quantity) >= minAmount.Value);

    if (maxAmount.HasValue)
      Query.Where(o => o.Items.Sum(i => (i.UnitPrice - i.Discount) * i.Quantity) <= maxAmount.Value);

    if (dateFrom.HasValue)
      Query.Where(o => o.OrderDate >= dateFrom.Value);

    if (dateTo.HasValue)
      Query.Where(o => o.OrderDate < dateTo.Value.AddDays(1));

    if (sessionIds is not null)
    {
      var nullableIds = sessionIds.Select(id => (Guid?)id).ToList();
      Query.Where(o => nullableIds.Contains(o.SessionId));
    }

    Query.Skip((page - 1) * pageSize).Take(pageSize);
  }
}
