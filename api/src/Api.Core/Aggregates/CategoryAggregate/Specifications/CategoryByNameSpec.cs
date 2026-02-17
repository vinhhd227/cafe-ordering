namespace Api.Core.Aggregates.CategoryAggregate.Specifications;

/// <summary>
///   Lấy Category theo Name (chưa bị xóa), dùng để check trùng tên
/// </summary>
public class CategoryByNameSpec : Specification<Category>
{
  public CategoryByNameSpec(string name)
  {
    Query.Where(c => c.Name == name && !c.IsDeleted);
  }
}
