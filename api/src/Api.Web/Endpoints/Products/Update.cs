using Api.UseCases.Products.Update;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Products;

public sealed class UpdateProductRequest
{
  public int ProductId { get; set; }
  public int? CategoryId { get; set; }
  public string Name { get; set; } = string.Empty;
  public decimal Price { get; set; }
  public string? Description { get; set; }
  public string? ImageUrl { get; set; }
  public bool IsActive { get; set; } = true;
  public bool IsAccompaniment { get; set; }
  public int? EstimatedPrepMinutes { get; set; }
  public decimal? CostPrice { get; set; }
  public decimal? DiscountPrice { get; set; }
  public string? Sku { get; set; }
  public string? Barcode { get; set; }
}

public class Update : Endpoint<UpdateProductRequest>
{
  private readonly IMediator _mediator;

  public Update(IMediator mediator)
  {
    _mediator = mediator;
  }

  public override void Configure()
  {
    Put("/api/products/{ProductId}");
    Policies("product.update");
    DontAutoTag();
    Description(b => b.WithTags("Products"));
  }

  public override async Task HandleAsync(UpdateProductRequest req, CancellationToken ct)
  {
    var result = await _mediator.Send(
      new UpdateProductCommand(
        req.ProductId,
        req.Name,
        req.Price,
        req.IsActive,
        req.CategoryId,
        req.IsAccompaniment,
        req.Description,
        req.ImageUrl,
        req.EstimatedPrepMinutes,
        req.CostPrice,
        req.DiscountPrice,
        req.Sku,
        req.Barcode), ct);

    await this.SendResultAsync(result, ct);
  }
}
