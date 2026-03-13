using Api.UseCases.Promotions.Validate;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Promotions;

public sealed class ValidatePromotionRequest
{
  public string Code { get; set; } = string.Empty;
  public decimal? OrderAmount { get; set; }
}

public class ValidatePromotion(IMediator mediator) : Endpoint<ValidatePromotionRequest, ValidatePromotionResult>
{
  public override void Configure()
  {
    Get("/api/promotions/validate/{code}");
    Policies("promotion.read");
    DontAutoTag();
    Description(b => b.WithTags("Promotions"));
  }

  public override async Task HandleAsync(ValidatePromotionRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(new ValidatePromotionQuery(req.Code, req.OrderAmount), ct);
    await this.SendResultAsync(result, ct);
  }
}
