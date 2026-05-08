using Api.UseCases.Categories.Reorder;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Categories;

public sealed class ReorderCategoriesRequest
{
  /// <summary>Category IDs in the desired display order (first element = top of list).</summary>
  public List<int> Ids { get; set; } = [];
}

public class Reorder : Endpoint<ReorderCategoriesRequest>
{
  private readonly IMediator _mediator;

  public Reorder(IMediator mediator)
  {
    _mediator = mediator;
  }

  public override void Configure()
  {
    Put("/api/categories/reorder");
    Policies("category.update");
    DontAutoTag();
    Description(b => b.WithTags("Categories"));
  }

  public override async Task HandleAsync(ReorderCategoriesRequest req, CancellationToken ct)
  {
    var result = await _mediator.Send(new ReorderCategoriesCommand(req.Ids), ct);

    await this.SendResultAsync(result, ct);
  }
}
