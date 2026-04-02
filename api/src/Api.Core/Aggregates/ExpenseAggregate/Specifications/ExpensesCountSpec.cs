using Api.Core.Aggregates.OrderAggregate;

namespace Api.Core.Aggregates.ExpenseAggregate.Specifications;

/// <summary>
///   Đếm Expense khớp với bộ lọc (dùng cho phân trang server-side)
/// </summary>
public class ExpensesCountSpec : Specification<Expense>
{
  public ExpensesCountSpec(
    string? category = null,
    string? paymentMethod = null,
    DateTime? dateFrom = null,
    DateTime? dateTo = null)
  {
    Query.Where(e => !e.IsDeleted);

    if (!string.IsNullOrWhiteSpace(category))
    {
      var target = ExpenseCategory.FromName(category, true);
      Query.Where(e => e.Category == target);
    }

    if (!string.IsNullOrWhiteSpace(paymentMethod))
    {
      var target = PaymentMethod.FromName(paymentMethod, true);
      Query.Where(e => e.PaymentMethod == target);
    }

    if (dateFrom.HasValue)
      Query.Where(e => e.PurchaseDate >= dateFrom.Value);

    if (dateTo.HasValue)
      Query.Where(e => e.PurchaseDate < dateTo.Value.AddDays(1));
  }
}
