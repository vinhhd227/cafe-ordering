using Api.UseCases.Products.DTOs;
using Api.UseCases.Products.List;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Products;

/// <summary>
/// Query parameters for the paginated product list.
/// </summary>
public sealed class ListProductsRequest
{
  /// <summary>1-based page number. Defaults to <c>1</c>.</summary>
  [QueryParam] public int Page { get; set; } = 1;

  /// <summary>Number of items per page. Defaults to <c>10</c>; max recommended is <c>100</c>.</summary>
  [QueryParam] public int PageSize { get; set; } = 10;

  /// <summary>
  /// Optional free-text filter applied to the product name.
  /// Case-insensitive partial match. Omit or leave empty to return all products.
  /// </summary>
  [QueryParam] public string? SearchTerm { get; set; }

  /// <summary>
  /// Optional filter by active status.
  /// <c>true</c> = only active, <c>false</c> = only inactive, omit = all.
  /// </summary>
  [QueryParam] public bool? IsActive { get; set; }

  /// <summary>Optional filter by category ID.</summary>
  [QueryParam] public int? CategoryId { get; set; }

  /// <summary>Optional minimum price filter (inclusive).</summary>
  [QueryParam] public decimal? MinPrice { get; set; }

  /// <summary>Optional maximum price filter (inclusive).</summary>
  [QueryParam] public decimal? MaxPrice { get; set; }
}

public class List(IMediator mediator) : Ep.Req<ListProductsRequest>.Res<PagedResult<ProductSummaryDto>>
{
  public override void Configure()
  {
    Get("/api/products");
    AllowAnonymous();
    DontAutoTag();
    Description(b => b.WithTags("Products"));
  }

  public override async Task HandleAsync(ListProductsRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(
      new ListProductsQuery(
        req.Page, req.PageSize,
        req.SearchTerm, req.IsActive,
        req.CategoryId, req.MinPrice, req.MaxPrice),
      ct);

    await this.SendResultAsync(result, ct);
  }
}
