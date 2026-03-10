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
    string? orderNumber = null,
    string? paymentMethod = null)
  {
    // Phải dùng local variable thay vì static field (PaymentStatus.Paid) trong lambda
    // EF Core không thể translate static member access trong expression tree khi dùng HasConversion
    var paid = PaymentStatus.Paid;

    Query
      .Where(o => o.PaymentStatus == paid && o.PaymentMethod == method)
      .Select(o => o.Items.Sum(i => (i.UnitPrice - i.Discount) * i.Quantity) + o.TipAmount);

    // Nếu user filter theo paymentMethod mà không khớp với method của spec → kết quả rỗng
    if (!string.IsNullOrWhiteSpace(paymentMethod))
    {
      var userMethod = PaymentMethod.FromName(paymentMethod, true);
      if (userMethod != method)
        Query.Where(_ => false);
    }

    if (!string.IsNullOrWhiteSpace(status))
    {
      var target = OrderStatus.FromName(status, true);
      Query.Where(o => o.Status == target);
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
      Query.Where(o => sessionIds.Contains(o.SessionId));
  }
}
