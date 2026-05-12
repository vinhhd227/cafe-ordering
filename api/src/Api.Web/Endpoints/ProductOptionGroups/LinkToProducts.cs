using Api.UseCases.ProductOptionGroups.LinkToProducts;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.ProductOptionGroups;

public class LinkGroupToProductsRequest
{
  public int GroupId { get; set; }
  public List<int> ProductIds { get; set; } = [];
}

public class LinkToProducts(IMediator mediator) : Ep.Req<LinkGroupToProductsRequest>.NoRes
{
  public override void Configure()
  {
    Post("/api/product-option-groups/{GroupId}/link-products");
    Policies("product.update");
    DontAutoTag();
    Description(b => b.WithTags("ProductOptionGroups"));
  }

  public override async Task HandleAsync(LinkGroupToProductsRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(new LinkGroupToProductsCommand(req.GroupId, req.ProductIds), ct);
    await this.SendResultAsync(result, ct);
  }
}
