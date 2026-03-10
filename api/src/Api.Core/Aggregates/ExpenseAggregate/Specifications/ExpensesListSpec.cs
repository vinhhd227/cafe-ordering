namespace Api.Core.Aggregates.ExpenseAggregate.Specifications;

/// <summary>
///   Lấy danh sách Expense có phân trang, lọc theo category và khoảng ngày mua
/// </summary>
public class ExpensesListSpec : Specification<Expense>
{
  public ExpensesListSpec(
    string? category = null,
    DateTime? dateFrom = null,
    DateTime? dateTo = null,
    int page = 1,
    int pageSize = 20)
  {
    Query
      .Where(e => !e.IsDeleted)
      .OrderByDescending(e => e.PurchaseDate);

    if (!string.IsNullOrWhiteSpace(category))
    {
      var target = ExpenseCategory.FromName(category, true);
      Query.Where(e => e.Category == target);
    }

    if (dateFrom.HasValue)
      Query.Where(e => e.PurchaseDate >= dateFrom.Value);

    if (dateTo.HasValue)
      Query.Where(e => e.PurchaseDate < dateTo.Value.AddDays(1));

    Query.Skip((page - 1) * pageSize).Take(pageSize);
  }
}
