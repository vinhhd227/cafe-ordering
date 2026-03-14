using Api.UseCases.Products.Tree;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Products;

public class GetTree(IMediator mediator) : EndpointWithoutRequest<List<ProductTreeCategoryDto>>
{
  public override void Configure()
  {
    Get("/api/admin/products/tree");
    Policies("product.read");
    DontAutoTag();
    Description(b => b.WithTags("Products"));
  }

  public override async Task HandleAsync(CancellationToken ct)
  {
    var result = await mediator.Send(new GetProductTreeQuery(), ct);
    await this.SendResultAsync(result, ct);
  }
}
