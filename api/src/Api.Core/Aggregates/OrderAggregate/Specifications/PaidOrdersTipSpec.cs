namespace Api.Core.Aggregates.OrderAggregate.Specifications;

/// <summary>
///   Projection spec: lấy TipAmount của các orders PAID theo phương thức thanh toán.
///   Dùng để tách riêng tiền tip ra khỏi doanh thu trong P&amp;L summary.
/// </summary>
public class PaidOrdersTipSpec : Specification<Order, decimal>
{
  public PaidOrdersTipSpec(
    PaymentMethod method,
    DateTime? dateFrom = null,
    DateTime? dateTo = null,
    IReadOnlyList<Guid>? sessionIds = null)
  {
    var paid = PaymentStatus.Paid;

    Query
      .Where(o => o.PaymentStatus == paid && o.PaymentMethod == method)
      .Select(o => o.TipAmount);

    if (dateFrom.HasValue)
      Query.Where(o => o.OrderDate >= dateFrom.Value);

    if (dateTo.HasValue)
      Query.Where(o => o.OrderDate < dateTo.Value.Date.AddDays(1));

    if (sessionIds is not null)
    {
      var nullableIds = sessionIds.Select(id => (Guid?)id).ToList();
      Query.Where(o => nullableIds.Contains(o.SessionId));
    }
  }
}
