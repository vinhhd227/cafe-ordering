using Api.Core.Aggregates.ProductAggregate;
using Api.UseCases.Products.Create;
using Api.UseCases.Products.OptionGroups;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Products;

public sealed class CreateAttributeValueRequest
{
  public string Label { get; set; } = string.Empty;
  public decimal PriceAdjustment { get; set; }
  public bool IsDefault { get; set; }
}

public sealed class CreateAttributeGroupRequest
{
  public string Name { get; set; } = string.Empty;
  public bool IsRequired { get; set; }
  public string SelectionType { get; set; } = "Single";
  public List<CreateAttributeValueRequest> Values { get; set; } = [];
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
  public List<CreateAttributeGroupRequest>? AttributeGroups { get; set; }
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
    IReadOnlyList<AttributeGroupInput>? attributeGroups = null;
    if (req.AttributeGroups is { Count: > 0 })
    {
      var parsed = new List<AttributeGroupInput>();
      foreach (var g in req.AttributeGroups)
      {
        if (!Enum.TryParse<OptionSelectionType>(g.SelectionType, true, out var selectionType))
          selectionType = OptionSelectionType.Single;

        parsed.Add(new AttributeGroupInput(
          g.Name,
          g.IsRequired,
          selectionType,
          g.Values.Select(v => new AttributeValueInput(v.Label, v.PriceAdjustment, v.IsDefault)).ToList()));
      }
      attributeGroups = parsed;
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
      attributeGroups);

    var result = await mediator.Send(command, ct);

    if (result.IsSuccess)
    {
      await Send.ResponseAsync(new { Id = result.Value }, StatusCodes.Status201Created, ct);
      return;
    }

    await this.SendResultAsync(result, ct);
  }
}
