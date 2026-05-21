using Api.Core.Aggregates.ProductAggregate;
using Api.UseCases.Products.Create;
using Api.UseCases.Products.VariantGroups;
using Api.UseCases.Products.Variants;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Products;

public sealed class CreateVariantValueRequest
{
  public string Label { get; set; } = string.Empty;
  public decimal Price { get; set; }
  public bool IsDefault { get; set; }
}

public sealed class CreateVariantGroupRequest
{
  public string Name { get; set; } = string.Empty;
  public bool IsRequired { get; set; }
  public string SelectionType { get; set; } = "Single";
  public List<CreateVariantValueRequest> Values { get; set; } = [];
}

public sealed class ProductVariantByLabelRequest
{
  public List<string> ValueLabels { get; set; } = [];
  public decimal Price { get; set; }
  public decimal? CostPrice { get; set; }
  public string? Sku { get; set; }
  public string? Barcode { get; set; }
  public bool IsActive { get; set; } = true;
}

public sealed class CreateProductRequest
{
  public string Name { get; set; } = string.Empty;
  public decimal Price { get; set; }
  public int? CategoryId { get; set; }
  public string? Description { get; set; }
  public string? ImageUrl { get; set; }
  public bool IsAccompaniment { get; set; }
  public int? EstimatedPrepMinutes { get; set; }
  public decimal? CostPrice { get; set; }
  public decimal? DiscountPrice { get; set; }
  public string? Sku { get; set; }
  public string? Barcode { get; set; }
  public List<CreateVariantGroupRequest>? VariantGroups { get; set; }
  public List<ProductVariantByLabelRequest>? Variants { get; set; }
}

public class Create(IMediator mediator) : Ep.Req<CreateProductRequest>.NoRes
{
  public override void Configure()
  {
    Post("/api/products");
    Policies("product.create");
    DontAutoTag();
    Description(b => b.WithTags("Products"));
  }

  public override async Task HandleAsync(CreateProductRequest req, CancellationToken ct)
  {
    IReadOnlyList<VariantGroupInput>? variantGroups = null;
    if (req.VariantGroups is { Count: > 0 })
    {
      var parsed = new List<VariantGroupInput>();
      foreach (var g in req.VariantGroups)
      {
        if (!Enum.TryParse<OptionSelectionType>(g.SelectionType, true, out var selectionType))
          selectionType = OptionSelectionType.Single;

        parsed.Add(new VariantGroupInput(
          g.Name,
          g.IsRequired,
          selectionType,
          g.Values.Select(v => new VariantValueInput(v.Label, v.Price, v.IsDefault)).ToList()));
      }
      variantGroups = parsed;
    }

    var command = new CreateProductCommand(
      req.Name,
      req.Price,
      req.CategoryId,
      req.Description,
      req.ImageUrl,
      req.IsAccompaniment,
      req.EstimatedPrepMinutes,
      req.CostPrice,
      req.DiscountPrice,
      req.Sku,
      req.Barcode,
      variantGroups,
      req.Variants?
        .Select(v => new ProductVariantLabelInput(
          v.ValueLabels,
          v.Price,
          v.CostPrice,
          v.Sku,
          v.Barcode,
          v.IsActive))
        .ToList());

    var result = await mediator.Send(command, ct);

    if (result.IsSuccess)
    {
      await Send.ResponseAsync(new { Id = result.Value }, StatusCodes.Status201Created, ct);
      return;
    }

    await this.SendResultAsync(result, ct);
  }
}
