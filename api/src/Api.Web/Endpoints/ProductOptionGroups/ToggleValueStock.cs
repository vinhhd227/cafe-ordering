using Api.UseCases.ProductOptionGroups.ToggleValueStock;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.ProductOptionGroups;

public class ToggleValueStockRequest
{
  public int Id { get; set; }
  public int ValueId { get; set; }
}

public class ToggleValueStock(IMediator mediator) : Ep.Req<ToggleValueStockRequest>.NoRes
{
  public override void Configure()
  {
    Patch("/api/product-option-groups/{Id}/values/{ValueId}/toggle-stock");
    Policies("product.update");
    DontAutoTag();
    Description(b => b.WithTags("ProductOptionGroups"));
  }

  public override async Task HandleAsync(ToggleValueStockRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(new ToggleValueStockCommand(req.Id, req.ValueId), ct);
    await this.SendResultAsync(result, ct);
  }
}
