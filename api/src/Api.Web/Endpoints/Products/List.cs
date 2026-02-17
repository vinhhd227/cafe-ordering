using Api.UseCases.Products.DTOs;
using Api.UseCases.Products.List;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Products;

public class ListProductsRequest
{
  [QueryParam] public int Page { get; set; } = 1;
  [QueryParam] public int PageSize { get; set; } = 10;
  [QueryParam] public string? SearchTerm { get; set; }
}

public class List : Endpoint<ListProductsRequest, PagedResult<ProductSummaryDto>>
{
  private readonly IMediator _mediator;

  public List(IMediator mediator)
  {
    _mediator = mediator;
  }

  public override void Configure()
  {
    Get("/api/products");
    AllowAnonymous();
    DontAutoTag();
    Summary(s => s.Summary = "Danh sách products phân trang");
  }

  public override async Task HandleAsync(ListProductsRequest req, CancellationToken ct)
  {
    var result = await _mediator.Send(new ListProductsQuery(req.Page, req.PageSize, req.SearchTerm), ct); // ← Bỏ (object)

    await this.SendResultAsync(result, ct);
  }
}
