using Api.Core.Aggregates.ProductAggregate;
using Api.Core.Aggregates.ProductAggregate.Specifications;
using Api.UseCases.Products.DTOs;

namespace Api.UseCases.Products.List;

/// <summary>
///   Handler lấy danh sách Products với phân trang
/// </summary>
public class ListProductsHandler(IReadRepositoryBase<Product> repository)
  : IQueryHandler<ListProductsQuery, Result<PagedResult<List<ProductSummaryDto>>>>
{
  public async ValueTask<Result<PagedResult<List<ProductSummaryDto>>>> Handle(
    ListProductsQuery request,
    CancellationToken ct)
  {
    var spec = new ProductsPagedSpec(request.Page, request.PageSize, request.SearchTerm);
    var products = await repository.ListAsync(spec, ct);

    var countSpec = new ProductsCountSpec(request.SearchTerm);
    var totalCount = await repository.CountAsync(countSpec, ct);

    var dtos = products
      .Select(p => new ProductSummaryDto(
        p.Id,
        p.Name,
        p.Price,
        p.IsActive,
        p.ImageUrl,
        p.Category?.Name))
      .ToList();
    
    var totalPages = (long)Math.Ceiling(totalCount / (double)request.PageSize);
    
    var pagedInfo = new PagedInfo(
      pageNumber: request.Page,
      pageSize: request.PageSize,
      totalPages: totalPages,
      totalRecords: totalCount);
    
    var pagedResult = new PagedResult<List<ProductSummaryDto>>(
      pagedInfo: pagedInfo,
      value: dtos);
    
    return Result.Success(pagedResult);
  }
}
