using Api.UseCases.Categories.Create;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Categories;

/// <summary>
/// Request payload for creating a new category.
/// </summary>
public sealed class CreateCategoryRequest
{
  /// <summary>Display name for the new category. Must be unique and non-empty.</summary>
  public string Name { get; set; } = string.Empty;

  /// <summary>Optional description for the category.</summary>
  public string? Description { get; set; }
}

public class Create : Endpoint<CreateCategoryRequest>
{
  private readonly IMediator _mediator;

  public Create(IMediator mediator)
  {
    _mediator = mediator;
  }

  public override void Configure()
  {
    Post("/api/categories");
    AllowAnonymous();
    DontAutoTag();
    Description(b => b.WithTags("Categories"));
  }

  public override async Task HandleAsync(CreateCategoryRequest req, CancellationToken ct)
  {
    var result = await _mediator.Send(
      new CreateCategoryCommand(req.Name, req.Description), ct);

    await this.SendResultAsync(result, ct);
  }
}
