using Api.Core.Aggregates.OrderAggregate;

namespace Api.Core.Aggregates.ExpenseAggregate.Specifications;

/// <summary>
///   Projection spec: lấy TotalAmount của Expense theo một category cụ thể
///   trong khoảng ngày mua, tuỳ chọn lọc theo payment method.
///   Dùng để tổng hợp chi phí theo loại (P&amp;L).
/// </summary>
public class ExpensesTotalByCategorySpec : Specification<Expense, int>
{
  public ExpensesTotalByCategorySpec(
    ExpenseCategory category,
    DateTime? dateFrom = null,
    DateTime? dateTo = null,
    PaymentMethod? paymentMethod = null)
  {
    Query
      .Where(e => !e.IsDeleted && e.Category == category)
      .Select(e => e.TotalAmount);

    if (paymentMethod is not null)
      Query.Where(e => e.PaymentMethod == paymentMethod);

    if (dateFrom.HasValue)
      Query.Where(e => e.PurchaseDate >= dateFrom.Value);

    if (dateTo.HasValue)
      Query.Where(e => e.PurchaseDate < dateTo.Value.AddDays(1));
  }
}
