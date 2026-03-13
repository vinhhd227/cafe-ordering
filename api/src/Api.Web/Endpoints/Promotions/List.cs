using Api.UseCases.Promotions.List;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Promotions;

public sealed class ListPromotionsRequest
{
  public bool? IsActive { get; set; }
  public string? Scope { get; set; }
  public string? DiscountType { get; set; }
  public int Page { get; set; } = 1;
  public int PageSize { get; set; } = 20;
}

public class ListPromotions(IMediator mediator) : Endpoint<ListPromotionsRequest, PagedPromotionsDto>
{
  public override void Configure()
  {
    Get("/api/admin/promotions");
    Policies("promotion.read");
    DontAutoTag();
    Description(b => b.WithTags("Promotions"));
  }

  public override async Task HandleAsync(ListPromotionsRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(
      new ListPromotionsQuery(req.IsActive, req.Scope, req.DiscountType, req.Page, req.PageSize), ct);

    await this.SendResultAsync(result, ct);
  }
}
