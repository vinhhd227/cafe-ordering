using Api.UseCases.ProductOptionGroups.Create;
using Api.UseCases.ProductOptionGroups.Update;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.ProductOptionGroups;

public class UpdateProductOptionGroupRequest
{
  public int Id { get; set; }
  public string Name { get; set; } = string.Empty;
  public bool IsRequired { get; set; }
  public bool AllowMultiple { get; set; }
  public bool AllowQuantity { get; set; }
  public List<ProductOptionValueRequest> Values { get; set; } = [];
}

public class Update(IMediator mediator) : Ep.Req<UpdateProductOptionGroupRequest>.NoRes
{
  public override void Configure()
  {
    Put("/api/product-option-groups/{Id}");
    Policies("product.update");
    DontAutoTag();
    Description(b => b.WithTags("ProductOptionGroups"));
  }

  public override async Task HandleAsync(UpdateProductOptionGroupRequest req, CancellationToken ct)
  {
    var command = new UpdateProductOptionGroupCommand(
      req.Id,
      req.Name,
      req.IsRequired,
      req.AllowMultiple,
      req.AllowQuantity,
      req.Values.Select(v => new ProductOptionValueInput(v.Name, v.Price, v.CostPrice)).ToList());

    var result = await mediator.Send(command, ct);
    await this.SendResultAsync(result, ct);
  }
}
