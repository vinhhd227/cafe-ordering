namespace Api.Core.Aggregates.CategoryAggregate.Specifications;

/// <summary>
///   Lấy danh sách Categories theo tập IDs (chưa bị xóa).
/// </summary>
public class CategoriesByIdsSpec : Specification<Category>
{
  public CategoriesByIdsSpec(IEnumerable<int> ids)
  {
    Query.Where(c => ids.Contains(c.Id) && !c.IsDeleted);
  }
}
