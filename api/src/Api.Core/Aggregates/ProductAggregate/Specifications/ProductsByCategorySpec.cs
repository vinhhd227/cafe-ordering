namespace Api.Core.Aggregates.ProductAggregate.Specifications;

/// <summary>
///   Lấy Products theo CategoryId (chưa bị xóa)
/// </summary>
public class ProductsByCategorySpec : Specification<Product>
{
  public ProductsByCategorySpec(int categoryId)
  {
    Query
      .Where(p => p.CategoryId == categoryId && !p.IsDeleted)
      .OrderBy(p => p.Name);
  }
}
