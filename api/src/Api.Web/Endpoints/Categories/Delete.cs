using System.Security.Claims;
using Api.UseCases.Categories.Delete;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Categories;

/// <summary>
/// Route parameters for soft-deleting a category.
/// </summary>
public sealed class DeleteCategoryRequest
{
  /// <summary>The integer ID of the category to delete.</summary>
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
    DontAutoTag();
    Description(b => b.WithTags("Categories"));
  }

  public override async Task HandleAsync(DeleteCategoryRequest req, CancellationToken ct)
  {
    var deletedBy = User.FindFirstValue(ClaimTypes.Name) ?? "system";

    var result = await _mediator.Send(
      new DeleteCategoryCommand(req.CategoryId, deletedBy), ct);

    await this.SendResultAsync(result, ct);
  }
}
