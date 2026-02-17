namespace Api.Core.Aggregates.CategoryAggregate.Specifications;

/// <summary>
///   Lấy tất cả Categories (chưa bị xóa)
/// </summary>
public class AllCategoriesSpec : Specification<Category>
{
  public AllCategoriesSpec()
  {
    Query
      .Where(c => !c.IsDeleted)
      .OrderBy(c => c.Name);
  }
}
