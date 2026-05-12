using Api.UseCases.ProductOptionGroups.AssignToProduct;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.ProductOptionGroups;

public class AssignOptionGroupsRequest
{
  public int ProductId { get; set; }
  public List<int> GroupIds { get; set; } = [];
}

public class AssignToProduct(IMediator mediator) : Ep.Req<AssignOptionGroupsRequest>.NoRes
{
  public override void Configure()
  {
    Put("/api/products/{ProductId}/assigned-option-groups");
    Policies("product.update");
    DontAutoTag();
    Description(b => b.WithTags("ProductOptionGroups"));
  }

  public override async Task HandleAsync(AssignOptionGroupsRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(
      new AssignOptionGroupsToProductCommand(req.ProductId, req.GroupIds), ct);
    await this.SendResultAsync(result, ct);
  }
}
