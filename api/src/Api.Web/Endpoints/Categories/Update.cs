using Api.UseCases.Categories.Update;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Categories;

public class UpdateCategoryRequest
{
  public int CategoryId { get; set; }
  public string Name { get; set; } = string.Empty;
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
    Summary(s => s.Summary = "Cập nhật tên category");
  }

  public override async Task HandleAsync(UpdateCategoryRequest req, CancellationToken ct)
  {
    var result = await _mediator.Send(
      new UpdateCategoryCommand(req.CategoryId, req.Name), ct);

    await this.SendResultAsync(result, ct);
  }
}
