namespace Api.Core.Aggregates.OrderAggregate.Specifications;

/// <summary>
///   Projection spec: lấy TipAmount của các orders đã PAID,
///   áp dụng cùng bộ filter như PaidOrdersTotalSpec (không cần filter theo paymentMethod vì tip là tổng).
/// </summary>
public class PaidTipTotalSpec : Specification<Order, decimal>
{
  public PaidTipTotalSpec(
    string? status = null,
    DateTime? dateFrom = null,
    DateTime? dateTo = null,
    IReadOnlyList<Guid>? sessionIds = null,
    decimal? minAmount = null,
    decimal? maxAmount = null,
    string? orderNumber = null,
    string? paymentMethod = null)
  {
    var paid = PaymentStatus.Paid;

    Query
      .Where(o => o.PaymentStatus == paid)
      .Select(o => o.TipAmount);

    if (!string.IsNullOrWhiteSpace(paymentMethod))
    {
      var userMethod = PaymentMethod.FromName(paymentMethod, true);
      Query.Where(o => o.PaymentMethod == userMethod);
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
    {
      var nullableIds = sessionIds.Select(id => (Guid?)id).ToList();
      Query.Where(o => nullableIds.Contains(o.SessionId));
    }
  }
}
