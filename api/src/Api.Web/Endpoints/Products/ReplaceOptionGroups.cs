using Api.Core.Aggregates.ProductAggregate;
using Api.UseCases.Products.OptionGroups;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Products;

public sealed class AttributeValueRequest
{
  public string Label { get; set; } = string.Empty;
  public decimal PriceAdjustment { get; set; }
  public bool IsDefault { get; set; }
}

public sealed class AttributeGroupRequest
{
  public string Name { get; set; } = string.Empty;
  public bool IsRequired { get; set; }
  public string SelectionType { get; set; } = "Single";
  public List<AttributeValueRequest> Values { get; set; } = [];
}

public sealed class ReplaceAttributeGroupsRequest
{
  public int ProductId { get; set; }
  public List<AttributeGroupRequest> Groups { get; set; } = [];
}

public class ReplaceAttributeGroupsEndpoint(IMediator mediator)
  : Endpoint<ReplaceAttributeGroupsRequest>
{
  public override void Configure()
  {
    Put("/api/products/{ProductId}/option-groups");
    Policies("product.update");
    DontAutoTag();
    Description(b => b.WithTags("Products"));
  }

  public override async Task HandleAsync(ReplaceAttributeGroupsRequest req, CancellationToken ct)
  {
    var command = new ReplaceProductAttributeGroupsCommand(
      req.ProductId,
      req.Groups
        .Select(g =>
        {
          if (!Enum.TryParse<OptionSelectionType>(g.SelectionType, true, out var selectionType))
            selectionType = OptionSelectionType.Single;
          return new AttributeGroupInput(
            g.Name,
            g.IsRequired,
            selectionType,
            g.Values.Select(v => new AttributeValueInput(v.Label, v.PriceAdjustment, v.IsDefault)).ToList());
        })
        .ToList());

    var result = await mediator.Send(command, ct);

    await this.SendResultAsync(result, ct);
  }
}
