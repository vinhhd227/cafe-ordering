using System.Security.Claims;
using Api.UseCases.Products.Delete;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Products;

/// <summary>
/// Route parameters for soft-deleting a product.
/// </summary>
public sealed class DeleteProductRequest
{
  /// <summary>The integer ID of the product to delete.</summary>
  public int ProductId { get; set; }
}

public class Delete(IMediator mediator) : Ep.Req<DeleteProductRequest>.NoRes
{
  public override void Configure()
  {
    Delete("/api/products/{ProductId}");
    AllowAnonymous();
    DontAutoTag();
    Description(b => b.WithTags("Products"));
  }

  public override async Task HandleAsync(DeleteProductRequest req, CancellationToken ct)
  {
    var deletedBy = User.FindFirstValue(ClaimTypes.Name) ?? "system";

    var result = await mediator.Send(
      new DeleteProductCommand(req.ProductId, deletedBy), ct);

    await this.SendResultAsync(result, ct);
  }
}
