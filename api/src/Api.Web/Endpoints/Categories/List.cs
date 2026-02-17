using Api.UseCases.Categories.DTOs;
using Api.UseCases.Categories.List;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Categories;

public class ListCategoriesRequest
{
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
    Summary(s => s.Summary = "Danh sách categories");
  }

  public override async Task HandleAsync(ListCategoriesRequest req, CancellationToken ct)
  {
    var result = await _mediator.Send(new ListCategoriesQuery(req.ActiveOnly), ct);

    await this.SendResultAsync(result, ct);
  }
}
