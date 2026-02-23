namespace Api.Core.Aggregates.ProductAggregate.Specifications;

/// <summary>
///   Lấy tất cả Products đang active (chưa bị xóa)
/// </summary>
public class ActiveProductsSpec : Specification<Product>
{
  public ActiveProductsSpec()
  {
    Query
      .Where(p => p.IsActive && !p.IsDeleted)
      .OrderBy(p => p.Name);
  }
}
