namespace Api.Core.Aggregates.ExpenseAggregate.Specifications;

/// <summary>
///   Đếm Expense khớp với bộ lọc (dùng cho phân trang server-side)
/// </summary>
public class ExpensesCountSpec : Specification<Expense>
{
  public ExpensesCountSpec(
    string? category = null,
    DateTime? dateFrom = null,
    DateTime? dateTo = null)
  {
    Query.Where(e => !e.IsDeleted);

    if (!string.IsNullOrWhiteSpace(category))
    {
      var target = ExpenseCategory.FromName(category, true);
      Query.Where(e => e.Category == target);
    }

    if (dateFrom.HasValue)
      Query.Where(e => e.PurchaseDate >= dateFrom.Value);

    if (dateTo.HasValue)
      Query.Where(e => e.PurchaseDate < dateTo.Value.AddDays(1));
  }
}
