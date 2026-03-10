using Api.UseCases.Promotions.DTOs;
using Api.UseCases.Promotions.Get;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Promotions;

public sealed class GetPromotionByIdRequest
{
  public int Id { get; set; }
}

public class GetPromotionById(IMediator mediator) : Endpoint<GetPromotionByIdRequest, PromotionDto>
{
  public override void Configure()
  {
    Get("/api/admin/promotions/{id}");
    Policies("StaffOrAdmin");
    DontAutoTag();
    Description(b => b.WithTags("Promotions"));
  }

  public override async Task HandleAsync(GetPromotionByIdRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(new GetPromotionQuery(req.Id, null), ct);
    await this.SendResultAsync(result, ct);
  }
}
