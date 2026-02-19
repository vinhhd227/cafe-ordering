using Api.UseCases.Categories.DTOs;
using Api.UseCases.Categories.List;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Categories;

/// <summary>
/// Query parameters for the category list.
/// </summary>
public sealed class ListCategoriesRequest
{
  /// <summary>
  /// When <c>true</c>, only categories with <c>IsActive = true</c> are returned.
  /// When <c>false</c> (default), all categories including inactive ones are returned.
  /// </summary>
  [QueryParam] public bool ActiveOnly { get; set; } = false;
}

public class List : Endpoint<ListCategoriesRequest, List<CategoryDto>>
{
  private readonly IMediator _mediator;

  public List(IMediator mediator)
  {
    _mediator = mediator;
  }

  public override void Configure()
  {
    Get("/api/categories");
    AllowAnonymous();
    DontAutoTag();
    Description(b => b.WithTags("Categories"));
  }

  public override async Task HandleAsync(ListCategoriesRequest req, CancellationToken ct)
  {
    var result = await _mediator.Send(new ListCategoriesQuery(req.ActiveOnly), ct);

    await this.SendResultAsync(result, ct);
  }
}
