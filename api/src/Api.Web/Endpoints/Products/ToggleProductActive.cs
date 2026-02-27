using Api.UseCases.Products.ToggleActive;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Products;

public sealed class ToggleProductActiveRequest
{
  public int ProductId { get; set; }
}

public class ToggleProductActive(IMediator mediator) : Endpoint<ToggleProductActiveRequest>
{
  public override void Configure()
  {
    Patch("/api/products/{ProductId}/toggle-active");
    Policies("product.update");
    DontAutoTag();
    Description(b => b.WithTags("Products"));
  }

  public override async Task HandleAsync(ToggleProductActiveRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(new ToggleProductActiveCommand(req.ProductId), ct);
    await this.SendResultAsync(result, ct);
  }
}
