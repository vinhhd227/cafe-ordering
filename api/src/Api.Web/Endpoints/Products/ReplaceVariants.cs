using Api.UseCases.Products.Variants;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Products;

public sealed class ProductVariantRequest
{
  public List<int> ValueIds { get; set; } = [];
  public decimal Price { get; set; }
  public decimal? CostPrice { get; set; }
  public string? Sku { get; set; }
  public string? Barcode { get; set; }
  public bool IsActive { get; set; } = true;
}

public sealed class ReplaceProductVariantsRequest
{
  public int ProductId { get; set; }
  public List<ProductVariantRequest> Variants { get; set; } = [];
}

public class ReplaceVariants(IMediator mediator)
  : Endpoint<ReplaceProductVariantsRequest, List<ProductVariantResultDto>>
{
  public override void Configure()
  {
    Put("/api/products/{ProductId}/variants");
    Policies("product.update");
    DontAutoTag();
    Description(b => b.WithTags("Products"));
  }

  public override async Task HandleAsync(ReplaceProductVariantsRequest req, CancellationToken ct)
  {
    var command = new ReplaceProductVariantsCommand(
      req.ProductId,
      req.Variants
        .Select(v => new ProductVariantInput(
          v.ValueIds,
          v.Price,
          v.CostPrice,
          v.Sku,
          v.Barcode,
          v.IsActive))
        .ToList());

    var result = await mediator.Send(command, ct);
    await this.SendResultAsync(result, ct);
  }
}
