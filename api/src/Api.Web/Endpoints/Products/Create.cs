using Api.UseCases.Products.Create;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Products;

/// <summary>
/// Request payload for creating a new product.
/// </summary>
public sealed class CreateProductRequest
{
  /// <summary>ID of the category this product belongs to.</summary>
  public int CategoryId { get; set; }

  /// <summary>Display name of the product (e.g. "Caramel Macchiato").</summary>
  public string Name { get; set; } = string.Empty;

  /// <summary>Base selling price in VND. Must be greater than zero.</summary>
  public decimal Price { get; set; }

  /// <summary>Optional short description shown on the menu card.</summary>
  public string? Description { get; set; }

  /// <summary>Optional publicly accessible URL of the product thumbnail image.</summary>
  public string? ImageUrl { get; set; }

  /// <summary>
  /// When <c>true</c>, customers can choose a temperature option
  /// (Hot / Iced / Blended). Typically enabled for beverages.
  /// </summary>
  public bool HasTemperatureOption { get; set; }

  /// <summary>
  /// When <c>true</c>, customers can select their preferred ice level
  /// (No Ice / Less Ice / Normal / Extra Ice).
  /// </summary>
  public bool HasIceLevelOption { get; set; }

  /// <summary>
  /// When <c>true</c>, customers can select their preferred sugar level
  /// (0 % / 25 % / 50 % / 75 % / 100 %).
  /// </summary>
  public bool HasSugarLevelOption { get; set; }
}

public class Create(IMediator mediator) : Ep.Req<CreateProductRequest>.NoRes
{
  public override void Configure()
  {
    Post("/api/products");
    AllowAnonymous();
    DontAutoTag();
    Description(b => b.WithTags("Products"));
  }

  public override async Task HandleAsync(CreateProductRequest req, CancellationToken ct)
  {
    var command = new CreateProductCommand(
      req.CategoryId,
      req.Name,
      req.Price,
      req.Description,
      req.ImageUrl,
      req.HasTemperatureOption,
      req.HasIceLevelOption,
      req.HasSugarLevelOption);

    var result = await mediator.Send(command, ct);

    if (result.IsSuccess)
    {
      await SendAsync(new { Id = result.Value }, StatusCodes.Status201Created, ct);
      return;
    }

    await this.SendResultAsync(result, ct);
  }
}
