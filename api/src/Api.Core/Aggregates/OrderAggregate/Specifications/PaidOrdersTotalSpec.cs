namespace Api.Core.Aggregates.OrderAggregate.Specifications;

/// <summary>
///   Projection spec: lấy TotalAmount của các orders đã PAID theo phương thức thanh toán,
///   áp dụng cùng bộ filter như OrdersCountSpec (trừ paymentStatus — luôn là PAID).
/// </summary>
public class PaidOrdersTotalSpec : Specification<Order, decimal>
{
  public PaidOrdersTotalSpec(
    PaymentMethod method,
    string? status = null,
    DateTime? dateFrom = null,
    DateTime? dateTo = null,
    IReadOnlyList<Guid>? sessionIds = null,
    decimal? minAmount = null,
    decimal? maxAmount = null,
    string? orderNumber = null)
  {
    Query
      .Where(o => o.PaymentStatus == PaymentStatus.Paid && o.PaymentMethod == method)
      .Select(o => o.TotalAmount);

    if (!string.IsNullOrWhiteSpace(status))
    {
      var target = OrderStatus.FromName(status, true);
      Query.Where(o => o.Status == target);
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
