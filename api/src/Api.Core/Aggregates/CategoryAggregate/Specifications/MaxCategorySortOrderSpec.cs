namespace Api.Core.Aggregates.CategoryAggregate.Specifications;

/// <summary>
///   Lấy category có SortOrder cao nhất (chưa bị xóa), dùng để tính SortOrder cho category mới.
/// </summary>
public class MaxCategorySortOrderSpec : Specification<Category>
{
  public MaxCategorySortOrderSpec()
  {
    Query
      .Where(c => !c.IsDeleted)
      .OrderByDescending(c => c.SortOrder)
      .Take(1);
  }
}
