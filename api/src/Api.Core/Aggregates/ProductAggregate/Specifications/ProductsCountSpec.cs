namespace Api.Core.Aggregates.ProductAggregate.Specifications;

/// <summary>
///   Đếm tổng Products (chưa bị xóa), hỗ trợ search theo tên.
///   Dùng kết hợp với ProductsPagedSpec để tính TotalCount.
/// </summary>
public class ProductsCountSpec : Specification<Product>
{
  public ProductsCountSpec(string? searchTerm = null)
  {
    Query.Where(p => !p.IsDeleted);

    if (!string.IsNullOrWhiteSpace(searchTerm))
    {
      Query.Where(p => p.Name.Contains(searchTerm));
    }
  }
}
