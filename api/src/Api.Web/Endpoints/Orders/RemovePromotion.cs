using Api.UseCases.Orders.DTOs;
using Api.UseCases.Promotions.Remove;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Orders;

public sealed class RemovePromotionRequest
{
  public int OrderId { get; set; }
  public int PromotionId { get; set; }
  public Guid SessionId { get; set; }
}

public class RemovePromotion(IMediator mediator) : Endpoint<RemovePromotionRequest, OrderDto>
{
  public override void Configure()
  {
    Delete("/api/orders/{orderId}/promotions/{promotionId}");
    AllowAnonymous();
    DontAutoTag();
    Description(b => b.WithTags("Orders"));
  }

  public override async Task HandleAsync(RemovePromotionRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(
      new RemovePromotionCommand(req.OrderId, req.PromotionId, req.SessionId), ct);
    await this.SendResultAsync(result, ct);
  }
}
