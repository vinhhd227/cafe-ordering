namespace Api.Core.Aggregates.CategoryAggregate.Specifications;

/// <summary>
///   Lấy tất cả Categories đang active (chưa bị xóa)
/// </summary>
public class ActiveCategoriesSpec : Specification<Category>
{
  public ActiveCategoriesSpec()
  {
    Query
      .Where(c => c.IsActive && !c.IsDeleted)
      .OrderBy(c => c.Name);
  }
}
