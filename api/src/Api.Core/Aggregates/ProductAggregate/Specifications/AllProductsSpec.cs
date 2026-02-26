namespace Api.Core.Aggregates.ProductAggregate.Specifications;

/// <summary>
///   Tất cả products chưa bị xóa (bao gồm cả inactive), sắp xếp theo Name.
///   Dùng cho admin menu view.
/// </summary>
public class AllProductsSpec : Specification<Product>
{
  public AllProductsSpec()
  {
    Query
      .Where(p => !p.IsDeleted)
      .OrderBy(p => p.Name);
  }
}
