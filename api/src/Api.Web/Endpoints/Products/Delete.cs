using System.Security.Claims;
using Api.UseCases.Products.Delete;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Products;

public class DeleteProductRequest
{
  public int ProductId { get; set; }
}

public class Delete : Endpoint<DeleteProductRequest>
{
  private readonly IMediator _mediator;

  public Delete(IMediator mediator)
  {
    _mediator = mediator;
  }

  public override void Configure()
  {
    Delete("/api/products/{ProductId}");
    AllowAnonymous();
    Summary(s => s.Summary = "Soft delete product");
  }

  public override async Task HandleAsync(DeleteProductRequest req, CancellationToken ct)
  {
    var deletedBy = User.FindFirstValue(ClaimTypes.Name) ?? "system";

    var result = await _mediator.Send(
      new DeleteProductCommand(req.ProductId, deletedBy), ct);

    await this.SendResultAsync(result, ct);
  }
}
