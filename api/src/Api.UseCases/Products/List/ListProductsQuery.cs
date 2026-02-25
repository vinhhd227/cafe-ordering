using Api.UseCases.Products.DTOs;

namespace Api.UseCases.Products.List;

/// <summary>
///   Query lấy danh sách Products với phân trang
/// </summary>
public record ListProductsQuery(
  int Page = 1,
  int PageSize = 10,
  string? SearchTerm = null,
  bool? IsActive = null,
  int? CategoryId = null,
  decimal? MinPrice = null,
  decimal? MaxPrice = null
) : IQuery<Result<PagedResult<List<ProductSummaryDto>>>>;
