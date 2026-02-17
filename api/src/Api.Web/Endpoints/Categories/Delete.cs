using System.Security.Claims;
using Api.UseCases.Categories.Delete;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Categories;

public class DeleteCategoryRequest
{
  public int CategoryId { get; set; }
}

public class Delete : Endpoint<DeleteCategoryRequest>
{
  private readonly IMediator _mediator;

  public Delete(IMediator mediator)
  {
    _mediator = mediator;
  }

  public override void Configure()
  {
    Delete("/api/categories/{CategoryId}");
    AllowAnonymous();
    Summary(s => s.Summary = "Soft delete category");
  }

  public override async Task HandleAsync(DeleteCategoryRequest req, CancellationToken ct)
  {
    var deletedBy = User.FindFirstValue(ClaimTypes.Name) ?? "system";

    var result = await _mediator.Send(
      new DeleteCategoryCommand(req.CategoryId, deletedBy), ct);

    await this.SendResultAsync(result, ct);
  }
}
