namespace Api.Core.Aggregates.ProductAggregate.Specifications;

/// <summary>
///   Đếm tổng Products (chưa bị xóa), hỗ trợ search theo tên.
///   Dùng kết hợp với ProductsPagedSpec để tính TotalCount.
/// </summary>
public class ProductsCountSpec : Specification<Product>
{
  public ProductsCountSpec(
    string? searchTerm = null,
    bool? isActive = null,
    int? categoryId = null,
    decimal? minPrice = null,
    decimal? maxPrice = null)
  {
    Query.Where(p => !p.IsDeleted);

    if (!string.IsNullOrWhiteSpace(searchTerm))
      Query.Where(p => p.Name.Contains(searchTerm));

    if (isActive.HasValue)
      Query.Where(p => p.IsActive == isActive.Value);

    if (categoryId.HasValue)
      Query.Where(p => p.CategoryId == categoryId.Value);

    if (minPrice.HasValue)
      Query.Where(p => p.Price >= minPrice.Value);

    if (maxPrice.HasValue)
      Query.Where(p => p.Price <= maxPrice.Value);
  }
}
