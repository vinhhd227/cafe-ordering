namespace Api.Core.Aggregates.ExpenseAggregate.Specifications;

/// <summary>
///   Projection spec: lấy TotalAmount của Expense theo một category cụ thể
///   trong khoảng ngày mua. Dùng để tổng hợp chi phí theo loại (P&amp;L).
/// </summary>
public class ExpensesTotalByCategorySpec : Specification<Expense, decimal>
{
  public ExpensesTotalByCategorySpec(
    ExpenseCategory category,
    DateTime? dateFrom = null,
    DateTime? dateTo = null)
  {
    Query
      .Where(e => !e.IsDeleted && e.Category == category)
      .Select(e => e.TotalAmount);

    if (dateFrom.HasValue)
      Query.Where(e => e.PurchaseDate >= dateFrom.Value);

    if (dateTo.HasValue)
      Query.Where(e => e.PurchaseDate < dateTo.Value.AddDays(1));
  }
}
