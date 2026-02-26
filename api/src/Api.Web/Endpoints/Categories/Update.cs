using Api.UseCases.Categories.Update;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Categories;

/// <summary>
/// Request payload for renaming a category.
/// </summary>
public sealed class UpdateCategoryRequest
{
  /// <summary>
  /// ID of the category to update. Provided as a route parameter
  /// (<c>/api/categories/{CategoryId}</c>), not in the request body.
  /// </summary>
  public int CategoryId { get; set; }

  /// <summary>New display name for the category. Must be unique and non-empty.</summary>
  public string Name { get; set; } = string.Empty;

  /// <summary>Optional description for the category.</summary>
  public string? Description { get; set; }

  /// <summary>Whether the category is active (visible on the menu).</summary>
  public bool IsActive { get; set; } = true;
}

public class Update : Endpoint<UpdateCategoryRequest>
{
  private readonly IMediator _mediator;

  public Update(IMediator mediator)
  {
    _mediator = mediator;
  }

  public override void Configure()
  {
    Put("/api/categories/{CategoryId}");
    AllowAnonymous();
    DontAutoTag();
    Description(b => b.WithTags("Categories"));
  }

  public override async Task HandleAsync(UpdateCategoryRequest req, CancellationToken ct)
  {
    var result = await _mediator.Send(
      new UpdateCategoryCommand(req.CategoryId, req.Name, req.Description, req.IsActive), ct);

    await this.SendResultAsync(result, ct);
  }
}
