using Api.UseCases.Categories.DTOs;
using Api.UseCases.Categories.Get;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Categories;

public class GetCategoryRequest
{
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
    Summary(s => s.Summary = "Lấy chi tiết category theo Id");
  }

  public override async Task HandleAsync(GetCategoryRequest req, CancellationToken ct)
  {
    var result = await _mediator.Send(new GetCategoryQuery(req.CategoryId), ct);

    await this.SendResultAsync(result, ct);
  }
}
