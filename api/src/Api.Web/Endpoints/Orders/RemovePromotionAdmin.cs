using Api.UseCases.Orders.DTOs;
using Api.UseCases.Promotions.Remove;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Orders;

public sealed class RemovePromotionAdminRequest
{
  public int OrderId { get; set; }
  public int PromotionId { get; set; }
}

public class RemovePromotionAdmin(IMediator mediator) : Endpoint<RemovePromotionAdminRequest, OrderDto>
{
  public override void Configure()
  {
    Delete("/api/admin/orders/{orderId}/promotions/{promotionId}");
    Policies("StaffOrAdmin");
    DontAutoTag();
    Description(b => b.WithTags("Orders"));
  }

  public override async Task HandleAsync(RemovePromotionAdminRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(
      new RemovePromotionCommand(req.OrderId, req.PromotionId, null), ct);
    await this.SendResultAsync(result, ct);
  }
}
