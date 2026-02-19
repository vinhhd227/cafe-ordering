using Api.UseCases.Categories.DTOs;
using Api.UseCases.Categories.Get;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Categories;

/// <summary>
/// Route parameters for retrieving a single category.
/// </summary>
public sealed class GetCategoryRequest
{
  /// <summary>The unique integer identifier of the category.</summary>
  public int CategoryId { get; set; }
}

public class Get : Endpoint<GetCategoryRequest, CategoryDto>
{
  private readonly IMediator _mediator;

  public Get(IMediator mediator)
  {
    _mediator = mediator;
  }

  public override void Configure()
  {
    Get("/api/categories/{CategoryId}");
    AllowAnonymous();
    DontAutoTag();
    Description(b => b.WithTags("Categories"));
  }

  public override async Task HandleAsync(GetCategoryRequest req, CancellationToken ct)
  {
    var result = await _mediator.Send(new GetCategoryQuery(req.CategoryId), ct);

    await this.SendResultAsync(result, ct);
  }
}
