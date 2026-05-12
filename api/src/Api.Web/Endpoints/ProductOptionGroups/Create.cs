using Api.UseCases.ProductOptionGroups.Create;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.ProductOptionGroups;

public class ProductOptionValueRequest
{
  public string Name { get; set; } = string.Empty;
  public decimal Price { get; set; }
  public decimal? CostPrice { get; set; }
}

public class CreateProductOptionGroupRequest
{
  public string Name { get; set; } = string.Empty;
  public bool IsRequired { get; set; }
  public bool AllowMultiple { get; set; }
  public bool AllowQuantity { get; set; }
  public List<ProductOptionValueRequest> Values { get; set; } = [];
  public List<int> ProductIds { get; set; } = [];
}

public class Create(IMediator mediator) : Endpoint<CreateProductOptionGroupRequest>
{
  public override void Configure()
  {
    Post("/api/product-option-groups");
    Policies("product.create");
    DontAutoTag();
    Description(b => b.WithTags("ProductOptionGroups"));
  }

  public override async Task HandleAsync(CreateProductOptionGroupRequest req, CancellationToken ct)
  {
    var command = new CreateProductOptionGroupCommand(
      req.Name,
      req.IsRequired,
      req.AllowMultiple,
      req.AllowQuantity,
      req.Values.Select(v => new ProductOptionValueInput(v.Name, v.Price, v.CostPrice)).ToList(),
      req.ProductIds);

    var result = await mediator.Send(command, ct);
    await this.SendResultAsync(result, ct);
  }
}
