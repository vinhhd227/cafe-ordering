using Api.UseCases.ProductOptionGroups.Get;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.ProductOptionGroups;

public class GetByIdRequest { public int Id { get; set; } }

public class GetById(IMediator mediator) : Endpoint<GetByIdRequest>
{
  public override void Configure()
  {
    Get("/api/product-option-groups/{Id}");
    Policies("product.read");
    DontAutoTag();
    Description(b => b.WithTags("ProductOptionGroups"));
  }

  public override async Task HandleAsync(GetByIdRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(new GetProductOptionGroupQuery(req.Id), ct);
    await this.SendResultAsync(result, ct);
  }
}
