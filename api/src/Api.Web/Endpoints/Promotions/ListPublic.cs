using Api.UseCases.Promotions.DTOs;
using Api.UseCases.Promotions.ListPublic;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Promotions;

public class ListPublicPromotions(IMediator mediator) : EndpointWithoutRequest<List<PromotionDto>>
{
  public override void Configure()
  {
    Get("/api/promotions/public");
    AllowAnonymous();
    DontAutoTag();
    Description(b => b.WithTags("Promotions"));
  }

  public override async Task HandleAsync(CancellationToken ct)
  {
    var result = await mediator.Send(new ListPublicPromotionsQuery(), ct);
    await this.SendResultAsync(result, ct);
  }
}

public class ListPublicPromotionsSummary : Summary<ListPublicPromotions>
{
  public ListPublicPromotionsSummary()
  {
    Summary = "List public promotions";
    Description = "Returns all active PUBLIC promotions available for clients to browse and apply. No authentication required.";
    Response<List<PromotionDto>>(200, "List of available public promotions.");
  }
}
