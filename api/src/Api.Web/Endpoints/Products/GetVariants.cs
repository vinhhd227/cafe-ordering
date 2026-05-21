using Api.UseCases.Products.Variants;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Products;

public sealed class GetProductVariantsRequest
{
  public int ProductId { get; set; }
}

public class GetVariants(IMediator mediator)
  : Endpoint<GetProductVariantsRequest, List<ProductVariantResultDto>>
{
  public override void Configure()
  {
    Get("/api/products/{ProductId}/variants");
    Policies("product.view");
    DontAutoTag();
    Description(b => b.WithTags("Products"));
  }

  public override async Task HandleAsync(GetProductVariantsRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(new GetProductVariantsQuery(req.ProductId), ct);
    await this.SendResultAsync(result, ct);
  }
}
