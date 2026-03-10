using Api.UseCases.Promotions.DTOs;
using Api.UseCases.Promotions.Toggle;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Promotions;

public sealed class TogglePromotionRequest
{
  public int Id { get; set; }
  public bool Activate { get; set; }
}

public class TogglePromotion(IMediator mediator) : Endpoint<TogglePromotionRequest, PromotionDto>
{
  public override void Configure()
  {
    Put("/api/admin/promotions/{id}/toggle");
    Policies("Admin");
    DontAutoTag();
    Description(b => b.WithTags("Promotions"));
  }

  public override async Task HandleAsync(TogglePromotionRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(new TogglePromotionCommand(req.Id, req.Activate), ct);
    await this.SendResultAsync(result, ct);
  }
}
