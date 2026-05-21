using Api.Core.Aggregates.ProductAggregate;
using Api.UseCases.Products.VariantGroups;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Products;

public sealed class VariantValueRequest
{
  public string Label { get; set; } = string.Empty;
  public decimal Price { get; set; }
  public bool IsDefault { get; set; }
}

public sealed class VariantGroupRequest
{
  public string Name { get; set; } = string.Empty;
  public bool IsRequired { get; set; }
  public string SelectionType { get; set; } = "Single";
  public List<VariantValueRequest> Values { get; set; } = [];
}

public sealed class ReplaceVariantGroupsRequest
{
  public int ProductId { get; set; }
  public List<VariantGroupRequest> Groups { get; set; } = [];
}

public class ReplaceVariantGroupsEndpoint(IMediator mediator)
  : Endpoint<ReplaceVariantGroupsRequest>
{
  public override void Configure()
  {
    Put("/api/products/{ProductId}/variant-groups");
    Policies("product.update");
    DontAutoTag();
    Description(b => b.WithTags("Products"));
  }

  public override async Task HandleAsync(ReplaceVariantGroupsRequest req, CancellationToken ct)
  {
    var command = new ReplaceProductVariantGroupsCommand(
      req.ProductId,
      req.Groups
        .Select(g =>
        {
          if (!Enum.TryParse<OptionSelectionType>(g.SelectionType, true, out var selectionType))
            selectionType = OptionSelectionType.Single;
          return new VariantGroupInput(
            g.Name,
            g.IsRequired,
            selectionType,
            g.Values.Select(v => new VariantValueInput(v.Label, v.Price, v.IsDefault)).ToList());
        })
        .ToList());

    var result = await mediator.Send(command, ct);

    await this.SendResultAsync(result, ct);
  }
}
