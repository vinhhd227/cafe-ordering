using Api.UseCases.Products.Update;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Products;

/// <summary>
/// Request payload for updating a product's basic details.
/// </summary>
public sealed class UpdateProductRequest
{
  /// <summary>
  /// ID of the product to update. Provided as a route parameter
  /// (<c>/api/products/{ProductId}</c>), not in the request body.
  /// </summary>
  public int ProductId { get; set; }

  /// <summary>New display name for the product.</summary>
  public string Name { get; set; } = string.Empty;

  /// <summary>New base selling price in VND. Must be greater than zero.</summary>
  public decimal Price { get; set; }

  /// <summary>Updated short description shown on the menu card. Pass <c>null</c> to clear.</summary>
  public string? Description { get; set; }

  /// <summary>Updated image URL. Pass <c>null</c> to clear.</summary>
  public string? ImageUrl { get; set; }
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
    AllowAnonymous();
    DontAutoTag();
    Description(b => b.WithTags("Products"));
  }

  public override async Task HandleAsync(UpdateProductRequest req, CancellationToken ct)
  {
    var result = await _mediator.Send(
      new UpdateProductCommand(req.ProductId, req.Name, req.Price, req.Description, req.ImageUrl), ct);

    await this.SendResultAsync(result, ct);
  }
}
