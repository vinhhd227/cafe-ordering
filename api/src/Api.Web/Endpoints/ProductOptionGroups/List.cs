using Api.UseCases.ProductOptionGroups.List;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.ProductOptionGroups;

public class ListProductOptionGroupsRequest
{
  public string? Search { get; set; }
  public bool? IsActive { get; set; }
}

public class List(IMediator mediator) : Endpoint<ListProductOptionGroupsRequest>
{
  public override void Configure()
  {
    Get("/api/product-option-groups");
    Policies("product.read");
    DontAutoTag();
    Description(b => b.WithTags("ProductOptionGroups"));
  }

  public override async Task HandleAsync(ListProductOptionGroupsRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(
      new ListProductOptionGroupsQuery(req.Search, req.IsActive), ct);
    await this.SendResultAsync(result, ct);
  }
}
