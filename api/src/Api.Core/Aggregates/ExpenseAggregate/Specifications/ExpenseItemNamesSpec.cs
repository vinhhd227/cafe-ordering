namespace Api.Core.Aggregates.ExpenseAggregate.Specifications;

/// <summary>
///   Projection spec: trả về danh sách tên mặt hàng (Name) chưa bị xóa,
///   lọc theo chuỗi tìm kiếm (contains, case-insensitive via EF Core).
///   Dùng để gợi ý autocomplete khi nhập tên chi phí.
/// </summary>
public class ExpenseItemNamesSpec : Specification<Expense, string>
{
  public ExpenseItemNamesSpec(string? query)
  {
    Query
      .Where(e => !e.IsDeleted)
      .Select(e => e.Name);

    if (!string.IsNullOrWhiteSpace(query))
      Query.Where(e => e.Name.ToLower().Contains(query.ToLower()));
  }
}
