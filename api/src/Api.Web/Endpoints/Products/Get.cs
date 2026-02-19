using Api.UseCases.Products.DTOs;
using Api.UseCases.Products.Get;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Products;

/// <summary>
/// Route parameters for retrieving a single product.
/// </summary>
public sealed class GetProductRequest
{
  /// <summary>The unique integer identifier of the product.</summary>
  public int ProductId { get; set; }
}

public class Get(IMediator mediator) : Ep.Req<GetProductRequest>.Res<ProductDto>
{
  public override void Configure()
  {
    Get("/api/products/{ProductId}");
    AllowAnonymous();
    DontAutoTag();
    Description(b => b.WithTags("Products"));
  }

  public override async Task HandleAsync(GetProductRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(new GetProductQuery(req.ProductId), ct);

    await this.SendResultAsync(result, ct);
  }
}
