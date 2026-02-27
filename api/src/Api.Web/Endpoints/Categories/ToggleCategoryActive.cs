using Api.UseCases.Categories.ToggleActive;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Categories;

public sealed class ToggleCategoryActiveRequest
{
  public int CategoryId { get; set; }
}

public class ToggleCategoryActive(IMediator mediator) : Endpoint<ToggleCategoryActiveRequest>
{
  public override void Configure()
  {
    Patch("/api/categories/{CategoryId}/toggle-active");
    Policies("category.update");
    DontAutoTag();
    Description(b => b.WithTags("Categories"));
  }

  public override async Task HandleAsync(ToggleCategoryActiveRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(new ToggleCategoryActiveCommand(req.CategoryId), ct);
    await this.SendResultAsync(result, ct);
  }
}
