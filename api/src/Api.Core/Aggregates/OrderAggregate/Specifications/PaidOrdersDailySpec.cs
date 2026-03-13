namespace Api.Core.Aggregates.OrderAggregate.Specifications;

/// <summary>
/// Projection record dùng cho daily revenue grouping — trả về date + amount mỗi order PAID.
/// </summary>
public record OrderDateAmount(DateTime Date, decimal Amount);

/// <summary>
/// Projection spec: trả về (OrderDate, Amount) của từng order đã PAID theo phương thức thanh toán.
/// Dùng để group-by-day trong memory tại handler.
/// </summary>
public class PaidOrdersDailySpec : Specification<Order, OrderDateAmount>
{
  public PaidOrdersDailySpec(
    PaymentMethod method,
    DateTime? dateFrom = null,
    DateTime? dateTo = null)
  {
    var paid = PaymentStatus.Paid;

    Query
      .Where(o => o.PaymentStatus == paid && o.PaymentMethod == method)
      .Select(o => new OrderDateAmount(
        o.OrderDate,
        o.Items.Sum(i => (i.UnitPrice - i.Discount) * i.Quantity) + o.TipAmount
      ));

    if (dateFrom.HasValue)
      Query.Where(o => o.OrderDate >= dateFrom.Value);

    if (dateTo.HasValue)
      Query.Where(o => o.OrderDate < dateTo.Value.AddDays(1));
  }
}
