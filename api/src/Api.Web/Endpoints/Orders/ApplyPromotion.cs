using Api.UseCases.Orders.DTOs;
using Api.UseCases.Promotions.Apply;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Orders;

public sealed class ApplyPromotionRequest
{
  public int OrderId { get; set; }
  public string? Code { get; set; }
  public Guid SessionId { get; set; }
}

public class ApplyPromotion(IMediator mediator) : Endpoint<ApplyPromotionRequest, OrderDto>
{
  public override void Configure()
  {
    Post("/api/orders/{orderId}/promotions");
    AllowAnonymous();
    DontAutoTag();
    Description(b => b.WithTags("Orders"));
  }

  public override async Task HandleAsync(ApplyPromotionRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(
      new ApplyPromotionCommand(req.OrderId, req.Code, null, req.SessionId), ct);
    await this.SendResultAsync(result, ct);
  }
}
