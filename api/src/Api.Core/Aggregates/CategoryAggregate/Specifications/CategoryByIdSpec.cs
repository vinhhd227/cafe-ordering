namespace Api.Core.Aggregates.CategoryAggregate.Specifications;

/// <summary>
///   Lấy Category theo Id (chưa bị xóa)
/// </summary>
public class CategoryByIdSpec : Specification<Category>
{
  public CategoryByIdSpec(int categoryId)
  {
    Query.Where(c => c.Id == categoryId && !c.IsDeleted);
  }
}
