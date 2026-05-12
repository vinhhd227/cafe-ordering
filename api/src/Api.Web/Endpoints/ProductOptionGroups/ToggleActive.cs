using Api.UseCases.ProductOptionGroups.ToggleActive;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.ProductOptionGroups;

public class ToggleActiveRequest { public int Id { get; set; } }

public class ToggleActive(IMediator mediator) : Ep.Req<ToggleActiveRequest>.NoRes
{
  public override void Configure()
  {
    Patch("/api/product-option-groups/{Id}/toggle-active");
    Policies("product.update");
    DontAutoTag();
    Description(b => b.WithTags("ProductOptionGroups"));
  }

  public override async Task HandleAsync(ToggleActiveRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(new ToggleProductOptionGroupCommand(req.Id), ct);
    await this.SendResultAsync(result, ct);
  }
}
