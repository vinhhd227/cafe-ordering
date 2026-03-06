namespace Api.Core.Aggregates.OrderAggregate.Specifications;

/// <summary>
///   Đếm tổng số Orders khớp với bộ lọc (dùng cho phân trang)
/// </summary>
public class OrdersCountSpec : Specification<Order>
{
  public OrdersCountSpec(
    string? status = null,
    string? paymentStatus = null,
    DateTime? dateFrom = null,
    DateTime? dateTo = null,
    IReadOnlyList<Guid>? sessionIds = null,
    decimal? minAmount = null,
    decimal? maxAmount = null,
    string? orderNumber = null)
  {
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

    if (!string.IsNullOrWhiteSpace(orderNumber))
      Query.Where(o => o.OrderNumber.Contains(orderNumber));

    if (minAmount.HasValue)
      Query.Where(o => o.TotalAmount >= minAmount.Value);

    if (maxAmount.HasValue)
      Query.Where(o => o.TotalAmount <= maxAmount.Value);

    if (dateFrom.HasValue)
      Query.Where(o => o.OrderDate >= dateFrom.Value);

    if (dateTo.HasValue)
      Query.Where(o => o.OrderDate < dateTo.Value.Date.AddDays(1));

    if (sessionIds is not null)
      Query.Where(o => sessionIds.Contains(o.SessionId));
  }
}
