using Api.UseCases.Products.DTOs;
using Api.UseCases.Products.Get;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Products;

public class GetProductRequest
{
  public int ProductId { get; set; }
}

public class Get : Endpoint<GetProductRequest, ProductDto>
{
  private readonly IMediator _mediator;

  public Get(IMediator mediator)
  {
    _mediator = mediator;
  }

  public override void Configure()
  {
    Get("/api/products/{ProductId}");
    AllowAnonymous();
    DontAutoTag();
    Summary(s => s.Summary = "Lấy chi tiết product theo Id");
  }

  public override async Task HandleAsync(GetProductRequest req, CancellationToken ct)
  {
    var result = await _mediator.Send(new GetProductQuery(req.ProductId), ct);

    await this.SendResultAsync(result, ct);
  }
}
